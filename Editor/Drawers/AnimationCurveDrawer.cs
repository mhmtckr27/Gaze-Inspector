using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class AnimationCurveDrawer : PropertyDrawerBase
    {
        private CurveRangeAttribute _curveRangeAttribute;
        
        protected internal override void Initialize()
        {
            base.Initialize();
            
            _curveRangeAttribute = (CurveRangeAttribute)Data.Attributes.FirstOrDefault(a => a is CurveRangeAttribute);
        }

        private float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void Draw(GUIContent label)
        {
            PropertyTreeGUI.BeginHorizontalLayout(label, out var labelRect);
            PropertyTreeGUI.RegisterPropertyRect(labelRect, Property);

            var rect = EditorGUILayout.GetControlRect(label != null, GetHeight());
            using (new EditorGui.MixedValueScope(Property.hasMultipleDifferentValues))
            {
                using (new EditorGUI.PropertyScope(rect, label, Property))
                {
                    EditorGUI.CurveField(rect, Property, _curveRangeAttribute?.Color ?? Color.green, _curveRangeAttribute?.Ranges ?? default, GUIContent.none);
                }
            }

            PropertyTreeGUI.EndHorizontalLayout();
        }
    }
}