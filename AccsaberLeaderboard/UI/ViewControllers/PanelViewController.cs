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

namespace AccsaberLeaderboard.UI.ViewControllers
{
    [ViewDefinition("AccsaberLeaderboard.UI.bsml.PanelView.bsml")]
    [HotReload(RelativePathToLayout = @"..\UI\bsml\PanelView.bsml")]
    internal class PanelViewController : BSMLAutomaticViewController
    {
#pragma warning disable IDE0044, IDE0051

        internal static event Action OnPlayerPictureClicked;

        [UIComponent("panelContainer")] private Backgroundable panelContainer;
        [UIComponent("globalRankText")] private TextMeshProUGUI globalRankText;
        [UIComponent("countryRankText")] private TextMeshProUGUI countryRankText;
        [UIComponent("totalAPText")] private TextMeshProUGUI TotalAPText;
        [UIComponent("profilePicture")] private ImageView profilePicture;

        [UIAction("OpenPlayerProfile")] private void OpenPlayerProfile() => OnPlayerPictureClicked?.Invoke();
        [UIAction("OpenReloaded")] private void OpenReloaded() => Application.OpenURL($"https://accsaberreloaded.com/players/{Plugin.Instance.PlayerID}");
        private void Awake()
        {
            Plugin.Log.Debug("PanelViewController Awake");
            BSEvents.levelCleared += SucceededMap;
            Task.Run(UpdatePlayer);
        }

        public void SetRanks(int globalRank, int countryRank, float totalAP)
        {
            globalRankText.SetText($"<color=#AAA>Global Rank:</color> #{globalRank}");
            countryRankText.SetText($"<color=#AAA>Country Rank:</color> #{countryRank}");
            TotalAPText.SetText($"<color=#AAA>Total AP:</color> <color=#A0F>{totalAP:N2}ap</color>");
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
                JToken overallPlayerStats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.Overall);
                IEnumerator WaitThenUpdate() 
                {
                    yield return new WaitForEndOfFrame();

                    SetRanks(AccsaberAPI.GetGlobalRank(overallPlayerStats), AccsaberAPI.GetCountryRank(overallPlayerStats), AccsaberAPI.GetAP(overallPlayerStats));
#if NEW_VERSION
                    panelContainer.ApplyColor(MiscUtils.ConvertHex(MiscUtils.GetColorForTitle(AccsaberAPI.GetPlayerTitle(playerInfo)) + '6'));
#else
                    BackgroundableHandler.TrySetBackgroundColor(panelContainer, MiscUtils.GetColorForTitle(AccsaberAPI.GetPlayerTitle(playerInfo)) + '6');
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
