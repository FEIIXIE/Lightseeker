using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum ExpeditionItemKind { Combat, Immediate }

    public static class QualityRecruitPriceRules
    {
        public const int LegacyRulesVersion = 0;
        public const int QualityScaledRulesVersion = 1;
        public const int CurrentRulesVersion = 2;

        public static int QuoteListedPrice(
            int templateLevelOnePrice,
            CharacterQuality quality,
            int runLevel,
            int rulesVersion = CurrentRulesVersion)
        {
            if (templateLevelOnePrice < 0)
                throw new ArgumentOutOfRangeException(nameof(templateLevelOnePrice));
            if (!Enum.IsDefined(typeof(CharacterQuality), quality))
                throw new ArgumentOutOfRangeException(nameof(quality));
            if (runLevel < 1 || runLevel > RunUnitRules.MaximumRunLevel)
                throw new ArgumentOutOfRangeException(nameof(runLevel));
            if (rulesVersion != LegacyRulesVersion &&
                rulesVersion != QualityScaledRulesVersion &&
                rulesVersion != CurrentRulesVersion)
                throw new ArgumentOutOfRangeException(nameof(rulesVersion));

            var levelOnePrice = templateLevelOnePrice;
            if (rulesVersion == QualityScaledRulesVersion)
            {
                var qualityPriceBps = QualityPriceBasisPoints(quality);
                levelOnePrice = (templateLevelOnePrice * qualityPriceBps + 9999) / 10000;
            }

            var runLevelMultiplier = runLevel == 1 ? 1 : runLevel == 2 ? 3 : 9;
            return levelOnePrice * runLevelMultiplier;
        }

        public static int QualityPriceBasisPoints(CharacterQuality quality)
        {
            switch (quality)
            {
                case CharacterQuality.Common: return 10000;
                case CharacterQuality.Uncommon: return 12000;
                case CharacterQuality.Rare: return 14500;
                case CharacterQuality.Epic: return 17500;
                case CharacterQuality.Legendary: return 21000;
                default: throw new ArgumentOutOfRangeException(nameof(quality));
            }
        }
    }

    public sealed class ExpeditionItemDefinition
    {
        public ExpeditionItemDefinition(string id, int cost, ExpeditionItemKind kind)
        {
            Id = id;
            Cost = cost;
            Kind = kind;
        }
        public string Id { get; private set; }
        public int Cost { get; private set; }
        public ExpeditionItemKind Kind { get; private set; }
    }

    public sealed class ExpeditionEconomyState
    {
        private readonly List<string> backpack = new List<string>();
        private readonly List<string> backpackInstanceIds = new List<string>();
        private readonly List<string> targetedRecruitTickets = new List<string>();
        private readonly HashSet<string> resolvedActionIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> lockedNodeContent =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public const int BackpackCapacity = 3;
        public bool FirstManualRecruitRerollUsed { get; private set; }
        public int RecruitRerollVouchers { get; private set; }
        public int RecruitmentDiscount { get; private set; }
        public int ChestRecruitmentDiscount { get; private set; }
        public int Morale { get; private set; } = 2;
        public string NextBattlePreparationId { get; private set; }
        public string PendingPreparationId
        {
            get
            {
                return !string.IsNullOrEmpty(NextBattlePreparationId) &&
                       NextBattlePreparationId.StartsWith("P", StringComparison.Ordinal)
                    ? NextBattlePreparationId
                    : string.Empty;
            }
        }
        public string ActiveBattleItemId { get; private set; }
        public string ActiveBattleItemInstanceId { get; private set; }
        public int ActiveBattleTargetSlot { get; private set; } = -1;
        public IReadOnlyList<string> Backpack { get { return backpack; } }
        public IReadOnlyList<string> BackpackInstanceIds { get { return backpackInstanceIds; } }
        public IReadOnlyList<string> TargetedRecruitTickets { get { return targetedRecruitTickets; } }

        public int PeekManualRerollCost()
        {
            if (targetedRecruitTickets.Count > 0) return 0;
            if (!FirstManualRecruitRerollUsed) return 0;
            return RecruitRerollVouchers > 0 ? 0 : 2;
        }

        public int ConsumeManualRerollCost()
        {
            if (targetedRecruitTickets.Count > 0) return 0;
            if (!FirstManualRecruitRerollUsed)
            {
                FirstManualRecruitRerollUsed = true;
                return 0;
            }
            if (RecruitRerollVouchers > 0)
            {
                RecruitRerollVouchers--;
                return 0;
            }
            return 2;
        }

        public int ApplyRecruitmentDiscount(int listedPrice)
        {
            var price = QuoteRecruitmentPrice(listedPrice);
            var useVoucher = RecruitmentDiscount >= ChestRecruitmentDiscount;
            var discount = useVoucher ? RecruitmentDiscount : ChestRecruitmentDiscount;
            if (discount <= 0) return price;
            if (useVoucher) RecruitmentDiscount = 0;
            else ChestRecruitmentDiscount = 0;
            return price;
        }

        public int QuoteRecruitmentPrice(int listedPrice)
        {
            var discount = Math.Max(RecruitmentDiscount, ChestRecruitmentDiscount);
            return Math.Max(0, Math.Max(0, listedPrice) - discount);
        }

        public void AddRerollVoucher(ref int coins)
        {
            if (RecruitRerollVouchers >= 1) coins += 2;
            else RecruitRerollVouchers = 1;
        }

        public void SetRecruitmentDiscount(int amount, bool fromChest)
        {
            if (fromChest) ChestRecruitmentDiscount = Math.Max(ChestRecruitmentDiscount, amount);
            else RecruitmentDiscount = Math.Max(RecruitmentDiscount, amount);
        }

        public bool TryAddCombatItem(string itemId, string itemInstanceId = null)
        {
            if (string.IsNullOrEmpty(itemId) || backpack.Count >= BackpackCapacity) return false;
            backpack.Add(itemId);
            backpackInstanceIds.Add(string.IsNullOrEmpty(itemInstanceId)
                ? "legacy-item-" + Guid.NewGuid().ToString("N")
                : itemInstanceId);
            return true;
        }

        public bool TryDiscardFirstCombatItem()
        {
            if (backpack.Count == 0) return false;
            backpack.RemoveAt(0);
            backpackInstanceIds.RemoveAt(0);
            return true;
        }

        public bool TryDiscardCombatItemAt(int index)
        {
            if (index < 0 || index >= backpack.Count) return false;
            backpack.RemoveAt(index);
            backpackInstanceIds.RemoveAt(index);
            return true;
        }

        public bool TryActivateBattleItem(int backpackIndex, int targetSlot)
        {
            if (!string.IsNullOrEmpty(ActiveBattleItemId) ||
                backpackIndex < 0 || backpackIndex >= backpack.Count) return false;
            ActiveBattleItemId = backpack[backpackIndex];
            ActiveBattleItemInstanceId = backpackInstanceIds[backpackIndex];
            // The v1.0 merchant contract applies every item to a whole side and
            // explicitly forbids player target selection.
            ActiveBattleTargetSlot = -1;
            backpack.RemoveAt(backpackIndex);
            backpackInstanceIds.RemoveAt(backpackIndex);
            return true;
        }

        public void ClearActiveBattleItem()
        {
            ActiveBattleItemId = null;
            ActiveBattleItemInstanceId = null;
            ActiveBattleTargetSlot = -1;
        }

        public void EnqueueTargetedRecruitTicket(string dimension, string tag)
        {
            if ((dimension != "profession" && dimension != "faction") ||
                string.IsNullOrEmpty(tag))
                throw new ArgumentException("A targeted recruit ticket requires a valid dimension and tag.");
            targetedRecruitTickets.Add(dimension + "=" + tag);
        }

        public string PeekTargetedRecruitTicket()
        {
            return targetedRecruitTickets.Count == 0 ? string.Empty : targetedRecruitTickets[0];
        }

        public bool ConsumeTargetedRecruitTicket(string ticket)
        {
            if (targetedRecruitTickets.Count == 0 ||
                !string.Equals(targetedRecruitTickets[0], ticket, StringComparison.Ordinal)) return false;
            targetedRecruitTickets.RemoveAt(0);
            return true;
        }

        public bool AddRuleMarker(string marker)
        {
            return !string.IsNullOrEmpty(marker) && resolvedActionIds.Add(marker);
        }

        public bool ConsumeRuleMarker(string marker)
        {
            return !string.IsNullOrEmpty(marker) && resolvedActionIds.Remove(marker);
        }

        public string FindRuleMarker(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return string.Empty;
            string selected = null;
            foreach (var marker in resolvedActionIds)
            {
                if (!marker.StartsWith(prefix, StringComparison.Ordinal)) continue;
                if (selected == null || string.CompareOrdinal(marker, selected) < 0)
                    selected = marker;
            }
            return selected ?? string.Empty;
        }

        public bool TryResolveAction(string actionId)
        {
            return !string.IsNullOrEmpty(actionId) && resolvedActionIds.Add(actionId);
        }

        public bool HasResolvedAction(string actionId)
        {
            return !string.IsNullOrEmpty(actionId) && resolvedActionIds.Contains(actionId);
        }

        public bool GetOrLockNodeContent(string contentKey, string proposedValue, out string lockedValue)
        {
            if (string.IsNullOrEmpty(contentKey))
            {
                lockedValue = proposedValue ?? string.Empty;
                return false;
            }
            if (lockedNodeContent.TryGetValue(contentKey, out lockedValue)) return false;
            lockedValue = proposedValue ?? string.Empty;
            lockedNodeContent[contentKey] = lockedValue;
            return true;
        }

        public bool TryGetLockedNodeContent(string contentKey, out string lockedValue)
        {
            if (string.IsNullOrEmpty(contentKey))
            {
                lockedValue = string.Empty;
                return false;
            }
            return lockedNodeContent.TryGetValue(contentKey, out lockedValue);
        }

        public void AddMorale(int amount)
        {
            Morale = Math.Max(0, Math.Min(2, Morale + amount));
        }

        public void SetNextBattlePreparation(string preparationId)
        {
            NextBattlePreparationId = preparationId;
        }

        public bool TryAcquirePreparation(string preparationId)
        {
            if (string.IsNullOrEmpty(preparationId) ||
                !preparationId.StartsWith("P", StringComparison.Ordinal) ||
                !string.IsNullOrEmpty(PendingPreparationId)) return false;
            var definition = ChapterOneExpeditionPreparationRules.Get(preparationId);
            if (definition == null || !CanAcquirePreparation(preparationId)) return false;
            NextBattlePreparationId = preparationId;
            resolvedActionIds.Add("prep:acquired:" + preparationId);
            if (preparationId == "P09" || preparationId == "P11")
                resolvedActionIds.Add("prep:acquired:EXTRA_GROWTH_PREP");
            return true;
        }

        public bool CanAcquirePreparation(string preparationId)
        {
            if (string.IsNullOrEmpty(preparationId)) return false;
            if (preparationId == "P09" || preparationId == "P11")
                return !resolvedActionIds.Contains("prep:acquired:EXTRA_GROWTH_PREP");
            if (preparationId == "P01" || preparationId == "P02" ||
                preparationId == "P03" || preparationId == "P04" ||
                preparationId == "P05" || preparationId == "P06" ||
                preparationId == "P07" || preparationId == "P08" ||
                preparationId == "P10" || preparationId == "P12")
                return !resolvedActionIds.Contains("prep:acquired:" + preparationId);
            return false;
        }

        public bool TryConsumePreparation(string preparationId)
        {
            if (!string.Equals(PendingPreparationId, preparationId, StringComparison.Ordinal))
                return false;
            NextBattlePreparationId = null;
            return true;
        }

        public void ConsumeNextBattlePreparation() { NextBattlePreparationId = null; }

        public void Restore(
            bool firstRerollUsed, int rerollVouchers, int recruitmentDiscount,
            int chestDiscount, int morale, string preparationId,
            IEnumerable<string> backpackItems, IEnumerable<string> actionIds,
            string activeBattleItemId = null, int activeBattleTargetSlot = -1,
            IEnumerable<string> lockedContentKeys = null,
            IEnumerable<string> lockedContentValues = null,
            IEnumerable<string> backpackItemInstanceIds = null,
            string activeBattleItemInstanceId = null,
            IEnumerable<string> recruitTickets = null)
        {
            FirstManualRecruitRerollUsed = firstRerollUsed;
            RecruitRerollVouchers = Math.Max(0, Math.Min(1, rerollVouchers));
            RecruitmentDiscount = Math.Max(0, recruitmentDiscount);
            ChestRecruitmentDiscount = Math.Max(0, chestDiscount);
            Morale = Math.Max(0, Math.Min(2, morale));
            NextBattlePreparationId = preparationId;
            ActiveBattleItemId = activeBattleItemId;
            ActiveBattleItemInstanceId = activeBattleItemInstanceId;
            ActiveBattleTargetSlot = string.IsNullOrEmpty(activeBattleItemId)
                ? -1 : activeBattleTargetSlot;
            backpack.Clear();
            backpackInstanceIds.Clear();
            if (backpackItems != null)
                foreach (var id in backpackItems)
                    if (!string.IsNullOrEmpty(id) && backpack.Count < BackpackCapacity) backpack.Add(id);
            if (backpackItemInstanceIds != null)
                foreach (var id in backpackItemInstanceIds)
                    if (!string.IsNullOrEmpty(id) && backpackInstanceIds.Count < backpack.Count)
                        backpackInstanceIds.Add(id);
            while (backpackInstanceIds.Count < backpack.Count)
                backpackInstanceIds.Add("legacy-item-" + Guid.NewGuid().ToString("N"));
            targetedRecruitTickets.Clear();
            if (recruitTickets != null)
                foreach (var ticket in recruitTickets)
                    if (!string.IsNullOrEmpty(ticket)) targetedRecruitTickets.Add(ticket);
            resolvedActionIds.Clear();
            if (actionIds != null)
                foreach (var id in actionIds) if (!string.IsNullOrEmpty(id)) resolvedActionIds.Add(id);
            lockedNodeContent.Clear();
            if (lockedContentKeys != null && lockedContentValues != null)
            {
                using (var keyEnumerator = lockedContentKeys.GetEnumerator())
                using (var valueEnumerator = lockedContentValues.GetEnumerator())
                {
                    while (keyEnumerator.MoveNext() && valueEnumerator.MoveNext())
                    {
                        var key = keyEnumerator.Current;
                        if (!string.IsNullOrEmpty(key))
                            lockedNodeContent[key] = valueEnumerator.Current ?? string.Empty;
                    }
                }
            }
        }

        public string[] CopyResolvedActionIds()
        {
            var result = new string[resolvedActionIds.Count];
            resolvedActionIds.CopyTo(result);
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }

        public string[] CopyLockedNodeContentKeys()
        {
            var keys = new List<string>(lockedNodeContent.Keys);
            keys.Sort(StringComparer.Ordinal);
            return keys.ToArray();
        }

        public string[] CopyLockedNodeContentValues()
        {
            var keys = new List<string>(lockedNodeContent.Keys);
            keys.Sort(StringComparer.Ordinal);
            var values = new string[keys.Count];
            for (var index = 0; index < keys.Count; index++) values[index] = lockedNodeContent[keys[index]];
            return values;
        }

        public void Reset()
        {
            FirstManualRecruitRerollUsed = false;
            RecruitRerollVouchers = 0;
            RecruitmentDiscount = 0;
            ChestRecruitmentDiscount = 0;
            Morale = 2;
            NextBattlePreparationId = null;
            ClearActiveBattleItem();
            backpack.Clear();
            backpackInstanceIds.Clear();
            targetedRecruitTickets.Clear();
            resolvedActionIds.Clear();
            lockedNodeContent.Clear();
        }
    }

    public static class ChapterOneExpeditionEconomy
    {
        public const int EconomyVersion = 2;
        private static readonly ExpeditionItemDefinition[] ItemDefinitions =
        {
            new ExpeditionItemDefinition("I01", 4, ExpeditionItemKind.Combat),
            new ExpeditionItemDefinition("I02", 5, ExpeditionItemKind.Combat),
            new ExpeditionItemDefinition("I03", 4, ExpeditionItemKind.Combat),
            new ExpeditionItemDefinition("I04", 5, ExpeditionItemKind.Combat),
            new ExpeditionItemDefinition("I05", 5, ExpeditionItemKind.Combat),
            new ExpeditionItemDefinition("I06", 5, ExpeditionItemKind.Combat),
            new ExpeditionItemDefinition("I07", 4, ExpeditionItemKind.Combat),
            new ExpeditionItemDefinition("I08", 5, ExpeditionItemKind.Combat),
            new ExpeditionItemDefinition("I09", 5, ExpeditionItemKind.Combat)
        };

        public static IReadOnlyList<ExpeditionItemDefinition> Items { get { return ItemDefinitions; } }

        public static int ProgressBand(float progress)
        {
            if (progress < 45f) return 1;
            if (progress < 62.3f) return 2;
            if (progress < 85f) return 3;
            return 4;
        }

        public static int SupplyCoinAmount(float progress)
        {
            return new[] { 3, 4, 5, 6 }[ProgressBand(progress) - 1];
        }

        public static int SupplyRecruitCoinAmount(float progress)
        {
            return new[] { 1, 2, 2, 3 }[ProgressBand(progress) - 1];
        }

        public static string[] GenerateCombatItemIds(
            uint mapSeed, string nodeId, float progress, int count, string sourceId)
        {
            var band = ProgressBand(progress);
            var maximumItem = band == 1 ? 2 : band == 2 ? 4 : 5;
            var pool = new List<string>();
            for (var index = 1; index <= maximumItem; index++)
                pool.Add("I" + index.ToString("00"));
            var random = new StableRandom(ChapterOneEncounterRules.Mix(
                mapSeed, nodeId + ":items:" + sourceId, EconomyVersion));
            var selected = new List<string>();
            var selectionCount = Math.Min(Math.Max(1, count), pool.Count);
            while (selected.Count < selectionCount)
            {
                var itemIndex = random.Next(pool.Count);
                selected.Add(pool[itemIndex]);
                pool.RemoveAt(itemIndex);
            }
            return selected.ToArray();
        }

        public static int TreasureCoinAmount(float progress)
        {
            return new[] { 5, 6, 7, 8 }[ProgressBand(progress) - 1];
        }

        public static string GenerateEventId(
            uint mapSeed, string nodeId, float progress, int morale, bool hasBackpackItem)
        {
            var candidates = new List<string> { "V01", "V03", "V06" };
            if (progress < 85f) candidates.Add("V02");
            if (morale >= 2) candidates.Add("V04");
            if (hasBackpackItem) candidates.Add("V05");
            if (morale < 2) candidates.Add("V07");
            if (progress < 83.5f) candidates.Add("V08");
            var random = new StableRandom(ChapterOneEncounterRules.Mix(
                mapSeed, nodeId + ":event", EconomyVersion));
            return random.Pick(candidates);
        }

        public static string[] GenerateShopItemIds(
            uint mapSeed, string nodeId, float progress, int morale, bool rallyHornUsed = false,
            IEnumerable<string> previousMerchantItemIds = null)
        {
            var random = new StableRandom(ChapterOneEncounterRules.Mix(mapSeed, nodeId + ":shop", EconomyVersion));
            var previous = previousMerchantItemIds == null
                ? null
                : new HashSet<string>(previousMerchantItemIds, StringComparer.Ordinal);
            var selected = new List<string>
            {
                PickMerchantCategory(random, new[] { "I01", "I02", "I03" }, previous),
                PickMerchantCategory(random, new[] { "I04", "I05", "I06" }, previous),
                PickMerchantCategory(random, new[] { "I07", "I08", "I09" }, previous)
            };
            for (var index = selected.Count - 1; index > 0; index--)
            {
                var swap = random.Next(index + 1);
                var value = selected[index];
                selected[index] = selected[swap];
                selected[swap] = value;
            }
            return selected.ToArray();
        }

        private static string PickMerchantCategory(
            StableRandom random, string[] candidates, HashSet<string> previous)
        {
            if (previous == null || previous.Count == 0) return random.Pick(candidates);
            var eligible = new List<string>();
            for (var index = 0; index < candidates.Length; index++)
                if (!previous.Contains(candidates[index])) eligible.Add(candidates[index]);
            return random.Pick(eligible.Count > 0 ? eligible : new List<string>(candidates));
        }

        public static int RecruitOfferLevel(uint mapSeed, string nodeId, int refreshSerial, int offerIndex, float progress)
        {
            var random = new StableRandom(ChapterOneEncounterRules.Mix(
                mapSeed, nodeId + ":recruit:" + refreshSerial + ":" + offerIndex, EconomyVersion));
            var roll = random.Next(1000);
            if (progress < 45f) return 1;
            if (progress < 62.3f) return roll < 50 ? 2 : 1;
            if (progress < 85f) return roll < 2 ? 3 : roll < 102 ? 2 : 1;
            return roll < 5 ? 3 : roll < 155 ? 2 : 1;
        }

        public static int RecruitOfferDefinitionIndex(
            uint mapSeed,
            int round,
            int refreshSerial,
            int offerIndex,
            int definitionCount)
        {
            if (round < 1) throw new ArgumentOutOfRangeException(nameof(round));
            if (refreshSerial < 1) throw new ArgumentOutOfRangeException(nameof(refreshSerial));
            if (offerIndex < 0) throw new ArgumentOutOfRangeException(nameof(offerIndex));
            if (definitionCount < 1) throw new ArgumentOutOfRangeException(nameof(definitionCount));

            var seed = unchecked(mapSeed ^ (uint)(round * 7919) ^ (uint)(refreshSerial * 104729));
            var random = new StableRandom(seed);
            var selected = 0;
            for (var index = 0; index <= offerIndex; index++)
                selected = random.Next(definitionCount);
            return selected;
        }

        public static string GenerateTargetedRecruitTicket(
            uint mapSeed, string runId, string nodeId, CharacterCatalog catalog,
            IReadOnlyList<string> lockedDeckDefinitionIds)
        {
            if (catalog == null || lockedDeckDefinitionIds == null ||
                lockedDeckDefinitionIds.Count == 0) return string.Empty;
            var professions = DistinctDeckTags(catalog, lockedDeckDefinitionIds, true);
            var factions = DistinctDeckTags(catalog, lockedDeckDefinitionIds, false);
            if (professions.Count == 0 && factions.Count == 0) return string.Empty;
            var random = new StableRandom(ChapterOneEncounterRules.Mix(
                mapSeed, (runId ?? string.Empty) + ":" + (nodeId ?? string.Empty) +
                         ":targeted-recruit-ticket-v1", EconomyVersion));
            var profession = professions.Count > 0 &&
                             (factions.Count == 0 || random.Next(2) == 0);
            var tags = profession ? professions : factions;
            return (profession ? "profession=" : "faction=") + random.Pick(tags);
        }

        public static string MostRepresentedRecruitTarget(
            uint mapSeed, string runId, string nodeId, CharacterCatalog catalog,
            IReadOnlyList<string> deployedDefinitionIds,
            IReadOnlyList<string> lockedDeckDefinitionIds, bool profession)
        {
            if (catalog == null || deployedDefinitionIds == null ||
                lockedDeckDefinitionIds == null) return string.Empty;
            var legal = new HashSet<string>(
                DistinctDeckTags(catalog, lockedDeckDefinitionIds, profession),
                StringComparer.Ordinal);
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var definitionId in deployedDefinitionIds)
            {
                if (string.IsNullOrEmpty(definitionId) || !catalog.Contains(definitionId)) continue;
                var definition = catalog.Get(definitionId);
                var tag = profession ? definition.ProfessionId : definition.FactionId;
                if (string.IsNullOrEmpty(tag) || !legal.Contains(tag)) continue;
                int count;
                counts.TryGetValue(tag, out count);
                counts[tag] = count + 1;
            }
            if (counts.Count == 0) return string.Empty;
            var maximum = 0;
            foreach (var pair in counts) maximum = Math.Max(maximum, pair.Value);
            var tied = new List<string>();
            foreach (var pair in counts) if (pair.Value == maximum) tied.Add(pair.Key);
            tied.Sort(StringComparer.Ordinal);
            var random = new StableRandom(ChapterOneEncounterRules.Mix(
                mapSeed, (runId ?? string.Empty) + ":" + (nodeId ?? string.Empty) +
                         (profession ? ":profession-target-v1" : ":faction-target-v1"),
                EconomyVersion));
            return tied[random.Next(tied.Count)];
        }

        public static bool MatchesRecruitTarget(
            CharacterCatalog catalog, string definitionId, string ticket)
        {
            if (catalog == null || string.IsNullOrEmpty(definitionId) ||
                !catalog.Contains(definitionId) || string.IsNullOrEmpty(ticket)) return false;
            var separator = ticket.IndexOf('=');
            if (separator <= 0 || separator >= ticket.Length - 1) return false;
            var dimension = ticket.Substring(0, separator);
            var tag = ticket.Substring(separator + 1);
            var definition = catalog.Get(definitionId);
            return dimension == "profession"
                ? string.Equals(definition.ProfessionId, tag, StringComparison.Ordinal)
                : dimension == "faction" &&
                  string.Equals(definition.FactionId, tag, StringComparison.Ordinal);
        }

        private static List<string> DistinctDeckTags(
            CharacterCatalog catalog, IReadOnlyList<string> definitionIds, bool profession)
        {
            var tags = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definitionId in definitionIds)
            {
                if (string.IsNullOrEmpty(definitionId) || !catalog.Contains(definitionId)) continue;
                var definition = catalog.Get(definitionId);
                var tag = profession ? definition.ProfessionId : definition.FactionId;
                if (!string.IsNullOrEmpty(tag)) tags.Add(tag);
            }
            var result = new List<string>(tags);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        public static ExpeditionItemDefinition GetItem(string itemId)
        {
            foreach (var item in ItemDefinitions) if (item.Id == itemId) return item;
            return null;
        }

        private sealed class StableRandom
        {
            private uint state;
            public StableRandom(uint seed) { state = seed == 0 ? 0x9E3779B9u : seed; }
            private uint NextUInt()
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state;
            }
            public int Next(int maximum) { return maximum <= 1 ? 0 : (int)(NextUInt() % (uint)maximum); }
            public T Pick<T>(IList<T> values) { return values[Next(values.Count)]; }
        }
    }
}
