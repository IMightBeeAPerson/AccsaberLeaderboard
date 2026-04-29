namespace AccsaberLeaderboard.Utils
{
    public static class MiscConsts
    {
        public const double DAYS_YEAR = 365.2422;

        public const double SECONDS_MILLI = 1e-3; // 0.001
        public const int SECONDS_MINUTE = 60;
        public const int SECONDS_HOUR = SECONDS_MINUTE * 60; // 3,600
        public const int SECONDS_DAY = SECONDS_HOUR * 24; // 86,400
        public const int SECONDS_WEEK = SECONDS_DAY * 7; // 604,800
        public const int SECONDS_YEAR = (int)(SECONDS_DAY * DAYS_YEAR); // 31,556,926

    }
}
