using BeatSaberMarkupLanguage.Attributes;

namespace AccsaberLeaderboard.Models
{
    internal class AccsaberScoreData(int score, string playerName, int rank, bool fullCombo, float ap, float acc) : LeaderboardTableView.ScoreData(score, playerName, rank, fullCombo)
    {
        public float AP { get; private set; } = ap;
        public float Acc { get; private set; } = acc;

        public class AccsaberScoreDataInfo(AccsaberScoreData scoreData)
        {
            [UIValue(nameof(Score))] public string Score => $"<color=#AAA>{scoreData.score:N0}</color>";
            [UIValue(nameof(PlayerName))] public string PlayerName => scoreData.playerName;
            [UIValue(nameof(Rank))] public string Rank => $"<color=#FA0>#{scoreData.rank}</color>";
            [UIValue(nameof(FullCombo))] public bool FullCombo => scoreData.fullCombo;
            [UIValue(nameof(AP))] public string AP => $"<color=#A0F>{scoreData.AP:N2}ap</color>";
            [UIValue(nameof(Acc))] public string Acc => $"<color=#0D0>{scoreData.Acc * 100f :N4}%</color>";
        }
    }
}
