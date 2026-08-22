using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class RectIntDrawer : PropertyDrawerBase
    {
        private static readonly GUIContent _positionLabel = new("Position");
        private static readonly GUIContent _sizeLabel = new("Size");
        
        public override void Draw(GUIContent label)
        {
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var rectIntValue = Property.rectIntValue;

                var children = Property.Children().ToList();
                var newPosition = DrawPosition(children.Take(2).ToArray());
                var newSize = DrawSize(children.TakeLast(2).ToArray());

                if (scope.changed)
                {
                    rectIntValue.position = newPosition;
                    rectIntValue.size = newSize;
                    Property.rectIntValue = rectIntValue;
                }
            }
        }

        private Vector2Int DrawPosition(SerializedProperty[] components)
        {
            return PropertyTreeGUI.MultiIntField(Property.rectIntValue.position, _positionLabel, components);
        }

        private Vector2Int DrawSize(SerializedProperty[] components)
        {
            return PropertyTreeGUI.MultiIntField(Property.rectIntValue.size, _sizeLabel, components);
        }
    }
}