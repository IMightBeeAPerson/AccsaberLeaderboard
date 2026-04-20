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
    [ViewDefinition("AccsaberLeaderboard.UI.bsml.PanelView.bsml")]
    [HotReload(RelativePathToLayout = @"..\UI\bsml\PanelView.bsml")]
    internal class PanelViewController : BSMLAutomaticViewController
    {
#pragma warning disable IDE0044, IDE0051, IDE0052

        internal static event Action OnPlayerPictureClicked;

        [UIComponent("panelContainer")] private Backgroundable panelContainer;

        [UIValue("overallColor")] private string overallColor = OVERALL;
        [UIValue("techColor")] private string techColor = TECH;
        [UIValue("standardColor")] private string standardColor = STANDARD;
        [UIValue("trueColor")] private string trueColor = TRUE;

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

        [UIAction("OpenPlayerProfile")] private void OpenPlayerProfile() => OnPlayerPictureClicked?.Invoke();
        [UIAction("OpenReloaded")] private void OpenReloaded() => Application.OpenURL($"https://accsaberreloaded.com/players/{Plugin.Instance.PlayerID}");
        private void Awake()
        {
            Plugin.Log.Debug("PanelViewController Awake");
            BSEvents.levelCleared += SucceededMap;
            Task.Run(UpdatePlayer);
        }

        public void SetTexts(JToken playerInfo)
        {

            JToken playerStats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.Overall);

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
                JToken playerInfo = await AccsaberAPI.GetPlayerInfo(Plugin.Instance.PlayerID, true);
                IEnumerator WaitThenUpdate() 
                {
                    yield return new WaitForEndOfFrame();

                    SetTexts(playerInfo);
#if NEW_VERSION
                    panelContainer.ApplyColor(MiscUtils.ConvertHex(MiscUtils.ChangeAlpha(MiscUtils.GetColorForTitle(AccsaberAPI.GetPlayerTitle(playerInfo)), "6")));
#else
                    BackgroundableHandler.TrySetBackgroundColor(panelContainer, MiscUtils.ChangeAlpha(MiscUtils.GetColorForTitle(AccsaberAPI.GetPlayerTitle(playerInfo)), "6"));
#endif

                    //Below line taken from: https://github.com/accsaber/accsaber-plugin/blob/dev/leaderboard-1.38/AccSaber/UI/ViewControllers/LeaderboardUserModalController.cs#L182
                    profilePicture.material = Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "UINoGlowRoundEdge");
#if NEW_VERSION
                    profilePicture.SetImageAsync(AccsaberAPI.GetPlayerAvatar(playerInfo));
#else
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
