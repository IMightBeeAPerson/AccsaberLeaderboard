using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Models;
using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using HMUI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using static AccsaberLeaderboard.Utils.ColorPalette;
using static RankModel;

namespace AccsaberLeaderboard.UI.ViewControllers
{
    internal class PlayerProfileModalViewController
    {
#pragma warning disable IDE0044, IDE0052

        [UIParams] private BSMLParserParams parserParams;

        #region UI Components & Objects

        [UIValue("colorGrey")] private string grey = GREY;


        [UIObject("loader")] private GameObject modalLoader;
        [UIObject("container")] private GameObject modalContainer;

        [UIObject("PlayerInfoWindow")] private GameObject modal;

        [UIComponent("playerImage")] private ImageView modalPlayerImage;

        [UIComponent("playerName")] private TextMeshProUGUI modalPlayerName;

        [UIComponent("levelRank")] private TextMeshProUGUI modalLevelRank;
        [UIComponent("level")] private TextMeshProUGUI modalLevel;

        [UIComponent("levelProgress")] private LayoutElement modalLevelProgress;
        [UIComponent("levelProgress")] private ImageView modalLevelProgress_image;
        [UIComponent("levelProgressInverse")] private LayoutElement modalLevelProgressInverse;
        [UIComponent("levelProgressInverse")] private ImageView modalLevelProgressInverse_image;
        [UIComponent("levelProgressNumber")] private TextMeshProUGUI modalLevelProgressNumber;

        [UIComponent("globalRank")] private TextMeshProUGUI modalGlobalRank;
        [UIComponent("countryRank")] private TextMeshProUGUI modalCountryRank;
        [UIComponent("overall")] private TextMeshProUGUI modalOverall;

        [UIComponent("tech")] private TextMeshProUGUI modalTech;
        [UIComponent("true")] private TextMeshProUGUI modalTrue;
        [UIComponent("standard")] private TextMeshProUGUI modalStandard;

        [UIComponent("tech_rank_global")] private TextMeshProUGUI modalTechGlobalRank;
        [UIComponent("tech_rank_country")] private TextMeshProUGUI modalTechCountryRank;
        [UIComponent("true_rank_global")] private TextMeshProUGUI modalTrueGlobalRank;
        [UIComponent("true_rank_country")] private TextMeshProUGUI modalTrueCountryRank;
        [UIComponent("standard_rank_global")] private TextMeshProUGUI modalStandardGlobalRank;
        [UIComponent("standard_rank_country")] private TextMeshProUGUI modalStandardCountryRank;
        #endregion

        public PlayerProfileModalViewController(GameObject parent)
        {
            MiscUtils.Parse("AccsaberLeaderboard.UI.bsml.PlayerProfile.bsml", parent.transform, this);
            modal.transform.SetParent(parent.transform);
        }
        private IEnumerator ShowPlayerStart()
        {
            yield return new WaitForEndOfFrame();

            (modal.transform as RectTransform).sizeDelta = new Vector2(70, 80);
            modalLoader.SetActive(true);
            modalContainer.SetActive(false);
        }
        private IEnumerator ShowPlayerTexts(JToken playerInfo)
        {
            string rank = AccsaberAPI.GetPlayerTitle(playerInfo);
            JToken stats = AccsaberAPI.GetPlayerStats(playerInfo, APCategory.Overall);

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

            modalLevelProgress_image.color = MiscUtils.ConvertHex(MiscUtils.GetColorForTitle((LevelTitle)Enum.Parse(typeof(LevelTitle), rank) + 1));
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
        public Task ShowPlayer(Task<JToken> playerInfoLoader, MonoBehaviour host)
        {
            parserParams.EmitEvent("ShowPlayerInfo");

            host.StartCoroutine(ShowPlayerStart());

            return Task.Run(async () =>
            {
                await playerInfoLoader;

                host.StartCoroutine(ShowPlayerTexts(playerInfoLoader.Result));
            });
        }
        public Task ShowPlayer(string playerId, MonoBehaviour host)
        {
            return ShowPlayer(Task.Run(async () => await AccsaberAPI.GetPlayerInfo(playerId, true)), host);
        }
    }
}
