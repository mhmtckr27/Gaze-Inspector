using UnityEngine;
using UnityEditor;

namespace GrayWolf.Gaze.Editor
{
    // -----------------------------
    // Drawer interface
    // -----------------------------
    /// <summary>
    /// The one thing every node in a <see cref="PropertyTree"/> can do: draw itself.
    /// </summary>
    /// <remarks>
    /// Implement <see cref="PropertyDrawer{T}"/> or <see cref="AttributeDrawer{T}"/> rather than this
    /// interface directly - only their subclasses are discovered and registered.
    /// </remarks>
    public interface INodeDrawer
    {
        /// <summary>
        /// Emits the GUI for this node using Unity's layout system.
        /// </summary>
        /// <param name="label">
        /// Label to draw the node with. May be <see cref="GUIContent.none"/> for a node whose label is
        /// supplied by its container, such as a list element.
        /// </param>
        void Draw(GUIContent label);
    }
}