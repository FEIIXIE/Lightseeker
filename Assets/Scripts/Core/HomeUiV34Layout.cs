namespace SeekerAutoChess.Core
{
    public struct NormalizedUiRect
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public NormalizedUiRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    public static class HomeUiV34Layout
    {
        public const string Version = "3.4";
        public const float ReferenceWidth = 841f;
        public const float ReferenceHeight = 1870f;

        public static readonly NormalizedUiRect PlayerProfile = new NormalizedUiRect(0.045f, 0.022f, 0.31f, 0.058f);
        public static readonly NormalizedUiRect ResourceRow = new NormalizedUiRect(0.60f, 0.024f, 0.355f, 0.042f);
        public static readonly NormalizedUiRect UtilityActions = new NormalizedUiRect(0.73f, 0.065f, 0.225f, 0.052f);
        public static readonly NormalizedUiRect SeasonEntry = new NormalizedUiRect(0.30f, 0.194f, 0.40f, 0.044f);
        public static readonly NormalizedUiRect ChapterHeading = new NormalizedUiRect(0.27f, 0.228f, 0.46f, 0.054f);
        public static readonly NormalizedUiRect IslandTitle = new NormalizedUiRect(0.32f, 0.276f, 0.36f, 0.035f);
        public static readonly NormalizedUiRect IslandHero = new NormalizedUiRect(0.19f, 0.315f, 0.62f, 0.33f);
        public static readonly NormalizedUiRect RepairCard = new NormalizedUiRect(0.175f, 0.67f, 0.65f, 0.105f);
        public static readonly NormalizedUiRect ExpeditionButton = new NormalizedUiRect(0.175f, 0.785f, 0.65f, 0.052f);
        public static readonly NormalizedUiRect BottomNavigation = new NormalizedUiRect(0f, 0.912f, 1f, 0.088f);
    }
}
