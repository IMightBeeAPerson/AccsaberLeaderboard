using AccsaberLeaderboard.UI.BSML_Addons.Tags;
using AccsaberLeaderboard.UI.BSML_Addons.TypeHandlers;
using BeatSaberMarkupLanguage;

namespace AccsaberLeaderboard.UI.BSML_Addons
{
    internal static class AddonAdder
    {
        public static void Load()
        {
            BSMLParser.instance.RegisterTag(new BetterVertical());
            BSMLParser.instance.RegisterTag(new BetterHorizontal());

            BSMLParser.instance.RegisterTypeHandler(new CustomBackgroundHandler());
        }
    }
}
