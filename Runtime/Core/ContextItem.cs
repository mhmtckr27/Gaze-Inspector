namespace GrayWolf.Gaze
{
    /// <summary>
    /// This is needed to add the data to <c>ContextContainer</c> and will control how the data are removed when calling Dispose on the <c>ContextContainer</c>.
    /// </summary>
    internal abstract class ContextItem
    {
        /// <summary>
        /// Resets the object so it can be used as a new instance next time it is created.
        /// To avoid memory allocations and generating garbage, the system reuses objects.
        /// This function should clear the object so it can be reused without leaking any
        /// information (e.g. pointers to objects that will no longer be valid to access).
        /// So it is important the implementation carefully clears all relevant members.
        /// Note that this is different from a Dispose or Destructor as the object in not
        /// freed but reset. This can be useful when havin large sub-allocated objects like
        /// arrays or lists which can be cleared and reused without re-allocating.
        /// </summary>
        public abstract void Reset();
    }
}