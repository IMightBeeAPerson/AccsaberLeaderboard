using AccsaberLeaderboard.API;
using AccsaberLeaderboard.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccsaberLeaderboard.Utils
{
#nullable enable
    internal static class DataUtils
    {
        private static List<LevelMilestone>? milestones;
        private static readonly Task milestoneListTask;

        static DataUtils()
        {
            milestones = null;
            milestoneListTask = Task.Run(async () =>
            {
                string dataStr = await APIHandler.CallAPI_String(HelpfulPaths.APAPI_MILESTONES, AccsaberAPI.throttler).ConfigureAwait(false);
                if (string.IsNullOrEmpty(dataStr)) return;
                List<LevelMilestone> outp = [.. JToken.Parse(dataStr).Children().Select(token =>
                {
                    string title = token["title"].ToString();
                    return new LevelMilestone((int)token["level"], ColorPalette.GetTitleColor(title), title);
                })];
                outp.Sort();
                milestones = outp;
            });
        }

        private static void WaitForMilestoneList()
        {
            milestoneListTask.GetAwaiter().GetResult();
            if (milestones is null)
                throw new Exception("There was an error with setting up the milestones list!!!");
        }

        public static LevelMilestone? GetNextMilestone(LevelMilestone milestone)
        {
            if (milestones is null)
                WaitForMilestoneList();
            int index = milestones!.IndexOf(milestone) + 1;
            if (index >= milestones.Count) 
                return null;
            return milestones[index];
        }
        public static string GetNextTitle(string title)
        {
            if (milestones is null)
                WaitForMilestoneList();
            LevelMilestone? milestone = milestones!.FirstOrDefault(stone => stone.LevelTitle.Equals(title));
            return GetNextMilestone(milestone)?.LevelTitle ?? ColorPalette.DEFAULT_COLOR;
        }
    }
}
