namespace AccsaberLeaderboard.Models
{
    public enum LeaderboardDisplayType
    {
        Global = 0, Country = 1, Friends = 2, Followed = 4, Rivals = 8, Relations = Friends | Followed | Rivals
    }
}
