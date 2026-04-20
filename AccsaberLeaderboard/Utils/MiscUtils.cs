using AccsaberLeaderboard.Models;
using UnityEngine;

namespace AccsaberLeaderboard.Utils
{
    public static class MiscUtils
    {
        public static string ClampString(this string str, int maxLength, string suffix = "...")
        {
            if (str.Length < maxLength) return str;
            return $"{str.Substring(0, maxLength)}{suffix}";
        }
        public static string GetColorForTitle(LevelTitle title) => GetColorForTitle(title.ToString());
        public static string GetColorForTitle(string title)
        { //Newcomer, Apprentice, Adept, Skilled, Expert, Master, Grandmaster, Legend, Transcendent, Mythic, Ascendant
            return title switch
            { // Made up colors for now, later get them through the API if possible
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
                _ => "#FFF"
            };
            /*
              --tier-newcomer: #6b7280;
              --tier-apprentice: #3b82f6;
              --tier-adept: #10b981;
              --tier-skilled: #cd7f32;
              --tier-expert: #c0c0d0;
              --tier-master: #fbbf24;
              --tier-grandmaster: #8b5cf6;
              --tier-legend: #f97316;
              --tier-transcendent: #22d3ee;
              --tier-mythic: #ef4444;
              --tier-ascendant: #f472b6;
             */
        }
        public static Color ConvertHex(string hex)
        {
            if (hex[0] == '#') hex = hex.Substring(1);
            bool longHex = hex.Length >= 6;
            int repeatNum = longHex ? 1 : 2;
            int[] vals = longHex ? new int[hex.Length / 2] : new int[hex.Length];
            for (int i = 0, valsDiv = longHex ? 2 : 1; i < hex.Length; i++)
            {
                int val = i % valsDiv == 0 ? 0 : vals[i / valsDiv];
                if (val > 0) 
                    val <<= 4;
                val += int.Parse(new string(hex[i], repeatNum), System.Globalization.NumberStyles.HexNumber);
                vals[i / valsDiv] = val;
            }
            return new Color(vals[0] / 255f, vals[1] / 255f, vals[2] / 255f, vals.Length >= 4 ? vals[3] / 255f : 1f);
        }
        public static string DimHex(string hex, int dimAmount)
        {
            bool hasHashtag = hex[0] == '#';
            if (hasHashtag) hex = hex.Substring(1);
            int baseNum = int.Parse(new string('1', hex.Length), System.Globalization.NumberStyles.HexNumber);
            return (hasHashtag ? "#" : "") + (int.Parse(hex, System.Globalization.NumberStyles.HexNumber) - (baseNum * dimAmount)).ToString("X");
        }
        public static string ChangeAlpha(string hex, string alpha)
        {
            bool hasHashtag = hex[0] == '#';
            if (hasHashtag) hex = hex.Substring(1);

            int hexNum = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            if (hex.Length % 3 == 0)
                hexNum <<= 8;
            hexNum += int.Parse(alpha.Length == 1 ? alpha + alpha : alpha, System.Globalization.NumberStyles.HexNumber);

            return (hasHashtag ? "#" : "") + hexNum.ToString("X");
        }
    }
}
