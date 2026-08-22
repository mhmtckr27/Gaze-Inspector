using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// Common base for everything that draws a node of a <see cref="PropertyTree"/>.
    /// </summary>
    /// <remarks>
    /// Derive from <see cref="PropertyDrawer{T}"/> to draw a field type, or from
    /// <see cref="AttributeDrawer{T}"/> to decorate whatever drawer an attribute is applied over.
    /// Deriving from this class directly produces a drawer nothing will ever resolve to.
    /// </remarks>
    public abstract class DrawerBase : INodeDrawer
    {
        internal PropertyTree Tree;
        internal DrawerBase Parent;
        internal string PropertyPath;
        internal SerializedObject so;

        /// <summary>
        /// The property this drawer is bound to, re-resolved from its path on each access so it stays
        /// valid across the serialized object being rebuilt. Null once the target object is gone.
        /// </summary>
        protected internal SerializedProperty Property => so?.FindProperty(PropertyPath);

        /// <summary>
        /// Drawers for this property's child properties, built on demand. Empty for a primitive.
        /// </summary>
        /// <remarks>
        /// A fresh list is built per access, so cache it in a local rather than indexing it repeatedly
        /// inside a draw loop.
        /// </remarks>
        protected internal List<DrawerBase> Children => Tree.BuildChildrenNodes(Property);

        /// <inheritdoc/>
        public abstract void Draw(GUIContent label);

        internal virtual PropertyData Data { get; set; }

        private string _messageScope;
        internal string MessageScope => _messageScope ??= GazeMessages.ScopeOf(PropertyPath);

        /// <summary>
        /// Contributes message boxes to be drawn above this property. Override to report validation
        /// problems without drawing them yourself.
        /// </summary>
        /// <param name="messages">Buffer to append to. Owned by the caller; do not retain it.</param>
        /// <remarks>
        /// <para>
        /// Called immediately before <see cref="Draw"/> on every OnGUI pass, Layout and Repaint alike, so
        /// an override must append the same messages in both or Unity will report a layout mismatch.
        /// </para>
        /// <para>
        /// Call <c>base.CollectMessages(messages)</c> unless you specifically mean to suppress the rest
        /// of the chain - for a decorator, the base call is what forwards to the drawer being decorated,
        /// and skipping it silently drops every message below this one.
        /// </para>
        /// <para>
        /// Deduplication and severity ordering are handled for you, and messages are collapsed per list
        /// rather than per element, so the same message on every element of a list is drawn once.
        /// </para>
        /// </remarks>
        protected internal virtual void CollectMessages(List<GazeMessage> messages) { }

        internal void InitializeInternal(SerializedProperty property)
        {
            so = property.serializedObject;
            PropertyPath = property.propertyPath;
            SetDataFrom(this);
            Initialize();
        }
                
        internal void SetDataFrom(DrawerBase drawer)
        {
            Data = Data?.From(drawer) ?? PropertyData.Create(drawer);
        }
        
        /// <summary>
        /// Runs once after the drawer is bound to its property, before the first
        /// <see cref="Draw"/>. Override to resolve attributes and cache anything derived from the
        /// property that will not change.
        /// </summary>
        /// <remarks>
        /// <see cref="Property"/> and <see cref="AttributeDrawerBase.Attribute"/> are both usable here.
        /// The tree is built from inside <c>OnInspectorGUI</c>, so a GUI skin is current and named
        /// <c>GUIStyle</c> lookups resolve.
        /// </remarks>
        protected internal virtual void Initialize() { }
    }
}