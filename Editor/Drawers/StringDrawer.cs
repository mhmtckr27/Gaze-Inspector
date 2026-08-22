using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class StringDrawer : PropertyDrawerBase
    {
        private float GetHeight()
        {
            //no wrap.
            if (!TryGetAdditionalData<MultilineDecorator.MultilineData>(out var drawerData))
                return EditorGUIUtility.singleLineHeight;
            
            return drawerData.LineCount * EditorGUIUtility.singleLineHeight;
        }
        
        public override void Draw(GUIContent label)
        {
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                string newVal;
                
                if(TryGetAdditionalData<MultilineDecorator.MultilineData>(out var drawerData) && drawerData.LineCount > 1)
                    newVal = PropertyTreeGUI.TextArea(Property, label, Property.stringValue, GetHeight());
                else
                    newVal = PropertyTreeGUI.TextField(Property, label, Property.stringValue, GetHeight(), Data.AdditionalData.Has<DelayedDecorator.DelayedData>());
                
                if(scope.changed)
                    Property.stringValue = newVal;
            }
        }
    }
}