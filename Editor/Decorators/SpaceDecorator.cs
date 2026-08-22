using System;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class SpaceDecorator : AttributeDrawer<SpaceAttribute>
    {
        private SpaceAttribute _attribute;

        protected internal override void Initialize()
        {
            _attribute = Attribute as SpaceAttribute;
        }

        public override void Draw(GUIContent label)
        {
            PropertyTreeGUI.BeginVerticalLayout(GUIContent.none, out _);
            GUILayout.Space(_attribute.height);
            PropertyTreeGUI.EndVerticalLayout();
            PropertyTreeGUI.DrawNode(Inner, label);
        }
    }
}