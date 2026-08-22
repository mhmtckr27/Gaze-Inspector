using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    // -----------------------------
    // Reflection + Attribute cache
    // -----------------------------
    internal static class ReflectionCache
    {
        // --- CACHES ---
        static readonly Dictionary<(Type Decl, string Name), (FieldInfo fi, Attribute[] attrs)> _fieldCache = new();
        static readonly Dictionary<(Type Root, string Path), (FieldInfo leaf, Type owner)> _pathCache = new();

        public static void ClearCaches()
        {
            _fieldCache.Clear();
            _pathCache.Clear();
        }

#if UNITY_EDITOR
        [InitializeOnLoad]
        static class _CacheBootstrap
        {
            static _CacheBootstrap()
            {
                AssemblyReloadEvents.beforeAssemblyReload += ClearCaches;
            }
        }
#endif

        public static Attribute[] GetAttributes(SerializedProperty property)
        {
            var (fi, attrs) = GetFieldInfoAndAttrs(property);
            return attrs ?? Array.Empty<Attribute>();
        }

        public static string GetManagedFieldTypePretty(SerializedProperty prop)
        {
            var (fi, _) = GetFieldInfoAndAttrs(prop);
            if (fi == null) return prop.propertyType.ToString();
            var t = fi.FieldType;
            return t.IsGenericType ? PrettyGenericName(t) : t.Name;
        }

        public static Type GetFieldType(SerializedProperty property)
        {
            var (fi, _) = GetFieldInfoAndAttrs(property);
            
            if (fi == null)
                return null;

            if(fi.FieldType.IsArray)
                return fi.FieldType.GetElementType();
            if(fi.FieldType.IsListLike())
                return fi.FieldType.GetGenericArguments()[0];
            
            return fi.FieldType;
        }

        public static Attribute[] GetTypeAttributes(SerializedProperty property)
        {
            var fieldType = GetFieldType(property);

            return fieldType?.GetCustomAttributes(false).Cast<Attribute>().ToArray() ?? Array.Empty<Attribute>();
        }

        public static Type GetRuntimeType(SerializedProperty property)
        {
            var result = FieldPathResolver.LeafResolver.ResolveLeaf(property);
            return result.ResolvedType;
        }

        // --- PUBLIC ENTRY ---
        public static (FieldInfo field, Attribute[] attrs) GetFieldInfoAndAttrs(SerializedProperty prop)
        {
            return FieldPathResolver.GetLeafFieldAndAttrs(prop);
        }


        internal static class FieldPathResolver
        {
            // Cache by (rootType, propertyPath). If you later add SerializeReference runtime types,
            // extend the key with an SR type key; for your current issue this is enough.
            static readonly Dictionary<(Type root, string path), FieldInfo> _cache = new();

            static readonly HashSet<Type> s_AllowedTypeAttrs = new()
            {
                typeof(System.FlagsAttribute),
                typeof(System.ObsoleteAttribute),
                // add your own type-level attributes here, e.g. typeof(MyCurveWindowAttribute)
            };
            
            public static (FieldInfo field, Attribute[] attrs) GetLeafFieldAndAttrs(SerializedProperty sp)
            {
                var r = LeafResolver.ResolveLeaf(sp);

                if (!r.EndedOnSerializeReferenceSlot && r.LeafField != null)
                {
                    var attrs = r.LeafField.GetCustomAttributes(true).Cast<Attribute>().ToList();
                    
                    
                    foreach (var attribute in r.LeafField.FieldType.GetCustomAttributes(true).Cast<Attribute>())
                    {
                        // quick namespace filter (keeps noise out)
                        if (attribute.GetType().Namespace is { } ns && (
                                ns.StartsWith("System.Runtime.CompilerServices") ||
                                ns.StartsWith("UnityEngine.Scripting") ||
                                ns.StartsWith("Unity.Collections") ||
                                ns.StartsWith("UnityEngine.Bindings")))
                            continue;
                        
                        
                        // whitelist actual type-level attrs you care about
                        if (!s_AllowedTypeAttrs.Contains(attribute.GetType()))
                            continue;
                        
                        attrs.Add(attribute);
                    }
                    
                    return (r.LeafField, attrs.ToArray());
                }

                // ended on SR slot (root SR or SR list element)
                var fieldAttrs = r.SlotField?.GetCustomAttributes(true).Cast<Attribute>() ??
                                 Enumerable.Empty<Attribute>();
                return (r.SlotField, fieldAttrs.ToArray());
            }

            internal static class LeafResolver
            {
                public readonly struct Result
                {
                    public readonly FieldInfo LeafField; // null if path ends at SR slot
                    public readonly FieldInfo SlotField; // SR slot field (only set when path ends at SR)
                    public readonly Type ResolvedType; // final type (runtime past SR, else compile-time)
                    public readonly bool EndedOnSerializeReferenceSlot;

                    public Result(FieldInfo leaf, FieldInfo slot, Type type, bool endedOnSR)
                    {
                        LeafField = leaf;
                        SlotField = slot;
                        ResolvedType = type;
                        EndedOnSerializeReferenceSlot = endedOnSR;
                    }
                }

                public static Result ResolveLeaf(SerializedProperty sp)
                {
                    var target = sp.serializedObject?.targetObject;

                    if (target == null)
                        return new Result(null, null, null, false);

                    var tokens = Tokenize(sp.propertyPath);
                    var currentType = target.GetType();
                    FieldInfo lastField = null;
                    FieldInfo slotField = null;
                    bool endedOnSR = false;
                    string prefix = null;

                    // NEW: track SR-on-list context
                    bool inSrList = false;
                    Type declaredElemType = null; // element declared type for SR list

                    for (int i = 0; i < tokens.Count; i++)
                    {
                        var tok = tokens[i];

                        if (tok.Kind == TokKind.Field)
                        {
                            var fi = FindField(currentType, tok.Name);
                            if (fi == null)
                                return new Result(lastField, slotField, currentType, endedOnSR);

                            lastField = fi;
                            currentType = fi.FieldType;
                            prefix = prefix == null ? tok.Name : $"{prefix}.{tok.Name}";

                            // SR on NON-list: pivot immediately (same as before)
                            if (fi.IsDefined(typeof(SerializeReference), false) && !IsListLike(fi.FieldType))
                            {
                                var srProp = sp.serializedObject.FindProperty(prefix);
                                var rt = GetRuntimeSrType(srProp);
                                if (rt == null)
                                {
                                    endedOnSR = (i == tokens.Count - 1);
                                    return new Result(leaf: null, slot: fi, type: fi.FieldType, endedOnSR: endedOnSR);
                                }

                                currentType = rt;
                                if (i == tokens.Count - 1)
                                    return new Result(leaf: null, slot: fi, type: currentType, endedOnSR: true);
                            }

                            // SR on LIST: don't pivot yet; mark that the next Array.data[k] is a managed reference element
                            if (fi.IsDefined(typeof(SerializeReference), false) && IsListLike(fi.FieldType))
                            {
                                inSrList = true;
                                declaredElemType = GetElementType(fi.FieldType) ?? typeof(object);
                            }
                        }
                        else // Array element
                        {
                            // advance to element declared type
                            currentType = GetElementType(currentType) ?? currentType;
                            prefix = (prefix ?? "") + $".Array.data[{tok.Index}]";

                            if (inSrList)
                            {
                                // This element is a managed reference; pivot to its runtime type
                                var elemProp = sp.serializedObject.FindProperty(prefix);
                                if (elemProp != null &&
                                    elemProp.propertyType == SerializedPropertyType.ManagedReference)
                                {
                                    var rt = GetRuntimeSrType(elemProp);
                                    if (rt != null)
                                    {
                                        currentType = rt;
                                    }
                                    else
                                    {
                                        // element is null; if path ends here, we ended on SR slot (the list element)
                                        if (i == tokens.Count - 1)
                                        {
                                            slotField = lastField; // the list field is the SR slot owner
                                            endedOnSR = true;
                                            return new Result(leaf: null, slot: slotField, type: declaredElemType,
                                                endedOnSR: true);
                                        }

                                        // otherwise we cannot resolve deeper fields on a null element
                                        return new Result(leaf: null, slot: slotField ?? lastField,
                                            type: declaredElemType, endedOnSR: true);
                                    }
                                }

                                // If path ends exactly at the element and it's non-null, report SR slot end with runtime type
                                if (i == tokens.Count - 1)
                                {
                                    slotField = lastField; // the list field
                                    endedOnSR = true;
                                    return new Result(leaf: null, slot: slotField, type: currentType, endedOnSR: true);
                                }

                                // Once we've stepped into the element, we're no longer at the list level
                                inSrList = false;
                            }
                        }
                    }

                    // Path continues past SR element (or no SR at all): lastField is the leaf field
                    return new Result(leaf: lastField, slot: null, type: currentType, endedOnSR: false);
                }

// helpers
                static bool IsListLike(Type t)
                {
                    if (t.IsArray) return true;
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)) return true;
                    return false;
                }

                // --- helpers (same as before) ---
                enum TokKind
                {
                    Field,
                    ArrayElement
                }

                readonly struct Tok
                {
                    public readonly TokKind Kind;
                    public readonly string Name;
                    public readonly int Index;

                    public Tok(string n)
                    {
                        Kind = TokKind.Field;
                        Name = n;
                        Index = -1;
                    }

                    public Tok(int i)
                    {
                        Kind = TokKind.ArrayElement;
                        Name = null;
                        Index = i;
                    }
                }

                static List<Tok> Tokenize(string path)
                {
                    var parts = path.Split('.');
                    var list = new List<Tok>(parts.Length);
                    for (int i = 0; i < parts.Length; i++)
                    {
                        var p = parts[i];
                        if (p == "Array" && i + 1 < parts.Length && parts[i + 1].StartsWith("data["))
                        {
                            var s = parts[++i];
                            int lb = s.IndexOf('[') + 1, rb = s.IndexOf(']', lb);
                            if (int.TryParse(s.Substring(lb, rb - lb), out int idx))
                                list.Add(new Tok(idx));
                            continue;
                        }

                        list.Add(new Tok(p));
                    }

                    return list;
                }

                static FieldInfo FindField(Type t, string name)
                {
                    const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                    for (var c = t; c != null; c = c.BaseType)
                        if (c.GetField(name, BF) is { } f)
                            return f;
                    return null;
                }

                static Type GetElementType(Type t)
                {
                    if (t.IsArray) return t.GetElementType();
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                        return t.GetGenericArguments()[0];
                    return null;
                }

                static Type GetRuntimeSrType(SerializedProperty srProp)
                {
                    if (srProp == null) return null;
                    if (srProp.isArray) return null;
                    var obj = srProp.managedReferenceValue;
                    if (obj != null) return obj.GetType();

                    var s = srProp.managedReferenceFullTypename; // "AsmName Full.Type.Name"
                    if (string.IsNullOrEmpty(s)) return null;
                    int space = s.IndexOf(' ');
                    if (space <= 0 || space >= s.Length - 1) return null;
                    string asm = s.Substring(0, space), full = s.Substring(space + 1);

                    var t = Type.GetType($"{full}, {asm}");
                    if (t != null) return t;
                    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                        if (a.GetName().Name == asm || a.FullName.StartsWith(asm))
                            if ((t = a.GetType(full)) != null)
                                return t;
                    return null;
                }
            }

            static FieldInfo FindFieldRecursive(Type t, string name)
            {
                const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                for (var c = t; c != null; c = c.BaseType)
                    if (c.GetField(name, BF) is { } f)
                        return f;
                return null;
            }

            // ---------- helpers ----------

            enum TokKind
            {
                Field,
                ArrayElement
            }

            readonly struct Tok
            {
                public readonly TokKind Kind;
                public readonly string Name; // for Field
                public readonly int Index; // for ArrayElement

                public Tok(string name)
                {
                    Kind = TokKind.Field;
                    Name = name;
                    Index = -1;
                }

                public Tok(int idx)
                {
                    Kind = TokKind.ArrayElement;
                    Name = null;
                    Index = idx;
                }

                public override string ToString() => Kind == TokKind.Field ? Name : $"[{Index}]";
            }

            static Type GetEnumerableElementType(Type t)
            {
                if (t.IsArray) return t.GetElementType();

                if (t.IsGenericType)
                {
                    var def = t.GetGenericTypeDefinition();
                    if (def == typeof(List<>)) return t.GetGenericArguments()[0];

                    // Optional: support other IList<T>/IEnumerable<T>
                    var ienumT = t.GetInterfaces()
                        .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                    if (ienumT != null) return ienumT.GetGenericArguments()[0];
                }

                return null;
            }
        }


        // Walks the propertyPath: handles nested fields and Array/List elements.
        // Returns the *leaf* FieldInfo and the final owner type we reached.
        // --- PATH RESOLUTION (unchanged logic, now used by the cache) ---
        static (FieldInfo leafField, Type ownerType) ResolveLeafFieeldInfo(Type rootOwner, SerializedProperty prop)
        {
            var tokens = prop.propertyPath.Split('.');
            Type owner = rootOwner;
            FieldInfo lastField = null;

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];

                if (token == "Array")
                {
                    i++; // skip "data[x]"
                    owner = GetEnumerableElementType(owner);
                    continue;
                }

                var fi = GetFieldInTypeHierarchy(owner, token);
                if (fi == null) break;

                lastField = fi;
                owner = fi.FieldType;
            }

            return (lastField, lastField?.DeclaringType ?? rootOwner);
        }

        public static FieldInfo GetFieldInTypeHierarchy(Type t, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            while (t != null)
            {
                var fi = t.GetField(name, flags);
                if (fi != null) return fi;
                t = t.BaseType;
            }

            return null;
        }

        static Type GetEnumerableElementType(Type t)
        {
            if (t.IsArray) return t.GetElementType();
            if (t.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(t))
                return t.GetGenericArguments()[0];
            // Fallback for non-generic IList/etc.
            return typeof(object);
        }

        static string GetFieldNameFromPath(SerializedProperty prop)
        {
            // This returns *the immediate field name* for the leaf node.
            // Example paths:
            //   "myList.Array.data[0].value" => returns "value"
            //   "myStruct.sub.value"         => returns "value"
            string path = prop.propertyPath.Replace(".Array.data[", "[");
            var parts = path.Split('.');
            string last = parts[^1];
            int bracket = last.IndexOf('[');
            return bracket >= 0 ? last.Substring(0, bracket) : last;
        }

        static string PrettyGenericName(Type t)
        {
            var name = t.Name;
            int idx = name.IndexOf('`');
            if (idx >= 0) name = name[..idx];
            var args = t.GetGenericArguments().Select(a => a.IsGenericType ? PrettyGenericName(a) : a.Name);
            return $"{name}<{string.Join(", ", args)}>";
        }
    }
}