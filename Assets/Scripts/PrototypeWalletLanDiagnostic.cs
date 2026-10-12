using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SeekerAutoChess.Core;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    public sealed partial class PrototypeBootstrap
    {
        private void TryAttachWalletLanDiagnostic()
        {
#if DEVELOPMENT_BUILD && UNITY_ANDROID
            WalletLanDiagnosticOverlay.TryAttach(this);
#endif
        }
    }

#if DEVELOPMENT_BUILD && UNITY_ANDROID
    /// <summary>
    /// Opt-in Seeker device diagnostic. It is compiled only into Development Builds and
    /// is activated only through explicit Android intent extras supplied by the test tool.
    /// It is not a formal game page and never displays wallet or server tokens.
    /// </summary>
    internal sealed class WalletLanDiagnosticOverlay : MonoBehaviour
    {
        private const string EnabledExtra = "lightseeker.wallet.diagnostic";
        private const string ServerUrlExtra = "lightseeker.wallet.serverUrl";
        private const string IdentityUriExtra = "lightseeker.wallet.identityUri";
        private const string IconUriExtra = "lightseeker.wallet.iconUri";
        private const string ApplicationNameExtra = "lightseeker.wallet.applicationName";
        private const string ChainIdExtra = "lightseeker.wallet.chainId";
        private const string InvitationCodeExtra = "lightseeker.wallet.invitationCode";
        private const string ExpeditionSeasonIdExtra =
            "lightseeker.wallet.expeditionSeasonId";

        private PrototypeBootstrap bootstrap;
        private CancellationTokenSource operationLifetime;
        private string diagnosticMessage = "Ready. Tap CONNECT & SIGN to open a wallet.";
        private bool operationInProgress;
        private bool hidden;
        private string invitationCode;
        private string expeditionSeasonId;
        private ExpeditionServerSelection selection;

        public static void TryAttach(PrototypeBootstrap bootstrap)
        {
            if (bootstrap == null || !Debug.isDebugBuild) return;

            string serverUrl;
            string identityUri;
            string iconUri;
            string applicationName;
            string chainId;
            string invitationCode;
            string expeditionSeasonId;
            if (!TryReadConfiguration(
                    out serverUrl, out identityUri, out iconUri, out applicationName,
                    out chainId, out invitationCode, out expeditionSeasonId))
                return;

            bootstrap.ConfigureWalletServerRuntime(
                serverUrl,
                allowInsecureLocalDevelopment: true,
                identityUri,
                iconUri,
                applicationName,
                chainId);

            var overlay = bootstrap.gameObject.AddComponent<WalletLanDiagnosticOverlay>();
            overlay.bootstrap = bootstrap;
            overlay.operationLifetime = new CancellationTokenSource();
            overlay.invitationCode = invitationCode;
            overlay.expeditionSeasonId = expeditionSeasonId;
            overlay.bootstrap.WalletServerSessionStateChanged += overlay.OnSessionStateChanged;
            overlay.RunRestoreAsync();
        }

        private static bool TryReadConfiguration(
            out string serverUrl,
            out string identityUri,
            out string iconUri,
            out string applicationName,
            out string chainId,
            out string invitationCode,
            out string expeditionSeasonId)
        {
            serverUrl = null;
            identityUri = null;
            iconUri = null;
            applicationName = null;
            chainId = WalletAuthenticationCoordinator.DevnetChainId;
            invitationCode = null;
            expeditionSeasonId = null;

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = activity == null
                       ? null
                       : activity.Call<AndroidJavaObject>("getIntent"))
            {
                if (intent == null || !intent.Call<bool>("getBooleanExtra", EnabledExtra, false))
                    return false;

                serverUrl = intent.Call<string>("getStringExtra", ServerUrlExtra);
                identityUri = intent.Call<string>("getStringExtra", IdentityUriExtra);
                iconUri = intent.Call<string>("getStringExtra", IconUriExtra);
                applicationName = intent.Call<string>("getStringExtra", ApplicationNameExtra);
                var configuredChainId = intent.Call<string>("getStringExtra", ChainIdExtra);
                if (!string.IsNullOrWhiteSpace(configuredChainId)) chainId = configuredChainId;
                invitationCode = intent.Call<string>("getStringExtra", InvitationCodeExtra);
                expeditionSeasonId = intent.Call<string>(
                    "getStringExtra", ExpeditionSeasonIdExtra);
            }

            if (string.IsNullOrWhiteSpace(serverUrl) ||
                string.IsNullOrWhiteSpace(identityUri) ||
                string.IsNullOrWhiteSpace(iconUri) ||
                string.IsNullOrWhiteSpace(applicationName) ||
                string.IsNullOrWhiteSpace(expeditionSeasonId))
            {
                Debug.LogError("Wallet LAN diagnostic intent is missing required configuration.");
                return false;
            }

            return true;
        }

        private void OnGUI()
        {
            if (hidden || bootstrap == null) return;

            GUI.depth = -10000;
            var width = Mathf.Min(Screen.width - 32f, 760f);
            var panel = new Rect(16f, 16f, width, 438f);
            var previousMatrix = GUI.matrix;
            var previousColor = GUI.color;
            var previousBackgroundColor = GUI.backgroundColor;
            var previousEnabled = GUI.enabled;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = new Color(0.025f, 0.08f, 0.10f, 0.96f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 25,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.84f, 0.45f) }
            };
            var textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 19
            };

            GUI.Label(new Rect(32f, 28f, width - 64f, 36f),
                "SEEKER WALLET LAN DIAGNOSTIC", titleStyle);
            GUI.Label(new Rect(32f, 68f, width - 64f, 94f),
                "State: " + bootstrap.WalletServerState + "\n" + diagnosticMessage,
                textStyle);

            GUI.enabled = !operationInProgress &&
                          bootstrap.WalletServerState != WalletServerSessionState.Authenticated;
            if (GUI.Button(new Rect(32f, 170f, 270f, 58f), "CONNECT & SIGN", buttonStyle))
                RunSignInAsync();
            GUI.enabled = !operationInProgress;
            if (GUI.Button(new Rect(318f, 170f, 190f, 58f), "SIGN OUT", buttonStyle))
                RunSignOutAsync();
            GUI.enabled = true;
            if (GUI.Button(new Rect(width - 174f, 170f, 142f, 58f), "HIDE", buttonStyle))
                hidden = true;

            GUI.Label(new Rect(32f, 238f, width - 64f, 30f),
                "Internal devnet entry diagnostic · season " + expeditionSeasonId,
                textStyle);
            GUI.enabled = CanUseExpeditionControls();
            if (GUI.Button(new Rect(32f, 278f, 154f, 58f), "LOAD", buttonStyle))
                RunLoadSelectionAsync();
            if (GUI.Button(new Rect(198f, 278f, 154f, 58f), "RECOVER", buttonStyle))
                RunRecoverAsync();
            GUI.enabled = CanStartDifficulty("D0");
            if (GUI.Button(new Rect(364f, 278f, 112f, 58f), "D0", buttonStyle))
                RunDifficultyAsync("D0");
            GUI.enabled = CanStartDifficulty("D1");
            if (GUI.Button(new Rect(32f, 350f, 154f, 58f), "D1 PAY", buttonStyle))
                RunDifficultyAsync("D1");
            GUI.enabled = CanStartDifficulty("D2");
            if (GUI.Button(new Rect(198f, 350f, 154f, 58f), "D2 PAY", buttonStyle))
                RunDifficultyAsync("D2");
            GUI.enabled = CanStartDifficulty("D3");
            if (GUI.Button(new Rect(364f, 350f, 154f, 58f), "D3 PAY", buttonStyle))
                RunDifficultyAsync("D3");

            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            GUI.backgroundColor = previousBackgroundColor;
            GUI.enabled = previousEnabled;
        }

        private bool CanUseExpeditionControls()
        {
            return !operationInProgress &&
                   bootstrap.WalletServerState == WalletServerSessionState.Authenticated;
        }

        private bool CanStartDifficulty(string difficultyId)
        {
            if (!CanUseExpeditionControls() || selection == null ||
                selection.Difficulties == null ||
                !string.IsNullOrWhiteSpace(selection.ActiveRunId)) return false;
            for (var index = 0; index < selection.Difficulties.Length; index++)
            {
                var difficulty = selection.Difficulties[index];
                if (difficulty != null && difficulty.DifficultyId == difficultyId)
                    return difficulty.IsUnlocked && difficulty.CanCreateIntent;
            }
            return false;
        }

        private async void RunLoadSelectionAsync()
        {
            if (!CanUseExpeditionControls()) return;
            operationInProgress = true;
            diagnosticMessage = "Loading authoritative expedition selection...";
            try
            {
                selection = await bootstrap.LoadChapterOneExpeditionSelectionAsync(
                    expeditionSeasonId, operationLifetime.Token);
                diagnosticMessage = DescribeSelection(selection);
            }
            catch (Exception exception)
            {
                HandleOperationFailure("LOAD", exception);
            }
            finally
            {
                operationInProgress = false;
            }
        }

        private async void RunRecoverAsync()
        {
            if (!CanUseExpeditionControls()) return;
            operationInProgress = true;
            diagnosticMessage = "Recovering current authoritative run...";
            try
            {
                var run = await bootstrap.RecoverChapterOneExpeditionRunAsync(
                    expeditionSeasonId, operationLifetime.Token);
                diagnosticMessage = run == null
                    ? "No active run was returned."
                    : "Recovered " + run.DifficultyId + " run " + ShortIdentifier(run.RunId) +
                      ", quota slot " + run.QuotaSlot + ".";
            }
            catch (Exception exception)
            {
                HandleOperationFailure("RECOVER", exception);
            }
            finally
            {
                operationInProgress = false;
            }
        }

        private async void RunDifficultyAsync(string difficultyId)
        {
            if (!CanStartDifficulty(difficultyId)) return;
            operationInProgress = true;
            try
            {
                await Task.Yield();
                diagnosticMessage =
                    "Formal entry is fail-closed here: the wallet diagnostic has no real " +
                    "deck, equipment and lantern snapshots. Start from the gameplay entry " +
                    "flow after it supplies a normalized combat input.";
            }
            catch (Exception exception)
            {
                HandleOperationFailure(difficultyId, exception);
            }
            finally
            {
                operationInProgress = false;
            }
        }

        private void HandleOperationFailure(string operation, Exception exception)
        {
            if (exception is OperationCanceledException)
                diagnosticMessage = operation + " cancelled.";
            else
            {
                diagnosticMessage = operation + " FAILED: " + exception.Message;
                Debug.LogException(exception);
            }
        }

        private static string DescribeSelection(ExpeditionServerSelection value)
        {
            if (value == null) return "Server returned no expedition selection.";
            var available = new StringBuilder();
            if (value.Difficulties != null)
            {
                for (var index = 0; index < value.Difficulties.Length; index++)
                {
                    var difficulty = value.Difficulties[index];
                    if (difficulty == null || !difficulty.IsUnlocked ||
                        !difficulty.CanCreateIntent) continue;
                    if (available.Length > 0) available.Append(',');
                    available.Append(difficulty.DifficultyId);
                }
            }
            return "Selection loaded: remaining " + value.RemainingSlots + "/" +
                   value.DailyLimit + ", available [" + available + "]" +
                   (string.IsNullOrWhiteSpace(value.ActiveRunId)
                       ? "."
                       : ", active run " + ShortIdentifier(value.ActiveRunId) + ".");
        }

        private static string DiagnosticHash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                var builder = new StringBuilder(bytes.Length * 2);
                for (var index = 0; index < bytes.Length; index++)
                    builder.Append(bytes[index].ToString("x2"));
                return builder.ToString();
            }
        }

        private async void RunRestoreAsync()
        {
            if (operationInProgress) return;
            operationInProgress = true;
            diagnosticMessage = "Restoring encrypted wallet session...";
            try
            {
                var session = await bootstrap.RestoreWalletServerSessionAsync(
                    operationLifetime.Token);
                diagnosticMessage = session == null
                    ? "No encrypted session found. Tap CONNECT & SIGN to open a wallet."
                    : "Restored account " + ShortAccount(session.WalletAddress) +
                      ". Refresh token rotated and server /v1/me verified.";
            }
            catch (OperationCanceledException)
            {
                diagnosticMessage = "Session restore cancelled.";
            }
            catch (Exception exception)
            {
                diagnosticMessage = "RESTORE FAILED: " + exception.Message;
                Debug.LogException(exception);
            }
            finally
            {
                operationInProgress = false;
            }
        }

        private async void RunSignInAsync()
        {
            if (operationInProgress) return;
            operationInProgress = true;
            diagnosticMessage = "Opening MWA wallet authorization...";
            try
            {
                WalletAuthenticationSession session = await bootstrap.SignInWalletServerAsync(
                    invitationCode,
                    operationLifetime.Token);
                invitationCode = null;
                diagnosticMessage = "Authenticated account " + ShortAccount(session.WalletAddress) +
                                    ". Server /v1/me verified.";
            }
            catch (OperationCanceledException)
            {
                diagnosticMessage = "Wallet operation cancelled.";
            }
            catch (Exception exception)
            {
                diagnosticMessage = "FAILED: " + exception.Message;
                Debug.LogException(exception);
            }
            finally
            {
                operationInProgress = false;
            }
        }

        private async void RunSignOutAsync()
        {
            if (operationInProgress) return;
            operationInProgress = true;
            diagnosticMessage = "Signing out...";
            try
            {
                var serverSessionRevoked = await bootstrap.SignOutWalletServerAsync(
                    operationLifetime.Token);
                diagnosticMessage = serverSessionRevoked
                    ? "Signed out; server session revoked, secure storage cleared, and wallet disconnected."
                    : "No server session was available; local wallet connection cleared only.";
            }
            catch (OperationCanceledException)
            {
                diagnosticMessage = "Sign-out cancelled.";
            }
            catch (Exception exception)
            {
                diagnosticMessage = "SIGN-OUT FAILED: " + exception.Message;
                Debug.LogException(exception);
            }
            finally
            {
                operationInProgress = false;
            }
        }

        private void OnSessionStateChanged(WalletServerSessionState state, string error)
        {
            if (!string.IsNullOrWhiteSpace(error)) diagnosticMessage = error;
        }

        private void OnDestroy()
        {
            if (bootstrap != null)
                bootstrap.WalletServerSessionStateChanged -= OnSessionStateChanged;
            if (operationLifetime == null) return;
            operationLifetime.Cancel();
            operationLifetime.Dispose();
            operationLifetime = null;
        }

        private static string ShortAccount(string address)
        {
            if (string.IsNullOrWhiteSpace(address) || address.Length <= 12) return address ?? string.Empty;
            return address.Substring(0, 6) + "..." + address.Substring(address.Length - 6);
        }

        private static string ShortIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= 16)
                return value ?? string.Empty;
            return value.Substring(0, 8) + "..." + value.Substring(value.Length - 6);
        }
    }
#endif
}
