using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum ExpeditionItemKind { Combat, Immediate }

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
        public string ActiveBattleItemId { get; private set; }
        public int ActiveBattleTargetSlot { get; private set; } = -1;
        public IReadOnlyList<string> Backpack { get { return backpack; } }

        public int PeekManualRerollCost()
        {
            if (!FirstManualRecruitRerollUsed) return 0;
            return RecruitRerollVouchers > 0 ? 0 : 2;
        }

        public int ConsumeManualRerollCost()
        {
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

        public bool TryAddCombatItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || backpack.Count >= BackpackCapacity) return false;
            backpack.Add(itemId);
            return true;
        }

        public bool TryDiscardFirstCombatItem()
        {
            if (backpack.Count == 0) return false;
            backpack.RemoveAt(0);
            return true;
        }

        public bool TryDiscardCombatItemAt(int index)
        {
            if (index < 0 || index >= backpack.Count) return false;
            backpack.RemoveAt(index);
            return true;
        }

        public bool TryActivateBattleItem(int backpackIndex, int targetSlot)
        {
            if (!string.IsNullOrEmpty(ActiveBattleItemId) ||
                backpackIndex < 0 || backpackIndex >= backpack.Count) return false;
            ActiveBattleItemId = backpack[backpackIndex];
            ActiveBattleTargetSlot = Math.Max(0, targetSlot);
            backpack.RemoveAt(backpackIndex);
            return true;
        }

        public void ClearActiveBattleItem()
        {
            ActiveBattleItemId = null;
            ActiveBattleTargetSlot = -1;
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

        public void AddMorale(int amount)
        {
            Morale = Math.Max(0, Math.Min(2, Morale + amount));
        }

        public void SetNextBattlePreparation(string preparationId)
        {
            NextBattlePreparationId = preparationId;
        }

        public void ConsumeNextBattlePreparation() { NextBattlePreparationId = null; }

        public void Restore(
            bool firstRerollUsed, int rerollVouchers, int recruitmentDiscount,
            int chestDiscount, int morale, string preparationId,
            IEnumerable<string> backpackItems, IEnumerable<string> actionIds,
            string activeBattleItemId = null, int activeBattleTargetSlot = -1,
            IEnumerable<string> lockedContentKeys = null,
            IEnumerable<string> lockedContentValues = null)
        {
            FirstManualRecruitRerollUsed = firstRerollUsed;
            RecruitRerollVouchers = Math.Max(0, Math.Min(1, rerollVouchers));
            RecruitmentDiscount = Math.Max(0, recruitmentDiscount);
            ChestRecruitmentDiscount = Math.Max(0, chestDiscount);
            Morale = Math.Max(0, Math.Min(2, morale));
            NextBattlePreparationId = preparationId;
            ActiveBattleItemId = activeBattleItemId;
            ActiveBattleTargetSlot = string.IsNullOrEmpty(activeBattleItemId)
                ? -1 : Math.Max(0, activeBattleTargetSlot);
            backpack.Clear();
            if (backpackItems != null)
                foreach (var id in backpackItems)
                    if (!string.IsNullOrEmpty(id) && backpack.Count < BackpackCapacity) backpack.Add(id);
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
            resolvedActionIds.Clear();
            lockedNodeContent.Clear();
        }
    }

    public static class ChapterOneExpeditionEconomy
    {
        public const int EconomyVersion = 1;
        private static readonly ExpeditionItemDefinition[] ItemDefinitions =
        {
            new ExpeditionItemDefinition("I01", 4, ExpeditionItemKind.Combat),
            new ExpeditionItemDefinition("I02", 5, ExpeditionItemKind.Combat),
            new ExpeditionItemDefinition("I03", 5, ExpeditionItemKind.Combat),
            new ExpeditionItemDefinition("I04", 5, ExpeditionItemKind.Combat),
            new ExpeditionItemDefinition("I05", 5, ExpeditionItemKind.Combat),
            new ExpeditionItemDefinition("I06", 8, ExpeditionItemKind.Immediate),
            new ExpeditionItemDefinition("I07", 3, ExpeditionItemKind.Immediate),
            new ExpeditionItemDefinition("I08", 4, ExpeditionItemKind.Immediate)
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
            while (selected.Count < Math.Min(Math.Max(1, count), pool.Count))
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
            uint mapSeed, string nodeId, float progress, int morale, bool rallyHornUsed = false)
        {
            var band = ProgressBand(progress);
            var pool = band == 1
                ? new List<string>(new[] { "I01", "I02", "I07", "I08" })
                : band == 2
                    ? new List<string>(new[] { "I01", "I02", "I03", "I04", "I06", "I07", "I08" })
                    : new List<string>(new[] { "I01", "I02", "I03", "I04", "I05", "I06", "I07", "I08" });
            if (band == 1 && morale < 2 && !rallyHornUsed) pool.Add("I06");
            if (morale >= 2 || rallyHornUsed) pool.Remove("I06");
            var random = new StableRandom(ChapterOneEncounterRules.Mix(mapSeed, nodeId + ":shop", EconomyVersion));
            var selected = new List<string>();
            var immediate = morale < 2 && !rallyHornUsed
                ? new[] { "I06", "I07", "I08" }
                : new[] { "I07", "I08" };

            if (band == 1)
            {
                selected.Add(random.Pick(new[] { "I01", "I02" }));
                selected.Add(random.Pick(new[] { "I07", "I08" }));
            }
            else if (band == 4)
            {
                // Late shops always contain answers for either reviewed boss family:
                // armor penetration for B1 and mitigation/cleanse for B2.
                selected.Add("I03");
                selected.Add(random.Pick(new[] { "I01", "I04" }));
                selected.Add(random.Pick(immediate));
            }
            else
            {
                selected.Add(random.Pick(new[] { "I01", "I02", "I03", "I04", "I05" }));
                selected.Add(random.Pick(immediate));
            }
            while (selected.Count < 4 && pool.Count > 0)
            {
                var id = pool[random.Next(pool.Count)];
                pool.Remove(id);
                if (!selected.Contains(id)) selected.Add(id);
            }
            return selected.ToArray();
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
