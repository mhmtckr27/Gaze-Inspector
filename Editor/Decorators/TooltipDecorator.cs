using System;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class TooltipDecorator : AttributeDrawer<TooltipAttribute>
    {
        protected internal override void Initialize()
        {
            Data.Label.tooltip = (Attribute as TooltipAttribute)?.tooltip;
        }

        public override void Draw(GUIContent label)
        {
            Inner.Draw(label);
        }
    }
}