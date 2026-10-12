using System;
using System.Collections.Generic;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class ExpeditionReplayRecorderTests
    {
        [Test]
        public void RecordsRestoresAndExportsAuthoritativeCheckpoint()
        {
            var binding = CreateBinding();
            var recorder = ExpeditionReplayRecorder.Start(binding);
            recorder.Record("route-1", "route.select", 0, "M00",
                new Dictionary<string, string>
                {
                    { "destinationNodeId", "M01" }
                });
            var saved = recorder.CaptureState();
            var restored = ExpeditionReplayRecorder.Restore(binding, saved);
            restored.Record("battle-1", "battle.resolve", 1, "M01",
                BattleFields("player"));

            var envelope = restored.CreateCheckpointEnvelope();

            Assert.That(envelope.RunId, Is.EqualTo(binding.Source.RunId.Replace("-", "")));
            Assert.That(envelope.AccountId,
                Is.EqualTo(binding.Source.AccountId.Replace("-", "")));
            Assert.That(envelope.Actions.Length, Is.EqualTo(2));
            Assert.That(envelope.Actions[1].Kind, Is.EqualTo("battle.resolve"));
            Assert.That(envelope.TranscriptSha256, Has.Length.EqualTo(64));
            Assert.DoesNotThrow(() => ExpeditionReplayClientContract.Validate(envelope));
        }

        [Test]
        public void RejectsTamperedRecoveryAndStateRollback()
        {
            var binding = CreateBinding();
            var recorder = ExpeditionReplayRecorder.Start(binding);
            recorder.Record("battle-1", "battle.resolve", 4, "M01",
                BattleFields("player"));
            var saved = recorder.CaptureState();
            saved.Actions[0].PayloadCanonical += "tampered";

            Assert.Throws<InvalidOperationException>(() =>
                ExpeditionReplayRecorder.Restore(binding, saved));

            var clean = ExpeditionReplayRecorder.Start(binding);
            clean.Record("route-1", "route.select", 4, "M00",
                new Dictionary<string, string>
                {
                    { "destinationNodeId", "M01" }
                });
            Assert.Throws<InvalidOperationException>(() => clean.Record(
                "battle-1", "battle.resolve", 3, "M01", BattleFields("player")));
        }

        [Test]
        public void DoesNotExportAnOpenNonBattleTranscript()
        {
            var recorder = ExpeditionReplayRecorder.Start(CreateBinding());
            recorder.Record("route-1", "route.select", 0, "M00",
                new Dictionary<string, string>
                {
                    { "destinationNodeId", "M01" }
                });

            Assert.Throws<InvalidOperationException>(() =>
                recorder.CreateCheckpointEnvelope());
        }

        private static IDictionary<string, string> BattleFields(string winner)
        {
            return new Dictionary<string, string>
            {
                { "encounterId", "M01:3:42" },
                { "battleSeed", "42" },
                { "battleInputSha256", new string('a', 64) },
                { "battleEventSha256", new string('b', 64) },
                { "winner", winner }
            };
        }

        private static AuthoritativeExpeditionRunBinding CreateBinding()
        {
            var snapshots = new ExpeditionLoadoutSnapshotHashes(
                new string('1', 64), new string('2', 64), new string('3', 64),
                new string('4', 64),
                FormalExpeditionSnapshotContract.SnapshotContractVersion + "|deck",
                FormalExpeditionSnapshotContract.SnapshotContractVersion + "|equipment",
                FormalExpeditionSnapshotContract.SnapshotContractVersion + "|lantern",
                FormalExpeditionSnapshotContract.SnapshotContractVersion + "|eligibility");
            var start = new DateTimeOffset(2026, 10, 8, 0, 0, 0, TimeSpan.Zero);
            var run = new ExpeditionServerRunSnapshot
            {
                RunId = "11111111-1111-1111-1111-111111111111",
                EntryIntentId = "22222222-2222-2222-2222-222222222222",
                AccountId = "33333333-3333-3333-3333-333333333333",
                WalletAddress = "wallet-a",
                ChapterId = ExpeditionEntryCoordinator.ChapterId,
                DifficultyId = "D0",
                DifficultyDisplayNameKey = "difficulty.d0",
                EntryPriceMicroSkr = 0,
                QuotaDayId = "2026-10-08_UTC",
                RewardOrigin = "expedition.chapter01",
                TradeStatePolicy = "bound",
                RewardTableVersion = ChapterOneDifficultyRules.RewardTableVersion,
                EncounterTableVersion = ChapterOneDifficultyRules.EncounterTableVersion,
                MapVersion = ChapterOneDifficultyRules.MapVersion,
                RulesetVersion = ChapterOneDifficultyRules.RulesVersion,
                SeasonId = "season-a",
                DeckSnapshotHash = snapshots.DeckSnapshotHash,
                EquipmentSnapshotHash = snapshots.EquipmentSnapshotHash,
                LanternSnapshotHash = snapshots.LanternSnapshotHash,
                FirstClearLanternEligibilityKeyHash =
                    snapshots.FirstClearEligibilityKeyHash,
                CombatInputContractVersion = snapshots.CombatInputContractVersion,
                CombatInputHash = snapshots.CombatInputHash,
                DeckSnapshotCanonical = snapshots.DeckSnapshotCanonical,
                EquipmentSnapshotCanonical = snapshots.EquipmentSnapshotCanonical,
                LanternSnapshotCanonical = snapshots.LanternSnapshotCanonical,
                FirstClearEligibilityCanonical = snapshots.FirstClearEligibilityCanonical,
                SeedCommitment = new string('5', 64),
                CreatedAt = start.AddHours(1).ToString("O"),
                AuthoritativeTimeZone = ChapterOneDifficultyRules.AuthoritativeTimeZone,
                QuotaDayStartAtUtc = start.ToString("O"),
                QuotaDayEndAtUtc = start.AddDays(1).ToString("O"),
                FormalCreationCloseAtUtc = start.AddHours(22).ToString("O"),
                RunExpiresAtUtc = start.AddDays(1).ToString("O"),
                TimeRulesVersion = ChapterOneDifficultyRules.TimeRulesVersion,
                QualityRecruitPriceRulesVersion = QualityRecruitPriceRules.CurrentRulesVersion
            };
            return FormalExpeditionSnapshotContract.Bind(run, run.AccountId,
                run.WalletAddress, run.SeasonId, snapshots);
        }
    }
}
