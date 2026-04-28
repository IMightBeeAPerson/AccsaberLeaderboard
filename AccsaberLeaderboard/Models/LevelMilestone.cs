using System;

namespace AccsaberLeaderboard.Models
{
    internal record LevelMilestone(int LevelThreshold, string LevelColor, string LevelTitle) : IComparable<LevelMilestone>, IComparable<int>, IEquatable<LevelMilestone>
    {
        public int CompareTo(LevelMilestone other) => LevelThreshold - other.LevelThreshold;
        public int CompareTo(int level) => LevelThreshold - level;

        
    }
}

namespace System.Runtime.CompilerServices { public class IsExternalInit { } }
