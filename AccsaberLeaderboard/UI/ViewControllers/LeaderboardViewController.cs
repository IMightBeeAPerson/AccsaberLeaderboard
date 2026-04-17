using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Harmony;
using AccsaberLeaderboard.Models;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

using static LeaderboardTableView;

namespace AccsaberLeaderboard.UI.ViewControllers
{
    [ViewDefinition("AccsaberLeaderboard.UI.bsml.LeaderboardView.bsml")]
    [HotReload(RelativePathToLayout = @"..\UI\bsml\LeaderboardView.bsml")]
    internal class LeaderboardViewController : BSMLAutomaticViewController
    {
#pragma warning disable IDE0044, IDE0051

        #region Instance Variables & Fields

        private readonly List<AccsaberScoreData> _scores = scoreDatas;
        private string currentHash;
        private BeatmapDifficulty currentDifficulty;

        public bool ValidMapSelected => !string.IsNullOrEmpty(currentHash) && currentDifficulty != default;

        #endregion

        #region Injects

        [Inject] private readonly StandardLevelDetailViewController sldvc;

        #endregion

        #region UI Values & Components

        [UIComponent("leaderboard")]
        private CustomCellListTableData leaderboard;

        [UIValue("leaderboard-infos")]
        private List<object> LeaderboardInfos => [.. scoreDatas.Select(score => (object)new AccsaberScoreData.AccsaberScoreDataInfo(score))];

        [UIComponent("vertical-icon-segments")]
        private SegmentedControl iconSegments;

        private static readonly List<AccsaberScoreData> scoreDatas = [];

        #endregion

        #region UI Actions

        [UIAction("OnCellSelected")]
        private void OnCellSelected(SegmentedControl _, int index)
        {
            // Handle segment selection if needed
        }

        [UIAction("#post-parse")]
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
            SldvcPatch.SldvcSet += TrySubscribeToMapSelection;
        }

        #endregion

        private void TrySubscribeToMapSelection()
        {
            if (sldvc is not null)
            {
                sldvc.didChangeDifficultyBeatmapEvent -= OnDifficultyBeatmapChanged;
                sldvc.didChangeDifficultyBeatmapEvent += OnDifficultyBeatmapChanged;
            }
        }

        private void TryUpdateCurrentMap()
        {
            if (sldvc is not null && sldvc.selectedDifficultyBeatmap is not null)
            {
                OnDifficultyBeatmapChanged(sldvc, sldvc.selectedDifficultyBeatmap);
            }
        }

        private void OnDifficultyBeatmapChanged(StandardLevelDetailViewController controller, IDifficultyBeatmap beatmap)
        {
            if (beatmap == null || beatmap.level == null)
                return;

            // Get hash from the level (custom levels use levelID format: "custom_level_HASH")
            string levelId = beatmap.level.levelID;
            string hash;
            if (levelId.StartsWith("custom_level_"))
                hash = levelId.Substring("custom_level_".Length);
            else
                hash = levelId; // fallback for official levels

            currentHash = hash;
            currentDifficulty = beatmap.difficulty;

            // Optionally, reload leaderboard for the new map
            Task.Run(() => LoadLeaderboardAsync(currentHash, currentDifficulty));
        }

        private async Task LoadLeaderboardAsync(string hash, BeatmapDifficulty diff)
        {
            AccsaberScoreData[] scores = await AccsaberAPI.GetScoreData(1, hash, diff);
            _scores.Clear();
            if (scores is not null)
                _scores.AddRange(scores);

            //leaderboard.SetDataSource(new AccsaberLeaderboardTableView(_scores), reloadData: true);
            IEnumerator ReloadData()
            {
                yield return new WaitForEndOfFrame();
                leaderboard.data = LeaderboardInfos;
                leaderboard.tableView.ReloadData();
            }
            StartCoroutine(ReloadData());
        }

        // DataSource for TableView
        /*private class AccsaberLeaderboardTableView(List<AccsaberScoreData> scores) : LeaderboardTableView
        {
            private new List<AccsaberScoreData> _scores = scores;
            //[SerializeField] private new float _rowHeight = 6f;

            public override TableCell CellForIdx(TableView tableView, int idx)
            {
                AccsaberLeaderboardTableCell cell = tableView.DequeueReusableCellForIdentifier("AccsaberLeaderboardTableCell") as AccsaberLeaderboardTableCell;
                cell ??= new AccsaberLeaderboardTableCell
                {
                    reuseIdentifier = "AccsaberLeaderboardTableCell"
                };
                AccsaberScoreData score = _scores[idx];
                cell.SetData(score);
                return cell;
            }

            public override void SetScores(List<ScoreData> scores, int specialScorePos)
            {
                _scores = [.. scores.Cast<AccsaberScoreData>()];
                _tableView.SetDataSource(this, reloadData: true);
            }
        }

        // Example TableCell implementation
        private class AccsaberLeaderboardTableCell : LeaderboardTableCell
        {
            //private TMPro.TextMeshProUGUI _text;

            //public float AP;

            protected override void Start()
            {
                base.Start();
                //_text = gameObject.AddComponent<TMPro.TextMeshProUGUI>();
                //_text.fontSize = 3;
            }

            public void SetData(AccsaberScoreData data)
            {
                //_text.text = $"#{data.rank} {data.playerName} - {data.AP}ap {data.Acc:N4}%";
                rank = data.rank;
                playerName = data.playerName;
                score = data.score;
                showFullCombo = data.fullCombo;
            }
        }//*/
    }
}
