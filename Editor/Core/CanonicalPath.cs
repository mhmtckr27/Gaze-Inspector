using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace GrayWolf.Gaze.Editor
{
    internal static class CanonicalPath
    {
        // Matches ".Array.data[123]"
        static readonly Regex s_ArraySeg = new Regex(@"\.Array\.data\[(\d+)\]", RegexOptions.Compiled);

        public static string Canonical(SerializedProperty prop)
        {
            var so = prop.serializedObject;
            string path = prop.propertyPath;
            if (string.IsNullOrEmpty(path)) return path;

            var sb = new StringBuilder(path.Length + 64);
            int last = 0;

            foreach (Match m in s_ArraySeg.Matches(path))
            {
                // copy chunk up to ".Array"
                sb.Append(path, last, m.Index - last);

                // parent array path is everything before ".Array"
                string arrayPath = path.Substring(0, m.Index);
                var arrayProp = so.FindProperty(arrayPath);
                int idx = int.Parse(m.Groups[1].Value);

                string uid = (arrayProp != null) ? ElementIdDatabase.For(arrayProp, idx) : $"idx:{idx}";
                sb.Append(".Array.uid[");
                sb.Append(uid);
                sb.Append(']');

                last = m.Index + m.Length;
            }

            // tail after the last match
            sb.Append(path, last, path.Length - last);
            return sb.ToString();
        }
    }
}