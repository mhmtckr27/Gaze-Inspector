using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class BoundsIntDrawer : PropertyDrawerBase
    {
        private static readonly GUIContent _positionLabel = new("Position");
        private static readonly GUIContent _sizeLabel = new("Size");
        
        public override void Draw(GUIContent label)
        {
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var value = Property.boundsIntValue;

                var children = Property.Children().ToList();
                
                var newPosition = DrawCenter(children[0]);
                var newSize = DrawExtent(children[1]);

                if (scope.changed)
                {
                    value.position = newPosition;
                    value.size = newSize;
                    Property.boundsIntValue = value;
                }
            }
        }

        private Vector3Int DrawCenter(SerializedProperty position)
        {
            return PropertyTreeGUI.MultiIntField(Property.boundsIntValue.position, _positionLabel, position.Children().ToArray());
        }

        private Vector3Int DrawExtent(SerializedProperty size)
        {
            return PropertyTreeGUI.MultiIntField(Property.boundsIntValue.size, _sizeLabel, size.Children().ToArray());
        }
    }
}