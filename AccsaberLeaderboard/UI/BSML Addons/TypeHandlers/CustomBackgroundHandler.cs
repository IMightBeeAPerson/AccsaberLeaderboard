using AccsaberLeaderboard.UI.Components;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.TypeHandlers;
using System.Collections.Generic;
using UnityEngine;

namespace AccsaberLeaderboard.UI.BSML_Addons.TypeHandlers
{
    [ComponentHandler(typeof(CustomBackground))]
    public class CustomBackgroundHandler : TypeHandler
    {
        public override Dictionary<string, string[]> Props => new()
        {
            { "bg", ["bg", "source", "src"] },
            { "bgColor", ["bg-color", "color"] }
        };

        public override void HandleType(BSMLParser.ComponentTypeWithData componentType, BSMLParserParams parserParams)
        {
            CustomBackground bg = componentType.component as CustomBackground;
            Color c = default;

            if (componentType.data.TryGetValue("bgColor", out string color))
                ColorUtility.TryParseHtmlString(color, out c);
            if (componentType.data.TryGetValue("bg", out string src))
                bg.Apply(src, c);
        }
    }
}
