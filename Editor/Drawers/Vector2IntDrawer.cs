using System;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class Vector2IntDrawer : PropertyDrawerBase
    {
        private bool _isDataSet;
        private bool _isDelayed;
        private bool _isSlider;
        private (Vector2Int min, Vector2Int max)? _range;

        private void EnsureData()
        {
            if(_isDataSet)
                return;
            
            _isDelayed = Data.AdditionalData.Has<DelayedDecorator.DelayedData>();
            
            if (TryGetAdditionalData<VectorRangeDecorator.VectorRangeData>(out var vectorRange))
            {
                _isSlider = vectorRange.isSlider;
                _range = (
                    new Vector2Int(
                        (int)vectorRange.min.x, 
                        (int)vectorRange.min.y), 
                    new Vector2Int(
                        (int)vectorRange.max.x, 
                        (int)vectorRange.max.y));
            }
            else if (TryGetAdditionalData<RangeData>(out var range))
            {
                _isSlider = range.isSlider ?? false;
                _range = (Vector2Int.one * (range.MinAsInt ?? int.MinValue), Vector2Int.one * (range.MaxAsInt ?? int.MaxValue));
            }
            
            _isDataSet = true;
        }

        public override void Draw(GUIContent label)
        {
            EnsureData();
            
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                Vector2Int newVal;
                
                if(_range.HasValue)
                    newVal = PropertyTreeGUI.Vector2IntField(Property, label, _isDelayed, _isSlider, _range?.min, _range?.max);
                else
                    newVal = PropertyTreeGUI.Vector2IntField(Property, label, _isDelayed);

                if (scope.changed)
                    Property.vector2IntValue = newVal;
            }
        }
    }
}