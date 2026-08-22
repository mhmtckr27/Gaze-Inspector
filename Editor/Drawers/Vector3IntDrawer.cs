using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class Vector3IntDrawer : PropertyDrawerBase
    {
        private bool _isDataSet;
        private bool _isDelayed;
        private bool _isSlider;
        private (Vector3Int min, Vector3Int max)? _range;

        private void EnsureData()
        {
            if(_isDataSet)
                return;

            _isDelayed = Data.AdditionalData.Has<DelayedDecorator.DelayedData>();

            if (TryGetAdditionalData<VectorRangeDecorator.VectorRangeData>(out var vectorRange))
            {
                _isSlider = vectorRange.isSlider;
                _range = (
                    new Vector3Int(
                        (int)vectorRange.min.x, 
                        (int)vectorRange.min.y,
                        (int)vectorRange.min.z), 
                    new Vector3Int(
                        (int)vectorRange.max.x, 
                        (int)vectorRange.max.y,
                        (int)vectorRange.max.z));
            }
            else if (TryGetAdditionalData<RangeData>(out var range))
            {
                _isSlider = range.isSlider ?? false;
                _range = (Vector3Int.one * (range.MinAsInt ?? int.MinValue), Vector3Int.one * (range.MaxAsInt ?? int.MaxValue));
            }
            
            _isDataSet = true;
        }
        
        public override void Draw(GUIContent label)
        {
            EnsureData();
            
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                Vector3Int newVal;
                
                if(_range.HasValue)
                    newVal = PropertyTreeGUI.Vector3IntField(Property, label, _isDelayed, _isSlider, _range?.min, _range?.max);
                else
                    newVal = PropertyTreeGUI.Vector3IntField(Property, label, _isDelayed);

                if (scope.changed)
                    Property.vector3IntValue = newVal;
            }
        }
    }
}