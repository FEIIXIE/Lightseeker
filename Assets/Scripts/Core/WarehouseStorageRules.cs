using System;

namespace SeekerAutoChess.Core
{
    public static class WarehouseStorageRules
    {
        public const int InitialCapacity = 50;

        public static int CountUsedSlots(
            int equipmentCount,
            int stoneCount,
            int lampOilCount,
            int mistLensCount)
        {
            if (equipmentCount < 0 || stoneCount < 0 || lampOilCount < 0 || mistLensCount < 0)
                throw new ArgumentOutOfRangeException(nameof(equipmentCount));
            return equipmentCount +
                   (stoneCount > 0 ? 1 : 0) +
                   (lampOilCount > 0 ? 1 : 0) +
                   (mistLensCount > 0 ? 1 : 0);
        }

        public static bool CanAccept(
            int equipmentCount,
            int stoneCount,
            int lampOilCount,
            int mistLensCount,
            int incomingEquipmentCount,
            int incomingStone,
            int incomingLampOil,
            int incomingMistLens,
            int capacity = InitialCapacity)
        {
            if (incomingEquipmentCount < 0 || incomingStone < 0 || incomingLampOil < 0 ||
                incomingMistLens < 0 || capacity < 0)
                return false;
            var current = CountUsedSlots(equipmentCount, stoneCount, lampOilCount, mistLensCount);
            var added = incomingEquipmentCount +
                        (stoneCount == 0 && incomingStone > 0 ? 1 : 0) +
                        (lampOilCount == 0 && incomingLampOil > 0 ? 1 : 0) +
                        (mistLensCount == 0 && incomingMistLens > 0 ? 1 : 0);
            return current + added <= capacity;
        }
    }
}
