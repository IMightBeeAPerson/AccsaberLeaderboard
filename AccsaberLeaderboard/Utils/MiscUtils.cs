using AccsaberLeaderboard.Models;
using BeatSaberMarkupLanguage;
using System.Collections.Generic;
using System.IO;
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
