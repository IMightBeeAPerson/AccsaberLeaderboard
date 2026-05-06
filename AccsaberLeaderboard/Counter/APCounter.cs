using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Calculators;
using AccsaberLeaderboard.Configuration;
using AccsaberLeaderboard.Counter.Settings;
using AccsaberLeaderboard.Models;
using AccsaberLeaderboard.Utils;
using CountersPlus.Counters.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
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
        private Task loadTask;
        private float complexity;
        private string complexityToString, hash, diffId;
        private BeatmapDifficulty mapDiff;

        private AccsaberScoreData[] toCompareAgainst;

        public override void CounterDestroy()
        {
            
        }

        public override void CounterInit()
        {
            displayText = CanvasUtility.CreateTextFromSettings(Settings);
            displayText.fontSize = PluginConfig.Instance.FontSize;

            UI.ViewControllers.LeaderboardViewController lvc = UI.ViewControllers.LeaderboardViewController.Instance;

            int decimalPlaces = PluginConfig.Instance.DecimalPlaces;
            complexityToString = decimalPlaces > 0 ? "0." + new string('#', decimalPlaces) : "N0";

            hash = beatmap.level.levelID.Split('_')[2];

            if (lvc is not null && lvc.ValidMapSelected && lvc.CurrentHash.Equals(hash) && lvc.CurrentDiff.Equals(beatmap.difficulty))
            {
                complexity = lvc.CurrentComplexity;
                diffId = lvc.DifficultyId;
                displayText.SetText($"{complexity:0.##}* {lvc.CurrentCategory} Acc");
                loadTask = Task.CompletedTask;
            }
            else
            {
                complexity = 0f;
                loadTask = Task.Run(async () =>
                {
                    AccsaberAPI.DifficultyInfoToken token = await AccsaberAPI.GetLeaderboard(hash, beatmap.difficulty);
                    if (token is not null)
                    {
                        complexity = AccsaberAPI.GetComplexity(token);
                        diffId = AccsaberAPI.GetDifficultyId(token);
                    }
                });
            }

            switch (PluginConfig.Instance.CounterMode)
            {
                case Utils.CounterModes.Normal:
                    sc.scoreDidChangeEvent += HandleScoreDidChangeNormal;
                    break;
                case Utils.CounterModes.Targets:
                    sc.scoreDidChangeEvent += HandleScoreDidChangeTarget;
                    toCompareAgainst = null;
                    Task.Run(async () => 
                    {
                        await loadTask;
                        toCompareAgainst = await GetRivals();
                    });
                    break;
            }
        }

        private void HandleScoreDidChangeNormal(int multScore, int modScore)
        {
            if (Mathf.Approximately(complexity, 0f))
                return;

            float acc = (float)multScore / sc.immediateMaxPossibleMultipliedScore;

            float ap = APCalc.Instance.GetAp(acc, complexity);

            displayText.SetText($"{ap.ToString(complexityToString)} ap");
        }
        private void HandleScoreDidChangeTarget(int multScore, int modScore)
        {
            if (Mathf.Approximately(complexity, 0f) || toCompareAgainst is null)
                return;

            float acc = (float)multScore / sc.immediateMaxPossibleMultipliedScore;

            float ap = APCalc.Instance.GetAp(acc, complexity);

            string outp = "", playerLine = $"<color=#FA0>#{{0}}</color> You - {ap.ToString(complexityToString)} ap";
            int maxScores = Math.Min(4, toCompareAgainst.Length);
            bool playerAdded = false;

            for (int i = 0, r = 1; i < maxScores; i++, r++)
            {
                AccsaberScoreData current = toCompareAgainst[i];

                if (!playerAdded && current.Acc < acc)
                {
                    playerAdded = true;
                    outp += string.Format(playerLine + '\n', r++);
                }

                outp += $"<color=#FA0>#{r}</color> <color=#AAA>{current.playerName.ClampString(15)} - {current.AP.ToString(complexityToString)} ap</color>";
                if (i != maxScores - 1 || !playerAdded)
                    outp += '\n';
            }

            if (!playerAdded)
                outp += string.Format(playerLine, maxScores + 1);

            displayText.SetText(outp);
        }


        private async Task<AccsaberScoreData[]> GetRivals()
        {
            await PlayerSocialLife.LoadInfo();

            HashSet<string> rivals = (HashSet<string>)PlayerSocialLife.GetIds(LeaderboardDisplayType.Rivals);
            rivals.Remove(PlayerSocialLife.PlayerID);

            Func<AccsaberAPI.ScoreInfoToken, bool> filter =
                token => rivals.Contains(AccsaberAPI.GetPlayerId(token));

            return (await AccsaberAPI.GetScoreData(1, diffId, filter, rivals.Count)).scores;
        }
    }
}
