using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AccsaberLeaderboard.Models;

using static AccsaberLeaderboard.API.APIHandler;
using static AccsaberLeaderboard.API.HelpfulPaths;

namespace AccsaberLeaderboard.API
{
    internal static class AccsaberAPI
    {
        private static readonly Throttler throttler = new(400, 60);
        public static async Task<LeaderboardTableView.ScoreData[]> GetScoreData(int page, string hash, BeatmapDifficulty diff)
        {
            try
            {
                IEnumerable<JToken> scores = await GetLeaderboardScores(hash, diff.ToString(), page - 1, 10).ConfigureAwait(false); // page is zero indexed while the given page is one indexed
                return [.. scores.Select(score => new LeaderboardTableView.ScoreData(GetScore(score), GetPlayerName(score), GetRank(score), (bool)score["fullCombo"]))];
            } catch (Exception e)
            {
                Plugin.Log.Error("Failure to get score data for map.");
                Plugin.Log.Debug($"Hash: {hash}, diff: {diff}\n{e}");
                return null;
            }
        }
        public static float GetComplexity(JToken diffData) => (float)(diffData["complexity"] ?? 0f);
        public static string GetSongName(JToken diffData) => diffData["songName"].ToString();
        public static string GetDiffName(JToken diffData) => diffData["difficulty"].ToString();
        public static string GetLeaderboardId(JToken diffData) => diffData["leaderboardId"].ToString();
        public static string GetHash(JToken diffData) => diffData["songHash"].ToString();
        public static bool MapIsUsable(JToken diffData) => diffData is not null && GetComplexity(diffData) > 0;
        public static bool AreRatingsNull(JToken diffData) => diffData["complexity"] is null;
        public static int GetMaxScore(JToken diffData) => (int)(diffData["maxScore"] ?? 0);
        public static int GetRank(JToken scoreData) => (int)scoreData["rank"];
        public static string GetPlayerName(JToken scoreData) => scoreData["userName"].ToString();
        public static float GetAcc(JToken scoreData) => (float)scoreData["accuracy"];
        public static async Task<int> GetMaxScore(string hash, int diffNum, string modeName) =>
            (int)JToken.Parse(await CallAPI_String(string.Format(APAPI_HASH_DIFF, hash, DiffNumToReloadedDiff(diffNum))))["difficulties"].Children().First()["maxScore"];
        public static async Task<string> GetHashData(string hash, int diffNum) =>
            await CallAPI_String(string.Format(APAPI_HASH_DIFF, hash, DiffNumToReloadedDiff(diffNum)), throttler, true, maxRetries: 1).ConfigureAwait(false);
        public static async Task<JToken> GetScoreData(string userId, string hash, string diff, string mode, bool quiet = false, CancellationToken ct = default)
        {
            string reloadedDiff = DiffNumToReloadedDiff(FromDiff((BeatmapDifficulty)Enum.Parse(typeof(BeatmapDifficulty), diff)));
            string dataStr = await CallAPI_String(string.Format(APAPI_SCORE, userId, hash.ToLower(), reloadedDiff), ct: ct).ConfigureAwait(false);
            if (dataStr is null || dataStr.Equals(string.Empty)) return null;
            return JToken.Parse(dataStr);
        }
        public static float GetPP(JToken scoreData) => (float)scoreData["ap"];
        public static int GetScore(JToken scoreData) => (int)scoreData["baseScore"];
        public static async Task<float> GetProfilePP(string userId)
        {
            return (float)JToken.Parse(await CallAPI_String(string.Format(APAPI_PLAYERID, userId)).ConfigureAwait(false))?["ap"];
        }
        public static async Task<float> GetProfilePP(string userId, APCategory accSaberType)
        {
            return (float)JToken.Parse(await CallAPI_String(string.Format(APAPI_PLAYERID_CATEGORY, userId, accSaberType.ToString().ToLower() + "_acc")).ConfigureAwait(false))?["ap"];
        }
        public static async Task<IEnumerable<JToken>> GetLeaderboardScores(string hash, string diff, int page = 0, int count = 10, CancellationToken ct = default)
        {
            if (ct.IsCancellationRequested) return null;
            diff = DiffNumToReloadedDiff(FromDiff((BeatmapDifficulty)Enum.Parse(typeof(BeatmapDifficulty), diff)));
            string dataStr = await CallAPI_String(string.Format(APAPI_HASH_DIFF, hash, diff), ct: ct).ConfigureAwait(false);

            if (dataStr is null || dataStr.Equals(string.Empty)) return null;
            JToken diffData = JToken.Parse(dataStr)["difficulties"].Children().First();

            dataStr = await CallAPI_String(string.Format(APAPI_LEADERBOARD_DIFF, diffData["id"].ToString(), page, count), ct: ct).ConfigureAwait(false);
            if (dataStr is null || dataStr.Equals(string.Empty)) return null;

            return JToken.Parse(dataStr)["content"].Children();
        }

    }
}
