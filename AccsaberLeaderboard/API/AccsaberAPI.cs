using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AccsaberLeaderboard.Models;
using System.Diagnostics.CodeAnalysis;
using AccsaberLeaderboard.Utils;

using static AccsaberLeaderboard.API.APIHandler;
using static AccsaberLeaderboard.API.HelpfulPaths;

namespace AccsaberLeaderboard.API
{
#nullable enable
    internal static class AccsaberAPI
    {
        public static readonly Throttler throttler = new(400, 60);
        public static readonly Func<string, Func<ScoreInfoToken, bool>> CountryFilterMaker = country => token => GetCountry(token).Equals(country);

        private static readonly ObjectCacher<PlayerInfoToken> playerInfoCacher = new();
        private static readonly ObjectCacher<(List<ScoreInfoToken> data, HashSet<string> userIds, List<int> blockedUserIndexes, int leaderboardSize)> scoreInfoCacher = new();

        private static readonly Dictionary<(string hash, BeatmapDifficulty mapDiff), string> diffIdCache = [];

        public const int PAGE_LENGTH = 10;
        public const int FILTER_PAGE_MULT = 10;

        static AccsaberAPI()
        {
            AccsaberLiveScores.OnScoreUpdated += token =>
            {
                string playerId = GetPlayerId(token), diffId = GetDifficultyId(token);

                playerInfoCacher.RemoveItem(playerId);

                if (scoreInfoCacher.TryGetCachedItem(diffId, out var item) && item.userIds.Contains(playerId))
                {
                    Plugin.Log.Notice($"Difficulty id {diffId} was removed from cache.");
                    scoreInfoCacher.RemoveItem(diffId);
                }
            };
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
        public static string GetDifficultyId(ScoreInfoToken scoreData) => scoreData["mapDifficultyId"]!.ToString();

        #endregion
        #region Player Info Getters

        public static string GetPlayerAvatar(PlayerInfoToken playerData) => playerData["avatarUrl"]!.ToString();
        public static LevelInfoToken GetPlayerLevelData(PlayerInfoToken playerData) => new((JObject)playerData["levelData"]);
        public static string GetPlayerName(PlayerInfoToken playerData) => playerData["name"]!.ToString();
        public static string GetPlayerId(PlayerInfoToken playerData) => playerData["id"]!.ToString();
        public static bool CheckPlayerForStats(PlayerInfoToken playerData) => playerData["statistics"] is not null;
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
        #region Sync Functions
        public static bool ScoreDataCached(string diffId, int page, Func<ScoreInfoToken, bool>? filter = null, int pageMult = FILTER_PAGE_MULT)
        { // page is one indexed.
            if (!scoreInfoCacher.TryGetCachedItem(diffId, out var info))
                return false;

            int count = filter is null ? info.data.Count : info.data.Count(filter);

            return count >= page * PAGE_LENGTH * pageMult;
        }
        public static bool TryGetRankWithFilter(string diffId, string userId, Func<ScoreInfoToken, bool> filter, out int rank)
        {
            // init rank to -1 in case a check fails
            rank = -1;

            // check for there being a cache for this map, as well as the targeted user id is in this cache.
            if (!scoreInfoCacher.TryGetCachedItem(diffId, out var info) || !info.userIds.Contains(userId))
                return false;

            // if the user is in the cache, get their score data.
            ScoreInfoToken score = info.data.Find(token => GetPlayerId(token).Equals(userId));

            // check to make sure that all scores before the targeted one are loaded (to insure that the page number will be correct).
            int userIndex = GetRank(score) - 1;
            if (info.data.Count <= userIndex || !GetPlayerId(info.data[userIndex]).Equals(userId))
                return false;

            // take all scores up to the player score, filter it using the filter, then since we know the target score in at the end, just return the length minus 1.
            rank = info.data.Take(userIndex + 1).Where(filter).Count() - 1;
            return true;
        }
        private static void CacheScoreData(string diffId, IEnumerable<ScoreInfoToken> scoreData, IEnumerable<int> blockedUserIndexes, int leaderboardSize)
        {
            if (scoreInfoCacher.TryGetCachedItem(diffId, out var val))
            {
                val.userIds.UnionWith(scoreData.Select(GetPlayerId));

                ref List<ScoreInfoToken> storedData = ref val.data;
                ref List<int> blocked = ref val.blockedUserIndexes;

                storedData = MergeListWithEnumerable(storedData, scoreData, token => GetRank(token));
                blocked = MergeListWithEnumerable(blocked, blockedUserIndexes);

                scoreInfoCacher.CacheItem(val, diffId);
            }
            else scoreInfoCacher.CacheItem(([.. scoreData], [.. scoreData.Select(GetPlayerId)], [.. blockedUserIndexes], leaderboardSize), diffId);
        }
        private static List<T> MergeListWithEnumerable<T>(List<T> left, IEnumerable<T> right) where T : IComparable
        {
            return MergeListWithEnumerable(left, right, a => a);
        }
        private static List<T> MergeListWithEnumerable<T>(List<T> left, IEnumerable<T> right, Func<T, IComparable> converter)
        {
            List<T> outp = new(left.Count + right.Count());
            IEnumerator<T>? rightEnum = right.GetEnumerator();

            rightEnum.MoveNext();
            int i = 0;
            while (i < left.Count)
            {
                if (converter(left[i]).CompareTo(converter(rightEnum.Current)) < 0)
                {
                    T toAdd = left[i++];
                    if (outp.Count == 0 || converter(outp.Last()).CompareTo(converter(toAdd)) != 0)
                        outp.Add(toAdd);
                }
                else
                {
                    outp.Add(rightEnum.Current);
                    if (!rightEnum.MoveNext())
                    {
                        rightEnum.Dispose();
                        rightEnum = null;
                        break;
                    }
                }
            }
            if (i < left.Count)
                outp.AddRange(left.Skip(i));
            if (rightEnum is not null)
                do
                    outp.Add(rightEnum.Current);
                while (rightEnum.MoveNext());

            return outp;
        }

        [return: NotNullIfNotNull(nameof(scoreData))]
        public static AccsaberScoreData? ConvertToScoreData(ScoreInfoToken? scoreData)
        {
            if (scoreData is null) return null;
            return new(scoreData);
        }
        public static void InvalidateCache() => scoreInfoCacher.ClearCache();
        public static void InvalidateCache(string diffId) => scoreInfoCacher.RemoveItem(diffId);

        #endregion
        #region Async Functions

        public static async Task<AccsaberScoreData[]?> GetScoreData(int page, string hash, BeatmapDifficulty diff)
        { // page is one indexed.
            string? diffId = await GetLeaderboardDifficultyId(hash, diff);
            if (diffId is null) return null;
            return await GetScoreData(page, diffId);
        }
        public static async Task<AccsaberScoreData[]?> GetScoreData(int page, string diffId, string? country = null)
        { // page is one indexed.
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
        public static async Task<(AccsaberScoreData[] scores, int truePage)> GetScoreData(int page, string diffId, Func<ScoreInfoToken, bool> filter, int scoresNeeded = PAGE_LENGTH, int pageMult = FILTER_PAGE_MULT, int maxCalls = 10, bool cacheBatch = true)
        { // page is zero indexed.
            try
            {
                if (maxCalls <= 0)
                    throw new ArgumentException("Don't call a function then ask it to do nothing.");

                int truePage = page, pageLength = PAGE_LENGTH * pageMult;
                page = (page - 1) / pageMult;

                List<AccsaberScoreData> outp = new(PAGE_LENGTH);

                List<ScoreInfoToken>? toCache = null;
                var currentCacheData = scoreInfoCacher.GetCachedItem(diffId);
                List<ScoreInfoToken>? currentCache = currentCacheData.data;
                if (cacheBatch)
                {
                    toCache = new(pageLength);
                    if (currentCache is not null && currentCache.Count / pageLength > page)
                    {
                        IEnumerable<AccsaberScoreData> cachedScores = currentCache.Skip(page * pageLength).Where(filter).Select(ConvertToScoreData)!;
                        if (currentCache.Count == currentCacheData.leaderboardSize || cachedScores.Count() >= scoresNeeded)
                        {
                            cachedScores = cachedScores.Take(scoresNeeded);
                            return ([.. cachedScores], (int)Math.Ceiling((float)currentCache.Count / PAGE_LENGTH));
                        }
                        if (cachedScores.Any())
                        {
                            truePage = currentCache.Count / PAGE_LENGTH;
                            page = truePage / pageMult;
                            scoresNeeded -= cachedScores.Count();
                            outp.AddRange(cachedScores);
                        }
                    }
                }

                int leaderboardSize = -1;

                do
                {
                    string dataStr = await CallAPI_String(string.Format(APAPI_LEADERBOARD_DIFF, diffId, page, pageLength), throttler).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(dataStr))
                        throw new ArgumentNullException("The leaderboard api is not returning any data.");

                    JToken response = JToken.Parse(dataStr);
                    if ((bool)response["empty"])
                        break;

                    if (leaderboardSize == -1)
                        leaderboardSize = (int)response["totalElements"];

                    IEnumerable<ScoreInfoToken> tokens = response["content"].Children().Select(token => new ScoreInfoToken((JObject)token));

                    if (cacheBatch)
                        toCache!.AddRange(tokens);

                    IEnumerable<AccsaberScoreData> scores = tokens.Where(filter).Select(ConvertToScoreData)!;
                    int scoreLen = scores.Count();
                    if (scoreLen >= scoresNeeded)
                    {
                        scores = scores.Take(scoresNeeded);
                        pageMult = (int)Math.Ceiling(scores.Last().rank / (float)PAGE_LENGTH); // This is just to update truePage correctly.
                        outp.AddRange(scores);
                        scoresNeeded = 0;
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
                } while (scoresNeeded > 0 && maxCalls > 0);

                if (cacheBatch)
                    CacheScoreData(diffId, toCache!, [], leaderboardSize);

                return ([.. outp], truePage);
            }
            catch (Exception e)
            {
                Plugin.Log.Error("Issue getting filtered score data.\n" + e);
                return default;
            }
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
        public static async Task<int> GetMaxScore(string hash, int diffNum) =>
            (int)JToken.Parse(await CallAPI_String(string.Format(APAPI_HASH_DIFF, hash, DiffNumToReloadedDiff(diffNum)), throttler))["difficulties"].Children().First()["maxScore"];
        public static async Task<string> GetHashData(string hash, int diffNum) =>
            await CallAPI_String(string.Format(APAPI_HASH_DIFF, hash, DiffNumToReloadedDiff(diffNum)), throttler, true, maxRetries: 1).ConfigureAwait(false);
        public static async Task<HashSet<string>> GetPlayerRelations(RelationType relation)
        {
            await PlayerSocialLife.LoadTask;
            return await GetPlayerRelations(relation, PlayerSocialLife.PlayerID);
        }
        public static async Task<HashSet<string>> GetPlayerRelations(RelationType relation, string playerId)
        {
            const int pageLength = PAGE_LENGTH * 10;
            int page = 0, callsLeft = 0;
            HashSet<string> outp = [];
            do
            {
                string dataStr = await CallAPI_String(string.Format(APAPI_RELATIONS, playerId, relation.ToString(), "outgoing", page, pageLength));
                if (string.IsNullOrEmpty(dataStr))
                    break;
                JToken response = JToken.Parse(dataStr);

                if (callsLeft == 0)
                    callsLeft = (int)response["totalElements"] / pageLength;

                IEnumerable<string> ids = response["content"].Children().Select(token => token["targetUserId"].ToString());
                foreach (string s in ids)
                    outp.Add(s);

            } while (callsLeft > 0);
            return outp;
        }
        public static async Task<ScoreInfoToken?> GetScoreData(string userId, string hash, BeatmapDifficulty diff, CancellationToken ct = default)
        {
            if (diffIdCache.TryGetValue((hash, diff), out string diffId) && scoreInfoCacher.TryGetCachedItem(diffId, out var val) && val.userIds.Contains(userId))
                return val.data.First(token => GetPlayerId(token).Equals(userId));

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

                DifficultyInfoToken outp = new(diffData);
                diffIdCache.TryAdd((hash, diff), GetDifficultyId(outp));
                return outp;
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
            if (diffIdCache.TryGetValue((hash, diff), out string outp))
                return outp;

            DifficultyInfoToken? diffInfo = await GetLeaderboard(hash, diff, ct);
            if (diffInfo is null) return null;
            return GetDifficultyId(diffInfo);
        } 
        public static async Task<IEnumerable<ScoreInfoToken>?> GetLeaderboardScores(string difficulty_id, int page = 0, int count = 10, CancellationToken ct = default)
        {
            if (scoreInfoCacher.TryGetCachedItem(difficulty_id, out var data) && (data.data.Count == data.leaderboardSize || page < data.data.Count / count))
            {
                int bottomRank = GetRank(data.data[page * count + count - 1]);
                int topRank = GetRank(data.data[page * count]);
                if (bottomRank - topRank == count - 1 + data.blockedUserIndexes.SkipWhile(index => index + 1 < topRank).TakeWhile(index => index + 1 < bottomRank).Count())
                    return data.data.Skip(page * count).Take(count);
            }

            string dataStr = await CallAPI_String(string.Format(APAPI_LEADERBOARD_DIFF, difficulty_id, page, count), throttler, true, ct: ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(dataStr)) return null;

            JToken dataToken = JToken.Parse(dataStr);
            List<ScoreInfoToken> outp = [.. dataToken["content"].Children().Select(token => new ScoreInfoToken((JObject)token))];

            int blockedUsers = 0;

            List<int> blockedUserIds = [];
            for (int i = outp.Count - 1; i >= 0; i--)
                if (PlayerSocialLife.PlayerBlocked.Contains(GetPlayerId(outp[i])))
                {
                    blockedUsers++;
                    blockedUserIds.Add(i + page * count);
                    outp.RemoveAt(i);
                }

            if (blockedUsers > 0)
            {
                int newPage = (page + 1) * count / blockedUsers;
                IEnumerable<ScoreInfoToken>? extras = await GetLeaderboardScores(difficulty_id, newPage, blockedUsers, ct);
                if (extras is null)
                    return null;
                outp.AddRange(extras);
            }

            CacheScoreData(difficulty_id, outp, blockedUserIds, (int)dataToken["totalElements"]);
            return outp.Take(count);
        }
        public static async Task<IEnumerable<ScoreInfoToken>?> GetLeaderboardScores(string difficulty_id, string country, int page = 0, int count = 10, CancellationToken ct = default)
        {
            if (scoreInfoCacher.TryGetCachedItem(difficulty_id, out var data)) 
            {
                if (data.data.Count == data.leaderboardSize || page < data.data.Count(CountryFilterMaker(country)) / count)
                    return data.data.Where(CountryFilterMaker(country)).Skip(page * count).Take(count); 
            }

            string dataStr = await CallAPI_String(string.Format(APAPI_LEADERBOARD_DIFF_COUNTRY, difficulty_id, country, page, count), throttler, true, ct: ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(dataStr)) return null;

            JToken dataToken = JToken.Parse(dataStr);
            List<ScoreInfoToken> outp = [.. dataToken["content"].Children().Select(token => new ScoreInfoToken((JObject)token))];

            int blockedUsers = 0;

            List<int> blockedUserIds = [];
            for (int i = outp.Count - 1; i >= 0; i--)
                if (PlayerSocialLife.PlayerBlocked.Contains(GetPlayerId(outp[i])))
                {
                    blockedUsers++;
                    blockedUserIds.Add(i + page * count);
                    outp.RemoveAt(i);
                }

            if (blockedUsers > 0)
            {
                int newPage = (page + 1) * count / blockedUsers;
                IEnumerable<ScoreInfoToken>? extras = await GetLeaderboardScores(difficulty_id, country, newPage, blockedUsers, ct);
                if (extras is null)
                    return null;
                outp.AddRange(extras);
            }

            CacheScoreData(difficulty_id, outp, blockedUserIds, (int)dataToken["totalElements"]);
            return outp.Take(count);
        }
        public static async Task<PlayerInfoToken?> GetPlayerInfo(string userId, bool stats, CancellationToken ct = default)
        {
            if (playerInfoCacher.TryGetCachedItem(userId, out PlayerInfoToken? outp) && (!stats || CheckPlayerForStats(outp!)))
                return outp;

            string dataStr = await CallAPI_String(string.Format(APAPI_PLAYERID, userId, stats.ToString().ToLower()), throttler, false, ct: ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(dataStr)) return null;

            outp = new(JObject.Parse(dataStr));
            playerInfoCacher.CacheItem(outp, userId);

            return outp;
        }

        #endregion
        #region Token Classes

        public class DifficultyInfoToken(JObject obj) : JObject(obj) { }
        public class ScoreInfoToken(JObject obj) : JObject(obj) { }
        public class PlayerInfoToken(JObject obj) : JObject(obj) { }
        public class LevelInfoToken(JObject obj) : JObject(obj) { }
        public class StatsInfoToken(JObject obj) : JObject(obj) { }
        public class MilestoneInfoToken(JObject obj) : JObject(obj) { }

        #endregion
    }
}
