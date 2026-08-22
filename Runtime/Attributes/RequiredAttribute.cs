using System;
using UnityEngine;

namespace GrayWolf.Gaze
{
    /// <summary>
    /// Shows an error box above the field while it is unset.
    /// </summary>
    /// <remarks>
    /// A string field counts as unset when it is null or empty; any other field counts as unset when its
    /// object reference is null. This reports the problem in the inspector only - it does not block
    /// entering play mode or building.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class RequiredAttribute : PropertyAttribute
    {
        /// <summary>
        /// Text of the error box, or <c>null</c> to use a message derived from the field's label.
        /// </summary>
        public readonly string ErrorMessage;

        /// <summary>
        /// Reports the field with a default message: "{label} is required." for a string field,
        /// "{label} must be assigned." for anything else.
        /// </summary>
        public RequiredAttribute()
        {
        }

        /// <summary>
        /// Reports the field with a custom message.
        /// </summary>
        /// <param name="errorMessage">Text to show. Falls back to the default message if null or empty.</param>
        public RequiredAttribute(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}