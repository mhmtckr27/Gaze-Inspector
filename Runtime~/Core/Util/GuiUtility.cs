using UnityEngine;

namespace GrayWolf.Gaze
{
    internal static partial class GuiUtility
    {
        private static readonly GUIContent s_Text = new();
        private static readonly GUIContent s_Image = new();
        private static readonly GUIContent s_TextImage = new();
        
        internal static GUIContent TempContent(string t)
        {
            s_Text.image = null;
            s_Text.text = t;
            s_Text.tooltip = null;
            return s_Text;
        }
        
        internal static GUIContent TempContent(Texture i)
        {
            s_Image.image = i;
            s_Image.text = null;
            s_Image.tooltip = null;
            return s_Image;
        }

        internal static readonly GUIContent DummyLabel = new GUIContent(" ");
        
        internal static GUIContent TempContent(string t, Texture i)
        {
            s_TextImage.image = i;
            s_TextImage.text = t;
            s_TextImage.tooltip = null;
            return s_TextImage;
        }
    }
}