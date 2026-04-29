using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using HMUI;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static AccsaberLeaderboard.API.AccsaberAPI;
using static AccsaberLeaderboard.Utils.ColorPalette;

namespace AccsaberLeaderboard.UI.ViewControllers
{
    internal class PlayerScoreModalViewController
    {
#pragma warning disable IDE0044, IDE0051

        #region UI Values

        [UIValue("labelColor")] public const string labelColor = GREY;
        [UIValue("dimColor")] public const string dimColor = DARK_BLUE;

        [UIValue("modalShowName")] public const string modalShowName = "ShowContainer";
        [UIValue("modalHideName")] public const string modalHideName = "HideContainer";

        [UIValue("image1x1")] public const string image1x1 = ResourcePaths.RESOURCE_1X1;
        [UIValue("playerImageBorder")] public const string playerImageBorderPath = ResourcePaths.RESOURCE_GRADIENT_CORNER;

        [UIValue("containerWidth")] public const float containerWidth = 80f;
        [UIValue("containerHeight")] public const float containerHeight = 80f;

        [UIValue("valueWidth")] public const float valueWidth = containerWidth / 3f;

        [UIValue("playerImageSize")] public const float playerImageSize = 20f;
        public const float borderSize = 3f;
        [UIValue("playerImageBGSize")] public const float playerImageBGSize = borderSize + playerImageSize;

        [UIValue("playerNameFontSize")] public const float playerNameFontSize = 7f;
        [UIValue("valueFontSize")] public const float valueFontSize = 4f;
        [UIValue("labelFontSize")] public const float labelFontSize = 2.5f;

        #endregion
        #region UI Components & Objects

        [UIParams] private BSMLParserParams parserParams;

        [UIObject("modal")] private GameObject modal;
        [UIComponent("modal")] private ModalView modalView;

        [UIObject("container")] private GameObject container;
        [UIObject("loader")] private GameObject loader;

        [UIComponent("playerNameText")] private TextMeshProUGUI playerNameText;

        [UIComponent("playerImage")] private ImageView playerImage;
        [UIComponent("playerImageBackground")] private ImageView playerImageBackground;
        [UIComponent("playerImageBorder")] private ImageView playerImageBorder;

        [UIComponent("complexityText")] private TextMeshProUGUI complexityText;
        [UIComponent("timeSetText")] private TextMeshProUGUI timeSetText;
        [UIComponent("accTypeText")] private TextMeshProUGUI accTypeText;

        [UIComponent("rankText")] private TextMeshProUGUI rankText;
        [UIComponent("apText")] private TextMeshProUGUI apText;
        [UIComponent("accText")] private TextMeshProUGUI accText;

        [UIComponent("weightedText")] private TextMeshProUGUI weightedText;
        [UIComponent("xpText")] private TextMeshProUGUI xpText;
        [UIComponent("scoreText")] private TextMeshProUGUI scoreText;

        #endregion
        #region Normal Variables

        public readonly PlayerProfileModalViewController ppmvc;
        private PlayerInfoToken lastUser;
        private MonoBehaviour currentHost;

        #endregion

        [UIAction("#post-parse")] private void PostParse()
        {
            playerImage.material = ResourcePaths.BORDER_MATERIAL;
            playerImageBackground.material = ResourcePaths.BORDER_MATERIAL;
            playerImageBorder.material = ResourcePaths.BORDER_MATERIAL;
            playerNameText.enableVertexGradient = true;
        }
        [UIAction("ShowProfile")] private void ShowProfile()
        {
            modalView.Hide(false);
            ppmvc.ShowPlayer(GetPlayerId(lastUser), currentHost, false);
        }


        public PlayerScoreModalViewController(GameObject parent)
        {
            MiscUtils.Parse(ResourcePaths.BSML_PLAYER_SCORE, parent.transform, this);
            modal.transform.SetParent(parent.transform);
            ppmvc = new(parent);
        }
        
        public Task ShowModal(MonoBehaviour host, ScoreInfoToken scoreInfo, PlayerInfoToken playerInfo = null)
        {
            ShowModalStart(host);

            return playerInfo is null ? Task.Run(() => ShowTextsAsync(host, scoreInfo)) : Task.Run(() => ShowTextsAsync(host, scoreInfo, playerInfo));
        }
        private async Task ShowTextsAsync(MonoBehaviour host, ScoreInfoToken scoreInfo)
        {
            PlayerInfoToken playerInfo = await GetPlayerInfo(GetPlayerId(scoreInfo), true);
            await ShowTextsAsync(host, scoreInfo, playerInfo);
        }
        private async Task ShowTextsAsync(MonoBehaviour host, ScoreInfoToken scoreInfo, PlayerInfoToken playerInfo) =>
            await Task.Run(() => host.StartCoroutine(ShowTexts(scoreInfo, playerInfo)));
        private void ShowModalStart(MonoBehaviour host)
        {
            currentHost = host;
            parserParams.EmitEvent(modalShowName);
            host.StartCoroutine(ShowStart());
        }
        private IEnumerator ShowStart()
        {
            yield return new WaitForEndOfFrame();

            (modal.transform as RectTransform).sizeDelta = new Vector2(containerWidth, containerHeight);
            loader.SetActive(true);
            container.SetActive(false);
        }
        private IEnumerator ShowTexts(ScoreInfoToken scoreInfo, PlayerInfoToken playerInfo)
        {
            lastUser = playerInfo;
            LevelInfoToken levelInfo = GetPlayerLevelData(playerInfo);

            yield return new WaitForEndOfFrame();

            string titleColor = GetTitleColor(GetTitle(levelInfo));
            playerNameText.colorGradient = MiscUtils.ColorToGradient(titleColor);
            playerNameText.SetText(GetPlayerName(scoreInfo));

            if (ColorUtility.TryParseHtmlString(titleColor, out Color c))
                playerImageBorder.color = c;

            timeSetText.SetText(GetScoreTimeSet(scoreInfo).ToRelativeTime(2));

            apText.SetText($"<color={AP}>{GetAP(scoreInfo):N2}ap</color>");
            accText.SetText($"<color={ACC}>{GetAcc(scoreInfo) * 100f:N4}%</color>");
            rankText.SetText($"<color={RANK}>#{GetRank(scoreInfo)}</color>");

            weightedText.SetText($"<color={AP}>{GetWeightedAP(scoreInfo):N2}ap</color>");
            xpText.SetText($"<color={LEVEL}>{GetXpGained(scoreInfo):N2}xp</color>");
            scoreText.SetText($"<color={GREY}>{GetScore(scoreInfo):N0}</color>");

#if NEW_VERSION
            playerImage.SetImageAsync(GetPlayerAvatar(scoreInfo));
#else
            playerImage.SetImage(GetPlayerAvatar(scoreInfo));
#endif

            yield return new WaitForFixedUpdate();

            loader.SetActive(false);
            container.SetActive(true);
        }
    }
}
