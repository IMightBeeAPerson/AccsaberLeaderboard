using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Models;
using AccsaberLeaderboard.UI.Components;
using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.TypeHandlers;
using BeatSaberMarkupLanguage.ViewControllers;
using BS_Utils.Utilities;
using HMUI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

using static AccsaberLeaderboard.Utils.ColorPalette;

namespace AccsaberLeaderboard.UI.ViewControllers
{
    [ViewDefinition(ResourcePaths.BSML_PANEL_VIEW)]
    [HotReload(RelativePathToLayout = @"..\UI\bsml\PanelView.bsml")]
    internal class PanelViewController : BSMLAutomaticViewController
    {
#pragma warning disable IDE0044, IDE0051, IDE0052
        public static PanelViewController Instance { get; private set; }

        internal static event Action OnPlayerPictureClicked, OnLogoClicked;

        private AccsaberAPI.PlayerInfoToken playerInfo = null;
        private APCategory toUpdate = APCategory.None;
        private object updateLock = new();

        [UIComponent("panelContainer")] private CustomBackground panelContainer;

        [UIComponent("globalRankText")] private TextMeshProUGUI globalRankText;
        [UIComponent("countryRankText")] private TextMeshProUGUI countryRankText;
        [UIComponent("totalAPText")] private TextMeshProUGUI totalAPText;

        [UIComponent("selectedLabelText")] private TextMeshProUGUI selectedLabelText;
        [UIComponent("selectedGlobalRankText")] private TextMeshProUGUI selectedGlobalRankText;
        [UIComponent("selectedCountryRankText")] private TextMeshProUGUI selectedCountryRankText;
        [UIComponent("selectedAPText")] private TextMeshProUGUI selectedAPText;

        [UIComponent("profilePicture")] private ImageView profilePicture;


        [UIValue("overallColor")] public const string overallColor = OVERALL;
        [UIValue("techColor")] public const string techColor = TECH;
        [UIValue("standardColor")] public const string standardColor = STANDARD;
        [UIValue("trueColor")] public const string trueColor = TRUE;

        [UIValue("dimmer")] public const string dimmer = DIMMER;

        [UIValue("containerWidth")] public const float containerWidth = 100f;
        [UIValue("containerHeight")] public const float containerHeight = 18f;

        [UIValue("containerPadding")] public const float containerPadding = 2.5f;
        [UIValue("elementPadding")] public const float elementPadding = 2.5f;

        [UIValue("containerBg")] public const string containerBg = ResourcePaths.RESOURCE_GRADIENT_PANEL;
        [UIValue("logoPic")] public const string logoPic = ResourcePaths.RESOURCE_LOGO;

        [UIValue("fontSizeCell")] public const float fontSizeCell = 5f;

        [UIValue("cellSpacing")] public const float cellSpacing = 0f;

        [UIValue("rowWidth")] public const float rowWidth = 60f;
        [UIValue("rowHeight")] public const float rowHeight = 14f;

        [UIValue("cellWidthBig")] public const float cellWidthBig = rowWidth / 2f;
        [UIValue("cellWidthSmall")] public const float cellWidthSmall = rowWidth / 4f;
        [UIValue("cellHeight")] public const float cellHeight = containerHeight / 3f - cellSpacing * 2f;

        [UIValue("imageSize")] public const float imageSize = (containerWidth - rowWidth - containerPadding * 2f - elementPadding * 2f) / 2f;


        [UIAction("OpenPlayerProfile")] private void OpenPlayerProfile() => OnPlayerPictureClicked?.Invoke();
        [UIAction("OpenReloaded")] private void OpenReloaded() => OnLogoClicked?.Invoke();

        [UIAction("#post-parse")] private void PostParse()
        {
            //Below lines taken from: https://github.com/accsaber/accsaber-plugin/blob/dev/leaderboard-1.38/AccSaber/UI/ViewControllers/LeaderboardUserModalController.cs#L182
            Material m = Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "UINoGlowRoundEdge");
            profilePicture.material = m;
            panelContainer.background.material = m;
        }

        private void Awake()
        {
            Plugin.Log.Debug("PanelViewController Awake");
            Instance = this;
            AccsaberLiveScores.OnPlayerScoreUpdated += _ => Task.Run(UpdatePlayer);
            Task.Run(UpdatePlayer);
        }

        public void SetCategoryTexts(APCategory category)
        {
            lock (updateLock)
            {
                toUpdate = category;
                if (playerInfo is not null)
                    UpdateCategoryTexts(category);
            }
        }
        public void UpdateCategoryTexts(APCategory category)
        {
            AccsaberAPI.StatsInfoToken playerStats = AccsaberAPI.GetPlayerStats(playerInfo, category);

            selectedLabelText.SetText($"<color={MiscUtils.GetColor(HelpfulPaths.CategoryIdToReloadedCategory(category.ToString()))}>{category}</color>");
            selectedGlobalRankText.SetText($"<color={GLOBAL}>#{AccsaberAPI.GetGlobalRank(playerStats)}</color>");
            selectedCountryRankText.SetText($"<color={COUNTRY}>#{AccsaberAPI.GetCountryRank(playerStats)}</color>");
            selectedAPText.SetText($"<color={AP}>{AccsaberAPI.GetAP(playerStats):N2}ap</color>");
        }
        private void SetOverallTexts()
        {
            AccsaberAPI.StatsInfoToken playerStats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.Overall);

            globalRankText.SetText($"<color={GLOBAL}>#{AccsaberAPI.GetGlobalRank(playerStats)}</color>");
            countryRankText.SetText($"<color={COUNTRY}>#{AccsaberAPI.GetCountryRank(playerStats)}</color>");
            totalAPText.SetText($"<color={AP}>{AccsaberAPI.GetAP(playerStats):N2}ap</color>");
        }

        private async Task UpdatePlayer()
        {
            try
            {
                playerInfo = await AccsaberAPI.GetPlayerInfo(Plugin.Instance.PlayerID, true);
                AccsaberAPI.LevelInfoToken levelInfo = AccsaberAPI.GetPlayerLevelData(playerInfo);
                IEnumerator WaitThenUpdate() 
                {
                    yield return new WaitForEndOfFrame();

                    SetOverallTexts();

                    if (ColorUtility.TryParseHtmlString(MiscUtils.ChangeAlpha(GetTitleColor(AccsaberAPI.GetTitle(levelInfo)), "6"), out Color c))
                        panelContainer.background.color = c;
#if NEW_VERSION
                    profilePicture.SetImageAsync(AccsaberAPI.GetPlayerAvatar(playerInfo));
#else
                    profilePicture.SetImage(AccsaberAPI.GetPlayerAvatar(playerInfo));
#endif
                    lock (updateLock)
                    {
                        if (toUpdate != APCategory.None)
                            UpdateCategoryTexts(toUpdate);
                    }
                }
                StartCoroutine(WaitThenUpdate());
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error updating player info: {ex}");
            }
        }
    }
}
