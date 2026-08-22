using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GrayWolf.Gaze.Editor
{
    // -----------------------------
    // Property Tree (builder)
    // -----------------------------
    /// <summary>
    /// The resolved drawer tree for one <see cref="SerializedObject"/>: one root node per top-level
    /// serialized property, each wrapped by the drawers its attributes call for.
    /// </summary>
    /// <remarks>
    /// Build one and hand it to <see cref="PropertyTreeGUI.DrawTree"/>. Construction resolves drawers
    /// and so must happen inside a GUI callback, not in <c>OnEnable</c>.
    /// </remarks>
    public sealed class PropertyTree
    {
        internal readonly List<DrawerBase> Roots = new();
        internal readonly SerializedObject SerializedObject;
        internal readonly SerializationMessages Serialization = new();
        internal readonly DrawerRegistry Registry;
        internal bool ContextMenuOpen;
        internal Rect ContextMenuPropArea;
        internal GenericMenu ContextMenu;
        internal int ContextMenuOpenCounter;

        internal bool HasContent => SerializedObject != null;

        /// <summary>
        /// Resolves a drawer for every serialized property of <paramref name="so"/>.
        /// </summary>
        /// <param name="so">The object to build a tree for.</param>
        /// <remarks>
        /// Building while Unity's object picker is open yields an empty tree, because the picker holds
        /// state that makes drawer resolution unreliable. Treat construction as something that can
        /// produce nothing and retry on a later frame rather than caching the result forever.
        /// </remarks>
        public PropertyTree(SerializedObject so)
        {
            Registry = new DrawerRegistry(this);
            if(IsObjectSelectorOpen())
                return;
            SerializedObject = so;
            Build();
        }

        private void Build()
        {
            Roots.Clear();
            if (SerializedObject == null) return;

            foreach (var root in SerializedObject.Roots())
            {
                Roots.Add(BuildNode(root));
            }
        }
        
        static bool IsObjectSelectorOpen()
        {
            var t = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.ObjectSelector");
            if (t == null) return false;
            // If any instance exists, the picker is open (or opening)
            return Resources.FindObjectsOfTypeAll(t).Length > 0;
        }

        internal void RebuildNode(DrawerBase drawer)
        {
            if (SerializedObject == null) 
                return;
            
            var parent = drawer.Parent;
            var newNode = BuildNode(drawer.Property);
            newNode.Parent = parent;
        }
        
        internal DrawerBase BuildNode(SerializedProperty property)
        {
            var drawer = Registry.Resolve(property);

            drawer.InitializeInternal(property);

            return drawer;
        }

        internal List<DrawerBase> BuildChildrenNodes(SerializedProperty property)
        {
            var drawer = Registry.Resolve(property);

            if (drawer.Property.IsPrimitive())
                return new List<DrawerBase>();
            
            var result = new List<DrawerBase>();

            // Only a dictionary hides its backing arrays, so resolve those paths once and only when they
            // can actually match - this used to run two FindPropertyRelative calls per child of every
            // property in the inspector, discarding both for anything that wasn't a dictionary.
            var isDictionary = drawer is SerializedDictionaryDrawer;
            var keyPath = isDictionary ? property.GetKeyData()?.propertyPath : null;
            var valuePath = isDictionary ? property.GetValueData()?.propertyPath : null;

            foreach (var child in drawer.Property.Children())
            {
                if(child.propertyType == SerializedPropertyType.ArraySize)
                    continue;

                if (isDictionary &&
                    (string.Equals(keyPath, child.propertyPath) || string.Equals(valuePath, child.propertyPath)))
                    continue;

                var childDrawer = Registry.Resolve(child);
                childDrawer.Parent = drawer;
                result.Add(childDrawer);
            }

            return result;
        }
    }
}