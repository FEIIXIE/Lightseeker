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
        public string QualityId { get; }
        public string ResourceId { get; }

        public AvatarArtEntry(string definitionId, string qualityId, string resourceId)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                throw new ArgumentException("Definition id is required.", nameof(definitionId));
            if (string.IsNullOrWhiteSpace(qualityId))
                throw new ArgumentException("Quality id is required.", nameof(qualityId));
            if (string.IsNullOrWhiteSpace(resourceId))
                throw new ArgumentException("Resource id is required.", nameof(resourceId));
            DefinitionId = definitionId;
            QualityId = qualityId;
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
            Entry("unit.guardian", "common", "ART-AVATAR-C01-LAMPGUARD-COMMON-v02"),
            Entry("unit.guardian", "uncommon", "ART-AVATAR-C01-LAMPGUARD-UNCOMMON-v02"),
            Entry("unit.guardian", "rare", "ART-AVATAR-C01-LAMPGUARD-RARE-v02"),
            Entry("unit.guardian", "epic", "ART-AVATAR-C01-OLIN-EPIC-BREAKWATER-TOWER-GUARD-v06"),
            Entry("unit.guardian", "legendary", "ART-AVATAR-C01-OLIN-LEGENDARY-TIDEGATE-TOWER-WARDEN-v04"),
            Entry("unit.healer", "common", "ART-AVATAR-C02-LAMPLADY-COMMON-v01"),
            Entry("unit.healer", "uncommon", "ART-AVATAR-C02-LAMPLADY-UNCOMMON-v01"),
            Entry("unit.healer", "rare", "ART-AVATAR-C02-LAMPLADY-RARE-v01"),
            Entry("unit.healer", "epic", "ART-AVATAR-C02-YIWEN-EPIC-TIDAL-LANTERN-OFFICER-v01"),
            Entry("unit.healer", "legendary", "ART-AVATAR-C02-YIWEN-LEGENDARY-RETURN-LIGHT-LANTERN-OFFICER-v02"),
            Entry("unit.thornGuard", "common", "ART-AVATAR-C03-RANGER-COMMON-v03"),
            Entry("unit.thornGuard", "uncommon", "ART-AVATAR-C03-RANGER-UNCOMMON-v03"),
            Entry("unit.thornGuard", "rare", "ART-AVATAR-C03-RANGER-RARE-v03"),
            Entry("unit.thornGuard", "epic", "ART-AVATAR-C03-LEIFU-EPIC-RETURN-ROUTE-CARTOGRAPHER-v03"),
            Entry("unit.thornGuard", "legendary", "ART-AVATAR-C03-LEIFU-LEGENDARY-LAST-WAYMARK-v03"),
            Entry("unit.archer", "common", "ART-AVATAR-C04-HERBALIST-COMMON-v01"),
            Entry("unit.archer", "uncommon", "ART-AVATAR-C04-HERBALIST-UNCOMMON-v01"),
            Entry("unit.archer", "rare", "ART-AVATAR-C04-HERBALIST-RARE-v01"),
            Entry("unit.archer", "epic", "ART-AVATAR-C04-MILA-EPIC-TIDEMARSH-VESTMENT-v01"),
            Entry("unit.archer", "legendary", "ART-AVATAR-C04-MILA-LEGENDARY-TIDEMARSH-MASTER-v01"),
            Entry("unit.starblade", "common", "ART-AVATAR-C05-STONEWARD-COMMON-v01"),
            Entry("unit.starblade", "uncommon", "ART-AVATAR-C05-STONEWARD-UNCOMMON-v01"),
            Entry("unit.starblade", "rare", "ART-AVATAR-C05-STONEWARD-RARE-v01"),
            Entry("unit.starblade", "epic", "ART-AVATAR-C05-BRUN-EPIC-FORGEGATE-MASTER-v03"),
            Entry("unit.starblade", "legendary", "ART-AVATAR-C05-BRUN-LEGENDARY-EVERBRIDGE-FORGE-LORD-v03"),
            Entry("unit.starpriest", "common", "ART-AVATAR-C06-EMBERHAND-COMMON-v01"),
            Entry("unit.starpriest", "uncommon", "ART-AVATAR-C06-EMBERHAND-UNCOMMON-v01"),
            Entry("unit.starpriest", "rare", "ART-AVATAR-C06-EMBERHAND-RARE-v01"),
            Entry("unit.starpriest", "epic", "ART-AVATAR-C06-NOCK-EPIC-RIFTSTONE-ARENA-KILNMASTER-v01"),
            Entry("unit.starpriest", "legendary", "ART-AVATAR-C06-NOCK-LEGENDARY-HUNDRED-FIRE-STEAMFORGED-v01")
        };

        private static readonly Dictionary<string, AvatarArtEntry> byDefinitionAndQuality = BuildIndex();

        public static IReadOnlyList<AvatarArtEntry> Entries { get { return entries; } }

        public static bool TryGet(string definitionId, out AvatarArtEntry entry)
        {
            return TryGet(definitionId, "common", out entry);
        }

        public static bool TryGet(string definitionId, string qualityId, out AvatarArtEntry entry)
        {
            entry = null;
            return !string.IsNullOrEmpty(definitionId) &&
                   !string.IsNullOrEmpty(qualityId) &&
                   byDefinitionAndQuality.TryGetValue(Key(definitionId, qualityId), out entry);
        }

        private static AvatarArtEntry Entry(string definitionId, string qualityId, string resourceId)
        {
            return new AvatarArtEntry(definitionId, qualityId, resourceId);
        }

        private static string Key(string definitionId, string qualityId)
        {
            return definitionId + "|" + qualityId;
        }

        private static Dictionary<string, AvatarArtEntry> BuildIndex()
        {
            var result = new Dictionary<string, AvatarArtEntry>(StringComparer.Ordinal);
            foreach (var entry in entries) result.Add(Key(entry.DefinitionId, entry.QualityId), entry);
            return result;
        }
    }
}
