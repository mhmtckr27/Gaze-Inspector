using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// Runs the saved-data checks and keeps them off the drawing path.
    /// </summary>
    /// <remarks>
    /// Automatically, only what was edited since the last save is compared against the file, so the cost
    /// scales with how much was changed rather than with the size of the scene. The whole-scene sweep is
    /// reserved for <see cref="VerifySavedData"/>, where the wait is asked for.
    /// </remarks>
    [InitializeOnLoad]
    internal static class SerializationWatcher
    {
        static SerializationWatcher()
        {
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        private static void OnSceneSaved(Scene scene)
        {
            if (!GazeInspectorSettings.instance.ValidateSerialization)
                return;

            Report(SerializationDrift.CheckTouched(scene), scene);
        }

        private static int Report(List<(Object target, List<string> missing)> results, Scene scene)
        {
            foreach (var (target, missing) in results)
                Debug.LogWarning(
                    $"[Gaze] '{target.name}' ({target.GetType().Name}) was not written to '{scene.path}'.\n" +
                    $"Unity saved the scene and skipped: {string.Join(", ", missing)}\n\n" +
                    "Edits to this component are being lost. Select it and use Rebuild Component on the " +
                    "error box's right-click menu.",
                    target);

            return results.Count;
        }

        /// <summary>
        /// Saves the open scenes, then sweeps every component in them. Driven from the settings window
        /// rather than a menu item, so the top-level menu stays down to the one entry newcomers need.
        /// </summary>
        internal static void VerifySavedData()
        {
            // The check compares against what is on disk, so the disk has to be current first.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            SerializationDrift.Forget();

            var drifting = 0;
            var missingTypes = 0;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (!scene.isLoaded)
                    continue;

                drifting += Report(SerializationDrift.CheckScene(scene), scene);
                missingTypes += ReportMissingTypes(scene);
            }

            EditorUtility.DisplayDialog("Gaze", Summarise(drifting, missingTypes), "OK");
        }

        /// <summary>
        /// Reports objects holding <c>[SerializeReference]</c> records whose type has been deleted, as Unity
        /// itself sees them. Cheap enough to sweep, and it needs no knowledge of the file format.
        /// </summary>
        private static int ReportMissingTypes(Scene scene)
        {
            var found = 0;

            foreach (var root in scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                    continue;

                // Built-in components cannot hold managed references, and asking about one logs an error
                // rather than answering false.
                if (component is not MonoBehaviour behaviour)
                    continue;

                if (!SerializationUtility.HasManagedReferencesWithMissingTypes(behaviour))
                    continue;

                var entries = SerializationUtility.GetManagedReferencesWithMissingTypes(behaviour);

                if (entries == null || entries.Length == 0)
                    continue;

                var names = new HashSet<string>();

                foreach (var entry in entries)
                {
                    var fullName = string.IsNullOrEmpty(entry.namespaceName)
                        ? entry.className
                        : $"{entry.namespaceName}.{entry.className}";

                    names.Add($"{fullName} ({entry.assemblyName})");
                }

                Debug.LogWarning(
                    $"[Gaze] '{component.name}' ({component.GetType().Name}) holds SerializeReference " +
                    $"records whose types no longer exist:\n  {string.Join("\n  ", names)}\n\n" +
                    "Rename the type back, or add [MovedFrom] to its replacement, to recover the values. " +
                    "To drop them, use Clear Missing Records on the error box's right-click menu.",
                    component);

                found++;
            }

            return found;
        }

        private static string Summarise(int drifting, int missingTypes)
        {
            if (drifting == 0 && missingTypes == 0)
                return "Everything in the loaded scenes wrote its fields to disk, and no SerializeReference " +
                       "record points at a type that is gone.";

            var lines = new List<string>();

            if (drifting > 0)
                lines.Add($"{drifting} component(s) did not write their fields to disk. Select one and use " +
                          "Rebuild Component on the error box's right-click menu.");

            if (missingTypes > 0)
                lines.Add($"{missingTypes} object(s) hold SerializeReference records whose type is gone.");

            lines.Add("Details are in the console.");
            return string.Join("\n\n", lines);
        }
    }
}
