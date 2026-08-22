using System;
using UnityEngine;

namespace GrayWolf.Gaze
{
    /// <summary>
    /// Constrains an <see cref="AnimationCurve"/> field to a fixed time and value window, and tints the
    /// curve preview.
    /// </summary>
    /// <remarks>
    /// The window bounds the curve editor's visible area; it does not clamp keys that were already
    /// outside it. Applies to <see cref="AnimationCurve"/> fields only.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class CurveRangeAttribute : PropertyAttribute
    {
        /// <summary>
        /// The curve window, packed the way Unity's <c>EditorGUI.CurveField</c> ranges parameter expects:
        /// <c>x</c> and <c>width</c> are the time axis, <c>y</c> and <c>height</c> the value axis. A
        /// window of time 0..1 and value -1..1 is therefore <c>(0, -1, 1, 2)</c>.
        /// </summary>
        public Rect Ranges { get; }

        /// <summary>
        /// Colour the curve is drawn in.
        /// </summary>
        public Color Color { get; }

        /// <summary>
        /// Bounds the curve window and draws it in the default pale blue (0.25, 0.75, 1, 1).
        /// </summary>
        /// <param name="timeMin">Left edge of the time axis.</param>
        /// <param name="timeMax">Right edge of the time axis. Must be greater than <paramref name="timeMin"/>.</param>
        /// <param name="valueMin">Bottom edge of the value axis.</param>
        /// <param name="valueMax">Top edge of the value axis. Must be greater than <paramref name="valueMin"/>.</param>
        public CurveRangeAttribute(
            float timeMin,
            float timeMax,
            float valueMin,
            float valueMax)
            : this(
            timeMin, 
            timeMax, 
            valueMin, 
            valueMax, 
            0.25f, 
            0.75f, 
            1f, 
            1f)
        {
        }

        /// <summary>
        /// Bounds the curve window and draws it in an explicit colour.
        /// </summary>
        /// <param name="tMin">Left edge of the time axis.</param>
        /// <param name="tMax">Right edge of the time axis. Must be greater than <paramref name="tMin"/>.</param>
        /// <param name="vMin">Bottom edge of the value axis.</param>
        /// <param name="vMax">Top edge of the value axis. Must be greater than <paramref name="vMin"/>.</param>
        /// <param name="colorR">Red channel, 0 to 1.</param>
        /// <param name="colorG">Green channel, 0 to 1.</param>
        /// <param name="colorB">Blue channel, 0 to 1.</param>
        /// <param name="colorA">Alpha channel, 0 to 1.</param>
        public CurveRangeAttribute(
            float tMin,
            float tMax,
            float vMin,
            float vMax,
            float colorR,
            float colorG,
            float colorB,
            float colorA = 1f
            )
        {
            Ranges = new Rect(tMin, vMin,
                tMax - tMin,
                vMax - vMin);
            Color = new Color(colorR, colorG, colorB, colorA);
        }
    }
}