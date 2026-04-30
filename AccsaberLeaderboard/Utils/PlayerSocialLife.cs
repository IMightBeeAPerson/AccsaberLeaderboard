using AccsaberLeaderboard.API;
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

        public static string PlayerID { get; private set; } = null;
        public static IReadOnlyList<string> PlayerFriends { get; private set; } = null;
        internal static HashSet<string> PlayerRivals { get; private set; } = null;
        public static IReadOnlyCollection<string> PlayerRivalIDs => PlayerRivals;

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
                IReadOnlyList<string> friends = [.. await BS_Utils.Gameplay.GetUserInfo.GetPlatformUserModel().GetUserFriendsUserIds(false).ConfigureAwait(false), playerId];
                HashSet<string> rivals = [.. await AccsaberAPI.GetPlayerRivals(playerId), playerId];

                PlayerRivals = rivals;
                PlayerFriends = friends;
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
