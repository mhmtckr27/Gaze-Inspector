namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// Per-property view state for <see cref="SerializedDictionaryDrawer"/>. Lives in
    /// <see cref="PropertyStateStore"/> so column width, search text and sort survive drawer rebuilds,
    /// reselection and domain reloads - the same way foldout and drag state already do.
    /// </summary>
    internal sealed class SerializedDictionaryViewState
    {
        public float KeyColumnFraction = 0.35f;
        public string SearchText = "";
        public bool SortEnabled;
        public int FocusIndex = -1;
    }
}
