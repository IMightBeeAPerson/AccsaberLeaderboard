using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Configuration;
using AccsaberLeaderboard.UI.BSML_Addons;
using BS_Utils.Utilities;
using IPA;
using IPA.Config.Stores;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using IPALogger = IPA.Logging.Logger;

namespace AccsaberLeaderboard
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }
        internal static HarmonyLib.Harmony Harmony { get; private set; }
        internal static bool IsAPIServiceWorking { get; private set; }

        private string playerID = null;
        private HashSet<string> playerFriends;
        private readonly object playerIDLock = new(), playerFriendsLock = new();
        public string PlayerID 
        { 
            get
            {
                lock (playerIDLock)
                {
                    if (playerID is null) LoadPlayerID().GetAwaiter().GetResult();
                    return playerID;
                }
            } 
        }
        public HashSet<string> PlayerFriends
        {
            get
            {
                lock (playerFriendsLock)
                {
                    if (playerFriends is null) LoadPlayerFriends().GetAwaiter().GetResult();
                    return playerFriends;
                }
            }
        }

        [Init]
        /// <summary>
        /// Called when the plugin is first loaded by IPA (either when the game starts or when the plugin is enabled if it starts disabled).
        /// [Init] methods that use a Constructor or called before regular methods like InitWithConfig.
        /// Only use [Init] with one Constructor.
        /// </summary>
        public void Init(IPALogger logger, IPA.Config.Config conf)
        {
            Instance = this;
            PluginConfig.Instance = conf.Generated<PluginConfig>();
            Log = logger;
            IsAPIServiceWorking = false;
            Task.Run(async () =>
            {
                var (Success, Content) = await APIHandler.CallAPI(HelpfulPaths.APAPI_TEST, AccsaberAPI.throttler, false).ConfigureAwait(false);
                IsAPIServiceWorking = Success;
                if (!Success) Log.Critical("The accsaber API is down! Turning off leaderboard.");
            });
        }

        #region BSIPA Config
        //Uncomment to use BSIPA's config
        /*
        [Init]
        public void InitWithConfig(Config conf)
        {
            Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
            Log.Debug("Config loaded");
        }
        */
        #endregion

        [OnStart]
        public void OnApplicationStart()
        {
            //Log.Debug("OnApplicationStart");
            //new GameObject("AccsaberLeaderboardController").AddComponent<AccsaberLeaderboardController>();
            Harmony = new HarmonyLib.Harmony("Person.AccsaberLeaderboard");
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
#if NEW_VERSION
            BeatSaberMarkupLanguage.Util.MainMenuAwaiter.MainMenuInitializing += LoadBSML;
#else
            BSEvents.menuSceneActive += LoadBSML;
#endif
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            //Log.Debug("OnApplicationQuit");
            AccsaberLiveScores.WebsocketCanceller.Cancel();
        }

        private void LoadBSML()
        {
            AddonAdder.Load();
            Task.Run(() => AccsaberLiveScores.StartWebsocket(AccsaberLiveScores.WebsocketCanceller.Token));
        }
        private const int LoadPlayerIDAttempts = 3;
        private async Task LoadPlayerID(int attemptsLeft = 3)
        {
            const int delayMillis = 500;
            try
            {
                playerID = (await BS_Utils.Gameplay.GetUserInfo.GetUserAsync()).platformUserId;
            } catch (System.Exception e)
            {
                int delay = (LoadPlayerIDAttempts - --attemptsLeft) * delayMillis;
                Log.Error($"Exception thrown while loading player ID. Trying again in {delay} milliseconds.");
                Log.Debug(e);
                await Task.Delay(delay);
                await LoadPlayerID(attemptsLeft);
            }
        }
        private async Task LoadPlayerFriends()
        {
            IReadOnlyList<string> friends = await BS_Utils.Gameplay.GetUserInfo.GetPlatformUserModel().GetUserFriendsUserIds(false).ConfigureAwait(false);
            playerFriends = [.. friends, PlayerID];
        }
    }
}
