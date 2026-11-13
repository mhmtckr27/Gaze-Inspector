using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GrayWolf.Gaze
{
    internal static class RectExtensions
    {
        private const string k_RectPattern = @"\(\s*x:\s*(?<x>-?\d+(\.\d+)?),\s*y:\s*(?<y>-?\d+(\.\d+)?),\s*width:\s*(?<w>-?\d+(\.\d+)?),\s*height:\s*(?<h>-?\d+(\.\d+)?)\s*\)";
        // Matches even if width/height are negative (not typical, but allowed here)
        static readonly string k_RectIntRegexPermissive = @"^\(\s*x:\s*(?<x>-?\d+)\s*,\s*y:\s*(?<y>-?\d+)\s*,\s*width:\s*(?<w>-?\d+)\s*,\s*height:\s*(?<h>-?\d+)\s*\)$";

        public static bool TryParse(string input, out Rect rect)
        {
            rect = default;

            if (string.IsNullOrEmpty(input))
                return false;

            if (TryParse(input, k_RectPattern, out rect))
                return true;
            
            return TryParse(input, k_RectIntRegexPermissive, out rect);
        }

        public static RectInt ToRectInt(this Rect rect)
        {
            return new RectInt(
                new Vector2Int(
                    (int)rect.position.x, 
                    (int)rect.position.y),
                new Vector2Int(
                    (int)rect.size.x, 
                    (int)rect.size.y));
        }

        private static bool TryParse(string input, string pattern, out Rect rect)
        {
            rect = default;
            var m = Regex.Match(input, pattern);
            if (!m.Success)
                return false;//throw new FormatException("Not a rect string");
            var culture = CultureInfo.InvariantCulture;
            rect = new Rect(
                float.Parse(m.Groups["x"].Value, culture),
                float.Parse(m.Groups["y"].Value, culture),
                float.Parse(m.Groups["w"].Value, culture),
                float.Parse(m.Groups["h"].Value, culture)
            );
            return true;
        }
    }
}