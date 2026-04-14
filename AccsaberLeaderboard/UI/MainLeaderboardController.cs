using System;
using AccsaberLeaderboard.UI.ViewControllers;
using HMUI;
using LeaderboardCore.Models;

namespace AccsaberLeaderboard.UI
{
    internal sealed class MainLeaderboardController: CustomLeaderboard
    {
        private readonly PanelViewController _panelViewController;
        private readonly LeaderboardViewController _leaderboardViewController;

        public MainLeaderboardController(PanelViewController panelViewController, LeaderboardViewController leaderboardViewController)
        {
            _panelViewController = panelViewController;
            _leaderboardViewController = leaderboardViewController;
        }
        protected override ViewController panelViewController => _panelViewController;
        protected override ViewController leaderboardViewController => _leaderboardViewController;
    }
}
