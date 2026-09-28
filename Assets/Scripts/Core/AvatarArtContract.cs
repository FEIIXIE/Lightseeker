using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    /// <summary>
    /// Shared UI-avatar handoff. Pages must reuse these assets and the same layer order:
    /// slot surface, avatar, then the foreground frame and status information.
    /// </summary>
    public sealed class AvatarArtEntry
    {
        public string DefinitionId { get; }
        public string ResourceId { get; }

        public AvatarArtEntry(string definitionId, string resourceId)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                throw new ArgumentException("Definition id is required.", nameof(definitionId));
            if (string.IsNullOrWhiteSpace(resourceId))
                throw new ArgumentException("Resource id is required.", nameof(resourceId));
            DefinitionId = definitionId;
            ResourceId = resourceId;
        }
    }

    public static class AvatarArtContract
    {
        public const int CanvasWidth = 1024;
        public const int CanvasHeight = 1024;
        public const float PivotX = 0.5f;
        public const float PivotY = 0.5f;
        public const float SlotSafeInsetRatio = 10f / 104f;
        public const string ResourcesFolder = "Art/Avatars/";
        public const string SlotResourcesFolder = "Art/Avatars/SlotCircular/";
        public const string SlotResourceSuffix = "-SLOT-CIRCLE-01";
        public const string OccupiedSlotSurfaceSemanticKey = "surface.avatar-slot.occupied.104";
        public const string SlotForegroundRimSemanticKey = "frame.avatar-slot.foreground.104";
        public const string OccupiedSlotSurfaceResourceId =
            "Slots/ART-UI-PREPARATION-SLOT-OCCUPIED-SURFACE-104-V6.7.4";
        public const string SlotForegroundRimResourceId =
            "Slots/ART-UI-PREPARATION-SLOT-FOREGROUND-RIM-104-V6.7.4";
        public const string RequiredLayerOrder = "SlotSurface -> Avatar -> ForegroundFrameAndInfo";

        private static readonly AvatarArtEntry[] entries =
        {
            Entry("unit.guardian", "ART-AVATAR-C01-LANTERN-SHIELD-GUARD-COMMON-03"),
            Entry("unit.healer", "ART-AVATAR-C02-LANTERN-BEARER-COMMON-03"),
            Entry("unit.thornGuard", "ART-AVATAR-C03-PATH-HUNTER-COMMON-03"),
            Entry("unit.archer", "ART-AVATAR-C04-MOSS-HERBALIST-COMMON-05"),
            Entry("unit.starblade", "ART-AVATAR-C05-ROCK-HAMMER-GUARD-COMMON-03"),
            Entry("unit.starpriest", "ART-AVATAR-C06-EMBER-THROWER-COMMON-03")
        };

        private static readonly Dictionary<string, AvatarArtEntry> byDefinitionId = BuildIndex();

        public static IReadOnlyList<AvatarArtEntry> Entries { get { return entries; } }

        public static bool TryGet(string definitionId, out AvatarArtEntry entry)
        {
            entry = null;
            return !string.IsNullOrEmpty(definitionId) && byDefinitionId.TryGetValue(definitionId, out entry);
        }

        private static AvatarArtEntry Entry(string definitionId, string resourceId)
        {
            return new AvatarArtEntry(definitionId, resourceId);
        }

        private static Dictionary<string, AvatarArtEntry> BuildIndex()
        {
            var result = new Dictionary<string, AvatarArtEntry>(StringComparer.Ordinal);
            foreach (var entry in entries) result.Add(entry.DefinitionId, entry);
            return result;
        }
    }
}
