using BeatSaberMarkupLanguage.Components;
using UnityEngine;

namespace AccsaberLeaderboard.UI.BSML_Addons.Components
{
    public class ClickableImageUnclipped : ClickableImage, ICanvasRaycastFilter
    {
        public new bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            // Only check THIS RectTransform, ignore parents
            return RectTransformUtility.RectangleContainsScreenPoint(
                rectTransform, screenPoint, eventCamera);
        }
    }
}
