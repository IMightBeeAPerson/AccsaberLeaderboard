using System.Linq;
using UnityEngine;

namespace AccsaberLeaderboard.Utils
{
    internal static class ResourcePaths
    {
        public const string BSML_PATH = "AccsaberLeaderboard.UI.bsml";
        public const string BSML_LEADERBOARD_VIEW = BSML_PATH + ".LeaderboardView.bsml";
        public const string BSML_LEADERBOARD_CELL = BSML_PATH + ".LeaderboardCell.bsml";
        public const string BSML_MILESTONE_MODAL = BSML_PATH + ".MilestoneModal.bsml";
        public const string BSML_PANEL_VIEW = BSML_PATH + ".PanelView.bsml";
        public const string BSML_PLAYER_PROFILE = BSML_PATH + ".PlayerProfile.bsml";
        public const string BSML_PLAYER_SCORE = BSML_PATH + ".PlayerScore.bsml";

        public const string RESOURCE_PATH = "AccsaberLeaderboard.Resources";
        public const string RESOURCE_1X1 = RESOURCE_PATH + ".1x1.png";
        public const string RESOURCE_LOGO = RESOURCE_PATH + ".accReloaded.png";
        public const string RESOURCE_SWAP = RESOURCE_PATH + ".swap.png";
        public const string RESOURCE_COUNTRY = RESOURCE_PATH + ".country.png";
        public const string RESOURCE_FRIENDS = RESOURCE_PATH + ".friends.png";
        public const string RESOURCE_RIVALS = RESOURCE_PATH + ".rivals.png";
        public const string RESOURCE_FOLLOWED = RESOURCE_PATH + ".followed.png";
        public const string RESOURCE_GLOBAL = RESOURCE_PATH + ".global.png";
        public const string RESOURCE_RELATIONS = RESOURCE_PATH + ".relations.png";
        public const string RESOURCE_TOP_ARROW = RESOURCE_PATH + ".topArrow.png";
        public const string RESOURCE_YOU = RESOURCE_PATH + ".you.png";
        public const string RESOURCE_GRADIENT = RESOURCE_PATH + ".gradient.png";
        public const string RESOURCE_GRADIENT_PANEL = RESOURCE_PATH + ".panelGradient.png";
        public const string RESOURCE_GRADIENT_CORNER = RESOURCE_PATH + ".cornerGradient.png";

        //Below line taken from: https://github.com/accsaber/accsaber-plugin/blob/dev/leaderboard-1.38/AccSaber/UI/ViewControllers/LeaderboardUserModalController.cs#L182
        public static readonly Material BORDER_MATERIAL = Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "UINoGlowRoundEdge");
    }
}
