using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class GradientDrawer : PropertyDrawerBase, IContextMenuProvider
    {
        private float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight;
        }
        
        public override void Draw(GUIContent label)
        {
            PropertyTreeGUI.BeginHorizontalLayout(label, out var labelRect);
            var rect = EditorGUILayout.GetControlRect(label != null, GetHeight(), EditorStyles.colorField);

            using (var changeScope = new EditorGUI.ChangeCheckScope())
            {
                TryGetAdditionalData(out ColorUsageData data);

                using (new EditorGui.MixedValueScope(Property.hasMultipleDifferentValues))
                {
                    using (new EditorGUI.PropertyScope(rect, label, Property))
                    {
                        var newVal = EditorGUI.GradientField(rect, GUIContent.none, Property.gradientValue,
                            data?.isHdr ?? false);

                        if (changeScope.changed)
                            Property.gradientValue = newVal;
                    }
                }
            }
            
            PropertyTreeGUI.EndHorizontalLayout();

            if (Event.current.type == EventType.Repaint)
                ContextClickArea = labelRect;
        }

        public Rect ContextClickArea { get; private set; }
        public GenericMenu Provide(out Rect highlightArea)
        {
            var menu = new GenericMenu();
            PropertyClipboard.AddContextMenu(menu, Property);
            highlightArea = ContextClickArea;
            return menu;
        }
    }
}