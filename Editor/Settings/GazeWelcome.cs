using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// Opens the settings window once per project, on the first domain reload after Gaze is
    /// added, so the package introduces itself instead of waiting to be found under a menu.
    /// </summary>
    /// <remarks>
    /// The "already shown" flag lives in <see cref="GazeInspectorSettings"/>, which means
    /// ProjectSettings/: per project, and surviving Editor restarts and domain reloads.
    /// EditorPrefs would be wrong here, being shared across every project on the machine, so
    /// only the first project to ever install Gaze would see the window. SessionState would be
    /// wrong the other way, resetting on every Editor restart.
    /// </remarks>
    [InitializeOnLoad]
    internal static class GazeWelcome
    {
        static GazeWelcome()
        {
            // The static constructor runs partway through a domain reload, before the Editor is
            // in a state where opening a window is safe. delayCall runs once that has settled.
            EditorApplication.delayCall += ShowOnce;
        }

        private static void ShowOnce()
        {
            // No one to introduce anything to on a build server.
            if (Application.isBatchMode)
                return;

            // Entering play mode is itself a domain reload. Opening a window over someone's
            // play session would be the same rudeness as opening it on every project load, so
            // leave the flag unspent and let the next reload do it.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var settings = GazeInspectorSettings.instance;
            if (settings.WelcomeShown)
                return;

            // Spent before the window opens, not after: if opening throws, the right outcome is
            // one missed welcome rather than a window that reappears on every reload from here.
            settings.WelcomeShown = true;

            GazeInspectorSettingsWindow.Open();
        }
    }
}
