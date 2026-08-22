using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// The serialization problem boxes drawn above an object's properties, and the recovery actions on
    /// their right-click menu.
    /// </summary>
    /// <remarks>
    /// Two things get reported: SerializeReference records whose type is gone, and a component whose fields
    /// are not reaching its file at all. Neither costs anything per frame beyond what Unity's own
    /// missing-type check costs, and the drift result is precomputed by <see cref="SerializationWatcher"/>.
    /// </remarks>
    internal sealed class SerializationMessages
    {
        private readonly List<ManagedReferenceMissingType> _entries = new();
        private readonly List<ManagedReferenceMissingType> _distinct = new();
        private readonly Dictionary<string, int> _counts = new();
        private readonly List<Object> _targets = new();

        private string _text;
        private Rect _rect;

        private string _driftText;
        private Rect _driftRect;

        private Object _primaryTarget;
        private Rect _lastHitArea;

        internal void Refresh(SerializedObject serializedObject)
        {
            _entries.Clear();
            _distinct.Clear();
            _counts.Clear();
            _targets.Clear();
            _text = null;
            _driftText = null;
            _primaryTarget = null;

            if (serializedObject == null)
                return;

            _primaryTarget = serializedObject.targetObject;

            foreach (var target in serializedObject.targetObjects)
            {
                _targets.Add(target);
                Collect(target);
            }

            if (GazeInspectorSettings.instance.ValidateSerialization &&
                SerializationDrift.IsDrifting(_primaryTarget))
                _driftText = ComposeDrift(SerializationDrift.MissingFields(_primaryTarget));

            if (_entries.Count > 0)
                _text = Compose();
        }

        internal void Draw()
        {
            if (_text != null)
            {
                var rect = GazeMessages.DrawBox(new GazeMessage(InfoMessageType.Error, _text));

                if (Event.current.type == EventType.Repaint)
                    _rect = rect;
            }

            if (_driftText == null)
                return;

            var driftRect = GazeMessages.DrawBox(new GazeMessage(InfoMessageType.Error, _driftText));

            if (Event.current.type == EventType.Repaint)
                _driftRect = driftRect;
        }

        internal bool Contains(Vector2 position)
        {
            if (_text != null && _rect.Contains(position))
            {
                _lastHitArea = _rect;
                return true;
            }

            if (_driftText != null && _driftRect.Contains(position))
            {
                _lastHitArea = _driftRect;
                return true;
            }

            return false;
        }

        internal Rect Area => _lastHitArea;

        internal void Fill(GenericMenu menu)
        {
            var hasEntries = _entries.Count > 0;

            if (hasEntries)
            {
                menu.AddItem(new GUIContent("Copy Type Name"), false,
                    () => EditorGUIUtility.systemCopyBuffer = TypeNames());
                menu.AddItem(new GUIContent("Copy Serialized Data"), false,
                    () => EditorGUIUtility.systemCopyBuffer = SerializedData());
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Copy Type Name"));
                menu.AddDisabledItem(new GUIContent("Copy Serialized Data"));
            }

            menu.AddSeparator("");

            if (hasEntries)
                menu.AddItem(new GUIContent("Clear Missing Records"), false, ClearMissingRecords);
            else
                menu.AddDisabledItem(new GUIContent("Clear Missing Records"));

            var rebuildable = _primaryTarget as Component;

            if (rebuildable != null && ComponentRebuilder.CanRebuild(rebuildable, out _))
                menu.AddItem(new GUIContent("Rebuild Component..."), false,
                    () => ComponentRebuilder.RebuildWithPrompt(rebuildable));
            else
                menu.AddDisabledItem(new GUIContent("Rebuild Component..."));
        }

        private void Collect(Object target)
        {
            if (target is not MonoBehaviour and not ScriptableObject)
                return;

            if (!SerializationUtility.HasManagedReferencesWithMissingTypes(target))
                return;

            var missingTypes = SerializationUtility.GetManagedReferencesWithMissingTypes(target);

            if (missingTypes == null)
                return;

            foreach (var missingType in missingTypes)
            {
                _entries.Add(missingType);

                var key = Display(missingType);

                if (_counts.TryGetValue(key, out var count))
                {
                    _counts[key] = count + 1;
                    continue;
                }

                _counts[key] = 1;
                _distinct.Add(missingType);
            }
        }

        private void ClearMissingRecords()
        {
            var confirmed = EditorUtility.DisplayDialog(
                "Clear missing records",
                "The SerializeReference records whose types are gone will be removed from this object.\n\n" +
                "Their data is unreadable while the types do not exist, but it is still in the file until " +
                "this runs. Rename the type back, or add [MovedFrom], first if you want the values.\n\n" +
                "This cannot be undone.",
                "Clear",
                "Cancel");

            if (!confirmed)
                return;

            var cleared = 0;

            foreach (var target in _targets)
            {
                if (target == null)
                    continue;

                // Same guard as Collect: asking anything else logs an error rather than answering false.
                if (target is not MonoBehaviour and not ScriptableObject)
                    continue;

                if (!SerializationUtility.HasManagedReferencesWithMissingTypes(target))
                    continue;

                if (!SerializationUtility.ClearAllManagedReferencesWithMissingTypes(target))
                    continue;

                EditorUtility.SetDirty(target);
                MarkOwningSceneDirty(target);
                cleared++;
            }

            if (cleared == 0)
            {
                Debug.LogWarning($"[Gaze] {nameof(SerializationMessages)} | Unity reported nothing to clear.");
                return;
            }

            Debug.Log($"[Gaze] {nameof(SerializationMessages)} | Cleared missing records on {cleared} " +
                      "object(s). Save to write the change to disk.");
        }

        private static void MarkOwningSceneDirty(Object target)
        {
            var component = target as Component;
            var gameObject = component != null ? component.gameObject : target as GameObject;

            if (gameObject == null)
                return;

            var scene = gameObject.scene;

            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
        }

        private string Compose()
        {
            var builder = new StringBuilder();

            builder.AppendLine(_distinct.Count == 1
                ? "1 missing SerializeReference type:"
                : $"{_distinct.Count} missing SerializeReference types:");

            foreach (var entry in _distinct)
            {
                var key = Display(entry);
                var count = _counts[key];

                builder.AppendLine(count > 1 ? $"  {key} ×{count}" : $"  {key}");
            }

            builder.AppendLine();
            builder.AppendLine("The data is still saved. Rename the type back, or add [MovedFrom] to the " +
                               "renamed type to migrate it.");
            builder.AppendLine("Records with no field pointing at them clear when the object is saved and " +
                               "reloaded.");
            builder.Append("Gaze cannot mark the individual fields: SerializedProperty reports a missing " +
                           "reference identically to an empty one.");

            return builder.ToString();
        }

        private static string ComposeDrift(IReadOnlyList<string> missing)
        {
            var builder = new StringBuilder();

            builder.AppendLine("This component is not being saved.");
            builder.AppendLine();

            // Name the actor. A user who just installed an inspector will otherwise assume the inspector
            // did this, and the passive voice invites exactly that.
            builder.AppendLine("Unity wrote the scene and skipped these fields:");

            foreach (var field in missing)
                builder.AppendLine($"  {field}");

            builder.AppendLine();
            builder.Append("Anything you change here will be lost. Right-click and choose Rebuild Component " +
                           "to replace it with a working one, keeping its values and every reference to it.");

            return builder.ToString();
        }

        private string TypeNames()
        {
            var builder = new StringBuilder();

            foreach (var entry in _distinct)
                builder.AppendLine(TypeName(entry));

            return builder.ToString();
        }

        private string SerializedData()
        {
            var builder = new StringBuilder();

            foreach (var entry in _entries)
            {
                builder.AppendLine(entry.serializedData);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string Display(ManagedReferenceMissingType entry) => $"{FullName(entry)} ({entry.assemblyName})";

        private static string TypeName(ManagedReferenceMissingType entry) => $"{FullName(entry)}, {entry.assemblyName}";

        private static string FullName(ManagedReferenceMissingType entry) =>
            string.IsNullOrEmpty(entry.namespaceName) ? entry.className : $"{entry.namespaceName}.{entry.className}";
    }
}
