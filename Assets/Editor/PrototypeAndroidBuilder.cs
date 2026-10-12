using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SeekerAutoChess.Prototype;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    [InitializeOnLoad]
    public static class PrototypeAndroidBuilder
    {
        private const string ExpectedBundleVersion = "0.43.413";
        private const int ExpectedAndroidVersionCode = 651;
        // A project-root request file lets an already-open Editor build without a second Unity process.
        private const string RequestFileName = "BuildVisualTest.request";
        private const string WalletLanDiagnosticRequestFileName = "BuildWalletLanDiagnostic.request";
        private const string HarborVerificationRequestFileName = "VerifyFormalHarbor.request";
        private const string MainnetRuntimeConfigAsset =
            "Assets/Resources/MainnetInviteRuntimeConfig.json";
        private const string LoginCBackgroundAsset =
            "Assets/Resources/Art/LoginC/V102/modules/" +
            "ART-UI-LOGIN-C-BACKGROUND-1080x2400-v1.0.2.png";
        static PrototypeAndroidBuilder()
        {
            EditorApplication.update -= BuildWhenRequested;
            EditorApplication.update += BuildWhenRequested;
        }

        private static void BuildWhenRequested()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var requestPath = Path.Combine(projectRoot, RequestFileName);
            var walletDiagnosticRequestPath = Path.Combine(
                projectRoot, WalletLanDiagnosticRequestFileName);
            if (!File.Exists(requestPath) && !File.Exists(walletDiagnosticRequestPath)) return;
            // A request can arrive before Auto Refresh notices source/resource changes.
            // Force a synchronous refresh first; if it triggers compilation/domain reload,
            // the request remains and the freshly loaded builder will consume it afterwards.
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            // BuildVisualTest.request may be used only as a refresh bridge for an
            // already-open Editor whose Auto Refresh is disabled. When the dedicated
            // Harbor verification request is present, consume the bridge and never
            // start an APK build.
            if (File.Exists(requestPath) &&
                File.Exists(Path.Combine(projectRoot, HarborVerificationRequestFileName)))
            {
                File.Delete(requestPath);
                return;
            }

            EditorApplication.update -= BuildWhenRequested;
            var buildWalletDiagnostic = File.Exists(walletDiagnosticRequestPath);
            if (File.Exists(requestPath)) File.Delete(requestPath);
            if (buildWalletDiagnostic) File.Delete(walletDiagnosticRequestPath);
            try
            {
                if (buildWalletDiagnostic) BuildWalletLanDiagnosticApk();
                else BuildVisualTestApk();
            }
            finally
            {
                // A failed native/Gradle build must not disable later retries in the same Editor session.
                EditorApplication.update -= BuildWhenRequested;
                EditorApplication.update += BuildWhenRequested;
            }
        }

        [MenuItem("Seeker/Build Android Visual Test APK _F9")]
        public static void BuildVisualTestApk()
        {
            ValidateLoginCBackgroundImport();
            // The project settings file can be changed while this Editor instance is open.
            // Synchronize the in-memory PlayerSettings before naming or producing the build.
            PlayerSettings.bundleVersion = ExpectedBundleVersion;
            PlayerSettings.Android.bundleVersionCode = ExpectedAndroidVersionCode;
            AssetDatabase.SaveAssets();
            var buildVersion = PlayerSettings.bundleVersion;
            var buildVersionCode = PlayerSettings.Android.bundleVersionCode;
            if (string.IsNullOrWhiteSpace(buildVersion))
                throw new InvalidOperationException("PlayerSettings.bundleVersion must be set before building.");
            if (buildVersionCode <= 0)
                throw new InvalidOperationException("PlayerSettings.Android.bundleVersionCode must be positive before building.");

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes are configured in Editor Build Settings.");

            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../outputs"));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory,
                "SeekerAutoChess-v" + buildVersion + "-arm64-test.apk");

            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                // Device-acceptance packages use a non-development player so Unity's
                // "Development Build" watermark never leaks into the game canvas.
                options = BuildOptions.CleanBuildCache
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed: " + report.summary.result);

            Debug.Log("Android visual-test APK built: " + outputPath);
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        [MenuItem("Seeker/Build Android Wallet LAN Diagnostic APK")]
        public static void BuildWalletLanDiagnosticApk()
        {
            PlayerSettings.bundleVersion = ExpectedBundleVersion;
            PlayerSettings.Android.bundleVersionCode = ExpectedAndroidVersionCode;
            PlayerSettings.Android.forceInternetPermission = true;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
            AssetDatabase.SaveAssets();

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes are configured in Editor Build Settings.");

            var outputDirectory = Path.GetFullPath(
                Path.Combine(Application.dataPath, "../../../outputs"));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory,
                "SeekerAutoChess-v" + ExpectedBundleVersion + "-wallet-lan-diagnostic.apk");

            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(
                BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.CleanBuildCache | BuildOptions.Development
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(
                    "Android wallet LAN diagnostic build failed: " + report.summary.result);

            Debug.Log("Android wallet LAN diagnostic APK built: " + outputPath);
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        [MenuItem("Seeker/Build Android Mainnet Access Candidate APK")]
        public static void BuildMainnetInviteCandidateApk()
        {
            ValidateLoginCBackgroundImport();
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var applicationId = RequireEnvironment("LIGHTSEEKER_ANDROID_APPLICATION_ID");
            if (!Regex.IsMatch(applicationId,
                    "^[a-z][a-z0-9_]*(\\.[a-z][a-z0-9_]*){2,}$",
                    RegexOptions.CultureInvariant) ||
                applicationId.IndexOf("prototype", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException(
                    "LIGHTSEEKER_ANDROID_APPLICATION_ID must be a permanent reverse-DNS identifier and cannot contain 'prototype'.");

            var requestedKeystorePath =
                RequireEnvironment("LIGHTSEEKER_ANDROID_KEYSTORE_PATH");
            if (!Path.IsPathFullyQualified(requestedKeystorePath))
                throw new InvalidOperationException(
                    "LIGHTSEEKER_ANDROID_KEYSTORE_PATH must be absolute.");
            var keystorePath = Path.GetFullPath(requestedKeystorePath);
            if (!File.Exists(keystorePath))
                throw new InvalidOperationException(
                    "LIGHTSEEKER_ANDROID_KEYSTORE_PATH must identify an existing absolute file.");
            if (string.Equals(Path.GetFileName(keystorePath), "debug.keystore",
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "The Android debug keystore is forbidden for a mainnet access candidate.");
            if (IsInsideDirectory(keystorePath, projectRoot))
                throw new InvalidOperationException(
                    "The Android release keystore must be stored outside the Unity project.");

            var runtimeConfiguration = new MainnetInviteRuntimeConfiguration
            {
                serverBaseUrl = RequireEnvironment("LIGHTSEEKER_MAINNET_API_URI"),
                identityUri = RequireEnvironment("LIGHTSEEKER_MAINNET_IDENTITY_URI"),
                iconUri = RequireEnvironment("LIGHTSEEKER_MAINNET_ICON_URI"),
                applicationName = RequireEnvironment("LIGHTSEEKER_MAINNET_APPLICATION_NAME"),
                chainId = "solana:mainnet",
                expeditionSeasonId = RequireEnvironment("LIGHTSEEKER_MAINNET_SEASON_ID"),
                accessMode = ReadMainnetAccessMode()
            };
            PrototypeBootstrap.ValidateMainnetInviteRuntimeConfiguration(runtimeConfiguration);

            var keystorePassword = RequireEnvironment("LIGHTSEEKER_ANDROID_KEYSTORE_PASSWORD");
            var keyAlias = RequireEnvironment("LIGHTSEEKER_ANDROID_KEY_ALIAS");
            var keyPassword = RequireEnvironment("LIGHTSEEKER_ANDROID_KEY_PASSWORD");
            var runtimeConfigPath = Path.Combine(projectRoot,
                MainnetRuntimeConfigAsset.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(runtimeConfigPath) ||
                File.Exists(runtimeConfigPath + ".meta"))
                throw new InvalidOperationException(
                    "A generated mainnet runtime configuration already exists; refusing to overwrite it.");

            var originalBundleVersion = PlayerSettings.bundleVersion;
            var originalVersionCode = PlayerSettings.Android.bundleVersionCode;
            var originalApplicationId = PlayerSettings.GetApplicationIdentifier(
                BuildTargetGroup.Android);
            var originalUseCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            var originalKeystoreName = PlayerSettings.Android.keystoreName;
            var originalKeystorePass = PlayerSettings.Android.keystorePass;
            var originalKeyaliasName = PlayerSettings.Android.keyaliasName;
            var originalKeyaliasPass = PlayerSettings.Android.keyaliasPass;
            var originalForceInternetPermission = PlayerSettings.Android.forceInternetPermission;
            var originalTargetArchitectures = PlayerSettings.Android.targetArchitectures;
            var originalScriptingBackend = PlayerSettings.GetScriptingBackend(
                BuildTargetGroup.Android);
            var originalIl2CppCompilerConfiguration =
                PlayerSettings.GetIl2CppCompilerConfiguration(BuildTargetGroup.Android);
            var originalBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
            try
            {
                File.WriteAllText(runtimeConfigPath,
                    JsonUtility.ToJson(runtimeConfiguration, prettyPrint: true),
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                AssetDatabase.ImportAsset(MainnetRuntimeConfigAsset,
                    ImportAssetOptions.ForceSynchronousImport);

                PlayerSettings.bundleVersion = ExpectedBundleVersion;
                PlayerSettings.Android.bundleVersionCode = ExpectedAndroidVersionCode;
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, applicationId);
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystorePath;
                PlayerSettings.Android.keystorePass = keystorePassword;
                PlayerSettings.Android.keyaliasName = keyAlias;
                PlayerSettings.Android.keyaliasPass = keyPassword;
                PlayerSettings.Android.forceInternetPermission = true;
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.SetScriptingBackend(
                    BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                // Unity's Release (-Os) Android clang has a reproducible instruction-
                // selection crash in this project's generated Generics translation unit.
                // Master is Unity's shipping configuration and uses a different optimizer
                // path while preserving a non-development, non-debuggable APK.
                PlayerSettings.SetIl2CppCompilerConfiguration(
                    BuildTargetGroup.Android, Il2CppCompilerConfiguration.Master);
                EditorUserBuildSettings.buildAppBundle = false;

                var scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray();
                if (scenes.Length == 0)
                    throw new InvalidOperationException(
                        "No enabled scenes are configured in Editor Build Settings.");

                var outputDirectory = Path.GetFullPath(
                    Path.Combine(Application.dataPath, "../../../outputs"));
                Directory.CreateDirectory(outputDirectory);
                var outputPath = Path.Combine(outputDirectory,
                    "SeekerAutoChess-v" + ExpectedBundleVersion + "-mainnet-" +
                    runtimeConfiguration.accessMode + "-candidate.apk");
                if (File.Exists(outputPath))
                    throw new InvalidOperationException(
                        "The mainnet access candidate output already exists; archive it before producing another candidate.");
                var buildOptions = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.CleanBuildCache
                };
                var report = BuildPipeline.BuildPlayer(buildOptions);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    // Unity 2022's bundled Android clang can occasionally crash while
                    // compiling one generated IL2CPP translation unit. The clean pass
                    // has already populated the cache, so one incremental retry normally
                    // compiles only the failed object and avoids another password prompt.
                    Debug.LogWarning(
                        "Android mainnet candidate clean build failed; retrying once with the populated IL2CPP cache.");
                    buildOptions.options = BuildOptions.None;
                    report = BuildPipeline.BuildPlayer(buildOptions);
                }
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException(
                        "Android mainnet access candidate build failed: " +
                        report.summary.result);
                Debug.Log("Android mainnet access candidate APK built: " + outputPath);
            }
            finally
            {
                PlayerSettings.bundleVersion = originalBundleVersion;
                PlayerSettings.Android.bundleVersionCode = originalVersionCode;
                PlayerSettings.SetApplicationIdentifier(
                    BuildTargetGroup.Android, originalApplicationId);
                PlayerSettings.Android.useCustomKeystore = originalUseCustomKeystore;
                PlayerSettings.Android.keystoreName = originalKeystoreName;
                PlayerSettings.Android.keystorePass = originalKeystorePass;
                PlayerSettings.Android.keyaliasName = originalKeyaliasName;
                PlayerSettings.Android.keyaliasPass = originalKeyaliasPass;
                PlayerSettings.Android.forceInternetPermission = originalForceInternetPermission;
                PlayerSettings.Android.targetArchitectures = originalTargetArchitectures;
                PlayerSettings.SetScriptingBackend(
                    BuildTargetGroup.Android, originalScriptingBackend);
                PlayerSettings.SetIl2CppCompilerConfiguration(
                    BuildTargetGroup.Android, originalIl2CppCompilerConfiguration);
                EditorUserBuildSettings.buildAppBundle = originalBuildAppBundle;
                AssetDatabase.DeleteAsset(MainnetRuntimeConfigAsset);
                if (File.Exists(runtimeConfigPath)) File.Delete(runtimeConfigPath);
                if (File.Exists(runtimeConfigPath + ".meta"))
                    File.Delete(runtimeConfigPath + ".meta");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static string RequireEnvironment(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(name + " is required.");
            return value.Trim();
        }

        private static string ReadMainnetAccessMode()
        {
            var value = Environment.GetEnvironmentVariable(
                "LIGHTSEEKER_MAINNET_ACCESS_MODE");
            return string.IsNullOrWhiteSpace(value)
                ? "invite"
                : value.Trim().ToLowerInvariant();
        }

        private static void ValidateLoginCBackgroundImport()
        {
            AssetDatabase.ImportAsset(LoginCBackgroundAsset,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(LoginCBackgroundAsset);
            var importer = AssetImporter.GetAtPath(LoginCBackgroundAsset) as TextureImporter;
            if (texture == null || texture.width != 1080 || texture.height != 2400 ||
                importer == null || importer.npotScale != TextureImporterNPOTScale.None ||
                importer.maxTextureSize != 4096 ||
                importer.textureCompression != TextureImporterCompression.Uncompressed)
                throw new InvalidOperationException(
                    "Login C background must import byte-faithfully at 1080x2400 without compression before building.");
        }

        private static bool IsInsideDirectory(string path, string directory)
        {
            var normalizedPath = Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedDirectory = Path.GetFullPath(directory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            return normalizedPath.StartsWith(normalizedDirectory,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
