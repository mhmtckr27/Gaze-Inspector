using System;
using UnityEngine;

namespace GrayWolf.Gaze
{
    /// <summary>
    /// Hides the field while the condition holds. The field is hidden entirely, not disabled.
    /// </summary>
    /// <seealso cref="ShowIfAttribute"/>
    [AttributeUsage(AttributeTargets.Field)]
    public class HideIfAttribute : ConditionalAttribute
    {
        /// <inheritdoc/>
        public override bool Invert => true;

        /// <summary>
        /// Hides the field while <paramref name="condition"/> is true.
        /// </summary>
        /// <param name="condition">Member name, optionally prefixed with '!'.</param>
        public HideIfAttribute(string condition) : base(condition)
        {
        }

        /// <summary>
        /// Hides the field while <paramref name="condition"/> equals <paramref name="optionalValue"/>.
        /// </summary>
        /// <param name="condition">Member name, optionally prefixed with '!'.</param>
        /// <param name="optionalValue">Value to compare against.</param>
        public HideIfAttribute(string condition, object optionalValue) : base(condition, optionalValue)
        {
        }
    }
}