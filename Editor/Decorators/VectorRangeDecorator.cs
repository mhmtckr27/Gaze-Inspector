using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class VectorRangeDecorator : AttributeDrawer<VectorRangeAttribute>
    {
        protected internal override void Initialize()
        {
            base.Initialize();
            var attribute = (VectorRangeAttribute) Attribute;
            if (Property.propertyType is 
                SerializedPropertyType.Vector2 or 
                SerializedPropertyType.Vector3 or 
                SerializedPropertyType.Vector4)
            {
                var data = Data.AdditionalData.GetOrCreate<VectorRangeData>();
                data.min = new Vector4(
                    attribute.minX ?? float.MinValue, 
                    attribute.minY ?? float.MinValue, 
                    attribute.minZ ?? float.MinValue, 
                    attribute.minW ?? float.MinValue);

                data.max = new Vector4(
                    attribute.maxX ?? float.MaxValue, 
                    attribute.maxY ?? float.MaxValue,
                    attribute.maxZ ?? float.MaxValue, 
                    attribute.maxW ?? float.MaxValue);

                data.isSlider = attribute.showSliders;
            }
            else if (Property.propertyType is
                     SerializedPropertyType.Vector2Int or
                     SerializedPropertyType.Vector3Int)
            {
                var data = Data.AdditionalData.GetOrCreate<VectorRangeData>();
                data.min = new Vector4(
                    attribute.minX ?? int.MinValue, 
                    attribute.minY ?? int.MinValue, 
                    attribute.minZ ?? int.MinValue, 
                    attribute.minW ?? int.MinValue);

                data.max = new Vector4(
                    attribute.maxX ?? int.MaxValue, 
                    attribute.maxY ?? int.MaxValue,
                    attribute.maxZ ?? int.MaxValue, 
                    attribute.maxW ?? int.MaxValue);

                data.isSlider = attribute.showSliders;
            }
        }

        public override void Draw(GUIContent label)
        {
            Inner.Draw(label);
        }
        
        internal class VectorRangeData : ContextItem
        {
            internal bool isSlider;
            internal Vector4 min;
            internal Vector4 max;
            
            public override void Reset()
            {
                isSlider = false;
                min = default;
                max = default;
            }
        }
    }
}