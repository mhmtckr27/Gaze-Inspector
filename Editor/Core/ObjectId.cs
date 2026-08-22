using UnityEditor;

namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// The identity Gaze keys its caches by. One place that knows how a Unity object is identified.
    /// </summary>
    /// <remarks>
    /// With Unity 6.5, <c>Object.GetInstanceID</c> and <c>EditorUtility.InstanceIDToObject</c>
    /// became obsolete. What replaced that instance id is: <c>EntityId</c>, which does not exist in
    /// 2022.3, 6000.0 or 6000.1. No single API spans the versions Gaze supports, so the
    /// version test lives here.
    ///
    /// ulong is the currency because EntityId has no conversion to int, only ToULong and
    /// FromULong, and narrowing a 6.5 id to 32 bits would risk collisions between objects.
    /// The pre-6.5 branch sign-extends before reinterpreting, so a negative instance ID
    /// round-trips through <see cref="ToObject"/> unchanged.
    /// </remarks>
    internal static class ObjectId
    {
        internal static ulong Of(UnityEngine.Object obj)
        {
#if UNITY_6000_5_OR_NEWER
            return UnityEngine.EntityId.ToULong(obj.GetEntityId());
#else
            return unchecked((ulong)(long)obj.GetInstanceID());
#endif
        }

        internal static UnityEngine.Object ToObject(ulong id)
        {
#if UNITY_6000_5_OR_NEWER
            return EditorUtility.EntityIdToObject(UnityEngine.EntityId.FromULong(id));
#else
            // 6000.3 deprecates InstanceIDToObject (CS0618) two versions before 6.5 turns it
            // into an error, but EntityId.FromULong only exists from 6.5, so on 6000.3 there is
            // nothing to build an EntityId from. Moving Of to EntityId while ToObject stayed on
            // instance IDs would leave the two no longer round-tripping, so both halves stay on
            // the legacy identity space until 6.5 and the warning is answered here, once,
            // instead of in every consumer's console.
#pragma warning disable 618
            return EditorUtility.InstanceIDToObject(unchecked((int)(long)id));
#pragma warning restore 618
#endif
        }
    }
}
