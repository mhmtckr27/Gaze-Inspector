using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class HideIfDecorator : AttributeDrawer<HideIfAttribute>
    {
        private HideIfAttribute _attribute;
        private readonly ConditionCache _condition = new();
        private readonly FoldoutAnimState _anim = new();
        private bool _seeded;

        protected internal override void Initialize()
        {
            _attribute = Attribute as HideIfAttribute;
        }

        protected internal override void CollectMessages(List<GazeMessage> messages)
        {
            var evalResult = _condition.Evaluate(Property, _attribute, out var error);

            if (!string.IsNullOrEmpty(error))
                messages.Add(new GazeMessage(InfoMessageType.Error, error));

            if (evalResult)
                base.CollectMessages(messages);
        }

        public override void Draw(GUIContent label)
        {
            var visible = _condition.Result;

            if (!_seeded)
            {
                _seeded = true;
                _anim.expanded = visible;
                _anim.value = visible ? 1f : 0f;
                _anim.stamp = EditorApplication.timeSinceStartup;
            }

            if (PropertyTreeGUI.BeginFadeGroup(_anim, visible))
                Inner.Draw(label);

            PropertyTreeGUI.EndFadeGroup();
        }
    }
}