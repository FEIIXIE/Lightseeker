using System;

namespace SeekerAutoChess.Core
{
    public struct ExpeditionEnergyAllocation
    {
        public readonly int RemainingEnergy;
        public readonly bool RewardsEnabled;

        public ExpeditionEnergyAllocation(int remainingEnergy, bool rewardsEnabled)
        {
            RemainingEnergy = remainingEnergy;
            RewardsEnabled = rewardsEnabled;
        }
    }

    public static class ExpeditionEnergyRules
    {
        public const int InitialEnergy = 5;

        public static ExpeditionEnergyAllocation BeginExpedition(int currentEnergy)
        {
            var available = Math.Max(0, currentEnergy);
            if (available == 0) return new ExpeditionEnergyAllocation(0, false);
            return new ExpeditionEnergyAllocation(available - 1, true);
        }
    }
}
