using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// Non-generic base for value drawers. Derive from <see cref="PropertyDrawer{T}"/> instead - only
    /// the generic form carries the field type that registration keys on.
    /// </summary>
    public abstract class PropertyDrawerBase : DrawerBase
    {
        internal bool TryGetAdditionalData<T>(out T data) where T : ContextItem
        {
            data = null;
            return Data?.AdditionalData?.TryGet(out data) ?? false;
        }

        /// <summary>
        /// Indented one-line summary of the property path, its serialized type and its field type, for
        /// debugging a tree.
        /// </summary>
        /// <returns>A diagnostic description of this drawer's property.</returns>
        public override string ToString() => $"{new string('•', Mathf.Max(0, Property.depth))} {Property?.propertyPath} [{Property?.propertyType}] : [{Data.TypeName}]";
    }
}