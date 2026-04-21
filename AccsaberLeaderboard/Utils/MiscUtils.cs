using AccsaberLeaderboard.Models;
using BeatSaberMarkupLanguage;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public static string GetColorForMilestoneRank(string rank) => rank switch
        {
            "bronze" => "#cd7f32",
            "silver" => "#c0c0c0",
            "gold" => "#ffd700",
            "platinum" => "#36cfb0",
            "diamond" => "#b9f2ff",
            "apex" => "#a855f7",
            _ => "#FFF"
        };
        public static Color ConvertHex(string hex)
        {
            if (hex[0] == '#') hex = hex.Substring(1);
            if (hex.Length <= 2) return ConvertHexShort(hex);
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
        /*
         public readonly struct Color(float r, float g, float b, float a) {
            public readonly float r = r, g = g, b = b, a = a;
            public override string ToString() => $"[{r},{g},{b},{a}]";
         }
         */
        private static Color ConvertHexShort(string hex)
        {
            int color, alpha;
            if (hex.Length > 1) alpha = int.Parse(new string(hex[1], 2), System.Globalization.NumberStyles.HexNumber);
            else alpha = 255;
            color = int.Parse(new string(hex[0], 2), System.Globalization.NumberStyles.HexNumber);
            return new Color(color / 255f, color / 255f, color / 255f, alpha / 255f);
        }
        public static string DimHex(string hex, int dimAmount)
        {
            bool hasHashtag = hex[0] == '#';
            if (hasHashtag) hex = hex.Substring(1);
            int leadingZeros = 0;
            while (hex[leadingZeros] == '0')
                leadingZeros++;
            string baseNumStr = "";
            foreach (char c in hex) baseNumStr += c == '0' ? '0' : '1';
            int baseNum = int.Parse(baseNumStr, System.Globalization.NumberStyles.HexNumber);
            int givenNum = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return (hasHashtag ? "#" : "") + new string('0', leadingZeros) + (givenNum - (baseNum * dimAmount)).ToString("X");
        }
        public static string ChangeAlpha(string hex, string alpha)
        {
            bool hasHashtag = hex[0] == '#';
            if (hasHashtag) hex = hex.Substring(1);

            int hexNum = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            if (hex.Length % 3 == 0)
                hexNum <<= 4 * (hex.Length / 3);
            hexNum += int.Parse(alpha.Length == 1 ? alpha + alpha : alpha, System.Globalization.NumberStyles.HexNumber);

            return (hasHashtag ? "#" : "") + hexNum.ToString("X");
        }
        public static void Parse(string resourcePath, Transform parent, object controller)
        {
#if NEW_VERSION
            BSMLParser.Instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), resourcePath), parent.gameObject, controller);
#else
            BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), resourcePath), parent.gameObject, controller);
#endif
        }
        #region Debug Functions

        public static string Print<T>(this IEnumerable<T> arr)
        {
            if (arr.Count() == 0) return "[]";
            string outp = "";
            foreach (T item in arr)
                outp += ", " + item;
            return $"[{outp.Substring(2)}]";
        }
        public static string Print(this IEnumerable<string> arr)
        {
            if (arr.Count() == 0) return "[]";
            string outp = "";
            foreach (string item in arr)
                outp += ", \"" + item + '"';
            return $"[{outp.Substring(2)}]";
        }

        #endregion
    }
}
