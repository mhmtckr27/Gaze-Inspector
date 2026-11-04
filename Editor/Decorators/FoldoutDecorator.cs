using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class FoldoutDecorator : AttributeDrawer<FoldoutAttribute>, IContextMenuProvider
    {
        private Rect _lastDrawnRect;
        
        public override void Draw(GUIContent label)
        {
            PropertyTreeGUI.BeginMeasuring(GUIStyle.none);
            if (PropertyTreeGUI.BeginFoldout(Property, label, out var rect))
            {
                EditorGuiUtility.PushIndentLevel(EditorGUI.indentLevel + 1);
                Inner.Draw(label);
                EditorGuiUtility.PopIndentLevel();
            }
            PropertyTreeGUI.EndFoldout();
            var measured= PropertyTreeGUI.EndMeasuring();

            if (Event.current.type == EventType.Repaint)
            {
                ContextClickArea = rect;
                _lastDrawnRect = measured;
            }
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