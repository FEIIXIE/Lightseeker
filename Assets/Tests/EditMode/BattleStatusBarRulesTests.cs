using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class BattleStatusBarRulesTests
    {
        [TestCase(20, 4)]
        [TestCase(40, 8)]
        [TestCase(50, 10)]
        [TestCase(100, 10)]
        [TestCase(0, 1)]
        public void SegmentCountMatchesFrozenCapacityRule(int maximumHealth, int expected)
        {
            Assert.AreEqual(expected, BattleStatusBarRules.SegmentCount(maximumHealth));
        }
    }
}
