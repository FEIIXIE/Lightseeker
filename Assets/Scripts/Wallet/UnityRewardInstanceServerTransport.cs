using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SeekerAutoChess.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace SeekerAutoChess.Wallet
{
    public sealed class UnityRewardInstanceServerTransport : IRewardInstanceServerTransport
    {
        private const int MaximumResponseBytes = 1024 * 1024;
        private readonly Uri baseUri;
        private readonly int timeoutSeconds;

        public UnityRewardInstanceServerTransport(string baseUrl,
            bool allowInsecureLocalDevelopment, int timeoutSeconds = 15)
        {
            Uri parsed;
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out parsed))
                throw new ArgumentException("The reward server base URL must be absolute.",
                    nameof(baseUrl));
            if (!IsAllowedEndpoint(parsed, allowInsecureLocalDevelopment))
                throw new ArgumentException(
                    "The reward server must use HTTPS. HTTP is only allowed explicitly for a local development host.",
                    nameof(baseUrl));
            if (timeoutSeconds < 1 || timeoutSeconds > 60)
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            baseUri = new Uri(parsed.AbsoluteUri.TrimEnd('/') + "/", UriKind.Absolute);
            this.timeoutSeconds = timeoutSeconds;
        }

        public async Task<AuthoritativeRewardInventorySnapshot> GetInventoryAsync(
            string accessToken, CancellationToken cancellationToken)
        {
            return MapInventory(await SendAsync<InventoryResponse>(
                UnityWebRequest.kHttpVerbGET, "v1/inventory/reward-instances",
                accessToken, cancellationToken));
        }

        public async Task<AuthoritativeRewardRecoveryResult> RecoverAsync(
            string accessToken, CancellationToken cancellationToken)
        {
            var response = await SendAsync<RecoveryResponse>(UnityWebRequest.kHttpVerbPOST,
                "v1/expeditions/chapter01/reward-instances/recover",
                accessToken, cancellationToken);
            return new AuthoritativeRewardRecoveryResult
            {
                ProcessedQueueItems = response.processedQueueItems,
                GeneratedEquipmentInstances = response.generatedEquipmentInstances,
                GeneratedLanternInstances = response.generatedLanternInstances,
                FailedQueueItems = response.failedQueueItems,
                PendingQueueItems = response.pendingQueueItems,
                Inventory = MapInventory(response.inventory),
                RequestId = response.requestId
            };
        }

        private async Task<T> SendAsync<T>(string method, string relativePath,
            string accessToken, CancellationToken cancellationToken) where T : class
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("An authenticated access token is required.",
                    nameof(accessToken));
            cancellationToken.ThrowIfCancellationRequested();
            var request = new UnityWebRequest(new Uri(baseUri, relativePath), method);
            try
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + accessToken);
                if (string.Equals(method, UnityWebRequest.kHttpVerbPOST,
                        StringComparison.Ordinal))
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
                    request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                }
                var operation = request.SendWebRequest();
                var completion = new TaskCompletionSource<bool>();
                operation.completed += ignored => completion.TrySetResult(true);
                if (operation.isDone) completion.TrySetResult(true);
                using (cancellationToken.Register(request.Abort)) await completion.Task;
                cancellationToken.ThrowIfCancellationRequested();
                var data = request.downloadHandler == null ? null : request.downloadHandler.data;
                if (data != null && data.Length > MaximumResponseBytes)
                    throw new WalletServerException(request.responseCode,
                        "CLIENT_RESPONSE_TOO_LARGE",
                        "The reward inventory response exceeded the client limit.", string.Empty);
                var text = request.downloadHandler == null
                    ? string.Empty : request.downloadHandler.text;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    var exception = CreateServerException(
                        request.responseCode, text, request.error);
                    Debug.LogWarning(
                        "Reward server request failed: path=" + relativePath +
                        " status=" + exception.StatusCode +
                        " code=" + exception.Code +
                        " requestId=" + (string.IsNullOrWhiteSpace(exception.RequestId)
                            ? "none"
                            : exception.RequestId));
                    throw exception;
                }
                var response = JsonUtility.FromJson<T>(text);
                if (response == null)
                    throw new WalletServerException(0, "CLIENT_RESPONSE_INVALID",
                        "The reward server returned invalid JSON.", string.Empty);
                return response;
            }
            finally
            {
                request.Dispose();
            }
        }

        private static AuthoritativeRewardInventorySnapshot MapInventory(
            InventoryResponse response)
        {
            if (response == null)
                throw new WalletServerException(0, "CLIENT_RESPONSE_INVALID",
                    "The reward inventory response is missing.", string.Empty);
            var sourceEquipment = response.equipment ?? new EquipmentResponse[0];
            var equipment = new AuthoritativeEquipmentInstance[sourceEquipment.Length];
            for (var index = 0; index < sourceEquipment.Length; index++)
            {
                var item = sourceEquipment[index] ?? new EquipmentResponse();
                var sourceAffixes = item.affixes ?? new EquipmentAffixResponse[0];
                var affixes = new AuthoritativeEquipmentAffix[sourceAffixes.Length];
                for (var affixIndex = 0; affixIndex < sourceAffixes.Length; affixIndex++)
                {
                    var affix = sourceAffixes[affixIndex] ?? new EquipmentAffixResponse();
                    affixes[affixIndex] = new AuthoritativeEquipmentAffix
                    {
                        AffixId = affix.affixId, Unit = affix.unit, Value = affix.value,
                        AllocatedBudgetBasisPoints = affix.allocatedBudgetBasisPoints,
                        Ordinal = affix.ordinal
                    };
                }
                equipment[index] = new AuthoritativeEquipmentInstance
                {
                    InstanceId = item.instanceId, TemplateId = item.templateId,
                    SlotId = item.slotId, FamilyId = item.familyId,
                    RarityId = item.rarityId, PrimaryRoll = item.primaryRoll,
                    PrimaryStatId = item.primaryStatId, PrimaryValue = item.primaryValue,
                    FixedEffectStatId = item.fixedEffectStatId,
                    FixedEffectValue = string.IsNullOrWhiteSpace(item.fixedEffectStatId)
                        ? (int?)null : item.fixedEffectValue,
                    AffixBudgetBasisPoints = item.affixBudgetBasisPoints,
                    Affixes = affixes, EquipmentPoolVersion = item.equipmentPoolVersion,
                    RarityRulesVersion = item.rarityRulesVersion,
                    RollRulesVersion = item.rollRulesVersion,
                    AffixRulesVersion = item.affixRulesVersion, SeasonId = item.seasonId,
                    AcquiredAt = item.acquiredAt,
                    StateVersion = item.stateVersion, StateHash = item.stateHash
                };
            }

            var sourceLanterns = response.lanterns ?? new LanternResponse[0];
            var lanterns = new AuthoritativeLanternInstance[sourceLanterns.Length];
            for (var index = 0; index < sourceLanterns.Length; index++)
            {
                var item = sourceLanterns[index] ?? new LanternResponse();
                var sourceGlyphs = item.glyphs ?? new LanternGlyphResponse[0];
                var glyphs = new AuthoritativeLanternGlyph[sourceGlyphs.Length];
                for (var glyphIndex = 0; glyphIndex < sourceGlyphs.Length; glyphIndex++)
                {
                    var glyph = sourceGlyphs[glyphIndex] ?? new LanternGlyphResponse();
                    glyphs[glyphIndex] = new AuthoritativeLanternGlyph
                    {
                        GlyphId = glyph.glyphId, Value = glyph.value, Ordinal = glyph.ordinal
                    };
                }
                lanterns[index] = new AuthoritativeLanternInstance
                {
                    InstanceId = item.instanceId, TemplateId = item.templateId,
                    AppearanceId = item.appearanceId, RarityId = item.rarityId,
                    BaseFlameStatId = item.baseFlameStatId,
                    BaseFlameValue = item.baseFlameValue, Glyphs = glyphs,
                    CoreEffectId = item.coreEffectId, RulesVersion = item.rulesVersion,
                    CoreRulesVersion = item.coreRulesVersion, SeasonId = item.seasonId,
                    ChapterId = item.chapterId, DifficultyId = item.difficultyId,
                    IsFirstClear = item.isFirstClear, ContractStage = item.contractStage,
                    CovenantOptionIds = item.covenantOptionIds ?? new string[0],
                    AcquiredAt = item.acquiredAt,
                    StateVersion = item.stateVersion, StateHash = item.stateHash
                };
            }

            return new AuthoritativeRewardInventorySnapshot
            {
                AccountId = response.accountId, Equipment = equipment, Lanterns = lanterns,
                PendingQueueItems = response.pendingQueueItems,
                FailedQueueItems = response.failedQueueItems, RequestId = response.requestId
            };
        }

        private static WalletServerException CreateServerException(long statusCode,
            string responseText, string fallbackMessage)
        {
            ErrorResponse error = null;
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                try { error = JsonUtility.FromJson<ErrorResponse>(responseText); }
                catch (ArgumentException) { }
            }
            return new WalletServerException(statusCode,
                error == null ? "TRANSPORT_FAILED" : error.code,
                error != null && !string.IsNullOrWhiteSpace(error.message) ? error.message :
                    (string.IsNullOrWhiteSpace(fallbackMessage)
                        ? "The reward server request failed." : fallbackMessage),
                error == null ? string.Empty : error.requestId);
        }

        private static bool IsAllowedEndpoint(Uri endpoint,
            bool allowInsecureLocalDevelopment)
        {
            if (string.Equals(endpoint.Scheme, Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase)) return true;
            if (!allowInsecureLocalDevelopment ||
                !string.Equals(endpoint.Scheme, Uri.UriSchemeHttp,
                    StringComparison.OrdinalIgnoreCase)) return false;
            if (string.Equals(endpoint.Host, "localhost",
                    StringComparison.OrdinalIgnoreCase)) return true;
            IPAddress address;
            if (!IPAddress.TryParse(endpoint.Host, out address)) return false;
            if (IPAddress.IsLoopback(address)) return true;
            var bytes = address.GetAddressBytes();
            return bytes.Length == 4 && (bytes[0] == 10 ||
                (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                (bytes[0] == 192 && bytes[1] == 168));
        }

#pragma warning disable 0649
        [Serializable] private sealed class EquipmentAffixResponse { public string affixId; public string unit; public int value; public int allocatedBudgetBasisPoints; public int ordinal; }
        [Serializable] private sealed class EquipmentResponse { public string instanceId; public string templateId; public string slotId; public string familyId; public string rarityId; public int primaryRoll; public string primaryStatId; public int primaryValue; public string fixedEffectStatId; public int fixedEffectValue; public int affixBudgetBasisPoints; public EquipmentAffixResponse[] affixes; public string equipmentPoolVersion; public string rarityRulesVersion; public string rollRulesVersion; public string affixRulesVersion; public string seasonId; public string acquiredAt; public int stateVersion; public string stateHash; }
        [Serializable] private sealed class LanternGlyphResponse { public string glyphId; public int value; public int ordinal; }
        [Serializable] private sealed class LanternResponse { public string instanceId; public string templateId; public string appearanceId; public string rarityId; public string baseFlameStatId; public int baseFlameValue; public LanternGlyphResponse[] glyphs; public string coreEffectId; public string rulesVersion; public string coreRulesVersion; public string seasonId; public string chapterId; public string difficultyId; public bool isFirstClear; public int contractStage; public string[] covenantOptionIds; public string acquiredAt; public int stateVersion; public string stateHash; }
        [Serializable] private sealed class InventoryResponse { public string accountId; public EquipmentResponse[] equipment; public LanternResponse[] lanterns; public int pendingQueueItems; public int failedQueueItems; public string requestId; }
        [Serializable] private sealed class RecoveryResponse { public int processedQueueItems; public int generatedEquipmentInstances; public int generatedLanternInstances; public int failedQueueItems; public int pendingQueueItems; public InventoryResponse inventory; public string requestId; }
        [Serializable] private sealed class ErrorResponse { public string code; public string message; public string requestId; }
#pragma warning restore 0649
    }
}
