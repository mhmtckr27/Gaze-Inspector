using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace GrayWolf.Gaze.Editor
{
    internal static class TypeExtensions
    {
        public static T GetAttribute<T>(this Type type) where T : Attribute
        {
            return type.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
        }

        public static Type GetAttributeTypeFrom(this Type attributeDrawerType)
        {
            Type result = null;
            var type = attributeDrawerType;
            
            do
            {
                result = type.GenericTypeArguments.FirstOrDefault();
                type = type.BaseType;
            } while(type != null && result == null);
            
            return result;
        }
        
        public static List<Type> GetConcreteDerivedTypes(this Type baseType)
        {
            var list = new List<Type>();
            
            foreach (var t in TypeCache.GetTypesDerivedFrom(baseType))
            {
                if (t.IsAbstract) 
                    continue;
                
                if (t.IsGenericTypeDefinition) 
                    continue;
                
                if (!HasSerializableDefaultCtor(t)) 
                    continue; // optional guard
                
                list.Add(t);
            }
            
            // also allow the base type itself if it's concrete
            if (!baseType.IsAbstract && !baseType.IsInterface) 
                list.Insert(0, baseType);
            
            return list;
        }

        private static bool HasSerializableDefaultCtor(Type t)
        {
            // Unity can serialize parameterless-ctor classes fine; you can relax this if you plan to use FormatterServices
            return t.GetConstructor(Type.EmptyTypes) != null;
        }
        
        
        internal static bool IsListLike(this Type t) => t.IsArray || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>));
    }
}