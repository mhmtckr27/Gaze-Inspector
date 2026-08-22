using System;

namespace GrayWolf.Gaze
{
    /// <summary>
    /// Draws a button under the inspector that invokes the decorated method.
    /// </summary>
    /// <remarks>
    /// The method must take no parameters. It may be private, and it is invoked once per selected
    /// target when editing multiple objects. Pending inspector edits are applied before the call and
    /// each target is registered with Undo, so the method may safely modify the object it is called on.
    /// An exception thrown by the method is logged and does not abort the remaining targets.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute : Attribute
    {
        /// <summary>
        /// Text shown on the button, or <c>null</c> to derive it from the method name.
        /// </summary>
        public readonly string Label;

        /// <summary>
        /// Labels the button with the method name, split into words
        /// (<c>ResetToDefaults</c> becomes "Reset To Defaults").
        /// </summary>
        public ButtonAttribute()
        {
        }

        /// <summary>
        /// Labels the button with <paramref name="label"/>.
        /// </summary>
        /// <param name="label">Button text. Falls back to the derived method name if null or empty.</param>
        public ButtonAttribute(string label)
        {
            Label = label;
        }
    }
}
