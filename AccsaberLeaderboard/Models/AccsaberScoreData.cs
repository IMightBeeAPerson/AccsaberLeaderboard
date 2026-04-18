using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage.Attributes;

namespace AccsaberLeaderboard.Models
{
    internal class AccsaberScoreData(int score, string playerName, int rank, bool fullCombo, float ap, float acc, string playerId) : LeaderboardTableView.ScoreData(score, playerName, rank, fullCombo)
    {
        public float AP { get; private set; } = ap;
        public float Acc { get; private set; } = acc;
        public string PlayerId { get; private set; } = playerId;
        public class AccsaberScoreDataInfo(AccsaberScoreData scoreData)
        {
            private readonly AccsaberScoreData scoreData = scoreData;

            public string PlayerId => scoreData.PlayerId;

            [UIValue(nameof(Score))] public string Score => $"<color=#AAA>{scoreData.score:N0}</color>";

            [UIValue(nameof(PlayerName))] public string PlayerName => scoreData.playerName.ClampString(20);

            [UIValue(nameof(Rank))] public string Rank => $"<color=#FA0>#{scoreData.rank}</color>";

            [UIValue(nameof(FullCombo))] public bool FullCombo => scoreData.fullCombo;

            [UIValue(nameof(AP))] public string AP => $"<color=#A0F>{scoreData.AP:N2}ap</color>";

            [UIValue(nameof(Acc))] public string Acc => $"<color=#0D0>{scoreData.Acc * 100f:N4}%</color>";

            [UIValue(nameof(FontSize))] public readonly float FontSize = 3f;
        }
    }
}
