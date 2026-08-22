using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class DelayedDecorator : AttributeDrawer<DelayedAttribute>
    {
        protected internal override void Initialize()
        {
            base.Initialize();
            Data.AdditionalData.GetOrCreate<DelayedData>();
        }

        public override void Draw(GUIContent label)
        {
            Inner.Draw(label);
        }
        
        internal class DelayedData : ContextItem
        {
            public override void Reset()
            {
            }
        }
    }
}