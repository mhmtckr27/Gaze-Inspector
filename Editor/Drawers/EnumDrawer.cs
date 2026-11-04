using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class EnumDrawer : PropertyDrawerBase, IContextMenuProvider
    {
        private EnumDrawerData _data;

        private EnumDrawerData EnumData
        {
            get
            {
                if (_data == null)
                    TryGetAdditionalData(out _data);
            
                return _data;
            }
        }
        
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
                        var newVal =
                            EnumData?.IsFlags ?? false
                                ? EditorGui.BetterEnumFlagsField(rect, GUIContent.none, Data.FieldType,
                                    Property.boxedValue)
                                : EditorGui.BetterEnumField(rect, GUIContent.none, Data.FieldType, Property.boxedValue);

                        if (changeScope.changed)
                            Property.boxedValue = newVal;
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

    internal class EnumDrawerData : ContextItem
    {
        internal bool IsFlags;

        public override void Reset()
        {
        }
    }
}