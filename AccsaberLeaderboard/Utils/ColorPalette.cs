namespace AccsaberLeaderboard.Utils
{
    public static class ColorPalette
    {
        public const string GLOBAL= "#89D0F5";
        public const string COUNTRY = "#FFA893";

        public const string GLOBAL_DIM = "#347BA0"; // dim by 5
        public const string COUNTRY_DIM = "#AA533E"; // dim by 5

        public const string RANK = "#FA0";
        public const string AP = "#A763C4";
        public const string ACC = "#0D0";

        public const string TECH = "#E65454";
        public const string STANDARD = "#53B6FF";
        public const string TRUE = "#39DD85";
        public const string OVERALL = "#FFD23C";

        public const string TECH_DIM = "#600000"; // dim by 8
        public const string STANDARD_DIM = "#003077"; // dim by 8
        public const string TRUE_DIM = "#015500"; // dim by 8
        public const string OVERALL_DIM = "#775004"; // dim by 8

        public const string HIGHLIGHT = "#5643A499";

        public const string LEVEL = "#0F0";
        public const string LEVEL_DIM = "#070"; // dim by 8

        public const string GREY = "#AAA";
        public const string DARK_BLUE = "#012";

        public const string DIMMER = "#000A"; //#33333388


        public const string DEFAULT_COLOR = "#000f";
        public static string GetTitleColor(string title) => title switch
        {
            "Newcomer" => "#6b7280",
            "Apprentice" => "#3b82f6",
            "Adept" => "#10b981",
            "Skilled" => "#cd7f32",
            "Expert" => "#c0c0d0",
            "Master" => "#fbbf24",
            "Grandmaster" => "#8b5cf6",
            "Legend" => "#f97316",
            "Transcendent" => "#22d3ee",
            "Mythic" => "#ef4444",
            "Ascendant" => "#f472b6",
            _ => DEFAULT_COLOR
        };
        public static string GetMilestoneRankColor(string rank) => rank switch
        {
            "bronze" => "#cd7f32",
            "silver" => "#c0c0c0",
            "gold" => "#ffd700",
            "platinum" => "#36cfb0",
            "diamond" => "#b9f2ff",
            "apex" => "#a855f7",
            _ => "#FFF"
        };
    }
}
