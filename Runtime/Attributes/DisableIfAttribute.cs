using System;
using UnityEngine;

namespace GrayWolf.Gaze
{
    /// <summary>
    /// Greys the field out while the condition holds. The field stays visible and readable.
    /// </summary>
    /// <seealso cref="EnableIfAttribute"/>
    [AttributeUsage(AttributeTargets.Field)]
    public class DisableIfAttribute : ConditionalAttribute
    {
        /// <inheritdoc/>
        public override bool Invert => true;

        /// <summary>
        /// Disables the field while <paramref name="condition"/> is true.
        /// </summary>
        /// <param name="condition">Member name, optionally prefixed with '!'.</param>
        public DisableIfAttribute(string condition) : base(condition)
        {
        }

        /// <summary>
        /// Disables the field while <paramref name="condition"/> equals <paramref name="optionalValue"/>.
        /// </summary>
        /// <param name="condition">Member name, optionally prefixed with '!'.</param>
        /// <param name="optionalValue">Value to compare against.</param>
        public DisableIfAttribute(string condition, object optionalValue) : base(condition, optionalValue)
        {
        }
    }
}