using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class LabelTextDecorator : AttributeDrawer<LabelTextAttribute>
    {
        private LabelTextAttribute _attribute;

        protected internal override void Initialize()
        {
            _attribute = Attribute as LabelTextAttribute;
        }

        public override void Draw(GUIContent label)
        {
            label.text = _attribute.Label;
            Inner.Draw(label);
        }
    }
}