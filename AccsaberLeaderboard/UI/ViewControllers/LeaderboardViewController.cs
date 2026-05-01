using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Models;
using AccsaberLeaderboard.UI.BSML_Addons.Components;
using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Zenject;
using AccsaberLeaderboard.UI.Components;
using AccsaberLeaderboard.Harmony;
using UnityEngine.UI;
using AccsaberLeaderboard.Configuration;
using BeatSaberMarkupLanguage;
using System.Reflection;

using static AccsaberLeaderboard.Models.AccsaberScoreData;
using static AccsaberLeaderboard.Utils.ColorPalette;
using static AccsaberLeaderboard.API.AccsaberAPI;

using Screen = HMUI.Screen;


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

        public static LeaderboardViewController Instance { get; private set; }
        private static TextSpacer Spacer = new();
        #endregion

        #region Instance Variables & Fields

        private readonly List<AccsaberScoreData> scoreDatas = [];
        private string currentHash;
        private BeatmapDifficulty currentDifficulty;
        private int page, nextPage, currentPage = -1, currentPlayerPage;
        private ScoreInfoToken currentPlayerScoreInfo;
        private AccsaberScoreDataInfo currentPlayerScore;
        private AsyncLock loadLeaderboardLock = new(), forceRefreshLock = new();
        private LeaderboardDisplayType displayType;
        private Stack<int> previousPages = [];
        private DifficultyInfoToken difficultyInfo;
        private PlayerScoreModalViewController psmvc;
        private PlayerMilestoneModalViewController pmmvc;
        private bool refreshRequested = false, loaded = false;

        private string titlePanelTitle;
        private Color titlePanelC;
        private Sprite titleDefaultSprite, titleCorrectSprite;

        public LeaderboardDisplayType DisplayType => displayType;
        private string DifficultyId => difficultyInfo is null ? null : GetDifficultyId(difficultyInfo);

        public bool ValidMapSelected => !string.IsNullOrEmpty(currentHash) && currentDifficulty != default;
        public bool OnPlayerPage {
            get
            {
                if (currentPlayerPage == -1) return true;
                return displayType switch
                {

                    LeaderboardDisplayType.Country => scoreDatas.First().rank <= GetRank(currentPlayerScoreInfo) && scoreDatas.Last().rank >= GetRank(currentPlayerScoreInfo),
                    _ => currentPage <= currentPlayerPage && (nextPage > currentPlayerPage || currentPage == nextPage)
                };
            }
        }
        public bool BelowPlayerPage
        {
            get
            {
                if (currentPlayerPage == -1) return false;
                return displayType switch
                {
                    LeaderboardDisplayType.Country => scoreDatas.First().rank > GetRank(currentPlayerScoreInfo),
                    _ => currentPage > currentPlayerPage
                };
            }
        }
        public bool UsesPreviousPages => displayType == LeaderboardDisplayType.Friends || displayType == LeaderboardDisplayType.Rivals;

        #endregion

        #region Injects

        [Inject] private readonly StandardLevelDetailViewController sldvc;

        #endregion

        #region Loading UI objects

        [UIObject("leaderboard_loading")] private GameObject leaderboardLoader;
        [UIObject("leaderboard")] private GameObject leaderboardContainer;

        #endregion

        #region UI Values & Components

        [UIValue("colorGrey")] private const string grey = GREY;
        [UIValue("mapStarColor")] private const string mapStarColor = OVERALL_DIM;

        [UIValue("topArrowPic")] private const string topArrowPic = ResourcePaths.RESOURCE_TOP_ARROW;
        [UIValue("youPic")] private const string youPic = ResourcePaths.RESOURCE_YOU;
        [UIValue("swapPic")] private const string swapPic = ResourcePaths.RESOURCE_SWAP;
        [UIValue("globalPic")] private const string globalPic = ResourcePaths.RESOURCE_GLOBAL;
        [UIValue("friendsPic")] private const string friendsPic = ResourcePaths.RESOURCE_FRIENDS;
        [UIValue("followedPic")] private const string followedPic = ResourcePaths.RESOURCE_FOLLOWED;
        [UIValue("rivalsPic")] private const string rivalsPic = ResourcePaths.RESOURCE_RIVALS;
        [UIValue("relationsPic")] private const string relationsPic = ResourcePaths.RESOURCE_RELATIONS;
        [UIValue("countryPic")] private const string countryPic = ResourcePaths.RESOURCE_COUNTRY;
        [UIValue("complexityBG")] private const string complexityBG = ResourcePaths.RESOURCE_GRADIENT_CORNER;

        [UIValue("containerWidth")] public const float containerWidth = 80f;
        [UIValue("containerHeight")] public const float containerHeight = 80f;

        [UIValue("complexityFontSize")] public const float complexityFontSize = 5f;

        [UIParams] private BSMLParserParams parserParams;
        [UIComponent("leaderboard")] private MyCustomCellListTableData leaderboard;
        [UIValue("leaderboard-infos")] private List<ICellDataSource> LeaderboardInfos 
        {
            get
            {
                IEnumerable<ICellDataSource> outp = scoreDatas.Select(score => (ICellDataSource)new AccsaberScoreDataInfo(score));
                if (currentPlayerScore is not null && !OnPlayerPage)
                    return BelowPlayerPage ? [.. outp.Prepend(Spacer).Prepend(currentPlayerScore)] : [.. outp.Append(Spacer).Append(currentPlayerScore)];
                return [.. outp];
            }
        }
        private float CellSize => OnPlayerPage ? BIG_CELL_SIZE : SMALL_CELL_SIZE;

        [UIObject("leaderboard_badMap")] private GameObject badMapMessage;

        [UIObject("titleContainer")] private GameObject titleContainer;

        [UIComponent("GlobalSelector")] private ClickableImage globalSelector;
        [UIComponent("FriendsSelector")] private ClickableImage friendsSelector;
        [UIComponent("FollowedSelector")] private ClickableImage followedSelector;
        [UIComponent("RivalsSelector")] private ClickableImage rivalsSelector;
        [UIComponent("RelationsSelector")] private ClickableImage relationsSelector;
        [UIComponent("CountrySelector")] private ClickableImage countrySelector;

        [UIComponent("mapStarText")] private TextMeshProUGUI mapStarText;
        [UIComponent("mapTypeText")] private TextMeshProUGUI mapTypeText;

        [UIComponent("selectorContainer")] private LayoutElement selectorContainer;

        [UIComponent("mapStarContainer")] private CustomBackground mapStarContainer;
        [UIComponent("mapModeContainer")] private CustomBackground mapModeContainer;

        #endregion

        #region UI Actions

        [UIAction("OnCellSelected")]
        private void OnCellSelected(ICellDataSource cell)
        {
            if (cell is AccsaberScoreDataInfo dataInfo)
                psmvc.ShowModal(this, dataInfo.ScoreInfo);
        }
        [UIAction("ToggleCombinedIcons")]
        private void ToggleCombinedIcons()
        {
            bool toggle = !PluginConfig.Instance.CombineRelations;
            IEnumerator UpdateUI()
            {
                yield return new WaitForEndOfFrame();

                friendsSelector.gameObject.SetActive(!toggle);
                followedSelector.gameObject.SetActive(!toggle);
                rivalsSelector.gameObject.SetActive(!toggle);
                relationsSelector.gameObject.SetActive(toggle);

                selectorContainer.preferredHeight = toggle ? 25f : 35f;
            }
            StartCoroutine(UpdateUI());
            PluginConfig.Instance.CombineRelations = toggle;
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            if (loaded)
                return;
            loaded = true;

            HandleHeaderPane();

            UpdateSelectors(LeaderboardDisplayType.Global);

            psmvc = new(leaderboardContainer);
            pmmvc = new(leaderboardContainer);

            if (PluginConfig.Instance.CombineRelations)
            {
                PluginConfig.Instance.CombineRelations = false;
                ToggleCombinedIcons();
            }

            // Subscribe to player picture click event & logo clicked event from PanelViewController
            PanelViewController.OnPlayerPictureClicked += () => psmvc.ppmvc.ShowPlayer(PlayerSocialLife.PlayerID, this);
            PanelViewController.OnLogoClicked += () => pmmvc.ShowMilestoneModal(PlayerSocialLife.PlayerID, this);

            LeaderboardShownPatch.LeaderboardShown += () =>
            {
                titleContainer.SetActive(true);

                if (!TryUpdateCurrentMap() && refreshRequested)
                    Task.Run(ForceRefresh);
            };
            LeaderboardHiddenPatch.LeaderboardHidden += () =>
            {
                titleContainer.SetActive(false);
            };

            // Subscribe to the websocket
            AccsaberLiveScores.OnPlayerScoreUpdated += token =>
            {
                currentPlayerScoreInfo = token;
                currentPage = 0;
                Task.Run(async () =>
                {
                    if (!await ForceRefresh(true))
                        refreshRequested = true;
                });
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
            if (UsesPreviousPages)
                previousPages.Clear();
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
                case LeaderboardDisplayType.Followed:
                case LeaderboardDisplayType.Rivals:
                case LeaderboardDisplayType.Relations:
                    page = previousPages.Pop();
                    break;
            }
            ReloadLeaderboard();
        }

        [UIAction("OnYouClicked")]
        private void OnYouClicked()
        {
            if (page == 0 || displayType != LeaderboardDisplayType.Global || currentHash is null) return;
            page = currentPlayerPage;
            ReloadLeaderboard();
        }

        [UIAction("OnPageDown")]
        private void OnPageDown()
        {
            if (scoreDatas.Count < PAGE_LENGTH || currentHash is null)
                return;
            if (UsesPreviousPages)
                previousPages.Push(page);
            page = nextPage;
            ReloadLeaderboard();
        }

        [UIAction("ShowGlobal")]
        private void ShowGlobal() => ChangeFilter(LeaderboardDisplayType.Global);

        [UIAction("ShowFriends")]
        private void ShowFriends() => ChangeFilter(LeaderboardDisplayType.Friends);
        [UIAction("ShowFollowed")]
        private void ShowFollowed() => ChangeFilter(LeaderboardDisplayType.Followed);
        [UIAction("ShowRivals")]
        private void ShowRivals() => ChangeFilter(LeaderboardDisplayType.Rivals);
        [UIAction("ShowRelations")]
        private void ShowRelations() => ChangeFilter(LeaderboardDisplayType.Relations);
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

        private GameObject GetHeaderPane()
        {
            // Code for finding this header taken from: https://github.com/BeatLeader/beatleader-mod/blob/1.29.4/Source/2_Core/Managers/Leaderboard/LeaderboardHeaderManager.cs
            Screen screen = gameObject.GetComponentInParent<Screen>();
            if (screen is null) return null;

            //var leaderboardVC = screen.transform.FindChildRecursive("PlatformLeaderboardViewController");
            Transform leaderboardVC = screen.transform.Find("PlatformLeaderboardViewController");
            if (leaderboardVC is null) return null;

            //GameObject header = leaderboardVC.transform.FindChildRecursive("HeaderPanel").gameObject;
            return leaderboardVC.transform.Find("HeaderPanel").gameObject;
        }
        private void HandleHeaderPane()
        {
            GameObject headerPane = GetHeaderPane();
            if (headerPane is null) return;

            TextMeshProUGUI title = headerPane.GetComponentInChildren<TextMeshProUGUI>();
            ImageView headerBG = headerPane.GetComponentInChildren<ImageView>();

            titleDefaultSprite = headerBG.sprite;
            titleCorrectSprite = Utilities.LoadSpriteRaw(Utilities.GetResource(Assembly.GetExecutingAssembly(), ResourcePaths.RESOURCE_GRADIENT_HEADER));

            LeaderboardShownPatch.LeaderboardShown += () =>
            {
                titlePanelTitle = title.text;
                title.SetText("Accsaber");

                titlePanelC = headerBG.color;

                headerBG.color = Color.white;
                headerBG.gradient = false;
                headerBG.sprite = titleCorrectSprite;
            };
            LeaderboardHiddenPatch.LeaderboardHidden += () =>
            {
                title.SetText(titlePanelTitle);
                titlePanelTitle = null;

                headerBG.color = titlePanelC;
                headerBG.gradient = true;
                headerBG.sprite = titleDefaultSprite;
            };

            MiscUtils.Parse(ResourcePaths.BSML_TITLE_PANEL, headerPane.transform, this);

            mapStarContainer.background.material = ResourcePaths.BORDER_MATERIAL;
            mapModeContainer.background.material = ResourcePaths.BORDER_MATERIAL;
        }

        private void ChangeFilter(LeaderboardDisplayType type)
        {
            if (displayType == type || currentHash is null)
                return;
            page = 1;
            currentPage = 0;
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
                case LeaderboardDisplayType.Followed:
                    followedSelector.DefaultColor = Color.white;
                    previousPages.Clear();
                    break;
                case LeaderboardDisplayType.Rivals:
                    rivalsSelector.DefaultColor = new Color(0.8f, 0.8f, 0.8f);
                    previousPages.Clear();
                    break;
                case LeaderboardDisplayType.Relations:
                    relationsSelector.DefaultColor = Color.white;
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
                case LeaderboardDisplayType.Followed:
                    followedSelector.DefaultColor = followedSelector.HighlightColor;
                    break;
                case LeaderboardDisplayType.Rivals:
                    rivalsSelector.DefaultColor = rivalsSelector.HighlightColor;
                    break;
                case LeaderboardDisplayType.Relations:
                    relationsSelector.DefaultColor = relationsSelector.HighlightColor;
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
                await LoadLeaderboardAsync();
            });
        }
        private void ReloadLeaderboard() => Task.Run(LoadLeaderboardAsync);
        
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

        private bool TryUpdateCurrentMap()
        {
#if NEW_VERSION
            if (sldvc is not null && sldvc.beatmapLevel is not null && sldvc.beatmapKey != default)
                return UpdateDiff(sldvc.beatmapLevel, sldvc.beatmapKey);
#else
            if (sldvc is not null && sldvc.selectedDifficultyBeatmap is not null)
                return UpdateDiff(sldvc.selectedDifficultyBeatmap);
#endif
            return false;
        }

#if NEW_VERSION
        private bool UpdateDiff(BeatmapLevel beatmap, BeatmapKey key)
        {
#else
        private bool UpdateDiff(IDifficultyBeatmap beatmap)
        {
#endif      
            //Plugin.Log.Info("Update called.");
            if (!gameObject.activeSelf)
                return false;

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
                return false; // same map, no need to update
            currentDifficulty = beatmap.difficulty;
#endif
            currentHash = hash;

            page = 1; // reset to first page on map change
            currentPage = 0;
            currentPlayerPage = 0;

            // reload leaderboard for the new map
            Task.Run(ForceRefresh);
            return true;
        }
        private IEnumerator UpdateComplexity()
        {
            yield return new WaitForEndOfFrame();

            mapStarText.SetText($"<color={OVERALL}>{GetComplexity(difficultyInfo):N2} {MiscUtils.STAR}</color>");
            string categoryId = GetCategoryId(difficultyInfo);
            APCategory category = (APCategory)Enum.Parse(typeof(APCategory), HelpfulPaths.ReloadedCategoryToCategoryId(categoryId));
            mapTypeText.SetText($"<color={MiscUtils.GetColor(categoryId)}>{category}</color>");

            string color = MiscUtils.GetColorDim(categoryId);
            if (ColorUtility.TryParseHtmlString(color, out Color c))
                mapModeContainer.background.color = c;

            PanelViewController.Instance.SetCategoryTexts(category);
        }
        private async Task<bool> ForceRefresh() => await ForceRefresh(true);
        private async Task<bool> ForceRefresh(bool overridePlayerScore)
        {
            AsyncLock.Releaser? theLock = await forceRefreshLock.LockAsync();
            if (theLock is null || !gameObject.activeSelf) return false;
            using (theLock.Value)
            {
                difficultyInfo = await GetLeaderboard(currentHash, currentDifficulty);

                if (difficultyInfo is null)
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
                    return true;
                }

                StartCoroutine(UpdateComplexity());

                ShowLoading();

                currentPlayerPage = await GetPlayerPage(overridePlayerScore);
                AccsaberScoreData data = ConvertToScoreData(currentPlayerScoreInfo);
                currentPlayerScore = data is null ? null : new AccsaberScoreDataInfo(data);

                await LoadLeaderboardAsync();
            }
            return true;
        }
        private void ShowLoading()
        {
            if (leaderboardLoader.activeSelf)
                return;

            bool gotCachedData = UsesPreviousPages ? FilteredScoreDataCached(DifficultyId, page) : ScoreDataCached(DifficultyId, page);

            IEnumerator StartLoading()
            {
                yield return new WaitForEndOfFrame();

                badMapMessage.SetActive(false);
                leaderboardContainer.SetActive(false);
                leaderboardLoader.SetActive(true);
            }
            if (!gotCachedData)
                StartCoroutine(StartLoading());
        }

        private async Task LoadLeaderboardAsync()
        {
            if (page == currentPage) return; // already on this page, no need to reload
            AsyncLock.Releaser? theLock = await loadLeaderboardLock.LockAsync();
            if (theLock is null) return;
            using (theLock.Value)
            {
                try
                {
                    currentPage = page;

                    ShowLoading();

                    await PlayerSocialLife.LoadTask;

                    scoreDatas.Clear();

                    AccsaberScoreData[] scores;
                    (AccsaberScoreData[] scores, int truePage) scoreData;
                    IReadOnlyCollection<string> ids = PlayerSocialLife.GetIds(displayType);
                    switch (displayType)
                    {
                        case LeaderboardDisplayType.Global:
                            scores = await GetScoreData(page, DifficultyId);
                            nextPage = page + 1;
                            break;
                        case LeaderboardDisplayType.Friends:
                        case LeaderboardDisplayType.Followed:
                        case LeaderboardDisplayType.Rivals:
                        case LeaderboardDisplayType.Relations:
                            int neededScores = Math.Min(ids.Count - previousPages.Count * 10, 10);
                            scoreData = await GetScoreData(page, DifficultyId, token => ids.Contains(GetPlayerId(token)), neededScores);
                            scores = scoreData.scores;
                            nextPage = scoreData.truePage;
                            break;
                        case LeaderboardDisplayType.Country:
                            string country = GetCountry(currentPlayerScoreInfo);
                            scores = await GetScoreData(page, DifficultyId, country);
                            nextPage = page + 1;
                            break;
                        default:
                            scores = null;
                            break;
                    }
                    if (scores is not null)
                        scoreDatas.AddRange(scores);

                    IEnumerator ReloadData()
                    {
                        yield return new WaitForFixedUpdate();

                        leaderboard.PrefNumberOfCells = OnPlayerPage ? 10 : 12;
                        leaderboard.MainCellSize = CellSize;
                        leaderboard.Data = LeaderboardInfos;

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

        private async Task<int> GetPlayerPage(bool overrideLastScore)
        {
            await PlayerSocialLife.LoadTask;

            if (overrideLastScore || currentPlayerScoreInfo is null)
                currentPlayerScoreInfo = await GetScoreData(PlayerSocialLife.PlayerID, currentHash, currentDifficulty);
            if (currentPlayerScoreInfo is null) return -1; // Player has no score on this map
            return (int)Math.Ceiling(GetRank(currentPlayerScoreInfo) / (float)PAGE_LENGTH);
        }
        #endregion

        private class TextSpacer : ICellDataSource
        {
            public string TemplatePath => "<vertical anchor-pos-y='0.5'><text text='...' align='Center' font-size='3'/></vertical>";

            public float CellSize => 1.5f;

            public int TemplateId { get; set; }
        }
    }
}
