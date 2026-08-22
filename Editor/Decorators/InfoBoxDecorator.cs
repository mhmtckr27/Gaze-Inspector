using System.Collections.Generic;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal sealed class InfoBoxDecorator : AttributeDrawer<InfoBoxAttribute>
    {
        private InfoBoxAttribute _attribute;

        protected internal override void Initialize()
        {
            _attribute = Attribute as InfoBoxAttribute;
        }

        protected internal override void CollectMessages(List<GazeMessage> messages)
        {
            messages.Add(new GazeMessage(_attribute.Type, _attribute.Message));
            base.CollectMessages(messages);
        }

        public override void Draw(GUIContent label)
        {
            Inner.Draw(label);
        }
    }
}