using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class FormalBattleAnimationContractTests
    {
        [Test]
        public void GateboardGuardCandidateKeepsFixedFrontAndGroundContract()
        {
            Assert.That(FormalBattleAnimationContract.CandidateStatus,
                Is.EqualTo("FORMAL_ANIMATION_CANDIDATE_PENDING_ART_AND_RUNTIME_ACCEPTANCE"));
            Assert.That(FormalBattleAnimationContract.CanvasWidth, Is.EqualTo(1212));
            Assert.That(FormalBattleAnimationContract.CanvasHeight, Is.EqualTo(1297));
            Assert.That(FormalBattleAnimationContract.GroundX, Is.EqualTo(606));
            Assert.That(FormalBattleAnimationContract.GroundYTopOrigin, Is.EqualTo(1267));
            Assert.That(FormalBattleAnimationContract.AllowsRootMotion, Is.False);
            Assert.That(FormalBattleAnimationContract.AllowsFacingFlip, Is.False);
        }

        [TestCase(FormalBattleAnimationIds.Idle, 1000, 3)]
        [TestCase(FormalBattleAnimationIds.BasicGateShieldStrike, 1250, 15)]
        [TestCase(FormalBattleAnimationIds.VariantGateStand, 1250, 15)]
        [TestCase(FormalBattleAnimationIds.Hit, 150, 8)]
        [TestCase(FormalBattleAnimationIds.Defeat, 650, 6)]
        public void GateboardGuardClipTimelinesMatchCandidateManifest(
            string clipId, int durationMs, int frameCount)
        {
            var clip = FormalBattleAnimationContract.GetClip(clipId);

            Assert.That(clip.DurationMs, Is.EqualTo(durationMs));
            Assert.That(clip.Frames.Count, Is.EqualTo(frameCount));
            Assert.That(clip.FrameAt(0).TimeMs, Is.Zero);
            Assert.That(clip.FrameAt(durationMs).TimeMs, Is.LessThanOrEqualTo(durationMs));
        }

        [TestCase(FormalBattleAnimationIds.BasicGateShieldStrike)]
        [TestCase(FormalBattleAnimationIds.VariantGateStand)]
        public void GateboardGuardActiveActionsUseOneReleaseAndOneImpactSlot(string clipId)
        {
            var clip = FormalBattleAnimationContract.GetClip(clipId);

            Assert.That(clip.ReleaseAtMs, Is.EqualTo(200));
            Assert.That(clip.ImpactAtMs, Is.EqualTo(300));
            Assert.That(clip.FrameAt(clip.ReleaseAtMs).TimeMs, Is.EqualTo(200));
            Assert.That(clip.FrameAt(clip.ImpactAtMs).TimeMs, Is.EqualTo(267));
        }

        [Test]
        public void IdleLoopsButActionAndDefeatClampToFinalFrame()
        {
            var idle = FormalBattleAnimationContract.GetClip(FormalBattleAnimationIds.Idle);
            var attack = FormalBattleAnimationContract.GetClip(FormalBattleAnimationIds.BasicGateShieldStrike);
            var defeat = FormalBattleAnimationContract.GetClip(FormalBattleAnimationIds.Defeat);

            Assert.That(idle.FrameAt(1000).TimeMs, Is.Zero);
            Assert.That(attack.FrameAt(5000).TimeMs, Is.EqualTo(1250));
            Assert.That(defeat.FrameAt(1000).TimeMs, Is.EqualTo(650));
        }
    }
}
