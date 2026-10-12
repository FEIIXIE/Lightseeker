using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public sealed class ChapterOneEventEligibility
    {
        public bool HasLaterBattle;
        public bool NextBattleHasBaseGold;
        public bool NextBattleIsBoss;
        public bool HasBackpackItem;
        public bool HasPendingP05;
        public bool HasPendingP06;
        public bool HasNaturalRecruitRefresh;
        public bool HasRecruitProfessionTarget;
        public bool HasRecruitFactionTarget;
        public bool HasEligibleNormalEnhancement;
        public bool ExtraGrowthAvailable;
        public int Coins;
    }

    /// <summary>
    /// Product-approved chapter-one event pool v1.5. E06 is intentionally absent:
    /// covenant-dependent lantern rules are disabled for the competition build.
    /// </summary>
    public static class ChapterOneExpeditionEventRules
    {
        public const string RulesVersion = "chapter01.events.v1.5";

        public static string Generate(
            uint mapSeed,
            string runId,
            string nodeId,
            int authoritativeStateVersion,
            ChapterOneEventEligibility eligibility,
            Func<string, bool> wasSeen)
        {
            if (eligibility == null) throw new ArgumentNullException(nameof(eligibility));
            var legal = LegalTemplates(eligibility);
            if (legal.Count == 0) return string.Empty;
            var unseen = new List<string>();
            foreach (var id in legal)
                if (wasSeen == null || !wasSeen(id)) unseen.Add(id);
            var candidates = unseen.Count > 0 ? unseen : legal;
            var seedText = (runId ?? string.Empty) + ":" + (nodeId ?? string.Empty) + ":" +
                           RulesVersion + ":" + authoritativeStateVersion;
            var mixed = ChapterOneEncounterRules.Mix(mapSeed, seedText, 15);
            return candidates[(int)(mixed % (uint)candidates.Count)];
        }

        public static IReadOnlyList<string> LegalTemplates(ChapterOneEventEligibility eligibility)
        {
            if (eligibility == null) throw new ArgumentNullException(nameof(eligibility));
            var result = new List<string>();
            if (eligibility.HasLaterBattle) result.Add("E01");
            if (eligibility.Coins >= 3 && eligibility.NextBattleHasBaseGold &&
                !eligibility.NextBattleIsBoss && !eligibility.HasPendingP05)
                result.Add("E02");
            if (eligibility.Coins >= 2 && eligibility.HasBackpackItem &&
                eligibility.HasLaterBattle && !eligibility.HasPendingP06)
                result.Add("E03");
            if (eligibility.Coins >= 3 && eligibility.HasNaturalRecruitRefresh &&
                eligibility.HasRecruitProfessionTarget && eligibility.HasRecruitFactionTarget)
                result.Add("E04");
            if (eligibility.Coins >= 7 && eligibility.HasEligibleNormalEnhancement &&
                eligibility.ExtraGrowthAvailable)
                result.Add("E05");
            return result;
        }

        public static int ChoiceCost(string eventId, int choiceIndex)
        {
            if (choiceIndex < 0 || choiceIndex > 2) return int.MaxValue;
            switch (eventId)
            {
                case "E02": return choiceIndex == 1 ? 3 : 0;
                case "E03": return choiceIndex == 2 ? 2 : 0;
                case "E04": return choiceIndex == 2 ? 3 : 2;
                case "E05": return choiceIndex == 0 ? 3 : choiceIndex == 1 ? 5 : 7;
                default: return eventId == "E01" ? 0 : int.MaxValue;
            }
        }

        public static string ChoiceId(string eventId, int choiceIndex)
        {
            if (string.IsNullOrEmpty(eventId) || choiceIndex < 0 || choiceIndex > 2)
                throw new ArgumentOutOfRangeException(nameof(choiceIndex));
            return eventId + ":" + (choiceIndex + 1);
        }

        public static string PendingMarker(string eventId, int choiceIndex)
        {
            return "event:pending:" + eventId + (char)('A' + choiceIndex);
        }
    }
}
