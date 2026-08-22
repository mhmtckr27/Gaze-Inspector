using UnityEngine;
using UnityEngine.Serialization;

namespace GrayWolf.Gaze.Editor
{
    internal class FormerlySerializedAsDecorator : AttributeDrawer<FormerlySerializedAsAttribute>
    {
        public override void Draw(GUIContent label)
        {
            Inner.Draw(label);
        }
    }
}