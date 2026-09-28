using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    /// <summary>
    /// Rules shared by the Unity prototype and the future authoritative server.
    /// Run levels are intentionally separate from external stars and quality.
    /// </summary>
    public static class RunUnitRules
    {
        public const int CopiesPerMerge = 3;
        public const int MaximumRunLevel = 3;
        public const int BasisPoints = 10000;

        private static readonly int[] HealthMultiplierBasisPoints = { 10000, 16500, 25500 };
        private static readonly int[] ActionMultiplierBasisPoints = { 10000, 15500, 23500 };

        public static int GetHealthMultiplierBasisPoints(int runLevel)
        {
            ValidateRunLevel(runLevel);
            return HealthMultiplierBasisPoints[runLevel - 1];
        }

        public static int GetActionMultiplierBasisPoints(int runLevel)
        {
            ValidateRunLevel(runLevel);
            return ActionMultiplierBasisPoints[runLevel - 1];
        }

        public static int ScaleHealth(int value, int runLevel)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            var scaled = (long)value * GetHealthMultiplierBasisPoints(runLevel);
            return checked((int)((scaled + BasisPoints - 1) / BasisPoints));
        }

        public static int ScaleAction(int value, int runLevel)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            var scaled = (long)value * GetActionMultiplierBasisPoints(runLevel);
            return checked((int)((scaled + BasisPoints / 2) / BasisPoints));
        }

        private static void ValidateRunLevel(int runLevel)
        {
            if (runLevel < 1 || runLevel > MaximumRunLevel)
                throw new ArgumentOutOfRangeException(nameof(runLevel));
        }

        public static int[] FindTriple(
            IReadOnlyList<string> definitionIds,
            IReadOnlyList<int> runLevels,
            string definitionId,
            int runLevel,
            int preferredIndex)
        {
            if (definitionIds == null) throw new ArgumentNullException(nameof(definitionIds));
            if (runLevels == null) throw new ArgumentNullException(nameof(runLevels));
            if (definitionIds.Count != runLevels.Count)
            {
                throw new ArgumentException("Definition and level collections must have equal length.");
            }
            if (string.IsNullOrEmpty(definitionId) || runLevel < 1 || runLevel >= MaximumRunLevel)
            {
                return Array.Empty<int>();
            }

            var matches = new List<int>(CopiesPerMerge);
            if (preferredIndex >= 0 && preferredIndex < definitionIds.Count &&
                definitionIds[preferredIndex] == definitionId && runLevels[preferredIndex] == runLevel)
            {
                matches.Add(preferredIndex);
            }

            for (var i = 0; i < definitionIds.Count && matches.Count < CopiesPerMerge; i++)
            {
                if (i == preferredIndex) continue;
                if (definitionIds[i] == definitionId && runLevels[i] == runLevel) matches.Add(i);
            }
            return matches.Count == CopiesPerMerge ? matches.ToArray() : Array.Empty<int>();
        }
    }
}
