using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// Replaces a component that has stopped writing itself to disk with a fresh one carrying the same
    /// values, and repoints everything that referenced the old one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deleting and re-adding a component by hand is the usual advice for this, and it quietly breaks every
    /// reference to it: the replacement gets a new file id, so each field pointing at the old component
    /// reads as None afterwards. This collects those references first and restores them, so the operation
    /// is invisible apart from the component starting to save again.
    /// </para>
    /// <para>
    /// For a component in a scene the reference surface is exactly the loaded scenes, because Unity does
    /// not allow an asset to reference a scene object. That makes the repoint complete rather than
    /// best-effort. Prefab assets have a wider surface (other prefabs, and every instance in every scene),
    /// so they are refused rather than half-handled.
    /// </para>
    /// </remarks>
    internal static class ComponentRebuilder
    {
        internal static bool RebuildWithPrompt(Component component)
        {
            if (!CanRebuild(component, out var reason))
            {
                EditorUtility.DisplayDialog("Gaze", reason, "OK");
                return false;
            }

            var referrers = FindReferrers(component);

            var confirmed = EditorUtility.DisplayDialog(
                "Rebuild component",
                $"{component.GetType().Name} on '{component.gameObject.name}' will be replaced with a fresh " +
                "component holding the same values.\n\n" +
                $"{referrers.Count} reference(s) to it across the loaded scenes will be repointed at the " +
                "replacement, so nothing turns into a missing reference.\n\n" +
                "The component keeps its position in the inspector, and the whole thing is one undo step.",
                "Rebuild",
                "Cancel");

            if (!confirmed)
                return false;

            if (Rebuild(component, referrers, out var error))
                return true;

            Debug.LogError($"[Gaze] {nameof(ComponentRebuilder)} | {error}");
            EditorUtility.DisplayDialog("Gaze", $"Rebuild failed.\n\n{error}", "OK");
            return false;
        }

        internal static bool CanRebuild(Component component, out string reason)
        {
            reason = null;

            if (component == null)
            {
                reason = "No component to rebuild.";
                return false;
            }

            if (component is Transform)
            {
                reason = "A Transform cannot be removed, so it cannot be rebuilt.";
                return false;
            }

            if (EditorUtility.IsPersistent(component))
            {
                reason = "This component lives in an asset. Rebuilding it would have to repoint references " +
                         "from other assets and from every instance in every scene, which this cannot do " +
                         "safely. Rebuild it on a scene instance instead.";
                return false;
            }

            if (PrefabUtility.GetPrefabInstanceStatus(component) != PrefabInstanceStatus.NotAPrefab)
            {
                reason = "This component belongs to a prefab instance. Rebuilding it would break the link to " +
                         "the prefab. Open the prefab and fix it there, or unpack the instance first.";
                return false;
            }

            if (!component.gameObject.scene.IsValid())
            {
                reason = "This component is not in a scene, so its references cannot be found.";
                return false;
            }

            return true;
        }

        private static bool Rebuild(
            Component component,
            List<(Object owner, string path)> referrers,
            out string error)
        {
            error = null;

            var gameObject = component.gameObject;
            var type = component.GetType();
            var scene = gameObject.scene;
            var oldIndex = Array.IndexOf(gameObject.GetComponents<Component>(), component);

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Rebuild Component");
            var group = Undo.GetCurrentGroup();

            try
            {
                var replacement = Undo.AddComponent(gameObject, type);

                if (replacement == null)
                {
                    error = $"Could not add a replacement {type.Name}.";
                    return false;
                }

                // Copies every serialized field, including managed references, off the live object. The live
                // object is the healthy side here; the file is the stale one.
                EditorUtility.CopySerialized(component, replacement);

                var repointed = 0;

                foreach (var (owner, path) in referrers)
                {
                    // A field on the old component that pointed at itself was just copied across, so it has
                    // to be fixed up on the replacement rather than on the object about to be destroyed.
                    var target = owner == (Object) component ? replacement : owner;

                    if (target == null)
                        continue;

                    using var serializedObject = new SerializedObject(target);
                    var property = serializedObject.FindProperty(path);

                    if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                        continue;

                    property.objectReferenceValue = replacement;
                    serializedObject.ApplyModifiedProperties();
                    repointed++;
                }

                Undo.DestroyObjectImmediate(component);

                if (oldIndex >= 0)
                {
                    var current = Array.IndexOf(gameObject.GetComponents<Component>(), replacement);

                    while (current > oldIndex && ComponentUtility.MoveComponentUp(replacement))
                        current--;
                }

                EditorUtility.SetDirty(replacement);

                if (scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(scene);

                SerializationDrift.Forget(replacement);

                Debug.Log($"[Gaze] {nameof(ComponentRebuilder)} | Rebuilt {type.Name} on '{gameObject.name}'. " +
                          $"{repointed} reference(s) repointed. Save the scene to write it out.", replacement);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
            finally
            {
                Undo.CollapseUndoOperations(group);
            }
        }

        /// <summary>
        /// Every serialized object reference pointing at <paramref name="component"/> anywhere in the loaded
        /// scenes, as (owning object, property path) pairs.
        /// </summary>
        private static List<(Object owner, string path)> FindReferrers(Component component)
        {
            var referrers = new List<(Object, string)>();
            var scenes = new List<Scene>();

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (scene.isLoaded)
                    scenes.Add(scene);
            }

            var owners = new List<Component>();

            foreach (var scene in scenes)
            foreach (var root in scene.GetRootGameObjects())
                owners.AddRange(root.GetComponentsInChildren<Component>(true));

            try
            {
                for (var i = 0; i < owners.Count; i++)
                {
                    var owner = owners[i];

                    if (owner == null)
                        continue;

                    if (i % 32 == 0 && EditorUtility.DisplayCancelableProgressBar(
                            "Gaze", $"Finding references ({i + 1}/{owners.Count})", (i + 1f) / owners.Count))
                        break;

                    using var serializedObject = new SerializedObject(owner);
                    var iterator = serializedObject.GetIterator();

                    if (!iterator.Next(true))
                        continue;

                    do
                    {
                        if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                            continue;

                        if (iterator.objectReferenceValue != (Object) component)
                            continue;

                        referrers.Add((owner, iterator.propertyPath));
                    }
                    while (iterator.Next(true));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return referrers;
        }
    }
}
