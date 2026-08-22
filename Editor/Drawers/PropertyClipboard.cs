using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GrayWolf.Gaze.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GrayWolf.Gaze.Editor
{
    // In-editor property clipboard. An in-memory Node graph is the primary storage: exact
    // CLR types, no format to version, no polymorphism-by-type-name problem, and paste
    // compatibility is a direct Type comparison. It does not survive a domain reload, so
    // subtrees that don't involve [SerializeReference] (i.e. round-trip cleanly through
    // Unity's own EditorJsonUtility) are also mirrored into SessionState, which does.
    /// <summary>
    /// The editor-wide clipboard behind the copy/paste entries on a property's right-click menu.
    /// </summary>
    /// <remarks>
    /// One slot, shared across every inspector. Copying captures the whole subtree - nested objects,
    /// collection contents and managed references included - and pasting is only offered when the target
    /// field's declared type matches exactly.
    /// </remarks>
    public static class PropertyClipboard
    {
        private const string SessionKey = "GrayWolf.Gaze.PropertyClipboard";
        private const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private sealed class Node
        {
            public string kind; // "sr" | "val" | "obj" | "arr" | "objref" | "exposedref"
            public Type declaredType; // declared field type; paste compatibility = direct Type equality

            public object value; // for "val": the actual cloned value (Vector3, Color, Gradient, custom struct, ...)

            public Type srType; // for "sr": concrete runtime type of the managed reference (diagnostics)
            public object srValue; // for "sr": the cloned managed reference instance

            public List<Node> children; // for "obj"
            public List<Node> items; // for "arr"

            public Object objectRef; // for "objref"/"exposedref": live reference (in-memory path)
            public string objectGuid; // asset identity, used to resolve after a domain reload
            public ulong objectLocalId;
        }

        // Wraps a SerializeReference value so EditorJsonUtility emits/consumes polymorphic $type info.
        [Serializable]
        private sealed class SRWrap
        {
            [SerializeReference] public object v;
        }

        // Generic wrapper letting EditorJsonUtility (de)serialize an arbitrary Unity-serializable value.
        [Serializable]
        private sealed class Box<T>
        {
            public T v;
        }

        // JSON-friendly mirror of Node, used only for the SessionState fallback.
        [Serializable]
        private sealed class FallbackNode
        {
            public string kind;
            public string declaredTypeAqn;
            public string valueJson; // "val": Box<T> json
            public List<FallbackNode> children;
            public List<FallbackNode> items;
            public string objectGuid;
            public ulong objectLocalId;
        }

        [Serializable]
        private sealed class FallbackEnvelope
        {
            public FallbackNode root;
        }

        private static Node s_clipboard;

        // Public API ------------- //
        // Only AddContextMenu is public: it is what a third-party IContextMenuProvider needs so its
        // menu matches the built-in drawers'. Copy/Paste carry clipboard-format semantics that are
        // still free to change, so they stay internal.

        internal static void Copy(SerializedProperty prop)
        {
            if (prop == null) return;

            s_clipboard = CaptureNode(prop);
            TrySaveFallback(s_clipboard);
        }

        internal static bool CanCopy(SerializedProperty property) => property != null;

        internal static bool CanPaste(SerializedProperty target)
        {
            if (target == null) return false;

            var node = ClipboardOrFallback();
            return node != null && GetDeclaredFieldType(target) == node.declaredType;
        }

        internal static void Paste(SerializedProperty target)
        {
            if (target == null) return;

            var node = ClipboardOrFallback();
            if (node == null)
            {
                Debug.LogWarning($"[Gaze] {nameof(PropertyClipboard)} | Clipboard is empty.");
                return;
            }

            Undo.RecordObject(target.serializedObject.targetObject, "Paste Property");
            ApplyNode(target, node);
            target.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Appends copy and paste entries for <paramref name="property"/> to a menu, so a custom
        /// right-click menu offers the same clipboard behaviour as the built-in drawers.
        /// </summary>
        /// <param name="menu">Menu to append to. A null menu is a no-op.</param>
        /// <param name="property">Property the entries act on. A null property is a no-op.</param>
        /// <param name="copyText">Label for the copy entry.</param>
        /// <param name="pasteText">Label for the paste entry.</param>
        /// <remarks>
        /// Both entries are always added, greyed out when unavailable, so the menu keeps a stable shape.
        /// Paste is greyed out unless the clipboard holds a value of the same declared field type.
        /// Pasting registers an undo step on the target object.
        /// </remarks>
        /// <seealso cref="IContextMenuProvider"/>
        public static void AddContextMenu(
            GenericMenu menu,
            SerializedProperty property,
            string copyText = "Copy",
            string pasteText = "Paste")
        {
            if (menu == null || property == null)
                return;

            if (CanCopy(property))
                menu.AddItem(new GUIContent(copyText), false, () => Copy(property));
            else
                menu.AddDisabledItem(new GUIContent(copyText), false);

            if (CanPaste(property))
                menu.AddItem(new GUIContent(pasteText), false, () => Paste(property));
            else
                menu.AddDisabledItem(new GUIContent(pasteText), false);
        }

        // --------- Capture (Copy side) --------- //

        private static Node CaptureNode(SerializedProperty p)
        {
            var declaredType = GetDeclaredFieldType(p);
            var n = new Node { declaredType = declaredType };

            if (IsManagedReference(p))
            {
                n.kind = "sr";
                var v = p.managedReferenceValue;
                n.srType = v?.GetType();
                n.srValue = CloneManagedReference(v);
                return n;
            }

            if (p.isArray && p.propertyType != SerializedPropertyType.String)
            {
                n.kind = "arr";
                n.items = new List<Node>(p.arraySize);
                for (var i = 0; i < p.arraySize; i++)
                    n.items.Add(CaptureNode(p.GetArrayElementAtIndex(i)));
                return n;
            }

            switch (p.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    n.kind = "objref";
                    CaptureObjectRef(n, p.objectReferenceValue);
                    return n;

                case SerializedPropertyType.ExposedReference:
                    n.kind = "exposedref";
                    CaptureObjectRef(n, p.exposedReferenceValue);
                    return n;

                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.String:
                case SerializedPropertyType.Color:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Rect:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.AnimationCurve:
                case SerializedPropertyType.Bounds:
                case SerializedPropertyType.Gradient:
                case SerializedPropertyType.Quaternion:
                case SerializedPropertyType.FixedBufferSize:
                case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.RectInt:
                case SerializedPropertyType.BoundsInt:
                case SerializedPropertyType.Hash128:
                {
                    n.kind = "val";
                    var fieldType = declaredType ?? GuessTypeFromProperty(p);
                    n.value = CloneValue(ReadValue(p, fieldType), fieldType);
                    return n;
                }

                case SerializedPropertyType.Generic:
                default:
                {
                    // Nested non-SR object => walk children via the iterator API, not reflection.
                    n.kind = "obj";
                    n.children = new List<Node>();
                    foreach (var child in p.Children())
                        n.children.Add(CaptureNode(child));
                    return n;
                }
            }
        }

        private static void CaptureObjectRef(Node n, Object obj)
        {
            n.objectRef = obj;
            if (!obj) return;

            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) return; // scene object - no stable fallback identity

            n.objectGuid = AssetDatabase.AssetPathToGUID(path);
            n.objectLocalId = (ulong)Unsupported.GetLocalIdentifierInFileForPersistentObject(obj);
        }

        // --------- Apply (Paste side) --------- //

        private static void ApplyNode(SerializedProperty target, Node n)
        {
            switch (n.kind)
            {
                case "sr":
                    target.managedReferenceValue = CloneManagedReference(n.srValue);
                    return;

                case "arr":
                    target.arraySize = 0;
                    foreach (var item in n.items)
                    {
                        target.InsertArrayElementAtIndex(target.arraySize);
                        ApplyNode(target.GetArrayElementAtIndex(target.arraySize - 1), item);
                    }
                    return;

                case "objref":
                case "exposedref":
                {
                    var obj = ResolveObjectRef(n);
                    if (target.propertyType == SerializedPropertyType.ObjectReference)
                        target.objectReferenceValue = obj;
                    else
                        target.exposedReferenceValue = obj;
                    return;
                }

                case "obj":
                {
                    // Unity doesn't expose field names robustly here; children are consumed in order.
                    var children = target.Children().ToArray();
                    for (var i = 0; i < children.Length && i < n.children.Count; i++)
                        ApplyNode(children[i], n.children[i]);
                    return;
                }

                case "val":
                default:
                {
                    var fieldType = n.declaredType ?? GuessTypeFromProperty(target);
                    WriteValue(target, CloneValue(n.value, fieldType), fieldType);
                    return;
                }
            }
        }

        private static Object ResolveObjectRef(Node n)
        {
            if (n.objectRef) return n.objectRef;
            if (string.IsNullOrEmpty(n.objectGuid)) return null;

            if (n.objectLocalId != 0)
                return FindAssetObject(n.objectGuid, (long)n.objectLocalId);

            var path = AssetDatabase.GUIDToAssetPath(n.objectGuid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Object>(path);
        }

        // Finds an asset or sub-asset by GUID + local file id.
        private static Object FindAssetObject(string guid, long localId)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;

            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!obj) continue;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var g, out long lid)
                    && g == guid && lid == localId)
                    return obj;
            }
            return null;
        }

        // --------- Cloning (protects the clipboard from aliasing live SerializedObject data) --------- //

        private static object CloneValue(object value, Type type)
        {
            if (value == null || type == null) return value;
            return ValueFromJson(ValueToJson(value, type), type);
        }

        private static object CloneManagedReference(object value)
        {
            if (value == null) return null;
            var json = EditorJsonUtility.ToJson(new SRWrap { v = value });
            var clone = new SRWrap();
            EditorJsonUtility.FromJsonOverwrite(json, clone);
            return clone.v;
        }

        private static string ValueToJson(object value, Type type)
        {
            var boxType = typeof(Box<>).MakeGenericType(type);
            var box = Activator.CreateInstance(boxType);
            boxType.GetField("v", BF).SetValue(box, value);
            return EditorJsonUtility.ToJson(box);
        }

        private static object ValueFromJson(string json, Type type)
        {
            var boxType = typeof(Box<>).MakeGenericType(type);
            var box = Activator.CreateInstance(boxType);
            EditorJsonUtility.FromJsonOverwrite(json, box);
            return boxType.GetField("v", BF).GetValue(box);
        }

        // --------- SessionState fallback (survives domain reload) --------- //

        private static Node ClipboardOrFallback()
        {
            if (s_clipboard != null) return s_clipboard;
            var loaded = TryLoadFallback();
            if (loaded != null) s_clipboard = loaded; // memoize for the rest of this domain's lifetime
            return loaded;
        }

        private static bool IsFallbackSafe(Node n)
        {
            switch (n.kind)
            {
                case "sr": return false; // polymorphic - only the in-memory path can carry this
                case "arr": return n.items.All(IsFallbackSafe);
                case "obj": return n.children.All(IsFallbackSafe);
                case "objref":
                case "exposedref": return true;
                default: return n.declaredType != null; // "val" needs a concrete type to round-trip
            }
        }

        private static void TrySaveFallback(Node node)
        {
            if (!IsFallbackSafe(node))
            {
                SessionState.EraseString(SessionKey);
                return;
            }

            var envelope = new FallbackEnvelope { root = ToFallback(node) };
            SessionState.SetString(SessionKey, EditorJsonUtility.ToJson(envelope));
        }

        private static Node TryLoadFallback()
        {
            var json = SessionState.GetString(SessionKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                var envelope = new FallbackEnvelope();
                EditorJsonUtility.FromJsonOverwrite(json, envelope);
                return envelope.root == null ? null : FromFallback(envelope.root);
            }
            catch
            {
                return null;
            }
        }

        private static FallbackNode ToFallback(Node n)
        {
            var f = new FallbackNode
            {
                kind = n.kind,
                declaredTypeAqn = n.declaredType?.AssemblyQualifiedName
            };

            switch (n.kind)
            {
                case "arr":
                    f.items = n.items.Select(ToFallback).ToList();
                    break;
                case "obj":
                    f.children = n.children.Select(ToFallback).ToList();
                    break;
                case "objref":
                case "exposedref":
                    f.objectGuid = n.objectGuid;
                    f.objectLocalId = n.objectLocalId;
                    break;
                default:
                    f.valueJson = ValueToJson(n.value, n.declaredType);
                    break;
            }

            return f;
        }

        private static Node FromFallback(FallbackNode f)
        {
            var declaredType = string.IsNullOrEmpty(f.declaredTypeAqn) ? null : Type.GetType(f.declaredTypeAqn);
            var n = new Node { kind = f.kind, declaredType = declaredType };

            switch (f.kind)
            {
                case "arr":
                    n.items = f.items.Select(FromFallback).ToList();
                    break;
                case "obj":
                    n.children = f.children.Select(FromFallback).ToList();
                    break;
                case "objref":
                case "exposedref":
                    n.objectGuid = f.objectGuid;
                    n.objectLocalId = f.objectLocalId;
                    break;
                default:
                    n.value = declaredType != null && !string.IsNullOrEmpty(f.valueJson)
                        ? ValueFromJson(f.valueJson, declaredType)
                        : null;
                    break;
            }

            return n;
        }

        // --------- Shared helpers --------- //

        private static bool IsManagedReference(SerializedProperty p) =>
            p.propertyType == SerializedPropertyType.ManagedReference;

        private static Type GetDeclaredFieldType(SerializedProperty prop) =>
            ReflectionCache.GetFieldInfoAndAttrs(prop).field.FieldType;

        private static Type GuessTypeFromProperty(SerializedProperty property)
        {
            return property.propertyType switch
            {
                SerializedPropertyType.Integer => typeof(int),
                SerializedPropertyType.Boolean => typeof(bool),
                SerializedPropertyType.Float => typeof(float),
                SerializedPropertyType.String => typeof(string),
                SerializedPropertyType.Color => typeof(Color),
                SerializedPropertyType.LayerMask => typeof(LayerMask),
                SerializedPropertyType.Enum => ReflectionCache.GetFieldType(property),
                SerializedPropertyType.Vector2 => typeof(Vector2),
                SerializedPropertyType.Vector3 => typeof(Vector3),
                SerializedPropertyType.Vector4 => typeof(Vector4),
                SerializedPropertyType.Rect => typeof(Rect),
                SerializedPropertyType.ArraySize => typeof(int),
                SerializedPropertyType.Character => typeof(string),
                SerializedPropertyType.AnimationCurve => typeof(AnimationCurve),
                SerializedPropertyType.Bounds => typeof(Bounds),
                SerializedPropertyType.Gradient => typeof(Gradient),
                SerializedPropertyType.Quaternion => typeof(Quaternion),
                SerializedPropertyType.FixedBufferSize => typeof(int),
                SerializedPropertyType.Vector2Int => typeof(Vector2Int),
                SerializedPropertyType.Vector3Int => typeof(Vector3Int),
                SerializedPropertyType.RectInt => typeof(RectInt),
                SerializedPropertyType.BoundsInt => typeof(BoundsInt),
                SerializedPropertyType.Hash128 => typeof(Hash128),
                _ => throw new ArgumentOutOfRangeException(nameof(property), $"Invalid Property Type: {property.propertyType}")
            };
        }

        // Value IO for non-SR leaf properties using the SerializedProperty API (covers Unity's built-in types)
        private static object ReadValue(SerializedProperty p, Type t) =>
            p.propertyType == SerializedPropertyType.Character ? p.GetValue().ToString() : p.GetValue();

        private static void WriteValue(SerializedProperty p, object value, Type t)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer:          p.intValue = Convert.ToInt32(value); break;
                case SerializedPropertyType.Boolean:          p.boolValue = Convert.ToBoolean(value); break;
                case SerializedPropertyType.Float:            p.floatValue = Convert.ToSingle(value); break;
                case SerializedPropertyType.String:           p.stringValue = value as string ?? string.Empty; break;
                case SerializedPropertyType.Enum:             p.boxedValue = EnumExtensions.NormalizeToEnum(t, value); break;
                case SerializedPropertyType.Color:            p.colorValue = (Color)value; break;
                case SerializedPropertyType.Vector2:          p.vector2Value = (Vector2)value; break;
                case SerializedPropertyType.Vector3:          p.vector3Value = (Vector3)value; break;
                case SerializedPropertyType.Vector4:          p.vector4Value = (Vector4)value; break;
                case SerializedPropertyType.Quaternion:       p.quaternionValue = (Quaternion)value; break;
                case SerializedPropertyType.Rect:             p.rectValue = (Rect)value; break;
                case SerializedPropertyType.Bounds:           p.boundsValue = (Bounds)value; break;
                case SerializedPropertyType.AnimationCurve:   p.animationCurveValue = (AnimationCurve)value; break;
                case SerializedPropertyType.ObjectReference:  p.objectReferenceValue = value as Object; break;
                case SerializedPropertyType.ExposedReference: p.exposedReferenceValue = value as Object; break;
                case SerializedPropertyType.LayerMask:        p.intValue = ((LayerMask)value).value; break;
                case SerializedPropertyType.ArraySize:        p.arraySize = Convert.ToInt32(value); break;
                case SerializedPropertyType.Character:        p.intValue = Convert.ToInt32((value as string)?.FirstOrDefault() ?? default); break;
                case SerializedPropertyType.Gradient:         p.gradientValue = (Gradient)value; break;
                case SerializedPropertyType.Vector2Int:       p.vector2IntValue = (Vector2Int)value; break;
                case SerializedPropertyType.Vector3Int:       p.vector3IntValue = (Vector3Int)value; break;
                case SerializedPropertyType.RectInt:          p.rectIntValue = (RectInt)value; break;
                case SerializedPropertyType.BoundsInt:        p.boundsIntValue = (BoundsInt)value; break;
                case SerializedPropertyType.Hash128:          p.hash128Value = (Hash128)value; break;
                default:
                    throw new NotSupportedException($"Unsupported write type: {p.propertyType}");
            }
        }
    }
}
