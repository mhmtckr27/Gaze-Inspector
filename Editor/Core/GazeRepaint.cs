// Editor-only
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    public static class GazeRepaint
    {
        internal static bool ShouldRepaint;

        public static void RequestRepaint() => ShouldRepaint = true;

        public static void TryRepaint(this UnityEditor.Editor editor)
        {
            if (!Consume())
                return;

            editor.Repaint();
        }

        public static void TryRepaint(this EditorWindow window)
        {
            if (!Consume())
                return;

            window.Repaint();
        }

        static bool Consume()
        {
            if (!ShouldRepaint && !WantsRepaint(Event.current))
                return false;

            ShouldRepaint = false;
            return true;
        }

        static bool WantsRepaint(Event e)
            => e != null && e.type != EventType.MouseMove && (e.type == EventType.Used || e.isMouse);
    }
}
