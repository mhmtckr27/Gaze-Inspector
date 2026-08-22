using System;
using UnityEngine;

namespace GrayWolf.Gaze
{
    /// <summary>
    /// Leaves the field editable only while the condition holds. The field stays visible and is greyed
    /// out when the condition fails.
    /// </summary>
    /// <seealso cref="DisableIfAttribute"/>
    [AttributeUsage(AttributeTargets.Field)]
    public class EnableIfAttribute : ConditionalAttribute
    {
        /// <inheritdoc/>
        public override bool Invert => false;

        /// <summary>
        /// Enables the field while <paramref name="condition"/> is true.
        /// </summary>
        /// <param name="condition">Member name, optionally prefixed with '!'.</param>
        public EnableIfAttribute(string condition) : base(condition)
        {
        }

        /// <summary>
        /// Enables the field while <paramref name="condition"/> equals <paramref name="optionalValue"/>.
        /// </summary>
        /// <param name="condition">Member name, optionally prefixed with '!'.</param>
        /// <param name="optionalValue">Value to compare against.</param>
        public EnableIfAttribute(string condition, object optionalValue) : base(condition, optionalValue)
        {
        }
    }
}