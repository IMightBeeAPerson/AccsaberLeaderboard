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
        private string complexityToString, diffToString, hash, diffId;
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
            complexityToString = decimalPlaces > 0 ? "0." + new string('0', decimalPlaces) : "0";
            diffToString = $"▲{complexityToString};▼{complexityToString};0";

#if NEW_VERSION
            hash = beatmap.levelID.Split('_')[2];
            BeatmapDifficulty diff = beatmapDiff.difficulty;
#else
            hash = beatmap.level.levelID.Split('_')[2];
            BeatmapDifficulty diff = beatmap.difficulty;
#endif

            if (lvc is not null && lvc.ValidMapSelected && lvc.CurrentHash.Equals(hash) && lvc.CurrentDiff.Equals(diff))
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
                    AccsaberAPI.DifficultyInfoToken token = await AccsaberAPI.GetLeaderboard(hash, diff);
                    if (token is not null)
                    {
                        complexity = AccsaberAPI.GetComplexity(token);
                        diffId = AccsaberAPI.GetDifficultyId(token);
                    }
                });
            }

            LeaderboardDisplayType displayType = PluginConfig.Instance.CounterMode switch
            {
                Utils.CounterModes.Targets => LeaderboardDisplayType.Rivals,
                Utils.CounterModes.Friends => LeaderboardDisplayType.Friends,
                Utils.CounterModes.Followed => LeaderboardDisplayType.Followed,
                Utils.CounterModes.Relations => LeaderboardDisplayType.Relations,
                _ => default
            };

            switch (PluginConfig.Instance.CounterMode)
            {
                case Utils.CounterModes.Normal:
                    sc.scoreDidChangeEvent += HandleScoreDidChangeNormal;
                    break;
                case Utils.CounterModes.Targets:
                case Utils.CounterModes.Friends:
                case Utils.CounterModes.Followed:
                case Utils.CounterModes.Relations:
                    sc.scoreDidChangeEvent += HandleScoreDidChangeRelations;

                    toCompareAgainst = null;
                    Task.Run(async () => 
                    {
                        await loadTask;
                        toCompareAgainst = await GetRelations(displayType);
                        int players = Math.Min(4, toCompareAgainst.Length) + 1;
                        displayText.fontSize *= (1f / players) + (players > 1 ? 0.25f : 0f);
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
        private void HandleScoreDidChangeRelations(int multScore, int modScore)
        {
            if (Mathf.Approximately(complexity, 0f) || toCompareAgainst is null)
                return;

            float acc = (float)multScore / sc.immediateMaxPossibleMultipliedScore;

            float ap = APCalc.Instance.GetAp(acc, complexity);

            string outp = "", playerLine = $"<color=#FA0>#{{0}}</color> <color=#0A0>You - <color={ColorPalette.AP}>{ap.ToString(complexityToString)}</color> ap</color>";
            int maxScores = Math.Min(4, toCompareAgainst.Length);
            bool playerAdded = false;
            string ppDiffColor = ColorPalette.TECH;

            for (int i = 0, r = 1; i < maxScores; i++, r++)
            {
                AccsaberScoreData current = toCompareAgainst[i];

                if (!playerAdded && current.Acc < acc)
                {
                    playerAdded = true;
                    outp += string.Format(playerLine + '\n', r++);
                    ppDiffColor = ColorPalette.TRUE;
                }

                float diff = ap - current.AP;

                outp += $"<color=#A60>#{r}</color> <color=#888>{current.playerName.ClampString(15)} - <color={ColorPalette.AP}>{current.AP.ToString(complexityToString)}</color> ap (<color={ppDiffColor}>{diff.ToString(diffToString)}</color>)</color>";

                if (i != maxScores - 1 || !playerAdded)
                    outp += '\n';
            }

            if (!playerAdded)
                outp += string.Format(playerLine, maxScores + 1);

            displayText.SetText(outp);
        }


        private async Task<AccsaberScoreData[]> GetRelations(LeaderboardDisplayType ldt)
        {
            await PlayerSocialLife.LoadInfo();

            HashSet<string> relations = PlayerSocialLife.GetIds_Internal(ldt);
            relations.Remove(PlayerSocialLife.PlayerID);

            Func<AccsaberAPI.ScoreInfoToken, bool> filter =
                token => relations.Contains(AccsaberAPI.GetPlayerId(token));

            return (await AccsaberAPI.GetScoreData(0, diffId, filter, relations.Count)).scores;
        }
    }
}
