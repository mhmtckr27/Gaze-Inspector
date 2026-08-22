using System.Collections.Generic;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class DisableIfDecorator : AttributeDrawer<DisableIfAttribute>
    {
        private DisableIfAttribute _attribute;
        private readonly ConditionCache _condition = new();

        protected internal override void Initialize()
        {
            _attribute = Attribute as DisableIfAttribute;
        }

        protected internal override void CollectMessages(List<GazeMessage> messages)
        {
            _condition.Evaluate(Property, _attribute, out var error);

            if (!string.IsNullOrEmpty(error))
                messages.Add(new GazeMessage(InfoMessageType.Error, error));

            base.CollectMessages(messages);
        }

        public override void Draw(GUIContent label)
        {
            using (new EditorGui.DisabledScope(!_condition.Result))
            {
                Inner.Draw(label);
            }
        }
    }
}