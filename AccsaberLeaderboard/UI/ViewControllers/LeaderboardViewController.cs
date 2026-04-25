using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Models;
using AccsaberLeaderboard.UI.BSML_Addons.Components;
using AccsaberLeaderboard.Utils;
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
using static AccsaberLeaderboard.Utils.ColorPalette;

namespace AccsaberLeaderboard.UI.ViewControllers
{
    [ViewDefinition(ResourcePaths.BSML_LEADERBOARD_VIEW)]
    [HotReload(RelativePathToLayout = @"..\UI\bsml\LeaderboardView.bsml")]
    internal class LeaderboardViewController : BSMLAutomaticViewController
    {
#pragma warning disable IDE0044, IDE0051
        #region Static Variables & Properties

        public const float BIG_CELL_SIZE = 5.9f;
        public const float BIG_FONT_SIZE = 3.5f;

        public const float SMALL_CELL_SIZE = 5.3f;
        public const float SMALL_FONT_SIZE = 3f;

        public static bool LeaderboardOnPlayerPage => Instance.OnPlayerPage;

        private static LeaderboardViewController Instance;
        #endregion

        #region Instance Variables & Fields

        private readonly List<AccsaberScoreData> scoreDatas = [];
        private string currentHash;
        private BeatmapDifficulty currentDifficulty;
        private int page, nextPage, currentPage = -1, currentPlayerPage;
        private AccsaberAPI.ScoreInfoToken currentPlayerScoreInfo;
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
        public bool OnPlayerPage {
            get => displayType switch
            {

                LeaderboardDisplayType.Friends or LeaderboardDisplayType.Global => currentPage <= currentPlayerPage && nextPage > currentPlayerPage,
                LeaderboardDisplayType.Country => scoreDatas.First().rank <= AccsaberAPI.GetRank(currentPlayerScoreInfo) && scoreDatas.Last().rank >= AccsaberAPI.GetRank(currentPlayerScoreInfo),
                _ => false
            };
        }

        #endregion

        #region Injects

        [Inject] private readonly StandardLevelDetailViewController sldvc;

        #endregion

        #region Loading UI objects

        [UIObject("leaderboard_loading")] private GameObject leaderboardLoader;
        [UIObject("leaderboard_container")] private GameObject leaderboardContainer;

        #endregion

        #region UI Values & Components

        [UIValue("colorGrey")] private const string grey = GREY;

        [UIValue("topArrowPic")] private const string topArrowPic = ResourcePaths.RESOURCE_TOP_ARROW;
        [UIValue("youPic")] private const string youPic = ResourcePaths.RESOURCE_YOU;
        [UIValue("globalPic")] private const string globalPic = ResourcePaths.RESOURCE_GLOBAL;
        [UIValue("friendsPic")] private const string friendsPic = ResourcePaths.RESOURCE_FRIENDS;
        [UIValue("countryPic")] private const string countryPic = ResourcePaths.RESOURCE_COUNTRY;

        [UIValue("containerWidth")] public const float containerWidth = 80f;
        [UIValue("containerHeight")] public const float containerHeight = 80f;


        [UIParams] private BSMLParserParams parserParams;
        [UIComponent("leaderboard")] private MyCustomCellListTableData leaderboard;
        [UIValue("leaderboard-infos")] private List<ICellDataSource> LeaderboardInfos 
        {
            get
            {
                IEnumerable<ICellDataSource> outp = scoreDatas.Select(score => (ICellDataSource)new AccsaberScoreDataInfo(score));
                outp = outp.Append(new TextSpacer());
                return [.. outp.Append(currentPlayerScore)];
            }
        }
        [UIValue("leaderboard-cellSize")] private float CellSize => OnPlayerPage ? BIG_CELL_SIZE : SMALL_CELL_SIZE;

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

            // Subscribe to player picture click event & logo clicked event from PanelViewController
            PanelViewController.OnPlayerPictureClicked += () => ppmvc.ShowPlayer(Plugin.Instance.PlayerID, this);
            PanelViewController.OnLogoClicked += () => pmmvc.ShowMilestoneModal(Plugin.Instance.PlayerID, this);

            // Subscribe to the websocket
            AccsaberLiveScores.OnPlayerScoreUpdated += token =>
            {
                currentPlayerScoreInfo = token;
                currentPage = 0;
                Task.Run(() => ForceRefresh(false));
            };

            //MiscUtils.Parse(ResourcePaths.BSML_LEADERBOARD_CELL, leaderboard.transform, );

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
                case LeaderboardDisplayType.Country:
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
            if (scoreDatas.Count < AccsaberAPI.PAGE_LENGTH || currentHash is null)
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
        private async Task ForceRefresh() => await ForceRefresh(true);
        private async Task ForceRefresh(bool overridePlayerScore)
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

                currentPlayerPage = await GetPlayerPage(overridePlayerScore);
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
                        AccsaberScoreData[] copy = new AccsaberScoreData[scoreDatas.Count];
                        scoreDatas.CopyTo(copy);
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

                    scoreDatas.Clear();

                    while (cache.Count > 0)
                    {
                        var item = cache.Pop();
                        if (item.page == page)
                        {
                            scoreDatas.AddRange(item.pageData);
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
                        scoreDatas.AddRange(scores);

                End:
                    IEnumerator ReloadData()
                    {
                        yield return new WaitForEndOfFrame();

                        leaderboard.Data = LeaderboardInfos;

                        yield return new WaitForFixedUpdate(); // small delay to ensure data is set before reloading

                        //leaderboard.TableView.ReloadData();

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
            return (int)Math.Ceiling((AccsaberAPI.GetRank(currentPlayerScoreInfo) - 1) / (float)AccsaberAPI.PAGE_LENGTH);
        }
        #endregion

        private class TextSpacer : ICellDataSource
        {
            public string TemplatePath => "<vertical child-expand-height='false'><text text='...' align='Left' font-size='3'/></vertical>";

            public float CellSize => 1.5f;

            public int TemplateId { get; set; }
        }
    }
}
