using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Harmony;
using AccsaberLeaderboard.Models;
using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Zenject;
using static AccsaberLeaderboard.Models.AccsaberScoreData;

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
        private AsyncLock loadLeaderboardLock = new();

        public bool ValidMapSelected => !string.IsNullOrEmpty(currentHash) && currentDifficulty != default;

        #endregion

        #region Injects

        [Inject] private readonly StandardLevelDetailViewController sldvc;

        #endregion

        #region Loading UI objects

        [UIObject("modal_loading")] private GameObject modalLoader;
        [UIObject("modal_container")] private GameObject modalContainer;

        [UIObject("leaderboard_loading")] private GameObject leaderboardLoader;
        [UIObject("leaderboard")] private GameObject leaderboardContainer;

        #endregion

        #region UI Values & Components

        [UIParams] private BSMLParserParams parserParams;

        [UIComponent("leaderboard")] private CustomCellListTableData leaderboard;

        [UIValue("leaderboard-infos")]
        private List<object> LeaderboardInfos => [.. scoreDatas.Select(score => (object)new AccsaberScoreDataInfo(score))];

        private static readonly List<AccsaberScoreData> scoreDatas = [];

        #endregion

        #region Modal UI Components

        [UIObject("PlayerInfoWindow")] private GameObject playerInfoWindow;

        [UIComponent("modal_playerImage")] private ImageView modalPlayerImage;


        [UIComponent("modal_playerName")] private TextMeshProUGUI modalPlayerName;

        [UIComponent("modal_levelRank")] private TextMeshProUGUI modalLevelRank;
        [UIComponent("modal_level")] private TextMeshProUGUI modalLevel;

        [UIComponent("modal_globalRank")] private TextMeshProUGUI modalGlobalRank;
        [UIComponent("modal_countryRank")] private TextMeshProUGUI modalCountryRank;
        [UIComponent("modal_overall")] private TextMeshProUGUI modalOverall;

        [UIComponent("modal_tech")] private TextMeshProUGUI modalTech;
        [UIComponent("modal_true")] private TextMeshProUGUI modalTrue;
        [UIComponent("modal_standard")] private TextMeshProUGUI modalStandard;
        
        [UIComponent("modal_tech_rank_global")] private TextMeshProUGUI modalTechGlobalRank;  
        [UIComponent("modal_tech_rank_country")] private TextMeshProUGUI modalTechCountryRank;  
        [UIComponent("modal_true_rank_global")] private TextMeshProUGUI modalTrueGlobalRank;  
        [UIComponent("modal_true_rank_country")] private TextMeshProUGUI modalTrueCountryRank;  
        [UIComponent("modal_standard_rank_global")] private TextMeshProUGUI modalStandardGlobalRank;  
        [UIComponent("modal_standard_rank_country")] private TextMeshProUGUI modalStandardCountryRank;  
        #endregion

        #region UI Actions

        [UIAction("OnCellSelected")]
        private void OnCellSelected(TableView _, AccsaberScoreDataInfo cell)
        {
            ShowPlayer(cell.PlayerId);
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            // Subscribe to player picture click event from PanelViewController
            PanelViewController.OnPlayerPictureClicked += () => ShowPlayer(Plugin.Instance.PlayerID);
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
                if (playerPage != page && playerPage >= 0)
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
        }
        private void ShowPlayer(string playerId)
        {
            parserParams.EmitEvent("ShowPlayerInfo");
            IEnumerator WaitThenUpdate()
            {
                yield return new WaitForEndOfFrame();

                (playerInfoWindow.transform as RectTransform).sizeDelta = new Vector2(70, 80);
                modalLoader.SetActive(true);
                modalContainer.SetActive(false);
            }
            StartCoroutine(WaitThenUpdate());
            Task.Run(() => SetupModal(playerId));
        }
        private async Task SetupModal(string playerId)
        {
            JToken playerInfo = await AccsaberAPI.GetPlayerInfo(playerId, true);
            string rank = AccsaberAPI.GetPlayerTitle(playerInfo);
            JToken stats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.Overall);

            IEnumerator SetTexts()
            {
                yield return new WaitForEndOfFrame();

                modalPlayerName.SetText(AccsaberAPI.GetPlayerName(playerInfo));

                modalLevelRank.SetText($"<color={MiscUtils.GetColorForTitle(rank)}>{rank}</color>");
                modalLevel.SetText($"<color=#0F0>Level {AccsaberAPI.GetPlayerLevel(playerInfo)}</color>");

                modalGlobalRank.SetText($"<color=#0FF>#{AccsaberAPI.GetGlobalRank(stats)}</color>");
                modalOverall.SetText($"<color=#FF0>{AccsaberAPI.GetAP(stats):N2}ap</color>");
                modalCountryRank.SetText($"<color=#F0F>#{AccsaberAPI.GetCountryRank(stats)}</color>");

                stats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.Tech);

                modalTech.SetText($"<color=#F55>{AccsaberAPI.GetAP(stats):N2}ap</color>");
                modalTechGlobalRank.SetText($"<color=#0AA>#{AccsaberAPI.GetGlobalRank(stats)}</color>");
                modalTechCountryRank.SetText($"<color=#A0A>#{AccsaberAPI.GetCountryRank(stats)}</color>");

                stats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.True);
                modalTrue.SetText($"<color=#090>{AccsaberAPI.GetAP(stats):N2}ap</color>");
                modalTrueGlobalRank.SetText($"<color=#0AA>#{AccsaberAPI.GetGlobalRank(stats)}</color>");
                modalTrueCountryRank.SetText($"<color=#A0A>#{AccsaberAPI.GetCountryRank(stats)}</color>");

                stats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.Standard);
                modalStandard.SetText($"<color=#33F>{AccsaberAPI.GetAP(stats):N2}ap</color>");
                modalStandardGlobalRank.SetText($"<color=#0AA>#{AccsaberAPI.GetGlobalRank(stats)}</color>");  
                modalStandardCountryRank.SetText($"<color=#A0A>#{AccsaberAPI.GetCountryRank(stats)}</color>");

                //Below line taken from: https://github.com/accsaber/accsaber-plugin/blob/dev/leaderboard-1.38/AccSaber/UI/ViewControllers/LeaderboardUserModalController.cs#L182
                modalPlayerImage.material = Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "UINoGlowRoundEdge");
#if NEW_VERSION
                modalPlayerImage.SetImageAsync(AccsaberAPI.GetPlayerAvatar(playerInfo));
#else
                modalPlayerImage.SetImage(AccsaberAPI.GetPlayerAvatar(playerInfo));
#endif

                modalLoader.SetActive(false);
                modalContainer.SetActive(true);
            }
            StartCoroutine(SetTexts());

        }
        private void TrySubscribeToMapSelection()
        {
            if (sldvc is not null)
            {
#if NEW_VERSION
                void Handler1(StandardLevelDetailViewController controller) => TryUpdateCurrentMap();
#else
                void Handler1(StandardLevelDetailViewController controller, IDifficultyBeatmap beatmap) => UpdateDiff(beatmap);
#endif
                void Handler2(StandardLevelDetailViewController controller, StandardLevelDetailViewController.ContentType contentType) => TryUpdateCurrentMap();

                sldvc.didChangeDifficultyBeatmapEvent -= Handler1;
                sldvc.didChangeContentEvent -= Handler2;

                sldvc.didChangeDifficultyBeatmapEvent += Handler1;
                sldvc.didChangeContentEvent += Handler2;
            }
        }

        private void TryUpdateCurrentMap()
        {
#if NEW_VERSION
            if (sldvc is not null && sldvc.beatmapLevel is not null)
                UpdateDiff(sldvc.beatmapLevel, sldvc.beatmapKey);
#else
            if (sldvc is not null && sldvc.selectedDifficultyBeatmap is not null)
                UpdateDiff(sldvc.selectedDifficultyBeatmap);
#endif
        }

#if NEW_VERSION
        private void UpdateDiff(BeatmapLevel beatmap, BeatmapKey key)
        {
#else
        private void UpdateDiff(IDifficultyBeatmap beatmap)
        {
            if (beatmap is null || beatmap.level is null)
                return;
#endif
            // Get hash from the level (custom levels use levelID format: "custom_level_HASH")
#if NEW_VERSION
            string levelId = beatmap.levelID;
#else
            string levelId = beatmap.level.levelID;
#endif
            string hash;
            if (levelId.StartsWith("custom_level_"))
                hash = levelId.Substring("custom_level_".Length);
            else
                hash = levelId; // fallback for official levels

            currentHash = hash;
#if NEW_VERSION
            currentDifficulty = key.difficulty;
#else
            currentDifficulty = beatmap.difficulty;
#endif
            page = 1; // reset to first page on map change

            // reload leaderboard for the new map
            Task.Run(() => LoadLeaderboardAsync(currentHash, currentDifficulty));
        }

        private async Task LoadLeaderboardAsync(string hash, BeatmapDifficulty diff)
        {
            AsyncLock.Releaser? theLock = await loadLeaderboardLock.TryLockAsync();
            if (theLock is null) return;
            using (theLock.Value)
            {
                try
                {
                    IEnumerator ShowLoading()
                    {
                        yield return new WaitForEndOfFrame();
                        leaderboardContainer.SetActive(false);
                        leaderboardLoader.SetActive(true);
                    }
                    StartCoroutine(ShowLoading());

                    _scores.Clear();
                    AccsaberScoreData[] scores = await AccsaberAPI.GetScoreData(page, hash, diff);
                    if (scores is not null)
                        _scores.AddRange(scores);

                    //leaderboard.SetDataSource(new AccsaberLeaderboardTableView(_scores), reloadData: true);
                    IEnumerator ReloadData()
                    {
                        yield return new WaitForEndOfFrame();
#if NEW_VERSION
                        leaderboard.Data = LeaderboardInfos;
#else
                        leaderboard.data = LeaderboardInfos;
#endif
                        yield return new WaitForSeconds(0.05f); // small delay to ensure data is set before reloading

#if NEW_VERSION
                        leaderboard.TableView.ReloadData();
#else
                        leaderboard.tableView.ReloadData();
#endif
                        leaderboardContainer.SetActive(true);
                        leaderboardLoader.SetActive(false);
                    }
                    StartCoroutine(ReloadData());
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"Error loading leaderboard: {ex}");
                }
            }
        }

        private async Task<int> GetPlayerPage()
        {
            JToken scoreInfo = await AccsaberAPI.GetScoreData(Plugin.Instance.PlayerID, currentHash, currentDifficulty.ToString());
            if (scoreInfo is null) return -1; // Player has no score on this map
            return (int)Math.Ceiling(AccsaberAPI.GetRank(scoreInfo) / (float)AccsaberAPI.PAGE_LENGTH);
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
