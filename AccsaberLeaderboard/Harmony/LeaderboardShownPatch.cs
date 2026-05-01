using HarmonyLib;
using LeaderboardCore.Models;
using JetBrains.Annotations;
using System;

namespace AccsaberLeaderboard.Harmony
{
    [HarmonyPatch(typeof(CustomLeaderboard), "Show")]
    internal static class LeaderboardShownPatch
    {
#pragma warning disable IDE0051
        public static event Action LeaderboardShown;
        [UsedImplicitly]
        private static void Postfix()
        {
            if (UI.ViewControllers.LeaderboardViewController.Instance?.gameObject?.activeSelf ?? false)
            {
                LeaderboardShown?.Invoke();
            }
        }
    }
}
