using System;
using System.Linq;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class C01C05ActiveSkillEventPipelineTests
    {
        [Test]
        public void C01SupportsMultiTargetChildrenWithoutRecursiveShieldBreak()
        {
            var pipeline = NewPipeline("c01", "caster", "enemy-0", "enemy-1");
            var action = pipeline.BeginAction("caster", "c01-action", Rule("C01.burst",
                CardBattleDeliveryMode.GroundPulse, 2, CardBattleImpactEmissionMode.SingleEventMultiTarget), 0);
            pipeline.ResolveImpact(action, "impact", 100, new[] {
                new CardBattleTargetResult { ChildIndex = 0, TargetId = "enemy-0", ShieldDelta = -5 },
                new CardBattleTargetResult { ChildIndex = 1, TargetId = "enemy-1", ShieldDelta = -5 }
            });
            pipeline.CompleteAction(action, 120, CardBattleActionOutcome.Completed,
                CardBattleCancelReason.None, CardBattleInterruptReason.None);
            Assert.That(pipeline.ProducedEvents[1].Results[1].ChildIndex, Is.EqualTo(1));
            Assert.That(pipeline.ValidateProducedEvents(), Is.Empty);
        }

        [Test]
        public void C01RejectsRecursiveShieldBreakMarker()
        {
            var pipeline = NewPipeline("c01-recursive", "caster", "enemy");
            var action = pipeline.BeginAction("caster", "a", Rule("C01.burst",
                CardBattleDeliveryMode.GroundPulse, 2, CardBattleImpactEmissionMode.SingleEventMultiTarget), 0);
            Assert.Throws<InvalidOperationException>(() => pipeline.ResolveImpact(action, "i", 1, new[] {
                new CardBattleTargetResult { ChildIndex = 0, TargetId = "enemy",
                    StatusChanges = new System.Collections.Generic.List<string> { "shield-break:recursive" } }
            }));
        }

        [Test]
        public void C02BeamRetainsOneHealthBeforeHealing()
        {
            var pipeline = NewPipeline("c02", "caster", "ally");
            var action = pipeline.BeginAction("caster", "a", Rule("C02.beam",
                CardBattleDeliveryMode.Beam, 1, CardBattleImpactEmissionMode.SingleEventSingleTarget), 0);
            pipeline.ResolveImpact(action, "i", 1, new[] {
                new CardBattleTargetResult { ChildIndex = 0, TargetId = "ally", Healing = 5, TargetHealthAfter = 1 }
            });
            Assert.Throws<InvalidOperationException>(() => pipeline.ResolveImpact(action, "duplicate", 2, new[] {
                new CardBattleTargetResult { ChildIndex = 0, TargetId = "ally", Healing = 5, TargetHealthAfter = 0 }
            }));
        }

        [Test]
        public void C03DirectKillCannotTransferWhiteMarkAndTransferChangesHash()
        {
            var pipeline = NewPipeline("c03", "caster", "enemy");
            var action = pipeline.BeginAction("caster", "a", Rule("C03.mark",
                CardBattleDeliveryMode.InstantTarget, 1, CardBattleImpactEmissionMode.SingleEventSingleTarget), 0);
            Assert.Throws<InvalidOperationException>(() => pipeline.ResolveImpact(action, "i", 1, new[] {
                new CardBattleTargetResult { ChildIndex = 0, TargetId = "enemy", IsDefeated = true,
                    TargetHealthAfter = 0,
                    StatusChanges = new System.Collections.Generic.List<string> { "white-mark:transfer" } }
            }));

            var validPipeline = NewPipeline("c03-transfer", "caster", "enemy");
            var validAction = validPipeline.BeginAction("caster", "a", Rule("C03.mark",
                CardBattleDeliveryMode.InstantTarget, 1, CardBattleImpactEmissionMode.SingleEventSingleTarget), 0);
            validPipeline.ResolveImpact(validAction, "i", 1, new[] {
                new CardBattleTargetResult { ChildIndex = 0, TargetId = "enemy", TargetHealthAfter = 90,
                    StatusChanges = new System.Collections.Generic.List<string> { "white-mark:transfer" } }
            });
            var before = validPipeline.FinalStateHash;
            validPipeline.AddStatusFeedback(validAction, "enemy", "white-mark", "old", "new", 1);
            validPipeline.ConsumeProducedEvents();
            Assert.That(validPipeline.ProducedEvents[2].StateHashAfter, Is.Not.Null.And.Not.Empty);
            Assert.That(validPipeline.FinalStateHash, Is.Not.EqualTo(before));
        }

        [Test]
        public void C04UsesIndependentChildrenAndNeverReplacesDeadTarget()
        {
            var pipeline = NewPipeline("c04", "caster", "enemy-0", "enemy-1");
            var action = pipeline.BeginAction("caster", "a", Rule("C04.pool",
                CardBattleDeliveryMode.InstantTarget, 2, CardBattleImpactEmissionMode.SingleEventMultiTarget), 0);
            pipeline.ResolveImpact(action, "i", 1, new[] {
                new CardBattleTargetResult { ChildIndex = 0, TargetId = "enemy-0", IsDefeated = true },
                new CardBattleTargetResult { ChildIndex = 1, TargetId = "enemy-1", TargetHealthAfter = 50 }
            });
            Assert.That(pipeline.ProducedEvents[1].Results[0].TargetId, Is.EqualTo("enemy-0"));
            Assert.That(pipeline.ProducedEvents[1].Results[1].TargetId, Is.EqualTo("enemy-1"));
        }

        [Test]
        public void C05BindsColumnWithoutAttackSpeedApproximation()
        {
            var pipeline = NewPipeline("c05", "caster", "enemy");
            var rule = Rule("C05.barrier", CardBattleDeliveryMode.GroundPulse, 1,
                CardBattleImpactEmissionMode.SingleEventSingleTarget);
            rule.TargetRule = "TR_COLUMN_BOUND";
            rule.Tags.Add("no-collider");
            var action = pipeline.BeginAction("caster", "a", rule, 0);
            pipeline.ResolveImpact(action, "i", 1, new[] {
                new CardBattleTargetResult { ChildIndex = 0, TargetId = "enemy", Damage = 1, TargetHealthAfter = 99 }
            });
            Assert.That(pipeline.ValidateProducedEvents(), Is.Empty);
            rule.Tags.Add("attack-speed-percent");
            Assert.Throws<InvalidOperationException>(() => pipeline.BeginAction("caster", "blocked", rule, 2));
        }

        [Test]
        public void C05RuleUsesGroundPulseAndColumnBinding()
        {
            var rule = C05ActiveSkillIntegration.CreateRebuildBridgeRule();
            Assert.DoesNotThrow(() => rule.Validate());
            Assert.That(rule.AbilityId, Is.EqualTo(C05ActiveSkillIntegration.RebuildBridgeAbilityId));
            Assert.That(rule.DeliveryMode, Is.EqualTo(CardBattleDeliveryMode.GroundPulse));
            Assert.That(rule.Tags, Does.Contain("no-collider"));
            Assert.That(rule.Tags, Does.Contain("barrier-column-bound"));
        }

        [Test]
        public void C05DoesNotProduceProjectileReleasedForGroundPulse()
        {
            var pipeline = NewPipeline("c05-ground", "brun", "target");
            var action = pipeline.BeginAction("brun", "c05-action",
                C05ActiveSkillIntegration.CreateRebuildBridgeRule(), 0);
            pipeline.ResolveImpact(action, "c05-impact", 1, new[] {
                new CardBattleTargetResult { ChildIndex = 0, TargetId = "target", TargetHealthAfter = 100 }
            });
            pipeline.CompleteAction(action, 2, CardBattleActionOutcome.Completed,
                CardBattleCancelReason.None, CardBattleInterruptReason.None);
            Assert.That(pipeline.ProducedEvents.Any(item =>
                item.Kind == CardBattleEventKind.ProjectileReleased), Is.False);
            Assert.That(pipeline.ValidateProducedEvents(), Is.Empty);
        }

        [Test]
        public void C05GroundPulsePreservesEventOrderAndStableChildIndex()
        {
            var pipeline = NewPipeline("c05-order", "brun", "target");
            var action = pipeline.BeginAction("brun", "c05-action",
                C05ActiveSkillIntegration.CreateRebuildBridgeRule(), 0);
            pipeline.ResolveImpact(action, "c05-impact", 10, new[] {
                new CardBattleTargetResult { ChildIndex = 0, TargetId = "target", TargetHealthAfter = 100 }
            });
            pipeline.CompleteAction(action, 20, CardBattleActionOutcome.Completed,
                CardBattleCancelReason.None, CardBattleInterruptReason.None);
            Assert.That(pipeline.ProducedEvents[0].Kind, Is.EqualTo(CardBattleEventKind.ActionStarted));
            Assert.That(pipeline.ProducedEvents[1].Kind, Is.EqualTo(CardBattleEventKind.ImpactResolved));
            Assert.That(pipeline.ProducedEvents[1].Results[0].ChildIndex, Is.EqualTo(0));
            Assert.That(pipeline.ProducedEvents[2].Kind, Is.EqualTo(CardBattleEventKind.ActionCompleted));
            Assert.That(pipeline.ProducedEvents[0].EventSequence,
                Is.LessThan(pipeline.ProducedEvents[1].EventSequence));
            Assert.That(pipeline.ProducedEvents[1].EventSequence,
                Is.LessThan(pipeline.ProducedEvents[2].EventSequence));
        }

        [Test]
        public void StatusExpirationRemovesStatusAndReconnectRestoresHash()
        {
            var pipeline = NewPipeline("status", "caster", "enemy");
            var action = pipeline.BeginAction("caster", "a", Rule("C03.status",
                CardBattleDeliveryMode.InstantTarget, 1, CardBattleImpactEmissionMode.SingleEventSingleTarget), 0);
            pipeline.ResolveImpact(action, "i", 1, new[] {
                new CardBattleTargetResult { ChildIndex = 0, TargetId = "enemy", TargetHealthAfter = 100 }
            });
            pipeline.AddStatusFeedback(action, "enemy", "white-mark", "", "active", 1);
            pipeline.ExpireStatus(action, "enemy", "white-mark", "active", 1);
            pipeline.ConsumeProducedEvents();
            Assert.That(pipeline.Projection.FindUnit("enemy").Statuses.ContainsKey("white-mark"), Is.False);
            var state = pipeline.CaptureReconnectState();
            var restored = NewPipeline("status", "caster", "enemy");
            restored.RestoreReconnectState(state);
            Assert.That(restored.FinalStateHash, Is.EqualTo(state.StateHash));
        }

        [Test]
        public void SkipClearsFlightsAndProducesDeterministicFinalHash()
        {
            var pipeline = NewPipeline("skip", "caster", "enemy");
            var rule = Rule("C01.projectile", CardBattleDeliveryMode.Projectile, 2,
                CardBattleImpactEmissionMode.SingleEventMultiTarget);
            rule.ProjectileProfileId = "PP_C01";
            var action = pipeline.BeginAction("caster", "a", rule, 0);
            pipeline.ReleaseProjectile(action, "p", "origin", "target", "PP_C01", 100);
            pipeline.ConsumeProducedEvents();
            Assert.That(pipeline.Projection.PendingProjectiles.Count, Is.EqualTo(1));
            pipeline.SettleForSkipOrAcceleration(4, new[] {
                new CardBattleUnitSnapshot { Id = "caster", Health = 100 },
                new CardBattleUnitSnapshot { Id = "enemy", Health = 80 }
            });
            Assert.That(pipeline.Projection.PendingProjectiles.Count, Is.Zero);
            Assert.That(pipeline.FinalStateHash, Is.EqualTo(
                C01C05ActiveSkillEventPipeline.ComputeStateHash(pipeline.Projection.Units)));
        }

        private static C01C05ActiveSkillEventPipeline NewPipeline(string battleId, params string[] ids)
        {
            var snapshot = new System.Collections.Generic.List<CardBattleUnitSnapshot>();
            foreach (var id in ids) snapshot.Add(new CardBattleUnitSnapshot { Id = id, Health = 100 });
            return new C01C05ActiveSkillEventPipeline(battleId, snapshot);
        }

        private static CardBattlePresentationRule Rule(string abilityId, CardBattleDeliveryMode mode,
            int maxTargets, CardBattleImpactEmissionMode emission)
        {
            return new CardBattlePresentationRule {
                AbilityId = abilityId, ActionClass = CardBattleActionClass.HeroActive,
                TargetRule = "TR_TARGET", MaxTargetCount = maxTargets, DeliveryMode = mode,
                ImpactEmissionMode = emission, ProjectileProfileId = mode == CardBattleDeliveryMode.Projectile
                    ? "PP_DEFAULT" : null
            };
        }
    }
}
