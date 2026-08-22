using System;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class FlagsDecorator : AttributeDrawer<FlagsAttribute>
    {
        protected internal override void Initialize()
        {
            Inner.Data.AdditionalData.GetOrCreate<EnumDrawerData>().IsFlags = Attribute is FlagsAttribute;
        }
        
        public override void Draw(GUIContent label)
        {
            Inner.Draw(label);
        }
    }
}