using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class ColorDrawer : PropertyDrawerBase, IContextMenuProvider
    {
        private float GetHeight()
        {
            //no wrap.
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
                        var newVal = EditorGUI.ColorField(rect, GUIContent.none, Property.colorValue, true,
                            data?.showAlpha ?? true, data?.isHdr ?? false);

                        if (changeScope.changed)
                            Property.colorValue = newVal;
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
            UnityPropertyMenu.AddUnityItems(menu, Property);
            highlightArea = ContextClickArea;
            return menu;
        }
    }
}