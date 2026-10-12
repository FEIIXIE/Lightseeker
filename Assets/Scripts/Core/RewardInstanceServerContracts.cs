using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SeekerAutoChess.Core
{
    public sealed class AuthoritativeEquipmentAffix
    {
        public string AffixId { get; set; }
        public string Unit { get; set; }
        public int Value { get; set; }
        public int AllocatedBudgetBasisPoints { get; set; }
        public int Ordinal { get; set; }
    }

    public sealed class AuthoritativeEquipmentInstance
    {
        public string InstanceId { get; set; }
        public string TemplateId { get; set; }
        public string SlotId { get; set; }
        public string FamilyId { get; set; }
        public string RarityId { get; set; }
        public int PrimaryRoll { get; set; }
        public string PrimaryStatId { get; set; }
        public int PrimaryValue { get; set; }
        public string FixedEffectStatId { get; set; }
        public int? FixedEffectValue { get; set; }
        public int AffixBudgetBasisPoints { get; set; }
        public AuthoritativeEquipmentAffix[] Affixes { get; set; }
        public string EquipmentPoolVersion { get; set; }
        public string RarityRulesVersion { get; set; }
        public string RollRulesVersion { get; set; }
        public string AffixRulesVersion { get; set; }
        public string SeasonId { get; set; }
        public string AcquiredAt { get; set; }
        public int StateVersion { get; set; }
        public string StateHash { get; set; }
    }

    public sealed class AuthoritativeLanternGlyph
    {
        public string GlyphId { get; set; }
        public int Value { get; set; }
        public int Ordinal { get; set; }
    }

    public sealed class AuthoritativeLanternInstance
    {
        public string InstanceId { get; set; }
        public string TemplateId { get; set; }
        public string AppearanceId { get; set; }
        public string RarityId { get; set; }
        public string BaseFlameStatId { get; set; }
        public int BaseFlameValue { get; set; }
        public AuthoritativeLanternGlyph[] Glyphs { get; set; }
        public string CoreEffectId { get; set; }
        public string RulesVersion { get; set; }
        public string CoreRulesVersion { get; set; }
        public string SeasonId { get; set; }
        public string ChapterId { get; set; }
        public string DifficultyId { get; set; }
        public bool IsFirstClear { get; set; }
        public int ContractStage { get; set; }
        public string[] CovenantOptionIds { get; set; }
        public string AcquiredAt { get; set; }
        public int StateVersion { get; set; }
        public string StateHash { get; set; }
    }

    public sealed class AuthoritativeRewardInventorySnapshot
    {
        public string AccountId { get; set; }
        public AuthoritativeEquipmentInstance[] Equipment { get; set; }
        public AuthoritativeLanternInstance[] Lanterns { get; set; }
        public int PendingQueueItems { get; set; }
        public int FailedQueueItems { get; set; }
        public string RequestId { get; set; }
    }

    public sealed class AuthoritativeRewardRecoveryResult
    {
        public int ProcessedQueueItems { get; set; }
        public int GeneratedEquipmentInstances { get; set; }
        public int GeneratedLanternInstances { get; set; }
        public int FailedQueueItems { get; set; }
        public int PendingQueueItems { get; set; }
        public AuthoritativeRewardInventorySnapshot Inventory { get; set; }
        public string RequestId { get; set; }
    }

    public interface IRewardInstanceServerTransport
    {
        Task<AuthoritativeRewardInventorySnapshot> GetInventoryAsync(
            string accessToken, CancellationToken cancellationToken);

        Task<AuthoritativeRewardRecoveryResult> RecoverAsync(
            string accessToken, CancellationToken cancellationToken);
    }

    public sealed class AuthoritativeRewardInventoryMirror
    {
        private AuthoritativeRewardInventorySnapshot current;

        public AuthoritativeRewardInventorySnapshot Current { get { return current; } }

        public void Replace(AuthoritativeRewardInventorySnapshot snapshot)
        {
            Validate(snapshot);
            if (current != null && !string.Equals(current.AccountId, snapshot.AccountId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "A reward inventory mirror cannot change accounts without being cleared.");
            current = snapshot;
        }

        public void Clear()
        {
            current = null;
        }

        private static void Validate(AuthoritativeRewardInventorySnapshot snapshot)
        {
            if (snapshot == null || !Guid.TryParse(snapshot.AccountId, out _) ||
                snapshot.PendingQueueItems < 0 || snapshot.FailedQueueItems < 0 ||
                string.IsNullOrWhiteSpace(snapshot.RequestId))
                throw new InvalidOperationException("The authoritative reward inventory is invalid.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in snapshot.Equipment ?? new AuthoritativeEquipmentInstance[0])
            {
                if (item == null || !Guid.TryParse(item.InstanceId, out _) ||
                    string.IsNullOrWhiteSpace(item.TemplateId) ||
                    string.IsNullOrWhiteSpace(item.SlotId) ||
                    string.IsNullOrWhiteSpace(item.FamilyId) ||
                    string.IsNullOrWhiteSpace(item.RarityId) ||
                    string.IsNullOrWhiteSpace(item.PrimaryStatId) ||
                    string.IsNullOrWhiteSpace(item.SeasonId) ||
                    !IsCanonicalUtc(item.AcquiredAt) || item.StateVersion < 1 ||
                    !IsSha256(item.StateHash) || !ids.Add(item.InstanceId))
                    throw new InvalidOperationException(
                        "The authoritative equipment inventory contains an invalid instance.");
            }

            foreach (var item in snapshot.Lanterns ?? new AuthoritativeLanternInstance[0])
            {
                if (item == null || !Guid.TryParse(item.InstanceId, out _) ||
                    string.IsNullOrWhiteSpace(item.TemplateId) ||
                    !HomeLanternModularArtContract.IsValidAppearanceId(item.AppearanceId) ||
                    string.IsNullOrWhiteSpace(item.RarityId) ||
                    string.IsNullOrWhiteSpace(item.BaseFlameStatId) ||
                    string.IsNullOrWhiteSpace(item.CoreEffectId) ||
                    string.IsNullOrWhiteSpace(item.RulesVersion) ||
                    string.IsNullOrWhiteSpace(item.CoreRulesVersion) ||
                    string.IsNullOrWhiteSpace(item.SeasonId) ||
                    string.IsNullOrWhiteSpace(item.ChapterId) ||
                    string.IsNullOrWhiteSpace(item.DifficultyId) ||
                    item.ContractStage < 0 || item.ContractStage > 6 ||
                    (item.CovenantOptionIds ?? new string[0]).Length != item.ContractStage ||
                    !IsCanonicalUtc(item.AcquiredAt) ||
                    item.StateVersion < 1 || !IsSha256(item.StateHash) ||
                    !ids.Add(item.InstanceId))
                    throw new InvalidOperationException(
                        "The authoritative lantern inventory contains an invalid instance.");
            }
        }

        private static bool IsSha256(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64) return false;
            foreach (var character in value)
                if (!(character >= '0' && character <= '9') &&
                    !(character >= 'A' && character <= 'F')) return false;
            return true;
        }

        private static bool IsCanonicalUtc(string value)
        {
            DateTimeOffset parsed;
            return DateTimeOffset.TryParse(value,
                       System.Globalization.CultureInfo.InvariantCulture,
                       System.Globalization.DateTimeStyles.RoundtripKind, out parsed) &&
                   parsed.Offset == TimeSpan.Zero;
        }
    }

    public sealed class RewardInstanceServerCoordinator
    {
        private readonly IRewardInstanceServerTransport transport;
        private readonly AuthoritativeRewardInventoryMirror mirror;

        public RewardInstanceServerCoordinator(IRewardInstanceServerTransport transport,
            AuthoritativeRewardInventoryMirror mirror)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.mirror = mirror ?? throw new ArgumentNullException(nameof(mirror));
        }

        public async Task<AuthoritativeRewardInventorySnapshot> SynchronizeAsync(
            string accessToken, bool recover, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("An authenticated access token is required.",
                    nameof(accessToken));
            AuthoritativeRewardInventorySnapshot snapshot;
            if (recover)
            {
                var result = await transport.RecoverAsync(accessToken, cancellationToken);
                if (result == null || result.Inventory == null ||
                    result.ProcessedQueueItems < 0 || result.GeneratedEquipmentInstances < 0 ||
                    result.GeneratedLanternInstances < 0 || result.PendingQueueItems < 0 ||
                    result.FailedQueueItems < 0)
                    throw new InvalidOperationException(
                        "The reward instance recovery response is invalid.");
                snapshot = result.Inventory;
            }
            else
            {
                snapshot = await transport.GetInventoryAsync(accessToken, cancellationToken);
            }

            mirror.Replace(snapshot);
            return snapshot;
        }
    }
}
