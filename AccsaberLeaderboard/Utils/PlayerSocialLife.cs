using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

        public static string PlayerID { get; private set; } = null;
        public static IReadOnlyCollection<string> PlayerRivalIDs => PlayerRivals;
        public static IReadOnlyCollection<string> PlayerFollowedIDs => PlayerFollowed;
        public static IReadOnlyCollection<string> PlayerFriendIDs => PlayerFriends;
        public static IReadOnlyCollection<string> PlayerRelationIDs => PlayerRelations;

        public static IReadOnlyCollection<string> GetIds(LeaderboardDisplayType displayType) => GetIds_Internal(displayType);
        private static HashSet<string> GetIds_Internal(LeaderboardDisplayType displayType) => displayType switch
        {
            LeaderboardDisplayType.Rivals => PlayerRivals,
            LeaderboardDisplayType.Followed => PlayerFollowed,
            LeaderboardDisplayType.Friends => PlayerFriends,
            LeaderboardDisplayType.Relations => PlayerRelations,
            _ => null
        };
        internal static void AddId(string id, LeaderboardDisplayType displayType)
        {
            GetIds_Internal(displayType).Add(id);
            if (displayType != LeaderboardDisplayType.Relations)
                PlayerRelations.Add(id);
        }
        internal static void RemoveId(string id, LeaderboardDisplayType displayType)
        {
            GetIds_Internal(displayType).Remove(id);
            if (displayType != LeaderboardDisplayType.Relations)
                PlayerRelations.Remove(id);
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
                loadTask = LoadInfo(3);
                await loadTask;
                lock (loadLock)
                    Monitor.PulseAll(loadLock);
            }
                
        }
        private static async Task LoadInfo(int retries)
        {
            try
            {
                string playerId = (await BS_Utils.Gameplay.GetUserInfo.GetUserAsync()).platformUserId;
                IReadOnlyList<string> steamFriends = await BS_Utils.Gameplay.GetUserInfo.GetPlatformUserModel().GetUserFriendsUserIds(false).ConfigureAwait(false);
                HashSet<string> friends = [.. steamFriends, playerId];
                HashSet<string> accFollowed = await AccsaberAPI.GetPlayerRelations(HelpfulPaths.RelationType.follower, playerId);
                accFollowed.Add(playerId);
                HashSet<string> rivals = await AccsaberAPI.GetPlayerRelations(HelpfulPaths.RelationType.rival, playerId);
                rivals.Add(playerId);
                HashSet<string> playerRelations = [];
                playerRelations.UnionWith(friends);
                playerRelations.UnionWith(accFollowed);
                playerRelations.UnionWith(rivals);

                PlayerRivals = rivals;
                PlayerFriends = friends;
                PlayerFollowed = accFollowed;
                PlayerRelations = playerRelations;
                PlayerID = playerId;
            } catch (Exception e)
            {
                Plugin.Log.Error("There was an error loading player info!" + (retries > 0 ? " Retrying in 1 second." : ""));
                Plugin.Log.Debug(e);
                if (retries == 0)
                    return;
                await Task.Delay(1000);
                await LoadInfo(retries - 1);
            }
        }
    }
}
