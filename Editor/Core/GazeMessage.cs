namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// One message box a drawer wants shown above its property.
    /// </summary>
    /// <seealso cref="DrawerBase.CollectMessages"/>
    public readonly struct GazeMessage
    {
        /// <summary>Severity, which selects the icon and the sort order among stacked messages.</summary>
        public readonly InfoMessageType Type;

        /// <summary>Message body. Wrapped to the inspector width when drawn.</summary>
        public readonly string Text;

        /// <summary>
        /// Creates a message.
        /// </summary>
        /// <param name="type">Severity of the message.</param>
        /// <param name="text">Message body.</param>
        public GazeMessage(InfoMessageType type, string text)
        {
            Type = type;
            Text = text;
        }
    }
}
