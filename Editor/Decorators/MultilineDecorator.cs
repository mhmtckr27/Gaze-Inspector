using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class MultilineDecorator : AttributeDrawer<MultilineAttribute>
    {
        private MultilineAttribute _attribute;

        protected internal override void Initialize()
        {
            _attribute = Attribute as MultilineAttribute;

            var data = Data.AdditionalData.GetOrCreate<MultilineData>();
            data.LineCount = _attribute?.lines ?? 1;
        }

        public override void Draw(GUIContent label)
        {
            Inner.Draw(label);
        }
        
        internal class MultilineData : ContextItem
        {
            internal int LineCount = 1;
        
            public override void Reset()
            {
            }
        }
    }
}