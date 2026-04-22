using AccsaberLeaderboard.UI.Components;
using AccsaberLeaderboard.UI.ViewControllers;
using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.TypeHandlers;
using HMUI;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static AccsaberLeaderboard.Utils.ColorPalette;

namespace AccsaberLeaderboard.Models
{
    internal class AccsaberMilestoneData(float target, float progress, string tier, string title, string description, string id)
    {
#pragma warning disable IDE0044, IDE0051
        public float Target { get; private set; } = target;
        public float Progress { get; private set; } = progress;
        public string Tier { get; private set; } = tier;
        public string Title { get; private set; } = title;
        public string Description { get; private set; } = description;
        public string ID { get; private set; } = id;
        public class AccsaberMilestoneDataInfo
        {
            private readonly AccsaberMilestoneData data;

            private readonly bool flip;
            private readonly float progressPercent;
            private float DisplayableProgress => progressPercent * 100f;

            public AccsaberMilestoneDataInfo(AccsaberMilestoneData milestoneData)
            {
                data = milestoneData;
                flip = milestoneData.Progress > milestoneData.Target;
                progressPercent = (flip ? milestoneData.Target / milestoneData.Progress : milestoneData.Progress / milestoneData.Target);
            }

            [UIValue(nameof(Progress))] public string Progress => $"<color={LEVEL}>" + (DisplayableProgress >= 99.99f ? "99.99" : DisplayableProgress.ToString("N2")) + "%</color>";
            [UIValue(nameof(ExactProgress))]
            public string ExactProgress
            {
                get
                {
                    float prog = flip ? data.Target : data.Progress;
                    float targ = flip ? data.Progress : data.Target;

                    string middle;
                    if (data.Target >= 1000)
                        middle = $"{prog:N0} / {targ:N0}";
                    else if (data.Target < 1)
                        middle = $"{prog * 100f:0.####}% / {targ * 100f:0.####}%";
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

            [UIComponent(nameof(cellContainer))] private CustomBackground cellContainer;


            [UIValue("oneXonePic")] public const string oneXonePic = ResourcePaths.RESOURCE_1X1;
            [UIValue("bgPath")] public const string bgPath = ResourcePaths.RESOURCE_GRADIENT;

            [UIValue(nameof(listWidth))] public const float listWidth = PlayerMilestoneModalViewController.listWidth;
            [UIValue(nameof(cellSize))] public const float cellSize = PlayerMilestoneModalViewController.cellSize;
            [UIValue(nameof(FontSize))] public const float FontSize = 3f;
            [UIValue(nameof(barSpacer))] public const float barSpacer = 5f;
            [UIValue(nameof(progLen))] public const float progLen = 10f;
            [UIValue(nameof(exactProgLen))] public const float exactProgLen = 25f;
            [UIValue(nameof(barLen))] public const float barLen = listWidth - barSpacer - progLen - exactProgLen;

            [UIAction("#post-parse")]
            private void PostParse()
            {
                PercentBarTop.transform.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barLen * progressPercent);
                PercentBarBottom.transform.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barLen * (1 - progressPercent));

                string rankColor = MiscUtils.GetColorForMilestoneRank(data.Tier);

                ColorUtility.TryParseHtmlString(rankColor, out Color c);
                PercentBarTop_image.color = c;
                ColorUtility.TryParseHtmlString(TECH, out c);
                PercentBarBottom_image.color = c;


                const float brightnessThreshold = 0.6f;

                ColorUtility.TryParseHtmlString(MiscUtils.ChangeAlpha(rankColor, "7"), out c);
                //float average = (c.r + c.g + c.b) / 3f;
                float maxColor = c.maxColorComponent;
                if (maxColor > brightnessThreshold)
                {
                    float curve = maxColor - brightnessThreshold;
                    c.r -= curve;
                    c.g -= curve;
                    c.b -= curve;
                }
                cellContainer.background.color = c;
                
            }
        }
    }
}
