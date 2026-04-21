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

        public static readonly float SMALL_CELL_SIZE = 5.3f;
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
        private JToken currentPlayerScoreInfo;
        private AccsaberScoreDataInfo currentPlayerScore;
        private AsyncLock loadLeaderboardLock = new(), forceRefreshLock = new();
        private LeaderboardDisplayType displayType;
        private Stack<int> previousPages = [];
        private string difficultyId;
        private PlayerProfileModalViewController ppmvc;
        private PlayerMilestoneModalViewController pmmvc;

        private Stack<(int page, int nextPage, IEnumerable<AccsaberScoreData> pageData)> cache = [];
        private bool cachePage;

        public bool ValidMapSelected => !string.IsNullOrEmpty(currentHash) && currentDifficulty != default;
        public bool OnPlayerPage => currentPage <= currentPlayerPage && nextPage > currentPlayerPage;

        #endregion

        #region Injects

        [Inject] private readonly StandardLevelDetailViewController sldvc;

        #endregion

        #region Loading UI objects

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
        [UIValue("colorGrey")] private string grey = GREY;

        [UIObject("leaderboard_badMap")] private GameObject badMapMessage;

        [UIComponent("GlobalSelector")] private ClickableImage globalSelector;
        [UIComponent("FriendsSelector")] private ClickableImage friendsSelector;
        [UIComponent("CountrySelector")] private ClickableImage countrySelector;

        #endregion

        #region UI Actions

        [UIAction("OnCellSelected")]
        private void OnCellSelected(TableView _, AccsaberScoreDataInfo cell)
        {
            ppmvc.ShowPlayer(cell.PlayerId, this);
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            UpdateSelectors(LeaderboardDisplayType.Global);
            ppmvc = new(leaderboardContainer);
            pmmvc = new(leaderboardContainer);
            // Subscribe to player picture click event from PanelViewController
            PanelViewController.OnPlayerPictureClicked += () => ppmvc.ShowPlayer(Plugin.Instance.PlayerID, this);
            PanelViewController.OnLogoClicked += () => pmmvc.ShowMilestoneModal(Plugin.Instance.PlayerID, this);
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
        private void ShowGlobal() => ChangeFilter(LeaderboardDisplayType.Global);

        [UIAction("ShowFriends")]
        private void ShowFriends() => ChangeFilter(LeaderboardDisplayType.Friends);
        [UIAction("ShowCountry")]
        private void ShowCountry() => ChangeFilter(LeaderboardDisplayType.Country);

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

        private void ChangeFilter(LeaderboardDisplayType type)
        {
            if (displayType == type || currentHash is null)
                return;
            page = 1;
            currentPage = 0;
            cache.Clear();
            UpdateSelectors(type);
            FullyReloadLeaderboard();
        }
        private void UpdateSelectors(LeaderboardDisplayType newDisplayType)
        {
            switch (displayType)
            {
                case LeaderboardDisplayType.Global:
                    globalSelector.DefaultColor = Color.white;
                    break;
                case LeaderboardDisplayType.Friends:
                    friendsSelector.DefaultColor = Color.white;
                    previousPages.Clear();
                    break;
                case LeaderboardDisplayType.Country:
                    countrySelector.DefaultColor = Color.white;
                    break;
            }

            switch (newDisplayType)
            {
                case LeaderboardDisplayType.Global:
                    globalSelector.DefaultColor = globalSelector.HighlightColor;
                    break;
                case LeaderboardDisplayType.Friends:
                    friendsSelector.DefaultColor = friendsSelector.HighlightColor;
                    break;
                case LeaderboardDisplayType.Country:
                    countrySelector.DefaultColor = countrySelector.HighlightColor;
                    break;
            }

            displayType = newDisplayType;
        }
        private void FullyReloadLeaderboard()
        {
            Task.Run(async () =>
            {
                currentPlayerPage = await GetPlayerPage(false);
                await LoadLeaderboardAsync(currentHash, currentDifficulty);
            });
        }
        private void ReloadLeaderboard() => Task.Run(() => LoadLeaderboardAsync(currentHash, currentDifficulty));
        
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
            if (levelId.Contains('_'))
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
            AsyncLock.Releaser? theLock = await forceRefreshLock.LockAsync();
            if (theLock is null) return;
            using (theLock.Value)
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

                currentPlayerPage = await GetPlayerPage(true);
                currentPlayerScore = new AccsaberScoreDataInfo(AccsaberAPI.ConvertToScoreData(currentPlayerScoreInfo));
                await LoadLeaderboardAsync(currentHash, currentDifficulty);
            }
        }

        private async Task LoadLeaderboardAsync(string hash, BeatmapDifficulty diff)
        {
            if (page == currentPage) return; // already on this page, no need to reload
            AsyncLock.Releaser? theLock = await loadLeaderboardLock.LockAsync();
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
                    (AccsaberScoreData[] scores, int truePage) scoreData;
                    switch (displayType)
                    {
                        case LeaderboardDisplayType.Global:
                            scores = await AccsaberAPI.GetScoreData(page, difficultyId);
                            nextPage = page + 1;
                            break;
                        case LeaderboardDisplayType.Friends:
                            scoreData = await AccsaberAPI.GetScoreData(page, difficultyId, token => Plugin.Instance.PlayerFriends.Contains(AccsaberAPI.GetPlayerId(token)));
                            scores = scoreData.scores;
                            nextPage = scoreData.truePage;
                            break;
                        case LeaderboardDisplayType.Country:
                            string country = AccsaberAPI.GetCountry(currentPlayerScoreInfo);
                            scores = await AccsaberAPI.GetScoreData(page, difficultyId, country);
                            nextPage = page + 1;
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
                        yield return new WaitForFixedUpdate(); // small delay to ensure data is set before reloading

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

        private async Task<int> GetPlayerPage(bool overrideLastScore)
        {
            if (overrideLastScore || currentPlayerScoreInfo is null)
                currentPlayerScoreInfo = await AccsaberAPI.GetScoreData(Plugin.Instance.PlayerID, currentHash, currentDifficulty);
            if (currentPlayerScoreInfo is null) return -1; // Player has no score on this map
            return (int)Math.Ceiling(AccsaberAPI.GetRank(currentPlayerScoreInfo) / (float)AccsaberAPI.PAGE_LENGTH);
        }
        #endregion
    }
}
