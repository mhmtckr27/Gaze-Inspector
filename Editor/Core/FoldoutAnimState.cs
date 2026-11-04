namespace GrayWolf.Gaze.Editor
{
    internal sealed class FoldoutAnimState
    {
        public bool expanded;          // logical state
        public float value = -1;
        public float duration = 0.125f;

        // double, and stamped from EditorApplication.timeSinceStartup rather than
        // Time.realtimeSinceStartup: the latter is the Player-loop clock and is documented to reset
        // on entering Play Mode, which produced a negative delta here and corrupted `value` (see the
        // comment in PropertyTreeGUI.BeginFadeGroup). EditorApplication.timeSinceStartup tracks the
        // Editor process itself and isn't affected by Play Mode transitions.
        public double stamp;
    }
}