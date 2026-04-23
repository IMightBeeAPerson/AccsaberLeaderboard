using System;

namespace AccsaberLeaderboard.Models
{
    internal record LevelMilestone(int LevelThreshold, string LevelColor, string LevelTitle) : IComparable<LevelMilestone>, IComparable<int>, IEquatable<LevelMilestone>
    {
        public const string DEFAULT_COLOR = "#000F";
        public int CompareTo(LevelMilestone other) => LevelThreshold - other.LevelThreshold;
        public int CompareTo(int level) => LevelThreshold - level;

        public static string GetTitleColor(string title) => title switch
        {
            "Newcomer" => "#6b7280",
            "Apprentice" => "#3b82f6",
            "Adept" => "#10b981",
            "Skilled" => "#cd7f32",
            "Expert" => "#c0c0d0",
            "Master" => "#fbbf24",
            "Grandmaster" => "#8b5cf6",
            "Legend" => "#f97316",
            "Transcendent" => "#22d3ee",
            "Mythic" => "#ef4444",
            "Ascendant" => "#f472b6",
            _ => DEFAULT_COLOR
        };
        public static string GetMilestoneRankColor(string rank) => rank switch
        {
            "bronze" => "#cd7f32",
            "silver" => "#c0c0c0",
            "gold" => "#ffd700",
            "platinum" => "#36cfb0",
            "diamond" => "#b9f2ff",
            "apex" => "#a855f7",
            _ => "#FFF"
        };
    }
}

namespace System.Runtime.CompilerServices { public class IsExternalInit { } }
