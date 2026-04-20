using System;

namespace AccsaberLeaderboard.API
{
    internal static class HelpfulPaths
    {
        public static readonly string SSAPI = "https://scoresaber.com/api/";
        //UNRANKED: https://scoresaber.com/api/leaderboard/by-hash/bdacecbf446f0f066f4189c7fe1a81c6d3664b90/info?difficulty=5
        //RANKED: https://scoresaber.com/api/leaderboard/by-hash/7c44cdc1e33e2f5f929867b29ceb3860c3716ddc/info?difficulty=5
        public static readonly string SSAPI_HASH = "https://scoresaber.com/api/leaderboard/by-hash/{0}/{1}?difficulty={2}&page={3}"; //hash, either "info" or "scores", diff_number, pageNumber
        public static readonly string SSAPI_USERID = "https://scoresaber.com/api/player/{0}/{1}"; //user_id, either "basic", "full", or "scores"
        public static readonly string SSAPI_DIFFS = "https://scoresaber.com/api/leaderboard/get-difficulties/{0}"; //hash
        public static readonly string SSAPI_LEADERBOARDID = "https://scoresaber.com/api/leaderboard/by-id/{0}/{1}"; //leaderboard_id, either "info" or "scores"
        public static readonly string SSAPI_PLAYERSCORES = "https://scoresaber.com/api/player/{0}/scores?limit={2}&sort=top&page={1}"; //user_id, page, count
        public static readonly string SSAPI_PLAYER_FILTER = "https://scoresaber.com/api/players?page={0}"; //page (count is always 50, sorted by rank)

        //Docs: https://api.accsaberreloaded.com/v1/docs
        // Category ID: b0000000-0000-0000-0000-000000000003 for Tech, 2 = Standard, 1 = True, none for overall.
        // Score endpoint example: https://api.accsaberreloaded.com/v1/users/76561198306905129/scores/by-hash/2a579bb1a3efa58af7640f9663c972ee84fea44a?difficulty=EXPERT&characteristic=Standard
        // Diff endpoint example: https://api.accsaberreloaded.com/v1/maps/hash/2A579BB1A3EFA58AF7640F9663C972EE84FEA44A?difficulty=EXPERT
        public static readonly string APAPI = "https://api.accsaberreloaded.com/v1/";
        public static readonly string APAPI_TEST = APAPI + "health/ping"; //no params
        public static readonly string APAPI_PLAYERID = APAPI + "users/{0}?statistics={1}"; //user_id, true or false for whether to include statistics in the response
        public static readonly string APAPI_PLAYERID_CATEGORY = APAPI + "users/{0}/statistics?category={1}"; //user_id, category (overall, true_acc, standard_acc, tech_acc)
        public static readonly string APAPI_SCORE = APAPI + "users/{0}/scores/by-hash/{1}?difficulty={2}&characteristic=Standard"; //user_id, hash, difficulty IN CAPS
        public static readonly string APAPI_SCORES = APAPI + "users/{0}/scores?page={1}&size={2}"; //user_id, page (zero indexed), count
        public static readonly string APAPI_CATEGORY_SCORES = "users/{0}/scores?categoryId={1}&page={2}&size={3}"; // user_id, category_id, page (zero indexed), count
        public static readonly string APAPI_HASH = APAPI + "maps/hash/{0}"; //hash
        public static readonly string APAPI_HASH_DIFF = APAPI + "maps/hash/{0}?difficulty={1}"; //hash, difficulty IN CAPS
        public static readonly string APAPI_LEADERBOARD_DIFF = APAPI + "maps/difficulties/{0}/scores?page={1}&size={2}"; //diff_id, page (zero indexed), count
        public static readonly string APAPI_PLAYER_LEVEL = APAPI + "users/{0}/level"; //user_id


        public static string DiffNumToReloadedDiff(int diffNum) => diffNum switch
        {
            1 => "EASY",
            3 => "NORMAL",
            5 => "HARD",
            7 => "EXPERT",
            9 => "EXPERT_PLUS",
            _ => throw new ArgumentException("Invalid difficulty number. Must be one of the following: 1, 3, 5, 7, 9.")
        };
        public static int ReloadedDiffToDiffNum(string diff) => diff switch
        {
            "EASY" => 1,
            "NORMAL" => 3,
            "HARD" => 5,
            "EXPERT" => 7,
            "EXPERT_PLUS" => 9,
            _ => throw new ArgumentException("Invalid difficulty string. Must be one of the following: EASY, NORMAL, HARD, EXPERT, EXPERT_PLUS.")
        };
        public static string ReloadedCategoryToCategoryId(string category) => category switch
        {
            "b0000000-0000-0000-0000-000000000001" => "True",
            "b0000000-0000-0000-0000-000000000002" => "Standard",
            "b0000000-0000-0000-0000-000000000003" => "Tech",
            "b0000000-0000-0000-0000-000000000005" => "Overall",
            _ => null
        };
        public static string CategoryIdToReloadedCategory(string category) => category switch
        {
            "True" => "b0000000-0000-0000-0000-000000000001",
            "Standard" => "b0000000-0000-0000-0000-000000000002",
            "Tech" => "b0000000-0000-0000-0000-000000000003",
            "Overall" => "b0000000-0000-0000-0000-000000000005",
            _ => null
        };
        public static int FromDiff(BeatmapDifficulty diff) => (int)diff * 2 + 1;
        public static BeatmapDifficulty ToDiff(int diffNum) => (BeatmapDifficulty)((diffNum - 1) / 2);
    }
}
