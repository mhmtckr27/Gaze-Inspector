using System;
using System.Collections.Generic;

namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// Non-generic base for attribute drawers. Derive from <see cref="AttributeDrawer{T}"/> instead -
    /// only the generic form carries the attribute type that registration keys on.
    /// </summary>
    public abstract class AttributeDrawerBase : DrawerBase
    {
        internal static bool AllowMultiple { get; set; }
        internal static bool Inherited { get; set; }
        internal static AttributeTargets Targets { get; set; }

        /// <summary>
        /// The attribute instance found on the field, carrying whatever the user passed to its
        /// constructor. Cast it to your attribute type; it is set before <see cref="DrawerBase.Initialize"/> runs.
        /// </summary>
        protected internal Attribute Attribute { get; set; }

        /// <summary>
        /// The drawer this one wraps - the next decorator down, or eventually the drawer that renders the
        /// value itself. Call <c>Inner.Draw(label)</c> to render the property being decorated; skip the
        /// call to hide it.
        /// </summary>
        protected internal DrawerBase Inner { get; set; }

        internal override PropertyData Data => Inner.Data;
        
        internal bool TryGetAdditionalData<T>(out T data) where T : ContextItem
        {
            data = null;
            return Data?.AdditionalData?.TryGet(out data) ?? false;
        }

        /// <summary>
        /// Forwards to the wrapped drawer, so that messages from further down the chain still reach the
        /// inspector. An override that does not call this suppresses them.
        /// </summary>
        /// <param name="messages">Buffer to append to.</param>
        protected internal override void CollectMessages(List<GazeMessage> messages)
        {
            Inner.CollectMessages(messages);
        }
    }
}