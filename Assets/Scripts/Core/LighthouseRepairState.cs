using System;

namespace SeekerAutoChess.Core
{
    /// <summary>
    /// Local M2 contract for the first lighthouse. The final online version will
    /// perform the same validation and consumption through an authoritative service.
    /// </summary>
    public sealed class LighthouseRepairState
    {
        public const int RequiredStone = 60;
        public const int RequiredLampOil = 12;
        public const int RequiredMistLens = 4;

        public bool BossDefeated { get; private set; }
        public bool IsRepaired { get; private set; }

        public void RecordBossVictory()
        {
            BossDefeated = true;
        }

        public bool CanRepair(int stone, int lampOil, int mistLens)
        {
            return !IsRepaired && BossDefeated &&
                   stone >= RequiredStone &&
                   lampOil >= RequiredLampOil &&
                   mistLens >= RequiredMistLens;
        }

        public bool TryRepair(ref int stone, ref int lampOil, ref int mistLens)
        {
            if (!CanRepair(stone, lampOil, mistLens)) return false;
            stone -= RequiredStone;
            lampOil -= RequiredLampOil;
            mistLens -= RequiredMistLens;
            IsRepaired = true;
            return true;
        }

        public void Restore(bool bossDefeated, bool isRepaired)
        {
            BossDefeated = bossDefeated || isRepaired;
            IsRepaired = isRepaired;
        }
    }
}
