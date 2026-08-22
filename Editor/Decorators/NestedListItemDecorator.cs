using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class NestedListItemDecorator : AttributeDrawer<NestedListItemAttribute>
    {
        public override void Draw(GUIContent label)
        {
            EditorGuiUtility.PushHierarchyMode(false);
            GUILayout.BeginVertical();
            Inner.Draw(label);
            GUILayout.EndVertical();
            EditorGuiUtility.PopHierarchyMode();
        }
    }
}