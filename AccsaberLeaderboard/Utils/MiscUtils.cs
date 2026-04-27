using BeatSaberMarkupLanguage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AccsaberLeaderboard.Utils
{
    public static class MiscUtils
    {
        public const char STAR = (char)9733;
        public static string ClampString(this string str, int maxLength, string suffix = "...")
        {
            if (str.Length < maxLength) return str;
            return $"{str.Substring(0, maxLength)}{suffix}";
        }
        public static string DimColor(string hex, int dimAmount)
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
        public static string InvertColor(string hex)
        {
            bool hasHashtag = hex[0] == '#';
            if (hasHashtag) hex = hex.Substring(1);
            int invertNumber = int.Parse(new string('F', hex.Length), System.Globalization.NumberStyles.HexNumber);
            return (hasHashtag ? "#" : "") + (invertNumber - int.Parse(hex, System.Globalization.NumberStyles.HexNumber)).ToString("X");
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
        public static BSMLParser GetParser() =>
#if NEW_VERSION
            BSMLParser.Instance;
#else
            BSMLParser.instance;
#endif
        public static void Parse(string resourcePath, Transform parent, object controller) =>
            GetParser().Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), resourcePath), parent.gameObject, controller);

        public static string ToRelativeTime(this DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime.ToUniversalTime();

            return timeSpan.TotalSeconds switch
            {
                < 2 => "1 second ago",
                < 60 => $"{timeSpan.TotalSeconds} seconds ago",
                < 120 => "1 minute ago",
                < 3600 => $"{(int)timeSpan.TotalMinutes} minutes ago",
                < 7200 => "1 hour ago",
                < 86400 => $"{(int)timeSpan.TotalHours} hours ago",
                < 172800 => "yesterday",
                < 2592000 => $"{(int)timeSpan.TotalDays} days ago",
                < 31536000 => $"{(int)(timeSpan.TotalDays / 30)} months ago",
                _ => $"{(int)(timeSpan.TotalDays / 365)} years ago"
            };
        }
        public static string GetColor(string categoryId) => categoryId.Last() switch
        {
            '1' => ColorPalette.TRUE,
            '2' => ColorPalette.STANDARD,
            '3' => ColorPalette.TECH,
            '5' => ColorPalette.OVERALL,
            _ => "#FFF"
        };

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
