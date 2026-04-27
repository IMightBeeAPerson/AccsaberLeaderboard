using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AccsaberLeaderboard.Models;

using static AccsaberLeaderboard.API.APIHandler;
using static AccsaberLeaderboard.API.HelpfulPaths;
using System.Diagnostics.CodeAnalysis;

namespace AccsaberLeaderboard.API
{
#nullable enable
    internal static class AccsaberAPI
    {
        internal static readonly Throttler throttler = new(400, 60);
        public const int PAGE_LENGTH = 10;
        public const int FILTER_PAGE_MULT = 10;
        public static async Task<AccsaberScoreData[]?> GetScoreData(int page, string hash, BeatmapDifficulty diff)
        {
            string? diffId = await GetLeaderboardDifficultyId(hash, diff);
            if (diffId is null) return null;
            return await GetScoreData(page, diffId);
        }
        public static async Task<AccsaberScoreData[]?> GetScoreData(int page, string diffId, string? country = null)
        {
            try
            {
                IEnumerable<JToken>? scores = await (country is null ? GetLeaderboardScores(diffId, page - 1, PAGE_LENGTH) :
                    GetLeaderboardScores(diffId, country, page - 1, PAGE_LENGTH)).ConfigureAwait(false); 
                if (scores is null) return null;
                return [.. scores.Select(token => ConvertToScoreData(new((JObject)token)))];
            }
            catch (Exception e)
            {
                Plugin.Log.Error("Failure to get score data for map.\n");
                Plugin.Log.Debug(e);
                return null;
            }
        }
        public static async Task<(AccsaberScoreData[] scores, int truePage)> GetScoreData(int page, string diffId, Func<ScoreInfoToken, bool> filter, int pageMult = FILTER_PAGE_MULT, int maxCalls = 10)
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
                    if ((bool)response["empty"])
                        break;


                    IEnumerable<ScoreInfoToken> tokens = response["content"].Children().Select(token => new ScoreInfoToken((JObject)token));

                    IEnumerable<AccsaberScoreData> scores = tokens.Where(filter).Select(ConvertToScoreData)!;
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

                    if ((bool)response["last"])
                        break;

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
        [return: NotNullIfNotNull(nameof(scoreData))]
        public static AccsaberScoreData? ConvertToScoreData(ScoreInfoToken? scoreData)
        {
            if (scoreData is null) return null;
            return new(scoreData);
        }
        public static async Task<List<MilestoneInfoToken>?> GetMilestoneData(string userId, Func<MilestoneInfoToken, bool>? filter = null, Comparison<MilestoneInfoToken>? sorter = null, int pageMult = FILTER_PAGE_MULT)
        {
            int page = 0;
            List<MilestoneInfoToken> outp = [];
            int pageLen = PAGE_LENGTH * pageMult;
            while (true)
            {
                string dataStr = await CallAPI_String(string.Format(APAPI_MILESTONE, userId, page, pageLen)).ConfigureAwait(false);
                if (string.IsNullOrEmpty(dataStr)) return null;
                JToken response = JToken.Parse(dataStr);
                if ((bool)response["last"])
                    break;
                IEnumerable<MilestoneInfoToken> data = response["content"].Children().Select(token => new MilestoneInfoToken((JObject)token));
                if (filter is not null)
                    data = data.Where(filter);
                outp.AddRange(data);
                ++page;
            }
            if (sorter is not null)
                outp.Sort(sorter);
            return outp;
        }
        public static async Task<List<MilestoneInfoToken>?> GetMilestoneData(string userId, bool completed, Func<MilestoneInfoToken, bool>? filter = null, Comparison<MilestoneInfoToken>? sorter = null)
        {
            string apapiFormat = completed ? APAPI_MILESTONE_COMPLETE : APAPI_MILESTONE_INCOMPLETE;

            string dataStr = await CallAPI_String(string.Format(apapiFormat, userId)).ConfigureAwait(false);
            if (string.IsNullOrEmpty(dataStr)) 
                return null;

            JToken response = JToken.Parse(dataStr);
            IEnumerable<MilestoneInfoToken>? data = response?.Children().Select(token => new MilestoneInfoToken((JObject)token));
            if (data is null)
                return null;

            if (filter is not null)
                data = data.Where(filter);

            List<MilestoneInfoToken> outp = [.. data];
            if (sorter is not null)
                outp.Sort(sorter);

            return outp;
        }
        #region Diff Info Getters
        public static float GetComplexity(DifficultyInfoToken diffData) => (float)(diffData["complexity"] ?? 0f);
        public static string GetSongName(DifficultyInfoToken diffData) => diffData["songName"].ToString();
        public static string GetDiffName(DifficultyInfoToken diffData) => diffData["difficulty"].ToString();
        public static string GetLeaderboardId(DifficultyInfoToken diffData) => diffData["leaderboardId"].ToString();
        public static string GetDifficultyId(DifficultyInfoToken diffData) => diffData["id"].ToString();
        public static string GetHash(DifficultyInfoToken diffData) => diffData["songHash"].ToString();
        public static bool MapIsUsable(DifficultyInfoToken diffData) => diffData is not null && GetComplexity(diffData) > 0;
        public static bool AreRatingsNull(DifficultyInfoToken diffData) => diffData["complexity"] is null;
        public static int GetMaxScore(DifficultyInfoToken diffData) => (int)(diffData["maxScore"] ?? 0);
        public static string GetCategoryId(DifficultyInfoToken diffData) => diffData["categoryId"]!.ToString();

        #endregion
        #region Score Info Getters

        public static int GetRank(ScoreInfoToken scoreData) => (int)scoreData["rank"];
        public static string GetUserName(ScoreInfoToken scoreData) => scoreData["userName"].ToString();
        public static float GetAcc(ScoreInfoToken scoreData) => (float)scoreData["accuracy"];
        public static int GetMistakes(ScoreInfoToken scoreData) {
            int outp = (int)scoreData["misses"] + (int)scoreData["badCuts"];
            if (scoreData["bombCuts"] is not null) outp += (int)scoreData["bombCuts"];
            if (scoreData["wallHits"] is not null) outp += (int)scoreData["wallHits"];
            return outp;
        }
        public static bool GetFullCombo(ScoreInfoToken scoreData) => GetMistakes(scoreData) == 0;
        public static float GetAP(ScoreInfoToken scoreData) => (float)scoreData["ap"];
        public static int GetScore(ScoreInfoToken scoreData) => (int)scoreData["score"];
        public static string GetCountry(ScoreInfoToken scoreData) => scoreData["country"]!.ToString();
        public static string GetPlayerId(ScoreInfoToken scoreData) => scoreData["userId"]!.ToString();
        public static string GetPlayerName(ScoreInfoToken scoreData) => scoreData["userName"]!.ToString();
        public static string GetPlayerAvatar(ScoreInfoToken scoreData) => scoreData["avatarUrl"]!.ToString();
        public static DateTime GetScoreTimeSet(ScoreInfoToken scoreData) => (DateTime)scoreData["timeSet"];
        public static float GetWeightedAP(ScoreInfoToken scoreData) => (float)scoreData["weightedAp"];
        public static float GetXpGained(ScoreInfoToken scoreData) => (float)scoreData["xpGained"];

        #endregion
        #region Player Info Getters

        public static string GetPlayerAvatar(PlayerInfoToken playerData) => playerData["avatarUrl"]!.ToString();
        public static LevelInfoToken GetPlayerLevelData(PlayerInfoToken playerData) => new((JObject)playerData["levelData"]);
        public static string GetPlayerName(PlayerInfoToken playerData) => playerData["name"]!.ToString();
        public static StatsInfoToken? GetPlayerStats(PlayerInfoToken playerData, APCategory category)
        {
            string id = CategoryIdToReloadedCategory(category.ToString());
            return playerData["statistics"]?.Children().FirstOrDefault(token => id.Equals(token["categoryId"]?.ToString())) is not JObject obj ? null : new(obj);
        }

        #endregion
        #region Level Info Getters

        public static int GetLevel(LevelInfoToken levelData) => (int)levelData["level"];
        public static string GetTitle(LevelInfoToken levelData) => levelData["title"]!.ToString();
        public static float GetCurrentLevelXp(LevelInfoToken levelData) => (float)levelData["xpForCurrentLevel"];
        public static float GetNextLevelXp(LevelInfoToken levelData) => (float)levelData["xpForNextLevel"];
        public static float GetProgress(LevelInfoToken levelData) => (float)levelData["progressPercent"];

        #endregion
        #region Stat Info Getters

        public static float GetAP(StatsInfoToken statsData) => (float)statsData["ap"];
        public static int GetGlobalRank(StatsInfoToken statsData) => (int)statsData["ranking"];
        public static int GetCountryRank(StatsInfoToken statsData) => (int)statsData["countryRanking"];

        #endregion
        #region Milestone Info Getters

        public static float GetProgress(MilestoneInfoToken milestoneData) => (float)milestoneData["normalizedProgress"];
        public static float GetCalculatedProgress(MilestoneInfoToken milestoneData) => 
            AccsaberMilestoneData.AccsaberMilestoneDataInfo.CalcProgress(GetTarget(milestoneData), GetProgressValue(milestoneData));
        public static float GetTarget(MilestoneInfoToken milestoneData) => (float)milestoneData["targetValue"];
        public static float GetProgressValue(MilestoneInfoToken milestoneData) => (float)(milestoneData["progress"] ?? 0f);
        public static string GetTier(MilestoneInfoToken milestoneData) => milestoneData["tier"]!.ToString();
        public static string GetTitle(MilestoneInfoToken milestoneData) => milestoneData["title"]!.ToString();
        public static string GetDescription(MilestoneInfoToken milestoneData) => milestoneData["description"]!.ToString();
        public static string GetId(MilestoneInfoToken milestoneData) => milestoneData["milestoneId"]!.ToString();
        public static AccsaberMilestoneData WrapData(MilestoneInfoToken milestoneData) => new(GetTarget(milestoneData), GetProgressValue(milestoneData),
            GetTier(milestoneData), GetTitle(milestoneData), GetDescription(milestoneData), GetId(milestoneData));

        #endregion
        public static async Task<int> GetMaxScore(string hash, int diffNum) =>
            (int)JToken.Parse(await CallAPI_String(string.Format(APAPI_HASH_DIFF, hash, DiffNumToReloadedDiff(diffNum)), throttler))["difficulties"].Children().First()["maxScore"];
        public static async Task<string> GetHashData(string hash, int diffNum) =>
            await CallAPI_String(string.Format(APAPI_HASH_DIFF, hash, DiffNumToReloadedDiff(diffNum)), throttler, true, maxRetries: 1).ConfigureAwait(false);
        public static async Task<ScoreInfoToken?> GetScoreData(string userId, string hash, BeatmapDifficulty diff, CancellationToken ct = default)
        {
            string reloadedDiff = DiffNumToReloadedDiff(FromDiff(diff));
            string dataStr = await CallAPI_String(string.Format(APAPI_SCORE, userId, hash.ToLower(), reloadedDiff), throttler, true, ct: ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(dataStr)) return null;
            return new(JObject.Parse(dataStr));
        }
        public static async Task<DifficultyInfoToken?> GetLeaderboard(string hash, BeatmapDifficulty diff, CancellationToken ct = default)
        {
            if (ct.IsCancellationRequested) return null;
            string diffStr = DiffNumToReloadedDiff(FromDiff(diff));
            try
            {
                string dataStr = await CallAPI_String(string.Format(APAPI_HASH_DIFF, hash, diffStr), throttler, true, ct: ct).ConfigureAwait(false);

                if (dataStr is null || dataStr.Equals(string.Empty)) return null;
                if (JToken.Parse(dataStr)["difficulties"].Children().FirstOrDefault() is not JObject diffData) return null;

                return new(diffData);
            }
            catch (Exception ex)
            {
                Plugin.Log.Info($"Issue URL: {string.Format(APAPI_HASH_DIFF, hash, diffStr)}");
                Plugin.Log.Error("There was an error getting a difficulty id: " + ex);
                return null;
            }
        }
        public static async Task<string?> GetLeaderboardDifficultyId(string hash, BeatmapDifficulty diff, CancellationToken ct = default)
        {
            DifficultyInfoToken? diffInfo = await GetLeaderboard(hash, diff, ct);
            if (diffInfo is null) return null;
            return GetDifficultyId(diffInfo);
        } 
        public static async Task<IEnumerable<ScoreInfoToken>?> GetLeaderboardScores(string difficulty_id, int page = 0, int count = 10, CancellationToken ct = default)
        {
            string dataStr = await CallAPI_String(string.Format(APAPI_LEADERBOARD_DIFF, difficulty_id, page, count), throttler, true, ct: ct).ConfigureAwait(false);
            if (dataStr is null || dataStr.Equals(string.Empty)) return null;

            return JToken.Parse(dataStr)["content"].Children().Select(token => new ScoreInfoToken((JObject)token));
        }
        public static async Task<IEnumerable<ScoreInfoToken>?> GetLeaderboardScores(string difficulty_id, string country, int page = 0, int count = 10, CancellationToken ct = default)
        {
            string dataStr = await CallAPI_String(string.Format(APAPI_LEADERBOARD_DIFF_COUNTRY, difficulty_id, country, page, count), throttler, true, ct: ct).ConfigureAwait(false);
            if (dataStr is null || dataStr.Equals(string.Empty)) return null;

            return JToken.Parse(dataStr)["content"].Children().Select(token => new ScoreInfoToken((JObject)token));
        }
        public static async Task<PlayerInfoToken?> GetPlayerInfo(string userId, bool stats, CancellationToken ct = default)
        {
            string dataStr = await CallAPI_String(string.Format(APAPI_PLAYERID, userId, stats.ToString().ToLower()), throttler, false, ct: ct).ConfigureAwait(false);

            return string.IsNullOrEmpty(dataStr) ? null : new(JObject.Parse(dataStr));
        }


        public class DifficultyInfoToken(JObject obj) : JObject(obj) { }
        public class ScoreInfoToken(JObject obj) : JObject(obj) { }
        public class PlayerInfoToken(JObject obj) : JObject(obj) { }
        public class LevelInfoToken(JObject obj) : JObject(obj) { }
        public class StatsInfoToken(JObject obj) : JObject(obj) { }
        public class MilestoneInfoToken(JObject obj) : JObject(obj) { }
    }
}
