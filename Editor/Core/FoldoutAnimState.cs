namespace GrayWolf.Gaze.Editor
{
    internal sealed class FoldoutAnimState
    {
        public bool expanded;          // logical state
        public float value = -1;
        public float duration = 0.125f;
        public float stamp;
    }
}