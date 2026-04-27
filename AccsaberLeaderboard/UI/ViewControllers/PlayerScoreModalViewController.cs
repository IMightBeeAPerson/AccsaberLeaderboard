using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using HMUI;
using System.Collections;
using System.Linq;
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

        [UIValue("modalShowName")] public const string modalShowName = "ShowContainer";
        [UIValue("modalHideName")] public const string modalHideName = "HideContainer";

        [UIValue("containerWidth")] public const float containerWidth = 80f;
        [UIValue("containerHeight")] public const float containerHeight = 80f;

        [UIValue("valueWidth")] public const float valueWidth = containerWidth / 3f;

        [UIValue("playerImageSize")] public const float playerImageSize = 20f;

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
        private string lastUserId;
        private MonoBehaviour currentHost;

        #endregion

        [UIAction("#post-parse")] private void PostParse()
        {
            //Below line taken from: https://github.com/accsaber/accsaber-plugin/blob/dev/leaderboard-1.38/AccSaber/UI/ViewControllers/LeaderboardUserModalController.cs#L182
            playerImage.material = Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "UINoGlowRoundEdge");
        }
        [UIAction("ShowProfile")] private void ShowProfile()
        {
            modalView.Hide(false);
            ppmvc.ShowPlayer(lastUserId, currentHost, false);
        }


        public PlayerScoreModalViewController(GameObject parent)
        {
            MiscUtils.Parse(ResourcePaths.BSML_PLAYER_SCORE, parent.transform, this);
            modal.transform.SetParent(parent.transform);
            ppmvc = new(parent);
        }
        
        public Task ShowModal(ScoreInfoToken scoreInfo, DifficultyInfoToken diffInfo, MonoBehaviour host)
        {
            ShowModalStart(host);

            return Task.Run(() => host.StartCoroutine(ShowTexts(scoreInfo, diffInfo)));
        }

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
        private IEnumerator ShowTexts(ScoreInfoToken scoreInfo, DifficultyInfoToken diffInfo)
        {
            const char star = (char)9733;
            lastUserId = GetPlayerId(scoreInfo);

            yield return new WaitForEndOfFrame();

            playerNameText.SetText(GetPlayerName(scoreInfo));

            complexityText.SetText($"<color={OVERALL}>{GetComplexity(diffInfo)} {star}</color>");
            timeSetText.SetText(GetScoreTimeSet(scoreInfo).ToRelativeTime());
            string categoryId = GetCategoryId(diffInfo);
            accTypeText.SetText($"<color={MiscUtils.GetColor(categoryId)}>{HelpfulPaths.ReloadedCategoryToCategoryId(categoryId)}</color>");

            rankText.SetText($"<color={RANK}>#{GetRank(scoreInfo)}</color>");
            apText.SetText($"<color={AP}>{GetAP(scoreInfo):N2}ap</color>");
            accText.SetText($"<color={ACC}>{GetAcc(scoreInfo) * 100f:N4}%</color>");

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
