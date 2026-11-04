using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// Appends Unity's prefab-override menu items (Apply to Prefab / Revert) to a hand-built context
    /// menu, so a property drawn by Gaze offers the same affordance Unity's own controls do.
    /// </summary>
    /// <remarks>
    /// Unity's own controls get this from <c>EditorGUI.EndProperty</c> internally calling
    /// <c>EditorGUI.FillPropertyContextMenu</c>, but that method is internal, has moved shape across
    /// Editor versions, and returns a <see cref="GenericMenu"/> rather than mutating one in place - a
    /// menu carries no public API to read items back out of, so there is no reliable way to merge its
    /// result into a menu already under construction. The public, documented surface for the same
    /// operation is <see cref="PrefabUtility"/>, which is what this builds on instead.
    /// </remarks>
    internal static class UnityPropertyMenu
    {
        internal static void AddUnityItems(GenericMenu menu, SerializedProperty property)
        {
            if (menu == null || property == null)
                return;

            var target = property.serializedObject.targetObject;
            if (target == null || !PrefabUtility.IsPartOfPrefabInstance(target))
                return;

            menu.AddSeparator("");

            if (property.prefabOverride)
            {
                menu.AddItem(new GUIContent("Revert"), false, () => Revert(property));
                menu.AddItem(new GUIContent("Apply to Prefab"), false, () => Apply(property));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Revert"), false);
                menu.AddDisabledItem(new GUIContent("Apply to Prefab"), false);
            }
        }

        private static void Revert(SerializedProperty property)
        {
            PrefabUtility.RevertPropertyOverride(property, InteractionMode.UserAction);
            property.serializedObject.Update();
        }

        private static void Apply(SerializedProperty property)
        {
            var target = property.serializedObject.targetObject;
            var source = PrefabUtility.GetCorrespondingObjectFromSource(target);
            var assetPath = source != null ? AssetDatabase.GetAssetPath(source) : null;

            if (string.IsNullOrEmpty(assetPath))
                return;

            PrefabUtility.ApplyPropertyOverride(property, assetPath, InteractionMode.UserAction);
            property.serializedObject.Update();
        }
    }
}
