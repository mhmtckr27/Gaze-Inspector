using UnityEditor;
using UnityEditor.SceneManagement;

namespace GrayWolf.Gaze.Editor
{
    [InitializeOnLoad]
    internal static class GazeEditorCache
    {
        public static event System.Action OnClear;
        
        static GazeEditorCache()
        {
            // Assembly reload
            AssemblyReloadEvents.beforeAssemblyReload += Clear;

            // Play mode (handles “disable domain reload” mode)
            EditorApplication.playModeStateChanged += _ => Clear();

            // Scene switches (scene reload can kill temp textures)
            EditorSceneManager.sceneOpened  += (_, __) => Clear();
            EditorSceneManager.sceneClosed  += _ => Clear();

            // Project changes can also affect editor resources
            EditorApplication.projectChanged += Clear;

            // Editor quitting
            EditorApplication.quitting += Clear;
        }

        private static void Clear()
        {
            OnClear?.Invoke();
        }
    }
}