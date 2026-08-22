using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class Vector4Drawer : PropertyDrawerBase
    {
        private bool _isDataSet;
        private bool _isDelayed;
        private bool _isSlider;
        private (Vector4 min, Vector4 max)? _range;

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
                _range = (Vector4.one * (range.min ?? float.MinValue), Vector4.one * (range.max ?? float.MaxValue));
            }
            
            _isDataSet = true;
        }
        
        public override void Draw(GUIContent label)
        {
            EnsureData();
            
            using (var scope = new EditorGUI.ChangeCheckScope())
            {                
                Vector4 newVal;
                
                if(_range.HasValue)
                    newVal = PropertyTreeGUI.Vector4Field(Property, label, _isDelayed, _isSlider, _range?.min, _range?.max);
                else
                    newVal = PropertyTreeGUI.Vector4Field(Property, label, _isDelayed);
                
                if (scope.changed)
                    Property.vector4Value = newVal;
            }
        }
    }
}