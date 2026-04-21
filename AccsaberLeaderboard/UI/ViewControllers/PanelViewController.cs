using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Models;
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

        internal static event Action OnPlayerPictureClicked, OnLogoClicked;

        [UIComponent("panelContainer")] private Backgroundable panelContainer;

        [UIComponent("globalRankText")] private TextMeshProUGUI globalRankText;
        [UIComponent("countryRankText")] private TextMeshProUGUI countryRankText;
        [UIComponent("totalAPText")] private TextMeshProUGUI totalAPText;

        [UIComponent("techGlobalRankText")] private TextMeshProUGUI techGlobalRankText;
        [UIComponent("techCountryRankText")] private TextMeshProUGUI techCountryRankText;
        [UIComponent("techAPText")] private TextMeshProUGUI techAPText;

        [UIComponent("standardGlobalRankText")] private TextMeshProUGUI standardGlobalRankText;
        [UIComponent("standardCountryRankText")] private TextMeshProUGUI standardCountryRankText;
        [UIComponent("standardAPText")] private TextMeshProUGUI standardAPText;

        [UIComponent("trueGlobalRankText")] private TextMeshProUGUI trueGlobalRankText;
        [UIComponent("trueCountryRankText")] private TextMeshProUGUI trueCountryRankText;
        [UIComponent("trueAPText")] private TextMeshProUGUI trueAPText;

        [UIComponent("profilePicture")] private ImageView profilePicture;


        [UIValue("overallColor")] public const string overallColor = OVERALL;
        [UIValue("techColor")] public const string techColor = TECH;
        [UIValue("standardColor")] public const string standardColor = STANDARD;
        [UIValue("trueColor")] public const string trueColor = TRUE;

        [UIValue("dimmer")] public const string dimmer = DIMMER;

        [UIValue("containerWidth")] public const float containerWidth = 100f;
        [UIValue("containerHeight")] public const float containerHeight = 20f;

        [UIValue("containerPadding")] public const float containerPadding = 2.5f;
        [UIValue("elementPadding")] public const float elementPadding = 2.5f;

        [UIValue("logoPic")] public const string logoPic = ResourcePaths.RESOURCE_LOGO;

        [UIValue("fontSizeCell")] public const float fontSizeCell = 4f;

        [UIValue("cellSpacing")] public const float cellSpacing = 1f;

        [UIValue("rowWidth")] public const float rowWidth = 60f;
        [UIValue("cellWidth")] public const float cellWidth = rowWidth / 4f;

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
            BSEvents.levelCleared += SucceededMap;
            Task.Run(UpdatePlayer);
        }

        public void SetTexts(AccsaberAPI.PlayerInfoToken playerInfo)
        {

            AccsaberAPI.StatsInfoToken playerStats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.Overall);

            globalRankText.SetText($"<color={GLOBAL}>#{AccsaberAPI.GetGlobalRank(playerStats)}</color>");
            countryRankText.SetText($"<color={COUNTRY}>#{AccsaberAPI.GetCountryRank(playerStats)}</color>");
            totalAPText.SetText($"<color={AP}>{AccsaberAPI.GetAP(playerStats):N1}ap</color>");

            playerStats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.Tech);

            techGlobalRankText.SetText($"<color={GLOBAL}>#{AccsaberAPI.GetGlobalRank(playerStats)}</color>");
            techCountryRankText.SetText($"<color={COUNTRY}>#{AccsaberAPI.GetCountryRank(playerStats)}</color>");
            techAPText.SetText($"<color={AP}>{AccsaberAPI.GetAP(playerStats):N1}ap</color>");

            playerStats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.Standard);

            standardGlobalRankText.SetText($"<color={GLOBAL}>#{AccsaberAPI.GetGlobalRank(playerStats)}</color>");
            standardCountryRankText.SetText($"<color={COUNTRY}>#{AccsaberAPI.GetCountryRank(playerStats)}</color>");
            standardAPText.SetText($"<color={AP}>{AccsaberAPI.GetAP(playerStats):N1}ap</color>");

            playerStats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.True);

            trueGlobalRankText.SetText($"<color={GLOBAL}>#{AccsaberAPI.GetGlobalRank(playerStats)}</color>");
            trueCountryRankText.SetText($"<color={COUNTRY}>#{AccsaberAPI.GetCountryRank(playerStats)}</color>");
            trueAPText.SetText($"<color={AP}>{AccsaberAPI.GetAP(playerStats):N1}ap</color>");
        }

        private void SucceededMap(StandardLevelScenesTransitionSetupDataSO transition, LevelCompletionResults results)
        {
            Task.Run(async () => {
                await Task.Delay(5000);
                await UpdatePlayer(); 
                LeaderboardViewController.ForceUpdate();
            });
        }
        private async Task UpdatePlayer()
        {
            try
            {
                AccsaberAPI.PlayerInfoToken playerInfo = await AccsaberAPI.GetPlayerInfo(Plugin.Instance.PlayerID, true);
                AccsaberAPI.LevelInfoToken levelInfo = AccsaberAPI.GetPlayerLevelData(playerInfo);
                IEnumerator WaitThenUpdate() 
                {
                    yield return new WaitForEndOfFrame();

                    SetTexts(playerInfo);
#if NEW_VERSION
                    panelContainer.ApplyColor(MiscUtils.ConvertHex(MiscUtils.ChangeAlpha(MiscUtils.GetColorForTitle(AccsaberAPI.GetPlayerTitle(playerInfo)), "6")));
                    profilePicture.SetImageAsync(AccsaberAPI.GetPlayerAvatar(playerInfo));
#else
                    BackgroundableHandler.TrySetBackgroundColor(panelContainer, MiscUtils.ChangeAlpha(LevelMilestone.GetTitleColor(AccsaberAPI.GetTitle(levelInfo)), "6"));
                    profilePicture.SetImage(AccsaberAPI.GetPlayerAvatar(playerInfo));
#endif
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
