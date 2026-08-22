using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class CurveRangeDecorator : AttributeDrawer<CurveRangeAttribute>
    {
        public override void Draw(GUIContent label)
        {
            Inner.Draw(label);
        }
    }
}