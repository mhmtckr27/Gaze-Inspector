using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    // -----------------------------
    // Tree GUI (measure + draw helpers)
    // -----------------------------
    /// <summary>
    /// Draws a <see cref="PropertyTree"/>, and exposes the labelled layout controls the built-in drawers
    /// are built from so a custom drawer can match their alignment and behaviour.
    /// </summary>
    /// <remarks>
    /// Everything here is layout-based; none of it takes a <see cref="Rect"/>. The field helpers pair a
    /// prefix label with a control and add label-drag support on numeric types, which is why a custom
    /// drawer should prefer them over calling <see cref="EditorGUILayout"/> directly.
    /// </remarks>
    public static class PropertyTreeGUI
    {
        private static readonly GUIContent _xLabel = new("X");
        private static readonly GUIContent _yLabel = new("Y");
        private static readonly GUIContent _zLabel = new("Z");
        private static readonly GUIContent _wLabel = new("W");

        private static readonly GUIContent[] _componentLabels = { _xLabel, _yLabel, _zLabel, _wLabel };
        
        private static Dictionary<Rect, string> _lastDrawnRects = new();
        
        internal static void DrawNode(DrawerBase node)
        {
            var resolved = node.Tree.Registry.Resolve(node.Property);
            DrawNode(resolved, node.Data.Label);
        }

        internal static void DrawNode(DrawerBase node, GUIContent label)
        {
            // Header and Space are bubbled to the outside of the chain, so a node that still is one of
            // them has not reached the block the messages belong to yet. Both forward back through here,
            // and the messages are drawn on the way down: under the decorators, above the field control.
            if (node is not HeaderDecorator and not SpaceDecorator)
                GazeMessages.Draw(node);

            node.Draw(label);
        }

        /// <summary>
        /// Reports, in place of the field, that a typed control was handed a property it cannot draw.
        /// </summary>
        /// <remarks>
        /// Gaze's own drawers are resolved by property type and never reach this. It exists for custom
        /// drawers calling these helpers directly, where returning a silent default would leave the author
        /// with a zero, no field, and nothing to explain either. Drawn rather than logged because these are
        /// per-frame call sites, and a console message would repeat until the editor was closed.
        /// </remarks>
        private static void DrawTypeMismatch(SerializedProperty property, SerializedPropertyType expected)
        {
            var path = property == null ? "<null>" : property.propertyPath;
            var actual = property == null ? "nothing" : property.propertyType.ToString();

            GazeMessages.DrawBox(new GazeMessage(
                InfoMessageType.Error,
                $"'{path}' is a {actual} property, but this control draws {expected}. " +
                "The field was skipped. Check the drawer resolving this property."));
        }

        /// <summary>
        /// Draws every root of the tree, along with its validation messages, missing-type warnings and
        /// right-click menus.
        /// </summary>
        /// <param name="tree">The tree to draw.</param>
        /// <remarks>
        /// Call once per OnGUI pass, between <c>serializedObject.Update()</c> and
        /// <c>ApplyModifiedProperties()</c>. This does not apply modifications itself.
        /// </remarks>
        public static void DrawTree(PropertyTree tree)
        {
            _lastDrawnRects.Clear();
            GazeMessages.ClearDrawn();

            if (Event.current.type == EventType.Layout)
                tree.Serialization.Refresh(tree.SerializedObject);

            tree.Serialization.Draw();

            foreach(var root in tree.Roots)
            {
                DrawNode(root);
            }
            
            if (tree.ContextMenuOpen && tree.ContextMenu != null)
            {
                tree.ContextMenu.ShowAsContext();
                tree.ContextMenu = null;
                tree.ContextMenuOpenCounter = 0;
            }
            
            if (Event.current.type == EventType.ContextClick)
            {
                var ctxProvider = tree.Registry.ContextMenuProviders.FirstOrDefault(cmp =>
                    cmp.ContextClickArea.Contains(Event.current.mousePosition));
                
                var r = _lastDrawnRects.FirstOrDefault(kvp => kvp.Key.Contains(Event.current.mousePosition));

                if (tree.Serialization.Contains(Event.current.mousePosition))
                {
                    tree.ContextMenuOpen = true;
                    tree.ContextMenuPropArea = tree.Serialization.Area;
                    tree.ContextMenuPropArea.xMax = tree.ContextMenuPropArea.xMin - 1;
                    tree.ContextMenuPropArea.xMin -= 4;
                    var menu = new GenericMenu();
                    tree.Serialization.Fill(menu);
                    tree.ContextMenu = menu;
                    Event.current.Use();
                }
                else if (!string.IsNullOrEmpty(r.Value))
                {
                    tree.ContextMenuOpen = true;
                    tree.ContextMenuPropArea = r.Key;
                    tree.ContextMenuPropArea.xMax = tree.ContextMenuPropArea.xMin - 1;
                    tree.ContextMenuPropArea.xMin -= 4;
                    var menu = new GenericMenu();
                    var prop = tree.SerializedObject.FindProperty(r.Value);
                    PropertyClipboard.AddContextMenu(menu, prop);
                    tree.ContextMenu = menu;
                    Event.current.Use();
                }
                else if (ctxProvider != null)
                {
                    var providedMenu = ctxProvider.Provide(out var highlightArea);

                    if (providedMenu != null)
                    {
                        tree.ContextMenuOpen = true;
                        tree.ContextMenu = providedMenu;
                        tree.ContextMenuPropArea = highlightArea;
                        tree.ContextMenuPropArea.xMax = tree.ContextMenuPropArea.xMin - 1;
                        tree.ContextMenuPropArea.xMin -= 4;

                        Event.current.Use();
                    }
                }
            }

            if (tree.ContextMenuOpen)
            {
                if (tree.ContextMenu == null)
                    tree.ContextMenuOpenCounter++;

                if (tree.ContextMenuOpenCounter > 5)
                {
                    tree.ContextMenuOpenCounter = 0;
                    tree.ContextMenuOpen = false;
                }

                EditorGUI.DrawRect(tree.ContextMenuPropArea, new Color(1f, 0.5f, 0.2f));
                GazeRepaint.ShouldRepaint = true;
            }
        }

        internal static string TextField(SerializedProperty property, GUIContent label, string value, float height, bool isDelayed)
        {
            BeginHorizontalLayout(label, out var labelRect);
            _lastDrawnRects[labelRect] = property.propertyPath;
            
            var rect = EditorGUILayout.GetControlRect(false, height, EditorStyles.textField);
            
            using (new EditorGui.MixedValueScope(property.hasMultipleDifferentValues))
            {
                using (new EditorGUI.PropertyScope(rect, label, property))
                {            
                    value = isDelayed 
                        ? EditorGUI.DelayedTextField(rect, GUIContent.none, value, EditorStyles.textField) 
                        : EditorGUI.TextField(rect, GUIContent.none, value, EditorStyles.textField);
                }
            }

            EndHorizontalLayout();
            return value;
        }

        internal static string TextArea(SerializedProperty property, GUIContent label, string value, float height)
        {
            BeginHorizontalLayout(label, out var labelRect);
            _lastDrawnRects[labelRect] = property.propertyPath;

            var rect = EditorGUILayout.GetControlRect(false, height, EditorStyles.textArea);
            using (new EditorGui.MixedValueScope(property.hasMultipleDifferentValues))
            {
                using (new EditorGUI.PropertyScope(rect, label, property))
                {
                    value = EditorGUI.TextArea(rect, value, EditorStyles.textArea);
                }
            }

            EndHorizontalLayout();
            return value;
        }

        internal static int IntField(SerializedProperty property, GUIContent label, bool isDelayed = false)
        {
            return IntField(property, label, property.intValue, isDelayed);
        }

        internal static int IntSlider(SerializedProperty property, GUIContent label, int minValue, int maxValue)
        {
            return IntField(property, label, property.intValue, false, true, minValue, maxValue);
        }
        
        internal static int IntField(
            SerializedProperty property, 
            GUIContent label, 
            int value, 
            bool isDelayed = false,
            bool isSlider = false, 
            int? minValue = null, 
            int? maxValue = null)
        {
            var valueLocal = value;
            BeginHorizontalLayout(label, out var labelRect);
            _lastDrawnRects[labelRect] = property.propertyPath;

            if (EditorGui.DragIntValue(labelRect, ref valueLocal))
                value = valueLocal;

            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight,
                EditorStyles.numberField);

            long newValue;
            using (new EditorGui.MixedValueScope(property.hasMultipleDifferentValues))
            {
                using (new EditorGUI.PropertyScope(rect, label, property))
                {
                    if (isSlider && minValue.HasValue && maxValue.HasValue)
                    {
                        newValue = (long) Math.Round(Slider(rect, value, minValue.Value, maxValue.Value));
                    }
                    else
                    {
                        newValue =
                            isDelayed
                                ? EditorGUI.DelayedIntField(rect, GUIContent.none, value)
                                : EditorGUI.LongField(rect, GUIContent.none, value);
                    }
                }
            }
            EndHorizontalLayout();
            
            if(minValue.HasValue)
                newValue = Math.Max(minValue.Value, newValue);
            if(maxValue.HasValue)
                newValue = Math.Min(maxValue.Value, newValue);

            if (newValue > int.MaxValue)
                return int.MaxValue;
            if (newValue < int.MinValue)
                return int.MinValue;
            
            return (int) newValue;
        }

        internal static Vector2Int Vector2IntField(
            SerializedProperty property, 
            GUIContent label, 
            bool isDelayed, 
            bool isSlider = false, 
            Vector2Int? minValue = null, 
            Vector2Int? maxValue = null)
        {
            const SerializedPropertyType expectedPropertyType = SerializedPropertyType.Vector2Int;
            if (property.propertyType != expectedPropertyType)
            {
                DrawTypeMismatch(property, expectedPropertyType);
                return default;
            }

            return Vector3IntField(property, label, property.vector2IntValue.ToVector3Int(), isDelayed, isSlider, minValue?.ToVector3Int(), maxValue?.ToVector3Int()).ToVector2Int();
        }
        
        internal static Vector3Int Vector3IntField(
            SerializedProperty property, 
            GUIContent label, 
            bool isDelayed, 
            bool isSlider = false, 
            Vector3Int? minValue = null, 
            Vector3Int? maxValue = null)
        {
            const SerializedPropertyType expectedPropertyType = SerializedPropertyType.Vector3Int;
            if (property.propertyType != expectedPropertyType)
            {
                DrawTypeMismatch(property, expectedPropertyType);
                return default;
            }

            return Vector3IntField(property, label, property.vector3IntValue, isDelayed, isSlider, minValue, maxValue);
        }
        
        private static Vector3Int Vector3IntField(
            SerializedProperty property, 
            GUIContent label, 
            Vector3Int value, 
            bool isDelayed, 
            bool isSlider = false, 
            Vector3Int? minValue = null, 
            Vector3Int? maxValue = null)
        {
            return MultiIntField(value, label, property.Children().ToArray(), isDelayed, isSlider, minValue, maxValue);
        }
        
        internal static float FloatField(SerializedProperty property, GUIContent label, bool isDelayed = false)
        {
            return FloatField(property, label, property.floatValue, isDelayed);
        }
        
        internal static float FloatSlider(SerializedProperty property, GUIContent label, float minValue, float maxValue)
        {
            return FloatField(property, label, property.floatValue, false, true, minValue, maxValue);
        }
        
        internal static float FloatField(
            SerializedProperty property,
            GUIContent label, 
            float value, 
            bool isDelayed = false,
            bool isSlider = false, 
            float? minValue = null, 
            float? maxValue = null)
        {
            const SerializedPropertyType expectedPropertyType = SerializedPropertyType.Float;
            if (property.propertyType != expectedPropertyType)
            {
                DrawTypeMismatch(property, expectedPropertyType);
                return default;
            }
            
            var valueLocal = value;
            BeginHorizontalLayout(label, out var labelRect);
            _lastDrawnRects[labelRect] = property.propertyPath;

            if (EditorGui.DragFloatValue(labelRect, ref valueLocal))
                value = valueLocal;
            
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, EditorStyles.numberField);

            float newValue;
            using (new EditorGui.MixedValueScope(property.hasMultipleDifferentValues))
            {
                using (new EditorGUI.PropertyScope(rect, label, property))
                {
                    if (isSlider && minValue.HasValue && maxValue.HasValue)
                    {
                        newValue = Slider(rect, value, minValue.Value, maxValue.Value);
                    }
                    else
                    {
                        newValue =
                            isDelayed
                                ? EditorGUI.DelayedFloatField(rect, GUIContent.none, value)
                                : EditorGUI.FloatField(rect, GUIContent.none, value);
                    }
                }
            }
            EndHorizontalLayout();
            return Mathf.Clamp(newValue, minValue ?? float.MinValue, maxValue ?? float.MaxValue);
        }
        
        internal static Vector2 Vector2Field(
            SerializedProperty property, 
            GUIContent label,
            bool isDelayed,
            bool isSlider = false, 
            Vector4? minValue = null, 
            Vector4? maxValue = null)
        {
            const SerializedPropertyType expectedPropertyType = SerializedPropertyType.Vector2;
            if (property.propertyType != expectedPropertyType)
            {
                DrawTypeMismatch(property, expectedPropertyType);
                return default;
            }
            
            return Vector4Field(property, label, property.vector2Value, isDelayed, isSlider, minValue, maxValue);
        }
        
        internal static Vector3 Vector3Field(
            SerializedProperty property, 
            GUIContent label,
            bool isDelayed, 
            bool isSlider = false, 
            Vector4? minValue = null, 
            Vector4? maxValue = null)
        {
            const SerializedPropertyType expectedPropertyType = SerializedPropertyType.Vector3;
            if (property.propertyType != expectedPropertyType)
            {
                DrawTypeMismatch(property, expectedPropertyType);
                return default;
            }
            
            return Vector4Field(property, label, property.vector3Value, isDelayed, isSlider, minValue, maxValue);
        }

        internal static Vector4 Vector4Field(
            SerializedProperty property,
            GUIContent label,
            bool isDelayed = false,
            bool isSlider = false,
            Vector4? minValue = null,
            Vector4? maxValue = null)
        {
            const SerializedPropertyType expectedPropertyType = SerializedPropertyType.Vector4;
            if (property.propertyType != expectedPropertyType)
            {
                DrawTypeMismatch(property, expectedPropertyType);
                return default;
            }

            return Vector4Field(property, label, property.vector4Value, isDelayed, isSlider, minValue, maxValue);
        }

        private static Vector4 Vector4Field(
            SerializedProperty property, 
            GUIContent label, 
            Vector4 value,
            bool isDelayed = false,
            bool isSlider = false, 
            Vector4? minValue = null, 
            Vector4? maxValue = null)
        {
            return MultiFloatField(value, label, property.Children().ToArray(), isDelayed, isSlider, minValue, maxValue);
        }

        internal static Vector2Int MultiIntField(Vector2Int value, GUIContent label, SerializedProperty[] components, bool isDelayed = false)
        {
            return MultiIntField(value.ToVector3Int(), label, components, isDelayed).ToVector2Int();
        }
        
        internal static Vector3Int MultiIntField(
            Vector3Int value, 
            GUIContent label, 
            SerializedProperty[] components, 
            bool isDelayed = false, 
            bool isSlider = false, 
            Vector3Int? minValue = null, 
            Vector3Int? maxValue = null)
        {
            var valueLocal = value;
            BeginHorizontalLayout(label, out var labelRect);
            _lastDrawnRects[labelRect] = components[0].GetParent().propertyPath;

            var dragHotZone = labelRect;
            dragHotZone.xMax -= 10f;

            if (EditorGui.DragVectorIntValue(dragHotZone, ref valueLocal))
                value = valueLocal;

            var parameterData = new MultiIntData[components.Length];
            for (var i = 0; i < components.Length; i++)
            {
                parameterData[i] = new MultiIntData()
                {
                    property = components[i],
                    label = _componentLabels[i],
                    value = value[i],
                    isDelayed = isDelayed,
                    isSlider = isSlider,
                    minValue = minValue?[i],
                    maxValue = maxValue?[i]
                };
            }
            
            var values = MultiIntField(parameterData);

            Vector3Int newValue = default;
            newValue = newValue.FromArray(values);
            EndHorizontalLayout();
            return newValue;
        }
        
        internal static Vector2 MultiFloatField(Vector2 value, GUIContent label, SerializedProperty[] components, bool isDelayed)
        {
            return MultiFloatField((Vector4) value, label, components, isDelayed);
        }
        
        internal static Vector3 MultiFloatField(Vector3 value, GUIContent label, SerializedProperty[] components, bool isDelayed)
        {
            return MultiFloatField((Vector4) value, label, components, isDelayed);
        }
        
        internal static Vector4 MultiFloatField(
            Vector4 value, 
            GUIContent label,
            SerializedProperty[] components,
            bool isDelayed,
            bool isSlider = false, 
            Vector4? minValue = null, 
            Vector4? maxValue = null)
        {
            var valueLocal = value;
            BeginHorizontalLayout(label, out var labelRect);
            _lastDrawnRects[labelRect] = components[0].GetParent().propertyPath;

            var dragHotZone = labelRect;
            dragHotZone.xMax -= 10f;

            if (EditorGui.DragVectorValue(dragHotZone, ref valueLocal))
                value = valueLocal;

            var parameterData = new MultiFloatData[components.Length];
            for (var i = 0; i < components.Length; i++)
            {
                parameterData[i] = new MultiFloatData()
                {
                    property = components[i],
                    label = _componentLabels[i],
                    value = value[i],
                    isDelayed = isDelayed,
                    isSlider = isSlider,
                    minValue = minValue?[i],
                    maxValue = maxValue?[i]
                };
            }
            
            var values = MultiFloatField(parameterData);

            Vector4 newValue = default;
            newValue = newValue.FromArray(values);
            EndHorizontalLayout();
            return newValue;
        }
        
        internal static int[] MultiIntField(params MultiIntData[] fields)
        {
            var newValues = new int[fields.Length];
            var row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, EditorStyles.numberField);

            EditorGuiUtility.PushLabelWidth(EditorGuiStyles.SubLabelWidth);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                newValues[i] = IntField(ComponentRect(row, i, fields.Length), field.property, field.label, field.value, field.isDelayed, field.isSlider, field.minValue, field.maxValue);
            }
            EditorGuiUtility.PopLabelWidth();

            return newValues;
        }

        internal static float[] MultiFloatField(params MultiFloatData[] fields)
        {
            var newValues = new float[fields.Length];
            var row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, EditorStyles.numberField);

            EditorGuiUtility.PushLabelWidth(EditorGuiStyles.SubLabelWidth);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                newValues[i] = FloatField(ComponentRect(row, i, fields.Length), field.property, field.label, field.value, field.isDelayed, field.isSlider, field.minValue, field.maxValue);
            }
            EditorGuiUtility.PopLabelWidth();

            return newValues;
        }

        /// <summary>
        /// The rect one component of a multi-component field occupies inside <paramref name="row"/>.
        /// </summary>
        /// <remarks>
        /// Both edges are rounded off the row's own origin rather than accumulated from the component
        /// before them, so the fractional part of the component width cannot build up across the row.
        /// The last component's right edge lands on <c>row.xMax</c> exactly, which is what holds the
        /// right edges of every field in the inspector in line while the window is being resized.
        /// </remarks>
        private static Rect ComponentRect(Rect row, int index, int count)
        {
            var width = (row.width - (count - 1) * EditorGuiStyles.SubLabelSpacing) / count;
            var offset = index * (width + EditorGuiStyles.SubLabelSpacing);

            var xMin = Mathf.Round(row.x + offset);
            var xMax = Mathf.Round(row.x + offset + width);

            return new Rect(xMin, row.y, xMax - xMin, row.height);
        }

        /// <summary>
        /// Draws the sub-label of one component and returns the rect left over for its control.
        /// </summary>
        /// <remarks>
        /// The control is then drawn with no label of its own, so the width measured against
        /// <see cref="EditorGuiStyles.MinSliderFieldWidth"/> is the same width Unity's slider measures
        /// internally. Measuring the whole component rect instead lets the two disagree, and the blank
        /// gap the threshold exists to prevent comes straight back.
        /// </remarks>
        private static Rect ComponentControlRect(Rect component, SerializedProperty property, GUIContent label, out Rect labelRect)
        {
            labelRect = new Rect(component.x, component.y, EditorGUIUtility.labelWidth, component.height);
            _lastDrawnRects[labelRect] = property.propertyPath;

            return EditorGUI.PrefixLabel(component, label);
        }

        /// <summary>
        /// A slider that hands its rect to the number field when the rect is too narrow to hold a track.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Below <see cref="EditorGuiStyles.MinSliderFieldWidth"/> Unity draws no track. It clamps the
        /// number field to <c>fieldWidth</c>, pins it to the right of the rect and leaves the rest of
        /// the rect blank. Widening <c>fieldWidth</c> to the rect sends the same branch down the path
        /// where the field fills the rect, so the space the hidden slider would have taken goes back to
        /// the field instead of standing empty.
        /// </para>
        /// <para>
        /// The threshold is read off the whole rect, before Unity splits it into a track and a field,
        /// and widening <c>fieldWidth</c> raises Unity's own threshold by the same amount, so giving the
        /// space back can never push the field over the threshold again. Both paths draw the same
        /// control, so the choice cannot change which control ids the row allocates, which is what lets
        /// it depend on a width that is only known from Repaint onwards.
        /// </para>
        /// </remarks>
        private static float Slider(Rect rect, float value, float minValue, float maxValue)
        {
            var isNarrow = rect.width < EditorGuiStyles.MinSliderFieldWidth;

            if (isNarrow)
                EditorGuiUtility.PushFieldWidth(rect.width);

            value = EditorGUI.Slider(rect, GUIContent.none, value, minValue, maxValue);

            if (isNarrow)
                EditorGuiUtility.PopFieldWidth();

            return value;
        }

        private static int IntField(
            Rect component,
            SerializedProperty property,
            GUIContent label,
            int value,
            bool isDelayed,
            bool isSlider,
            int? minValue,
            int? maxValue)
        {
            var rect = ComponentControlRect(component, property, label, out var labelRect);

            var valueLocal = value;
            if (EditorGui.DragIntValue(labelRect, ref valueLocal))
                value = valueLocal;

            long newValue;
            using (new EditorGui.MixedValueScope(property.hasMultipleDifferentValues))
            {
                using (new EditorGUI.PropertyScope(rect, label, property))
                {
                    if (isSlider && minValue.HasValue && maxValue.HasValue)
                    {
                        newValue = (long) Math.Round(Slider(rect, value, minValue.Value, maxValue.Value));
                    }
                    else
                    {
                        newValue =
                            isDelayed
                                ? EditorGUI.DelayedIntField(rect, GUIContent.none, value)
                                : EditorGUI.LongField(rect, GUIContent.none, value);
                    }
                }
            }

            if(minValue.HasValue)
                newValue = Math.Max(minValue.Value, newValue);
            if(maxValue.HasValue)
                newValue = Math.Min(maxValue.Value, newValue);

            if (newValue > int.MaxValue)
                return int.MaxValue;
            if (newValue < int.MinValue)
                return int.MinValue;

            return (int) newValue;
        }

        private static float FloatField(
            Rect component,
            SerializedProperty property,
            GUIContent label,
            float value,
            bool isDelayed,
            bool isSlider,
            float? minValue,
            float? maxValue)
        {
            var rect = ComponentControlRect(component, property, label, out var labelRect);

            var valueLocal = value;
            if (EditorGui.DragFloatValue(labelRect, ref valueLocal))
                value = valueLocal;

            float newValue;
            using (new EditorGui.MixedValueScope(property.hasMultipleDifferentValues))
            {
                using (new EditorGUI.PropertyScope(rect, label, property))
                {
                    if (isSlider && minValue.HasValue && maxValue.HasValue)
                    {
                        newValue = Slider(rect, value, minValue.Value, maxValue.Value);
                    }
                    else
                    {
                        newValue =
                            isDelayed
                                ? EditorGUI.DelayedFloatField(rect, GUIContent.none, value)
                                : EditorGUI.FloatField(rect, GUIContent.none, value);
                    }
                }
            }

            return Mathf.Clamp(newValue, minValue ?? float.MinValue, maxValue ?? float.MaxValue);
        }

        #region Public API
        
        /// <summary>
        /// A labelled single-line text field.
        /// </summary>
        /// <param name="label">Prefix label, or null to draw the field full width with no label.</param>
        /// <param name="value">Current text.</param>
        /// <param name="height">Field height in pixels.</param>
        /// <param name="isDelayed">
        /// True to report the new value only once the user presses Enter or moves focus away, rather
        /// than on every keystroke.
        /// </param>
        /// <returns>The edited text.</returns>
        public static string TextField(GUIContent label, string value, float height, bool isDelayed = false)
        {
            BeginHorizontalLayout(label, out _);
            
            var rect = EditorGUILayout.GetControlRect(false, height, EditorStyles.textField);
            value = isDelayed 
                ? EditorGUI.DelayedTextField(rect, GUIContent.none, value, EditorStyles.textField)
                : EditorGUI.TextField(rect, GUIContent.none, value, EditorStyles.textField);

            EndHorizontalLayout();
            return value;
        }
        
        /// <summary>
        /// A labelled multi-line text area.
        /// </summary>
        /// <param name="label">Prefix label, or null to draw the area full width with no label.</param>
        /// <param name="value">Current text.</param>
        /// <param name="height">Area height in pixels. Fixed - it does not grow with the content.</param>
        /// <returns>The edited text.</returns>
        public static string TextArea(GUIContent label, string value, float height)
        {
            BeginHorizontalLayout(label, out _);
            
            var rect = EditorGUILayout.GetControlRect(false, height, EditorStyles.textArea);

            value = EditorGUI.TextArea(rect, value, EditorStyles.textArea);
            
            EndHorizontalLayout();
            return value;
        }
        
                
        /// <summary>
        /// A labelled integer field, optionally a slider, whose label can be dragged to scrub the value.
        /// </summary>
        /// <param name="label">Prefix label, or null to draw the field full width with no label.</param>
        /// <param name="value">Current value.</param>
        /// <param name="isDelayed">
        /// True to report the new value only once the user presses Enter or moves focus away. Ignored
        /// when drawing a slider.
        /// </param>
        /// <param name="isSlider">
        /// True to draw a slider. Takes effect only when both <paramref name="minValue"/> and
        /// <paramref name="maxValue"/> are supplied.
        /// </param>
        /// <param name="minValue">Lower bound, or null for unbounded.</param>
        /// <param name="maxValue">Upper bound, or null for unbounded.</param>
        /// <returns>The edited value, clamped to the supplied bounds.</returns>
        /// <remarks>
        /// The bounds clamp the returned value whether or not a slider is drawn, so a typed-in number
        /// outside the range is corrected too.
        /// </remarks>
        public static int IntField(
            GUIContent label,
            int value,
            bool isDelayed = false,
            bool isSlider = false,
            int? minValue = null,
            int? maxValue = null)
        {
            var valueLocal = value;
            BeginHorizontalLayout(label, out var labelRect);

            if (EditorGui.DragIntValue(labelRect, ref valueLocal))
                value = valueLocal;
            
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, EditorStyles.numberField);

            if (isSlider && minValue.HasValue && maxValue.HasValue)
            {
                value = Mathf.RoundToInt(Slider(rect, value, minValue.Value, maxValue.Value));
            }
            else
            {
                value = isDelayed 
                    ? EditorGUI.DelayedIntField(rect, GUIContent.none, value)
                    : EditorGUI.IntField(rect, GUIContent.none, value);
            }
            
            EndHorizontalLayout();
            return Mathf.Clamp(value, minValue ?? int.MinValue, maxValue ?? int.MaxValue);
        }
        
        /// <summary>
        /// A labelled float field, optionally a slider, whose label can be dragged to scrub the value.
        /// </summary>
        /// <param name="label">Prefix label, or null to draw the field full width with no label.</param>
        /// <param name="value">Current value.</param>
        /// <param name="isSlider">
        /// True to draw a slider. Takes effect only when both <paramref name="minValue"/> and
        /// <paramref name="maxValue"/> are supplied.
        /// </param>
        /// <param name="minValue">Lower bound, or null for unbounded.</param>
        /// <param name="maxValue">Upper bound, or null for unbounded.</param>
        /// <returns>The edited value, clamped to the supplied bounds.</returns>
        /// <remarks>
        /// The bounds clamp the returned value whether or not a slider is drawn. This overload has no
        /// delayed mode, unlike <see cref="IntField(GUIContent, int, bool, bool, int?, int?)"/>.
        /// </remarks>
        public static float FloatField(
            GUIContent label,
            float value,
            bool isSlider = false,
            float? minValue = null,
            float? maxValue = null)
        {
            var valueLocal = value;
            BeginHorizontalLayout(label, out var labelRect);

            if (EditorGui.DragFloatValue(labelRect, ref valueLocal))
                value = valueLocal;
            
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, EditorStyles.numberField);

            if (isSlider && minValue.HasValue && maxValue.HasValue)
                value = Slider(rect, value, minValue.Value, maxValue.Value);
            else 
                value = EditorGUI.FloatField(rect, GUIContent.none, value);
            
            EndHorizontalLayout();
            return Mathf.Clamp(value, minValue ?? float.MinValue, maxValue ?? float.MaxValue);
        }
        
        /// <summary>
        /// A labelled <see cref="Vector2Int"/> field whose label can be dragged to scrub all components
        /// together.
        /// </summary>
        /// <param name="label">Prefix label, or null to draw the field full width with no label.</param>
        /// <param name="value">Current value.</param>
        /// <returns>The edited value.</returns>
        public static Vector2Int Vector2IntField(GUIContent label, Vector2Int value)
        {
            var valueLocal = value;
            BeginHorizontalLayout(label, out var labelRect);

            var dragHotZone = labelRect;
            dragHotZone.xMax -= 10f;
            
            if (EditorGui.DragVectorIntValue(dragHotZone, ref valueLocal))
                value = valueLocal;
            
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            value = EditorGUI.Vector2IntField(rect, GUIContent.none, value);
            
            EndHorizontalLayout();
            return value;
        }
        
        
        /// <summary>
        /// A labelled <see cref="Vector3Int"/> field whose label can be dragged to scrub all components
        /// together.
        /// </summary>
        /// <param name="label">Prefix label, or null to draw the field full width with no label.</param>
        /// <param name="value">Current value.</param>
        /// <returns>The edited value.</returns>
        public static Vector3Int Vector3IntField(GUIContent label, Vector3Int value)
        {
            var valueLocal = value;
            BeginHorizontalLayout(label, out var labelRect);

            var dragHotZone = labelRect;
            dragHotZone.xMax -= 10f;
            
            if (EditorGui.DragVectorIntValue(dragHotZone, ref valueLocal))
                value = valueLocal;
            
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            value = EditorGUI.Vector3IntField(rect, GUIContent.none, value);
            
            EndHorizontalLayout();
            return value;
        }
        
        /// <summary>
        /// A labelled <see cref="Vector2"/> field whose label can be dragged to scrub all components
        /// together.
        /// </summary>
        /// <param name="label">Prefix label, or null to draw the field full width with no label.</param>
        /// <param name="value">Current value.</param>
        /// <returns>The edited value.</returns>
        public static Vector2 Vector2Field(GUIContent label, Vector2 value)
        {
            var valueLocal = value;
            BeginHorizontalLayout(label, out var labelRect);

            var dragHotZone = labelRect;
            dragHotZone.xMax -= 10f;
            
            if (EditorGui.DragVectorValue(dragHotZone, ref valueLocal))
                value = valueLocal;
            
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            value = EditorGUI.Vector2Field(rect, GUIContent.none, value);
            
            EndHorizontalLayout();
            return value;
        }
        
        /// <summary>
        /// A labelled <see cref="Vector3"/> field whose label can be dragged to scrub all components
        /// together.
        /// </summary>
        /// <param name="label">Prefix label, or null to draw the field full width with no label.</param>
        /// <param name="value">Current value.</param>
        /// <returns>The edited value.</returns>
        public static Vector3 Vector3Field(GUIContent label, Vector3 value)
        {
            var valueLocal = value;
            BeginHorizontalLayout(label, out var labelRect);

            var dragHotZone = labelRect;
            dragHotZone.xMax -= 10f;
            
            if (EditorGui.DragVectorValue(dragHotZone, ref valueLocal))
                value = valueLocal;
            
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            value = EditorGUI.Vector3Field(rect, GUIContent.none, value);
            
            EndHorizontalLayout();
            return value;
        }
        
        /// <summary>
        /// A labelled <see cref="Vector4"/> field whose label can be dragged to scrub all components
        /// together.
        /// </summary>
        /// <param name="label">Prefix label, or null to draw the field full width with no label.</param>
        /// <param name="value">Current value.</param>
        /// <returns>The edited value.</returns>
        public static Vector4 Vector4Field(GUIContent label, Vector4 value)
        {
            var valueLocal = value;
            BeginHorizontalLayout(label, out var labelRect);

            var dragHotZone = labelRect;
            dragHotZone.xMax -= 10f;
            
            if (EditorGui.DragVectorValue(dragHotZone, ref valueLocal))
                value = valueLocal;
            
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            value = EditorGUI.Vector4Field(rect, GUIContent.none, value);
            
            EndHorizontalLayout();
            return value;
        }
        
        #endregion

        internal struct MultiFloatData
        {
            internal SerializedProperty property;
            internal GUIContent label;
            internal float value;
            internal bool isDelayed;
            internal bool isSlider;
            internal float? minValue;
            internal float? maxValue;
        }
        
        internal struct MultiIntData
        {
            internal SerializedProperty property;
            internal GUIContent label;
            internal int value;
            internal bool isDelayed;
            internal bool isSlider;
            internal int? minValue;
            internal int? maxValue;
        }
        
        internal static bool BeginFoldout(SerializedProperty property, bool newExpanded)
        {
            var state = PropertyStateStore.GetFoldoutAnimState(property);

            if (newExpanded != state.expanded)
                property.isExpanded = newExpanded;

            return BeginFadeGroup(state, newExpanded);
        }

        internal static bool BeginFoldout(Rect rect, SerializedProperty property, GUIContent label)
        {
            var state = PropertyStateStore.GetFoldoutAnimState(property);
            
            var newExpanded = EditorGUI.Foldout(rect, state.expanded, label, true);
            
            return BeginFoldout(property, newExpanded);
        }
        
        internal static bool BeginFoldout(SerializedProperty property, GUIContent label, out Rect rect)
        {
            rect = EditorGUILayout.GetControlRect(label != null);
            return BeginFoldout(rect, property, label);
        }
        
        internal static void EndFoldout()
        {
            EndFadeGroup();
        }
        
        private static int _animatingFadeGroup = -1;
        private static int _currentFadeGroup = 0;

        /// <summary>
        /// Opens a group whose drawn extent can be read back from <see cref="EndMeasuring"/>. Use it to
        /// learn the rect a block of layout content actually occupied.
        /// </summary>
        /// <param name="style">Style for the group, or null for one with no margins or padding.</param>
        public static void BeginMeasuring(GUIStyle style = null)
        {
            GUILayout.BeginVertical(style ?? EditorGuiStyles.None);
        }

        /// <summary>
        /// Closes the group opened by <see cref="BeginMeasuring"/> and reports the rect it occupied.
        /// </summary>
        /// <returns>
        /// The measured rect, or <c>default</c> during the Layout pass, when no rect exists yet. Guard
        /// on <c>Event.current.type == EventType.Repaint</c> before storing the result.
        /// </returns>
        public static Rect EndMeasuring()
        {
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Layout)
                return default;
            
            return GUILayoutUtility.GetLastRect();
        }
        
        internal static bool BeginFadeGroup(FoldoutAnimState state, bool on)
        {
            if (on != state.expanded)
            {
                state.expanded = on;
                state.stamp = Time.realtimeSinceStartup;
            }

            if (Event.current.type == EventType.Layout)
            {
                var delta = Time.realtimeSinceStartup - state.stamp;
                state.value = Mathf.MoveTowards(state.value, on ? 1 : 0, delta * (1f / state.duration));
                state.stamp = Time.realtimeSinceStartup;
            }

            return BeginFadeGroup(state.value);
        }

        /// <summary>
        /// Opens a group that fades and slides its contents in or out, for animating a foldout.
        /// </summary>
        /// <param name="t">
        /// Linear progress from 0 (fully collapsed) to 1 (fully expanded). Pass the raw value; it is
        /// eased internally, so pre-smoothing it double-applies the curve.
        /// </param>
        /// <returns>True when the contents should be drawn. False means fully collapsed.</returns>
        /// <remarks>
        /// <para>
        /// Always pair with <see cref="EndFadeGroup"/>, including when this returns false - the call is
        /// what unwinds the group's layout state, and skipping it corrupts the rest of the inspector.
        /// </para>
        /// <para>
        /// Groups may nest. Only the outermost one that is mid-animation actually animates; inner ones
        /// track depth so their contents move with it rather than fading independently.
        /// </para>
        /// </remarks>
        public static bool BeginFadeGroup(float t)
        {
            if(t > 0.0 && t < 1.0 && _animatingFadeGroup == -1)
            {
                // EditorGUILayout.BeginFadeGroup calls GUI.BeginGroup and only then LockContextWidth, so it
                // pins contextWidth to the *group's* width rather than the view's. Any label width derived
                // implicitly from contextWidth would be narrower for the length of the animation and snap
                // back when it ends, sliding labels sideways. Capture the real one first and pin it.
                var labelWidth = EditorGUIUtility.labelWidth;

                t = Mathf.Clamp01((float)(t * t * (3.0 - 2.0 * t)));
                EditorGUILayout.BeginFadeGroup(t);
                GUILayout.BeginVertical();
                EditorGuiUtility.PushColor(new Color(GUI.color.r, GUI.color.g, GUI.color.b, t));
                EditorGuiUtility.PushLabelWidth(labelWidth);
                _animatingFadeGroup = _currentFadeGroup;
                GazeRepaint.ShouldRepaint = true;
            }

            _currentFadeGroup++;
            return t > 0.0;
        }

        /// <summary>
        /// Closes the group opened by <see cref="BeginFadeGroup"/>. Must be called once per
        /// <see cref="BeginFadeGroup"/>, whatever that call returned.
        /// </summary>
        public static void EndFadeGroup()
        {
            _currentFadeGroup--;
            if(_animatingFadeGroup != _currentFadeGroup)
                return;
            
            GUILayout.EndVertical();
            EditorGUILayout.EndFadeGroup();
            EditorGuiUtility.PopColor();
            EditorGuiUtility.PopLabelWidth();
            _animatingFadeGroup = -1;
        }
        
        /// <summary>
        /// Opens a prefix-labelled group whose contents stack vertically in the value column.
        /// </summary>
        /// <param name="label">
        /// Prefix label, or null to indent by the current indent level and draw no label.
        /// </param>
        /// <param name="labelRect">
        /// Receives the rect the label occupied, for hit-testing a label drag or a context click. Set to
        /// a default rect when <paramref name="label"/> is null.
        /// </param>
        /// <param name="style">Style for the group, or null for none.</param>
        /// <param name="options">Layout options for the group.</param>
        /// <returns>The group's rect.</returns>
        /// <remarks>
        /// Label width and indent level are pushed for the contents so nested controls align under the
        /// value column, and <see cref="EndVerticalLayout"/> restores them. Pair the two calls on every
        /// path.
        /// </remarks>
        public static Rect BeginVerticalLayout(GUIContent label, out Rect labelRect, GUIStyle style = null, params GUILayoutOption[] options)
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            labelRect = new Rect();
            
            if(style == null)
                GUILayout.BeginHorizontal();
            else
                GUILayout.BeginHorizontal(style);
            
            if (label == null)
            {
                GUILayout.Space(EditorGui.CurrentIndentAmount);
            }
            else
            {
                EditorGUILayout.PrefixLabel(label);
                labelRect = GUILayoutUtility.GetLastRect();
            }
            
            var rect = style == null 
                ? EditorGUILayout.BeginVertical(options) 
                : EditorGUILayout.BeginVertical(style, options);
            
            EditorGuiUtility.PushLabelWidth(labelWidth - EditorGui.CurrentIndentAmount);
            EditorGuiUtility.PushIndentLevel(0);
            return rect;
        }
        
        /// <summary>
        /// Closes the group opened by <see cref="BeginVerticalLayout"/> and restores the label width and
        /// indent level.
        /// </summary>
        public static void EndVerticalLayout()
        {
            EditorGuiUtility.PopLabelWidth();
            EditorGuiUtility.PopIndentLevel();
            EditorGUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
        //
        /// <summary>
        /// Opens a prefix-labelled group whose contents flow horizontally across the value column. This
        /// is the shape every built-in single-line drawer uses.
        /// </summary>
        /// <param name="label">
        /// Prefix label, or null to indent by the current indent level and draw no label.
        /// </param>
        /// <param name="labelRect">
        /// Receives the rect the label occupied, for hit-testing a label drag or a context click. Set to
        /// a default rect when <paramref name="label"/> is null.
        /// </param>
        /// <param name="style">Style for the group, or null for none.</param>
        /// <param name="options">Layout options for the group.</param>
        /// <returns>The group's rect.</returns>
        /// <remarks>
        /// Label width and indent level are pushed for the contents so nested controls align under the
        /// value column, and <see cref="EndHorizontalLayout"/> restores them. Pair the two calls on every
        /// path.
        /// </remarks>
        public static Rect BeginHorizontalLayout(GUIContent label, out Rect labelRect, GUIStyle style = null, params GUILayoutOption[] options)
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            labelRect = default;
            
            if(style == null)
                GUILayout.BeginHorizontal();
            else
                GUILayout.BeginHorizontal(style);
            
            if (label == null)
            {
                GUILayout.Space(EditorGui.CurrentIndentAmount);
            }
            else
            {
                EditorGUILayout.PrefixLabel(label);
                labelRect = GUILayoutUtility.GetLastRect();
            }

            var rect = style == null 
                ? EditorGUILayout.BeginHorizontal(options) 
                : EditorGUILayout.BeginHorizontal(style, options);
            
            EditorGuiUtility.PushLabelWidth(labelWidth - EditorGui.CurrentIndentAmount);
            EditorGuiUtility.PushIndentLevel(0);
            return rect;
        }
        
        /// <summary>
        /// Opens a horizontally striped row, alternating background by row parity the way the built-in
        /// list and dictionary drawers do.
        /// </summary>
        /// <param name="row">Row index. Only its parity matters; negative values are fine.</param>
        /// <param name="evenStyle">Background for even rows, or null for the default stripe.</param>
        /// <param name="oddStyle">Background for odd rows, or null for the default stripe.</param>
        /// <returns>The row's rect.</returns>
        public static Rect BeginListItem(int row, GUIStyle evenStyle = null, GUIStyle oddStyle = null)
        {
            return EditorGUILayout.BeginHorizontal(row % 2 == 0
                ? evenStyle ?? EditorGuiStyles.ListItemEvenIndex
                : oddStyle ?? EditorGuiStyles.ListItemOddIndex);
        }
        
        /// <summary>
        /// Closes the row opened by <see cref="BeginListItem"/>.
        /// </summary>
        public static void EndListItem()
        {
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Closes the group opened by <see cref="BeginHorizontalLayout"/> and restores the label width
        /// and indent level.
        /// </summary>
        public static void EndHorizontalLayout()
        {
            EditorGuiUtility.PopLabelWidth();
            EditorGuiUtility.PopIndentLevel();
            EditorGUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
        }
    }
}
