using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SeekerAutoChess.Core
{
    public sealed class ExpeditionLoadoutSnapshotHashes
    {
        public ExpeditionLoadoutSnapshotHashes(string deckSnapshotHash,
            string equipmentSnapshotHash, string lanternSnapshotHash,
            string firstClearEligibilityKeyHash,
            string deckSnapshotCanonical,
            string equipmentSnapshotCanonical,
            string lanternSnapshotCanonical,
            string firstClearEligibilityCanonical)
        {
            DeckSnapshotHash = RequireHash(deckSnapshotHash, nameof(deckSnapshotHash));
            EquipmentSnapshotHash = RequireHash(equipmentSnapshotHash, nameof(equipmentSnapshotHash));
            LanternSnapshotHash = RequireHash(lanternSnapshotHash, nameof(lanternSnapshotHash));
            FirstClearEligibilityKeyHash = RequireHash(firstClearEligibilityKeyHash,
                nameof(firstClearEligibilityKeyHash));
            DeckSnapshotCanonical = RequireCanonical(deckSnapshotCanonical,
                nameof(deckSnapshotCanonical));
            EquipmentSnapshotCanonical = RequireCanonical(equipmentSnapshotCanonical,
                nameof(equipmentSnapshotCanonical));
            LanternSnapshotCanonical = RequireCanonical(lanternSnapshotCanonical,
                nameof(lanternSnapshotCanonical));
            FirstClearEligibilityCanonical = RequireCanonical(firstClearEligibilityCanonical,
                nameof(firstClearEligibilityCanonical));
            CombatInputHash = FormalExpeditionSnapshotContract.ComputeCombatInputHash(
                DeckSnapshotHash, EquipmentSnapshotHash, LanternSnapshotHash,
                FirstClearEligibilityKeyHash);
        }

        public string DeckSnapshotHash { get; private set; }
        public string EquipmentSnapshotHash { get; private set; }
        public string LanternSnapshotHash { get; private set; }
        public string FirstClearEligibilityKeyHash { get; private set; }
        public string CombatInputContractVersion
        {
            get { return FormalExpeditionSnapshotContract.CombatInputContractVersion; }
        }
        public string CombatInputHash { get; private set; }
        public string DeckSnapshotCanonical { get; private set; }
        public string EquipmentSnapshotCanonical { get; private set; }
        public string LanternSnapshotCanonical { get; private set; }
        public string FirstClearEligibilityCanonical { get; private set; }

        private static string RequireHash(string value, string parameterName)
        {
            if (!FormalExpeditionSnapshotContract.IsSha256Hex(value))
                throw new ArgumentException("A lowercase SHA-256 hash is required.", parameterName);
            return value;
        }

        private static string RequireCanonical(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value) ||
                !value.StartsWith(FormalExpeditionSnapshotContract.SnapshotContractVersion,
                    StringComparison.Ordinal))
                throw new ArgumentException("A normalized expedition snapshot is required.",
                    parameterName);
            return value;
        }
    }

    public sealed class AuthoritativeExpeditionRunBinding
    {
        internal AuthoritativeExpeditionRunBinding(ExpeditionServerRunSnapshot source,
            uint mapSeed, ExpeditionRunRewardSnapshot rewards)
        {
            Source = source;
            MapSeed = mapSeed;
            Rewards = rewards;
        }

        public ExpeditionServerRunSnapshot Source { get; private set; }
        public uint MapSeed { get; private set; }
        public ExpeditionRunRewardSnapshot Rewards { get; private set; }
    }

    public sealed class RestoredFormalExpeditionSnapshot
    {
        internal RestoredFormalExpeditionSnapshot(
            ExpeditionLoadoutSnapshotHashes hashes,
            RunDeckSnapshot deck,
            IDictionary<string, string[]> equipmentInstanceIdsByDefinition,
            LanternSnapshot lantern)
        {
            Hashes = hashes ?? throw new ArgumentNullException(nameof(hashes));
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
            if (equipmentInstanceIdsByDefinition == null)
                throw new ArgumentNullException(nameof(equipmentInstanceIdsByDefinition));
            EquipmentInstanceIdsByDefinition =
                new Dictionary<string, string[]>(equipmentInstanceIdsByDefinition,
                    StringComparer.Ordinal);
            Lantern = lantern ?? throw new ArgumentNullException(nameof(lantern));
        }

        public ExpeditionLoadoutSnapshotHashes Hashes { get; private set; }
        public RunDeckSnapshot Deck { get; private set; }
        public IReadOnlyDictionary<string, string[]> EquipmentInstanceIdsByDefinition
        { get; private set; }
        public LanternSnapshot Lantern { get; private set; }
    }

    public static class FormalExpeditionSnapshotContract
    {
        public const string SnapshotContractVersion = "chapter01.loadout-snapshot.v1";
        public const string CombatInputContractVersion = "chapter01.combat-input.v1";
        public const string MapSeedDerivationVersion = "chapter01.map-seed.v1";

        public static ExpeditionLoadoutSnapshotHashes Capture(
            RunDeckSnapshot deck,
            IDictionary<string, EquipmentLoadoutSnapshot> equipmentByDefinition,
            LanternSnapshot lantern,
            string accountId,
            string seasonId)
        {
            if (deck == null) throw new ArgumentNullException(nameof(deck));
            if (equipmentByDefinition == null)
                throw new ArgumentNullException(nameof(equipmentByDefinition));
            if (lantern == null) throw new ArgumentNullException(nameof(lantern));
            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentException("An account id is required.", nameof(accountId));
            Guid parsedAccountId;
            var canonicalAccountId = Guid.TryParse(accountId, out parsedAccountId) &&
                                     parsedAccountId != Guid.Empty
                ? parsedAccountId.ToString("N")
                : accountId;
            if (string.IsNullOrWhiteSpace(seasonId))
                throw new ArgumentException("A season id is required.", nameof(seasonId));

            var deckCanonical = new StringBuilder(SnapshotContractVersion);
            Append(deckCanonical, "count", deck.UnitDefinitionIds.Count.ToString(
                CultureInfo.InvariantCulture));
            for (var index = 0; index < deck.UnitDefinitionIds.Count; index++)
            {
                var definitionId = deck.UnitDefinitionIds[index];
                var characterInstanceId = deck.CharacterInstanceIds[index];
                if (string.IsNullOrWhiteSpace(definitionId) ||
                    string.IsNullOrWhiteSpace(characterInstanceId))
                    throw new InvalidOperationException(
                        "Formal expedition decks require immutable character instance ids.");
                Append(deckCanonical, "definition", definitionId);
                Append(deckCanonical, "instance", characterInstanceId);
            }

            var equipmentCanonical = new StringBuilder(SnapshotContractVersion);
            for (var index = 0; index < deck.UnitDefinitionIds.Count; index++)
            {
                var definitionId = deck.UnitDefinitionIds[index];
                EquipmentLoadoutSnapshot loadout;
                if (!equipmentByDefinition.TryGetValue(definitionId, out loadout) ||
                    loadout == null)
                    throw new InvalidOperationException(
                        "Every formal deck member requires an equipment snapshot.");
                Append(equipmentCanonical, "definition", definitionId);
                var equipmentIds = new List<string>(loadout.EquipmentInstanceIds);
                equipmentIds.Sort(StringComparer.Ordinal);
                Append(equipmentCanonical, "count", equipmentIds.Count.ToString(
                    CultureInfo.InvariantCulture));
                foreach (var equipmentId in equipmentIds)
                {
                    if (string.IsNullOrWhiteSpace(equipmentId))
                        throw new InvalidOperationException(
                            "Formal equipment snapshots require immutable instance ids.");
                    Append(equipmentCanonical, "equipment", equipmentId);
                }
            }

            var lanternCanonical = new StringBuilder(SnapshotContractVersion);
            Append(lanternCanonical, "none", lantern.IsNone ? "1" : "0");
            Append(lanternCanonical, "instance", lantern.LanternInstanceId);
            Append(lanternCanonical, "template", lantern.TemplateId);
            Append(lanternCanonical, "quality", ((int)lantern.Quality).ToString(
                CultureInfo.InvariantCulture));
            Append(lanternCanonical, "season", lantern.SeasonId);
            Append(lanternCanonical, "chapter", lantern.SourceChapterId);
            Append(lanternCanonical, "core", lantern.CoreEffectId);
            Append(lanternCanonical, "baseStat", lantern.BaseFlameStatId);
            Append(lanternCanonical, "baseValue", lantern.BaseFlameValue.ToString(
                CultureInfo.InvariantCulture));
            Append(lanternCanonical, "stage", lantern.ContractStage.ToString(
                CultureInfo.InvariantCulture));
            Append(lanternCanonical, "rules", lantern.RulesVersion);
            Append(lanternCanonical, "modifierCount", lantern.ModifierRolls.Count.ToString(
                CultureInfo.InvariantCulture));
            foreach (var modifier in lantern.ModifierRolls)
            {
                Append(lanternCanonical, "modifier", modifier.ModifierId);
                Append(lanternCanonical, "value", modifier.Value.ToString(
                    CultureInfo.InvariantCulture));
            }

            var eligibilityCanonical = new StringBuilder(SnapshotContractVersion);
            // ASP.NET serializes Guid response fields in the hyphenated `D` form while
            // the server's first-clear contract binds the canonical snapshot to the
            // compact `N` form. Hash the contract form, not the transport form.
            Append(eligibilityCanonical, "account", canonicalAccountId);
            Append(eligibilityCanonical, "season", seasonId);
            Append(eligibilityCanonical, "chapter", ExpeditionEntryCoordinator.ChapterId);
            Append(eligibilityCanonical, "purpose", "first-clear-lantern-eligibility");

            return new ExpeditionLoadoutSnapshotHashes(
                Sha256Hex(deckCanonical.ToString()),
                Sha256Hex(equipmentCanonical.ToString()),
                Sha256Hex(lanternCanonical.ToString()),
                Sha256Hex(eligibilityCanonical.ToString()),
                deckCanonical.ToString(),
                equipmentCanonical.ToString(),
                lanternCanonical.ToString(),
                eligibilityCanonical.ToString());
        }

        public static AuthoritativeExpeditionRunBinding Bind(
            ExpeditionServerRunSnapshot run,
            string expectedAccountId,
            string expectedWalletAddress,
            string expectedSeasonId,
            ExpeditionLoadoutSnapshotHashes expectedHashes)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (expectedHashes == null) throw new ArgumentNullException(nameof(expectedHashes));
            RequireSameGuid(run.AccountId, expectedAccountId, "account");
            RequireExact(run.WalletAddress, expectedWalletAddress, "wallet");
            RequireExact(run.SeasonId, expectedSeasonId, "season");
            RequireExact(run.ChapterId, ExpeditionEntryCoordinator.ChapterId, "chapter");
            if (string.IsNullOrWhiteSpace(run.RunId) ||
                string.IsNullOrWhiteSpace(run.EntryIntentId))
                throw new InvalidOperationException(
                    "The authoritative expedition run identity is incomplete.");
            ChapterOneDifficultyRules.Get(run.DifficultyId);
            RequireExact(run.RewardTableVersion,
                ChapterOneDifficultyRules.RewardTableVersion, "reward table version");
            if (!string.Equals(run.EncounterTableVersion,
                    ChapterOneDifficultyRules.EncounterTableVersion, StringComparison.Ordinal) &&
                !string.Equals(run.EncounterTableVersion,
                    ChapterOneDifficultyRules.LegacyEncounterTableVersion, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The authoritative encounter table version is unsupported.");
            RequireExact(run.MapVersion,
                ChapterOneDifficultyRules.MapVersion, "map version");
            RequireExact(run.RulesetVersion,
                ChapterOneDifficultyRules.RulesVersion, "ruleset version");
            if (run.QualityRecruitPriceRulesVersion !=
                    QualityRecruitPriceRules.QualityScaledRulesVersion &&
                run.QualityRecruitPriceRulesVersion !=
                    QualityRecruitPriceRules.CurrentRulesVersion)
                throw new InvalidOperationException(
                    "The authoritative expedition recruitment price rules version is unsupported.");
            ValidateUtcWindow(run);
            RequireExact(run.DeckSnapshotHash,
                expectedHashes.DeckSnapshotHash, "deck snapshot hash");
            RequireExact(run.EquipmentSnapshotHash,
                expectedHashes.EquipmentSnapshotHash, "equipment snapshot hash");
            RequireExact(run.LanternSnapshotHash,
                expectedHashes.LanternSnapshotHash, "lantern snapshot hash");
            RequireExact(run.FirstClearLanternEligibilityKeyHash,
                expectedHashes.FirstClearEligibilityKeyHash,
                "first-clear eligibility hash");
            RequireExact(run.CombatInputContractVersion,
                CombatInputContractVersion, "combat input contract version");
            RequireExact(run.CombatInputHash,
                expectedHashes.CombatInputHash, "combat input hash");
            RequireExact(run.DeckSnapshotCanonical,
                expectedHashes.DeckSnapshotCanonical, "deck snapshot canonical");
            RequireExact(run.EquipmentSnapshotCanonical,
                expectedHashes.EquipmentSnapshotCanonical, "equipment snapshot canonical");
            RequireExact(run.LanternSnapshotCanonical,
                expectedHashes.LanternSnapshotCanonical, "lantern snapshot canonical");
            RequireExact(run.FirstClearEligibilityCanonical,
                expectedHashes.FirstClearEligibilityCanonical,
                "first-clear eligibility canonical");
            if (!IsSha256Hex(run.SeedCommitment))
                throw new InvalidOperationException(
                    "The authoritative expedition seed commitment is invalid.");

            var rewardTable = run.RewardTableVersion;
            var rewards = new ExpeditionRunRewardSnapshot(
                run.DifficultyId,
                run.RewardOrigin,
                run.EntryPriceMicroSkr == 0 ? "free" : "paid",
                new ExpeditionRewardTableReference(
                    "chapter01.full-clear.currency", rewardTable),
                new ExpeditionRewardTableReference(
                    "chapter01.full-clear.equipment", rewardTable),
                new ExpeditionRewardTableReference(
                    "chapter01.full-clear.lantern", rewardTable),
                run.RulesetVersion);
            return new AuthoritativeExpeditionRunBinding(
                run, DeriveMapSeed(run.SeedCommitment), rewards);
        }

        public static RestoredFormalExpeditionSnapshot RestoreAuthoritative(
            ExpeditionServerRunSnapshot run)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            var hashes = new ExpeditionLoadoutSnapshotHashes(
                Sha256Hex(run.DeckSnapshotCanonical),
                Sha256Hex(run.EquipmentSnapshotCanonical),
                Sha256Hex(run.LanternSnapshotCanonical),
                Sha256Hex(run.FirstClearEligibilityCanonical),
                run.DeckSnapshotCanonical,
                run.EquipmentSnapshotCanonical,
                run.LanternSnapshotCanonical,
                run.FirstClearEligibilityCanonical);
            RequireExact(run.DeckSnapshotHash, hashes.DeckSnapshotHash,
                "deck snapshot hash");
            RequireExact(run.EquipmentSnapshotHash, hashes.EquipmentSnapshotHash,
                "equipment snapshot hash");
            RequireExact(run.LanternSnapshotHash, hashes.LanternSnapshotHash,
                "lantern snapshot hash");
            RequireExact(run.FirstClearLanternEligibilityKeyHash,
                hashes.FirstClearEligibilityKeyHash,
                "first-clear eligibility hash");
            RequireExact(run.CombatInputContractVersion,
                hashes.CombatInputContractVersion, "combat input contract version");
            RequireExact(run.CombatInputHash, hashes.CombatInputHash,
                "combat input hash");

            var deckReader = new CanonicalReader(run.DeckSnapshotCanonical);
            var deckCount = deckReader.ReadInt("count", 1, 64);
            var definitions = new List<string>(deckCount);
            var instances = new List<string>(deckCount);
            for (var index = 0; index < deckCount; index++)
            {
                definitions.Add(deckReader.ReadRequired("definition"));
                instances.Add(deckReader.ReadRequired("instance"));
            }
            deckReader.RequireComplete();
            var deck = new RunDeckSnapshot(definitions, instances);

            var equipmentReader = new CanonicalReader(run.EquipmentSnapshotCanonical);
            var equipment = new Dictionary<string, string[]>(StringComparer.Ordinal);
            for (var index = 0; index < deckCount; index++)
            {
                var definitionId = equipmentReader.ReadRequired("definition");
                if (!string.Equals(definitionId, definitions[index], StringComparison.Ordinal) ||
                    equipment.ContainsKey(definitionId))
                    throw new InvalidOperationException(
                        "The authoritative equipment snapshot deck order is invalid.");
                var count = equipmentReader.ReadInt("count", 0, 16);
                var ids = new string[count];
                var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
                for (var equipmentIndex = 0; equipmentIndex < count; equipmentIndex++)
                {
                    ids[equipmentIndex] = equipmentReader.ReadRequired("equipment");
                    if (!uniqueIds.Add(ids[equipmentIndex]))
                        throw new InvalidOperationException(
                            "The authoritative equipment snapshot contains duplicate ids.");
                }
                equipment.Add(definitionId, ids);
            }
            equipmentReader.RequireComplete();

            var lanternReader = new CanonicalReader(run.LanternSnapshotCanonical);
            var isNone = lanternReader.ReadInt("none", 0, 1) == 1;
            var lanternInstanceId = lanternReader.Read("instance");
            var templateId = lanternReader.Read("template");
            var qualityValue = lanternReader.ReadInt("quality", 0,
                Enum.GetValues(typeof(LanternQuality)).Length - 1);
            var seasonId = lanternReader.Read("season");
            var sourceChapterId = lanternReader.Read("chapter");
            var coreEffectId = lanternReader.Read("core");
            var baseStatId = lanternReader.Read("baseStat");
            var baseValue = lanternReader.ReadInt("baseValue", 0, int.MaxValue);
            var contractStage = lanternReader.ReadInt("stage", 0, 6);
            var rulesVersion = lanternReader.ReadRequired("rules");
            var modifierCount = lanternReader.ReadInt("modifierCount", 0, 32);
            var modifiers = new List<LanternModifierRoll>(modifierCount);
            var modifierIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < modifierCount; index++)
            {
                var modifierId = lanternReader.ReadRequired("modifier");
                var value = lanternReader.ReadInt("value", 0, int.MaxValue);
                if (!modifierIds.Add(modifierId))
                    throw new InvalidOperationException(
                        "The authoritative lantern snapshot contains duplicate modifiers.");
                modifiers.Add(new LanternModifierRoll(modifierId, value));
            }
            lanternReader.RequireComplete();
            var lantern = LanternSnapshot.Restore(
                isNone, lanternInstanceId, templateId, (LanternQuality)qualityValue,
                seasonId, sourceChapterId, coreEffectId, modifiers, rulesVersion,
                baseStatId, baseValue, contractStage);

            var eligibilityReader = new CanonicalReader(
                run.FirstClearEligibilityCanonical);
            RequireSameGuid(eligibilityReader.ReadRequired("account"), run.AccountId,
                "first-clear eligibility account");
            RequireExact(eligibilityReader.ReadRequired("season"), run.SeasonId,
                "first-clear eligibility season");
            RequireExact(eligibilityReader.ReadRequired("chapter"), run.ChapterId,
                "first-clear eligibility chapter");
            RequireExact(eligibilityReader.ReadRequired("purpose"),
                "first-clear-lantern-eligibility", "first-clear eligibility purpose");
            eligibilityReader.RequireComplete();

            return new RestoredFormalExpeditionSnapshot(hashes, deck, equipment, lantern);
        }

        private static void ValidateUtcWindow(ExpeditionServerRunSnapshot run)
        {
            RequireExact(run.AuthoritativeTimeZone,
                ChapterOneDifficultyRules.AuthoritativeTimeZone,
                "authoritative time zone");
            RequireExact(run.TimeRulesVersion,
                ChapterOneDifficultyRules.TimeRulesVersion,
                "time rules version");
            DateTimeOffset start;
            DateTimeOffset end;
            DateTimeOffset close;
            DateTimeOffset expires;
            DateTimeOffset created;
            if (!DateTimeOffset.TryParse(run.QuotaDayStartAtUtc, out start) ||
                !DateTimeOffset.TryParse(run.QuotaDayEndAtUtc, out end) ||
                !DateTimeOffset.TryParse(run.FormalCreationCloseAtUtc, out close) ||
                !DateTimeOffset.TryParse(run.RunExpiresAtUtc, out expires) ||
                !DateTimeOffset.TryParse(run.CreatedAt, out created) ||
                start.Offset != TimeSpan.Zero || end.Offset != TimeSpan.Zero ||
                close.Offset != TimeSpan.Zero || expires.Offset != TimeSpan.Zero ||
                created.Offset != TimeSpan.Zero || start >= close || close >= end ||
                end - start != TimeSpan.FromDays(1) || expires != end ||
                created < start || created >= close ||
                !string.Equals(run.QuotaDayId,
                    start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "_UTC",
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The authoritative expedition UTC window is invalid.");
        }

        public static uint DeriveMapSeed(string seedCommitment)
        {
            if (!IsSha256Hex(seedCommitment))
                throw new ArgumentException(
                    "A lowercase SHA-256 seed commitment is required.",
                    nameof(seedCommitment));
            var bytes = Encoding.UTF8.GetBytes(
                MapSeedDerivationVersion + "|" + seedCommitment);
            byte[] digest;
            using (var algorithm = SHA256.Create()) digest = algorithm.ComputeHash(bytes);
            var seed = ((uint)digest[0] << 24) |
                       ((uint)digest[1] << 16) |
                       ((uint)digest[2] << 8) |
                       digest[3];
            return seed == 0 ? 0x51EA2C31u : seed;
        }

        public static bool IsSha256Hex(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64) return false;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if ((character < '0' || character > '9') &&
                    (character < 'a' || character > 'f')) return false;
            }
            return true;
        }

        public static string ComputeCombatInputHash(string deckSnapshotHash,
            string equipmentSnapshotHash, string lanternSnapshotHash,
            string firstClearEligibilityKeyHash)
        {
            return Sha256Hex(string.Join("|", new[]
            {
                CombatInputContractVersion,
                "deck:" + deckSnapshotHash,
                "equipment:" + equipmentSnapshotHash,
                "lantern:" + lanternSnapshotHash,
                "eligibility:" + firstClearEligibilityKeyHash
            }));
        }

        private static string Sha256Hex(string canonical)
        {
            byte[] digest;
            using (var algorithm = SHA256.Create())
                digest = algorithm.ComputeHash(Encoding.UTF8.GetBytes(canonical));
            var builder = new StringBuilder(digest.Length * 2);
            foreach (var value in digest) builder.Append(value.ToString("x2"));
            return builder.ToString();
        }

        private static void Append(StringBuilder builder, string key, string value)
        {
            value = value ?? string.Empty;
            builder.Append('|').Append(key).Append(':')
                .Append(value.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':').Append(value);
        }

        private static void RequireExact(string actual, string expected, string field)
        {
            if (string.IsNullOrWhiteSpace(expected) ||
                !string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The authoritative expedition " + field + " does not match the local contract.");
        }

        private static void RequireSameGuid(string actual, string expected, string field)
        {
            Guid actualGuid;
            Guid expectedGuid;
            if (!Guid.TryParse(actual, out actualGuid) || actualGuid == Guid.Empty ||
                !Guid.TryParse(expected, out expectedGuid) || actualGuid != expectedGuid)
                throw new InvalidOperationException(
                    "The authoritative expedition " + field + " does not match the local contract.");
        }

        private sealed class CanonicalReader
        {
            private readonly string canonical;
            private int offset;

            public CanonicalReader(string canonical)
            {
                if (string.IsNullOrEmpty(canonical) ||
                    !canonical.StartsWith(SnapshotContractVersion, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "The authoritative expedition canonical snapshot is invalid.");
                this.canonical = canonical;
                offset = SnapshotContractVersion.Length;
            }

            public string Read(string expectedKey)
            {
                if (offset >= canonical.Length || canonical[offset] != '|')
                    throw new InvalidOperationException(
                        "The authoritative expedition canonical snapshot is truncated.");
                offset++;
                var keyEnd = canonical.IndexOf(':', offset);
                if (keyEnd < 0) throw Invalid();
                var key = canonical.Substring(offset, keyEnd - offset);
                if (!string.Equals(key, expectedKey, StringComparison.Ordinal)) throw Invalid();
                offset = keyEnd + 1;
                var lengthEnd = canonical.IndexOf(':', offset);
                if (lengthEnd < 0) throw Invalid();
                int length;
                if (!int.TryParse(canonical.Substring(offset, lengthEnd - offset),
                        NumberStyles.None, CultureInfo.InvariantCulture, out length) || length < 0)
                    throw Invalid();
                offset = lengthEnd + 1;
                if (length > canonical.Length - offset) throw Invalid();
                var value = canonical.Substring(offset, length);
                offset += length;
                return value;
            }

            public string ReadRequired(string expectedKey)
            {
                var value = Read(expectedKey);
                if (string.IsNullOrWhiteSpace(value)) throw Invalid();
                return value;
            }

            public int ReadInt(string expectedKey, int minimum, int maximum)
            {
                int value;
                if (!int.TryParse(Read(expectedKey), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out value) ||
                    value < minimum || value > maximum)
                    throw Invalid();
                return value;
            }

            public void RequireComplete()
            {
                if (offset != canonical.Length) throw Invalid();
            }

            private static InvalidOperationException Invalid()
            {
                return new InvalidOperationException(
                    "The authoritative expedition canonical snapshot is malformed.");
            }
        }
    }
}
