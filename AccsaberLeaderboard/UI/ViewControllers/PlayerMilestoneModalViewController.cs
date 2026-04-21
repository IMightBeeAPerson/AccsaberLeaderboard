using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using HMUI;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static AccsaberLeaderboard.Utils.ColorPalette;

namespace AccsaberLeaderboard.UI.ViewControllers
{
    internal class PlayerMilestoneModalViewController
    {
#pragma warning disable IDE0044, IDE0051, IDE0052

        [UIParams] private BSMLParserParams parserParams;

        [UIObject("modal")] private GameObject modal;

        [UIObject("loader")] private GameObject loader;
        [UIObject("container")] private GameObject container;

        [UIComponent("list")] private CustomCellListTableData list;
        [UIValue("listValues")] private List<object> listValues = [];


        public PlayerMilestoneModalViewController(GameObject parent)
        {
            MiscUtils.Parse("AccsaberLeaderboard.UI.bsml.MilestoneModal.bsml", parent.transform, this);
            modal.transform.SetParent(parent.transform);
        }
        

        public Task ShowMilestoneModal(Task<List<JToken>> milestoneInfoLoader, MonoBehaviour host)
        {
            parserParams.EmitEvent("ShowModalInfo");

            host.StartCoroutine(ShowModalStart());

            return Task.Run(async () =>
            {
                await milestoneInfoLoader;

                host.StartCoroutine(ShowModal(milestoneInfoLoader.Result));
            });
        }
        public Task ShowMilestoneModal(string playerId, MonoBehaviour host)
        {
            return ShowMilestoneModal(Task.Run(() => ShowModalAsync(playerId)), host);
        }

        private Task<List<JToken>> ShowModalAsync(string userId)
        {
            return AccsaberAPI.GetMilestoneData(userId, token => !(bool)token["completed"], (a, b) =>
            {
                float diff = ((float)b["normalizedProgress"]) - ((float)a["normalizedProgress"]);
                return diff < 0 ? -1 : Mathf.Approximately(0f, diff) ? 0 : 1;
            });
        }

        private IEnumerator ShowModalStart()
        {
            yield return new WaitForEndOfFrame();

            (modal.transform as RectTransform).sizeDelta = new Vector2(100, 80);
            loader.SetActive(true);
            container.SetActive(false);
        }
        private IEnumerator ShowModal(List<JToken> sortedMilestones)
        {
            List<MilestoneInfo> sortedMilestoneInfos = [.. sortedMilestones.Select(token => new MilestoneInfo(
                    new MilestoneData(
                        (float)token["targetValue"],
                        (float)token["progress"],
                        token["tier"].ToString(),
                        token["title"].ToString(),
                        token["description"].ToString(),
                        token["milestoneId"].ToString()
                        )))];

            listValues = [.. sortedMilestoneInfos.Cast<object>()];

            list.data = listValues;

            yield return new WaitUntil(() => list.data.Count > 0);
            yield return new WaitForSeconds(0.1f);

#if NEW_VERSION
            list.TableView.ReloadData();
#else
            list.tableView.ReloadData();
#endif

            loader.SetActive(false);
            container.SetActive(true);

        }
        private class MilestoneData(float target, float progress, string tier, string title, string description, string id)
        {
            public float Target { get; private set; } = target;
            public float Progress { get; private set; } = progress;
            public string Tier { get; private set; } = tier;
            public string Title { get; private set; } = title;
            public string Description { get; private set; } = description;
            public string ID { get; private set; } = id;
        }
        private class MilestoneInfo(MilestoneData data)
        {
            private readonly MilestoneData data = data;

            private readonly bool flip = data.Progress > data.Target;
            private readonly float progressPercent = data.Progress > data.Target ? data.Target / data.Progress : data.Progress / data.Target;

            [UIValue(nameof(Progress))] public string Progress => $"<color={LEVEL}>{progressPercent * 100f :N2}%</color>";
            [UIValue(nameof(ExactProgress))] public string ExactProgress
            {
                get
                {
                    float prog = flip ? data.Target : data.Progress;
                    float targ = flip ? data.Progress : data.Target;

                    string middle;
                    if (data.Target >= 1000)
                        middle = $"{prog:N0} / {targ:N0}";
                    else if (data.Target < 1)
                        middle = $"{prog * 100f :0.####}% / {targ * 100f :0.####}%";
                    else
                        middle = $"{prog:0.####} / {targ:0.####}";

                    return $"<color={LEVEL_DIM}>(" + middle + ")</color>";
                }
            }
            [UIValue(nameof(Tier))] public string Tier => $"<color={MiscUtils.GetColorForMilestoneRank(data.Tier)}>{data.Tier}</color>";
            [UIValue(nameof(Title))] public string Title => $"{data.Title}";
            [UIValue(nameof(Description))] public string Description => $"<color={GREY}>{data.Description}</color>";


            [UIComponent(nameof(PercentBarTop))] private LayoutElement PercentBarTop;
            [UIComponent(nameof(PercentBarTop))] private ImageView PercentBarTop_image;
            [UIComponent(nameof(PercentBarBottom))] private LayoutElement PercentBarBottom;
            [UIComponent(nameof(PercentBarBottom))] private ImageView PercentBarBottom_image;


            [UIValue(nameof(FontSize))] public const float FontSize = 3f;
            [UIValue(nameof(dimGrey))] private const string dimGrey = DIM_GREY;
            [UIValue(nameof(barLen))] private const float barLen = 45f;
            [UIValue(nameof(progLen))] private const float progLen = 75 - barLen;

            [UIAction("#post-parse")] private void PostParse()
            {
                PercentBarTop.transform.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barLen * progressPercent);
                PercentBarBottom.transform.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barLen * (1 - progressPercent));

                PercentBarTop_image.color = MiscUtils.ConvertHex(MiscUtils.GetColorForMilestoneRank(data.Tier));
                PercentBarBottom_image.color = MiscUtils.ConvertHex(TECH);
            }
        }
    }
}
