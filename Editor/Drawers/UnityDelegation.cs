// Put this in your core editor asm
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal static class UnityDelegation
    {
        // Cache per member to avoid reflection cost
        static readonly Dictionary<MemberInfo, bool> _delegateCache = new();

        public static bool ShouldDelegate(SerializedProperty property)
        {
            var attributesWhitelist = AttributeDrawerRegistry.AttributesWeHandle;
            var member = ReflectionCache.GetFieldInfoAndAttrs(property).field;
            
            if (member != null && _delegateCache.TryGetValue(member, out var cached)) return cached;

            // 1) Field-level PropertyAttributes we DON'T handle → delegate.
            //    Metadata-only attributes are consumed by the type-resolved drawer rather than requesting
            //    one, so they must not count as a reason to delegate - same rule HasDrawersForAllAttributes
            //    applies, kept in sync through the shared set.
            if (member != null)
            {
                foreach (var a in member.GetCustomAttributes(true))
                {
                    var attrType = a.GetType();

                    if (AttributeDrawerRegistry.MetadataOnlyAttributes.Contains(attrType))
                        continue;

                    if (!attributesWhitelist.Contains(attrType))
                        return Cache(member, true);
                }
            }

            // 2) Unity handler has a property drawer (type- or attr-based) → delegate
            var handler = GetHandler(property);
            if (handler != null && HasPropertyDrawer(handler))
                return Cache(member, true);

            // 3) Unity handler has decorator drawers we don't handle → delegate
            if (handler != null && HasForeignDecorators(handler, attributesWhitelist))
                return Cache(member, true);

            return Cache(member, false);
        }

        static bool Cache(MemberInfo mi, bool v)
        {
            if (mi != null) _delegateCache[mi] = v;
            return v;
        }

        // --- PropertyHandler reflection helpers ---
        static readonly Type T_PropertyHandler = typeof(EditorGUI).Assembly.GetType("UnityEditor.PropertyHandler");
        static readonly Type T_ScriptAttrUtil  = typeof(EditorGUI).Assembly.GetType("UnityEditor.ScriptAttributeUtility");

        static readonly MethodInfo MI_GetHandler = T_ScriptAttrUtil?.GetMethod("GetHandler",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null,
            new[] { typeof(SerializedProperty) }, null);

        static readonly FieldInfo FI_PropertyDrawer =
            T_PropertyHandler?.GetField("m_PropertyDrawer", BindingFlags.Instance | BindingFlags.NonPublic);

        static readonly FieldInfo FI_DecoratorDrawers =
            T_PropertyHandler?.GetField("m_DecoratorDrawers", BindingFlags.Instance | BindingFlags.NonPublic);

        static object GetHandler(SerializedProperty sp) => MI_GetHandler?.Invoke(null, new object[] { sp });

        static bool HasPropertyDrawer(object handler) => FI_PropertyDrawer?.GetValue(handler) != null;

        static bool HasForeignDecorators(object handler, HashSet<Type> attributesWhitelist)
        {
            if (FI_DecoratorDrawers?.GetValue(handler) is not IEnumerable list)
                return false;

            foreach (var deco in list)
            {
                if (deco == null)
                    continue;

                // Each decorator drawer holds its attribute in a private 'm_Attribute' field
                var aField = deco.GetType().GetField("m_Attribute", BindingFlags.Instance | BindingFlags.NonPublic);

                if (aField?.GetValue(deco) is not PropertyAttribute attrObj)
                    continue;

                if (!attributesWhitelist.Contains(attrObj.GetType()))
                    return true; // unknown decorator -> delegate
            }

            return false;
        }
    }
}
