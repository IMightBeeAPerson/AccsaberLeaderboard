using HarmonyLib;
using JetBrains.Annotations;
using LeaderboardCore.Models;
using System;

namespace AccsaberLeaderboard.Harmony
{
    [HarmonyPatch(typeof(CustomLeaderboard), "Hide")]
    internal class LeaderboardHiddenPatch
    {
        public static event Action LeaderboardHidden;
        internal static bool wasShown = false;
#pragma warning disable IDE0051
        [UsedImplicitly]
        private static void Postfix()
        {
            if (wasShown && (!UI.ViewControllers.LeaderboardViewController.Instance?.gameObject?.activeSelf ?? false))
            {
                LeaderboardHidden?.Invoke();
                wasShown = false;
            }
        }
    }
}
