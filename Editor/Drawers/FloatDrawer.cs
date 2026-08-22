using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class FloatDrawer : PropertyDrawerBase
    {
        public override void Draw(GUIContent label)
        {
            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                float newVal;

                if (TryGetAdditionalData(out RangeData rangeData))
                {
                    var isSlider = rangeData.isSlider ?? false;

                    newVal = PropertyTreeGUI.FloatField(Property, label, Property.floatValue, 
                        Data.AdditionalData.Has<DelayedDecorator.DelayedData>(), 
                        isSlider, 
                        rangeData.min, rangeData.max);
                }
                else
                {
                    newVal = PropertyTreeGUI.FloatField(Property, label, Data.AdditionalData.Has<DelayedDecorator.DelayedData>());
                }
            
                if (checkScope.changed)
                    Property.floatValue = newVal;
            }
        }
    }
}