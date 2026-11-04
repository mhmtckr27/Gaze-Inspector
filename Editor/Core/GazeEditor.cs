using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// The fallback inspector that draws every object Gaze is responsible for.
    /// </summary>
    /// <remarks>
    /// Registered as a fallback editor, so any type with its own <c>[CustomEditor]</c> keeps that editor
    /// and is unaffected. Turning Gaze off in its settings makes this fall through to Unity's default
    /// inspector, which is also what happens per-property wherever a custom drawer is involved.
    /// </remarks>
    [CustomEditor(typeof(Object), true, isFallback = true)]
    [CanEditMultipleObjects]
    public class GazeEditor : UnityEditor.Editor
    {
        private PropertyTree _tree;

        private void OnEnable()
        {
            _tree = null;
        }

        /// <summary>
        /// Builds the property tree if needed, then draws it along with any
        /// <see cref="ButtonAttribute"/> buttons on the target.
        /// </summary>
        public override void OnInspectorGUI()
        {
            var settings = GazeInspectorSettings.instance;

            if (!settings.Enabled || serializedObject.targetObject == null)
            {
                DrawDefaultInspector();
                return;
            }

            // Not in OnEnable: Unity's own PropertyDrawer constructors resolve named GUIStyles, which
            // needs a current skin.
            if (_tree is not { HasContent: true })
            {
                // Drawers populate their per-pass layout state (page indices, row rects, column widths)
                // on Layout. A tree built during Repaint has none of it yet and draws collection headers
                // with no rows, so wait for a Layout pass rather than drawing a half-initialised tree.
                if (Event.current.type != EventType.Layout)
                {
                    Repaint();
                    return;
                }

                _tree = new PropertyTree(serializedObject);
            }

            serializedObject.Update();

            PropertyTreeGUI.DrawTree(_tree);

            ButtonDrawer.Draw(serializedObject);

            OverlayDrawer.Flush();

            // Noting what was actually edited keeps the after-save check proportional to the change rather
            // than to the size of the scene.
            if (serializedObject.ApplyModifiedProperties())
                SerializationDrift.Touched(serializedObject);

            this.TryRepaint();
        }
    }
}