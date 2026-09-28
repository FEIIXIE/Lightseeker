using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class ExpeditionMapVisualTests
    {
        [Test]
        public void SameSeedAndNode_RecreateNeutralIslandSelection()
        {
            var first = ExpeditionMapVisualSelector.Create(12345u, "chapter1-8-3");
            var second = ExpeditionMapVisualSelector.Create(12345u, "chapter1-8-3");

            Assert.That(second.IslandTemplateIndex, Is.EqualTo(first.IslandTemplateIndex));
            Assert.That(second.MirrorX, Is.EqualTo(first.MirrorX));
            Assert.That(second.DecorationProfileIndex, Is.EqualTo(first.DecorationProfileIndex));
            Assert.That(second.AnchorIndices, Is.EqualTo(first.AnchorIndices));
            Assert.That(second.DecorationSizeOffsets, Is.EqualTo(first.DecorationSizeOffsets));
        }

        [Test]
        public void FixedSample_StaysInsideFormalPackageBounds()
        {
            var sawMirror = false;
            var sawUnmirrored = false;
            var templates = new System.Collections.Generic.HashSet<int>();
            var profiles = new System.Collections.Generic.HashSet<int>();

            for (uint seed = 1; seed <= 128; seed++)
            for (var node = 0; node < 15; node++)
            {
                var selection = ExpeditionMapVisualSelector.Create(
                    seed, "chapter1-" + (node + 1) + "-0");
                Assert.That(selection.IslandTemplateIndex,
                    Is.InRange(0, ExpeditionMapVisualSelector.IslandTemplateCount - 1));
                Assert.That(selection.DecorationProfileIndex,
                    Is.InRange(0, ExpeditionMapVisualSelector.DecorationProfileCount - 1));
                Assert.That(selection.AnchorIndices.Count, Is.InRange(2, 3));
                Assert.That(selection.AnchorIndices, Is.All.InRange(0, 3));
                Assert.That(selection.DecorationSizeOffsets, Is.All.InRange(-1, 1));
                templates.Add(selection.IslandTemplateIndex);
                profiles.Add(selection.DecorationProfileIndex);
                if (selection.MirrorX) sawMirror = true;
                else sawUnmirrored = true;
            }

            Assert.That(templates.Count, Is.EqualTo(ExpeditionMapVisualSelector.IslandTemplateCount));
            Assert.That(profiles.Count, Is.EqualTo(ExpeditionMapVisualSelector.DecorationProfileCount));
            Assert.That(sawMirror && sawUnmirrored, Is.True);
        }
    }
}
