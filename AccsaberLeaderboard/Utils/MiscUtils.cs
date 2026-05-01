using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using TMPro;
using UnityEngine;

using static AccsaberLeaderboard.Utils.MiscConsts;

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
        public static int ConvertCharFromHex(char c) => c > '9' ? char.ToUpper(c) - 'A' + 10 : c - '0';
        public static string DimColor(this string hex, int dimAmount)
        {
            bool hasHashtag = hex[0] == '#';
            if (hasHashtag) hex = hex.Substring(1);
            int leadingZeros = 0;
            while (hex[leadingZeros] == '0')
                leadingZeros++;
            int dimNum = 0;
            for (int i = 0; i < hex.Length; i++)
            {
                dimNum <<= 4;
                int val = ConvertCharFromHex(hex[i]);
                dimNum += Math.Min(val, dimAmount);
            }
            int givenNum = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            string outp = new string('0', leadingZeros) + (givenNum - dimNum).ToString("X");
            if (outp.Length < hex.Length) outp = new string('0', hex.Length - outp.Length) + outp;
            return (hasHashtag ? "#" : "") + outp;
        }
        public static VertexGradient ColorToGradient(this string hex, int dimBase = 4)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c1);
            ColorUtility.TryParseHtmlString(DimColor(hex, dimBase), out Color c2);
            ColorUtility.TryParseHtmlString(DimColor(hex, dimBase * 2), out Color c3);
            return new(c1, c2, c2, c3);
        }
        public static string InvertColor(this string hex)
        {
            bool hasHashtag = hex[0] == '#';
            if (hasHashtag) hex = hex.Substring(1);
            int invertNumber = int.Parse(new string('F', hex.Length), System.Globalization.NumberStyles.HexNumber);
            return (hasHashtag ? "#" : "") + (invertNumber - int.Parse(hex, System.Globalization.NumberStyles.HexNumber)).ToString("X");
        }
        public static string ChangeAlpha(this string hex, string alpha)
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
        public static BSMLParserParams Parse(string resourcePath, Transform parent, object controller) =>
            GetParser().Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), resourcePath), parent.gameObject, controller);

        public static string ToRelativeTime(this DateTime dateTime, int layersDeep = 2)
        {
            TimeSpan timeSpan = DateTime.UtcNow - dateTime.ToUniversalTime();

            string outp = "";

            while (timeSpan.Ticks > 0 && layersDeep-- > 0)
            {
                var (timeDiff, str) = GetMostSignificantTime(timeSpan, dateTime);
                timeSpan -= timeDiff;
                dateTime = dateTime.AddSeconds(timeDiff.TotalSeconds);
                outp += (layersDeep == 0 || timeSpan.Ticks == 0 ? " and " : ", ") + str;
            }

            return outp.Substring(2) + " ago.";
        }
        public static (TimeSpan timeDiff, string str) GetMostSignificantTime(TimeSpan timeDiff, DateTime startTime)
        {
            double totalSeconds = timeDiff.TotalSeconds;
            string outp;
            if (timeDiff.Ticks < 10)
                outp = $"{timeDiff.Ticks * 100} nanoseconds";
            else
                outp = totalSeconds switch
                {
                    < SECONDS_MILLI => $"{(int)(timeDiff.Ticks / 10)} microseconds",
                    < SECONDS_MILLI * 2 => "1 millisecond",
                    < 1 => $"{(int)timeDiff.TotalMilliseconds} milliseconds",
                    < 2 => "1 second",
                    < SECONDS_MINUTE => $"{(int)totalSeconds} seconds",
                    < SECONDS_MINUTE * 2 => "1 minute",
                    < SECONDS_HOUR => $"{(int)timeDiff.TotalMinutes} minutes",
                    < SECONDS_HOUR * 2 => "1 hour",
                    < SECONDS_DAY => $"{(int)timeDiff.TotalHours} hours",
                    < SECONDS_DAY * 2 => "1 day",
                    < SECONDS_WEEK => $"{(int)timeDiff.TotalDays} days",
                    < SECONDS_WEEK * 2 => "1 week",
                    < SECONDS_WEEK * 4 => $"{(int)(timeDiff.TotalDays / 7)} weeks",
                    < SECONDS_YEAR => "", // Handle months below
                    < SECONDS_YEAR * 2 => "1 year",
                    _ => $"{(int)(timeDiff.TotalDays / DAYS_YEAR)} years"
                };

            if (outp.Length == 0)
            {
                int months = 0;
                int totalSecondsInMonths = 0, toAdd = SECONDS_DAY * DateTime.DaysInMonth(startTime.Year, startTime.Month);
                while (totalSecondsInMonths + toAdd < totalSeconds)
                {
                    months++;
                    startTime = startTime.AddMonths(1);
                    totalSecondsInMonths += toAdd;
                    toAdd = SECONDS_DAY * DateTime.DaysInMonth(startTime.Year, startTime.Month);
                }
                outp = months == 0 ? $"{(int)(timeDiff.TotalDays / 7)} weeks" : $"{months} month{(months == 1 ? "" : "s")}";
                return (months == 0 ? TimeSpan.FromDays((int)(timeDiff.TotalDays / 7) * 7) : TimeSpan.FromSeconds(totalSecondsInMonths), outp);
            }

            TimeSpan timeSpent = totalSeconds switch
            {
                < SECONDS_MICRO => timeDiff,
                < SECONDS_MILLI => TimeSpan.FromTicks((int)(timeDiff.Ticks / 10) * 10),
                < 1 => TimeSpan.FromMilliseconds((int)timeDiff.TotalMilliseconds),
                < SECONDS_MINUTE => TimeSpan.FromSeconds((int)totalSeconds),
                < SECONDS_HOUR => TimeSpan.FromMinutes((int)timeDiff.TotalMinutes),
                < SECONDS_DAY => TimeSpan.FromHours((int)timeDiff.TotalHours),
                < SECONDS_WEEK => TimeSpan.FromDays((int)timeDiff.TotalDays),
                < SECONDS_YEAR => TimeSpan.FromDays((int)(timeDiff.TotalDays / 7) * 7),
                _ => TimeSpan.FromSeconds((int)(timeDiff.TotalDays / DAYS_YEAR) * SECONDS_YEAR)
            };

            return (timeSpent, outp);
        }
        public static string GetColor(string categoryId) => categoryId.Last() switch
        {
            '1' => ColorPalette.TRUE,
            '2' => ColorPalette.STANDARD,
            '3' => ColorPalette.TECH,
            '5' => ColorPalette.OVERALL,
            _ => "#FFF"
        };
        public static string GetColorDim(string categoryId) => categoryId.Last() switch
        {
            '1' => ColorPalette.TRUE_DIM,
            '2' => ColorPalette.STANDARD_DIM,
            '3' => ColorPalette.TECH_DIM,
            '5' => ColorPalette.OVERALL_DIM,
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
