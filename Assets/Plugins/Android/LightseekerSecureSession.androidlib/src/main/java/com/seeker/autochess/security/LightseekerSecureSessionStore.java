package com.seeker.autochess.security;

import android.content.Context;
import android.content.SharedPreferences;
import android.os.Build;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;

import java.nio.charset.StandardCharsets;
import java.security.KeyStore;

import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

/**
 * Stores only AES-GCM ciphertext in SharedPreferences. The non-exportable AES key
 * is generated and retained by AndroidKeyStore for this application.
 */
public final class LightseekerSecureSessionStore {
    private static final String ANDROID_KEY_STORE = "AndroidKeyStore";
    private static final String KEY_ALIAS = "lightseeker.wallet.session.aes.v1";
    private static final String PREFERENCES = "lightseeker_secure_wallet_sessions_v1";
    private static final String TRANSFORMATION = "AES/GCM/NoPadding";
    private static final int GCM_TAG_BITS = 128;

    private LightseekerSecureSessionStore() {
    }

    public static void save(Context context, String storageKey, String plaintext)
            throws Exception {
        requireSupportedDevice();
        requireArguments(context, storageKey);
        if (plaintext == null) {
            throw new IllegalArgumentException("Wallet session plaintext is required.");
        }

        Cipher cipher = Cipher.getInstance(TRANSFORMATION);
        cipher.init(Cipher.ENCRYPT_MODE, getOrCreateKey());
        byte[] ciphertext = cipher.doFinal(plaintext.getBytes(StandardCharsets.UTF_8));
        String encoded = Base64.encodeToString(cipher.getIV(), Base64.NO_WRAP)
                + "."
                + Base64.encodeToString(ciphertext, Base64.NO_WRAP);
        preferences(context).edit().putString(storageKey, encoded).commit();
    }

    public static String load(Context context, String storageKey) throws Exception {
        requireSupportedDevice();
        requireArguments(context, storageKey);
        String encoded = preferences(context).getString(storageKey, null);
        if (encoded == null || encoded.isEmpty()) {
            return null;
        }

        int separator = encoded.indexOf('.');
        if (separator <= 0 || separator >= encoded.length() - 1) {
            throw new IllegalStateException("Encrypted wallet session payload is malformed.");
        }
        byte[] iv = Base64.decode(encoded.substring(0, separator), Base64.NO_WRAP);
        byte[] ciphertext = Base64.decode(encoded.substring(separator + 1), Base64.NO_WRAP);
        Cipher cipher = Cipher.getInstance(TRANSFORMATION);
        cipher.init(Cipher.DECRYPT_MODE, getOrCreateKey(),
                new GCMParameterSpec(GCM_TAG_BITS, iv));
        byte[] plaintext = cipher.doFinal(ciphertext);
        return new String(plaintext, StandardCharsets.UTF_8);
    }

    public static void clear(Context context, String storageKey) {
        requireArguments(context, storageKey);
        preferences(context).edit().remove(storageKey).commit();
    }

    private static SecretKey getOrCreateKey() throws Exception {
        KeyStore keyStore = KeyStore.getInstance(ANDROID_KEY_STORE);
        keyStore.load(null);
        SecretKey existing = (SecretKey) keyStore.getKey(KEY_ALIAS, null);
        if (existing != null) {
            return existing;
        }

        KeyGenerator generator = KeyGenerator.getInstance(
                KeyProperties.KEY_ALGORITHM_AES, ANDROID_KEY_STORE);
        generator.init(new KeyGenParameterSpec.Builder(
                KEY_ALIAS,
                KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT)
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                .setRandomizedEncryptionRequired(true)
                .build());
        return generator.generateKey();
    }

    private static SharedPreferences preferences(Context context) {
        return context.getApplicationContext().getSharedPreferences(
                PREFERENCES, Context.MODE_PRIVATE);
    }

    private static void requireSupportedDevice() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.M) {
            throw new IllegalStateException(
                    "Encrypted wallet session storage requires Android 6.0 or newer.");
        }
    }

    private static void requireArguments(Context context, String storageKey) {
        if (context == null) {
            throw new IllegalArgumentException("Android context is required.");
        }
        if (storageKey == null || storageKey.isEmpty()) {
            throw new IllegalArgumentException("Wallet session storage key is required.");
        }
    }
}
