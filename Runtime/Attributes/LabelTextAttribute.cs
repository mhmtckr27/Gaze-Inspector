using System;
using UnityEngine;

namespace GrayWolf.Gaze
{
    /// <summary>
    /// Replaces the field's inspector label, which Unity would otherwise derive from the field name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class LabelTextAttribute : PropertyAttribute
    {
        /// <summary>Text to show in place of the derived field name.</summary>
        public readonly string Label;

        /// <summary>
        /// Labels the field <paramref name="label"/>.
        /// </summary>
        /// <param name="label">Replacement label text.</param>
        public LabelTextAttribute(string label)
        {
            Label = label;
        }
    }
}