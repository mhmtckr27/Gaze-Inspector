using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class OnValueChangedDecorator : AttributeDrawer<OnValueChangedAttribute>
    {
        private const BindingFlags MemberFlags =
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        private static readonly Dictionary<(Type Type, string Method), MethodInfo> MethodCache = new();

        private OnValueChangedAttribute _attribute;
        private FieldInfo _fieldInfo;
        private readonly Dictionary<UnityEngine.Object, object> _oldValueByTarget = new();

        protected internal override void Initialize()
        {
            _attribute = Attribute as OnValueChangedAttribute;
            _fieldInfo = ReflectionCache.GetFieldInfoAndAttrs(Property).field;
            CacheOldValues();
        }

        protected internal override void CollectMessages(List<GazeMessage> messages)
        {
            if (_fieldInfo != null && !TryResolveMethod(out var error))
                messages.Add(new GazeMessage(InfoMessageType.Error, error));

            base.CollectMessages(messages);
        }

        public override void Draw(GUIContent label)
        {
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                Inner.Draw(label);

                if (scope.changed)
                {
                    ValueChanged();
                }
            }

            CacheOldValues();
        }

        private void CacheOldValues()
        {
            _oldValueByTarget.Clear();

            if (_fieldInfo == null)
                return;

            foreach (var target in so.targetObjects)
            {
                var owner = GetOwningObject(target, Property.propertyPath) ?? target;
                _oldValueByTarget[target] = _fieldInfo.GetValue(owner);
            }
        }

        private void ValueChanged()
        {
            if (_fieldInfo == null || string.IsNullOrEmpty(_attribute.FunctionName))
                return;

            // Values written through EditorGUI only land on the live objects once applied;
            // the callback must see the post-edit value, not the buffered SerializedProperty one.
            so.ApplyModifiedProperties();

            var propertyPath = Property.propertyPath;

            foreach (var target in so.targetObjects)
            {
                var owner = GetOwningObject(target, propertyPath) ?? target;
                var method = ResolveMethod(owner.GetType(), _attribute.FunctionName);

                if (method == null)
                    continue;

                _oldValueByTarget.TryGetValue(target, out var oldValue);
                var newValue = _fieldInfo.GetValue(owner);
                var args = method.GetParameters().Length == 2 ? new[] { oldValue, newValue } : null;

                Undo.RecordObject(target, $"Change {Property.displayName}");

                try
                {
                    method.Invoke(method.IsStatic ? null : owner, args);
                }
                catch (Exception e)
                {
                    Debug.LogException(new Exception($"[Gaze] {nameof(OnValueChangedDecorator)} | " +
                        $"'{_attribute.FunctionName}' threw. Details: {e}"), target);
                }
            }
        }

        private bool TryResolveMethod(out string error)
        {
            error = null;

            var owner = GetOwningObject(so.targetObject, Property.propertyPath) ?? so.targetObject;

            if (ResolveMethod(owner.GetType(), _attribute.FunctionName) != null)
                return true;

            error = $"Could not resolve '{_attribute.FunctionName}' on {owner.GetType().Name} or in parents. " +
                    "Expected a method taking zero parameters, or two parameters (old value, new value).";
            return false;
        }

        /// <summary>
        /// Walks the base type chain manually (private members on base types are not
        /// surfaced by FlattenHierarchy) looking for a method with zero or two parameters.
        /// Stops before entering Unity's own assembly so a typo can't silently resolve to
        /// something on MonoBehaviour/Component/ScriptableObject/Object.
        /// </summary>
        private static MethodInfo ResolveMethod(Type type, string methodName)
        {
            var key = (type, methodName);

            if (MethodCache.TryGetValue(key, out var cached))
                return cached;

            MethodInfo found = null;

            for (var t = type;
                 t != null && t != typeof(object) && t.Assembly != typeof(UnityEngine.Object).Assembly;
                 t = t.BaseType)
            {
                found = t.GetMethods(MemberFlags)
                    .Where(m => m.Name == methodName)
                    .FirstOrDefault(m => m.GetParameters().Length is 0 or 2);

                if (found != null)
                    break;
            }

            MethodCache[key] = found;
            return found;
        }

        // Mirrors ConditionResolver.GetOwningObject, parameterized by root object so it can
        // resolve the correct instance per selected target during multi-object edits.
        private static object GetOwningObject(object root, string propertyPath)
        {
            object current = root;

            var path = propertyPath.Replace(".Array.data[", "[");
            var segments = path.Split('.');

            for (var i = 0; i < segments.Length - 1; i++)
            {
                var segment = segments[i];
                var bracket = segment.IndexOf('[');

                if (bracket >= 0)
                {
                    var name = segment.Substring(0, bracket);
                    var indexText = segment
                        .Substring(bracket)
                        .Replace("[", string.Empty)
                        .Replace("]", string.Empty);

                    if (!int.TryParse(indexText, out var index))
                        return null;

                    current = GetElementAt(current, name, index);
                }
                else
                {
                    current = GetMemberValue(current, segment);
                }

                if (current == null)
                    return null;
            }

            return current;
        }

        private static object GetMemberValue(object source, string name)
        {
            if (source == null)
                return null;

            for (var t = source.GetType(); t != null && t != typeof(object); t = t.BaseType)
            {
                var field = t.GetField(name, MemberFlags);
                if (field != null)
                    return field.GetValue(source);

                var property = t.GetProperty(name, MemberFlags);
                if (property != null && property.CanRead)
                    return property.GetValue(source, null);
            }

            return null;
        }

        private static object GetElementAt(object source, string name, int index)
        {
            if (GetMemberValue(source, name) is not IEnumerable enumerable)
                return null;

            if (enumerable is IList list)
                return index >= 0 && index < list.Count ? list[index] : null;

            var enumerator = enumerable.GetEnumerator();
            try
            {
                for (var i = 0; i <= index; i++)
                {
                    if (!enumerator.MoveNext())
                        return null;
                }

                return enumerator.Current;
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
    }
}
