using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    // Project-wide Gaze settings, stored under ProjectSettings/ (survives domain
    // reloads and Editor restarts, and is naturally shareable via version control with the team).
    [FilePath("ProjectSettings/GazeInspectorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class GazeInspectorSettings : ScriptableSingleton<GazeInspectorSettings>
    {
        internal static readonly int DefaultPageSize = 15;
        internal static readonly int DefaultDictionaryFoldoutThreshold = 3;

        [SerializeField] private bool enabled = true;
        [SerializeField] private int pageSize = DefaultPageSize;
        [SerializeField] private int dictionaryFoldoutThreshold = DefaultDictionaryFoldoutThreshold;
        [SerializeField] private bool validateSerialization = true;
        [SerializeField] private bool welcomeShown;

        /// <summary>
        /// Whether this project has already had the settings window opened for it once, on the
        /// first domain reload after Gaze was added. Deliberately left out of
        /// <see cref="ResetToDefaults"/>: resetting preferences should not make the window
        /// reappear, since it is a record of something that happened rather than a preference.
        /// </summary>
        internal bool WelcomeShown
        {
            get => welcomeShown;
            set => Set(ref welcomeShown, value);
        }

        internal bool Enabled
        {
            get => enabled;
            set => Set(ref enabled, value);
        }
        
        internal int PageSize
        {
            get => pageSize;
            set => Set(ref pageSize, Mathf.Max(1, value));
        }

        internal int DictionaryFoldoutThreshold
        {
            get => dictionaryFoldoutThreshold;
            set => Set(ref dictionaryFoldoutThreshold, Mathf.Max(0, value));
        }

        /// <summary>
        /// Checks scenes and assets for SerializeReference records whose types are gone, on script reload
        /// and while drawing the inspector. Turning this off silences the reports and the repair entries.
        /// </summary>
        internal bool ValidateSerialization
        {
            get => validateSerialization;
            set => Set(ref validateSerialization, value);
        }

        internal void ResetToDefaults()
        {
            enabled = true;
            pageSize = DefaultPageSize;
            dictionaryFoldoutThreshold = DefaultDictionaryFoldoutThreshold;
            validateSerialization = true;
            Persist();
        }

        private void Set<T>(ref T field, T value)
        {
            if (Equals(field, value)) return;
            field = value;
            Persist();
        }

        private void Persist() => Save(true);
    }
}
