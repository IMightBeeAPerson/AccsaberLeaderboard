using AccsaberLeaderboard.UI.BSML_Addons.Tags;
using AccsaberLeaderboard.UI.BSML_Addons.TypeHandlers;
using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage;

namespace AccsaberLeaderboard.UI.BSML_Addons
{
    internal static class AddonAdder
    {
        public static void Load()
        {
            BSMLParser instance = MiscUtils.GetParser();

            instance.RegisterTag(new BetterVertical());
            instance.RegisterTag(new BetterHorizontal());
            instance.RegisterTag(new MyCustomList());

            instance.RegisterTypeHandler(new CustomBackgroundHandler());
            instance.RegisterTypeHandler(new MyCustomCellListTableDataHandler());

            Plugin.Log.Info("Tags Loaded.");
        }
    }
}
