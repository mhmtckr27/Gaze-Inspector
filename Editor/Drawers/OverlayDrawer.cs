using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GrayWolf.Gaze.Editor
{
    internal static class OverlayDrawer
    {
        private struct Item { public int Z; public Action Draw; }

        // Key by GUIView id
        static readonly List<Item> _byView = new();

        static readonly PropertyInfo _pGUIViewCurrent =
            Type.GetType("UnityEditor.GUIView, UnityEditor")
                ?.GetProperty("current", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        private static ulong CurrentViewId()
        {
            var gv = _pGUIViewCurrent?.GetValue(null) as ScriptableObject;
            return gv ? ObjectId.Of(gv) : 0UL;
        }

        private static RenderTexture _prev;
        private static RenderTexture _current;
        private static Vector2 _pos;
        private static EventType curr;
        private static Material material;

        public static void Begin(Vector2 size)
        {
            UnityEngine.EventType type = Event.current.type;
            int num;
            switch (type)
            {
                case UnityEngine.EventType.Repaint:
                case UnityEngine.EventType.Layout:
                case UnityEngine.EventType.Used:
                    num = (int) type;
                    break;
                default:
                    num = 11;
                    break;
            }
            curr=Event.current.type;
            Event.current.type = (EventType)num;


            GUILayout.BeginArea(new Rect(_pos, size), EditorGuiStyles.None);

            if(Event.current.type != EventType.Repaint)
                return;
                
            _prev = RenderTexture.active;
            if (_current != null)
                RenderTexture.ReleaseTemporary(_current);
            
            _current = RenderTexture.GetTemporary(Screen.width, Screen.height , 0);
            RenderTexture.active = _current;
            GL.Clear(false, true, Color.clear);
        }

        public static void End()
        {
            if (Event.current.type == EventType.Repaint)
                RenderTexture.active = _prev;
            GUILayout.EndArea();
            Event.current.type = (EventType) curr;
        }
        
        public static void Draw(Vector2 position)
        {
            if (Event.current.type != EventType.Layout)
                _pos = position;
            if (Event.current.type != EventType.Repaint)
                return;
            if ((Object) material == (Object) null)
                material = new Material(Shader.Find("Unlit/Transparent"));
            if (!(_current != null))
                return;
            Graphics.Blit(_current, RenderTexture.active, material);
            RenderTexture.ReleaseTemporary(_current);
            _current = (RenderTexture) null;
        }
        
        public static void Push(Action draw, int z = 0)
        {
            if (draw == null) 
                return;
            
            _byView.Add(new Item { Z = z, Draw = draw });            
        }

        // Flush for the *current* GUIView (called from the Inspector overlay host)
        public static void Flush()
        {
            // if (Event.current.type != EventType.Repaint) return;    // do nothing on non-Repaint
            
            if (_byView.Count > 1) 
                _byView.Sort((a, b) => a.Z.CompareTo(b.Z)); // higher Z last/on top
            
                
            for (var i = 0; i < _byView.Count; i++)
            {
                _byView[i].Draw();
            }
            
            _byView.Clear();
        }
    }
}