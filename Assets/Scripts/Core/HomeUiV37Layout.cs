namespace SeekerAutoChess.Core
{
    /// <summary>
    /// Pixel-locked interaction geometry for the E35 v3.7 home screen.
    /// Coordinates are normalized against the 1080 x 2400 Seeker master.
    /// Static artwork is owned by the art composite; runtime code must only add
    /// data overlays and invisible hit regions inside these declared bounds.
    /// </summary>
    public static class HomeUiV37Layout
    {
        public const string Version = "3.7";
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 2400f;

        public static readonly NormalizedUiRect Profile = new NormalizedUiRect(0.0428f, 0.0214f, 0.3330f, 0.0674f);
        public static readonly NormalizedUiRect Energy = new NormalizedUiRect(0.6112f, 0.0257f, 0.0797f, 0.0428f);
        public static readonly NormalizedUiRect Skr = new NormalizedUiRect(0.7206f, 0.0257f, 0.0844f, 0.0428f);
        public static readonly NormalizedUiRect TideCrystal = new NormalizedUiRect(0.8359f, 0.0257f, 0.1034f, 0.0428f);
        public static readonly NormalizedUiRect Affairs = new NormalizedUiRect(0.7158f, 0.0615f, 0.1034f, 0.0578f);
        public static readonly NormalizedUiRect Warehouse = new NormalizedUiRect(0.8300f, 0.0615f, 0.1118f, 0.0578f);
        public static readonly NormalizedUiRect Season = new NormalizedUiRect(0.2889f, 0.1882f, 0.4210f, 0.0802f);
        public static readonly NormalizedUiRect Island = new NormalizedUiRect(0.1998f, 0.3096f, 0.6005f, 0.3508f);
        public static readonly NormalizedUiRect StatusCard = new NormalizedUiRect(0.1736f, 0.6658f, 0.6504f, 0.1086f);
        public static readonly NormalizedUiRect Expedition = new NormalizedUiRect(0.1736f, 0.7834f, 0.6504f, 0.0529f);
        public static readonly NormalizedUiRect BottomNavigation = new NormalizedUiRect(0f, 0.9107f, 1f, 0.0893f);

        public static NormalizedUiRect BottomNavigationSlot(int index)
        {
            const int slotCount = 5;
            if (index < 0) index = 0;
            if (index >= slotCount) index = slotCount - 1;
            var slotWidth = BottomNavigation.Width / slotCount;
            return new NormalizedUiRect(BottomNavigation.X + slotWidth * index,
                BottomNavigation.Y, slotWidth, BottomNavigation.Height);
        }
    }
}
