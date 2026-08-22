using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class CharacterDrawer : PropertyDrawerBase
    {
        private float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight;
        }
        
        public override void Draw(GUIContent label)
        {
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var newVal = PropertyTreeGUI.TextField(Property, label, char.ConvertFromUtf32(Property.intValue), GetHeight(), Data.AdditionalData.Has<DelayedDecorator.DelayedData>());

                if (scope.changed)
                    Property.intValue = string.IsNullOrEmpty(newVal) ? 0 : newVal[0];
            }
        }
    }
}