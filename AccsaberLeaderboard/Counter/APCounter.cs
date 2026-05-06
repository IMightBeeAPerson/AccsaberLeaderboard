using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Calculators;
using CountersPlus.Counters.Custom;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Zenject;

namespace AccsaberLeaderboard.Counter
{
#pragma warning disable IDE0044, IDE0051
    internal class APCounter : BasicCustomCounter
    {
#if NEW_VERSION
        [Inject] private BeatmapLevel beatmap;
        [Inject] private BeatmapKey beatmapDiff;
#else
        [Inject] private IDifficultyBeatmap beatmap;
#endif
        [Inject] private ScoreController sc;

        private TMP_Text displayText;
        private float complexity;
        public override void CounterDestroy()
        {
            
        }

        public override void CounterInit()
        {
            sc.scoreDidChangeEvent += HandleScoreDidChange;

            displayText = CanvasUtility.CreateTextFromSettings(Settings);

            complexity = 0f;

            UI.ViewControllers.LeaderboardViewController lvc = UI.ViewControllers.LeaderboardViewController.Instance;

            string hash = beatmap.level.levelID.Split('_')[2];

            if (lvc is not null && lvc.ValidMapSelected && lvc.CurrentHash.Equals(hash) && lvc.CurrentDiff.Equals(beatmap.difficulty))
                complexity = lvc.CurrentComplexity;
            else
            {
                complexity = 0f;
                Task.Run(async () =>
                {
                    AccsaberAPI.DifficultyInfoToken token = await AccsaberAPI.GetLeaderboard(hash, beatmap.difficulty);
                    if (token is not null)
                        complexity = AccsaberAPI.GetComplexity(token);
                });
            }
        }

        private void HandleScoreDidChange(int multScore, int modScore)
        {
            if (Mathf.Approximately(complexity, 0f))
                return;

            float acc = (float)multScore / sc.immediateMaxPossibleMultipliedScore;
            Plugin.Log.Info($"acc = {acc}");

            float ap = APCalc.Instance.GetAp(acc, complexity);

            displayText.SetText($"{ap:0.##} ap");
        }
    }
}
