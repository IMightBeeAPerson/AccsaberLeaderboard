using AccsaberLeaderboard.UI.BSML_Addons.Components;
using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage.Attributes;

using static AccsaberLeaderboard.UI.ViewControllers.LeaderboardViewController;
using static AccsaberLeaderboard.Utils.ColorPalette;

namespace AccsaberLeaderboard.Models
{
    internal class AccsaberScoreData(int score, string playerName, int rank, bool fullCombo, float ap, float acc, string playerId) : LeaderboardTableView.ScoreData(score, playerName, rank, fullCombo)
    {
        public float AP { get; private set; } = ap;
        public float Acc { get; private set; } = acc;
        public string PlayerId { get; private set; } = playerId;
        public class AccsaberScoreDataInfo(AccsaberScoreData scoreData) : ICellDataSource
        {
            public string TemplatePath => ResourcePaths.BSML_LEADERBOARD_CELL;

            public float CellSize => LeaderboardOnPlayerPage ? BIG_CELL_SIZE : SMALL_CELL_SIZE;

            public int TemplateId { get; set; }

            private readonly AccsaberScoreData scoreData = scoreData;

            public string PlayerId => scoreData.PlayerId;

            [UIValue(nameof(Score))] public string Score => $"<color={GREY}>{scoreData.score:N0}</color>";

            [UIValue(nameof(PlayerName))] public string PlayerName => scoreData.playerName.ClampString(LeaderboardOnPlayerPage ? 15 : 20);

            [UIValue(nameof(Rank))] public string Rank => $"<color={RANK}>#{scoreData.rank}</color>";

            [UIValue(nameof(FullCombo))] public bool FullCombo => scoreData.fullCombo;

            [UIValue(nameof(AP))] public string AP => $"<color={ColorPalette.AP}>{scoreData.AP:N2}ap</color>";

            [UIValue(nameof(Acc))] public string Acc => $"<color={ACC}>{scoreData.Acc * 100f:N4}%</color>";
            [UIValue(nameof(BGColor))] public string BGColor => PlayerId.Equals(Plugin.Instance.PlayerID) ? HIGHLIGHT : "#0009";

            [UIValue(nameof(FontSize))] public readonly float FontSize = LeaderboardOnPlayerPage ? BIG_FONT_SIZE : SMALL_FONT_SIZE;
            [UIValue(nameof(ContainerHeight))] public float ContainerHeight => (LeaderboardOnPlayerPage ? BIG_CELL_SIZE : SMALL_CELL_SIZE) - 0.1f;

            [UIValue(nameof(parentContainerWidth))] public const float parentContainerWidth = containerWidth;
            [UIValue(nameof(containerPadding))] public const float containerPadding = 1f;
            [UIValue(nameof(elementSpacing))] public const float elementSpacing = 0f;

            [UIValue(nameof(rankWidth))] public const float rankWidth = 10f;
            [UIValue(nameof(apWidth))] public const float apWidth = 14f + apPadding;
            [UIValue(nameof(apPadding))] public const float apPadding = 5f;
            [UIValue(nameof(accWidth))] public const float accWidth = 14f;
            [UIValue(nameof(scoreWidth))] public const float scoreWidth = 12f;
            [UIValue(nameof(nameWidth))] public const float nameWidth = containerWidth - rankWidth - apWidth - accWidth - scoreWidth - elementSpacing * 4f - containerPadding * 2f;
        }
    }
}
