using System;
using UnityEngine;

namespace GrayWolf.Gaze
{
    /// <summary>
    /// Calls <see cref="FunctionName"/> after the field is edited in the inspector.
    /// The method may be private, may be declared on a base type, and must take either
    /// zero parameters or two parameters (old value, new value).
    /// Fires only on direct user edits through the inspector - not on Undo/Redo,
    /// script-driven changes to the field, or domain reload.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class OnValueChangedAttribute : PropertyAttribute
    {
        /// <summary>Name of the method to call. Resolved on the object that declares the field.</summary>
        public readonly string FunctionName;

        /// <summary>
        /// Calls <paramref name="functionName"/> after each inspector edit of the field.
        /// </summary>
        /// <param name="functionName">
        /// Method name on the declaring type or one of its base types. The method must take no
        /// parameters, or exactly two of the field's type (old value, new value).
        /// </param>
        public OnValueChangedAttribute(string functionName)
        {
            FunctionName = functionName;
        }
    }
}