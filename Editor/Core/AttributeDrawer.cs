using System;

namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// Base class for a drawer that reacts to attribute <typeparamref name="T"/> on a field. Subclasses
    /// are discovered and registered automatically; there is nothing to call to install one.
    /// </summary>
    /// <typeparam name="T">The attribute type this drawer handles.</typeparam>
    /// <remarks>
    /// <para>
    /// An attribute drawer wraps whatever drawer would otherwise have rendered the field, reached
    /// through <see cref="AttributeDrawerBase.Inner"/>. That makes it a decorator: it can draw around
    /// the field, change the GUI state the field is drawn in, replace its label, or skip it entirely.
    /// A <see cref="DrawerBase.Draw"/> override that never calls <c>Inner.Draw</c> hides the property.
    /// </para>
    /// <para>
    /// The attribute instance is available as <see cref="AttributeDrawerBase.Attribute"/> from
    /// <see cref="DrawerBase.Initialize"/> onwards. Two drawers declaring the same
    /// <typeparamref name="T"/> is a conflict, and the first one found wins.
    /// </para>
    /// <para>
    /// Tagging a field with an attribute that has no drawer hands the whole property back to Unity's
    /// default rendering, so an attribute meant purely as configuration still needs a drawer, even an
    /// empty one that just forwards to <see cref="AttributeDrawerBase.Inner"/>.
    /// </para>
    /// </remarks>
    public abstract class AttributeDrawer<T> : AttributeDrawerBase where T : Attribute
    {
        static AttributeDrawer()
        {
            var attributeUsageAttribute = typeof(T).GetAttribute<AttributeUsageAttribute>();
            
            if(attributeUsageAttribute == null)
                return;

            AllowMultiple = attributeUsageAttribute.AllowMultiple;
            Inherited = attributeUsageAttribute.Inherited;
            Targets = attributeUsageAttribute.ValidOn;
        }
    }
}