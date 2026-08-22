using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class RectDrawer : PropertyDrawerBase
    {
        private static readonly GUIContent _positionLabel = new("Position");
        private static readonly GUIContent _sizeLabel = new("Size");

        public override void Draw(GUIContent label)
        {
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var rectValue = Property.rectValue;

                var children = Property.Children().ToList();
                var newPosition = DrawPosition(children.Take(2).ToArray());
                var newSize = DrawSize(children.TakeLast(2).ToArray());

                if (scope.changed)
                {
                    rectValue.position = newPosition;
                    rectValue.size = newSize;
                    Property.rectValue = rectValue;
                }
            }
        }

        private Vector2 DrawPosition(SerializedProperty[] components)
        {
            return PropertyTreeGUI.MultiFloatField(Property.rectValue.position, _positionLabel, components, Data.AdditionalData.Has<DelayedDecorator.DelayedData>());
        }

        private Vector2 DrawSize(SerializedProperty[] components)
        {
            return PropertyTreeGUI.MultiFloatField(Property.rectValue.size, _sizeLabel, components, Data.AdditionalData.Has<DelayedDecorator.DelayedData>());
        }
    }
}