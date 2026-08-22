using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Build once per domain reload
namespace GrayWolf.Gaze.Editor
{
    [InitializeOnLoad]
    internal static class CustomPropertyDrawerRegistry
    {
        // attrType -> drawerType
        static readonly Dictionary<Type, Type> _attrDrawers = new();

        // list so we can choose closest base type at lookup
        static readonly List<TypeDrawerInfo> _typeDrawers = new();

        struct TypeDrawerInfo
        {
            public Type TargetType; // field type the drawer targets
            public Type DrawerType; // the PropertyDrawer type
            public bool UseForChildren; // apply to derived types too
        }

        static readonly BindingFlags kPrivInst = BindingFlags.NonPublic | BindingFlags.Instance;
        static readonly FieldInfo kCPD_mType = typeof(CustomPropertyDrawer).GetField("m_Type", kPrivInst);

        static readonly FieldInfo kCPD_mUseForChildren =
            typeof(CustomPropertyDrawer).GetField("m_UseForChildren", kPrivInst);

        static CustomPropertyDrawerRegistry() => BuildIndex();

        static void BuildIndex()
        {
            _attrDrawers.Clear();
            _typeDrawers.Clear();

            if (kCPD_mType == null || kCPD_mUseForChildren == null)
            {
                Debug.LogWarning(
                    $"[Gaze] {nameof(CustomPropertyDrawerRegistry)} | This editor version does not expose " +
                    "CustomPropertyDrawer.m_Type / m_UseForChildren, so custom PropertyDrawers cannot be " +
                    "discovered. Gaze will draw those fields with its own drawers instead.");
                return;
            }

            foreach (var t in TypeCache.GetTypesWithAttribute<CustomPropertyDrawer>())
            {
                if (t == null ||
                    t.IsAbstract ||
                    !typeof(PropertyDrawer).IsAssignableFrom(t))
                    continue;

                // Drawer can declare multiple [CustomPropertyDrawer] attributes
                var attrs = t.GetCustomAttributes(typeof(CustomPropertyDrawer), inherit: false)
                    .Cast<CustomPropertyDrawer>();

                foreach (var cpd in attrs)
                {
                    var target = kCPD_mType.GetValue(cpd) as Type;

                    if (target == null) continue;

                    var children = kCPD_mUseForChildren.GetValue(cpd) is true;

                    if (typeof(PropertyAttribute).IsAssignableFrom(target))
                    {
                        if(t.Namespace?.StartsWith("UnityEditor") ?? false)
                            continue;

                        // Attribute drawer
                        // Later drawers override earlier ones (Unity behavior: last one wins)
                        _attrDrawers[target] = t;
                    }
                    else
                    {
                        // Type drawer
                        _typeDrawers.Add(new TypeDrawerInfo
                        {
                            TargetType = target,
                            DrawerType = t,
                            UseForChildren = children
                        });
                    }
                }
            }

            // Optional: ensure most-specific types come first to speed lookups
            _typeDrawers.Sort((a, b) =>
            {
                if (a.TargetType == b.TargetType) return 0;
                if (a.TargetType.IsAssignableFrom(b.TargetType)) return 1; // b is more specific
                if (b.TargetType.IsAssignableFrom(a.TargetType)) return -1; // a is more specific
                return StringComparer.Ordinal.Compare(a.TargetType.FullName, b.TargetType.FullName);
            });
        }

        // ---------- Public lookup API ----------

        /// <summary>
        /// Try to get a PropertyDrawer type for a field's PropertyAttribute (sorted by PropertyAttribute.order).
        /// </summary>
        private static Type[] GetAttributeDrawersFor(PropertyAttribute[] fieldAttributes)
        {
            if (fieldAttributes == null) 
                return Array.Empty<Type>();

            var list = new List<Type>();
            
            // Unity sorts by PropertyAttribute.order (lowest first). We mirror that.
            foreach (var att in fieldAttributes.OrderBy(a => a.order))
            {
                var attType = att.GetType();
                // Exact match only (Unity does exact type, not base-type, for attr drawers)
                if (_attrDrawers.TryGetValue(attType, out var drawerType))
                    list.Add(drawerType);
            }

            return list.ToArray();
        }

        internal static bool HasAttributeDrawerFor(SerializedProperty property)
        {
            var attrs = ReflectionCache.GetAttributes(property).Where(a => typeof(PropertyAttribute).IsAssignableFrom(a.GetType())).Cast<PropertyAttribute>().ToArray();
            
            if (attrs is not { Length: > 0 })
                return false;
            
            var drawers = GetAttributeDrawersFor(attrs);

            return drawers is { Length: > 0 };
        }

        internal static bool HasCustomDrawerFor(SerializedProperty property)
        {
            return GetTypeDrawerFor(property) != null;
        }

        /// <summary>
        /// Try to get a PropertyDrawer type for a field type (chooses closest base if useForChildren).
        /// </summary>
        public static Type GetTypeDrawerFor(SerializedProperty property)
        {
            var fieldType = ReflectionCache.GetFieldType(property);
            if (fieldType == null) return null;

            // Exact match first
            for (int i = 0; i < _typeDrawers.Count; i++)
            {
                var info = _typeDrawers[i];
                if (info.TargetType == fieldType) return info.DrawerType;
            }

            // Then closest base/interface where UseForChildren == true
            // We walk up the inheritance chain; first hit wins.
            var t = fieldType;
            while (t != null && t != typeof(object))
            {
                for (int i = 0; i < _typeDrawers.Count; i++)
                {
                    var info = _typeDrawers[i];
                    if (!info.UseForChildren) continue;
                    if (info.TargetType.IsAssignableFrom(t))
                        return info.DrawerType;
                }

                t = t.BaseType;
            }

            // Optionally: interface support (Unity's own matching is class-based; add if you want)
            foreach (var itf in fieldType.GetInterfaces())
            {
                for (int i = 0; i < _typeDrawers.Count; i++)
                {
                    var info = _typeDrawers[i];
                    if (!info.UseForChildren) continue;
                    if (info.TargetType == itf) return info.DrawerType;
                }
            }

            return null;
        }
    }
}