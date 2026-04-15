using AccsaberLeaderboard.Installers;
using HarmonyLib;
using JetBrains.Annotations;
using System;

namespace AccsaberLeaderboard.Harmony
{
    [HarmonyPatch(typeof(MainSettingsMenuViewControllersInstaller), "InstallBindings")]
    public static class LoadPatch
    {
#pragma warning disable IDE0051
        [UsedImplicitly]
        private static void Postfix(MainSettingsMenuViewControllersInstaller __instance)
        {
            try
            {
                MainUIInstaller.Install(__instance.GetContainer());
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error in LoadPatch: {ex}");
            }
        }
    }
}
