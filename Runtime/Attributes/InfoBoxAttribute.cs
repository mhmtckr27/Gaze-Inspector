using System;
using UnityEngine;

namespace GrayWolf.Gaze
{
    /// <summary>
    /// Draws a fixed help box above the field.
    /// </summary>
    /// <remarks>
    /// The message is static; nothing about the field's value affects it. Several boxes may be stacked
    /// on one field, in which case they are ordered most severe first. On a field inside a list, the
    /// box is drawn once for the list rather than once per element.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class InfoBoxAttribute : PropertyAttribute
    {
        /// <summary>Text shown in the box.</summary>
        public readonly string Message;

        /// <summary>Icon and severity of the box.</summary>
        public readonly InfoMessageType Type;

        /// <summary>
        /// Draws a help box carrying <paramref name="message"/>.
        /// </summary>
        /// <param name="message">Text to show.</param>
        /// <param name="type">Icon and severity. Defaults to <see cref="InfoMessageType.Info"/>.</param>
        public InfoBoxAttribute(string message, InfoMessageType type = InfoMessageType.Info)
        {
            Message = message;
            Type = type;
        }
    }
}