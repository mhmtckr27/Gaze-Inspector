using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GrayWolf.Gaze
{
    internal static class BoundsExtensions
    {
        // Float number pattern: 1, -1, 1.0, .5, 1e-3, -2.5E+4, etc.
        const string Float = @"[-+]?(?:\d+(?:\.\d+)?|\.\d+)(?:[eE][-+]?\d+)?";

        // Center: (0.00, 1.00, 0.00), Extents: (1.00, 1.00, 1.00)
        public static readonly Regex BoundsRegex = new Regex(
            $@"^\s*Center:\s*\(\s*(?<cx>{Float})\s*,\s*(?<cy>{Float})\s*,\s*(?<cz>{Float})\s*\)\s*,\s*Extents:\s*\(\s*(?<ex>{Float})\s*,\s*(?<ey>{Float})\s*,\s*(?<ez>{Float})\s*\)\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        
        // Position: (0, 1, 0), Size: (2, 2, 2)
        // Permissive: signed for size too (if you want to accept weird inputs)
        public static readonly Regex BoundsIntRegexPermissive = new Regex(
            @"^\s*Position:\s*\(\s*(?<px>-?\d+)\s*,\s*(?<py>-?\d+)\s*,\s*(?<pz>-?\d+)\s*\)\s*,\s*Size:\s*\(\s*(?<sx>-?\d+)\s*,\s*(?<sy>-?\d+)\s*,\s*(?<sz>-?\d+)\s*\)\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        
        public static bool TryParse(string input, out Bounds bounds)
        {
            bounds = default;
            var m = BoundsRegex.Match(input);
            if (!m.Success)
                return false;//throw new FormatException("Not a Bounds string");
            var ci = CultureInfo.InvariantCulture;

            var center = new Vector3(
                float.Parse(m.Groups["cx"].Value, ci),
                float.Parse(m.Groups["cy"].Value, ci),
                float.Parse(m.Groups["cz"].Value, ci));

            var extents = new Vector3(
                float.Parse(m.Groups["ex"].Value, ci),
                float.Parse(m.Groups["ey"].Value, ci),
                float.Parse(m.Groups["ez"].Value, ci));

            // Bounds constructor takes center + SIZE (not extents)
            var size = extents * 2f;
            bounds = new Bounds(center, size);
            return true;
        }

        public static bool TryParse(string input, out BoundsInt boundsInt)
        {
            boundsInt = default;
            var m = BoundsIntRegexPermissive.Match(input);
            if (!m.Success)
                return false;//throw new FormatException("Not a BoundsInt string");

            int px = int.Parse(m.Groups["px"].Value, System.Globalization.CultureInfo.InvariantCulture);
            int py = int.Parse(m.Groups["py"].Value, System.Globalization.CultureInfo.InvariantCulture);
            int pz = int.Parse(m.Groups["pz"].Value, System.Globalization.CultureInfo.InvariantCulture);

            int sx = int.Parse(m.Groups["sx"].Value, System.Globalization.CultureInfo.InvariantCulture);
            int sy = int.Parse(m.Groups["sy"].Value, System.Globalization.CultureInfo.InvariantCulture);
            int sz = int.Parse(m.Groups["sz"].Value, System.Globalization.CultureInfo.InvariantCulture);

            boundsInt = new BoundsInt(new Vector3Int(px, py, pz), new Vector3Int(sx, sy, sz));
            return true;
        }
    }
}