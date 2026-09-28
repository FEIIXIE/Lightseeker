using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class CardBattlePresentationContractTests
    {
        [Test]
        public void ProjectileDoesNotMutateSnapshotBeforeImpact()
        {
            var rule = ProjectileRule();
            var stream = new CardBattleEventStream("battle");
            var action = stream.BeginAction("C03", "a1", rule, 0);
            stream.ReleaseProjectile(action, "p1", "weapon", "body", "PP_ARROW", 180);
            stream.ResolveImpact(action, "i1", 140, new[] {
                new CardBattleTargetResult { ChildIndex = 0, TargetId = "enemy", TargetHealthAfter = 90 }
            });
            stream.CompleteAction(action, 300, CardBattleActionOutcome.Completed,
                CardBattleCancelReason.None, CardBattleInterruptReason.None);

            var projection = new CardBattleProjection("battle");
            projection.SetInitialSnapshot(new[] { new CardBattleUnitSnapshot { Id = "enemy", Health = 100 } });
            Assert.That(projection.Consume(stream.Events[0]), Is.EqualTo(CardBattleEventAcceptResult.Applied));
            Assert.That(projection.Consume(stream.Events[1]), Is.EqualTo(CardBattleEventAcceptResult.Applied));
            Assert.That(projection.FindUnit("enemy").Health, Is.EqualTo(100));
            Assert.That(projection.Consume(stream.Events[2]), Is.EqualTo(CardBattleEventAcceptResult.Applied));
            Assert.That(projection.FindUnit("enemy").Health, Is.EqualTo(90));
        }

        [Test]
        public void DuplicateImpactIsNotReplayed()
        {
            var stream = new CardBattleEventStream("battle");
            var action = stream.BeginAction("C01", "a1", new CardBattlePresentationRule {
                AbilityId = "C01.basic", ActionClass = CardBattleActionClass.BasicMelee,
                TargetRule = "TR_OPPOSING_FRONT_FALLBACK", DeliveryMode = CardBattleDeliveryMode.Melee,
                ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventSingleTarget
            }, 0);
            stream.ResolveImpact(action, "i1", 120, new[] {
                new CardBattleTargetResult { ChildIndex = 0, TargetId = "enemy", Damage = 5, TargetHealthAfter = 95 }
            });
            var projection = new CardBattleProjection("battle");
            projection.SetInitialSnapshot(new[] { new CardBattleUnitSnapshot { Id = "enemy", Health = 100 } });
            Assert.That(projection.Consume(stream.Events[0]), Is.EqualTo(CardBattleEventAcceptResult.Applied));
            Assert.That(projection.Consume(stream.Events[1]), Is.EqualTo(CardBattleEventAcceptResult.Applied));
            Assert.That(projection.Consume(stream.Events[1]), Is.EqualTo(CardBattleEventAcceptResult.Duplicate));
            Assert.That(projection.FindUnit("enemy").Health, Is.EqualTo(95));
        }

        [Test]
        public void SnapshotSkipClearsPendingFlight()
        {
            var stream = new CardBattleEventStream("battle");
            var action = stream.BeginAction("C03", "a1", ProjectileRule(), 0);
            stream.ReleaseProjectile(action, "p1", "weapon", "body", "PP_ARROW", 180);
            var projection = new CardBattleProjection("battle");
            projection.SetInitialSnapshot(new[] { new CardBattleUnitSnapshot { Id = "enemy", Health = 100 } });
            Assert.That(projection.Consume(stream.Events[0]), Is.EqualTo(CardBattleEventAcceptResult.Applied));
            Assert.That(projection.Consume(stream.Events[1]), Is.EqualTo(CardBattleEventAcceptResult.Applied));
            Assert.That(projection.PendingProjectiles.Count, Is.EqualTo(1));
            projection.SkipToSnapshot(8, new[] { new CardBattleUnitSnapshot { Id = "enemy", Health = 80 } });
            Assert.That(projection.PendingProjectiles.Count, Is.Zero);
            Assert.That(projection.LastConfirmedEventSequence, Is.EqualTo(8));
            Assert.That(projection.FindUnit("enemy").Health, Is.EqualTo(80));
        }

        private static CardBattlePresentationRule ProjectileRule()
        {
            return new CardBattlePresentationRule {
                AbilityId = "C03.basic", ActionClass = CardBattleActionClass.BasicProjectile,
                TargetRule = "TR_OPPOSING_FRONT_FALLBACK", DeliveryMode = CardBattleDeliveryMode.Projectile,
                ProjectileProfileId = "PP_ARROW", ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventSingleTarget
            };
        }
    }
}
