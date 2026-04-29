using HarmonyLib;
using LeaderboardCore.Models;
using JetBrains.Annotations;
using System;

namespace AccsaberLeaderboard.Harmony
{
    [HarmonyPatch(typeof(CustomLeaderboard), "Show")]
    internal static class LeaderboardShownPatch
    {
        public static event Action LeaderboardSwapped;
        [UsedImplicitly]
        private static void Postfix()
        {
            if (UI.ViewControllers.LeaderboardViewController.Instance?.gameObject?.activeSelf ?? false)
            {
                LeaderboardSwapped?.Invoke();
            }
        }
    }
}
