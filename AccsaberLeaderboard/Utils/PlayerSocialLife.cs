using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;
using Newtonsoft.Json.Linq;

namespace AccsaberLeaderboard.Utils
{
    public static class PlayerSocialLife
    {
        private static readonly AsyncLock loadLock = new();
        private static Task loadTask = null;
        public static Task LoadTask => LoadInfo();

        private static HashSet<string> PlayerFriends = null;
        private static HashSet<string> PlayerFollowed = null;
        private static HashSet<string> PlayerRivals = null;
        private static HashSet<string> PlayerRelations = null;

        internal static HashSet<string> PlayerBlocked = null; // never expose this above internal

        private static readonly Dictionary<string, string> UserIdToRelationId = [];

        private static bool exposeFollowed = false, exposeRivals = false;
        private static AccsaberAPI.AuthInfo authInfo;

        public static string PlayerID { get; private set; } = null;
        public static IReadOnlyCollection<string> PlayerRivalIDs => exposeRivals ? PlayerRivals : null;
        internal static IReadOnlyCollection<string> PlayerRivalIDs_Internal => PlayerRivals;
        public static IReadOnlyCollection<string> PlayerFollowedIDs => exposeFollowed ? PlayerFollowed : null;
        internal static IReadOnlyCollection<string> PlayerFollowedIDs_Internal => PlayerFollowed;
        public static IReadOnlyCollection<string> PlayerFriendIDs => PlayerFriends;
        public static IReadOnlyCollection<string> PlayerRelationIDs => PlayerRelations;

        public static IReadOnlyCollection<string> GetIds(LeaderboardDisplayType displayType) => displayType switch
        {
            LeaderboardDisplayType.Rivals => PlayerRivalIDs,
            LeaderboardDisplayType.Followed => PlayerFollowedIDs,
            LeaderboardDisplayType.Friends => PlayerFriendIDs,
            LeaderboardDisplayType.Relations => PlayerRelationIDs,
            _ => null
        };
        public static IReadOnlyCollection<string> GetIds(HelpfulPaths.RelationType relationType) => GetIds(relationType.Convert());
        internal static HashSet<string> GetIds_Internal(LeaderboardDisplayType displayType) => displayType switch
        {
            LeaderboardDisplayType.Rivals => PlayerRivals,
            LeaderboardDisplayType.Followed => PlayerFollowed,
            LeaderboardDisplayType.Friends => PlayerFriends,
            LeaderboardDisplayType.Relations => PlayerRelations,
            LeaderboardDisplayType.Blocked => PlayerBlocked,
            _ => null
        };
        internal static async Task<bool> AddId(string id, LeaderboardDisplayType displayType)
        {
            HashSet<string> set = GetIds_Internal(displayType);

            if (set is null)
                return false;

            set.Add(id);
            if (displayType != LeaderboardDisplayType.Relations)
                PlayerRelations.Add(id);

            var (success, relationId) = await AccsaberAPI.AddPlayerRelation(displayType.Convert(), id);

            if (success)
                UserIdToRelationId.TryAdd(id, relationId);

            return success;
        }
        internal static async Task<bool> RemoveId(string id, LeaderboardDisplayType displayType)
        {
            HashSet<string> set = GetIds_Internal(displayType);

            if (set is null)
                return false;

            set.Remove(id);
            if (displayType != LeaderboardDisplayType.Relations)
                PlayerRelations.Remove(id);

            if (!UserIdToRelationId.TryGetValue(id, out string relationId))
                return false;

            bool success = await AccsaberAPI.RemovePlayerRelation(relationId);
            return success;
        }
        public static async Task LoadInfo()
        {
            if (loadTask is not null)
            {
                await loadTask;
                return;
            }
            AsyncLock.Releaser? theLock = await loadLock.TryLockAsync();
            if (theLock is null)
            {
                if (loadTask is null)
                    lock (loadLock)
                        Monitor.Wait(loadLock);
                else
                    await loadTask;
                return;
            }
            using (theLock.Value)
            {
                loadTask = LoadInfoTask();
                await loadTask;
                lock (loadLock)
                    Monitor.PulseAll(loadLock);
            }
                
        }
        private static async Task LoadInfoTask(int retries = 3)
        {
            try
            { // todo: add blocked players and set the bool for exposing followed/rivals.
                IPlatformUserModel model = BS_Utils.Gameplay.GetUserInfo.GetPlatformUserModel();

                authInfo = await DoAuth();

                string playerId = authInfo.UserId;

                IReadOnlyList<string> steamFriends = await model.GetUserFriendsUserIds(false).ConfigureAwait(false);
                HashSet<string> friends = [.. steamFriends, playerId];

                HashSet<string> accFollowed = await DoPlayerRelation(HelpfulPaths.RelationType.follower, playerId);
                HashSet<string> rivals = await DoPlayerRelation(HelpfulPaths.RelationType.rival, playerId);
                HashSet<string> blocked = await DoPlayerRelation(HelpfulPaths.RelationType.blocked, playerId);

                HashSet<string> playerRelations = [];
                playerRelations.UnionWith(friends);
                playerRelations.UnionWith(accFollowed);
                playerRelations.UnionWith(rivals);

                (exposeFollowed, exposeRivals) = await AccsaberAPI.ExposeRelations();

                PlayerRivals = rivals;
                PlayerFriends = friends;
                PlayerFollowed = accFollowed;
                PlayerRelations = playerRelations;
                PlayerBlocked = blocked;
                PlayerID = playerId;
            } catch (Exception e)
            {
                Plugin.Log.Error("There was an error loading player info!" + (retries > 0 ? " Retrying in 1 second." : ""));
                Plugin.Log.Debug(e);
                if (retries == 0)
                    return;
                await Task.Delay(1000);
                await LoadInfoTask(retries - 1);
            }
        }
        private static async Task<HashSet<string>> DoPlayerRelation(HelpfulPaths.RelationType type, string mainPlayerId)
        {
            var (ids, relations) = await AccsaberAPI.GetPlayerRelations(type);

            foreach (var (userId, relationId) in relations)
                UserIdToRelationId.TryAdd(userId, relationId);

            HashSet<string> outp = ids;
            outp.Add(mainPlayerId);

            return outp;
        }
        private static async Task<AccsaberAPI.AuthInfo> DoAuth()
        {
            if (authInfo.UserId is not null && authInfo.ExpirationDate > DateTime.Now)
                return authInfo;

            try
            {
                IPlatformUserModel model = BS_Utils.Gameplay.GetUserInfo.GetPlatformUserModel();

                string session = (await model.GetUserAuthToken()).token;

                return await AccsaberAPI.Authenticate(session);
            } catch (Exception e)
            {
                Plugin.Log.Error($"There was an error authenticating.");
                Plugin.Log.Debug(e);
            }
            return default;
        }
    }
}
