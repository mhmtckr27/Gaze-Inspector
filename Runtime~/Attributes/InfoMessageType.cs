namespace GrayWolf.Gaze
{
    /// <summary>
    /// Severity of an inspector message box, which selects its icon. Ordered least to most severe;
    /// stacked messages on one field are sorted by this order, most severe first.
    /// </summary>
    public enum InfoMessageType
    {
        /// <summary>Generic message box with no type.</summary>
        None,
        /// <summary>Information message box.</summary>
        Info,
        /// <summary>Warning message box.</summary>
        Warning,
        /// <summary>Error message box.</summary>
        Error,
    }
}