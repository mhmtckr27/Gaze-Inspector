using System;

namespace GrayWolf.Gaze
{
    internal static partial class GuiUtility
    {
        internal static double CalculateFloatDragSensitivity(double value)
        {
            return double.IsInfinity(value) || double.IsNaN(value) ? 0.0 : Math.Max(1.0, Math.Pow(Math.Abs(value), 0.5)) * 0.029999999329447746;
        }
    }
}