using System;
using System.Collections.Generic;
using System.Linq;
using GrayWolf.Gaze.Collections;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    // -----------------------------
    // Registry
    // -----------------------------
    internal sealed class DrawerRegistry
    {
        private readonly PropertyTree _tree;
        private readonly Dictionary<SerializedPropertyType, Type> _byPropType = new();
        private readonly Dictionary<string, DrawerBase> _drawers = new();
        internal readonly List<IContextMenuProvider> ContextMenuProviders = new();

        public DrawerRegistry(PropertyTree tree)
        {
            _tree = tree;
            _byPropType[SerializedPropertyType.Generic         ] = typeof(InlineGenericDrawer);
            _byPropType[SerializedPropertyType.Integer         ] = typeof(IntDrawer);
            _byPropType[SerializedPropertyType.Boolean         ] = typeof(ToggleDrawer);
            _byPropType[SerializedPropertyType.Float           ] = typeof(FloatDrawer);
            _byPropType[SerializedPropertyType.String          ] = typeof(StringDrawer);
            _byPropType[SerializedPropertyType.Color           ] = typeof(ColorDrawer);
                    
            _byPropType[SerializedPropertyType.ObjectReference ] = typeof(ObjectReferenceDrawer);
            _byPropType[SerializedPropertyType.LayerMask       ] = typeof(MaskDrawer);
            
            _byPropType[SerializedPropertyType.Enum            ] = typeof(EnumDrawer);
            _byPropType[SerializedPropertyType.Vector2         ] = typeof(Vector2Drawer);
            _byPropType[SerializedPropertyType.Vector3         ] = typeof(Vector3Drawer);
            _byPropType[SerializedPropertyType.Vector4         ] = typeof(Vector4Drawer);
            
            _byPropType[SerializedPropertyType.Rect            ] = typeof(RectDrawer);
            _byPropType[SerializedPropertyType.Character       ] = typeof(CharacterDrawer);

            _byPropType[SerializedPropertyType.AnimationCurve  ] = typeof(AnimationCurveDrawer);
            _byPropType[SerializedPropertyType.Bounds          ] = typeof(BoundsDrawer);
            
            _byPropType[SerializedPropertyType.Gradient        ] = typeof(GradientDrawer);
            _byPropType[SerializedPropertyType.Quaternion      ] = typeof(QuaternionDrawer);
            _byPropType[SerializedPropertyType.ExposedReference] = typeof(ExposedReferenceDrawer);
            _byPropType[SerializedPropertyType.Vector2Int      ] = typeof(Vector2IntDrawer);
            _byPropType[SerializedPropertyType.Vector3Int      ] = typeof(Vector3IntDrawer);
            _byPropType[SerializedPropertyType.RectInt         ] = typeof(RectIntDrawer);
            _byPropType[SerializedPropertyType.BoundsInt       ] = typeof(BoundsIntDrawer);
            _byPropType[SerializedPropertyType.ManagedReference] = typeof(InlineGenericDrawer);
            _byPropType[SerializedPropertyType.Hash128         ] = typeof(Hash128Drawer);
        }

        internal Type GetDrawerType(SerializedProperty property)
        {
            var unityHasDrawers = UnityDelegation.ShouldDelegate(property);
            var weHaveAllAttrDrawers = AttributeDrawerRegistry.HasDrawersForAllAttributes(property);
            var hasCustomDrawer = CustomPropertyDrawerRegistry.HasCustomDrawerFor(property);
            var bla = CustomPropertyDrawerRegistry.HasAttributeDrawerFor(property);

            if (property.propertyPath == "m_Script")
                return typeof(FallbackPropertyDrawer);
            
            if (unityHasDrawers && !weHaveAllAttrDrawers || hasCustomDrawer || bla)
                return typeof(FallbackPropertyDrawer);

            if (AttributeDrawerRegistry.TryGetDrawerType(property, out var drawerType))
                return drawerType;
            
            if (IsOrDerivedFromGeneric(ReflectionCache.GetFieldType(property), typeof(SerializedDictionary<,>)))
                return typeof(SerializedDictionaryDrawer);
            
            if (property.isArray && property.propertyType != SerializedPropertyType.String)
                return typeof(ListDrawer);
            
            if (!_byPropType.TryGetValue(property.propertyType, out drawerType))
                drawerType = typeof(FallbackPropertyDrawer);
            
            return drawerType;
        }

        
        internal PropertyDrawerBase CreateDrawer(Type type, SerializedProperty property, params object[] parameters)
        {
            try
            {
                var drawer = Activator.CreateInstance(type, parameters) as PropertyDrawerBase;
                drawer.Tree = _tree;
                drawer.InitializeInternal(property);
                
                TryAddContextMenuProvider(drawer);
                
                return drawer;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Gaze] {nameof(DrawerRegistry)} | Could not create {type.Name} " +
                               $"for '{property.propertyPath}'. Details: {e}");
            }

            return null;
        }

        private PropertyDrawerBase CreateDrawer<T>(SerializedProperty property, params object[] parameters)
        {
            return CreateDrawer(typeof(T), property, parameters);
        }

        public DrawerBase Resolve(SerializedProperty property)
        {
            var key = property.propertyPath;

            if (_drawers.TryGetValue(key, out var drawer))
                return drawer;

            var drawerType = GetDrawerType(property);
            drawer = CreateDrawer(drawerType, property);
            drawer = WrapWithDecorators(drawer);
            _drawers.Add(key, drawer);
            return drawer;
        }

        internal DrawerBase WrapWithDecorators(DrawerBase drawer)
        {
            var property = drawer.Property;
            
            if (property.propertyPath == "m_Script")
            {
                var decorator = AttributeDrawerRegistry.GetDrawersForAttribute(typeof(ReadOnlyAttribute)).FirstOrDefault();

                if (decorator != null)
                {
                    drawer = AttributeDrawerRegistry.Wrap(typeof(ReadOnlyDecorator), new ReadOnlyAttribute(), drawer);
                    return drawer;
                }
            }

            if (drawer is FallbackPropertyDrawer)
                return drawer; // give it to Unity entirely

            var pairs = AttributeDrawerRegistry.Resolve(drawer);

            foreach (var (attribute, drawerType) in pairs.forwarded)
            {
                drawer = AttributeDrawerRegistry.Wrap(drawerType, attribute, drawer);
                TryAddContextMenuProvider(drawer);
            }

            foreach (var (attribute, drawerType) in pairs.self)
            {
                drawer = AttributeDrawerRegistry.Wrap(drawerType, attribute, drawer);
                TryAddContextMenuProvider(drawer);
            }            
            return drawer;
        }

        internal void TryAddContextMenuProvider(DrawerBase drawer)
        {
            if (drawer is IContextMenuProvider contextMenuProvider)
                ContextMenuProviders.Add(contextMenuProvider);
        }

        internal static DrawerBase Wrap(Type decoratorType, PropertyDrawerBase inner, params object[] parameters)
        {
            AttributeDrawerBase wrapper = (AttributeDrawerBase) Activator.CreateInstance(decoratorType, parameters);
            wrapper.Inner = inner;
            wrapper.Data = inner.Data;
            return wrapper;
        }

        private static bool IsOrDerivedFromGeneric(Type type, Type genericTypeDef)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDef)
                    return true;
                type = type.BaseType;
            }
            return false;
        }
    }
}