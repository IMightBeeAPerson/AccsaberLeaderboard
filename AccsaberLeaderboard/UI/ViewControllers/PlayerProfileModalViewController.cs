using AccsaberLeaderboard.Models;
using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using HMUI;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using static AccsaberLeaderboard.Utils.ColorPalette;
using static AccsaberLeaderboard.API.AccsaberAPI;
using BeatSaberMarkupLanguage.Components;
using AccsaberLeaderboard.UI.Components;
using System.Linq;

namespace AccsaberLeaderboard.UI.ViewControllers
{
    internal class PlayerProfileModalViewController
    {
#pragma warning disable IDE0044, IDE0051, IDE0052

        [UIParams] private BSMLParserParams parserParams;

        #region UI Components & Objects

        [UIValue("colorGrey")] public const string grey = GREY;
        [UIValue("colorFollowBG")] public const string followBGColor = RELATIONS_ACC;
        [UIValue("colorRivalBG")] public const string rivalBGColor = RELATIONS_TARGETED;
        [UIValue("colorDim")] public const string dim = DARK_BLUE;

        [UIValue("oneXonePic")] public const string oneXonePic = ResourcePaths.RESOURCE_1X1;
        [UIValue("profileBGPic")] public const string profileBGPic = ResourcePaths.RESOURCE_GRADIENT_CORNER;
        [UIValue("followPic")] public const string followPic = ResourcePaths.RESOURCE_FOLLOWED;
        [UIValue("rivalPic")] public const string rivalPic = ResourcePaths.RESOURCE_RIVALS;

        [UIValue("titleFontSize")] public const float titleFontSize = 7f;
        [UIValue("fontSizeBig")] public const float fontSizeBig = 4f;
        [UIValue("fontSizeMid")] public const float fontSizeMid = 3.5f;
        [UIValue("fontSizeSmall")] public const float fontSizeSmall = 3f;
        [UIValue("labelFontSize")] public const float labelFontSize = 2.5f;

        [UIValue("containerWidth")] public const float containerWidth = 70f;
        [UIValue("containerHeight")] public const float containerHeight = 80f;

        [UIValue("namePadding")] public const float namePadding = 10f;
        [UIValue("nameSize")] public const float nameSize = containerWidth - namePadding * 2f;

        [UIValue("levelTextPadding")] public const float levelTextPadding = 17f;
        [UIValue("rowPadding")] public const float rowPadding = 5f;
        [UIValue("barPadding")] public const float barPadding = 10f;

        [UIValue("barLen")] public const float barLen = containerWidth - barPadding * 2;
        [UIValue("barGirth")] public const float barGirth = 1f;

        public const float profilePicBorderSize = 3f;
        [UIValue("profilePicSize")] public const float profilePicSize = 20f;
        [UIValue("profileBGPicSize")] public const float profileBGPicSize = 20f + profilePicBorderSize;


        [UIObject("loader")] private GameObject modalLoader;
        [UIObject("container")] private GameObject modalContainer;

        [UIObject("PlayerInfoWindow")] private GameObject modal;
        [UIComponent("PlayerInfoWindow")] private ModalView modalView;

        [UIComponent("playerImage")] private ImageView modalPlayerImage;
        [UIComponent("playerImageBackground")] private ImageView modalPlayerImageBackground;
        [UIComponent("playerImageBorder")] private ImageView modalPlayerImageBorder;

        [UIComponent("playerName")] private TextMeshProUGUI modalPlayerName;

        [UIComponent("followerContainer")] private CustomBackground followerContainer;
        [UIComponent("rivalContainer")] private CustomBackground rivalContainer;

        [UIComponent("setAsFollowerButton")] private ClickableImage setAsFollowerButton;
        [UIComponent("setAsRivalButton")] private ClickableImage setAsRivalButton;

        [UIComponent("levelRank")] private TextMeshProUGUI modalLevelRank;
        [UIComponent("level")] private TextMeshProUGUI modalLevel;

        [UIComponent("levelProgress")] private LayoutElement modalLevelProgress;
        [UIComponent("levelProgress")] private ImageView modalLevelProgress_image;
        [UIComponent("levelProgressInverse")] private LayoutElement modalLevelProgressInverse;
        [UIComponent("levelProgressInverse")] private ImageView modalLevelProgressInverse_image;

        [UIComponent("levelXpProgress")] private TextMeshProUGUI modalLevelXpProgress;
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

        private bool isFollower, isRival;
        private string playerId;

        [UIAction("#post-parse")] private void PostParse()
        {
            modalPlayerImage.material = ResourcePaths.BORDER_MATERIAL;
            modalPlayerImageBackground.material = ResourcePaths.BORDER_MATERIAL;
            modalPlayerImageBorder.material = ResourcePaths.BORDER_MATERIAL;

            followerContainer.background.material = ResourcePaths.BORDER_MATERIAL;
            rivalContainer.background.material = ResourcePaths.BORDER_MATERIAL;

            modalPlayerName.enableVertexGradient = true;

            setAsFollowerButton.HighlightColor = RELOADED.Color();
            setAsRivalButton.HighlightColor = TARGETED.Color();
        }

        [UIAction("SetAsFollower")] private void SetAsFollower()
        {
            isFollower = !isFollower;
            Swap(setAsFollowerButton);
            if (isFollower)
                PlayerSocialLife.AddId(playerId, LeaderboardDisplayType.Followed);
            else
                PlayerSocialLife.RemoveId(playerId, LeaderboardDisplayType.Followed);
        }
        [UIAction("SetAsRival")] private void SetAsRival()
        {
            isRival = !isRival;
            Swap(setAsRivalButton);
            if (isRival)
                PlayerSocialLife.AddId(playerId, LeaderboardDisplayType.Rivals);
            else
                PlayerSocialLife.RemoveId(playerId, LeaderboardDisplayType.Rivals);
        }

        public PlayerProfileModalViewController(GameObject parent)
        {
            MiscUtils.Parse(ResourcePaths.BSML_PLAYER_PROFILE, parent.transform, this);
            modal.transform.SetParent(parent.transform);
        }

        private void Swap(ClickableImage img)
        {
            (img.HighlightColor, img.DefaultColor) = (img.DefaultColor, img.HighlightColor);
        }
        private void ResetButtons(bool follower = false, bool rival = false)
        {
            if (follower != isFollower)
            {
                isFollower = follower;
                Swap(setAsFollowerButton);
            }
            if (rival != isRival)
            {
                isRival = rival;
                Swap(setAsRivalButton);
            }
        }
        private IEnumerator ShowPlayerStart()
        {
            yield return new WaitForEndOfFrame();

            (modal.transform as RectTransform).sizeDelta = new Vector2(containerWidth, containerHeight);
            modalLoader.SetActive(true);
            modalContainer.SetActive(false);
        }
        private IEnumerator ShowPlayerTexts(PlayerInfoToken playerInfo)
        {
            StatsInfoToken stats = GetPlayerStats(playerInfo, APCategory.Overall);
            LevelInfoToken levelInfo = GetPlayerLevelData(playerInfo);
            string rank = GetTitle(levelInfo);

            yield return new WaitForEndOfFrame();

            string titleColor = GetTitleColor(rank);
            modalPlayerName.colorGradient = MiscUtils.ColorToGradient(titleColor);
            modalPlayerName.SetText(GetPlayerName(playerInfo));

            modalLevelRank.SetText($"<color={titleColor}>{rank}</color>");
            modalLevel.SetText($"<color={LEVEL}>Level {GetLevel(levelInfo)}</color>");

            modalLevelXpProgress.SetText($"<color={LEVEL_DIM}>({GetCurrentLevelXp(levelInfo):N0}xp / {GetNextLevelXp(levelInfo):N0}xp)</color>");

            float xpPercent = GetProgress(levelInfo);
            modalLevelProgressNumber.SetText($"<color={GREY}>{xpPercent:N2}%</color>");
            xpPercent /= 100f;

            

            modalLevelProgress.transform.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barLen * xpPercent);
            modalLevelProgressInverse.transform.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barLen * (1 - xpPercent));

            ColorUtility.TryParseHtmlString(GetTitleColor(DataUtils.GetNextTitle(rank)), out Color c);
            modalLevelProgress_image.color = c;
            ColorUtility.TryParseHtmlString(GetTitleColor(rank), out c);
            modalLevelProgressInverse_image.color = c;

            modalGlobalRank.SetText($"<color={GLOBAL}>#{GetGlobalRank(stats)}</color>");
            modalOverall.SetText($"<color={OVERALL}>{GetAP(stats):N2}ap</color>");
            modalCountryRank.SetText($"<color={COUNTRY}>#{GetCountryRank(stats)}</color>");

            stats = GetPlayerStats(playerInfo, APCategory.Tech);

            modalTech.SetText($"<color={TECH}>{GetAP(stats):N2}ap</color>");
            modalTechGlobalRank.SetText($"<color={GLOBAL_DIM}>#{GetGlobalRank(stats)}</color>");
            modalTechCountryRank.SetText($"<color={COUNTRY_DIM}>#{GetCountryRank(stats)}</color>");

            stats = GetPlayerStats(playerInfo, APCategory.True);
            modalTrue.SetText($"<color={TRUE}>{GetAP(stats):N2}ap</color>");
            modalTrueGlobalRank.SetText($"<color={GLOBAL_DIM}>#{GetGlobalRank(stats)}</color>");
            modalTrueCountryRank.SetText($"<color={COUNTRY_DIM}>#{GetCountryRank(stats)}</color>");

            stats = GetPlayerStats(playerInfo, APCategory.Standard);
            modalStandard.SetText($"<color={STANDARD}>{GetAP(stats):N2}ap</color>");
            modalStandardGlobalRank.SetText($"<color={GLOBAL_DIM}>#{GetGlobalRank(stats)}</color>");
            modalStandardCountryRank.SetText($"<color={COUNTRY_DIM}>#{GetCountryRank(stats)}</color>");

            modalPlayerImageBorder.color = c;

#if NEW_VERSION
            modalPlayerImage.SetImageAsync(GetPlayerAvatar(playerInfo));
#else
            modalPlayerImage.SetImage(GetPlayerAvatar(playerInfo));
#endif
            playerId = GetPlayerId(playerInfo);
            bool isMainCharacter = PlayerSocialLife.PlayerID.Equals(playerId);

            setAsFollowerButton.gameObject.SetActive(!isMainCharacter);
            setAsRivalButton.gameObject.SetActive(!isMainCharacter);
            if (!isMainCharacter)
                ResetButtons(PlayerSocialLife.PlayerFollowedIDs.Contains(playerId), PlayerSocialLife.PlayerRivalIDs.Contains(playerId));

            yield return new WaitForFixedUpdate();

            modalLoader.SetActive(false);
            modalContainer.SetActive(true);
        }
        public Task ShowPlayer(Task<PlayerInfoToken> playerInfoLoader, MonoBehaviour host, bool animated = true)
        {
            modalView.Show(animated, true);

            host.StartCoroutine(ShowPlayerStart());

            return Task.Run(async () =>
            {
                host.StartCoroutine(ShowPlayerTexts(await playerInfoLoader));
            });
        }
        public Task ShowPlayer(string playerId, MonoBehaviour host, bool animated = true)
        {
            return ShowPlayer(Task.Run(async () => await GetPlayerInfo(playerId, true)), host, animated);
        }
    }
}
