using System;
using System.Linq;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class AudioEventRouterTests
    {
        [Test]
        public void FiveAuthoritativeEventKindsMapToCues()
        {
            var router = new BattleAudioEventRouter();
            Assert.That(router.Consume(Event(CardBattleEventKind.ActionStarted, "S_OLIN_OPEN_DAWN", 1, "a")).Count, Is.EqualTo(1));
            Assert.That(router.Consume(Event(CardBattleEventKind.ProjectileReleased, "S_OLIN_OPEN_DAWN", 2, "b")).Count, Is.EqualTo(1));
            var impact = Event(CardBattleEventKind.ImpactResolved, "S_OLIN_OPEN_DAWN", 3, "c");
            impact.Results.Add(Result("target", 0, 10, false));
            Assert.That(router.Consume(impact).Count, Is.EqualTo(1));
            Assert.That(router.Consume(Status("C01_SHIELD_GAIN", 4, "d")).Count, Is.EqualTo(1));
            Assert.That(router.Consume(Completed(CardBattleActionOutcome.Completed, 5, "e")).Count, Is.EqualTo(1));
        }

        [Test]
        public void ActionIdEventSequenceAndChildIndexDeduplicate()
        {
            var router = new BattleAudioEventRouter();
            var item = Event(CardBattleEventKind.ActionStarted, "S_OLIN_OPEN_DAWN", 1, "same");
            Assert.That(router.Consume(item).Count, Is.EqualTo(1));
            Assert.That(router.Consume(item.Copy()).Count, Is.EqualTo(0));
            var child = Event(CardBattleEventKind.ImpactResolved, "S_OLIN_OPEN_DAWN", 2, "same");
            child.Results.Add(Result("a", 0, 1, false));
            child.Results.Add(Result("b", 1, 1, false));
            Assert.That(router.Consume(child).Count, Is.EqualTo(2));
            Assert.That(router.Consume(child.Copy()).Count, Is.EqualTo(0));
        }

        [Test]
        public void MultiTargetChildrenTriggerIndependently()
        {
            var router = new BattleAudioEventRouter();
            var item = Event(CardBattleEventKind.ImpactResolved, "S_OLIN_OPEN_DAWN", 1, "multi");
            item.Results.Add(Result("a", 0, 1, false));
            item.Results.Add(Result("b", 1, 1, false));
            var cues = router.Consume(item);
            Assert.That(cues[0].ChildIndex, Is.EqualTo(0));
            Assert.That(cues[1].ChildIndex, Is.EqualTo(1));
        }

        [Test]
        public void InvalidAndEmptyResultsDoNotPlayFakeHit()
        {
            var router = new BattleAudioEventRouter();
            var empty = Event(CardBattleEventKind.ImpactResolved, "S_OLIN_OPEN_DAWN", 1, "empty");
            Assert.That(router.Consume(empty).Count, Is.EqualTo(0));
            var invalid = Event(CardBattleEventKind.ImpactResolved, "S_OLIN_OPEN_DAWN", 2, "invalid");
            invalid.Results.Add(Result("dead", 0, 10, false));
            Assert.That(router.Consume(invalid, id => false).Count, Is.EqualTo(0));
            var finalHit = Event(CardBattleEventKind.ImpactResolved, "S_OLIN_OPEN_DAWN", 3, "final");
            finalHit.Results.Add(Result("dead", 0, 10, true));
            Assert.That(router.Consume(finalHit, id => false).Count, Is.EqualTo(1));
        }

        [Test]
        public void CancelInterruptedAndNoTargetCompleteAsTerminalCues()
        {
            var router = new BattleAudioEventRouter();
            foreach (var outcome in new[] { CardBattleActionOutcome.Cancelled,
                CardBattleActionOutcome.Interrupted, CardBattleActionOutcome.NoValidTarget })
            {
                var cues = router.Consume(Completed(outcome, 10 + (long)outcome, outcome.ToString()));
                Assert.That(cues.Count, Is.EqualTo(1));
                Assert.That(cues[0].IsTerminal, Is.True);
            }
        }

        [Test]
        public void SkipAndAccelerationOnlyConvergePlaybackState()
        {
            var router = new BattleAudioEventRouter();
            router.SetPlaybackRate(2f);
            Assert.That(router.PlaybackRate, Is.EqualTo(2f));
            var cue = router.Consume(Event(CardBattleEventKind.ActionStarted, "S_OLIN_OPEN_DAWN", 1, "skip")).Single();
            router.SkipAndFlush(cue.ActionId);
            router.SetPlaybackRate(1f);
            Assert.That(router.Consume(Event(CardBattleEventKind.ActionStarted, "S_OLIN_OPEN_DAWN", 2, "after-skip")).Count, Is.EqualTo(1));
        }

        [Test]
        public void ReconnectDoesNotReplayConfirmedEvent()
        {
            var router = new BattleAudioEventRouter();
            var item = Event(CardBattleEventKind.ActionStarted, "S_OLIN_OPEN_DAWN", 4, "reconnect");
            router.ReconcileAfterReconnect(4, new[] { item });
            Assert.That(router.Consume(item).Count, Is.EqualTo(0));
        }

        [Test]
        public void C01ToC05PlaceholderMappingsAreComplete()
        {
            var router = new BattleAudioEventRouter();
            var ids = new[] { "S_OLIN_OPEN_DAWN", "S_YIWEN_RETURN_BEACON", "S_LEIF_WHITE_TRAIL",
                "S_MILA_HIDDEN_SPRING", "S_C05_BARRIER" };
            for (var index = 0; index < ids.Length; index++)
                Assert.That(router.Consume(Event(CardBattleEventKind.ActionStarted, ids[index], index + 1,
                    "map-" + index)).Count, Is.EqualTo(1));
            Assert.That(router.Consume(Status("C02_BEAM", 20, "beam")).Single().CueId, Is.EqualTo("BTL_C02_BEAM"));
            Assert.That(router.Consume(Status("C03_MARK_TRANSFER", 21, "mark")).Single().CueId, Is.EqualTo("BTL_C03_MARK_TRANSFER"));
            Assert.That(router.Consume(Status("C04_SEED_RELEASE", 22, "seed")).Single().CueId, Is.EqualTo("BTL_C04_SEED_RELEASE"));
            Assert.That(router.Consume(Status("C05_R31", 23, "r31")).Single().CueId, Is.EqualTo("BTL_C05_R31"));
        }

        [Test]
        public void MixerGroupsAreIndependent()
        {
            var mixer = new UiAudioEventRouter().Mixer;
            mixer.SetGroup("master", 0.9f);
            mixer.SetGroup("music", 0.8f);
            mixer.SetGroup("battle", 0.7f);
            mixer.SetGroup("ui", 0.6f);
            Assert.That(mixer.Master, Is.EqualTo(0.9f));
            Assert.That(mixer.Music, Is.EqualTo(0.8f));
            Assert.That(mixer.Battle, Is.EqualTo(0.7f));
            Assert.That(mixer.Ui, Is.EqualTo(0.6f));
        }

        [Test]
        public void MuteTakesEffectImmediately()
        {
            var router = new UiAudioEventRouter();
            router.SetMuted(true);
            Assert.That(router.Consume(UiAudioCueIds.Tap, "mute", 1), Is.Null);
            router.SetMuted(false);
            Assert.That(router.Consume(UiAudioCueIds.Tap, "unmute", 2), Is.Not.Null);
        }

        [Test]
        public void ContinuousClicksAreRateLimited()
        {
            var router = new UiAudioEventRouter { CooldownTicks = 2 };
            Assert.That(router.Consume(UiAudioCueIds.Tap, "one", 10), Is.Not.Null);
            Assert.That(router.Consume(UiAudioCueIds.Tap, "two", 11), Is.Null);
            Assert.That(router.Consume(UiAudioCueIds.Tap, "three", 12), Is.Not.Null);
        }

        [Test]
        public void BattleAudioConcurrencyIsBounded()
        {
            var router = new BattleAudioEventRouter(2);
            Assert.That(router.Consume(Event(CardBattleEventKind.ActionStarted, "S_OLIN_OPEN_DAWN", 1, "one")).Count, Is.EqualTo(1));
            Assert.That(router.Consume(Event(CardBattleEventKind.ActionStarted, "S_OLIN_OPEN_DAWN", 2, "two")).Count, Is.EqualTo(1));
            Assert.That(router.Consume(Event(CardBattleEventKind.ActionStarted, "S_OLIN_OPEN_DAWN", 3, "three")).Count, Is.EqualTo(0));
            Assert.That(router.SuppressedCount, Is.EqualTo(1));
        }

        [Test]
        public void MissingResourcesUseFallbackWithoutThrowing()
        {
            var battle = new BattleAudioEventRouter();
            var ui = new UiAudioEventRouter();
            Assert.That(battle.Consume(Event(CardBattleEventKind.ActionStarted, "S_OLIN_OPEN_DAWN", 1, "fallback")).Single().IsFallback, Is.True);
            Assert.That(ui.Consume(UiAudioCueIds.Tap, "fallback", 1).IsFallback, Is.True);
        }

        [Test]
        public void DisablingAudioDoesNotRejectAuthorityPipelineSemantics()
        {
            var router = new BattleAudioEventRouter();
            router.SetEnabled(false);
            var item = Event(CardBattleEventKind.ActionStarted, "S_OLIN_OPEN_DAWN", 1, "disabled");
            Assert.That(router.Consume(item).Count, Is.EqualTo(0));
            Assert.That(item.EventSequence, Is.EqualTo(1));
            Assert.That(item.ActionId, Is.EqualTo("disabled"));
        }

        [Test]
        public void CueCarriesAuthorityMetadata()
        {
            var item = Event(CardBattleEventKind.ActionStarted, "S_OLIN_OPEN_DAWN", 1, "metadata");
            item.SourceId = "source";
            item.AuthorityTick = 17;
            item.StateHashAfter = "hash";
            var cue = new BattleAudioEventRouter().Consume(item).Single();
            Assert.That(cue.SourceId, Is.EqualTo("source"));
            Assert.That(cue.AuthorityTick, Is.EqualTo(17));
            Assert.That(cue.StateHashAfter, Is.EqualTo("hash"));
        }

        [Test]
        public void RepresentativeMainClickCuesShareTheApprovedResource()
        {
            var cues = new[] { UiAudioCueIds.Confirm, UiAudioCueIds.Cancel,
                UiAudioCueIds.TabSwitch, UiAudioCueIds.Purchase, UiAudioCueIds.BattleStart,
                UiAudioCueIds.Upgrade, UiAudioCueIds.Tap };
            foreach (var cueId in cues.Where(id => id != UiAudioCueIds.Cancel))
                Assert.That(UiMainClickAudioPolicy.ResolveResourcePath(cueId),
                    Is.EqualTo(UiMainClickAudioPolicy.MainClickResourcePath));
            Assert.That(UiMainClickAudioPolicy.ResolveResourcePath(UiAudioCueIds.Cancel),
                Is.EqualTo(UiMainClickAudioPolicy.CancelResourcePath));
        }

        [Test]
        public void FormationPickAndDropUseIndependentApprovedResource()
        {
            Assert.That(UiMainClickAudioPolicy.ResolveResourcePath(UiAudioCueIds.DragPick),
                Is.EqualTo(UiMainClickAudioPolicy.FormationDropResourcePath));
            Assert.That(UiMainClickAudioPolicy.ResolveResourcePath(UiAudioCueIds.DragDrop),
                Is.EqualTo(UiMainClickAudioPolicy.FormationDropResourcePath));
            Assert.That(UiMainClickAudioPolicy.FormationDropResourcePath,
                Is.Not.EqualTo(UiMainClickAudioPolicy.MainClickResourcePath));
        }

        [Test]
        public void MainClickVolumeIsReducedWithoutChangingFormationDropVolume()
        {
            Assert.That(UiMainClickAudioPolicy.ResolveVolumeScale(
                UiMainClickAudioPolicy.MainClickResourcePath), Is.EqualTo(0.7f));
            Assert.That(UiMainClickAudioPolicy.ResolveVolumeScale(
                UiMainClickAudioPolicy.FormationDropResourcePath), Is.EqualTo(1f));
        }

        [Test]
        public void ResourceCacheKeepsEachResourcePathIndependent()
        {
            var cache = new UiAudioResourceCache<string>();
            var missingLoads = 0;
            var mainLoads = 0;
            var dropLoads = 0;
            Assert.That(cache.GetOrLoad("Audio/UI/MISSING",
                () => { missingLoads++; return null; }), Is.Null);
            Assert.That(cache.GetOrLoad("Audio/UI/MISSING",
                () => { missingLoads++; return "unexpected"; }), Is.Null);
            Assert.That(cache.GetOrLoad(UiMainClickAudioPolicy.MainClickResourcePath,
                () => { mainLoads++; return "main"; }), Is.EqualTo("main"));
            Assert.That(cache.GetOrLoad(UiMainClickAudioPolicy.FormationDropResourcePath,
                () => { dropLoads++; return "drop"; }), Is.EqualTo("drop"));
            Assert.That(cache.GetOrLoad(UiMainClickAudioPolicy.MainClickResourcePath,
                () => { mainLoads++; return "wrong"; }), Is.EqualTo("main"));
            Assert.That(cache.Count, Is.EqualTo(2));
            Assert.That(missingLoads, Is.EqualTo(1));
            Assert.That(mainLoads, Is.EqualTo(1));
            Assert.That(dropLoads, Is.EqualTo(1));
        }

        [Test]
        public void MutedMainClickIsRejectedBeforePlayback()
        {
            var router = new UiAudioEventRouter();
            router.SetMuted(true);
            Assert.That(router.Consume(UiAudioCueIds.Tap, "muted-main-click", 1), Is.Null);
        }

        [Test]
        public void MainClickStillUsesRouterCooldown()
        {
            var router = new UiAudioEventRouter { CooldownTicks = 2 };
            Assert.That(router.Consume(UiAudioCueIds.Tap, "first", 1), Is.Not.Null);
            Assert.That(router.Consume(UiAudioCueIds.Tap, "limited", 2), Is.Null);
        }

        [Test]
        public void UnmappedCueKeepsFallbackWithoutThrowing()
        {
            Assert.DoesNotThrow(() => UiMainClickAudioPolicy.ResolveResourcePath(UiAudioCueIds.Error));
            Assert.That(UiMainClickAudioPolicy.ResolveResourcePath(UiAudioCueIds.Error), Is.Null);
        }

        [Test]
        public void FrozenBattleResourcesMapToAuthoritativeCueFamilies()
        {
            Assert.That(BattleAudioResourcePolicy.ResolveResourcePath("BTL_MELEE_SWING"),
                Is.EqualTo(BattleAudioResourcePolicy.MeleeResourcePath));
            Assert.That(BattleAudioResourcePolicy.ResolveResourcePath("BTL_RANGED_HIT"),
                Is.EqualTo(BattleAudioResourcePolicy.RangedResourcePath));
            Assert.That(BattleAudioResourcePolicy.ResolveResourcePath("BTL_IMPACT_HEAL"),
                Is.EqualTo(BattleAudioResourcePolicy.BasicHealResourcePath));
            Assert.That(BattleAudioResourcePolicy.ResolveResourcePath("BTL_C01_ACTION_STARTED"),
                Is.EqualTo(BattleAudioResourcePolicy.SkillReleaseResourcePath));
        }

        [Test]
        public void HomeBackgroundMusicUsesMusicGroupAndHonorsMute()
        {
            var mixer = new AudioMixerGroups { Master = 0.8f, Music = 0.5f };
            Assert.That(HomeBackgroundMusicPolicy.ResourcePath,
                Is.EqualTo("Audio/Music/BGM_HOME_KINGDOM_QUEST_SOUNDIMAGE_V01"));
            Assert.That(HomeBackgroundMusicPolicy.ResolveVolume(mixer),
                Is.EqualTo(0.8f * 0.5f * HomeBackgroundMusicPolicy.VolumeScale).Within(0.0001f));
            mixer.Muted = true;
            Assert.That(HomeBackgroundMusicPolicy.ResolveVolume(mixer), Is.Zero);
        }

        [Test]
        public void BackgroundMusicRoutesGeneralExpeditionPreparationAndActiveBattleContexts()
        {
            Assert.That(BackgroundMusicPolicy.ResolveTrack(false, false),
                Is.EqualTo(BackgroundMusicTrack.Home));
            Assert.That(BackgroundMusicPolicy.ResolveTrack(true, false),
                Is.EqualTo(BackgroundMusicTrack.Expedition));
            Assert.That(BackgroundMusicPolicy.ResolveTrack(true, true),
                Is.EqualTo(BackgroundMusicTrack.Battle));
            Assert.That(BackgroundMusicPolicy.ResolveTrack(false, true),
                Is.EqualTo(BackgroundMusicTrack.Battle));
        }

        [Test]
        public void BackgroundMusicUsesApprovedResourcesAndTransitionDurations()
        {
            Assert.That(BackgroundMusicPolicy.ResourcePath(BackgroundMusicTrack.Expedition),
                Is.EqualTo("Audio/Music/BGM_EXPEDITION_THE_VOYAGE_BEGINS_30S_LOOP_SOUNDIMAGE_V02"));
            Assert.That(BackgroundMusicPolicy.ResourcePath(BackgroundMusicTrack.Battle),
                Is.EqualTo("Audio/Music/BGM_BATTLE_TRAINING_SOUNDIMAGE_V01"));
            Assert.That(BackgroundMusicPolicy.TransitionSeconds(
                BackgroundMusicTrack.Home, BackgroundMusicTrack.Expedition), Is.EqualTo(1.0f));
            Assert.That(BackgroundMusicPolicy.TransitionSeconds(
                BackgroundMusicTrack.Expedition, BackgroundMusicTrack.Battle), Is.EqualTo(0.8f));
            Assert.That(BackgroundMusicPolicy.TransitionSeconds(
                BackgroundMusicTrack.Battle, BackgroundMusicTrack.Expedition), Is.EqualTo(1.5f));
        }

        [Test]
        public void AttributionCatalogCoversEveryActiveThirdPartyRuntimeResource()
        {
            Assert.That(ThirdPartyAttributionCatalog.Entries.Count, Is.EqualTo(12));
            Assert.That(ThirdPartyAttributionCatalog.Entries.Count(
                entry => entry.Category == AttributionCategory.Music), Is.EqualTo(3));
            Assert.That(ThirdPartyAttributionCatalog.Entries.Count(
                entry => entry.Category == AttributionCategory.SoundEffects), Is.EqualTo(7));
            Assert.That(ThirdPartyAttributionCatalog.Entries.Count(
                entry => entry.Category == AttributionCategory.Fonts), Is.EqualTo(2));
            Assert.That(ThirdPartyAttributionCatalog.Entries.Any(entry =>
                entry.Title == "Kingdom Quest" &&
                entry.Credit.Contains("Eric Matyas") &&
                entry.SourceUrl == "https://soundimage.org/fantasy-11/"), Is.True);
            Assert.That(ThirdPartyAttributionCatalog.Entries.Any(entry =>
                entry.Title == "The Voyage Begins" && entry.Credit.Contains("crossfaded") &&
                entry.SourceUrl == "https://soundimage.org/epic-battle/"), Is.True);
            Assert.That(ThirdPartyAttributionCatalog.Entries.Any(entry =>
                entry.Title == "Training" && entry.Credit.Contains("Eric Matyas") &&
                entry.SourceUrl == "https://soundimage.org/sports/"), Is.True);
            Assert.That(ThirdPartyAttributionCatalog.Entries.All(entry =>
                !string.IsNullOrEmpty(entry.SourceUrl) && !string.IsNullOrEmpty(entry.LicenseUrl)), Is.True);
        }

        private static CardBattleEvent Event(CardBattleEventKind kind, string ability, long sequence, string action)
        {
            return new CardBattleEvent { Kind = kind, BattleId = "audio-test", EventSequence = sequence,
                ActionId = action, AbilityId = ability, SourceId = "source", AuthorityTick = sequence,
                Outcome = CardBattleActionOutcome.Completed };
        }

        private static CardBattleEvent Status(string key, long sequence, string action)
        {
            var item = Event(CardBattleEventKind.StatusFeedback, "S_OLIN_OPEN_DAWN", sequence, action);
            item.StatusKey = key;
            return item;
        }

        private static CardBattleEvent Completed(CardBattleActionOutcome outcome, long sequence, string action)
        {
            var item = Event(CardBattleEventKind.ActionCompleted, "S_OLIN_OPEN_DAWN", sequence, action);
            item.Outcome = outcome;
            return item;
        }

        private static CardBattleTargetResult Result(string target, int child, int damage, bool defeated)
        {
            return new CardBattleTargetResult { TargetId = target, ChildIndex = child, Damage = damage, IsDefeated = defeated };
        }
    }
}
