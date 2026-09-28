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
    /// Exact dynamic-overlay geometry supplied with the E36 v3.8 runtime split.
    /// These are reference pixels on the locked 1080 x 2400 Seeker canvas.
    /// </summary>
    public static class HomeUiV38Layout
    {
        public const string Version = "3.8";
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 2400f;

        public static readonly ReferenceUiRect PlayerName = new ReferenceUiRect(176f, 65f, 130f, 46f);
        public static readonly ReferenceUiRect PlayerPower = new ReferenceUiRect(217f, 118f, 152f, 40f);
        public static readonly ReferenceUiRect EnergyValue = new ReferenceUiRect(704f, 62f, 40f, 46f);
        public static readonly ReferenceUiRect SkrValue = new ReferenceUiRect(851f, 62f, 40f, 46f);
        public static readonly ReferenceUiRect TideValue = new ReferenceUiRect(1000f, 62f, 44f, 46f);
        public static readonly ReferenceUiRect Season = new ReferenceUiRect(406f, 470f, 270f, 73f);
        public static readonly ReferenceUiRect Chapter = new ReferenceUiRect(317f, 546f, 446f, 68f);
        public static readonly ReferenceUiRect IslandName = new ReferenceUiRect(457f, 652f, 167f, 51f);
        public static readonly ReferenceUiRect StatusTitle = new ReferenceUiRect(375f, 1664f, 333f, 62f);
        public static readonly ReferenceUiRect StoneCount = new ReferenceUiRect(356f, 1777f, 101f, 44f);
        public static readonly ReferenceUiRect OilCount = new ReferenceUiRect(566f, 1777f, 100f, 44f);
        public static readonly ReferenceUiRect LensCount = new ReferenceUiRect(772f, 1777f, 101f, 44f);
        public static readonly ReferenceUiRect ExpeditionLabel = new ReferenceUiRect(399f, 1911f, 286f, 77f);
        public static readonly ReferenceUiRect ExpeditionButton = new ReferenceUiRect(186f, 1880f, 708f, 123f);
        public static readonly ReferenceUiRect BottomNavigation = new ReferenceUiRect(0f, 2187f, 1080f, 213f);

        public static ReferenceUiRect BottomNavigationSlot(int index)
        {
            const int slotCount = 5;
            if (index < 0) index = 0;
            if (index >= slotCount) index = slotCount - 1;
            var slotWidth = BottomNavigation.Width / slotCount;
            return new ReferenceUiRect(BottomNavigation.X + slotWidth * index,
                BottomNavigation.Y, slotWidth, BottomNavigation.Height);
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
    }
}
