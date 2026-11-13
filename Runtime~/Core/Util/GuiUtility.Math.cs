using System;
using UnityEngine;

namespace GrayWolf.Gaze
{
    internal static partial class GuiUtility
    {
        internal static float ClampToFloat(double value)
        {
            if (double.IsPositiveInfinity(value))
                return float.PositiveInfinity;
            if (double.IsNegativeInfinity(value))
                return float.NegativeInfinity;
            if (value < -3.4028234663852886E+38)
                return float.MinValue;
            return value > 3.4028234663852886E+38 ? float.MaxValue : (float) value;
        }
        
        internal static float RoundBasedOnMinimumDifference(float valueToRound, float minDifference)
        {
            return (double) minDifference == 0.0 ? DiscardLeastSignificantDecimal(valueToRound) : (float) Math.Round((double) valueToRound, GetNumberOfDecimalsForMinimumDifference(minDifference), MidpointRounding.AwayFromZero);
        }
        
        internal static float DiscardLeastSignificantDecimal(float v)
        {
            int digits = Mathf.Clamp((int)(5.0 - (double)Mathf.Log10(Mathf.Abs(v))), 0, 15);
            return (float)Math.Round((double)v, digits, MidpointRounding.AwayFromZero);
        }
        
        internal static int GetNumberOfDecimalsForMinimumDifference(float minDifference)
        {
            return Mathf.Clamp(-Mathf.FloorToInt(Mathf.Log10(Mathf.Abs(minDifference))), 0, 15);
        }
        
        internal static double RoundBasedOnMinimumDifference(double valueToRound, double minDifference)
        {
            return minDifference == 0.0 ? DiscardLeastSignificantDecimal(valueToRound) : Math.Round(valueToRound, GetNumberOfDecimalsForMinimumDifference(minDifference), MidpointRounding.AwayFromZero);
        }
        
        internal static double DiscardLeastSignificantDecimal(double v)
        {
            int digits = Math.Max(0, (int) (5.0 - Math.Log10(Math.Abs(v))));
            try
            {
                return Math.Round(v, digits);
            }
            catch
            {
                return 0.0;
            }
        }

        internal static int GetNumberOfDecimalsForMinimumDifference(double minDifference)
        {
            return (int) Math.Max(0.0, -Math.Floor(Math.Log10(Math.Abs(minDifference))));
        }
    }
}