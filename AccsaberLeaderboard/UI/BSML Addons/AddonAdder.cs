using AccsaberLeaderboard.UI.BSML_Addons.Components;
using AccsaberLeaderboard.UI.BSML_Addons.Tags;
using AccsaberLeaderboard.UI.BSML_Addons.TypeHandlers;
using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage;
using UnityEngine;

namespace AccsaberLeaderboard.UI.BSML_Addons
{
    internal static class AddonAdder
    {
        private static bool hasLoaded = false;
        public static void Load()
        {
            if (hasLoaded) return;
            hasLoaded = true;

            BSMLParser instance = MiscUtils.GetParser();

            instance.RegisterTag(new BetterVertical());
            instance.RegisterTag(new BetterHorizontal());
            instance.RegisterTag(new MyCustomList());

            instance.RegisterTypeHandler(new CustomBackgroundHandler());
            instance.RegisterTypeHandler(new MyCustomCellListTableDataHandler());
        }
    }
}
