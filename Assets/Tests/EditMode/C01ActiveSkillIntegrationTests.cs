using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class C01ActiveSkillIntegrationTests
    {
        [Test]
        public void C01ActiveAndPassiveIdsUseTheFrozenEntryPoints()
        {
            Assert.That(C01ActiveSkillIntegration.IsC01Ability("S_OLIN_OPEN_DAWN"), Is.True);
            Assert.That(C01ActiveSkillIntegration.IsC01Ability("P_OLIN_TIDE"), Is.True);
            Assert.That(C01ActiveSkillIntegration.IsC01Ability("C02.other"), Is.False);
        }

        [Test]
        public void OpenDawnNormalizesMultiTargetOrderAndChildIndexes()
        {
            var normalized = C01ActiveSkillIntegration.NormalizeTargets(new[] {
                Result("b", 80), Result("a", 90)
            }, Units());
            Assert.That(normalized.Select(item => item.TargetId).ToArray(), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(normalized.Select(item => item.ChildIndex).ToArray(), Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void OpenDawnWithoutProjectileDoesNotEmitRelease()
        {
            var pipeline = NewPipeline("no-projectile");
            var action = pipeline.BeginAction("source", "a",
                C01ActiveSkillIntegration.CreateOpenDawnRule(false, 1), 1);
            pipeline.ResolveImpact(action, "i", 2, new[] { Result("target", 90) });
            pipeline.CompleteAction(action, 3, CardBattleActionOutcome.Completed,
                CardBattleCancelReason.None, CardBattleInterruptReason.None);
            Assert.That(pipeline.ProducedEvents.Any(item => item.Kind == CardBattleEventKind.ProjectileReleased), Is.False);
        }

        [Test]
        public void OpenDawnProjectileReleaseIsPresentationOnly()
        {
            var pipeline = NewPipeline("projectile");
            var action = pipeline.BeginAction("source", "a",
                C01ActiveSkillIntegration.CreateOpenDawnRule(true, 1), 1);
            pipeline.ReleaseProjectile(action, "p", "origin", "target", "PP_C01_OPEN_DAWN", 999);
            pipeline.ResolveImpact(action, "i", 2, new[] { Result("target", 90) });
            Assert.That(pipeline.ProducedEvents.Any(item => item.Kind == CardBattleEventKind.ProjectileReleased), Is.True);
            Assert.That(pipeline.ProducedEvents.Single(item => item.Kind == CardBattleEventKind.ImpactResolved)
                .AuthorityTick, Is.EqualTo(2));
        }

        [Test]
        public void SkipAccelerationAndReconnectCanShareTheSameFinalHash()
        {
            var snapshot = new[] {
                new CardBattleUnitSnapshot { Id = "source", Health = 100 },
                new CardBattleUnitSnapshot { Id = "target", Health = 90 }
            };
            var skip = NewPipeline("skip");
            skip.SettleForSkipOrAcceleration(10, snapshot);
            var acceleration = NewPipeline("acceleration");
            acceleration.SettleForSkipOrAcceleration(10, snapshot);
            Assert.That(acceleration.FinalStateHash, Is.EqualTo(skip.FinalStateHash));
            var reconnectState = skip.CaptureReconnectState();
            var reconnect = NewPipeline("skip");
            reconnect.RestoreReconnectState(reconnectState);
            Assert.That(reconnect.FinalStateHash, Is.EqualTo(skip.FinalStateHash));
        }

        private static C01C05ActiveSkillEventPipeline NewPipeline(string id)
        {
            return new C01C05ActiveSkillEventPipeline(id, Units());
        }

        private static List<CardBattleUnitSnapshot> Units()
        {
            return new List<CardBattleUnitSnapshot> {
                new CardBattleUnitSnapshot { Id = "source", Health = 100 },
                new CardBattleUnitSnapshot { Id = "target", Health = 100 },
                new CardBattleUnitSnapshot { Id = "a", Health = 100 },
                new CardBattleUnitSnapshot { Id = "b", Health = 100 }
            };
        }

        private static CardBattleTargetResult Result(string targetId, int health)
        {
            return new CardBattleTargetResult { TargetId = targetId, TargetHealthAfter = health };
        }
    }
}
