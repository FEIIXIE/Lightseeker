using System;
using System.Security.Cryptography;
using System.Text;

namespace SeekerAutoChess.Core
{
    /// <summary>
    /// Client-only mirror of the approved chapter-one return-settlement curve.
    /// It is used exclusively by the competition demo, where expedition gameplay
    /// is intentionally local. Stable hashing keeps every material decision
    /// deterministic for the same run.
    /// </summary>
    public static class CompetitionDemoSettlementRules
    {
        public const string RewardTableVersion = "chapter01.return-settlement.demo.v1";
        private const int PartialDenominator = 26;

        public static ExpeditionServerSettlementPlan CreateD0Plan(
            string runId,
            bool bossCleared,
            string deepestCompletedNodeId)
        {
            var rank = bossCleared ? 26 : ProgressRank(deepestCompletedNodeId);
            if (bossCleared)
            {
                return new ExpeditionServerSettlementPlan
                {
                    DifficultyId = "D0",
                    TerminalReason = "BossCleared",
                    ProgressRank = rank,
                    TideCrystal = 50,
                    LampGlow = 2,
                    TideIron = 4,
                    MistLeather = 1,
                    StarSand = Roll(runId, "star-sand-full", 8, 100) ? 1 : 0,
                    BountyTickets = 0,
                    EquipmentQuantity = 1,
                    EquipmentRarityIds = new[] { "common" },
                    LanternQuantity = 1,
                    LanternRarityId = "common",
                    IsFirstClearLantern = true,
                    GrantsFirstClear = true,
                    UnlocksNextDifficulty = false,
                    RequiresEquipmentInstanceGeneration = false,
                    RequiresLanternInstanceGeneration = false,
                    RewardTableVersion = RewardTableVersion
                };
            }

            return new ExpeditionServerSettlementPlan
            {
                DifficultyId = "D0",
                TerminalReason = "Abandoned",
                ProgressRank = rank,
                TideCrystal = 50 * rank / PartialDenominator,
                LampGlow = Fractional(runId, "lamp-glow", 2, rank),
                TideIron = Fractional(runId, "tide-iron", 4, rank),
                MistLeather = Fractional(runId, "mist-leather", 1, rank),
                StarSand = Roll(runId, "star-sand-return", 8 * rank,
                    100 * PartialDenominator) ? 1 : 0,
                BountyTickets = 0,
                EquipmentQuantity = 0,
                EquipmentRarityIds = new string[0],
                LanternQuantity = 0,
                LanternRarityId = null,
                IsFirstClearLantern = false,
                GrantsFirstClear = false,
                UnlocksNextDifficulty = false,
                RequiresEquipmentInstanceGeneration = false,
                RequiresLanternInstanceGeneration = false,
                RewardTableVersion = RewardTableVersion
            };
        }

        public static int ProgressRank(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || nodeId.Length != 3 || nodeId[0] != 'M')
                return 0;
            int ordinal;
            if (!int.TryParse(nodeId.Substring(1), out ordinal) || ordinal <= 0) return 0;
            if (ordinal <= 2) return 1;
            if (ordinal <= 5) return 2;
            if (ordinal <= 7) return 3;
            if (ordinal <= 10) return 4;
            if (ordinal <= 12) return 5;
            if (ordinal <= 14) return 6;
            if (ordinal <= 18) return 7;
            if (ordinal == 19) return 8;
            if (ordinal <= 21) return 9;
            if (ordinal <= 23) return 10;
            if (ordinal <= 25) return 11;
            if (ordinal <= 27) return 12;
            return ordinal == 28 ? 13 : 0;
        }

        private static int Fractional(string runId, string materialId, int fullQuantity, int rank)
        {
            var numerator = Math.Max(0, fullQuantity) * Math.Max(0, rank);
            var whole = numerator / PartialDenominator;
            var remainder = numerator % PartialDenominator;
            return whole + (Roll(runId, materialId, remainder, PartialDenominator) ? 1 : 0);
        }

        private static bool Roll(string runId, string domain, int numerator, int denominator)
        {
            if (numerator <= 0 || denominator <= 0) return false;
            if (numerator >= denominator) return true;
            var canonical = string.Join("|",
                runId ?? string.Empty, RewardTableVersion, domain, "local-demo");
            var digest = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(canonical));
            var sample = BitConverter.ToUInt32(digest, 0);
            return (ulong)sample * (ulong)denominator <
                   (ulong)numerator * ((ulong)uint.MaxValue + 1UL);
        }
    }
}
