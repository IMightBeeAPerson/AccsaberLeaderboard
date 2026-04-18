using System;
using AccsaberLeaderboard.UI.ViewControllers;
using HMUI;
using LeaderboardCore.Managers;
using LeaderboardCore.Models;
using Zenject;

namespace AccsaberLeaderboard.UI
{
    internal sealed class MainLeaderboardController : CustomLeaderboard, IInitializable, IDisposable
    {
        private readonly PanelViewController _panelViewController;
        private readonly ViewControllers.LeaderboardViewController _leaderboardViewController;
        private readonly CustomLeaderboardManager _customLeaderboardManager;

#pragma warning disable IDE0290
        public MainLeaderboardController(CustomLeaderboardManager customLeaderboardManager, PanelViewController panelViewController, ViewControllers.LeaderboardViewController leaderboardViewController)
        {
                _customLeaderboardManager = customLeaderboardManager;
                _panelViewController = panelViewController;
                _leaderboardViewController = leaderboardViewController;
        }

        protected override ViewController panelViewController => _panelViewController;
        protected override ViewController leaderboardViewController => _leaderboardViewController;

#if NEW_VERSION
        public override bool ShowForLevel(BeatmapKey? beatmapKey)
#else
        public override bool ShowForLevel(IPreviewBeatmapLevel selectedLevel)
#endif
        {
            return true;// _leaderboardViewController.ValidMapSelected;
        }
        public void Initialize()
        {
            _customLeaderboardManager.Register(this);
        }
        public void Dispose()
        {
            _customLeaderboardManager.Unregister(this);
        }
    }
}
