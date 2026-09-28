using System;

namespace SeekerAutoChess.Core
{
    /// <summary>
    /// Produces a deterministic, ordered and non-uniform vertical layout for one map layer.
    /// The strongest offsets are placed between random neighbouring islands instead of
    /// adding tiny independent noise to fixed rows, so adjacent layers do not form a grid.
    /// </summary>
    public static class ExpeditionMapColumnLayout
    {
        public const int LayoutGenerationVersion = 2;

        private static readonly float[][] HorizontalProfiles =
        {
            new[] { -62f, 28f, -24f, 64f, -46f },
            new[] { 34f, -60f, 16f, 66f, -38f },
            new[] { -18f, 64f, -58f, 30f, -44f },
            new[] { 58f, -22f, -66f, 40f, -8f }
        };

        public static float HorizontalOffset(
            uint mapSeed, int layer, int nodeCount, int nodeIndex)
        {
            if (nodeCount < 1) throw new ArgumentOutOfRangeException("nodeCount");
            if (nodeIndex < 0 || nodeIndex >= nodeCount)
                throw new ArgumentOutOfRangeException("nodeIndex");

            var state = Mix(mapSeed ^ ((uint)layer * 0xC2B2AE35u) ^
                unchecked((uint)LayoutGenerationVersion * 0x27D4EB2Fu));
            if (nodeCount == 1)
                return (NextUnit(ref state) * 2f - 1f) * 36f;

            var profile = HorizontalProfiles[Next(ref state, HorizontalProfiles.Length)];
            var mirror = Next(ref state, 2) == 0 ? 1f : -1f;
            var phase = Next(ref state, profile.Length);
            var profileIndex = (nodeIndex + phase) % profile.Length;

            // Small per-node noise stops the finite profiles from becoming noticeable;
            // the strong profile values keep the left/right stagger visibly larger than
            // the old eight-pixel jitter.
            var nodeState = Mix(state ^ ((uint)(nodeIndex + 1) * 0x165667B1u));
            var noise = (NextUnit(ref nodeState) * 2f - 1f) * 7f;
            return Math.Max(-72f, Math.Min(72f, profile[profileIndex] * mirror + noise));
        }

        public static float CenterY(
            uint mapSeed,
            int layer,
            int nodeCount,
            int nodeIndex,
            float contentHeight,
            float touchHeight,
            float edgeMargin,
            float minimumClearance)
        {
            if (nodeCount < 1) throw new ArgumentOutOfRangeException("nodeCount");
            if (nodeIndex < 0 || nodeIndex >= nodeCount)
                throw new ArgumentOutOfRangeException("nodeIndex");

            var minimumCenter = edgeMargin + touchHeight * 0.5f;
            var maximumCenter = contentHeight - edgeMargin - touchHeight * 0.5f;
            if (maximumCenter < minimumCenter) return contentHeight * 0.5f;

            var state = Mix(mapSeed ^ ((uint)layer * 0x9E3779B9u) ^
                unchecked((uint)LayoutGenerationVersion * 0x85EBCA6Bu));
            if (nodeCount == 1)
            {
                // Milestone islands remain near the middle but are not forced onto one row.
                var unit = NextUnit(ref state);
                return minimumCenter + (maximumCenter - minimumCenter) * (0.30f + unit * 0.40f);
            }

            var minimumGap = touchHeight + minimumClearance;
            var requiredSpan = minimumGap * (nodeCount - 1);
            var slack = Math.Max(0f, maximumCenter - minimumCenter - requiredSpan);

            // Reserve part of the slack for moving the whole group, then concentrate the
            // rest in one or two seeded internal gaps. This creates visible stagger while
            // preserving node order and at least the requested touch clearance.
            float internalSlack;
            switch (nodeCount)
            {
                case 2: internalSlack = Math.Min(slack, 420f); break;
                case 3: internalSlack = Math.Min(slack, 360f); break;
                case 4: internalSlack = Math.Min(slack, 280f); break;
                default: internalSlack = slack * 0.68f; break;
            }

            var groupTravel = Math.Max(0f, slack - internalSlack);
            var groupOffset = NextUnit(ref state) * groupTravel;
            var gapCount = nodeCount - 1;
            var primaryGap = Next(ref state, gapCount);
            var secondaryGap = primaryGap;
            if (gapCount > 1)
            {
                secondaryGap = Next(ref state, gapCount - 1);
                if (secondaryGap >= primaryGap) secondaryGap++;
            }
            var primaryShare = 0.60f + NextUnit(ref state) * 0.22f;
            var primaryExtra = internalSlack * primaryShare;
            var secondaryExtra = internalSlack - primaryExtra;

            var center = minimumCenter + groupOffset + nodeIndex * minimumGap;
            if (nodeIndex > primaryGap) center += primaryExtra;
            if (nodeIndex > secondaryGap) center += secondaryExtra;
            return center;
        }

        private static int Next(ref uint state, int maximum)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return maximum <= 1 ? 0 : (int)(state % (uint)maximum);
        }

        private static float NextUnit(ref uint state)
        {
            return Next(ref state, 10001) / 10000f;
        }

        private static uint Mix(uint value)
        {
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value;
        }
    }
}
