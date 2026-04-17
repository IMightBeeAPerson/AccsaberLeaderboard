using HarmonyLib;
using JetBrains.Annotations;
using System;

namespace AccsaberLeaderboard.Harmony
{
    [HarmonyPatch(typeof(StandardLevelDetailViewController), "DidActivate")]
    internal static class SldvcPatch
    {
#pragma warning disable IDE0051
        internal static StandardLevelDetailViewController Instance { get; private set; }
        internal static Action SldvcSet;
        [UsedImplicitly]
        private static void Postfix(StandardLevelDetailViewController __instance)
        {
            Instance = __instance;
            SldvcSet?.Invoke();
        }
    }
}
