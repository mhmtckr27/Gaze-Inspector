using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GrayWolf.Gaze.Collections;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    [InitializeOnLoad]
    internal static class AttributeDrawerRegistry
    {
        // Map: AttributeType → Sorted drawer types (highest priority last)
        internal static HashSet<Type> AttributesWeHandle { get; } = new();

        /// <summary>
        /// Attributes that carry configuration rather than requesting a drawer. They are consumed
        /// elsewhere - SerializeField by Unity, SerializedDictionaryAttribute by the type-resolved
        /// SerializedDictionaryDrawer - so the absence of an AttributeDrawer for them is not a reason to
        /// hand the property to Unity. Without this, tagging a field with one of them silently downgraded
        /// it to the fallback drawer while the untagged field beside it drew correctly.
        /// </summary>
        internal static readonly HashSet<Type> MetadataOnlyAttributes = new()
        {
            typeof(SerializeField),
            typeof(SerializedDictionaryAttribute)
        };

        static readonly Dictionary<Type, List<Type>> _typeToDrawers = new();
        static readonly Dictionary<Type, List<Type>> _attrToDrawers = new();

        static AttributeDrawerRegistry() => Rebuild();

        static void Rebuild()
        {
            AttributesWeHandle.Clear();
            _typeToDrawers.Clear();
            _attrToDrawers.Clear();

            foreach (var t in TypeCache.GetTypesDerivedFrom<AttributeDrawerBase>())
            {
                if (t == null || t.IsAbstract || t.IsGenericTypeDefinition) continue;

                // Try to find an AttributeDrawer<TAttr> somewhere in the inheritance chain
                if (!TryGetClosedGenericArg(t, typeof(AttributeDrawer<>), out var attrType)) continue;

                // Sanity
                if (!typeof(Attribute).IsAssignableFrom(attrType)) continue;

                AttributesWeHandle.Add(attrType);

                if (!_attrToDrawers.TryGetValue(attrType, out var list))
                    _attrToDrawers[attrType] = list = new List<Type>(1);

                list.Add(t);
            }

            foreach (var t in TypeCache.GetTypesDerivedFrom<PropertyDrawerBase>())
            {
                if (t == null || t.IsAbstract || t.IsGenericTypeDefinition) continue;

                // Try to find a PropertyDrawer<TValue> somewhere in the inheritance chain
                if (!TryGetClosedGenericArg(t, typeof(PropertyDrawer<>), out var propType)) continue;

                if (!_typeToDrawers.TryGetValue(propType, out var list))
                    _typeToDrawers[propType] = list = new List<Type>(1);

                if (list.Count > 0)
                {
                    Debug.LogWarning($"[Gaze] {nameof(AttributeDrawerRegistry)} | " +
                                     $"'{t.FullName}' and '{list[0].FullName}' both declare " +
                                     $"PropertyDrawer<{propType.Name}>. " +
                                     $"'{list[0].FullName}' is used; the other is ignored.");
                }

                list.Add(t);
            }
        }

        // Walk up the base chain to find a closed generic with the given open generic definition
        static bool TryGetClosedGenericArg(Type concrete, Type openGeneric, out Type arg0)
        {
            arg0 = null;
            for (var t = concrete; t != null && t != typeof(object); t = t.BaseType)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == openGeneric)
                {
                    var args = t.GetGenericArguments();
                    if (args.Length >= 1)
                    {
                        arg0 = args[0];
                        return true;
                    }
                }
            }
            return false;
        }
        
        internal static IReadOnlyList<Type> GetDrawersForAttribute(Type attributeType)
        {
            return _attrToDrawers.TryGetValue(attributeType, out var list) ? list : Array.Empty<Type>();
        }

        internal static bool TryGetDrawerType(SerializedProperty property, out Type drawerType)
        {
            drawerType = null;
            var type = ReflectionCache.GetFieldType(property);
            
            if(type == null)
                return false;

            if (property.isArray)
                return false;
            
            var result = _typeToDrawers.TryGetValue(type, out var list);
            drawerType = list?.FirstOrDefault();
            return result;
        }

        // ----- Public API -----

        // Returns drawers (types) for a given Attribute type (already priority-sorted)

        internal static DrawerBase Wrap<T>(Attribute attribute, PropertyDrawerBase inner) where T : AttributeDrawerBase
        {
            return Wrap(typeof(T), attribute, inner);
        }

        internal static DrawerBase Wrap(Type drawerType, Attribute attribute, DrawerBase inner)
        {
            if (inner.Property.isArray && drawerType == typeof(SerializeReferenceDecorator))
                return inner;
            
            var wrapper = (AttributeDrawerBase) Activator.CreateInstance(drawerType);
            wrapper.Tree = inner.Tree;
            wrapper.Inner = inner;
            inner.Parent = wrapper;
            wrapper.Attribute = attribute;
            wrapper.InitializeInternal(inner.Property);
            return wrapper;
        }

        public static bool HasDrawersForAllAttributes(SerializedProperty property)
        {
            var field = ReflectionCache.GetFieldInfoAndAttrs(property).field;

            if (field == null) 
                return false;

            foreach (var attrData in field.GetCustomAttributesData())
            {
                var attrType = attrData.AttributeType;
                
                if (!typeof(Attribute).IsAssignableFrom(attrType))
                    continue;

                if(MetadataOnlyAttributes.Contains(attrType))
                    continue;

                if (GetDrawersForAttribute(attrType).Count == 0) 
                    return false;
                
                // Get the real attribute instance (includes ctor args/props)
                if (field.GetCustomAttributes(attrType, inherit: false).FirstOrDefault() == null)
                    return false;
            }

            return true;
        }
        
        // Element-safe forwards (parent array field → each element)
        static readonly HashSet<Type> kForwardToElements = new()
        {
            typeof(RangeAttribute),
            typeof(MinAttribute),
            typeof(DelayedAttribute),
            typeof(SerializeReference)
        };
        

    // Attributes that should NEVER flow down implicitly
        static readonly HashSet<Type> kNeverInherit = new()
        {
            typeof(HeaderAttribute),
            typeof(SpaceAttribute),
        };

        private static readonly HashSet<Type> kIgnoreAttributes = MetadataOnlyAttributes;
        
        // Returns (selfAttrs for this field), (forwardedAttrs from parent if element), (inherited group attrs)
        private static (
            List<Attribute> self, 
            List<Attribute> forwarded)
            ResolveAttributes(DrawerBase drawer)
        {
            var sp = drawer.Property;
            var (fi, leafAttrs) = ReflectionCache.GetFieldInfoAndAttrs(sp);

            if (fi == null)
                return (new List<Attribute>(), new List<Attribute>());

            // 1) Self-declared attributes on THIS field (declaration order)
            var self = new List<Attribute>(leafAttrs.Length);

            if (fi.FieldType.IsListLike() && !drawer.Property.isArray)
            {
            }
            else
            {
                self.AddRange(leafAttrs);
            }

            // 2) Forward-from-parent if we are an array/list element
            var forwarded = new List<Attribute>(2);
            if (sp.IsListItem())
            {
                var parent = drawer.Property.GetParent(); // your tree has this
                var parentFi = ReflectionCache.GetFieldInfoAndAttrs(parent).field;
                if (parentFi != null)
                {
                    foreach (var a in parentFi.GetCustomAttributes(true))
                    {
                        var t = a.GetType();
                        if (kForwardToElements.Contains(t))
                            forwarded.Add((Attribute)a);
                    }
                }
            }

            // 4) Drop anything that should never flow implicitly
            forwarded.RemoveAll(a => kNeverInherit.Contains(a.GetType()));

            self.RemoveAll(a => kIgnoreAttributes.Contains(a.GetType()));
            forwarded.RemoveAll(a => kIgnoreAttributes.Contains(a.GetType()));
            return (self, forwarded);
        }

        // Resolve *declared* field attributes → concrete drawer instances paired with the attribute instance (keeps source order)
        public static (
            IReadOnlyList<(Attribute attr, Type drawerType)> self, 
            IReadOnlyList<(Attribute attr, Type drawerType)> forwarded)
            Resolve(DrawerBase propertyDrawer)
        {
            var property = propertyDrawer.Property;
            
            var (selfAttrs, forwardedAttrs) = ResolveAttributes(propertyDrawer);

            var (self, forwarded, inherited) = (
                new List<(Attribute attr, Type drawerType)>(), 
                new List<(Attribute attr, Type drawerType)>(), 
                new List<(Attribute attr, Type drawerType)>());

            var srDrawer = selfAttrs.Any(a => a is SerializeReference) || forwardedAttrs.Any(a => a is SerializeReference);
            
            if (!property.IsPrimitive() && (!property.IsListItem() || srDrawer))
            {
                if (!srDrawer && !property.isArray && propertyDrawer is not SerializedDictionaryDrawer
                    && !TryGetDrawerType(property, out _))
                {
                    selfAttrs.Insert(0, new FoldoutAttribute());
                }

                if (!property.IsListItem() && property.HasAnyListParent())
                {
                    selfAttrs.Insert(0, new NestedListItemAttribute());
                }
            }

            selfAttrs = BubbleHeaderAndSpaceToStart(selfAttrs);
            
            foreach (var attribute in selfAttrs)
            {
                var drawer = GetDrawersForAttribute(attribute.GetType()).FirstOrDefault();
                if (drawer != null)
                    self.Add((attribute, drawer));
            }
            foreach (var attribute in forwardedAttrs)
            {
                var drawer = GetDrawersForAttribute(attribute.GetType()).FirstOrDefault();
                if (drawer != null)
                    forwarded.Add((attribute, drawer));
            }
            
            self.Reverse();
            forwarded.Reverse();
            
            return (self, forwarded);
        }

                
        // Convenience: move Header/Space to the end
        public static List<Attribute> BubbleHeaderAndSpaceToStart(IReadOnlyList<Attribute> src)
            => BubbleToStart(src, static t =>
                t == typeof(HeaderAttribute) ||
                t == typeof(SpaceAttribute));

        // Generic: move anything matching `isTail` to the end (stable)
        public static List<Attribute> BubbleToStart(
            IReadOnlyList<Attribute> src,
            Func<Type, bool> isHead)
        {
            // minimal allocs
            var head = new List<Attribute>(src.Count);
            var tail = new List<Attribute>(4);

            for (int i = 0; i < src.Count; i++)
            {
                var a = src[i];
                (isHead(a.GetType()) ? head : tail).Add(a);
            }

            // head (preserved order) + tail (preserved order)
            head.AddRange(tail);
            return head;
        }
    }
}
