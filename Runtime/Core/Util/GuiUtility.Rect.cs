using UnityEngine;

namespace GrayWolf.Gaze
{
    internal static partial class GuiUtility
    {
        internal static bool HitTest(Rect rect, Vector2 point, int offset)
        {
            return (double) point.x >= (double) rect.xMin - (double) offset && (double) point.x < (double) rect.xMax + (double) offset && (double) point.y >= (double) rect.yMin - (double) offset && (double) point.y < (double) rect.yMax + (double) offset;
        }

        internal static bool HitTest(Rect rect, Vector2 point, bool isDirectManipulationDevice)
        {
            int offset = 0;
            return HitTest(rect, point, offset);
        }

        internal static bool HitTest(Rect rect, Event evt)
        {
            return HitTest(rect, evt.mousePosition, IsDirectManipulationDevice(evt));
        }
        
        internal static bool IsDirectManipulationDevice(Event evt)
        {
            return evt.pointerType is PointerType.Pen or PointerType.Touch;
        }
    }
}