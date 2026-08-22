using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GrayWolf.Gaze.Editor
{
    internal static class SerializedPropertyExtensions
    {
        internal static IEnumerable<SerializedProperty> Children(this SerializedProperty property, bool onlyImmediateChildren = true, bool visibleOnly = true)
        {
            //early out if no children of specified kind (visible/not visible)
            switch (visibleOnly)
            {
                case true when !property.hasVisibleChildren:
                case false when !property.hasChildren:
                    yield break;
            }

            // Sum immediate children (depth == parent.depth + 1)
            var it  = property.Copy();
            var end = it.GetEndProperty().Copy();
            int targetDepth = property.depth + 1;
            
            if (visibleOnly ? it.NextVisible(true) : it.Next(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(it, end)) break;
                    if (onlyImmediateChildren && it.depth != targetDepth) continue; // only immediate children
        
                    // Copy before advancing iterator further
                    yield return it.Copy();
        
                } while (visibleOnly ? it.NextVisible(!onlyImmediateChildren) : it.Next(!onlyImmediateChildren));
            }
        }

        internal static SerializedProperty Root(this SerializedProperty property)
        {
            if (property == null) 
                return null;

            if (property.GetParent() == null)
                return property;

            do
            {
                property = property.GetParent();
                
                if(property is { depth: 0 })
                    return property;
            }
            while (property != null);

            return null;
        }

        internal static SerializedProperty GetParent(this SerializedProperty property)
        {
            var lastIndex = property.propertyPath.LastIndexOf('.');
            if (lastIndex == -1)
                return null;
            var path = property.propertyPath.Remove(lastIndex);
            return property.serializedObject.FindProperty(path).Copy();
        }

        internal static void LayoutChanged(this SerializedProperty property)
        {
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.UpdateIfRequiredOrScript();
            GUI.FocusControl(null);
        }

        internal static bool IsPrimitive(this SerializedProperty property)
        {
            return IsPrimitive(property.propertyType);
        }

        internal static bool IsPrimitive(this SerializedPropertyType propertyType)
        {
            return propertyType switch
            {
                SerializedPropertyType.Generic            => false,
                SerializedPropertyType.Integer            => true,
                SerializedPropertyType.Boolean            => true,
                SerializedPropertyType.Float              => true,
                SerializedPropertyType.String             => true,
                SerializedPropertyType.Color              => true,
                SerializedPropertyType.ObjectReference    => true,
                SerializedPropertyType.LayerMask          => true,
                SerializedPropertyType.Enum               => true,
                SerializedPropertyType.Vector2            => true,
                SerializedPropertyType.Vector3            => true,
                SerializedPropertyType.Vector4            => true,
                SerializedPropertyType.Rect               => false,
                SerializedPropertyType.ArraySize          => true,
                SerializedPropertyType.Character          => true,
                SerializedPropertyType.AnimationCurve     => true,
                SerializedPropertyType.Bounds             => false,
                SerializedPropertyType.Gradient           => true,
                SerializedPropertyType.Quaternion         => true,
                SerializedPropertyType.ExposedReference   => true,
                SerializedPropertyType.FixedBufferSize    => true,
                SerializedPropertyType.Vector2Int         => true,
                SerializedPropertyType.Vector3Int         => true,
                SerializedPropertyType.RectInt            => false,
                SerializedPropertyType.BoundsInt          => false,
                SerializedPropertyType.ManagedReference   => false,
                SerializedPropertyType.Hash128            => true,
#if UNITY_6000_0_OR_NEWER
                SerializedPropertyType.RenderingLayerMask => true,
#endif
                _                                         => throw new ArgumentOutOfRangeException()
            };
        }
        
        
        internal static bool HasAnyListParent(this SerializedProperty property)
        {
            var parent = property.GetParent();

            while (parent != null)
            {
                if (parent.isArray)
                    return true;
                
                parent = parent.GetParent();
            }

            return false;
        }
        
        internal static bool IsListItem(this SerializedProperty property)
        {
            var parent = property.GetParent();
            return parent?.isArray ?? false;
        }

        internal static object GetValue(this SerializedProperty property)
        {
            if (property == null)
                return null;
            return property.propertyType switch
            {
                SerializedPropertyType.Generic            => property.boxedValue,
                SerializedPropertyType.Integer            => property.intValue,
                SerializedPropertyType.Boolean            => property.boolValue,
                SerializedPropertyType.Float              => property.floatValue,
                SerializedPropertyType.String             => property.stringValue,
                SerializedPropertyType.Color              => property.colorValue,
                SerializedPropertyType.ObjectReference    => property.objectReferenceValue,
                SerializedPropertyType.LayerMask          => (LayerMask) property.intValue,
                SerializedPropertyType.Enum               => property.enumValueIndex,
                SerializedPropertyType.Vector2            => property.vector2Value,
                SerializedPropertyType.Vector3            => property.vector3Value,
                SerializedPropertyType.Vector4            => property.vector4Value,
                SerializedPropertyType.Rect               => property.rectValue,
                SerializedPropertyType.ArraySize          => property.arraySize,
                SerializedPropertyType.Character          => (char) property.intValue,
                SerializedPropertyType.AnimationCurve     => property.animationCurveValue,
                SerializedPropertyType.Bounds             => property.boundsValue,
                SerializedPropertyType.Gradient           => property.gradientValue,
                SerializedPropertyType.Quaternion         => property.quaternionValue,
                SerializedPropertyType.ExposedReference   => property.exposedReferenceValue,
                SerializedPropertyType.FixedBufferSize    => property.fixedBufferSize,
                SerializedPropertyType.Vector2Int         => property.vector2IntValue,
                SerializedPropertyType.Vector3Int         => property.vector3IntValue,
                SerializedPropertyType.RectInt            => property.rectIntValue,
                SerializedPropertyType.BoundsInt          => property.boundsIntValue,
                SerializedPropertyType.ManagedReference   => property.managedReferenceValue,
                SerializedPropertyType.Hash128            => property.hash128Value,
#if UNITY_6000_0_OR_NEWER
                SerializedPropertyType.RenderingLayerMask => (RenderingLayerMask) property.intValue,
#endif
                _                                         => throw new ArgumentOutOfRangeException()
            };
        }
        
        internal static bool IsValueEquals(this SerializedProperty property, object expectedValue)
        {
            var (propertyValue, castExpectedValue) = property.propertyType switch
            {
                SerializedPropertyType.Integer            => (property.intValue, (int) expectedValue),
                SerializedPropertyType.Boolean            => (property.boolValue, (bool) expectedValue),
                SerializedPropertyType.Float              => (property.floatValue, (float) expectedValue),
                SerializedPropertyType.String             => (property.stringValue, (string) expectedValue),
                SerializedPropertyType.Color              => (property.colorValue, (Color) expectedValue),
                SerializedPropertyType.ObjectReference    => (property.objectReferenceValue, (Object) expectedValue),
                SerializedPropertyType.LayerMask          => (property.intValue, (int) expectedValue),
                SerializedPropertyType.Enum               => (property.enumValueFlag, (int) expectedValue),
                SerializedPropertyType.Vector2            => (property.vector2Value, (Vector2) expectedValue),
                SerializedPropertyType.Vector3            => (property.vector3Value, (Vector3) expectedValue),
                SerializedPropertyType.Vector4            => (property.vector4Value, (Vector4) expectedValue),
                SerializedPropertyType.Rect               => (property.rectValue, (Rect) expectedValue),
                SerializedPropertyType.AnimationCurve     => (property.animationCurveValue, (AnimationCurve) expectedValue),
                SerializedPropertyType.Bounds              => (property.boundsValue, (Bounds) expectedValue),
                SerializedPropertyType.Gradient           => (property.gradientValue, (Gradient) expectedValue),
                SerializedPropertyType.Quaternion         => (property.quaternionValue, (Quaternion) expectedValue),
                SerializedPropertyType.ExposedReference   => (property.exposedReferenceValue, (Object) expectedValue),
                SerializedPropertyType.Vector2Int         => (property.vector2IntValue, (Vector2Int) expectedValue),
                SerializedPropertyType.Vector3Int         => (property.vector3IntValue, (Vector3Int) expectedValue),
                SerializedPropertyType.RectInt            => (property.rectIntValue, (RectInt) expectedValue),
                SerializedPropertyType.BoundsInt          => (property.boundsIntValue, (BoundsInt) expectedValue),
                SerializedPropertyType.ManagedReference   => (property.managedReferenceValue, (object) expectedValue),
                SerializedPropertyType.Hash128            => (property.hash128Value, (Hash128) expectedValue),
#if UNITY_6000_0_OR_NEWER
                SerializedPropertyType.RenderingLayerMask => ((uint) property.intValue, (uint) expectedValue),
#endif
                _                                          => (property.boxedValue, (object) expectedValue)
            };

            return castExpectedValue.Equals(propertyValue);
        }

        internal static SerializedProperty GetKeyData(this SerializedProperty property)
            => property.FindPropertyRelative("keyData") != null
                ? property.FindPropertyRelative("keyData")
                : property.FindPropertyRelative("keyData__backingField");

        internal static SerializedProperty GetValueData(this SerializedProperty property)
            => property.FindPropertyRelative("valueData") != null
                ? property.FindPropertyRelative("valueData")
                : property.FindPropertyRelative("valueData__backingField");
    }
}