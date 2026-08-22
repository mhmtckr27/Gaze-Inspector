using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal sealed class ConditionCache
    {
        private ulong _targetId;
        private bool _result;
        private string _error;

        internal bool Result => _result;

        internal bool Evaluate(SerializedProperty property, ConditionalAttribute attribute, out string error)
        {
            var targetId = ObjectId.Of(property.serializedObject.targetObject);

            if (Event.current.type == EventType.Layout || targetId != _targetId)
            {
                _result = ConditionResolver.Evaluate(property, attribute, out _error);
                _targetId = targetId;
            }

            error = _error;
            return _result;
        }
    }
}