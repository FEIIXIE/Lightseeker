using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public sealed class ChapterOneMapNodeRule
    {
        public ChapterOneMapNodeRule(string id, int anchorIndex, int stage, float journeyProgress)
        {
            Id = id;
            AnchorIndex = anchorIndex;
            Stage = stage;
            JourneyProgress = journeyProgress;
        }

        public string Id { get; private set; }
        public int AnchorIndex { get; private set; }
        public int Stage { get; private set; }
        public float JourneyProgress { get; private set; }
    }

    /// <summary>
    /// Data-only chapter-one rules. Visual coordinates remain authoritative in the
    /// artist-authored manual-label-anchors JSON; this config only describes gameplay
    /// progression, special nodes and seeded content quotas.
    /// </summary>
    public sealed class ChapterOneMapRules
    {
        // Generated from outputs/第一章30锚点地图合并机器配置-v0.3.json.
        // Keep the shipped JSON beside Resources/Config/Chapter01 as the machine authority;
        // golden seed tests prevent this A-D projection from drifting across languages.
        private static readonly string[][] ReviewedSkeletons =
        {
            new[]
            {
                "M00>M01", "M00>M02", "M01>M03", "M01>M04", "M02>M04", "M02>M05",
                "M03>M06", "M04>M06", "M04>M07", "M05>M07", "M06>M08", "M06>M09",
                "M07>M09", "M07>M10", "M08>M11", "M09>M11", "M09>M12", "M10>M12",
                "M11>M13", "M11>M14", "M12>M14", "M13>M15", "M13>M16", "M14>M16",
                "M14>M17", "M15>M18", "M16>M18", "M17>M18", "M18>M19", "M19>M20",
                "M19>M21", "M20>M22", "M20>M23", "M21>M23", "M22>M24", "M22>M25",
                "M23>M24", "M24>M27", "M25>M26", "M25>M27", "M26>M28", "M27>M28",
                "M28>M29"
            },
            new[]
            {
                "M00>M01", "M00>M02", "M01>M03", "M01>M04", "M02>M04", "M02>M05",
                "M03>M06", "M04>M06", "M04>M07", "M05>M07", "M06>M08", "M06>M09",
                "M07>M09", "M07>M10", "M08>M11", "M09>M11", "M09>M12", "M10>M12",
                "M11>M13", "M11>M14", "M12>M14", "M13>M15", "M13>M16", "M14>M16",
                "M14>M17", "M15>M18", "M16>M18", "M17>M18", "M18>M19", "M19>M20",
                "M19>M21", "M20>M22", "M20>M23", "M21>M23", "M22>M24", "M22>M25",
                "M23>M24", "M24>M27", "M25>M26", "M25>M27", "M26>M28", "M27>M28",
                "M28>M29", "M01>M06", "M02>M07", "M06>M11", "M07>M12"
            },
            new[]
            {
                "M00>M01", "M00>M02", "M01>M03", "M01>M04", "M02>M04", "M02>M05",
                "M03>M06", "M04>M06", "M04>M07", "M05>M07", "M06>M08", "M06>M09",
                "M07>M09", "M07>M10", "M08>M11", "M09>M11", "M09>M12", "M10>M12",
                "M11>M13", "M11>M14", "M12>M14", "M13>M15", "M13>M16", "M14>M16",
                "M14>M17", "M15>M18", "M16>M18", "M17>M18", "M18>M19", "M19>M20",
                "M19>M21", "M20>M22", "M20>M23", "M21>M23", "M22>M24", "M22>M25",
                "M23>M24", "M24>M27", "M25>M26", "M25>M27", "M26>M28", "M27>M28",
                "M28>M29", "M03>M04", "M08>M09", "M09>M10"
            },
            new[]
            {
                "M00>M01", "M00>M02", "M01>M03", "M01>M04", "M02>M04", "M02>M05",
                "M03>M06", "M04>M06", "M04>M07", "M05>M07", "M06>M08", "M06>M09",
                "M07>M09", "M07>M10", "M08>M11", "M09>M11", "M09>M12", "M10>M12",
                "M11>M13", "M12>M13", "M12>M14", "M13>M15", "M13>M16", "M14>M16",
                "M14>M17", "M15>M18", "M16>M18", "M17>M18", "M18>M19", "M19>M20",
                "M19>M21", "M20>M22", "M21>M22", "M21>M23", "M22>M24", "M22>M25",
                "M23>M24", "M24>M27", "M25>M26", "M25>M27", "M26>M28", "M27>M28",
                "M28>M29", "M11>M12", "M20>M21"
            }
        };

        private readonly List<ChapterOneMapNodeRule> nodes;
        private readonly HashSet<int> mandatoryBattleStages;

        private ChapterOneMapRules(
            List<ChapterOneMapNodeRule> nodeRules,
            IEnumerable<int> battleStages)
        {
            nodes = nodeRules;
            mandatoryBattleStages = new HashSet<int>(battleStages);
        }

        public IReadOnlyList<ChapterOneMapNodeRule> Nodes { get { return nodes; } }
        public string StartNodeId { get { return "M00"; } }
        public string GuardianNodeId { get { return "M19"; } }
        public string BossNodeId { get { return "M29"; } }
        public int StageCount { get { return 15; } }
        public int NormalBattleCount { get { return 14; } }
        public int OptionalEliteCount { get { return 3; } }
        public int SupplyCount { get { return 0; } }
        public int EventCount { get { return 2; } }
        public int ShopCount { get { return 2; } }
        public int RestCount { get { return 2; } }
        public int TreasureCount { get { return 4; } }
        public int MaximumContentAttempts { get { return 2048; } }

        public string SkeletonId(uint seed)
        {
            return ((char)('A' + SkeletonIndex(seed))).ToString();
        }

        public IReadOnlyList<string> SkeletonEdges(uint seed)
        {
            return ReviewedSkeletons[SkeletonIndex(seed)];
        }

        private static int SkeletonIndex(uint seed)
        {
            return (int)unchecked((seed - 1u) % (uint)ReviewedSkeletons.Length);
        }

        public bool IsMandatoryBattleStage(int stage)
        {
            return mandatoryBattleStages.Contains(stage);
        }

        public static ChapterOneMapRules CreateDefault()
        {
            // IDs and stage grouping mirror the 30 approved manual anchors. The
            // coordinates themselves are deliberately not duplicated here.
            var stages = new[]
            {
                1, 2, 2, 3, 3, 3, 4, 4, 5, 5,
                5, 6, 6, 7, 7, 8, 8, 8, 8, 9,
                10, 10, 11, 11, 12, 12, 13, 13, 14, 15
            };
            var progress = new[]
            {
                0.000f, 6.267f, 6.794f, 12.951f, 13.062f, 14.920f,
                19.412f, 19.412f, 25.347f, 27.399f, 31.226f, 33.805f,
                35.996f, 41.986f, 42.457f, 47.615f, 48.558f, 51.747f,
                57.765f, 62.285f, 69.107f, 72.213f, 75.874f, 77.371f,
                83.500f, 84.526f, 91.791f, 92.374f, 96.339f, 100.000f
            };
            var result = new List<ChapterOneMapNodeRule>(stages.Length);
            for (var index = 0; index < stages.Length; index++)
            {
                result.Add(new ChapterOneMapNodeRule(
                    "M" + index.ToString("00"), index, stages[index], progress[index]));
            }
            return new ChapterOneMapRules(result, new int[0]);
        }
    }
}
