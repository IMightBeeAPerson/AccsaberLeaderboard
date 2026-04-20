using AccsaberLeaderboard.API;
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
using UnityEngine.UI;
using UnityEngine.UIElements;
using Zenject;

using static AccsaberLeaderboard.Models.AccsaberScoreData;
using static AccsaberLeaderboard.Utils.ColorPalette;

namespace AccsaberLeaderboard.UI.ViewControllers
{
    [ViewDefinition("AccsaberLeaderboard.UI.bsml.LeaderboardView.bsml")]
    [HotReload(RelativePathToLayout = @"..\UI\bsml\LeaderboardView.bsml")]
    internal class LeaderboardViewController : BSMLAutomaticViewController
    {
#pragma warning disable IDE0044, IDE0051
        #region Static Variables & Properties

        public static readonly float SMALL_CELL_SIZE = 5.1f;
        public static readonly float BIG_CELL_SIZE = 5.9f;

        public static bool LeaderboardOnPlayerPage => Instance.OnPlayerPage;

        private static event Action RefreshRequested;
        private static readonly List<AccsaberScoreData> scoreDatas = [];

        private static LeaderboardViewController Instance;
        #endregion

        #region Instance Variables & Fields

        private readonly List<AccsaberScoreData> _scores = scoreDatas;
        private string currentHash;
        private BeatmapDifficulty currentDifficulty;
        private int page, nextPage, currentPage = -1, currentPlayerPage;
        private AccsaberScoreDataInfo currentPlayerScore;
        private AsyncLock loadLeaderboardLock = new();
        private LeaderboardDisplayType displayType;
        private Stack<int> previousPages = [];
        private string difficultyId;

        private Stack<(int page, int nextPage, IEnumerable<AccsaberScoreData> pageData)> cache = [];
        private bool cachePage;

        public bool ValidMapSelected => !string.IsNullOrEmpty(currentHash) && currentDifficulty != default;
        public bool OnPlayerPage => currentPage <= currentPlayerPage && nextPage > currentPlayerPage;

        #endregion

        #region Injects

        [Inject] private readonly StandardLevelDetailViewController sldvc;

        #endregion

        #region Loading UI objects

        [UIObject("modal_loading")] private GameObject modalLoader;
        [UIObject("modal_container")] private GameObject modalContainer;

        [UIObject("leaderboard_loading")] private GameObject leaderboardLoader;
        [UIObject("leaderboard_container")] private GameObject leaderboardContainer;

        #endregion

        #region Player UI Values & Components

#pragma warning disable IDE0052
        [UIValue("player_fontSize")] private float playerFontSize = AccsaberScoreDataInfo.SMALL_FONT_SIZE;
        [UIValue("player_BGColor")] private string playerBGColor = HIGHLIGHT;
#pragma warning restore IDE0052

        [UIObject("player_container")] private GameObject playerContainer;

        [UIComponent("player_rankText")] private TextMeshProUGUI playerRankText;
        [UIComponent("player_nameText")] private TextMeshProUGUI playerNameText;
        [UIComponent("player_apText")] private TextMeshProUGUI playerApText;
        [UIComponent("player_accText")] private TextMeshProUGUI playerAccText;
        [UIComponent("player_scoreText")] private TextMeshProUGUI playerScoreText;

        #endregion

        #region UI Values & Components

        [UIParams] private BSMLParserParams parserParams;
        [UIComponent("leaderboard")] private CustomCellListTableData leaderboard;
        [UIValue("leaderboard-infos")] private List<object> LeaderboardInfos => [.. scoreDatas.Select(score => (object)new AccsaberScoreDataInfo(score))];
        [UIValue("leaderboard-cellSize")] private float CellSize => OnPlayerPage ? BIG_CELL_SIZE : SMALL_CELL_SIZE;

        [UIObject("leaderboard_badMap")] private GameObject badMapMessage;

        [UIComponent("GlobalSelector")] private ClickableImage globalSelector;
        [UIComponent("FriendsSelector")] private ClickableImage friendsSelector;

        #endregion

        #region Modal UI Components

        [UIObject("PlayerInfoWindow")] private GameObject playerInfoWindow;

        [UIComponent("modal_playerImage")] private ImageView modalPlayerImage;

        [UIComponent("modal_playerName")] private TextMeshProUGUI modalPlayerName;

        [UIComponent("modal_levelRank")] private TextMeshProUGUI modalLevelRank;
        [UIComponent("modal_level")] private TextMeshProUGUI modalLevel;

        [UIComponent("modal_levelProgress")] private LayoutElement modalLevelProgress;
        [UIComponent("modal_levelProgress")] private ImageView modalLevelProgress_image;
        [UIComponent("modal_levelProgressInverse")] private LayoutElement modalLevelProgressInverse;
        [UIComponent("modal_levelProgressInverse")] private ImageView modalLevelProgressInverse_image;
        [UIComponent("modal_levelProgressNumber")] private TextMeshProUGUI modalLevelProgressNumber;

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
            displayType = LeaderboardDisplayType.Global;
            UpdateSelectors();
            // Subscribe to player picture click event from PanelViewController
            PanelViewController.OnPlayerPictureClicked += () => ShowPlayer(Plugin.Instance.PlayerID);
            // Subscribe to refresh event from other controllers
            RefreshRequested += () =>
            {
                currentPage = 0; // reset current page to force reload
                Task.Run(ForceRefresh);
            };
            // Subscribe to map selection event
            TrySubscribeToMapSelection();
            // Optionally, load leaderboard for the current map if available
            TryUpdateCurrentMap();
        }

        [UIAction("OnPageTop")]
        private void OnPageTop()
        {
            if (page == 1 || currentHash is null) return; // Already on the first page
            page = 1;
            if (displayType == LeaderboardDisplayType.Friends)
                previousPages.Clear();
            cache.Clear();
            ReloadLeaderboard();
        }

        [UIAction("OnPageUp")]
        private void OnPageUp()
        {
            if (page == 1 || currentHash is null) return; // Can't go back from the first page
            switch (displayType)
            {
                case LeaderboardDisplayType.Global:
                    page--;
                    break;
                case LeaderboardDisplayType.Friends:
                    page = previousPages.Pop();
                    break;
            }
            cachePage = true;
            ReloadLeaderboard();
        }

        [UIAction("OnYouClicked")]
        private void OnYouClicked()
        {
            if (page == 0 || displayType != LeaderboardDisplayType.Global || currentHash is null) return;
            page = currentPlayerPage;
            cache.Clear();
            ReloadLeaderboard();
        }

        [UIAction("OnPageDown")]
        private void OnPageDown()
        {
            if (_scores.Count < AccsaberAPI.PAGE_LENGTH || currentHash is null)
                return;
            if (displayType == LeaderboardDisplayType.Friends)
                previousPages.Push(page);
            page = nextPage;
            cachePage = true;
            ReloadLeaderboard();
        }

        [UIAction("ShowGlobal")]
        private void ShowGlobal()
        {
            if (displayType == LeaderboardDisplayType.Global || currentHash is null)
                return;
            page = 1;
            currentPage = 0;
            cache.Clear();
            previousPages.Clear();
            displayType = LeaderboardDisplayType.Global;
            UpdateSelectors();
            FullyReloadLeaderboard();
        }

        [UIAction("ShowFriends")]
        private void ShowFriends()
        {
            if (displayType == LeaderboardDisplayType.Friends || currentHash is null)
                return;
            page = 1;
            currentPage = 0;
            cache.Clear();
            displayType = LeaderboardDisplayType.Friends;
            UpdateSelectors();
            FullyReloadLeaderboard();
        }

        #endregion
        #region Public Methods

        public static void ForceUpdate() => RefreshRequested?.Invoke();

        #endregion
        #region Private Methods
        private void Awake()
        {
            Plugin.Log.Debug("LeaderboardViewController Awake");
            Instance = this;
        }

        private void UpdateSelectors()
        {
            switch (displayType)
            {
                case LeaderboardDisplayType.Global:
                    globalSelector.DefaultColor = globalSelector.HighlightColor;
                    friendsSelector.DefaultColor = Color.white;
                    break;
                case LeaderboardDisplayType.Friends:
                    globalSelector.DefaultColor = Color.white;
                    friendsSelector.DefaultColor = friendsSelector.HighlightColor;
                    break;
            }
        }
        private void FullyReloadLeaderboard()
        {
            Task.Run(async () =>
            {
                currentPlayerPage = await GetPlayerPage();
                await LoadLeaderboardAsync(currentHash, currentDifficulty);
            });
        }
        private void ReloadLeaderboard() => Task.Run(() => LoadLeaderboardAsync(currentHash, currentDifficulty));
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
                modalLevel.SetText($"<color={LEVEL}>Level {AccsaberAPI.GetPlayerLevel(playerInfo)}</color>");

                float xpPercent = AccsaberAPI.GetPlayerXPPercent(playerInfo);
                modalLevelProgressNumber.SetText($"<color={GREY}>{xpPercent:N2}%</color>");
                xpPercent /= 100f;

                const float barLen = 50f;

                modalLevelProgress.transform.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barLen * xpPercent);
                modalLevelProgressInverse.transform.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barLen * (1 - xpPercent));

                modalLevelProgress_image.color = MiscUtils.ConvertHex(MiscUtils.GetColorForTitle(((LevelTitles)Enum.Parse(typeof(LevelTitles), rank) + 1).ToString()));
                modalLevelProgressInverse_image.color = MiscUtils.ConvertHex(MiscUtils.GetColorForTitle(rank));

                modalGlobalRank.SetText($"<color={GLOBAL}>#{AccsaberAPI.GetGlobalRank(stats)}</color>");
                modalOverall.SetText($"<color={OVERALL}>{AccsaberAPI.GetAP(stats):N2}ap</color>");
                modalCountryRank.SetText($"<color={COUNTRY}>#{AccsaberAPI.GetCountryRank(stats)}</color>");

                stats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.Tech);

                modalTech.SetText($"<color={TECH}>{AccsaberAPI.GetAP(stats):N2}ap</color>");
                modalTechGlobalRank.SetText($"<color={GLOBAL_DIM}>#{AccsaberAPI.GetGlobalRank(stats)}</color>");
                modalTechCountryRank.SetText($"<color={COUNTRY_DIM}>#{AccsaberAPI.GetCountryRank(stats)}</color>");

                stats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.True);
                modalTrue.SetText($"<color={TRUE}>{AccsaberAPI.GetAP(stats):N2}ap</color>");
                modalTrueGlobalRank.SetText($"<color={GLOBAL_DIM}>#{AccsaberAPI.GetGlobalRank(stats)}</color>");
                modalTrueCountryRank.SetText($"<color={COUNTRY_DIM}>#{AccsaberAPI.GetCountryRank(stats)}</color>");

                stats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.Standard);
                modalStandard.SetText($"<color={STANDARD}>{AccsaberAPI.GetAP(stats):N2}ap</color>");
                modalStandardGlobalRank.SetText($"<color={GLOBAL_DIM}>#{AccsaberAPI.GetGlobalRank(stats)}</color>");  
                modalStandardCountryRank.SetText($"<color={COUNTRY_DIM}>#{AccsaberAPI.GetCountryRank(stats)}</color>");

                //Below line taken from: https://github.com/accsaber/accsaber-plugin/blob/dev/leaderboard-1.38/AccSaber/UI/ViewControllers/LeaderboardUserModalController.cs#L182
                modalPlayerImage.material = Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "UINoGlowRoundEdge");
#if NEW_VERSION
                modalPlayerImage.SetImageAsync(AccsaberAPI.GetPlayerAvatar(playerInfo));
#else
                modalPlayerImage.SetImage(AccsaberAPI.GetPlayerAvatar(playerInfo));
#endif
                yield return new WaitForFixedUpdate();

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
                void Handler1(StandardLevelDetailViewController controller)
                {
                    TryUpdateCurrentMap();
                }
#else
                void Handler1(StandardLevelDetailViewController controller, IDifficultyBeatmap beatmap)
                {
                    if (beatmap is not null)
                        UpdateDiff(beatmap);
                }
#endif
                void Handler2(StandardLevelDetailViewController controller, StandardLevelDetailViewController.ContentType contentType)
                {
                    if (contentType > StandardLevelDetailViewController.ContentType.Loading && contentType < StandardLevelDetailViewController.ContentType.Error)
                        TryUpdateCurrentMap();
                }

                sldvc.didChangeDifficultyBeatmapEvent -= Handler1;
                sldvc.didChangeContentEvent -= Handler2;

                sldvc.didChangeDifficultyBeatmapEvent += Handler1;
                sldvc.didChangeContentEvent += Handler2;
            }
        }

        private void TryUpdateCurrentMap()
        {
#if NEW_VERSION
            if (sldvc is not null && sldvc.beatmapLevel is not null && sldvc.beatmapKey != default)
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
#endif      
            //Plugin.Log.Info("Update called.");
            // Get hash from the level (custom levels use levelID format: "custom_level_HASH")
#if NEW_VERSION
            string levelId = beatmap.levelID;
#else
            string levelId = beatmap.level.levelID;
#endif
            string hash;
            if (levelId.StartsWith("custom_level_"))
                hash = levelId.Split('_')[2];
            else
                hash = levelId; // fallback for official levels

#if NEW_VERSION
            if (hash.Equals(currentHash) && key.difficulty.Equals(currentDifficulty))
                return; // same map, no need to update
            currentDifficulty = key.difficulty;
#else
            if (hash.Equals(currentHash) && beatmap.difficulty.Equals(currentDifficulty))
                return; // same map, no need to update
            currentDifficulty = beatmap.difficulty;
#endif
            currentHash = hash;

            page = 1; // reset to first page on map change
            currentPage = 0;
            currentPlayerPage = 0;
            cache.Clear();
            cachePage = false;

            // reload leaderboard for the new map
            Task.Run(ForceRefresh);
        }
        private async Task ForceRefresh()
        {
            difficultyId = await AccsaberAPI.GetLeaderboardDifficultyId(currentHash, currentDifficulty);

            if (difficultyId is null)
            {
                currentHash = null;
                currentDifficulty = default;
                IEnumerator ShowBad()
                {
                    yield return new WaitForEndOfFrame();
                    leaderboardContainer.SetActive(false);
                    badMapMessage.SetActive(true);
                }
                StartCoroutine(ShowBad());
                return;
            }

            JToken scoreInfo = await AccsaberAPI.GetScoreData(Plugin.Instance.PlayerID, currentHash, currentDifficulty);
            currentPlayerScore = new AccsaberScoreDataInfo(AccsaberAPI.ConvertToScoreData(scoreInfo));
            currentPlayerPage = await GetPlayerPage(scoreInfo);
            await LoadLeaderboardAsync(currentHash, currentDifficulty);
        }

        private async Task LoadLeaderboardAsync(string hash, BeatmapDifficulty diff)
        {
            if (page == currentPage) return; // already on this page, no need to reload
            AsyncLock.Releaser? theLock = await loadLeaderboardLock.TryLockAsync();
            if (theLock is null) return;
            using (theLock.Value)
            {
                try
                {
                    (int, int, IEnumerable<AccsaberScoreData>) toCache;
                    if (cachePage)
                    {
                        AccsaberScoreData[] copy = new AccsaberScoreData[_scores.Count];
                        _scores.CopyTo(copy);
                        toCache = (currentPage, nextPage, copy);
                    }
                    else toCache = default;

                        currentPage = page;
                    IEnumerator ShowLoading()
                    {
                        yield return new WaitForEndOfFrame();
                        badMapMessage.SetActive(false);
                        leaderboardContainer.SetActive(false);
                        leaderboardLoader.SetActive(true);
                    }
                    StartCoroutine(ShowLoading());

                    _scores.Clear();

                    while (cache.Count > 0)
                    {
                        var item = cache.Pop();
                        if (item.page == page)
                        {
                            _scores.AddRange(item.pageData);
                            nextPage = item.nextPage;
                            //Plugin.Log.Info($"Using cache for page #{item.page}.");
                            goto End;
                        }
                        if (item.page > page)
                        {
                            //Plugin.Log.Info($"Discarding cache on page #{item.page}.");
                            continue;
                        }
                        cache.Push(item);
                        break;
                    }

                    AccsaberScoreData[] scores;
                    switch (displayType)
                    {
                        case LeaderboardDisplayType.Global:
                            scores = await AccsaberAPI.GetScoreData(page, difficultyId);
                            nextPage = page + 1;
                            break;
                        case LeaderboardDisplayType.Friends:
                            var scoreData = await AccsaberAPI.GetScoreData(page, difficultyId, Plugin.Instance.PlayerFriends);
                            scores = scoreData.scores;
                            nextPage = scoreData.truePage;
                            break;
                        default:
                            scores = null;
                            break;
                    }
                    if (scores is not null)
                        _scores.AddRange(scores);

                End:
                    IEnumerator ReloadData()
                    {
                        yield return new WaitForEndOfFrame();
#if NEW_VERSION
                        leaderboard.Data = LeaderboardInfos;
                        leaderboard.CellSizeValue = CellSize;
#else
                        leaderboard.data = LeaderboardInfos;
                        leaderboard.cellSize = CellSize;
#endif
                        if (_scores.Count > 0 && !OnPlayerPage)
                        {
                            playerRankText.SetText(currentPlayerScore.Rank);
                            playerNameText.SetText(currentPlayerScore.PlayerName);
                            playerApText.SetText(currentPlayerScore.AP);
                            playerAccText.SetText(currentPlayerScore.Acc);
                            playerScoreText.SetText(currentPlayerScore.Score);
                            playerContainer.SetActive(true);
                        }
                        else playerContainer.SetActive(false);
                        yield return new WaitForSeconds(0.05f); // small delay to ensure data is set before reloading

#if NEW_VERSION
                        leaderboard.TableView.ReloadData();
#else
                        leaderboard.tableView.ReloadData();
#endif
                        leaderboardContainer.SetActive(true);
                        leaderboardLoader.SetActive(false);
                    }

                    if (cachePage)
                    {
                        cache.Push(toCache);
                        cachePage = false;
                        //Plugin.Log.Info($"Cached page #{cache.Peek().page}, stored {cache.Peek().pageData.Count()} scores.");
                    }
                    StartCoroutine(ReloadData());
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"Error loading leaderboard: {ex}");
                }
            }
        }

        private async Task<int> GetPlayerPage(JToken scoreInfo = null)
        {
            scoreInfo ??= await AccsaberAPI.GetScoreData(Plugin.Instance.PlayerID, currentHash, currentDifficulty);
            if (scoreInfo is null) return -1; // Player has no score on this map
            return (int)Math.Ceiling(AccsaberAPI.GetRank(scoreInfo) / (float)AccsaberAPI.PAGE_LENGTH);
        }
        #endregion
    }
}
