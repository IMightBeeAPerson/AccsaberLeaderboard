using AccsaberLeaderboard.Models;
using AccsaberLeaderboard.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static AccsaberLeaderboard.API.APIHandler;
using static AccsaberLeaderboard.API.HelpfulPaths;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AccsaberLeaderboard.API
{
#nullable enable
    internal static class AccsaberAPI
    {
        public static readonly Throttler throttler = new(400, 60);
        public static readonly Func<string, Func<ScoreInfoToken, bool>> CountryFilterMaker = country => token => GetCountry(token).Equals(country);

        private static readonly ObjectCacher<PlayerInfoToken> playerInfoCacher = new();
        private static readonly ObjectCacher<ScoreCache> scoreInfoCacher = new();

        private static readonly Dictionary<(string hash, BeatmapDifficulty mapDiff), string> diffIdCache = [];

        public const int PAGE_LENGTH = 10;
        public const int FILTER_PAGE_MULT = 10;

        static AccsaberAPI()
        {
            AccsaberLiveScores.OnScoreUpdated += token =>
            {
                string playerId = GetPlayerId(token), diffId = GetDifficultyId(token);

                playerInfoCacher.RemoveItem(playerId);

                if (scoreInfoCacher.TryGetCachedItem(diffId, out var item) && item.UserIds.Contains(playerId))
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
        public static bool ScoreDataCached(string diffId, int page, Func<ScoreInfoToken, bool>? filter = null, int setCount = -1)
        { // page is one indexed.
            if (!scoreInfoCacher.TryGetCachedItem(diffId, out ScoreCache info))
                return false;

            int count;

            if (filter is null)
            {
                page--;
                int topIdx = page * PAGE_LENGTH, bottomIdx = topIdx + PAGE_LENGTH;
                int blocked = info.BlockedUserIndexes.SkipWhile(idx => idx < topIdx).TakeWhile(idx => idx < bottomIdx).Count();
                bottomIdx += blocked;
                filter = token =>
                {
                    int rank = GetRank(token);
                    return topIdx < rank && bottomIdx >= rank;
                };
                count = info.Data.Count(filter);
                //Plugin.Log.Info($"count = {count}, page = {page}");
                return PAGE_LENGTH == count;
            }

            count = info.Data.Count(filter);

            return count == setCount || count - ((page - 1) * PAGE_LENGTH) >= PAGE_LENGTH;
        }
        public static bool ScoreDataCached(string diffId, int page, string country)
        { // page is one indexed
            if (!scoreInfoCacher.TryGetCachedItem(diffId, out ScoreCache info))
                return false;

            int count = info.Data.Count(CountryFilterMaker(country));

            return count >= page * PAGE_LENGTH || (info.LeaderboardLengths.TryGetValue(country, out int len) && count == len - info.BlockedUserIndexes.Count);
        }
        public static bool TryGetRankWithFilter(string diffId, string userId, Func<ScoreInfoToken, bool> filter, out int rank)
        {
            // init rank to -1 in case a check fails
            rank = -1;

            // check for there being a cache for this map, as well as the targeted user id is in this cache.
            if (!scoreInfoCacher.TryGetCachedItem(diffId, out var info) || !info.UserIds.Contains(userId))
                return false;
            //Plugin.Log.Info("Passed check 1.");

            // if the user is in the cache, get their score data.
            ScoreInfoToken score = info.Data.Find(token => GetPlayerId(token).Equals(userId));

            // check to make sure that all scores before the targeted one are loaded (to insure that the page number will be correct).
            int userIndex = GetRank(score) - 1;
            if (info.Data.Count <= userIndex || !GetPlayerId(info.Data[userIndex]).Equals(userId))
                return false;
            //Plugin.Log.Info("Passed check 2.");

            // take all scores up to the player score, filter it using the filter, then since we know the target score in at the end, just return the length minus 1.
            rank = info.Data.Take(userIndex + 1).Where(filter).Count() - 1;

            return true;
        }
        private static void CacheScoreData(string diffId, IEnumerable<ScoreInfoToken> scoreData, IEnumerable<int> BlockedUserIndexes, int leaderboardSize, string country = "N/A")
        {
            if (scoreInfoCacher.TryGetCachedItem(diffId, out var val))
            {
                val.UserIds.UnionWith(scoreData.Select(GetPlayerId));

                ref List<ScoreInfoToken> storedData = ref val.Data;
                ref List<int> blocked = ref val.BlockedUserIndexes;

                storedData = MergeListWithEnumerable(storedData, scoreData, token => GetRank(token));
                if (BlockedUserIndexes.Any())
                    blocked = MergeListWithEnumerable(blocked, BlockedUserIndexes);
                if (!val.LeaderboardLengths.ContainsKey(country) && leaderboardSize >= 0)
                    val.LeaderboardLengths.Add(country, leaderboardSize);

                scoreInfoCacher.CacheItem(val, diffId);
            }
            else
            {
                Dictionary<string, int> len = leaderboardSize >= 0 ? new() { { country, leaderboardSize } } : [];
                scoreInfoCacher.CacheItem(new(len, [.. scoreData], [.. scoreData.Select(GetPlayerId)], [.. BlockedUserIndexes]), diffId);
            }

            //ScoreCache c = scoreInfoCacher.GetCachedItem(diffId);
            //Plugin.Log.Info($"The cache now has {c.Data.Count} entries: {c.Data.Select(GetRank).Print()}");
            //Plugin.Log.Info($"There are the following sizes: { c.LeaderboardLengths.Values.Print()}");

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
            {
                if (converter(outp.Last()).CompareTo(converter(left[i])) == 0)
                    i++;
                outp.AddRange(left.Skip(i));
            }
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
        public static void RemovePlayerFromCache(string playerId)
        {
            foreach (KeyValuePair<string, ScoreCache> diff in scoreInfoCacher)
            {
                if (!diff.Value.UserIds.Contains(playerId))
                    continue;

                diff.Value.UserIds.Remove(playerId);
                ScoreInfoToken info = diff.Value.Data.First(token => GetPlayerId(token).Equals(playerId));
                diff.Value.Data.Remove(info);
                int idx = GetRank(info) - 1;

                if (diff.Value.BlockedUserIndexes.Count < 2 || diff.Value.BlockedUserIndexes.Last() < idx)
                    diff.Value.BlockedUserIndexes.Add(idx);
                else for (int i = diff.Value.BlockedUserIndexes.Count - 2; i >= 0; i--)
                        if (diff.Value.BlockedUserIndexes[i] < idx)
                        {
                            diff.Value.BlockedUserIndexes.Insert(i + 1, idx);
                            break;
                        }
            }
        }
        private static (AccsaberScoreData[] scores, bool success) SearchInCache(ScoreCache cache, ref int page, Func<ScoreInfoToken, bool> filter, int pageLength, int scoresNeeded, int pageMult)
        {
            List<ScoreInfoToken>? currentCache = cache.Data;
            if (currentCache is not null && currentCache.Count / pageLength > page)
            {
                IEnumerable<AccsaberScoreData> cachedScores = currentCache.Skip(page * pageLength).Where(filter).Select(ConvertToScoreData)!;
                int cachedScoresLen = cachedScores.Count();
                if (currentCache.Count == cache.LeaderboardSize || cachedScoresLen >= scoresNeeded)
                {
                    cachedScores = cachedScores.Take(scoresNeeded);
                    return ([.. cachedScores], true);
                }
                if (cachedScores.Any())
                {
                    int truePage = currentCache.Count / PAGE_LENGTH;
                    page = truePage / pageMult;
                    scoresNeeded -= cachedScores.Count();
                    return ([.. cachedScores], false);
                }
            }
            return ([], false);
        }

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
        { // page is one indexed.
            try
            {
                if (maxCalls <= 0)
                    throw new ArgumentException("Don't call a function then ask it to do nothing.");

                int truePage = page, pageLength = PAGE_LENGTH * pageMult;
                page = (page - 1) / pageMult;

                List<AccsaberScoreData> outp = new(PAGE_LENGTH);

                List<ScoreInfoToken>? toCache = null;
                ScoreCache currentCacheData = scoreInfoCacher.GetCachedItem(diffId);
                if (cacheBatch)
                {
                    toCache = new(pageLength);
                    var (scores, success) = SearchInCache(currentCacheData, ref page, filter, pageLength, scoresNeeded, pageMult);
                    if (success)
                        return (scores, currentCacheData.Data.Count / PAGE_LENGTH);
                    else
                        outp.AddRange(scores);
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
        public static async Task<AccsaberScoreData[]?> GetScoreData(int page, string diffId, RelationType relation)
        { // page is one indexed
            try
            {
                --page;
                if (scoreInfoCacher.TryGetCachedItem(diffId, out ScoreCache cache))
                {
                    HashSet<string> relations = PlayerSocialLife.GetIds_Internal(relation.Convert());
                    IEnumerable<ScoreInfoToken> tokens = cache.Data.Where(token => relations.Contains(GetPlayerId(token)));

                    int tokenCount = tokens.Count();
                    int scoreCount = cache.RelationScoresCount[(int)relation];
                    int pageCount = page * PAGE_LENGTH;

                    //Plugin.Log.Info($"token count = {tokenCount} || score count = {scoreCount} || page count = {pageCount}");

                    if (scoreCount == 0)
                        return [];

                    if (tokenCount == scoreCount || tokenCount > pageCount && (scoreCount < pageCount + PAGE_LENGTH || tokenCount >= pageCount + PAGE_LENGTH))
                        return [.. tokens.Skip(pageCount).Take(PAGE_LENGTH).Select(token => new AccsaberScoreData(token))];

                }

                string dataStr = await CallAPI_String(string.Format(APAPI_LEADERBOARD_DIFF_RELATION, diffId, relation.ToString(), page, PAGE_LENGTH));

                if (string.IsNullOrEmpty(dataStr))
                    return null;

                JToken dataToken = JToken.Parse(dataStr);
                IEnumerable<ScoreInfoToken> outp = dataToken["content"].Children().Select(token => new ScoreInfoToken((JObject)token));

                CacheScoreData(diffId, outp, [], -1);
                cache = scoreInfoCacher.GetCachedItem(diffId);
                cache.RelationScoresCount[(int)relation] = (int)dataToken["totalElements"];
                scoreInfoCacher.CacheItem(cache, diffId);

                return [.. outp.Select(token => new AccsaberScoreData(token))];

            } catch (Exception e)
            {
                Plugin.Log.Error("There was an error getting score data.");
                Plugin.Log.Debug(e);
            }
            return null;
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
        public static async Task<Dictionary<RelationType, (HashSet<string> userIds, Dictionary<string, string> relations)>> GetPlayerRelations()
        {
            const int pageLength = PAGE_LENGTH * 10;
            int page = 0, callsLeft = 0;
            Dictionary<RelationType, (HashSet<string> userIds, Dictionary<string, string> relations)> outp = [];

            foreach (RelationType rt in Enum.GetValues(typeof(RelationType)))
                outp[rt] = ([], []);

            do
            {
                string dataStr = await CallAPI_String(string.Format(APAPI_AUTH_GET_RELATIONS_ALL, page, pageLength));
                if (string.IsNullOrEmpty(dataStr))
                    break;
                JToken response = JToken.Parse(dataStr);

                if (callsLeft == 0)
                    callsLeft = (int)response["totalElements"] / pageLength;

                //IEnumerable<(string userId, string relationId)> ids = response["content"].Children().Select(token => (token["targetUserId"].ToString(), token["id"].ToString()));
                IEnumerable<JToken> ids = response["content"].Children();
                foreach (JToken token in ids)
                {
                    RelationType rt = (RelationType)Enum.Parse(typeof(RelationType), token["type"].ToString());
                    string userId = token["targetUserId"].ToString(), relationId = token["id"].ToString();
                    outp[rt].userIds.Add(userId);
                    outp[rt].relations.Add(userId, relationId);
                }

            } while (callsLeft > 0);
            return outp;
        }
        public static async Task<(HashSet<string> ids, IEnumerable<(string userId, string relationId)> relations)> GetPlayerRelations(RelationType relation, string playerId)
        {
            const int pageLength = PAGE_LENGTH * 10;
            int page = 0, callsLeft = 0;
            HashSet<string> userIds = [];
            List<(string, string)> relations = [];
            do
            {
                string dataStr = await CallAPI_String(string.Format(APAPI_RELATIONS, playerId, relation.ToString(), "outgoing", page, pageLength));
                if (string.IsNullOrEmpty(dataStr))
                    break;
                JToken response = JToken.Parse(dataStr);

                if (callsLeft == 0)
                    callsLeft = (int)response["totalElements"] / pageLength;

                IEnumerable<(string userId, string relationId)> ids = response["content"].Children().Select(token => (token["targetUserId"].ToString(), token["id"].ToString()));
                foreach (var (userId, _) in ids)
                    userIds.Add(userId);
                relations.AddRange(ids);

            } while (callsLeft > 0);
            return (userIds, relations);
        }
        public static async Task<(bool success, string? relationId)> AddPlayerRelation(RelationType relation, string targetId)
        {
            if (!long.TryParse(targetId, out long id))
            {
                Plugin.Log.Error($"The target id given to SetPlayerRelation, \"{targetId}\", is not able to be parsed!");
                return (false, null);
            }

            HttpRequestMessage request = new(HttpMethod.Post, APAPI_AUTH_SET_RELATION)
            {
                Content = new StringContent($"{{\"targetUserId\": {id}, \"type\": \"{relation}\"}}", System.Text.Encoding.UTF8, "application/json")
            };

            var (Success, Content) = await CallAPI(request, throttler, maxRetries: 1).ConfigureAwait(false);

            if (!Success)
                return (false, null);

            return (true, JToken.Parse(await Content.ReadAsStringAsync())["id"].ToString());
        }
        public static async Task<bool> RemovePlayerRelation(string relationId)
        {
            HttpRequestMessage request = new(HttpMethod.Delete, string.Format(APAPI_AUTH_DELETE_RELATION, relationId));

            return (await CallAPI(request, throttler, maxRetries: 1).ConfigureAwait(false)).Success;
        }
        public static async Task<ScoreInfoToken?> GetScoreData(string userId, string hash, BeatmapDifficulty diff, CancellationToken ct = default)
        {
            if (diffIdCache.TryGetValue((hash, diff), out string diffId) && scoreInfoCacher.TryGetCachedItem(diffId, out var val) && val.UserIds.Contains(userId))
                return val.Data.First(token => GetPlayerId(token).Equals(userId));

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

                if (string.IsNullOrEmpty(dataStr)) return null;
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
            if (scoreInfoCacher.TryGetCachedItem(difficulty_id, out var data))
            {
                int minRank = page * count + 1, maxRank = minRank + count;
                int topIdx = data.Data.FindIndex(token =>
                {
                    int rank = GetRank(token);
                    return rank >= minRank && rank < maxRank; 
                });

                if (topIdx >= 0 && data.Data.Count > topIdx + count - 1)
                {

                    int topRank = GetRank(data.Data[topIdx]);
                    int bottomRank = GetRank(data.Data[topIdx + count - 1]);

                    IEnumerable<int> temp = data.BlockedUserIndexes.SkipWhile(idx => idx < topRank);
                    //int blockedUserCountBefore = data.BlockedUserIndexes.Count - temp.Count(); // To use later if I decide to shift pages.
                    int blockedUserCount = temp.TakeWhile(idx => idx < bottomRank).Count();

                    //Plugin.Log.Info($"bottom = {bottomRank}, top = {topRank}, bottom idx = {topIdx + count - 1}, top idx = {topIdx}, blocked = {blockedUserCount}");

                    if (topRank - page * count == 1 && bottomRank - (page + 1) * count == blockedUserCount)
                        return data.Data.Skip(topIdx).Take(count);
                }
            }

            string dataStr = await CallAPI_String(string.Format(APAPI_LEADERBOARD_DIFF, difficulty_id, page, count), throttler, true, ct: ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(dataStr)) return null;

            JToken dataToken = JToken.Parse(dataStr);
            List<ScoreInfoToken>? outp = [.. dataToken["content"].Children().Select(token => new ScoreInfoToken((JObject)token))];

            List<int>? blockedUserIds;
            (outp, blockedUserIds) = await HandleBlockedPlayers(outp!, data, page, count, (inPage, inCount) => GetLeaderboardScores(difficulty_id, inPage, inCount, ct));
            if (outp is null || blockedUserIds is null)
                return null;

            CacheScoreData(difficulty_id, outp, blockedUserIds, (int)dataToken["totalElements"]);
            return outp.Take(count);
        }
        public static async Task<IEnumerable<ScoreInfoToken>?> GetLeaderboardScores(string difficulty_id, string country, int page = 0, int count = 10, CancellationToken ct = default)
        {
            if (scoreInfoCacher.TryGetCachedItem(difficulty_id, out ScoreCache data)) 
            {
                int trueCount = data.Data.Count(CountryFilterMaker(country));
                int total = data.LeaderboardLengths.TryGetValue(country, out int leng) ? leng : 0;
                //Plugin.Log.Info($"true count = {trueCount}, blocked = {data.BlockedUserIndexes.Count}, total = {total}");
                if (data.LeaderboardLengths.TryGetValue(country, out int len) && trueCount == len - data.BlockedUserIndexes.Count || page < trueCount / count)
                    return data.Data.Where(CountryFilterMaker(country)).Skip(page * count).Take(count); 
            }

            string dataStr = await CallAPI_String(string.Format(APAPI_LEADERBOARD_DIFF_COUNTRY, difficulty_id, country, page, count), throttler, true, ct: ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(dataStr)) return null;

            JToken dataToken = JToken.Parse(dataStr);
            List<ScoreInfoToken>? outp = [.. dataToken["content"].Children().Select(token => new ScoreInfoToken((JObject)token))];

            List<int>? blockedUserIds;
            (outp, blockedUserIds) = await HandleBlockedPlayers(outp!, data, page, count, (inPage, inCount) => GetLeaderboardScores(difficulty_id, country, inPage, inCount, ct));
            if (outp is null || blockedUserIds is null)
                return null;

            CacheScoreData(difficulty_id, outp, blockedUserIds, (int)dataToken["totalElements"], country);
            return outp.Take(count);
        }
        private static async Task<(List<ScoreInfoToken>? newOutp, List<int>? blockedIds)> HandleBlockedPlayers(List<ScoreInfoToken> scoreTokens, ScoreCache data, int page, int count,
            Func<int, int, Task<IEnumerable<ScoreInfoToken>?>> getExtraScores)
        {
            if (data.BlockedUserIndexes is null)
                return (scoreTokens, []);

            if (data.BlockedUserIndexes.Count > 0)
            {
                IEnumerable<ScoreInfoToken> toUnblock = scoreTokens.Where(token => data.BlockedUserIndexes.Contains(GetRank(token) - 1) && !PlayerSocialLife.PlayerBlocked.Contains(GetPlayerId(token)));
                if (toUnblock.Any())
                {
                    List<int> toUnblockIdx = [.. toUnblock.Select(token => GetRank(token) - 1)];
                    foreach (int i in toUnblockIdx)
                        data.BlockedUserIndexes.Remove(i);
                }
            }

            int blockedUsers = 0;
            List<int> blockedUserIds = [];

            if (PlayerSocialLife.PlayerBlocked.Count > 0)
            {
                bool addNewEntries = data.BlockedUserIndexes is null || PlayerSocialLife.PlayerBlocked.Count != data.BlockedUserIndexes.Count;

                for (int i = scoreTokens.Count - 1; i >= 0; i--)
                    if (PlayerSocialLife.PlayerBlocked.Contains(GetPlayerId(scoreTokens[i])))
                    {
                        blockedUsers++;
                        if (addNewEntries)
                            blockedUserIds.Add(GetRank(scoreTokens[i]) - 1);
                        scoreTokens.RemoveAt(i);
                    }

                if (blockedUsers > 0)
                {
                    int newPage = (page + 1) * count / blockedUsers;
                    IEnumerable<ScoreInfoToken>? extras = await getExtraScores(newPage, blockedUsers);
                    if (extras is null)
                        return (null, null);
                    scoreTokens.AddRange(extras);
                }
            }

            return (scoreTokens, blockedUserIds);
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

        internal static async Task<(bool friends, bool rivals)> ExposeRelations()
        {
            string dataStr = await CallAPI_String(string.Format(APAPI_AUTH_GET_SETTINGS, "privacy"), throttler).ConfigureAwait(false);

            if (string.IsNullOrEmpty(dataStr))
                return (false, false);

            JToken privacySettings = JToken.Parse(dataStr);
            return (privacySettings["privacy.followingVisibility"].ToString().Equals("public"), privacySettings["privacy.rivalsVisibility"].ToString().Equals("public"));
        }
        internal static async Task<AuthInfo> Authenticate(string ticket)
        {
            HttpRequestMessage request = new(HttpMethod.Post, APAPI_AUTH)
            {
                Content = new StringContent($"{{\"provider\": \"steamTicket\", \"ticket\": \"{ticket}\"}}", System.Text.Encoding.UTF8, "application/json")
            };
            var (success, content) = await CallAPI(request, throttler, maxRetries: 1).ConfigureAwait(false);

            if (!success)
                return default;

            try
            {
                JToken token = JToken.Parse(await content.ReadAsStringAsync());
                AuthInfo outp = new(token["accessToken"].ToString(), token["refreshToken"].ToString(), DateTime.Now.AddSeconds((long)token["expiresIn"]), token["userId"].ToString());

                SetAuthForClient(outp);

                Plugin.Log.Info("Successfully authenticated!");

                return outp;
            }
            catch (Exception e)
            {
                Plugin.Log.Error("There was an issue on the end part of doing authentication.");
                Plugin.Log.Debug(e);
                return default;
            }
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
        #region Misc structs

        private struct ScoreCache
        {
            public Dictionary<string, int> LeaderboardLengths;
            public List<ScoreInfoToken> Data;
            public HashSet<string> UserIds;
            public List<int> BlockedUserIndexes;
            public int[] RelationScoresCount;

            public readonly int LeaderboardSize => LeaderboardLengths["N/A"];

            public ScoreCache(Dictionary<string, int> leaderboardLengths, List<ScoreInfoToken> data, HashSet<string> userIds, List<int> blockedUserIndexes)
            {
                LeaderboardLengths = leaderboardLengths;
                Data = data;
                UserIds = userIds;
                BlockedUserIndexes = blockedUserIndexes;
                RelationScoresCount = new int[Enum.GetValues(typeof(RelationType)).Length];

                for (int i = 0; i < RelationScoresCount.Length; i++)
                    RelationScoresCount[i] = -1;
            }
        }
        internal readonly struct AuthInfo(string accessToken, string refreshToken, DateTime expirationDate, string userId)
        {
            public readonly string AccessToken = accessToken;
            public readonly string RefreshToken = refreshToken;
            public readonly DateTime ExpirationDate = expirationDate;
            public readonly string UserId = userId;
        }

        #endregion
    }
}
