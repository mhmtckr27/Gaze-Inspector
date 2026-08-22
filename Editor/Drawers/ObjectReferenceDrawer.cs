using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class ObjectReferenceDrawer : PropertyDrawerBase, IContextMenuProvider
    {
        private float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight;
        }
        
        public override void Draw(GUIContent label)
        {
            PropertyTreeGUI.BeginHorizontalLayout(label, out var labelRect);
            
            var rect = EditorGUILayout.GetControlRect(label != null, GetHeight());
            EditorGUI.ObjectField(rect, Property, Data.FieldType, GUIContent.none);
            
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