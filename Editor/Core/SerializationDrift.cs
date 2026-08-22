using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// Detects a component whose live serialized fields no longer match what is stored for it in its file,
    /// which means edits to that component are not reaching disk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is deliberately cause-agnostic. A component can end up detached from its serialized form for
    /// reasons that leave every Unity API reporting health: the right type resolves, no managed references
    /// are missing, the scene saves, every other object in it writes correctly, and that one component is
    /// still written from a stale field set. Comparing the two representations catches it without needing
    /// to know why.
    /// </para>
    /// <para>
    /// The check only runs after a save. Before one, a field added to a script is legitimately present on
    /// the object and absent from the file, and reporting that would fire on every script edit. Surviving a
    /// save is what makes it a real failure: Unity rewrote the file and still did not write the field.
    /// </para>
    /// <para>
    /// After a save it checks only what was edited since the last one. Per component the comparison costs a
    /// <c>GlobalObjectId.GetGlobalObjectIdSlow</c> and a <c>SerializedObject</c>, which is nothing for a
    /// handful of objects and real money across a populated scene. Sweeping everything is left to
    /// <see cref="CheckScene"/>, behind an explicit menu item.
    /// </para>
    /// </remarks>
    internal static class SerializationDrift
    {
        // "--- !u!114 &15503831"
        private static readonly Regex s_DocumentLine =
            new(@"^---\s+!u!\d+\s+&(?<id>-?\d+)", RegexOptions.Compiled);

        // A top-level key of a document, at exactly two spaces of indent. Excludes "  - rid: 1" list items.
        private static readonly Regex s_TopLevelKey =
            new(@"^  (?<key>[A-Za-z_][A-Za-z0-9_]*):", RegexOptions.Compiled);

        // Serialized by Unity itself rather than by the user's script, so they are not drift.
        private static readonly HashSet<string> s_EngineKeys = new()
        {
            "m_ObjectHideFlags",
            "m_CorrespondingSourceObject",
            "m_PrefabInstance",
            "m_PrefabAsset",
            "m_GameObject",
            "m_Enabled",
            "m_EditorHideFlags",
            "m_Script",
            "m_Name",
            "m_EditorClassIdentifier",
            "serializedVersion",
            "references"
        };

        private const long MaxScannedFileBytes = 64L * 1024L * 1024L;

        // Edited through the Inspector since the last save. Instance ids rather than references, so a
        // destroyed object simply fails to resolve.
        private static readonly HashSet<ulong> s_Touched = new();

        // Confirmed after a save, so the inspector can report without re-reading anything per frame.
        private static readonly Dictionary<ulong, List<string>> s_Drifting = new();

        /// <summary>
        /// Notes that an edit was applied to <paramref name="serializedObject"/>, so the next save knows
        /// which handful of components are worth comparing against the file.
        /// </summary>
        internal static void Touched(SerializedObject serializedObject)
        {
            if (serializedObject == null)
                return;

            foreach (var target in serializedObject.targetObjects)
                if (target != null)
                    s_Touched.Add(ObjectId.Of(target));
        }

        internal static void Forget()
        {
            s_Touched.Clear();
            s_Drifting.Clear();
        }

        internal static void Forget(Object target)
        {
            if (target == null)
                return;

            s_Drifting.Remove(ObjectId.Of(target));
            s_Touched.Remove(ObjectId.Of(target));
        }

        /// <summary>True when <paramref name="target"/> survived a save without its fields being written.</summary>
        internal static bool IsDrifting(Object target)
        {
            return target != null && s_Drifting.ContainsKey(ObjectId.Of(target));
        }

        /// <summary>Field names present on the live object but missing from its file. Empty when not drifting.</summary>
        internal static IReadOnlyList<string> MissingFields(Object target)
        {
            return target != null && s_Drifting.TryGetValue(ObjectId.Of(target), out var missing)
                ? missing
                : Array.Empty<string>();
        }

        /// <summary>
        /// Checks only the components edited since the last save. This is the automatic path, so its cost
        /// scales with how much was changed rather than with the size of the scene.
        /// </summary>
        internal static List<(Object target, List<string> missing)> CheckTouched(Scene scene)
        {
            var results = new List<(Object, List<string>)>();

            if (s_Touched.Count == 0 || !IsCheckable(scene))
                return results;

            var keysByOwner = ReadKeys(scene.path);

            if (keysByOwner == null)
                return results;

            // Copied, because a component rebuilt in response to a finding mutates the set.
            var touched = new List<ulong>(s_Touched);
            s_Touched.Clear();

            foreach (var instanceId in touched)
            {
                var target = ObjectId.ToObject(instanceId);

                if (!Qualifies(target, scene))
                    continue;

                Evaluate(target, keysByOwner, results);
            }

            return results;
        }

        /// <summary>
        /// Checks every component in <paramref name="scene"/>. Only for the explicit menu item: this is the
        /// expensive one, and it reports progress so it can be cancelled.
        /// </summary>
        internal static List<(Object target, List<string> missing)> CheckScene(Scene scene)
        {
            var results = new List<(Object, List<string>)>();

            if (!IsCheckable(scene))
                return results;

            var keysByOwner = ReadKeys(scene.path);

            if (keysByOwner == null)
                return results;

            var candidates = new List<Object>();

            foreach (var root in scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<Component>(true))
                if (Qualifies(component, scene))
                    candidates.Add(component);

            try
            {
                for (var i = 0; i < candidates.Count; i++)
                {
                    if (i % 64 == 0 && EditorUtility.DisplayCancelableProgressBar(
                            "Gaze", $"Checking saved data ({i + 1}/{candidates.Count})", (i + 1f) / candidates.Count))
                        break;

                    Evaluate(candidates[i], keysByOwner, results);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return results;
        }

        private static bool IsCheckable(Scene scene)
        {
            return scene.IsValid() && scene.isLoaded && !string.IsNullOrEmpty(scene.path);
        }

        private static bool Qualifies(Object target, Scene scene)
        {
            if (target is not MonoBehaviour behaviour)
                return false;

            if (behaviour.gameObject.scene != scene)
                return false;

            // A prefab instance stores overrides rather than fields, so there is nothing to compare.
            return PrefabUtility.GetPrefabInstanceStatus(behaviour) == PrefabInstanceStatus.NotAPrefab;
        }

        private static void Evaluate(
            Object target,
            Dictionary<ulong, HashSet<string>> keysByOwner,
            List<(Object, List<string>)> results)
        {
            var missing = MissingFor(target, keysByOwner);

            if (missing.Count == 0)
            {
                s_Drifting.Remove(ObjectId.Of(target));
                return;
            }

            s_Drifting[ObjectId.Of(target)] = missing;
            results.Add((target, missing));
        }

        private static List<string> MissingFor(Object target, Dictionary<ulong, HashSet<string>> keysByOwner)
        {
            var missing = new List<string>();
            var ownerId = GlobalObjectId.GetGlobalObjectIdSlow(target).targetObjectId;

            if (!keysByOwner.TryGetValue(ownerId, out var stored))
                return missing;

            using var serializedObject = new SerializedObject(target);

            var iterator = serializedObject.GetIterator();

            // Next rather than NextVisible, so a [HideInInspector] field still counts.
            if (!iterator.Next(true))
                return missing;

            do
            {
                if (iterator.depth != 0)
                    continue;

                var path = iterator.propertyPath;

                if (s_EngineKeys.Contains(path) || stored.Contains(path))
                    continue;

                missing.Add(path);
            }
            while (iterator.Next(false));

            return missing;
        }

        private static Dictionary<ulong, HashSet<string>> ReadKeys(string assetPath)
        {
            string absolutePath;

            try
            {
                absolutePath = PackagePaths.ToAbsolutePath(assetPath);

                if (!File.Exists(absolutePath) || new FileInfo(absolutePath).Length > MaxScannedFileBytes)
                    return null;
            }
            catch (Exception)
            {
                return null;
            }

            var keysByOwner = new Dictionary<ulong, HashSet<string>>();

            try
            {
                using var reader = new StreamReader(absolutePath);

                HashSet<string> current = null;
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    var documentMatch = s_DocumentLine.Match(line);

                    if (documentMatch.Success)
                    {
                        current = new HashSet<string>();
                        keysByOwner[ParseFileId(documentMatch.Groups["id"].Value)] = current;
                        continue;
                    }

                    if (current == null)
                        continue;

                    var keyMatch = s_TopLevelKey.Match(line);

                    if (keyMatch.Success)
                        current.Add(keyMatch.Groups["key"].Value);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Gaze] {nameof(SerializationDrift)} | Could not read '{assetPath}'. " +
                                 $"Details: {e.Message}");
                return null;
            }

            return keysByOwner;
        }

        private static ulong ParseFileId(string text)
        {
            if (ulong.TryParse(text, out var unsigned))
                return unsigned;

            return long.TryParse(text, out var signed) ? unchecked((ulong) signed) : 0UL;
        }
    }
}
