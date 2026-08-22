using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using Object = UnityEngine.Object;

namespace GrayWolf.Gaze.Editor
{
    internal static class ConditionResolver
    {
        private enum ErrorKind
        {
            NotResolvedAsBool,
            NotResolvedAsValue,
            NotBool
        }

        private const BindingFlags MemberFlags =
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        private static readonly Dictionary<(Type Type, string Member), Func<object, bool>> Accessors
            = new Dictionary<(Type, string), Func<object, bool>>();

        private static readonly Dictionary<(Type Type, string Member), Func<object, object>> ValueAccessors
            = new Dictionary<(Type, string), Func<object, object>>();

        private static readonly Dictionary<(Type Type, string Member, ErrorKind Kind), string> UnresolvedErrors
            = new Dictionary<(Type, string, ErrorKind), string>();

        public static bool Evaluate(
            SerializedProperty property,
            ConditionalAttribute attribute,
            out string error)
        {
            error = null;

            if (property == null || attribute == null)
                return true;

            var condition = attribute.Condition;

            if (string.IsNullOrEmpty(condition))
            {
                error = "No condition was specified.";
                return true;
            }

            if (string.IsNullOrWhiteSpace(condition))
            {
                error = "A condition name was empty.";
                return true;
            }

            // condition negation: "!useCustomRate"
            var negate = false;
            var name = condition.Trim();
            while (name.Length > 1 && name[0] == '!')
            {
                negate = !negate;
                name = name.Substring(1).Trim();
            }

            bool value;

            if (attribute.HasOptionalValue)
            {
                if (!TryResolveValue(property, name, out var actual))
                {
                    error = GetError(property, name, ErrorKind.NotResolvedAsValue);
                    return true;
                }

                if (!TryCompare(actual, attribute.OptionalValue, out value))
                {
                    error = DescribeComparisonFailure(name, actual, attribute.OptionalValue);
                    return true;
                }
            }
            else if (!TryResolve(property, name, out value))
            {
                error = GetError(
                    property,
                    name,
                    TryResolveValue(property, name, out _) ? ErrorKind.NotBool : ErrorKind.NotResolvedAsBool);

                return true;
            }

            if (negate)
                value = !value;

            var result = value;
            return attribute.Invert ? !result : result;
        }

        private static bool TryResolve(SerializedProperty property, string memberName, out bool value)
        {
            value = false;

            if (property == null || string.IsNullOrEmpty(memberName))
                return false;

            // Fast path: a serialized bool sibling, or one further up the chain.
            var direct = FindConditionProperty(property, memberName, SerializedPropertyType.Boolean);
            if (direct != null)
            {
                value = direct.boolValue;
                return true;
            }

            // Slow path: reflection against the owning instance.
            var owner = GetOwningObject(property);
            return TryResolveByReflection(owner, memberName, out value);
        }

        private static bool TryResolveValue(SerializedProperty property, string memberName, out object value)
        {
            value = null;

            if (property == null || string.IsNullOrEmpty(memberName))
                return false;

            var direct = FindConditionProperty(property, memberName, null);
            if (direct != null && TryReadComparableValue(direct, out value))
                return true;

            var owner = GetOwningObject(property);
            return TryResolveValueByReflection(owner, memberName, out value);
        }

        /// <summary>
        /// Reflection-only resolution. Exposed so other drawers (validators,
        /// button enable states) can reuse the same cache.
        /// </summary>
        private static bool TryResolveByReflection(object target, string memberName, out bool value)
        {
            value = false;

            if (target == null || string.IsNullOrEmpty(memberName))
                return false;

            var key = (target.GetType(), memberName);

            if (!Accessors.TryGetValue(key, out Func<object, bool> accessor))
            {
                accessor = BuildAccessor(key.Item1, memberName);
                Accessors[key] = accessor; // null is a cached miss
            }

            if (accessor == null)
                return false;

            try
            {
                value = accessor(target);
                return true;
            }
            catch (Exception)
            {
                // A user property or method threw. Treat as unresolved rather
                // than letting it escape into OnGUI and break the inspector.
                return false;
            }
        }

        private static bool TryResolveValueByReflection(object target, string memberName, out object value)
        {
            value = null;

            if (target == null || string.IsNullOrEmpty(memberName))
                return false;

            var key = (target.GetType(), memberName);

            if (!ValueAccessors.TryGetValue(key, out Func<object, object> accessor))
            {
                accessor = BuildValueAccessor(key.Item1, memberName);
                ValueAccessors[key] = accessor;
            }

            if (accessor == null)
                return false;

            try
            {
                value = accessor(target);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ------------------------------------------------------------------
        // Comparison
        // ------------------------------------------------------------------

        private static bool TryCompare(object actual, object expected, out bool equal)
        {
            equal = false;

            if (actual is Object || expected is Object)
            {
                if (actual != null && actual is not Object)
                    return false;

                if (expected != null && expected is not Object)
                    return false;

                equal = (Object) actual == (Object) expected;
                return true;
            }

            if (actual == null || expected == null)
            {
                equal = actual == null && expected == null;
                return true;
            }

            var left = UnwrapEnum(actual);
            var right = UnwrapEnum(expected);

            if (IsNumeric(left) && IsNumeric(right))
            {
                try
                {
                    equal = NumericEquals(left, right);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            var leftType = left.GetType();
            var rightType = right.GetType();

            if (!leftType.IsAssignableFrom(rightType) && !rightType.IsAssignableFrom(leftType))
                return false;

            equal = left.Equals(right);
            return true;
        }

        private static object UnwrapEnum(object value)
            => value is Enum
                ? Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()))
                : value;

        private static bool IsNumeric(object value)
        {
            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        private static bool NumericEquals(object left, object right)
        {
            var leftCode = Type.GetTypeCode(left.GetType());
            var rightCode = Type.GetTypeCode(right.GetType());

            if (leftCode == TypeCode.Single || rightCode == TypeCode.Single)
                return Convert.ToSingle(left).Equals(Convert.ToSingle(right));

            if (leftCode == TypeCode.Double || rightCode == TypeCode.Double)
                return Convert.ToDouble(left).Equals(Convert.ToDouble(right));

            if (leftCode == TypeCode.Decimal || rightCode == TypeCode.Decimal)
                return Convert.ToDecimal(left) == Convert.ToDecimal(right);

            if (leftCode == TypeCode.UInt64 || rightCode == TypeCode.UInt64)
            {
                if (IsNegative(left, leftCode) || IsNegative(right, rightCode))
                    return false;

                return Convert.ToUInt64(left) == Convert.ToUInt64(right);
            }

            return Convert.ToInt64(left) == Convert.ToInt64(right);
        }

        private static bool IsNegative(object value, TypeCode code)
        {
            switch (code)
            {
                case TypeCode.SByte: return (sbyte) value < 0;
                case TypeCode.Int16: return (short) value < 0;
                case TypeCode.Int32: return (int) value < 0;
                case TypeCode.Int64: return (long) value < 0;
                default: return false;
            }
        }

        // ------------------------------------------------------------------
        // Errors
        // ------------------------------------------------------------------

        private static string GetError(SerializedProperty property, string name, ErrorKind kind)
        {
            var targetType = property.serializedObject.targetObject.GetType();
            var key = (targetType, name, kind);

            if (UnresolvedErrors.TryGetValue(key, out var error))
                return error;

            error = kind switch
            {
                ErrorKind.NotResolvedAsBool =>
                    $"Could not resolve '{name}' on {targetType.Name}. " +
                    "Expected a bool field, a bool property, or a parameterless method returning bool.",
                ErrorKind.NotResolvedAsValue =>
                    $"Could not resolve '{name}' on {targetType.Name}. " +
                    "Expected a field, a property, or a parameterless method.",
                _ =>
                    $"'{name}' on {targetType.Name} is not a bool. " +
                    "Point the condition at a bool member, or pass a value to compare it against."
            };

            UnresolvedErrors[key] = error;
            return error;
        }

        private static string DescribeComparisonFailure(string name, object actual, object expected)
            => $"Cannot compare '{name}' ({Describe(actual)}) to {Describe(expected)}. " +
               "Expected the same type on both sides, two numbers, or an enum and its underlying value.";

        private static string Describe(object value)
            => value == null ? "null" : $"{value.GetType().Name} '{value}'";

        // ------------------------------------------------------------------
        // SerializedProperty path
        // ------------------------------------------------------------------

        /// <summary>
        /// Walks up the property path looking for a serialized bool with the
        /// given name, starting at the field's own container and ending at the
        /// serialized object root.
        /// </summary>
        private static SerializedProperty FindConditionProperty(
            SerializedProperty property,
            string name,
            SerializedPropertyType? type)
        {
            var so = property.serializedObject;
            var path = property.propertyPath;

            int dot;
            while ((dot = path.LastIndexOf('.')) >= 0)
            {
                path = path.Substring(0, dot);

                SerializedProperty parent = so.FindProperty(path);
                SerializedProperty found = parent?.FindPropertyRelative(name);

                if (found != null && (type == null || found.propertyType == type))
                    return found;
            }

            SerializedProperty root = so.FindProperty(name);

            return root != null && (type == null || root.propertyType == type)
                ? root
                : null;
        }

        private static bool TryReadComparableValue(SerializedProperty property, out object value)
        {
            try
            {
                value = property.propertyType == SerializedPropertyType.Enum
                    ? property.enumValueFlag
                    : property.GetValue();

                return true;
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }

        // ------------------------------------------------------------------
        // Reflection path
        // ------------------------------------------------------------------

        /// <summary>
        /// Builds a delegate that reads a bool member off an instance of
        /// <paramref name="type"/>, or null when no such member exists.
        /// Walks the base type chain manually because FlattenHierarchy does
        /// not surface private members declared on base types.
        /// </summary>
        private static Func<object, bool> BuildAccessor(Type type, string memberName)
        {
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                var field = t.GetField(memberName, MemberFlags);
                if (field != null && field.FieldType == typeof(bool))
                {
                    if (field.IsStatic)
                        return _ => (bool)field.GetValue(null);

                    return o => (bool)field.GetValue(o);
                }

                var property = t.GetProperty(memberName, MemberFlags);
                if (property != null && property.PropertyType == typeof(bool) && property.CanRead)
                {
                    var getter = property.GetGetMethod(true);
                    if (getter != null)
                    {
                        if (getter.IsStatic)
                            return _ => (bool)getter.Invoke(null, null);

                        return o => (bool)getter.Invoke(o, null);
                    }
                }

                var method = t.GetMethod(memberName, MemberFlags, null, Type.EmptyTypes, null);

                if (method != null && method.ReturnType == typeof(bool))
                {
                    if (method.IsStatic)
                        return _ => (bool)method.Invoke(null, null);

                    return o => (bool)method.Invoke(o, null);
                }
            }

            return null;
        }

        private static Func<object, object> BuildValueAccessor(Type type, string memberName)
        {
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                var field = t.GetField(memberName, MemberFlags);
                if (field != null)
                {
                    if (field.IsStatic)
                        return _ => field.GetValue(null);

                    return o => field.GetValue(o);
                }

                var property = t.GetProperty(memberName, MemberFlags);
                if (property != null && property.CanRead)
                {
                    var getter = property.GetGetMethod(true);
                    if (getter != null)
                    {
                        if (getter.IsStatic)
                            return _ => getter.Invoke(null, null);

                        return o => getter.Invoke(o, null);
                    }
                }

                var method = t.GetMethod(memberName, MemberFlags, null, Type.EmptyTypes, null);

                if (method != null && method.ReturnType != typeof(void))
                {
                    if (method.IsStatic)
                        return _ => method.Invoke(null, null);

                    return o => method.Invoke(o, null);
                }
            }

            return null;
        }

        // ------------------------------------------------------------------
        // Property path to instance
        // ------------------------------------------------------------------

        /// <summary>
        /// Returns the object that declares <paramref name="property"/>, which
        /// is the target object itself for a top level field, or a nested
        /// struct, class or collection element further down.
        /// </summary>
        private static object GetOwningObject(SerializedProperty property)
        {
            object current = property.serializedObject.targetObject;

            // "list.Array.data[3].speed" becomes "list[3].speed"
            var path = property.propertyPath.Replace(".Array.data[", "[");
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

                    if (!int.TryParse(indexText, out int index))
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
                for (int i = 0; i <= index; i++)
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
