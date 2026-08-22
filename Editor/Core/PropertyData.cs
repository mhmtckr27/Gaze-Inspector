using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal sealed class PropertyData
    {
        internal SerializedProperty Property;

        internal GUIContent Label;                 // display name as GUIContent
        internal Attribute[] Attributes;           // attributes on the backing field
        internal string TypeName;                  // managed field type (pretty)
        internal ContextContainer AdditionalData = new();
        internal Type FieldType;
        internal Type DeclaredType;
        internal Type RuntimeType;
        internal string PropertyPath;               // cache if you need

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append($"Path: {PropertyPath}\t|\tTypeName: {TypeName}\t|\tFieldType: {FieldType}\t|\tRuntimeType: {RuntimeType}");
            return builder.ToString();
        }

        public static PropertyData Create(DrawerBase drawer)
        {
            var property = drawer.Property;
            return new PropertyData()
            {
                Property     = property.Copy(),
                Label        = new GUIContent(property.displayName)
                {
                    tooltip  = property.tooltip
                },
                Attributes   = ReflectionCache.GetAttributes(property),
                TypeName     = ReflectionCache.GetManagedFieldTypePretty(property),
                FieldType    = ReflectionCache.GetFieldType(property),
                RuntimeType  = ReflectionCache.GetRuntimeType(property),
                PropertyPath = property.propertyPath,
            };
        }
        
        internal PropertyData From(DrawerBase drawer)
        {
            var property = drawer.Property;
            Property     = property.Copy();
            Label        = new GUIContent(Property.displayName)
            {
                tooltip  = Property.tooltip
            };
            Attributes   = ReflectionCache.GetAttributes(Property);
            TypeName     = ReflectionCache.GetManagedFieldTypePretty(Property);
            FieldType    = ReflectionCache.GetFieldType(Property);
            RuntimeType  = ReflectionCache.GetRuntimeType(Property);
            PropertyPath = property.propertyPath;
            return this;
        }

        private static Dictionary<Type, List<Attribute>> GetAttributes(SerializedProperty property)
        {
            var result = new Dictionary<Type, List<Attribute>>();
            var attributes = ReflectionCache.GetAttributes(property);

            foreach (var attribute in attributes)
            {
                var type = attribute.GetType();
                
                if (!result.ContainsKey(type))
                    result.Add(type, new List<Attribute>());
                
                result[type].Add(attribute);
            }
            
            return result;
        }
    }
}