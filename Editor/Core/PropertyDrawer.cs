
namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// Base class for a drawer that renders fields of type <typeparamref name="T"/>. Subclasses are
    /// discovered and registered automatically; there is nothing to call to install one.
    /// </summary>
    /// <typeparam name="T">
    /// The declared field type this drawer renders. For an array or <see cref="System.Collections.Generic.List{T}"/>
    /// field this is the <em>element</em> type - the collection itself keeps the built-in list drawer,
    /// and each element is drawn by this one.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// Matching is by exact type. <c>PropertyDrawer&lt;Base&gt;</c> does not draw a field declared as a
    /// subclass of <c>Base</c>, and it does not match through an interface.
    /// </para>
    /// <para>
    /// A Unity <c>[CustomPropertyDrawer]</c> registered for the same type takes precedence, and the
    /// property falls back to Unity's own drawing. Two drawers declaring the same
    /// <typeparamref name="T"/> is a conflict: the first one found wins, and the collision is logged.
    /// </para>
    /// <para>
    /// Override <see cref="DrawerBase.Draw"/> to render the field, and
    /// <see cref="DrawerBase.Initialize"/> to cache anything derived from the property.
    /// </para>
    /// </remarks>
    public abstract class PropertyDrawer<T> : PropertyDrawerBase
    {
    }
}