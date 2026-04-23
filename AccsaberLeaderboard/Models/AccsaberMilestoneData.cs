using AccsaberLeaderboard.UI.Components;
using AccsaberLeaderboard.UI.ViewControllers;
using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
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

            private readonly Color bgColor, rankColor;
            private float DisplayableProgress => progressPercent * 100f;
            private float Prog, Targ;

            public AccsaberMilestoneDataInfo(AccsaberMilestoneData milestoneData)
            {
                data = milestoneData;
                flip = milestoneData.Progress > milestoneData.Target;
                Prog = flip ? data.Target : data.Progress;
                Targ = flip ? data.Progress : data.Target;

                progressPercent = CalcProgress(milestoneData.Target, milestoneData.Progress, flip);

                ColorUtility.TryParseHtmlString(LevelMilestone.GetMilestoneRankColor(data.Tier), out rankColor);

                const float brightnessThreshold = 0.6f;

                Color c = rankColor.ColorWithAlpha(0.5f);
                float maxColor = c.maxColorComponent;
                if (maxColor > brightnessThreshold)
                {
                    float curve = maxColor - brightnessThreshold;
                    c.r -= curve;
                    c.g -= curve;
                    c.b -= curve;
                }
                bgColor = c;

            }

            [UIValue(nameof(Progress))] public string Progress => $"<color={LEVEL}>" + (DisplayableProgress >= 99.99f ? "99.99" : DisplayableProgress.ToString("N2")) + "%</color>";
            [UIValue(nameof(ExactProgress))]
            public string ExactProgress
            {
                get
                {
                    string middle;
                    if (data.Target >= 1000)
                        middle = $"{Prog:N0} / {Targ:N0}";
                    else if (data.Target < 1)
                        middle = $"{Prog * 100f:0.####}% / {Targ * 100f:0.####}%";
                    else
                        middle = $"{Prog:0.####} / {Targ:0.####}";

                    return $"<color={LEVEL_DIM}>(" + middle + ")</color>";
                }
            }
            [UIValue(nameof(Tier))] public string Tier => $"<color={LevelMilestone.GetMilestoneRankColor(data.Tier)}>{data.Tier}</color>";
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

                PercentBarTop_image.color = rankColor;
                ColorUtility.TryParseHtmlString(TECH, out Color c);
                PercentBarBottom_image.color = c;

                cellContainer.background.color = bgColor;
            }

            public static float CalcProgress(float target, float progress) =>
                CalcProgress(target, progress, progress > target);
            public static float CalcProgress(float target, float progress, bool swap)
            {
                const float shiftAmount = 0.97f; // Shift both values down to make it more meaningful to go from 97 to 98

                bool isPercent = target < 1f;
                bool needsShifting = target >= (shiftAmount + 0.01f);

                if (swap)
                    (progress, target) = (target, progress);
                if (isPercent)
                {
                    const float baseNum = 2f;
                    const float expMult = 3f;
                    const float expMultSquared = expMult * expMult;

                    float progPercent = needsShifting ? (progress - shiftAmount) / (target - shiftAmount) : progress / target;

                    float exp = expMultSquared * progPercent - expMultSquared;

                    return Mathf.Pow(baseNum, exp);
                }
                else return progress / target;
            }
        }
    }
}
