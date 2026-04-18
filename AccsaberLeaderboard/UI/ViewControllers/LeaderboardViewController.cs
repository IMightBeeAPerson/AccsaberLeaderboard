using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Harmony;
using AccsaberLeaderboard.Models;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

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
        private int page;

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

        [UIAction("OnPageUp")]
        private void OnPageUp()
        {
            if (page == 1) return; // Can't go back from the first page
            page--;
            Task.Run(() => LoadLeaderboardAsync(currentHash, currentDifficulty));
        }

        [UIAction("OnYouClicked")]
        private void OnYouClicked()
        {
            Task.Run(async () =>
            {
                int playerPage = await GetPlayerPage();
                if (playerPage != page)
                {
                    page = playerPage;
                    await LoadLeaderboardAsync(currentHash, currentDifficulty);
                }
            });
        }

        [UIAction("OnPageDown")]
        private void OnPageDown()
        {
            page++;
            Task.Run(() => LoadLeaderboardAsync(currentHash, currentDifficulty));
        }

        #endregion
        private void Awake()
        {
            Plugin.Log.Debug("LeaderboardViewController Awake");
            SldvcPatch.SldvcSet += TrySubscribeToMapSelection;
        }
        private void TrySubscribeToMapSelection()
        {
            if (sldvc is not null)
            {
                void Handler1(StandardLevelDetailViewController controller, IDifficultyBeatmap beatmap) => UpdateDiff(beatmap);
                void Handler2(StandardLevelDetailViewController controller, StandardLevelDetailViewController.ContentType contentType) => TryUpdateCurrentMap();

                sldvc.didChangeDifficultyBeatmapEvent += Handler1;
                sldvc.didChangeContentEvent += Handler2;
            }
        }

        private void TryUpdateCurrentMap()
        {
            if (sldvc is not null && sldvc.selectedDifficultyBeatmap is not null)
            {
                UpdateDiff(sldvc.selectedDifficultyBeatmap);
            }
        }

        private void UpdateDiff(IDifficultyBeatmap beatmap)
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
            page = 1; // reset to first page on map change

            // reload leaderboard for the new map
            Task.Run(() => LoadLeaderboardAsync(currentHash, currentDifficulty));
        }

        private async Task LoadLeaderboardAsync(string hash, BeatmapDifficulty diff)
        {
            _scores.Clear();
            AccsaberScoreData[] scores = await AccsaberAPI.GetScoreData(page, hash, diff);
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

        private async Task<int> GetPlayerPage()
        {
            return (int)Math.Ceiling(AccsaberAPI.GetRank(await AccsaberAPI.GetScoreData(Plugin.Instance.PlayerID, currentHash, currentDifficulty.ToString())) / (float)AccsaberAPI.PAGE_LENGTH);
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
