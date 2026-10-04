using System;

namespace SeekerAutoChess.Core
{
    public static class WarehouseStorageRules
    {
        public const int InitialCapacity = 50;

        public static int CountUsedSlots(int equipmentCount, int lanternCount = 0)
        {
            if (equipmentCount < 0) throw new ArgumentOutOfRangeException(nameof(equipmentCount));
            if (lanternCount < 0) throw new ArgumentOutOfRangeException(nameof(lanternCount));
            return equipmentCount + lanternCount;
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
}
