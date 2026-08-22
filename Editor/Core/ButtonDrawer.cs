using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal static class ButtonDrawer
    {
        public static void Draw(SerializedObject so)
        {
            var buttons = ButtonCache.GetButtons(so.targetObject.GetType());

            foreach (var (method, attribute) in buttons)
            {
                var label = string.IsNullOrEmpty(attribute.Label)
                    ? ObjectNames.NicifyVariableName(method.Name)
                    : attribute.Label;

                if (GUILayout.Button(label))
                    Invoke(so, method);
            }
        }

        private static void Invoke(SerializedObject so, MethodInfo method)
        {
            so.ApplyModifiedProperties();

            foreach (var target in so.targetObjects)
            {
                Undo.RecordObject(target, $"Invoke {method.Name}");

                try
                {
                    method.Invoke(target, null);
                }
                catch (Exception e)
                {
                    Debug.LogException(new Exception($"[Gaze] {nameof(ButtonDrawer)} | " +
                        $"'{method.Name}' threw. Details: {e}"), target);
                }

                EditorUtility.SetDirty(target);
            }
        }
    }
}
