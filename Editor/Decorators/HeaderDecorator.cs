using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class HeaderDecorator : AttributeDrawer<HeaderAttribute>
    {
        private HeaderAttribute _headerAttribute;

        protected internal override void Initialize()
        {
            _headerAttribute = Attribute as HeaderAttribute;
        }

        public override void Draw(GUIContent label)
        {
            var rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight * 1.5f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
            rect.yMin += EditorGUIUtility.singleLineHeight * 0.5f;
            EditorGUI.LabelField(rect, _headerAttribute.header, EditorStyles.boldLabel);
            PropertyTreeGUI.DrawNode(Inner, Inner.Data.Label);
        }
    }
}