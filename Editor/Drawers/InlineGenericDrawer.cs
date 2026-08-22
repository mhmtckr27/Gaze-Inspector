using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal sealed class InlineGenericDrawer : PropertyDrawerBase
    {
        public override void Draw(GUIContent label)
        {
            PropertyTreeGUI.BeginVerticalLayout(GUIContent.none, out _);
            foreach (var child in Children)
            {
                PropertyTreeGUI.DrawNode(child, child.Data.Label);
            }
            PropertyTreeGUI.EndVerticalLayout();
        }
    }
}