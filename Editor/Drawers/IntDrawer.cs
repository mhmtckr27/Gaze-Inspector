using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class IntDrawer : PropertyDrawerBase
    {
        public override void Draw(GUIContent label)
        {
            using (var changeScope = new EditorGUI.ChangeCheckScope())
            {
                int newVal;

                if (TryGetAdditionalData(out RangeData rangeData))
                {
                    var isSlider = rangeData.isSlider ?? false;

                    newVal = PropertyTreeGUI.IntField(Property, label, Property.intValue, 
                        Data.AdditionalData.Has<DelayedDecorator.DelayedData>(), 
                        isSlider, 
                        rangeData.MinAsInt, rangeData.MaxAsInt);
                }
                else
                {
                    newVal = PropertyTreeGUI.IntField(Property, label, Data.AdditionalData.Has<DelayedDecorator.DelayedData>());
                }

                if (changeScope.changed)
                    Property.intValue = newVal;
            }
        }
    }
}