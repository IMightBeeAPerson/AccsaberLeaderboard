namespace AccsaberLeaderboard.Utils
{
    public static class MiscUtils
    {
        public static string ClampString(this string str, int maxLength, string suffix = "...")
        {
            if (str.Length < maxLength) return str;
            return $"{str.Substring(0, maxLength)}{suffix}";
        }
    }
}
