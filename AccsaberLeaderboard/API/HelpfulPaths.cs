using AccsaberLeaderboard.Models;
using System;

namespace AccsaberLeaderboard.API
{
    public static class HelpfulPaths
    {
        //Docs: https://api.accsaberreloaded.com/v1/docs
        // Category ID: b0000000-0000-0000-0000-000000000003 for Tech, 2 = Standard, 1 = True, none for overall.
        // Score endpoint example: https://api.accsaberreloaded.com/v1/users/76561198306905129/scores/by-hash/2a579bb1a3efa58af7640f9663c972ee84fea44a?difficulty=EXPERT&characteristic=Standard
        // Diff endpoint example: https://api.accsaberreloaded.com/v1/maps/hash/2A579BB1A3EFA58AF7640F9663C972EE84FEA44A?difficulty=EXPERT
        public const string APAPI = "https://api.accsaberreloaded.com/v1/";
        public const string APAPI_TEST = APAPI + "health/ping"; //no params
        public const string APAPI_PLAYERID = APAPI + "users/{0}?statistics={1}"; //user_id, true or false for whether to include statistics in the response
        public const string APAPI_PLAYERID_CATEGORY = APAPI + "users/{0}/statistics?category={1}"; //user_id, category (overall, true_acc, standard_acc, tech_acc)
        public const string APAPI_SCORE = APAPI + "users/{0}/scores/by-hash/{1}?difficulty={2}&characteristic=Standard"; //user_id, hash, difficulty IN CAPS
        public const string APAPI_SCORES = APAPI + "users/{0}/scores?page={1}&size={2}"; //user_id, page (zero indexed), count
        public const string APAPI_CATEGORY_SCORES = "users/{0}/scores?categoryId={1}&page={2}&size={3}"; // user_id, category_id, page (zero indexed), count
        public const string APAPI_HASH = APAPI + "maps/hash/{0}"; //hash
        public const string APAPI_HASH_DIFF = APAPI + "maps/hash/{0}?difficulty={1}"; //hash, difficulty IN CAPS
        public const string APAPI_LEADERBOARD_DIFF = APAPI + "maps/difficulties/{0}/scores?page={1}&size={2}"; //diff_id, page (zero indexed), count
        public const string APAPI_LEADERBOARD_DIFF_COUNTRY = APAPI + "maps/difficulties/{0}/scores?country={1}&page={2}&size={3}"; //diff_id, country, page (zero indexed), count

        public const string APAPI_MILESTONE = APAPI + "users/{0}/milestones?page={1}&size={2}"; //user_id, page, size
        public const string APAPI_MILESTONES = APAPI + "levels"; //no params
        public const string APAPI_MILESTONE_COMPLETE = APAPI + "users/{0}/milestones/completed"; //user_id
        public const string APAPI_MILESTONE_INCOMPLETE = APAPI + "users/{0}/milestones/uncompleted"; //user_id

        //user_id, RelationType, one of: ["outgoing", "incoming"], page (zero indexed), count
        public const string APAPI_RELATIONS = APAPI + "users/{0}/relations?type={1}&direction={2}&page={3}&size={4}";
        public const string APAPI_AUTH_SET_RELATION = APAPI + "users/me/relations"; // POST endpoint
        public const string APAPI_AUTH_DELETE_RELATION = APAPI_AUTH_SET_RELATION + "/{0}"; // DELETE endpoint: relationId
        public const string APAPI_AUTH_GET_RELATIONS = APAPI_AUTH_SET_RELATION + "?type={0}&page={1}&size={2}"; // RelationType, page (zero indexed), size
        public const string APAPI_AUTH_GET_RELATIONS_ALL = APAPI_AUTH_SET_RELATION + "?page={0}&size={1}"; // page (zero indexed), size

        public const string APAPI_AUTH_GET_SETTINGS = APAPI + "users/me/settings/{0}"; // group ("privacy", "appearance", etc)

        public const string APAPI_AUTH = APAPI + "auth/ingame"; // POST endpoint

        public const string APAPI_WEBSOCKET = "wss://accsaberreloaded.com/ws/scores";

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

        public static LeaderboardDisplayType Convert(this RelationType rt) => (LeaderboardDisplayType)(int)Math.Pow(2, (int)rt + 2);
        public static RelationType Convert(this LeaderboardDisplayType ldt)
        {
            RelationType outp = (RelationType)(int)(Math.Log((int)ldt, 2) - 2);

            if (outp < RelationType.follower || outp > RelationType.blocked)
                throw new ArgumentException("The given display type must be able to be converted to RelationType");

            return outp;
        }
        public enum RelationType
        {
            follower, rival, blocked
        }
    }
}
