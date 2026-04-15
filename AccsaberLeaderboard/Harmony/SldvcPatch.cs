using HarmonyLib;
using JetBrains.Annotations;

namespace AccsaberLeaderboard.Harmony
{
    [HarmonyPatch(typeof(StandardLevelDetailViewController), "DidActivate")]
    internal static class SldvcPatch
    {
#pragma warning disable IDE0051
        internal static StandardLevelDetailViewController Instance { get; private set; }
        [UsedImplicitly]
        private static void Postfix(StandardLevelDetailViewController __instance)
        {
            Instance = __instance;
        }
    }
}
