using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Models;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.TypeHandlers;
using BeatSaberMarkupLanguage.ViewControllers;
using Newtonsoft.Json.Linq;
using System.Collections;
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

        [UIComponent("panelContainer")] private Backgroundable panelContainer;
        [UIComponent("globalRankText")] private TextMeshProUGUI globalRankText;
        [UIComponent("countryRankText")] private TextMeshProUGUI countryRankText;
        [UIComponent("TotalAPText")] private TextMeshProUGUI TotalAPText;
        private void Awake()
        {
            Plugin.Log.Debug("PanelViewController Awake");
            Task.Run(UpdatePlayer);
        }

        public void SetRanks(int globalRank, int countryRank, float totalAP)
        {
            globalRankText.text = $"<color=#AAA>Global Rank:</color> #{globalRank}";
            countryRankText.text = $"<color=#AAA>Country Rank:</color> #{countryRank}";
            TotalAPText.text = $"<color=#A0F>{totalAP:N2}ap</color>";
        }

        private async Task UpdatePlayer()
        {
            try
            {
                JToken playerInfo = await AccsaberAPI.GetPlayerInfo(Plugin.Instance.PlayerID, true);
                JToken overallPlayerStats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.Overall);
                SetRanks(AccsaberAPI.GetGlobalRank(overallPlayerStats), AccsaberAPI.GetCountryRank(overallPlayerStats), AccsaberAPI.GetPP(overallPlayerStats));

                BackgroundableHandler.TrySetBackgroundColor(panelContainer, GetColorForTitle(AccsaberAPI.GetPlayerTitle(playerInfo)) + '6');
            }
            catch (System.Exception ex)
            {
                Plugin.Log.Error($"Error updating player info: {ex}");
            }
        }
        private static string GetColorForTitle(string title)
        { //Newcomer Apprentice Adept Skilled Expert Master Grandmaster Legend Transendent Mythic Ascendant
            return title switch
            { // Made up colors for now, later get them through the API if possible
                "Newcomer" => "#999",
                "Apprentice" => "#05F",
                "Adept" => "#2F4",
                "Skilled" => "#A70",
                "Expert" => "#EEE",
                "Master" => "#FF0",
                "Grandmaster" => "#80E",
                "Legend" => "#FA0",
                "Transcendent" => "#0FF",
                "Mythic" => "#F00",
                "Ascendant" => "#F38",
                _ => "#FFFFFF"
            };
        }
    }
}
