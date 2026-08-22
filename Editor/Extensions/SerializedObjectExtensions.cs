using System.Collections.Generic;
using UnityEditor;

namespace GrayWolf.Gaze.Editor
{
    internal static class SerializedObjectExtensions
    {
        public static IEnumerable<SerializedProperty> Roots(this SerializedObject serializedObject)
        {
            if (serializedObject == null) 
                yield break;

            var it = serializedObject.GetIterator().Copy();
            if (!it.NextVisible(true)) yield break; // move to first

            do
            {
                if (it.depth != 0) continue;                 // only top-level
                yield return it.Copy();                      // always return a Copy()
            }
            while (it.NextVisible(false));
        }
    }
}