using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public static class WarehouseStorageRules
    {
        public const int InitialCapacity = 50;

        public static int CountUsedSlots(int equipmentCount, int lanternCount = 0,
            int cosmeticCount = 0)
        {
            if (equipmentCount < 0) throw new ArgumentOutOfRangeException(nameof(equipmentCount));
            if (lanternCount < 0) throw new ArgumentOutOfRangeException(nameof(lanternCount));
            if (cosmeticCount < 0) throw new ArgumentOutOfRangeException(nameof(cosmeticCount));
            return equipmentCount + lanternCount + cosmeticCount;
        }

        public static bool CanAccept(
            int equipmentCount,
            int incomingEquipmentCount,
            int lanternCount = 0,
            int capacity = InitialCapacity)
        {
            if (incomingEquipmentCount < 0 || capacity < 0)
                return false;
            return CountUsedSlots(equipmentCount, lanternCount) + incomingEquipmentCount <= capacity;
        }
    }

    public enum WarehouseSortMode
    {
        Time = 0,
        Type = 1,
        Name = 2
    }

    public sealed class WarehouseSortEntry
    {
        public string Token { get; }
        public int AcquisitionOrder { get; }
        public int KindOrder { get; }
        public int TypeOrder { get; }
        public string Name { get; }

        public WarehouseSortEntry(string token, int acquisitionOrder, int kindOrder,
            int typeOrder, string name)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            AcquisitionOrder = acquisitionOrder;
            KindOrder = kindOrder;
            TypeOrder = typeOrder;
            Name = name ?? string.Empty;
        }
    }

    public static class WarehouseLayoutRules
    {
        public static List<int> VisibleSlotIndices(string[] slots, Predicate<string> includeToken)
        {
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            var result = new List<int>(slots.Length);
            for (var index = 0; index < slots.Length; index++)
            {
                var token = slots[index];
                if (string.IsNullOrEmpty(token) ||
                    (includeToken != null && !includeToken(token))) continue;
                result.Add(index);
            }
            return result;
        }

        public static bool TryReorderVisible(string[] slots, Predicate<string> includeToken,
            int sourceDisplayIndex, int destinationDisplayIndex)
        {
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            var visible = VisibleSlotIndices(slots, includeToken);
            if (sourceDisplayIndex < 0 || sourceDisplayIndex >= visible.Count ||
                destinationDisplayIndex < 0 || visible.Count == 0) return false;
            destinationDisplayIndex = Math.Min(destinationDisplayIndex, visible.Count - 1);
            if (sourceDisplayIndex == destinationDisplayIndex) return false;

            var ordered = new List<string>(visible.Count);
            for (var index = 0; index < visible.Count; index++)
                ordered.Add(slots[visible[index]]);
            var moved = ordered[sourceDisplayIndex];
            ordered.RemoveAt(sourceDisplayIndex);
            ordered.Insert(destinationDisplayIndex, moved);
            for (var index = 0; index < visible.Count; index++)
                slots[visible[index]] = ordered[index];
            return true;
        }

        public static bool TryMove(string[] slots, int sourceIndex, int destinationIndex)
        {
            if (slots == null || sourceIndex < 0 || destinationIndex < 0 ||
                sourceIndex >= slots.Length || destinationIndex >= slots.Length ||
                sourceIndex == destinationIndex || string.IsNullOrEmpty(slots[sourceIndex]))
                return false;
            var source = slots[sourceIndex];
            slots[sourceIndex] = slots[destinationIndex];
            slots[destinationIndex] = source;
            return true;
        }

        public static bool Organize(string[] slots)
        {
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            var compact = new List<string>(slots.Length);
            for (var index = 0; index < slots.Length; index++)
                if (!string.IsNullOrEmpty(slots[index])) compact.Add(slots[index]);
            var changed = false;
            for (var index = 0; index < slots.Length; index++)
            {
                var next = index < compact.Count ? compact[index] : null;
                if (!string.Equals(slots[index], next, StringComparison.Ordinal)) changed = true;
                slots[index] = next;
            }
            return changed;
        }

        public static bool Sort(string[] slots, IEnumerable<WarehouseSortEntry> entries,
            WarehouseSortMode mode)
        {
            return Sort(slots, entries, mode, null);
        }

        public static bool Sort(string[] slots, IEnumerable<WarehouseSortEntry> entries,
            WarehouseSortMode mode, Predicate<string> includeToken)
        {
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            var byToken = new Dictionary<string, WarehouseSortEntry>(StringComparer.Ordinal);
            foreach (var entry in entries)
                if (entry != null && !byToken.ContainsKey(entry.Token)) byToken.Add(entry.Token, entry);
            var ordered = new List<WarehouseSortEntry>();
            var targetIndices = new List<int>();
            for (var index = 0; index < slots.Length; index++)
            {
                WarehouseSortEntry entry;
                var token = slots[index];
                if (string.IsNullOrEmpty(token) ||
                    (includeToken != null && !includeToken(token)) ||
                    !byToken.TryGetValue(token, out entry)) continue;
                targetIndices.Add(index);
                ordered.Add(entry);
            }
            ordered.Sort((left, right) => Compare(left, right, mode));
            var changed = false;
            for (var index = 0; index < targetIndices.Count; index++)
            {
                var slotIndex = targetIndices[index];
                var next = ordered[index].Token;
                if (!string.Equals(slots[slotIndex], next, StringComparison.Ordinal)) changed = true;
                slots[slotIndex] = next;
            }
            return changed;
        }

        public static bool Reconcile(string[] slots, IEnumerable<string> currentTokens)
        {
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            if (currentTokens == null) throw new ArgumentNullException(nameof(currentTokens));
            var current = new HashSet<string>(StringComparer.Ordinal);
            var orderedCurrent = new List<string>();
            foreach (var token in currentTokens)
            {
                if (string.IsNullOrEmpty(token) || !current.Add(token)) continue;
                orderedCurrent.Add(token);
            }
            var occupied = new HashSet<string>(StringComparer.Ordinal);
            var changed = false;
            for (var index = 0; index < slots.Length; index++)
            {
                var token = slots[index];
                if (!string.IsNullOrEmpty(token) && current.Contains(token) && occupied.Add(token)) continue;
                if (!string.IsNullOrEmpty(token)) changed = true;
                slots[index] = null;
            }
            for (var currentIndex = 0; currentIndex < orderedCurrent.Count; currentIndex++)
            {
                var token = orderedCurrent[currentIndex];
                if (occupied.Contains(token)) continue;
                for (var slotIndex = 0; slotIndex < slots.Length; slotIndex++)
                {
                    if (!string.IsNullOrEmpty(slots[slotIndex])) continue;
                    slots[slotIndex] = token;
                    occupied.Add(token);
                    changed = true;
                    break;
                }
            }
            return changed;
        }

        private static int Compare(WarehouseSortEntry left, WarehouseSortEntry right,
            WarehouseSortMode mode)
        {
            int result;
            switch (mode)
            {
                case WarehouseSortMode.Type:
                    result = left.KindOrder.CompareTo(right.KindOrder);
                    if (result == 0) result = left.TypeOrder.CompareTo(right.TypeOrder);
                    if (result == 0) result = StringComparer.Ordinal.Compare(left.Name, right.Name);
                    break;
                case WarehouseSortMode.Name:
                    result = StringComparer.Ordinal.Compare(left.Name, right.Name);
                    if (result == 0) result = left.KindOrder.CompareTo(right.KindOrder);
                    break;
                default:
                    result = left.AcquisitionOrder.CompareTo(right.AcquisitionOrder);
                    break;
            }
            return result != 0 ? result : StringComparer.Ordinal.Compare(left.Token, right.Token);
        }
    }
}
