using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class TextAreaDecorator : AttributeDrawer<TextAreaAttribute>
    {
        private TextAreaAttribute _attribute;

        protected internal override void Initialize()
        {
            _attribute = Attribute as TextAreaAttribute;
        }

        /// <summary>
        /// Reports a <c>[TextArea]</c> on a field it cannot apply to. Nothing breaks when that happens, the
        /// field just draws normally, so this is a warning rather than an error: without it the attribute
        /// silently does nothing and the author has no way to tell.
        /// </summary>
        protected internal override void CollectMessages(List<GazeMessage> messages)
        {
            if (Property is { propertyType: not SerializedPropertyType.String })
                messages.Add(new GazeMessage(
                    InfoMessageType.Warning,
                    $"[TextArea] only applies to string fields. '{Property.propertyPath}' is " +
                    $"{Property.propertyType}, so the attribute is ignored."));

            base.CollectMessages(messages);
        }

        public override void Draw(GUIContent label)
        {
            if (Property.propertyType != SerializedPropertyType.String)
            {
                Inner.Draw(label);
                return;
            }

            var rect = EditorGUILayout.GetControlRect(label != null, GetVisibleHeight(), EditorStyles.textArea);
            EditorGUI.PropertyField(rect, Property, label);
        }

        private float GetVisibleHeight()
        {
            return 21f + (Mathf.Clamp(GetLineCount(), _attribute.minLines + 1, _attribute.maxLines + 1) - 1) * 15;
        }

        private int GetLineCount()
        {
            return Mathf.CeilToInt(EditorStyles.textArea.CalcHeight(GuiUtility.TempContent(Property.stringValue), EditorGUIUtility.currentViewWidth) / 15f);
        }
    }
}