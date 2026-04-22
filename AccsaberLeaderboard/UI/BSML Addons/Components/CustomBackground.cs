using AccsaberLeaderboard.Utils;
using BeatSaberMarkupLanguage;
using HMUI;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace AccsaberLeaderboard.UI.Components
{
    public class CustomBackground : MonoBehaviour
    {
        public Image background = null;

        public void Apply(string src, Color tint = default)
        {
            if (tint == default)
                tint = Color.white;

            if (background is not null) 
                Destroy(background);

            Image img = gameObject.AddComponent<ImageView>();
            img.material = Utilities.ImageResources.NoGlowMat;
            img.rectTransform.SetParent(transform, false);
            img.sprite = Utilities.LoadSpriteRaw(Utilities.GetResource(Assembly.GetExecutingAssembly(), src));
            img.type = Image.Type.Simple;
            img.color = tint;


            gameObject.AddComponent<LayoutElement>();

            background = img;
        }
    }
}
