using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using System.Collections.Generic;
using System.Threading.Tasks;
using AccsaberLeaderboard.API;
using Zenject;
using JetBrains.Annotations;

namespace AccsaberLeaderboard.UI.ViewControllers
{
    [ViewDefinition("AccsaberLeaderboard.UI.bsml.LeaderboardView.bsml")]
    [HotReload(RelativePathToLayout = @"..\UI\bsml\LeaderboardView.bsml")]
    internal class LeaderboardViewController : BSMLAutomaticViewController
    {
#pragma warning disable IDE0044, IDE0051
        #region Static Variables & Fields

        //private static StandardLevelDetailViewController sldvc => SldvcPatch.Instance;

        #endregion

        #region Instance Variables & Fields

        [UsedImplicitly] private readonly List<LeaderboardTableView.ScoreData> _scores = scoreDatas;
        [UsedImplicitly] private string currentHash;
        [UsedImplicitly] private BeatmapDifficulty currentDifficulty;

        #endregion

        #region Injects

        [Inject] private readonly StandardLevelDetailViewController sldvc;

        #endregion

        #region UI Values & Components

        [UIComponent("leaderboard"), UsedImplicitly]
        private TableView leaderboard;

        [UIComponent("vertical-icon-segments"), UsedImplicitly]
        private SegmentedControl iconSegments;

        private static readonly List<LeaderboardTableView.ScoreData> scoreDatas = [];

        #endregion

        #region UI Actions

        [UIAction("OnCellSelected"), UsedImplicitly]
        private void OnCellSelected(SegmentedControl _, int index)
        {
            // Handle segment selection if needed
        }

        [UIAction("#post-parse"), UsedImplicitly]
        private void PostParse()
        {
            // Subscribe to map selection event
            TrySubscribeToMapSelection();
            // Optionally, load leaderboard for the current map if available
            TryUpdateCurrentMap();
        }

        private void Awake()
        {
            Plugin.Log.Debug("LeaderboardViewController Awake");
        }
        //protected void OnEnable() { }
        //protected void OnDisable() { }

        #endregion

        private void TrySubscribeToMapSelection()
        {
            if (sldvc is not null)
            {
                sldvc.didChangeDifficultyBeatmapEvent -= OnDifficultyBeatmapChanged;
                sldvc.didChangeDifficultyBeatmapEvent += OnDifficultyBeatmapChanged;
            }
        }

        private void OnDifficultyBeatmapChanged(StandardLevelDetailViewController controller, IDifficultyBeatmap beatmap)
        {
            if (beatmap == null || beatmap.level == null)
                return;

            // Get hash from the level (custom levels use levelID format: "custom_level_HASH")
            string levelId = beatmap.level.levelID;
            string hash = null;
            if (levelId.StartsWith("custom_level_"))
                hash = levelId.Substring("custom_level_".Length);
            else
                hash = levelId; // fallback for official levels

            currentHash = hash;
            currentDifficulty = beatmap.difficulty;

            // Optionally, reload leaderboard for the new map
            _ = LoadLeaderboardAsync(currentHash, currentDifficulty);
        }

        private void TryUpdateCurrentMap()
        {
            if (sldvc is not null && sldvc.selectedDifficultyBeatmap is not null)
            {
                OnDifficultyBeatmapChanged(sldvc, sldvc.selectedDifficultyBeatmap);
            }
        }

        private async Task LoadLeaderboardAsync(string hash, BeatmapDifficulty diff)
        {
            LeaderboardTableView.ScoreData[] scores = await AccsaberAPI.GetScoreData(1, hash, diff);
            _scores.Clear();
            if (scores is not null)
                _scores.AddRange(scores);

            leaderboard.SetDataSource(new ScoreTableDataSource(_scores), reloadData: true);
        }

        // DataSource for TableView
        private class ScoreTableDataSource(List<LeaderboardTableView.ScoreData> scores) : TableView.IDataSource
        {
            private readonly List<LeaderboardTableView.ScoreData> _scores = scores;

            public int NumberOfCells()
            {
                return _scores.Count;
            }

            public float CellSize()
            {
                return 6f; // Match your BSML cell-size
            }

            public TableCell CellForIdx(TableView tableView, int idx)
            {
                // You should implement a custom TableCell for your leaderboard row
                // For now, use a basic cell
                LeaderboardCell cell = tableView.DequeueReusableCellForIdentifier("LeaderboardCell") as LeaderboardCell;
                cell ??= new LeaderboardCell
                    {
                        reuseIdentifier = "LeaderboardCell"
                    };
                LeaderboardTableView.ScoreData score = _scores[idx];
                cell.SetData(score);
                return cell;
            }
        }

        // Example TableCell implementation
        private class LeaderboardCell : TableCell
        {
            private TMPro.TextMeshProUGUI _text;

            protected override void Start()
            {
                base.Start();
                _text = gameObject.AddComponent<TMPro.TextMeshProUGUI>();
                _text.fontSize = 3;
            }

            public void SetData(LeaderboardTableView.ScoreData data)
            {
                _text.text = $"{data.rank}. {data.playerName} - {data.score}";
            }
        }
    }
}
