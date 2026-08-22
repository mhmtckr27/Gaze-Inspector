using System;
using UnityEngine;

namespace GrayWolf.Gaze
{
    /// <summary>
    /// Shows the field greyed out and blocks editing it from the inspector.
    /// </summary>
    /// <remarks>
    /// The field is still serialized and still writable from script; this only removes the inspector as
    /// a way to change it. Use <see cref="DisableIfAttribute"/> when the field should be editable under
    /// some conditions.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }
}