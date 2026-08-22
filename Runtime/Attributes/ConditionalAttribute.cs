using System;
using UnityEngine;

namespace GrayWolf.Gaze
{
    /// <summary>
    /// Base for any attribute that shows, hides, enables or disables a field
    /// based on the value of a member bool or a bool-returning function.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A condition may name a serialized field, a property, or a parameterless
    /// method returning bool. The member may be private, static, or declared on a base type.
    /// Prefix a name with '!' to negate, for example "!useCustomRate".
    /// </para>
    /// <para>
    /// Resolution starts at the field's own container and walks up to the serialized object root, so a
    /// condition inside a nested struct or a list element resolves against its immediate neighbours
    /// first. An unresolvable name leaves the field visible and enabled and reports the problem in the
    /// inspector rather than throwing.
    /// </para>
    /// <para>
    /// Supplying an <see cref="OptionalValue"/> switches from "is this bool true" to "does this member
    /// equal that value", which is what allows a condition on an enum or a number.
    /// </para>
    /// </remarks>
    public abstract class ConditionalAttribute : PropertyAttribute
    {
        /// <summary>
        /// Name of the member to test, optionally prefixed with '!' to negate the result.
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// Value that <see cref="Condition"/> is compared against when <see cref="HasOptionalValue"/>
        /// is set. Ignored otherwise.
        /// </summary>
        public object OptionalValue { get; set; }

        /// <summary>
        /// True when the attribute was constructed with a comparison value, which selects equality
        /// testing over plain bool testing. Set by the constructor, not by the user.
        /// </summary>
        public bool HasOptionalValue { get; }

        /// <summary>
        /// Inverts the combined result. Set by the attribute subclass, not by
        /// the user. HideIf is ShowIf with this set to true.
        /// </summary>
        public abstract bool Invert { get; }

        /// <summary>
        /// Tests <paramref name="condition"/> as a bool.
        /// </summary>
        /// <param name="condition">Member name, optionally prefixed with '!'.</param>
        protected ConditionalAttribute(string condition)
        {
            Condition = condition;
        }

        /// <summary>
        /// Tests whether <paramref name="condition"/> equals <paramref name="optionalValue"/>.
        /// </summary>
        /// <param name="condition">Member name, optionally prefixed with '!' to negate the comparison.</param>
        /// <param name="optionalValue">Value to compare against. Typically an enum member or a number.</param>
        protected ConditionalAttribute(string condition, object optionalValue)
        {
            Condition = condition;
            OptionalValue = optionalValue;
            HasOptionalValue = true;
        }
    }
}