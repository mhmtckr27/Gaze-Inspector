using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class Vector2Drawer : PropertyDrawerBase
    {
        private bool _isDataSet;
        private bool _isDelayed;
        private bool _isSlider;
        private (Vector2 min, Vector2 max)? _range;

        private void EnsureData()
        {
            if(_isDataSet)
                return;

            _isDelayed = Data.AdditionalData.Has<DelayedDecorator.DelayedData>();
            
            if (TryGetAdditionalData<VectorRangeDecorator.VectorRangeData>(out var vectorRange))
            {
                _isSlider = vectorRange.isSlider;
                _range = (vectorRange.min, vectorRange.max);
            }
            else if (TryGetAdditionalData<RangeData>(out var range))
            {
                _isSlider = range.isSlider ?? false;
                _range = (Vector2.one * (range.min ?? float.MinValue), Vector2.one * (range.max ?? float.MaxValue));
            }
            
            _isDataSet = true;
        }
        
        public override void Draw(GUIContent label)
        {
            EnsureData();
            
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                Vector2 newVal;
                
                if(_range.HasValue)
                    newVal = PropertyTreeGUI.Vector2Field(Property, label, _isDelayed, _isSlider, _range?.min, _range?.max);
                else
                    newVal = PropertyTreeGUI.Vector2Field(Property, label, _isDelayed);

                if (scope.changed)
                    Property.vector2Value = newVal;
            }
        }
    }
}