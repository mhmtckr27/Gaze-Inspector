using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class Hash128Drawer : PropertyDrawerBase, IContextMenuProvider
    {
        private Rect _lastDrawnRect;
        
        public override void Draw(GUIContent label)
        {
            PropertyTreeGUI.BeginHorizontalLayout(label, out var labelRect);
            _lastDrawnRect = EditorGUILayout.GetControlRect(label != null, EditorGUIUtility.singleLineHeight,
                EditorStyles.textField);
            EditorGUI.PropertyField(_lastDrawnRect, Property, GUIContent.none);
            PropertyTreeGUI.EndHorizontalLayout();
            if (Event.current.type == EventType.Repaint)
            {
                ContextClickArea = labelRect;
            }
        }
        
        public Rect ContextClickArea { get; protected set; }

        GenericMenu IContextMenuProvider.Provide(out Rect highlightArea)
        {
            var menu = new GenericMenu();
            PropertyClipboard.AddContextMenu(menu, Property);
            highlightArea = _lastDrawnRect;
            return menu;
        }
    }
}