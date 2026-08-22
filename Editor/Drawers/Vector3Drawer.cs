using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class Vector3Drawer : PropertyDrawerBase
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
                _range = (Vector3.one * (range.min ?? float.MinValue), Vector3.one * (range.max ?? float.MaxValue));
            }
            
            _isDataSet = true;
        }
        
        public override void Draw(GUIContent label)
        {
            EnsureData();
            
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                Vector3 newVal;
                
                if(_range.HasValue)
                    newVal = PropertyTreeGUI.Vector3Field(Property, label, _isDelayed, _isSlider, _range?.min, _range?.max);
                else
                    newVal = PropertyTreeGUI.Vector3Field(Property, label, _isDelayed);

                if (scope.changed)
                    Property.vector3Value = newVal;
            }
        }
    }
}