using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class HomeLighthouseV140LayoutTests
    {
        [Test]
        public void DynamicRectsMatchApprovedV140Contract()
        {
            AssertRect(HomeLighthouseV140Layout.FullCanvas, 0f, 0f, 1080f, 2400f);
            AssertRect(HomeLighthouseV140Layout.PlayerName, 180f, 56f, 220f, 52f);
            AssertRect(HomeLighthouseV140Layout.PlayerPower, 222f, 103f, 220f, 40f);
            AssertRect(HomeLighthouseV140Layout.EnergyValue, 666f, 50f, 42f, 44f);
            AssertRect(HomeLighthouseV140Layout.SkrValue, 815f, 50f, 64f, 44f);
            AssertRect(HomeLighthouseV140Layout.TideValue, 976f, 52f, 52f, 44f);
            AssertRect(HomeLighthouseV140Layout.AffairsLabel, 746f, 213f, 106f, 38f);
            AssertRect(HomeLighthouseV140Layout.WarehouseLabel, 880f, 213f, 106f, 38f);
            AssertRect(HomeLighthouseV140Layout.Chapter, 250f, 340f, 580f, 62f);
            AssertRect(HomeLighthouseV140Layout.IslandName, 390f, 438f, 300f, 50f);
            AssertRect(HomeLighthouseV140Layout.PanelTitle, 330f, 1595f, 420f, 48f);
            AssertRect(HomeLighthouseV140Layout.LanternLabel, 398f, 1692f, 310f, 34f);
            AssertRect(HomeLighthouseV140Layout.LanternName, 398f, 1726f, 310f, 48f);
            AssertRect(HomeLighthouseV140Layout.LanternDetails, 398f, 1774f, 330f, 44f);
            AssertRect(HomeLighthouseV140Layout.LanternChange, 702f, 1727f, 182f, 52f);
            AssertRect(HomeLighthouseV140Layout.ExpeditionLabel, 260f, 1928f, 560f, 72f);
        }

        [Test]
        public void ModulePlacementsMatchApprovedV140Manifest()
        {
            AssertRect(HomeLighthouseV140Layout.BackgroundModule, 0f, 0f, 1080f, 2400f);
            AssertRect(HomeLighthouseV140Layout.TowerModule, 215f, 640f, 650f, 770f);
            AssertRect(HomeLighthouseV140Layout.LitOverlayModule, 0f, 0f, 1080f, 2400f);
            AssertRect(HomeLighthouseV140Layout.ChapterOrnamentModule, 145f, 333f, 790f, 160f);
            AssertRect(HomeLighthouseV140Layout.PlayerNameLineModule, 170f, 55f, 275f, 63f);
            AssertRect(HomeLighthouseV140Layout.PlayerMedallionModule, 56f, 45f, 122f, 122f);
            AssertRect(HomeLighthouseV140Layout.PlayerPowerBadgeModule, 188f, 105f, 32f, 32f);
            AssertRect(HomeLighthouseV140Layout.EnergyGlyphModule, 612f, 37f, 46f, 68f);
            AssertRect(HomeLighthouseV140Layout.ResourceSeparatorOneModule, 714f, 39f, 6f, 64f);
            AssertRect(HomeLighthouseV140Layout.SkrBadgeModule, 750f, 40f, 62f, 62f);
            AssertRect(HomeLighthouseV140Layout.ResourceSeparatorTwoModule, 882f, 39f, 6f, 64f);
            AssertRect(HomeLighthouseV140Layout.TideCrystalModule, 906f, 43f, 64f, 64f);
            AssertRect(HomeLighthouseV140Layout.AffairsModule, 756f, 126f, 86f, 86f);
            AssertRect(HomeLighthouseV140Layout.ResourceSeparatorThreeModule, 863f, 137f, 6f, 64f);
            AssertRect(HomeLighthouseV140Layout.WarehouseModule, 890f, 126f, 86f, 86f);
            AssertRect(HomeLighthouseV140Layout.LanternMedallionModule, 201f, 1685f, 142f, 142f);
            AssertRect(HomeLighthouseV140Layout.LanternPanelModule, 108f, 1534f, 864f, 351f);
            AssertRect(HomeLighthouseV140Layout.ExpeditionNormalModule, 170f, 1900f, 740f, 132f);
            AssertRect(HomeLighthouseV140Layout.ExpeditionPressedModule, 170f, 1898f, 740f, 137f);
            AssertRect(HomeLighthouseV140Layout.ExpeditionDisabledModule, 170f, 1899f, 740f, 134f);
            AssertRect(HomeLighthouseV140Layout.NavNeutralModule, 0f, 2158f, 1080f, 242f);
        }

        [Test]
        public void NavigationSpriteHasFiveContiguousHitSlotsAndExactLabelSlots()
        {
            var labelX = new[] { 68f, 277f, 480f, 684f, 893f };
            for (var index = 0; index < 5; index++)
            {
                AssertRect(HomeLighthouseV140Layout.BottomNavigationSlot(index),
                    index * 216f, 2158f, 216f, 242f);
                AssertRect(HomeLighthouseV140Layout.NavigationLabel(index),
                    labelX[index], 2332f, 120f, 48f);
            }
        }

        [Test]
        public void NavigationSelectionUsesFiveApprovedIndependentNativePixelPlacements()
        {
            Assert.That(HomeLighthouseV140Layout.NavigationVersion,
                Is.EqualTo("home-navigation-selection-v1.0.0"));
            AssertRect(HomeLighthouseV140Layout.NavigationSelection(0), 46f, 2221f, 169f, 129f);
            AssertRect(HomeLighthouseV140Layout.NavigationSelection(1), 253f, 2222f, 169f, 129f);
            AssertRect(HomeLighthouseV140Layout.NavigationSelection(2), 456f, 2217f, 169f, 129f);
            AssertRect(HomeLighthouseV140Layout.NavigationSelection(3), 660f, 2221f, 169f, 129f);
            AssertRect(HomeLighthouseV140Layout.NavigationSelection(4), 869f, 2220f, 169f, 129f);
        }

        [Test]
        public void HudTextSlotsDoNotOverlapTheirNeighbouringArtModules()
        {
            Assert.That(Overlaps(HomeLighthouseV140Layout.PlayerPower,
                HomeLighthouseV140Layout.PlayerPowerBadgeModule), Is.False);
            Assert.That(Overlaps(HomeLighthouseV140Layout.EnergyValue,
                HomeLighthouseV140Layout.EnergyGlyphModule), Is.False);
            Assert.That(Overlaps(HomeLighthouseV140Layout.EnergyValue,
                HomeLighthouseV140Layout.ResourceSeparatorOneModule), Is.False);
            Assert.That(Overlaps(HomeLighthouseV140Layout.SkrValue,
                HomeLighthouseV140Layout.SkrBadgeModule), Is.False);
            Assert.That(Overlaps(HomeLighthouseV140Layout.SkrValue,
                HomeLighthouseV140Layout.ResourceSeparatorTwoModule), Is.False);
            Assert.That(Overlaps(HomeLighthouseV140Layout.TideValue,
                HomeLighthouseV140Layout.TideCrystalModule), Is.False);
            Assert.That(Overlaps(HomeLighthouseV140Layout.AffairsLabel,
                HomeLighthouseV140Layout.AffairsModule), Is.False);
            Assert.That(Overlaps(HomeLighthouseV140Layout.WarehouseLabel,
                HomeLighthouseV140Layout.WarehouseModule), Is.False);
        }

        [TestCase(true, false, false, 5, HomeExpeditionButtonVisualState.Normal)]
        [TestCase(true, true, false, 4, HomeExpeditionButtonVisualState.Continue)]
        [TestCase(true, false, true, 0, HomeExpeditionButtonVisualState.FreeNoReward)]
        [TestCase(false, true, false, 4, HomeExpeditionButtonVisualState.Disabled)]
        public void ExpeditionButtonStateUsesDeliveredSpriteMapping(bool enabled, bool hasProgress,
            bool startsNewRun, int energy, HomeExpeditionButtonVisualState expected)
        {
            Assert.That(HomeLighthouseV140Layout.ResolveExpeditionButtonState(
                enabled, hasProgress, startsNewRun, energy), Is.EqualTo(expected));
        }

        [Test]
        public void LighthouseV140CompositionUsesNativeModulesInsideLockedPortraitCanvas()
        {
            Assert.That(HomeLighthouseV140Layout.Version, Is.EqualTo("home-lighthouse-v1.5.0"));
            Assert.That(HomeLighthouseV140Layout.ReferenceWidth, Is.EqualTo(1080f));
            Assert.That(HomeLighthouseV140Layout.ReferenceHeight, Is.EqualTo(2400f));
            var modules = new[]
            {
                HomeLighthouseV140Layout.BackgroundModule,
                HomeLighthouseV140Layout.TowerModule,
                HomeLighthouseV140Layout.LitOverlayModule,
                HomeLighthouseV140Layout.ChapterOrnamentModule,
                HomeLighthouseV140Layout.PlayerNameLineModule,
                HomeLighthouseV140Layout.PlayerMedallionModule,
                HomeLighthouseV140Layout.PlayerPowerBadgeModule,
                HomeLighthouseV140Layout.EnergyGlyphModule,
                HomeLighthouseV140Layout.ResourceSeparatorOneModule,
                HomeLighthouseV140Layout.SkrBadgeModule,
                HomeLighthouseV140Layout.ResourceSeparatorTwoModule,
                HomeLighthouseV140Layout.TideCrystalModule,
                HomeLighthouseV140Layout.AffairsModule,
                HomeLighthouseV140Layout.ResourceSeparatorThreeModule,
                HomeLighthouseV140Layout.WarehouseModule,
                HomeLighthouseV140Layout.LanternMedallionModule,
                HomeLighthouseV140Layout.LanternPanelModule,
                HomeLighthouseV140Layout.ExpeditionNormalModule,
                HomeLighthouseV140Layout.ExpeditionPressedModule,
                HomeLighthouseV140Layout.ExpeditionDisabledModule,
                HomeLighthouseV140Layout.NavNeutralModule,
                HomeLighthouseV140Layout.NavigationSelection(0),
                HomeLighthouseV140Layout.NavigationSelection(1),
                HomeLighthouseV140Layout.NavigationSelection(2),
                HomeLighthouseV140Layout.NavigationSelection(3),
                HomeLighthouseV140Layout.NavigationSelection(4)
            };
            foreach (var module in modules)
                Assert.That(HomeLighthouseV140Layout.FitsReferenceCanvas(module), Is.True);
        }

        private static bool Overlaps(ReferenceUiRect left, ReferenceUiRect right)
        {
            return left.X < right.X + right.Width && left.X + left.Width > right.X &&
                   left.Y < right.Y + right.Height && left.Y + left.Height > right.Y;
        }

        private static void AssertRect(ReferenceUiRect actual, float x, float y, float width, float height)
        {
            Assert.That(actual.X, Is.EqualTo(x));
            Assert.That(actual.Y, Is.EqualTo(y));
            Assert.That(actual.Width, Is.EqualTo(width));
            Assert.That(actual.Height, Is.EqualTo(height));
        }
    }
}
