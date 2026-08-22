using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class RangeDecorator : AttributeDrawer<RangeAttribute>
    {
        protected internal override void Initialize()
        {
            base.Initialize();
            var attribute = (RangeAttribute) Attribute;
            var data = Data.AdditionalData.GetOrCreate<RangeData>();
            data.min = attribute.min;
            data.max = attribute.max;
            data.isSlider = Property.propertyType is SerializedPropertyType.Float or SerializedPropertyType.Integer;
        }

        public override void Draw(GUIContent label)
        {
            Inner.Draw(label);
        }
    }
}