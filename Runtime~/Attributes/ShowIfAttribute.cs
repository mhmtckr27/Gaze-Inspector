using System;

namespace GrayWolf.Gaze
{
    /// <summary>
    /// Draws the field only while the condition holds. The field is hidden entirely, not disabled.
    /// </summary>
    /// <seealso cref="HideIfAttribute"/>
    [AttributeUsage(AttributeTargets.Field)]
    public class ShowIfAttribute : ConditionalAttribute
    {
        /// <inheritdoc/>
        public override bool Invert => false;

        /// <summary>
        /// Shows the field while <paramref name="condition"/> is true.
        /// </summary>
        /// <param name="condition">Member name, optionally prefixed with '!'.</param>
        public ShowIfAttribute(string condition) : base(condition)
        {
        }

        /// <summary>
        /// Shows the field while <paramref name="condition"/> equals <paramref name="optionalValue"/>.
        /// </summary>
        /// <param name="condition">Member name, optionally prefixed with '!'.</param>
        /// <param name="optionalValue">Value to compare against.</param>
        public ShowIfAttribute(string condition, object optionalValue) : base(condition, optionalValue)
        {
        }
    }
}