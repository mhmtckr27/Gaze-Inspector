using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class BoundsDrawer : PropertyDrawerBase
    {
        private static readonly GUIContent _positionLabel = new("Center");
        private static readonly GUIContent _sizeLabel = new("Extents");
        
        public override void Draw(GUIContent label)
        {
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var value = Property.boundsValue;

                var children = Property.Children().ToList();

                var newCenter = DrawCenter(children[0]);
                var newExtents = DrawExtent(children[1]);

                if (scope.changed)
                {
                    value.center = newCenter;
                    value.extents = newExtents;
                    Property.boundsValue = value;
                }
            }
        }

        private Vector3 DrawCenter(SerializedProperty center)
        {
            return PropertyTreeGUI.MultiFloatField(Property.boundsValue.center, _positionLabel, center.Children().ToArray(), Data.AdditionalData.Has<DelayedDecorator.DelayedData>());
        }

        private Vector3 DrawExtent(SerializedProperty extents)
        {
            return PropertyTreeGUI.MultiFloatField(Property.boundsValue.extents, _sizeLabel, extents.Children().ToArray(), Data.AdditionalData.Has<DelayedDecorator.DelayedData>());
        }
    }
}