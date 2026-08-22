using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal static class ElementIdDatabase
    {
        static readonly Dictionary<(string tgt, string listPath), List<string>> _map = new();

        static string GOID(UnityEngine.Object obj)
        {
#if UNITY_2019_2_OR_NEWER
            var goid = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            return goid.ToString();
#else
            var p = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(p)) return "asset:" + AssetDatabase.AssetPathToGUID(p);
            return "iid:" + obj.GetInstanceID();
#endif
        }

        static List<string> EnsureList(SerializedProperty listProp)
        {
            var key = (GOID(listProp.serializedObject.targetObject), listProp.propertyPath);
            if (!_map.TryGetValue(key, out var arr))
            {
                arr = new List<string>(listProp.arraySize);
                for (int i = 0; i < listProp.arraySize; i++) arr.Add(Guid.NewGuid().ToString("N"));
                _map[key] = arr;
            }
            // length sync
            if (arr.Count < listProp.arraySize) { int add = listProp.arraySize - arr.Count; while (add-- > 0) arr.Add(Guid.NewGuid().ToString("N")); }
            else if (arr.Count > listProp.arraySize) { arr.RemoveRange(listProp.arraySize, arr.Count - listProp.arraySize); }
            return arr;
        }

        public static string For(SerializedProperty listProp, int index)
        {
            var arr = EnsureList(listProp);
            if (index < 0 || index >= arr.Count) return "";
            // de-dup on duplicate operations
            if (index > 0 && arr[index] == arr[index - 1]) { arr[index] = Guid.NewGuid().ToString("N"); }
            return arr[index];
        }

        public static void OnReorder(SerializedProperty listProp, int from, int to)
        {
            var arr = EnsureList(listProp);
            
            if (from == to || from < 0 || to < 0 || from >= arr.Count || to >= arr.Count) 
                return;
            
            var uid = arr[from];
            arr.RemoveAt(from);
            arr.Insert(to, uid);
        }

        public static void OnInsert(SerializedProperty listProp, int atIndex)
        {
            var arr = EnsureList(listProp); atIndex = Mathf.Clamp(atIndex, 0, arr.Count);
            arr.Insert(atIndex, Guid.NewGuid().ToString("N"));
        }

        public static void OnDelete(SerializedProperty listProp, int atIndex)
        {
            var arr = EnsureList(listProp);
            
            if (atIndex < 0 || atIndex >= arr.Count)
                return;
            
            arr.RemoveAt(atIndex);
        }

        // helpful when moving across different lists:
        public static string StealUid(SerializedProperty srcList, int from)
        {
            var arr = EnsureList(srcList); if (from < 0 || from >= arr.Count) return null;
            var uid = arr[from]; arr.RemoveAt(from); return uid;
        }
        public static void InsertWithUid(SerializedProperty dstList, int at, string uid)
        {
            var arr = EnsureList(dstList); at = Mathf.Clamp(at, 0, arr.Count);
            arr.Insert(at, uid ?? Guid.NewGuid().ToString("N"));
        }
    }
}
