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
        internal static readonly Throttler throttler = new(400, 60);
        public const int PAGE_LENGTH = 10;
        public const int FILTER_PAGE_MULT = 10;
        public static async Task<AccsaberScoreData[]> GetScoreData(int page, string hash, BeatmapDifficulty diff)
        {
            return await GetScoreData(page, await GetLeaderboardDifficultyId(hash, diff));
        }
        public static async Task<AccsaberScoreData[]> GetScoreData(int page, string diffId)
        {
            try
            {
                IEnumerable<JToken> scores = await GetLeaderboardScores(diffId, page - 1, PAGE_LENGTH).ConfigureAwait(false); 
                if (scores is null) return null;
                return [.. scores.Select(ConvertToScoreData)];
            }
            catch (Exception e)
            {
                Plugin.Log.Error("Failure to get score data for map.\n");
                Plugin.Log.Debug(e);
                return null;
            }
        }
        public static async Task<(AccsaberScoreData[] scores, int truePage)> GetScoreData(int page, string diffId, Func<JToken, bool> filter, int pageMult = FILTER_PAGE_MULT, int maxCalls = 10)
        {
            try
            {
                List<AccsaberScoreData> outp = new(PAGE_LENGTH);
                int scoresNeeded = PAGE_LENGTH, truePage = page;
                page = (page - 1) / pageMult;
                if (maxCalls <= 0)
                    throw new ArgumentException("Don't call a function then ask it to do nothing.");
                int pageLength = PAGE_LENGTH * pageMult;
                do
                {
                    string dataStr = await CallAPI_String(string.Format(APAPI_LEADERBOARD_DIFF, diffId, page, pageLength), throttler).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(dataStr))
                        throw new ArgumentNullException("The leaderboard api is not returning any data.");

                    JToken response = JToken.Parse(dataStr);
                    if ((bool)response["last"])
                        break;
                    if (((int)response["numberOfElements"]) == 0)
                        continue;

                    IEnumerable<JToken> tokens = response["content"].Children();

                    IEnumerable<AccsaberScoreData> scores = tokens.Where(filter).Select(ConvertToScoreData);
                    int scoreLen = scores.Count();
                    if (scoreLen >= scoresNeeded)
                    {
                        scores = scores.Take(scoresNeeded);
                        pageMult = (int)Math.Ceiling(scores.Last().rank / (float)PAGE_LENGTH); // This is just to update truePage correctly.
                        outp.AddRange(scores);
                    }
                    else
                    {
                        outp.AddRange(scores);
                        scoresNeeded -= scoreLen;
                    }
                    truePage += pageMult;
                    page++;
                    maxCalls--;
                } while (outp.Count < PAGE_LENGTH && maxCalls > 0);
                return ([.. outp], truePage);
            }
            catch (Exception e)
            {
                Plugin.Log.Error("Issue getting filtered score data.\n" + e);
                return default;
            }
        }
        public static AccsaberScoreData ConvertToScoreData(JToken scoreData) =>
            new(GetScore(scoreData), GetUserName(scoreData), GetRank(scoreData), GetFullCombo(scoreData), GetAP(scoreData), GetAcc(scoreData), GetPlayerId(scoreData));
        public static float GetComplexity(JToken diffData) => (float)(diffData["complexity"] ?? 0f);
        public static string GetSongName(JToken diffData) => diffData["songName"].ToString();
        public static string GetDiffName(JToken diffData) => diffData["difficulty"].ToString();
        public static string GetLeaderboardId(JToken diffData) => diffData["leaderboardId"].ToString();
        public static string GetHash(JToken diffData) => diffData["songHash"].ToString();
        public static bool MapIsUsable(JToken diffData) => diffData is not null && GetComplexity(diffData) > 0;
        public static bool AreRatingsNull(JToken diffData) => diffData["complexity"] is null;
        public static int GetMaxScore(JToken diffData) => (int)(diffData["maxScore"] ?? 0);
        public static int GetRank(JToken scoreData) => (int)scoreData["rank"];
        public static string GetUserName(JToken scoreData) => scoreData["userName"].ToString();
        public static float GetAcc(JToken scoreData) => (float)scoreData["accuracy"];
        public static int GetMistakes(JToken scoreData) {
            int outp = (int)scoreData["misses"] + (int)scoreData["badCuts"];
            if (scoreData["bombCuts"] is not null) outp += (int)scoreData["bombCuts"];
            if (scoreData["wallHits"] is not null) outp += (int)scoreData["wallHits"];
            return outp;
        }
        public static bool GetFullCombo(JToken scoreData) => GetMistakes(scoreData) == 0;
        public static float GetAP(JToken scoreData) => (float)scoreData["ap"];
        public static int GetScore(JToken scoreData) => (int)scoreData["score"];
        public static string GetCountry(JToken scoreData) => scoreData["country"]?.ToString();
        public static string GetPlayerId(JToken scoreData) => scoreData["userId"]?.ToString();
        public static string GetPlayerAvatar(JToken playerData) => playerData["avatarUrl"]?.ToString();
        public static string GetPlayerTitle(JToken playerData) => playerData["levelTitle"]?.ToString();
        public static LevelTitle GetAndConvertPlayerTitle(JToken playerData) => (LevelTitle)Enum.Parse(typeof(LevelTitle), GetPlayerTitle(playerData));
        public static int GetPlayerLevel(JToken playerData) => (int)playerData["level"];
        public static string GetPlayerName(JToken playerData) => playerData["name"]?.ToString();
        public static float GetPlayerXPPercent(JToken playerData) => (float)playerData["progressPercent"];
        public static JToken GetPlayerStats(JToken playerData, APCategory category)
        {
            string id = CategoryIdToReloadedCategory(category.ToString());
            if (playerData is null) Plugin.Log.Warn("playerData is null.");
            if (id is null) Plugin.Log.Warn("id is null.");
            return playerData["statistics"]?.Children().FirstOrDefault(token => id.Equals(token["categoryId"]?.ToString()));
        }
        public static int GetGlobalRank(JToken statsData) => (int)statsData["ranking"];
        public static int GetCountryRank(JToken statsData) => (int)statsData["countryRanking"];
        public static async Task<int> GetMaxScore(string hash, int diffNum) =>
            (int)JToken.Parse(await CallAPI_String(string.Format(APAPI_HASH_DIFF, hash, DiffNumToReloadedDiff(diffNum)), throttler))["difficulties"].Children().First()["maxScore"];
        public static async Task<string> GetHashData(string hash, int diffNum) =>
            await CallAPI_String(string.Format(APAPI_HASH_DIFF, hash, DiffNumToReloadedDiff(diffNum)), throttler, true, maxRetries: 1).ConfigureAwait(false);
        public static async Task<JToken> GetScoreData(string userId, string hash, BeatmapDifficulty diff, CancellationToken ct = default)
        {
            string reloadedDiff = DiffNumToReloadedDiff(FromDiff(diff));
            string dataStr = await CallAPI_String(string.Format(APAPI_SCORE, userId, hash.ToLower(), reloadedDiff), throttler, ct: ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(dataStr)) return null;
            return JToken.Parse(dataStr);
        }

        public static async Task<string> GetLeaderboardDifficultyId(string hash, BeatmapDifficulty diff, CancellationToken ct = default)
        {
            if (ct.IsCancellationRequested) return null;
            string diffStr = DiffNumToReloadedDiff(FromDiff(diff));
            try
            {
                string dataStr = await CallAPI_String(string.Format(APAPI_HASH_DIFF, hash, diffStr), throttler, true, ct: ct).ConfigureAwait(false);

                if (dataStr is null || dataStr.Equals(string.Empty)) return null;
                JToken diffData = JToken.Parse(dataStr)["difficulties"].Children().FirstOrDefault();
                if (diffData is null) return null;

                return diffData["id"].ToString();
            }
            catch (Exception ex)
            {
                Plugin.Log.Info($"Issue URL: {string.Format(APAPI_HASH_DIFF, hash, diffStr)}");
                Plugin.Log.Error("There was an error getting a difficulty id: " + ex);
                return null;
            }

        } 
        public static async Task<IEnumerable<JToken>> GetLeaderboardScores(string hash, BeatmapDifficulty diff, int page = 0, int count = 10, CancellationToken ct = default)
        {
            return await GetLeaderboardDifficultyId(hash, diff, ct).ContinueWith(task => GetLeaderboardScores(task.Result, page, count, ct).GetAwaiter().GetResult());
        }
        public static async Task<IEnumerable<JToken>> GetLeaderboardScores(string difficulty_id, int page = 0, int count = 10, CancellationToken ct = default)
        {
            string dataStr = await CallAPI_String(string.Format(APAPI_LEADERBOARD_DIFF, difficulty_id, page, count), throttler, true, ct: ct).ConfigureAwait(false);
            if (dataStr is null || dataStr.Equals(string.Empty)) return null;

            return JToken.Parse(dataStr)["content"].Children();
        }
        public static async Task<JToken> GetPlayerInfo(string userId, bool stats, CancellationToken ct = default)
        {
            string dataStr = await CallAPI_String(string.Format(APAPI_PLAYERID, userId, stats.ToString().ToLower()), throttler, false, ct: ct).ConfigureAwait(false);

            return string.IsNullOrEmpty(dataStr) ? null : JToken.Parse(dataStr);
        }
    }
}
