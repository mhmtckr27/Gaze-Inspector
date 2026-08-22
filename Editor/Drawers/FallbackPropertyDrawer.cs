using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// Fallback drawer: delegates to EditorGUI.PropertyField with matching height.
    /// For Generic (struct/class), we include children so Unity renders them.
    /// </summary>
    internal sealed class FallbackPropertyDrawer : PropertyDrawerBase
    {
        private float GetHeight()
        {
            var includeChildren = Property.propertyType == SerializedPropertyType.Generic;
            return EditorGUI.GetPropertyHeight(Property, Data.Label, includeChildren);
        }

        public override void Draw(GUIContent label)
        {
            var rect = EditorGUILayout.GetControlRect(label != null, GetHeight());
            EditorGUI.PropertyField(rect, Property, label);
        }
    }
}
