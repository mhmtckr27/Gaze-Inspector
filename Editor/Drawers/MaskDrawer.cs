using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class MaskDrawer : PropertyDrawerBase, IContextMenuProvider
    {
        private Rect _lastDrawnRect;
        
        private float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight;
        }
        
        public override void Draw(GUIContent label)
        {
            PropertyTreeGUI.BeginHorizontalLayout(label, out var labelRect);
            var rect = EditorGUILayout.GetControlRect(label != null, GetHeight(), EditorStyles.layerMaskField);
            using (new EditorGui.MixedValueScope(Property.hasMultipleDifferentValues))
            {
                using (new EditorGUI.PropertyScope(rect, label, Property))
                {
                    using (var changeScope = new EditorGUI.ChangeCheckScope())
                    {
                        var newVal = EditorGUI.MaskField(rect, GUIContent.none, Property.intValue, GetLayerNames());

                        if (changeScope.changed)
                            Property.boxedValue = newVal;
                    }
                }
            }

            PropertyTreeGUI.EndHorizontalLayout();

            if (Event.current.type == EventType.Repaint)
            {
                ContextClickArea = labelRect;
                _lastDrawnRect = GUILayoutUtility.GetLastRect();
            }
        }

        private string[] GetLayerNames()
        {
#if UNITY_6000_0_OR_NEWER
            if (Property.propertyType == SerializedPropertyType.RenderingLayerMask)
                return MaskExtensions.GetRenderingLayerNames();
#endif
            return MaskExtensions.GetLayerNames();
        }

        public Rect ContextClickArea { get; private set; }

        GenericMenu IContextMenuProvider.Provide(out Rect highlightArea)
        {
            var menu = new GenericMenu();
            PropertyClipboard.AddContextMenu(menu, Property);
            UnityPropertyMenu.AddUnityItems(menu, Property);
            highlightArea = _lastDrawnRect;
            return menu;
        }
    }
}