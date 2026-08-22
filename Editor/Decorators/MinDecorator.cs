using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class MinDecorator : AttributeDrawer<MinAttribute>
    {
        protected internal override void Initialize()
        {
            base.Initialize();
            var attribute = (MinAttribute) Attribute;
            var data = Data.AdditionalData.GetOrCreate<RangeData>();
            data.min = attribute.min;
        }

        public override void Draw(GUIContent label)
        {
            Inner.Draw(label);
        }
    }
}