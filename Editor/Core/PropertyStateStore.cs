using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal static class PropertyStateStore
    {
        private sealed class PerTarget
        {
            public readonly Dictionary<(string canonicalPath, Type t, string sub), object> map = new(512);
        }

        private static readonly ConditionalWeakTable<UnityEngine.Object, PerTarget> _byTarget = new();

        public static FoldoutAnimState GetFoldoutAnimState(SerializedProperty prop)
        { 
            var st = Get<FoldoutAnimState>(prop);

            if (st == null)
            {
                st = GetOrCreate<FoldoutAnimState>(prop, out _);
                st.expanded = prop.isExpanded;
            }
            
            if (Mathf.Approximately(st.value, -1f))
            {
                st.value = st.expanded ? 1f : 0f;
            }
            
            return st;
        }
        
        public static T Get<T>(SerializedProperty prop, string subKey = "") where T : class, new()
        {
            var target = prop.serializedObject.targetObject;
            var per = _byTarget.GetValue(target, _ => new PerTarget());
            string cpath = CanonicalPath.Canonical(prop);
            var key = (cpath, typeof(T), subKey ?? "");
            if (!per.map.TryGetValue(key, out var o))
                return null;
            
            return (T)o;
        }
        
        public static T GetOrCreate<T>(SerializedProperty prop, out bool isNew, string subKey = "") where T : class, new()
        {
            var target = prop.serializedObject.targetObject;
            var per = _byTarget.GetValue(target, _ => new PerTarget());
            string cpath = CanonicalPath.Canonical(prop);
            var key = (cpath, typeof(T), subKey ?? "");
            if (!per.map.TryGetValue(key, out var o))
            {
                per.map[key] = o = new T();
                isNew = true;
            }
            else
            {
                isNew = false;
            }
            return (T)o;
        }
        
        // Optional: migrate when moving an element across different lists (listPath changes)
        public static void MigrateUidBranch(UnityEngine.Object target, string oldListPath, string newListPath, string uid)
        {
            if (!_byTarget.TryGetValue(target, out var per)) return;
            var oldPrefix = CanonicalForListUid(oldListPath, uid);
            var newPrefix = CanonicalForListUid(newListPath, uid);

            var toMove = new List<KeyValuePair<(string, Type, string), object>>();
            foreach (var kv in per.map)
                if (kv.Key.Item1.StartsWith(oldPrefix, StringComparison.Ordinal))
                    toMove.Add(kv);

            foreach (var kv in toMove)
            {
                per.map.Remove(kv.Key);
                var newPath = newPrefix + kv.Key.Item1.Substring(oldPrefix.Length);
                per.map[(newPath, kv.Key.Item2, kv.Key.Item3)] = kv.Value;
            }

            static string CanonicalForListUid(string listPath, string uid)
                => string.IsNullOrEmpty(uid) ? listPath : $"{listPath}.Array.uid[{uid}]";
        }
    }
}
