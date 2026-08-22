using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// Implemented by a drawer that wants to serve its own right-click menu instead of the default
    /// copy/paste one.
    /// </summary>
    /// <remarks>
    /// A drawer implementing this is registered automatically when the tree resolves it. Providers are
    /// consulted in registration order and the first whose <see cref="ContextClickArea"/> contains the
    /// pointer wins, so overlapping areas resolve to whichever drawer was built first.
    /// </remarks>
    public interface IContextMenuProvider
    {
        /// <summary>
        /// Region that counts as a right-click on this drawer, in the inspector's GUI space. Usually the
        /// label row, or the toolbar for a collection.
        /// </summary>
        /// <remarks>
        /// Rects are only meaningful once laid out, so assign this during a Repaint pass and leave it
        /// alone otherwise. A default rect never matches and simply means "no menu here".
        /// </remarks>
        Rect ContextClickArea { get; }

        /// <summary>
        /// Builds the menu to show. Called on the click, not on the frame the menu opens.
        /// </summary>
        /// <param name="highlightArea">
        /// Region to flash while the menu is open. Set it to the whole block the menu applies to, which
        /// for a foldout or a list is wider than <see cref="ContextClickArea"/>.
        /// </param>
        /// <returns>
        /// The menu to show, or <c>null</c> to swallow the click without showing anything.
        /// </returns>
        /// <remarks>
        /// Call <see cref="PropertyClipboard.AddContextMenu"/> to get the same copy/paste entries the
        /// built-in drawers offer, then add your own.
        /// </remarks>
        GenericMenu Provide(out Rect highlightArea);
    }
}