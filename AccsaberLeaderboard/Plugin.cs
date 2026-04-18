using AccsaberLeaderboard.Configuration;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;
using AccsaberLeaderboard.Installers;
using System.Threading.Tasks;
using BS_Utils.Utilities;

namespace AccsaberLeaderboard
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }
        internal static HarmonyLib.Harmony Harmony { get; private set; }

        public string PlayerID { get
            {
                if (playerID is null) LoadPlayerID().GetAwaiter().GetResult();
                return playerID;
            } }
        private string playerID = null;

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
            //BSEvents.menuSceneActive += () => Task.Run(LoadPlayerID);
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            //Log.Debug("OnApplicationQuit");

        }

        private async Task LoadPlayerID()
        {
            playerID = (await BS_Utils.Gameplay.GetUserInfo.GetUserAsync()).platformUserId;
        }
    }
}
