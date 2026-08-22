using System;
using UnityEngine;

namespace GrayWolf.Gaze.Collections
{
    /// <summary>
    /// Renames the columns of a <see cref="SerializedDictionary{TKey,TValue}"/> field in the inspector,
    /// and optionally titles each row from a field on the value.
    /// </summary>
    /// <remarks>
    /// Purely cosmetic. A <see cref="SerializedDictionary{TKey,TValue}"/> field draws as a table with or
    /// without this attribute.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializedDictionaryAttribute : PropertyAttribute
    {
        /// <summary>Header text for the key column.</summary>
        public string Key { get; }

        /// <summary>Header text for the value column.</summary>
        public string Value { get; }

        /// <summary>
        /// Name of a serialized field on the value type whose contents title each row, or <c>null</c> to
        /// leave rows untitled.
        /// </summary>
        public string ValueTitleFieldName { get; }

        /// <summary>
        /// Configures how the dictionary table is labelled.
        /// </summary>
        /// <param name="key">Header text for the key column.</param>
        /// <param name="value">Header text for the value column.</param>
        /// <param name="valueTitleFieldName">
        /// Direct field name on the value type, not a dotted path. Naming one draws every row as a
        /// foldout titled by that field, however few fields the value has. A name that does not resolve
        /// leaves rows untitled and logs a warning.
        /// </param>
        public SerializedDictionaryAttribute(
            string key = "Key",
            string value = "Value",
            string valueTitleFieldName = null)
        {
            Key = key;
            Value = value;
            ValueTitleFieldName = valueTitleFieldName;
        }
    }
}
