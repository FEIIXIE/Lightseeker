namespace SeekerAutoChess.Core
{
    public enum HomeExpeditionButtonVisualState
    {
        Normal,
        Continue,
        FreeNoReward,
        Disabled
    }

    public struct ReferenceUiRect
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public ReferenceUiRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// Runtime composition geometry supplied by the approved home-lighthouse-v1.5.0 contract and
    /// home-navigation-selection-v1.0.0 dual-resource navigation contract. All coordinates are locked
    /// reference pixels on the native 1080 x 2400 portrait canvas.
    /// </summary>
    public static class HomeLighthouseV140Layout
    {
        public const string Version = "home-lighthouse-v1.5.0";
        public const string NavigationVersion = "home-navigation-selection-v1.0.0";
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 2400f;

        public static readonly ReferenceUiRect FullCanvas = new ReferenceUiRect(0f, 0f, 1080f, 2400f);
        public static readonly ReferenceUiRect PlayerName = new ReferenceUiRect(180f, 56f, 220f, 52f);
        public static readonly ReferenceUiRect PlayerPower = new ReferenceUiRect(222f, 103f, 220f, 40f);
        public static readonly ReferenceUiRect EnergyValue = new ReferenceUiRect(666f, 50f, 42f, 44f);
        public static readonly ReferenceUiRect SkrValue = new ReferenceUiRect(815f, 50f, 64f, 44f);
        public static readonly ReferenceUiRect TideValue = new ReferenceUiRect(976f, 52f, 52f, 44f);
        public static readonly ReferenceUiRect Affairs = new ReferenceUiRect(746f, 126f, 106f, 125f);
        public static readonly ReferenceUiRect Warehouse = new ReferenceUiRect(880f, 126f, 106f, 125f);
        public static readonly ReferenceUiRect AffairsLabel = new ReferenceUiRect(746f, 213f, 106f, 38f);
        public static readonly ReferenceUiRect WarehouseLabel = new ReferenceUiRect(880f, 213f, 106f, 38f);

        public static readonly ReferenceUiRect Chapter = new ReferenceUiRect(250f, 340f, 580f, 62f);
        public static readonly ReferenceUiRect IslandName = new ReferenceUiRect(390f, 438f, 300f, 50f);
        public static readonly ReferenceUiRect IslandInteraction = new ReferenceUiRect(215f, 640f, 650f, 770f);

        public static readonly ReferenceUiRect PanelTitle = new ReferenceUiRect(330f, 1595f, 420f, 48f);
        public static readonly ReferenceUiRect LanternLabel = new ReferenceUiRect(398f, 1692f, 310f, 34f);
        public static readonly ReferenceUiRect LanternName = new ReferenceUiRect(398f, 1726f, 310f, 48f);
        public static readonly ReferenceUiRect LanternDetails = new ReferenceUiRect(398f, 1774f, 330f, 44f);
        public static readonly ReferenceUiRect LanternChange = new ReferenceUiRect(702f, 1727f, 182f, 52f);

        public static readonly ReferenceUiRect ExpeditionButton = new ReferenceUiRect(170f, 1898f, 740f, 137f);
        public static readonly ReferenceUiRect ExpeditionLabel = new ReferenceUiRect(260f, 1928f, 560f, 72f);

        public static readonly ReferenceUiRect BottomNavigation = new ReferenceUiRect(0f, 2158f, 1080f, 242f);

        public static readonly ReferenceUiRect BackgroundModule = new ReferenceUiRect(0f, 0f, 1080f, 2400f);
        public static readonly ReferenceUiRect TowerModule = new ReferenceUiRect(215f, 640f, 650f, 770f);
        public static readonly ReferenceUiRect LitOverlayModule = new ReferenceUiRect(0f, 0f, 1080f, 2400f);
        public static readonly ReferenceUiRect ChapterOrnamentModule = new ReferenceUiRect(145f, 333f, 790f, 160f);
        public static readonly ReferenceUiRect PlayerNameLineModule = new ReferenceUiRect(170f, 55f, 275f, 63f);
        public static readonly ReferenceUiRect PlayerMedallionModule = new ReferenceUiRect(56f, 45f, 122f, 122f);
        public static readonly ReferenceUiRect PlayerPowerBadgeModule = new ReferenceUiRect(188f, 105f, 32f, 32f);
        public static readonly ReferenceUiRect EnergyGlyphModule = new ReferenceUiRect(612f, 37f, 46f, 68f);
        public static readonly ReferenceUiRect ResourceSeparatorOneModule = new ReferenceUiRect(714f, 39f, 6f, 64f);
        public static readonly ReferenceUiRect SkrBadgeModule = new ReferenceUiRect(750f, 40f, 62f, 62f);
        public static readonly ReferenceUiRect ResourceSeparatorTwoModule = new ReferenceUiRect(882f, 39f, 6f, 64f);
        public static readonly ReferenceUiRect TideCrystalModule = new ReferenceUiRect(906f, 43f, 64f, 64f);
        public static readonly ReferenceUiRect AffairsModule = new ReferenceUiRect(756f, 126f, 86f, 86f);
        public static readonly ReferenceUiRect ResourceSeparatorThreeModule = new ReferenceUiRect(863f, 137f, 6f, 64f);
        public static readonly ReferenceUiRect WarehouseModule = new ReferenceUiRect(890f, 126f, 86f, 86f);
        public static readonly ReferenceUiRect LanternMedallionModule = new ReferenceUiRect(201f, 1685f, 142f, 142f);
        public static readonly ReferenceUiRect LanternPanelModule = new ReferenceUiRect(108f, 1534f, 864f, 351f);
        public static readonly ReferenceUiRect ExpeditionNormalModule = new ReferenceUiRect(170f, 1900f, 740f, 132f);
        public static readonly ReferenceUiRect ExpeditionPressedModule = new ReferenceUiRect(170f, 1898f, 740f, 137f);
        public static readonly ReferenceUiRect ExpeditionDisabledModule = new ReferenceUiRect(170f, 1899f, 740f, 134f);
        public static readonly ReferenceUiRect NavNeutralModule = new ReferenceUiRect(0f, 2158f, 1080f, 242f);

        public static ReferenceUiRect NavigationSelection(int index)
        {
            if (index < 0) index = 0;
            if (index > 4) index = 4;
            switch (index)
            {
                case 0: return new ReferenceUiRect(46f, 2221f, 169f, 129f);
                case 1: return new ReferenceUiRect(253f, 2222f, 169f, 129f);
                case 2: return new ReferenceUiRect(456f, 2217f, 169f, 129f);
                case 3: return new ReferenceUiRect(660f, 2221f, 169f, 129f);
                default: return new ReferenceUiRect(869f, 2220f, 169f, 129f);
            }
        }

        public static ReferenceUiRect BottomNavigationSlot(int index)
        {
            const int slotCount = 5;
            if (index < 0) index = 0;
            if (index >= slotCount) index = slotCount - 1;
            var slotWidth = BottomNavigation.Width / slotCount;
            return new ReferenceUiRect(BottomNavigation.X + slotWidth * index,
                BottomNavigation.Y, slotWidth, BottomNavigation.Height);
        }

        public static ReferenceUiRect NavigationLabel(int index)
        {
            const int slotCount = 5;
            if (index < 0) index = 0;
            if (index >= slotCount) index = slotCount - 1;
            float x;
            switch (index)
            {
                case 0: x = 68f; break;
                case 1: x = 277f; break;
                case 2: x = 480f; break;
                case 3: x = 684f; break;
                default: x = 893f; break;
            }
            return new ReferenceUiRect(x, 2332f, 120f, 48f);
        }

        public static HomeExpeditionButtonVisualState ResolveExpeditionButtonState(
            bool enabled, bool hasProgress, bool startsNewRun, int expeditionEnergy)
        {
            if (!enabled) return HomeExpeditionButtonVisualState.Disabled;
            if (hasProgress) return HomeExpeditionButtonVisualState.Continue;
            if (startsNewRun && expeditionEnergy <= 0)
                return HomeExpeditionButtonVisualState.FreeNoReward;
            return HomeExpeditionButtonVisualState.Normal;
        }

        public static bool FitsReferenceCanvas(ReferenceUiRect rect)
        {
            return rect.X >= 0f && rect.Y >= 0f && rect.Width > 0f && rect.Height > 0f &&
                   rect.X + rect.Width <= ReferenceWidth && rect.Y + rect.Height <= ReferenceHeight;
        }
    }
}
