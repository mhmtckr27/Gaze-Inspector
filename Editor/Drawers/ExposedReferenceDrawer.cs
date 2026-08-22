using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal sealed class ExposedReferenceDrawer : PropertyDrawerBase
    {
        private float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight; // single line; expand if you want extra status lines
        }

        public override void Draw(GUIContent label)
        {
            PropertyTreeGUI.BeginHorizontalLayout(label, out _);
            
            var position = EditorGUILayout.GetControlRect(label != null, GetHeight());
            // Label split

            // Resolver context (PlayableDirector or any IExposedPropertyTable)
            var resolver = Property.serializedObject.context as IExposedPropertyTable;

            // Figure out the T for ExposedReference<T> so we can type-filter the picker
            var fieldType = GetExposedGenericArgument(Property); // see helper below; can be null

            // Mixed value handling
            bool prevMixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = Property.hasMultipleDifferentValues;

            // Draw
            EditorGUI.BeginChangeCheck();
            if (resolver != null)
            {
                // Bound mode: edit the resolved reference
                var current = Property.exposedReferenceValue; // resolved via resolver
                var next = EditorGUI.ObjectField(position, current, fieldType ?? typeof(Object), /*allowSceneObjects:*/
                    true);
                if (EditorGUI.EndChangeCheck())
                    Property.exposedReferenceValue = next; // sets/clears the binding on the resolver
            }
            else
            {
                // No resolver: edit defaultValue (asset reference)
                var defaultProp = Property.FindPropertyRelative("defaultValue"); // Unity exposes this child
                var next = EditorGUI.ObjectField(position, defaultProp.objectReferenceValue, fieldType ?? typeof(Object), /*allowSceneObjects:*/ false);
                if (EditorGUI.EndChangeCheck())
                    defaultProp.objectReferenceValue = next;
            }

            EditorGUI.showMixedValue = prevMixed;
            
            PropertyTreeGUI.EndHorizontalLayout();
        }

        // --- helpers ---

        // Try to get T from ExposedReference<T> via reflection on the field the Property points to.
        static System.Type GetExposedGenericArgument(SerializedProperty p)
        {
            // If you already have a field cache in your framework, use that.
            // Minimal fallback using Property.type string is unreliable. Prefer reflection cache.
            var t = ReflectionCache.GetFieldType(p);
            if (t.IsGenericType && t.GetGenericTypeDefinition().Name == "ExposedReference`1")
                return t.GetGenericArguments()[0];
            return null;
        }
    }
}