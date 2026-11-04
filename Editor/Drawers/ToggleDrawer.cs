using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class ToggleDrawer : PropertyDrawerBase, IContextMenuProvider
    {
        private float GetHeight()
        {
            //no wrap.
            return EditorGUIUtility.singleLineHeight;
        }

        public override void Draw(GUIContent label)
        {
            PropertyTreeGUI.BeginHorizontalLayout(label, out var labelRect);
            ContextClickArea = labelRect;
            
            var rect = EditorGUILayout.GetControlRect(label != null, GetHeight(), EditorStyles.toggle);
            using (new EditorGui.MixedValueScope(Property.hasMultipleDifferentValues))
            {
                using (new EditorGUI.PropertyScope(rect, label, Property))
                {
                    using (var changeScope = new EditorGUI.ChangeCheckScope())
                    {
                        var newVal = EditorGUI.Toggle(rect, GUIContent.none, Property.boolValue);

                        if (changeScope.changed)
                            Property.boolValue = newVal;
                    }
                }
            }
            
            PropertyTreeGUI.EndHorizontalLayout();
        }

        public Rect ContextClickArea { get; private set; }
        public GenericMenu Provide(out Rect highlightArea)
        {
            var menu = new GenericMenu();
            PropertyClipboard.AddContextMenu(menu, Property);
            UnityPropertyMenu.AddUnityItems(menu, Property);
            highlightArea = ContextClickArea;
            return menu;
        }
    }
}