using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccsaberLeaderboard.UI.ViewControllers
{
    [ViewDefinition("AccsaberLeaderboard.UI.bsml.LeaderboardView.bsml")]
    [HotReload(RelativePathToLayout = @"..\UI\bsml\LeaderboardView.bsml")]
    internal sealed class LeaderboardViewController: BSMLAutomaticViewController
    {
        #region UI Values & Components
        [UIComponent(nameof(leaderboard))]
        private readonly LeaderboardTableView leaderboard;
        //private readonly IconSegmentedControl iconSegments;
        #endregion
        #region UI Actions
        [UIAction(nameof(OnCellSelected))]
        private void OnCellSelected(SegmentedControl _, int index)
        {
        }
#pragma warning disable IDE0051
        [UIAction("#post-parse")]
        private void PostParse()
        {
        }
        #endregion
    }
}
