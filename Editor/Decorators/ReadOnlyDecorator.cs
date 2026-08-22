using System;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class ReadOnlyDecorator : AttributeDrawer<ReadOnlyAttribute>
    {
        public override void Draw(GUIContent label)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                Inner.Draw(label);
            }
        }
    }
}