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
        { //Newcomer Apprentice Adept Skilled Expert Master Grandmaster Legend Transendent Mythic Ascendant
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
                _ => "#FFFFFF"
            };
        }
    }
}
