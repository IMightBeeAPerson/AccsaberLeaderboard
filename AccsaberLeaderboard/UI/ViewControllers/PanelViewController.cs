using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Models;
using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.TypeHandlers;
using BeatSaberMarkupLanguage.ViewControllers;
using BS_Utils.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using TMPro;

namespace AccsaberLeaderboard.UI.ViewControllers
{
    [ViewDefinition("AccsaberLeaderboard.UI.bsml.PanelView.bsml")]
    [HotReload(RelativePathToLayout = @"..\UI\bsml\PanelView.bsml")]
    internal class PanelViewController : BSMLAutomaticViewController
    {
#pragma warning disable IDE0044, IDE0051

        [UIComponent("panelContainer")] private Backgroundable panelContainer;
        [UIComponent("globalRankText")] private TextMeshProUGUI globalRankText;
        [UIComponent("countryRankText")] private TextMeshProUGUI countryRankText;
        [UIComponent("TotalAPText")] private TextMeshProUGUI TotalAPText;
        private void Awake()
        {
            Plugin.Log.Debug("PanelViewController Awake");
            BSEvents.levelCleared += SucceededMap;
            Task.Run(UpdatePlayer);
        }

        public void SetRanks(int globalRank, int countryRank, float totalAP)
        {
            globalRankText.text = $"<color=#AAA>Global Rank:</color> #{globalRank}";
            countryRankText.text = $"<color=#AAA>Country Rank:</color> #{countryRank}";
            TotalAPText.text = $"<color=#A0F>{totalAP:N2}ap</color>";
        }

        private void SucceededMap(StandardLevelScenesTransitionSetupDataSO transition, LevelCompletionResults results)
        {
            Task.Run(async () => { await Task.Delay(5000); await UpdatePlayer(); });
        }
        private async Task UpdatePlayer()
        {
            try
            {
                JToken playerInfo = await AccsaberAPI.GetPlayerInfo(Plugin.Instance.PlayerID, true);
                JToken overallPlayerStats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.Overall);
                SetRanks(AccsaberAPI.GetGlobalRank(overallPlayerStats), AccsaberAPI.GetCountryRank(overallPlayerStats), AccsaberAPI.GetAP(overallPlayerStats));

                BackgroundableHandler.TrySetBackgroundColor(panelContainer, MiscUtils.GetColorForTitle(AccsaberAPI.GetPlayerTitle(playerInfo)) + '6');
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error updating player info: {ex}");
            }
        }
    }
}
