using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public sealed class ExpeditionMapVisualSelection
    {
        private readonly int[] anchorIndices;
        private readonly int[] decorationSizeOffsets;

        internal ExpeditionMapVisualSelection(
            int islandTemplateIndex,
            bool mirrorX,
            int decorationProfileIndex,
            int[] selectedAnchorIndices,
            int[] selectedDecorationSizeOffsets)
        {
            IslandTemplateIndex = islandTemplateIndex;
            MirrorX = mirrorX;
            DecorationProfileIndex = decorationProfileIndex;
            anchorIndices = selectedAnchorIndices;
            decorationSizeOffsets = selectedDecorationSizeOffsets;
        }

        public int VisualGenerationVersion { get { return ExpeditionMapVisualSelector.VisualGenerationVersion; } }
        public int IslandTemplateIndex { get; private set; }
        public bool MirrorX { get; private set; }
        public int DecorationProfileIndex { get; private set; }
        public IReadOnlyList<int> AnchorIndices { get { return anchorIndices; } }
        public IReadOnlyList<int> DecorationSizeOffsets { get { return decorationSizeOffsets; } }
    }

    /// <summary>
    /// Selects neutral island visuals from map seed and node id only. Encounter type is
    /// deliberately absent so changing a node type cannot change its landform or decor.
    /// </summary>
    public static class ExpeditionMapVisualSelector
    {
        public const int VisualGenerationVersion = 1;
        public const int IslandTemplateCount = 15;
        public const int DecorationProfileCount = 6;

        public static ExpeditionMapVisualSelection Create(uint mapSeed, string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) throw new ArgumentException("Node id is required.", "nodeId");
            var state = Mix(mapSeed ^ StableHash(nodeId) ^
                (uint)VisualGenerationVersion * 0x9E3779B9u);
            var islandTemplateIndex = Next(ref state, IslandTemplateCount);
            var mirrorX = Next(ref state, 2) == 1;
            var decorationProfileIndex = Next(ref state, DecorationProfileCount);
            var anchorOrder = new[] { 0, 1, 2, 3 };
            Shuffle(anchorOrder, ref state);
            var decorationCount = Next(ref state, 100) < 28 ? 2 : 3;
            var selectedAnchors = new int[decorationCount];
            var sizeOffsets = new int[decorationCount];
            for (var index = 0; index < decorationCount; index++)
            {
                selectedAnchors[index] = anchorOrder[index];
                sizeOffsets[index] = Next(ref state, 3) - 1;
            }
            return new ExpeditionMapVisualSelection(
                islandTemplateIndex,
                mirrorX,
                decorationProfileIndex,
                selectedAnchors,
                sizeOffsets);
        }

        private static uint StableHash(string value)
        {
            var hash = 2166136261u;
            for (var index = 0; index < value.Length; index++)
            {
                hash ^= value[index];
                hash *= 16777619u;
            }
            return hash;
        }

        private static int Next(ref uint state, int maximum)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return maximum <= 1 ? 0 : (int)(state % (uint)maximum);
        }

        private static void Shuffle(int[] values, ref uint state)
        {
            for (var index = values.Length - 1; index > 0; index--)
            {
                var other = Next(ref state, index + 1);
                var temporary = values[index];
                values[index] = values[other];
                values[other] = temporary;
            }
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
