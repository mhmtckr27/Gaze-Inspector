using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class RequiredDecorator : AttributeDrawer<RequiredAttribute>
    {
        private string STRING_ERROR = "";
        private string OTHER_TYPES_ERROR = "";
        
        private RequiredAttribute _attribute;

        protected internal override void Initialize()
        {
            _attribute = Attribute as RequiredAttribute;

            if (!string.IsNullOrEmpty(_attribute.ErrorMessage))
            {
                STRING_ERROR = _attribute.ErrorMessage;
                OTHER_TYPES_ERROR = _attribute.ErrorMessage;
            }
            else
            {
                STRING_ERROR = $"{Data.Label.text} is required.";
                OTHER_TYPES_ERROR = $"{Data.Label.text} must be assigned.";
            }
        }

        protected internal override void CollectMessages(List<GazeMessage> messages)
        {
            if (Data.Property.propertyType == SerializedPropertyType.String)
            {
                if (string.IsNullOrEmpty(Data.Property.stringValue))
                {
                    messages.Add(new GazeMessage(InfoMessageType.Error, STRING_ERROR));
                }
            }
            else if (Data.Property.objectReferenceValue == null)
            {
                messages.Add(new GazeMessage(InfoMessageType.Error, OTHER_TYPES_ERROR));
            }
            
            base.CollectMessages(messages);
        }

        public override void Draw(GUIContent label)
        {
            Inner.Draw(label);
        }
    }
}