using System;

namespace SeekerAutoChess.Core
{
    public enum FormalExpeditionEntryAction
    {
        RecoverActiveRun = 0,
        BeginFreeD0 = 1,
        BeginPaidDifficulty = 2
    }

    public sealed class FormalExpeditionEntryPlan
    {
        internal FormalExpeditionEntryPlan(
            FormalExpeditionEntryAction action,
            string seasonId,
            string difficultyId,
            string activeRunId,
            ExpeditionLoadoutSnapshotHashes snapshotHashes)
        {
            Action = action;
            SeasonId = seasonId;
            DifficultyId = difficultyId;
            ActiveRunId = activeRunId;
            SnapshotHashes = snapshotHashes;
        }

        public FormalExpeditionEntryAction Action { get; private set; }
        public string SeasonId { get; private set; }
        public string DifficultyId { get; private set; }
        public string ActiveRunId { get; private set; }
        public ExpeditionLoadoutSnapshotHashes SnapshotHashes { get; private set; }
    }

    public static class FormalExpeditionEntryPreparationContract
    {
        public static FormalExpeditionEntryPlan Prepare(
            ExpeditionServerSelection selection,
            string expectedSeasonId,
            string requestedDifficultyId,
            ExpeditionLoadoutSnapshotHashes snapshotHashes)
        {
            if (selection == null) throw new ArgumentNullException(nameof(selection));
            RequireExact(selection.ChapterId, ExpeditionEntryCoordinator.ChapterId, "chapter");
            RequireExact(selection.SeasonId, expectedSeasonId, "season");
            ValidateUtcWindow(selection);
            ValidateQuota(selection);

            if (!string.IsNullOrWhiteSpace(selection.ActiveRunId))
            {
                return new FormalExpeditionEntryPlan(
                    FormalExpeditionEntryAction.RecoverActiveRun,
                    expectedSeasonId,
                    null,
                    selection.ActiveRunId,
                    snapshotHashes);
            }

            if (snapshotHashes == null) throw new ArgumentNullException(nameof(snapshotHashes));

            if (selection.RemainingSlots <= 0)
                throw new InvalidOperationException(
                    "The formal expedition daily quota is exhausted.");
            if (string.IsNullOrWhiteSpace(requestedDifficultyId))
                throw new ArgumentException(
                    "A formal expedition difficulty is required.",
                    nameof(requestedDifficultyId));

            var selected = FindRequestedDifficulty(
                selection.Difficulties, requestedDifficultyId);
            ValidateSelectedDifficulty(selected, requestedDifficultyId == "D0");
            if (!selected.IsUnlocked || !selected.CanCreateIntent)
                throw new InvalidOperationException(
                    "The requested formal expedition difficulty is not available.");

            return new FormalExpeditionEntryPlan(
                requestedDifficultyId == "D0"
                    ? FormalExpeditionEntryAction.BeginFreeD0
                    : FormalExpeditionEntryAction.BeginPaidDifficulty,
                expectedSeasonId,
                requestedDifficultyId,
                null,
                snapshotHashes);
        }

        private static void ValidateQuota(ExpeditionServerSelection selection)
        {
            if (selection.DailyLimit != ChapterOneDifficultyRules.DailyFormalRunLimit ||
                selection.UsedSlots < 0 ||
                selection.UsedSlots > selection.DailyLimit ||
                selection.RemainingSlots != selection.DailyLimit - selection.UsedSlots)
                throw new InvalidOperationException(
                    "The server returned an invalid formal expedition quota.");
        }

        private static void ValidateUtcWindow(ExpeditionServerSelection selection)
        {
            RequireExact(selection.AuthoritativeTimeZone,
                ChapterOneDifficultyRules.AuthoritativeTimeZone, "authoritative time zone");
            RequireExact(selection.TimeRulesVersion,
                ChapterOneDifficultyRules.TimeRulesVersion, "time rules version");
            DateTimeOffset start;
            DateTimeOffset end;
            DateTimeOffset close;
            if (!DateTimeOffset.TryParse(selection.QuotaDayStartAtUtc, out start) ||
                !DateTimeOffset.TryParse(selection.QuotaDayEndAtUtc, out end) ||
                !DateTimeOffset.TryParse(selection.FormalCreationCloseAtUtc, out close) ||
                start.Offset != TimeSpan.Zero || end.Offset != TimeSpan.Zero ||
                close.Offset != TimeSpan.Zero || start >= close || close >= end ||
                end - start != TimeSpan.FromDays(1) ||
                !string.Equals(selection.QuotaDayId,
                    start.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) + "_UTC",
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The server returned an invalid UTC expedition window.");
        }

        private static ExpeditionServerDifficulty FindRequestedDifficulty(
            ExpeditionServerDifficulty[] source, string requestedDifficultyId)
        {
            if (source == null || source.Length == 0)
                throw new InvalidOperationException(
                    "The server selection does not contain any difficulties.");

            ExpeditionServerDifficulty selected = null;
            foreach (var item in source)
            {
                if (item == null || !string.Equals(item.DifficultyId,
                        requestedDifficultyId, StringComparison.Ordinal)) continue;
                if (selected != null)
                    throw new InvalidOperationException(
                        "The server returned a duplicate requested difficulty.");
                selected = item;
            }
            if (selected == null)
                throw new InvalidOperationException(
                    "The server selection does not contain the requested difficulty.");
            return selected;
        }

        private static void ValidateSelectedDifficulty(
            ExpeditionServerDifficulty item, bool fixedFreeD0Entry)
        {
            if (fixedFreeD0Entry)
            {
                if (item.EntryPriceMicroSkr != 0)
                    throw new InvalidOperationException(
                        "The fixed D0 entry returned a non-zero price.");
                return;
            }
            var expected = ChapterOneDifficultyRules.Get(item.DifficultyId);
            var expectedPrice = checked(
                (long)expected.EntryPriceSkr * MarketRules.MicroSkrPerSkr);
            if (!string.Equals(item.DisplayNameKey, expected.DisplayNameKey,
                    StringComparison.Ordinal) ||
                item.EntryPriceMicroSkr != expectedPrice ||
                !OptionalDifficultyIdEquals(item.RequiresBossClearDifficultyId,
                    expected.RequiredBossClearDifficultyId))
                throw new InvalidOperationException(
                    "The selected server difficulty contract does not match the client rules: " +
                    item.DifficultyId + ".");
        }

        private static bool OptionalDifficultyIdEquals(string left, string right)
        {
            var normalizedLeft = string.IsNullOrWhiteSpace(left) ? null : left;
            var normalizedRight = string.IsNullOrWhiteSpace(right) ? null : right;
            return string.Equals(normalizedLeft, normalizedRight, StringComparison.Ordinal);
        }

        private static void RequireExact(string actual, string expected, string field)
        {
            if (string.IsNullOrWhiteSpace(expected) ||
                !string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The formal expedition " + field + " does not match the client contract.");
        }
    }
}
