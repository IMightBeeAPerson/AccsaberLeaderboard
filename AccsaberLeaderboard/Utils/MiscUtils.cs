using System.Linq;
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
        public static string GetColorForTitle(string title)
        { //Newcomer, Apprentice, Adept, Skilled, Expert, Master, Grandmaster, Legend, Transcendent, Mythic, Ascendant
            return title switch
            { // Made up colors for now, later get them through the API if possible
                "Newcomer" => "#999",
                "Apprentice" => "#05F",
                "Adept" => "#2F4",
                "Skilled" => "#A70",
                "Expert" => "#EEE",
                "Master" => "#FF0",
                "Grandmaster" => "#80E",
                "Legend" => "#FA0",
                "Transcendent" => "#0FF",
                "Mythic" => "#F00",
                "Ascendant" => "#F38",
                _ => "#FFF"
            };
        }
        public static Color ConvertHex(string hex)
        {
            if (hex[0] == '#') hex = hex.Substring(1);
            int repeatNum = hex.Length >= 6 ? 1 : 2;
            int[] vals = hex.Length >= 6 ? new int[hex.Length / 2] : new int[hex.Length];
            for (int i = 0; i < hex.Length; i++)
                vals[i] = int.Parse(new string(hex[i], repeatNum), System.Globalization.NumberStyles.HexNumber);
            return vals.Length == 3 ? new Color(vals[0] / 255f, vals[1] / 255f, vals[2] / 255f) : new Color(vals[0] / 255f, vals[1] / 255f, vals[2] / 255f, vals[3] / 255f);
        }
        public static string DimHex(string hex, int dimAmount)
        {
            bool hasHashtag = hex[0] == '#';
            if (hasHashtag) hex = hex.Substring(1);
            int baseNum = int.Parse(new string('1', hex.Length), System.Globalization.NumberStyles.HexNumber);
            return (hasHashtag ? "#" : "") + (int.Parse(hex, System.Globalization.NumberStyles.HexNumber) - (baseNum * dimAmount)).ToString("X");
        }
    }
}
