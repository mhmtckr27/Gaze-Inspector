using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// Low-level GUI helpers shared by the built-in drawers, and the GUI-state scopes a custom drawer
    /// needs to match their behaviour.
    /// </summary>
    /// <seealso cref="PropertyTreeGUI"/>
    public static class EditorGui
    {
        private static readonly Action _endActiveEditing = Create();
        private static double s_DragSensitivity;
        private static Vector2 s_DragStartPos;
        private static int s_DragStartIntValue;
        private static double s_DragStartValue;
        private static DragCandidateState s_DragCandidateState;
        private static Vector3Int s_DragStartValueVectorInt;
        private static Vector4 s_DragStartValueVector;
        private static readonly int s_DragNumberValueHash = "Gaze_LabelNumberDrag".GetHashCode(); 
        private static readonly int s_HoverButtonHash = "Gaze_HoverableButton".GetHashCode();
        private static readonly Dictionary<int, bool> s_Hovered = new(64);

        private static double s_CurrentValue;
        
        /// <summary>
        /// Width in pixels that the current <see cref="EditorGUI.indentLevel"/> is worth. Use it to
        /// reserve leading space by hand where a layout group has flattened the indent level.
        /// </summary>
        public static float CurrentIndentAmount => EditorGUI.indentLevel * 15;
        
        // Draws the label and returns the remaining rect for the control.
        internal static Rect LabeledFieldRect(Rect r, GUIContent label, GUIStyle style = null)
        {
            r.height = EditorGUIUtility.singleLineHeight;

            // Draws the label with proper focus behavior and gives you the remaining rect.
            if (label == null)
                return r;
            
            var id = GUIUtility.GetControlID(FocusType.Keyboard, r);
            
            EditorGUILayout.PrefixLabel(label);
            return GUILayoutUtility.GetLastRect();
        }
        
        /// <summary>
        /// Draws a button that repaints immediately when hovered.
        /// </summary>
        internal static bool HoverableButton(Rect rect, GUIContent content, GUIStyle style = null)
        {
            // Stable ID across Layout/Repaint for this rect
            int id = GUIUtility.GetControlID(s_HoverButtonHash, FocusType.Passive, rect);

            // Ensure the window receives MouseMove in Inspector
            var win = EditorWindow.mouseOverWindow ?? EditorWindow.focusedWindow;
            
            if (win != null && !win.wantsMouseMove) 
                win.wantsMouseMove = true;

            // Cursor hint (also helps Unity deliver MouseMove reliably over this rect)
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Arrow);

            var e = Event.current;
            if (e.type 
                is EventType.MouseMove 
                or EventType.MouseEnterWindow 
                or EventType.MouseLeaveWindow
                or EventType.MouseDrag)
            {
                var nowHover = rect.Contains(e.mousePosition);
                var wasHover = s_Hovered.TryGetValue(id, out var prev) && prev;

                if (nowHover != wasHover)
                {
                    s_Hovered[id] = nowHover;
                    win?.Repaint(); // repaint on hover ENTER or EXIT
                }
            }

            return style == null 
                ? GUI.Button(rect, content) 
                : GUI.Button(rect, content, style);
        }
        
        /// <summary>
        /// Rounds a rect's edges onto the device pixel grid. GUI.DrawTexture's border path and
        /// EditorGUI.DrawRect do no snapping of their own, so a hairline whose edges land mid-pixel is
        /// spread across two rows at partial coverage and can resolve to nothing visible. Layout y is the
        /// running total of everything above a drawer, so unrelated content elsewhere in the inspector
        /// decides whether a given border survives. Both edges are snapped rather than position+size, so
        /// adjacent rects stay flush instead of drifting apart by a pixel.
        /// </summary>
        internal static Rect SnapToPixelGrid(Rect rect)
        {
            var ppp = EditorGUIUtility.pixelsPerPoint;

            if (ppp <= 0f)
                return rect;

            // Snap against the on-screen position rather than the local one. GUI groups translate
            // coordinates and a clipped group's origin is routinely fractional, so rounding the local value
            // alone puts the rect on a different device pixel depending on which groups are open - and that
            // set changes the moment a foldout starts animating.
            var origin = GUIUtility.GUIToScreenPoint(Vector2.zero);

            return Rect.MinMaxRect(
                Snap(rect.xMin, origin.x, ppp),
                Snap(rect.yMin, origin.y, ppp),
                Snap(rect.xMax, origin.x, ppp),
                Snap(rect.yMax, origin.y, ppp));

            static float Snap(float value, float originOffset, float pixelsPerPoint)
                => Mathf.Round((value + originOffset) * pixelsPerPoint) / pixelsPerPoint - originOffset;
        }

        internal static void DrawBorder(Rect rect, Color borderColor, Vector4? borderWidths = null, Vector4? borderRadiuses = null,bool drawShadow = false, Color? shadowColor = null)
        {
            borderWidths ??= Vector4.one;
            borderRadiuses ??= Vector4.zero;

            rect = SnapToPixelGrid(rect);

            if (drawShadow)
            {
                var shadowRect = SnapToPixelGrid(new Rect(rect.x, rect.yMax - 1f, rect.width, 2));
                GUI.DrawTexture(
                    shadowRect,
                    Texture2D.whiteTexture,
                    ScaleMode.StretchToFill, 
                    true, 
                    0f,
                    shadowColor ?? EditorGuiStyles.ShadowColor, 
                    borderWidths.Value, 
                    borderRadiuses.Value);
            }
            
            GUI.DrawTexture(
                rect, 
                Texture2D.whiteTexture, 
                ScaleMode.StretchToFill, 
                true, 
                0f, 
                borderColor, 
                borderWidths.Value, 
                borderRadiuses.Value);
        }
        
        
        internal static bool DragIntValue(Rect dragHotZone, ref int value)
        {
            var valueVec3Int = new Vector3Int(value, 0, 0);
            var isDragging = DragVectorIntValue(dragHotZone, ref valueVec3Int);
            value = valueVec3Int.x;
            return isDragging;
        }
        
        internal static bool DragVectorIntValue(Rect dragHotZone, ref Vector2Int value)
        {
            var valueVec3Int = (Vector3Int) value;
            var isDragging = DragVectorIntValue(dragHotZone, ref valueVec3Int);
            value = (Vector2Int) valueVec3Int;

            return isDragging;
        }
        
        internal static bool DragVectorIntValue(Rect dragHotZone, ref Vector3Int value)
        {
            if (Event.current.type == EventType.Layout)
                return false;
            
            var numberFieldValue = new NumberFieldValue
            {
                doubleVal = 0.0
            };
            
            DragNumberValue(dragHotZone, ref numberFieldValue, 0.25f);
            
            if (!numberFieldValue.success)
                return false;

            switch (s_DragCandidateState)
            {
                case DragCandidateState.NotDragging:
                {
                    break;
                }
                case DragCandidateState.InitiatedDragging:
                {
                    EditorGUIUtility.editingTextField = false;
                    GUIUtility.keyboardControl = 0;
                    s_DragStartValueVectorInt = value;
                    break;
                }
                case DragCandidateState.CurrentlyDragging:
                {
                    var delta = numberFieldValue.doubleVal;
                    s_CurrentValue += delta;

                    value.x = (int)(s_DragStartValueVectorInt.x + s_CurrentValue);
                    value.y = (int)(s_DragStartValueVectorInt.y + s_CurrentValue);
                    value.z = (int)(s_DragStartValueVectorInt.z + s_CurrentValue);
                    break;
                }
                case DragCandidateState.DragCancelled:
                {
                    value = s_DragStartValueVectorInt;
                    s_DragCandidateState = DragCandidateState.NotDragging;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            return true;
        }

        internal static bool DragFloatValue(Rect dragHotZone, ref float value)
        {
            var valueVec4 = new Vector4(value, 0, 0, 0);
            var isDragging = DragVectorValue(dragHotZone, ref valueVec4);
            value = valueVec4.x;
            return isDragging;
        }
        
        internal static bool DragVectorValue(Rect dragHotZone, ref Vector2 value)
        {
            var valueVec4 = (Vector4) value;
            var isDragging = DragVectorValue(dragHotZone, ref valueVec4);
            value = valueVec4;

            return isDragging;
        }
        
        internal static bool DragVectorValue(Rect dragHotZone, ref Vector3 value)
        {
            var valueVec4 = (Vector4) value;
            var isDragging = DragVectorValue(dragHotZone, ref valueVec4);
            value = valueVec4;

            return isDragging;
        }

        internal static bool DragVectorValue(Rect dragHotZone, ref Vector4 value)
        {
            if (Event.current.type == EventType.Layout)
                return false;
            
            var numberFieldValue = new NumberFieldValue
            {
                doubleVal = 0.0
            };
            
            DragNumberValue(dragHotZone, ref numberFieldValue);
            
            if (!numberFieldValue.success)
                return false;

            switch (s_DragCandidateState)
            {
                case DragCandidateState.NotDragging:
                {
                    break;
                }
                case DragCandidateState.InitiatedDragging:
                {
                    EditorGUIUtility.editingTextField = false;
                    GUIUtility.keyboardControl = 0;
                    s_DragStartValueVector = value;
                    break;
                }
                case DragCandidateState.CurrentlyDragging:
                {
                    var delta = numberFieldValue.doubleVal;

                    value.x = GuiUtility.ClampToFloat(GuiUtility.RoundBasedOnMinimumDifference(value.x + delta, s_DragSensitivity));
                    value.y = GuiUtility.ClampToFloat(GuiUtility.RoundBasedOnMinimumDifference(value.y + delta, s_DragSensitivity));
                    value.z = GuiUtility.ClampToFloat(GuiUtility.RoundBasedOnMinimumDifference(value.z + delta, s_DragSensitivity));
                    value.w = GuiUtility.ClampToFloat(GuiUtility.RoundBasedOnMinimumDifference(value.w + delta, s_DragSensitivity));
                    break;
                }
                case DragCandidateState.DragCancelled:
                {
                    value = s_DragStartValueVector;
                    s_DragCandidateState = DragCandidateState.NotDragging;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            return true;
        }

        private static void DragNumberValue(Rect dragHotZone, ref NumberFieldValue value, float dragSens = -1f)
        {
            var id = GUIUtility.GetControlID(s_DragNumberValueHash, FocusType.Keyboard, dragHotZone);

            var dragSensitivity = 
                Event.current.GetTypeForControl(id) == EventType.MouseDown 
                ? Mathf.Approximately(dragSens, -1f) ? (float) GuiUtility.CalculateFloatDragSensitivity(s_DragStartValue) : dragSens 
                : 0.0f;
            
            var current = Event.current;
            
            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (!GuiUtility.HitTest(dragHotZone, current) || current.button != 0)
                        break;
                    EditorGUIUtility.editingTextField = false;
                    GUIUtility.hotControl = id;
                    _endActiveEditing();
                    current.Use();
                    GUIUtility.keyboardControl = id;
                    s_DragCandidateState = DragCandidateState.InitiatedDragging;
                    s_DragStartValue = value.doubleVal;
                    s_CurrentValue = 0;
                    value.success = true;
                    s_DragStartPos = current.mousePosition;
                    s_DragSensitivity = dragSensitivity;
                    current.Use();
                    EditorGUIUtility.SetWantsMouseJumping(1);
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl != id || s_DragCandidateState == 0)
                        break;
                    GUIUtility.hotControl = 0;
                    s_DragCandidateState = DragCandidateState.NotDragging;
                    s_CurrentValue = 0;
                    current.Use();
                    EditorGUIUtility.SetWantsMouseJumping(0);
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id)
                        break;
                    switch (s_DragCandidateState)
                    {
                        case DragCandidateState.InitiatedDragging:
                            if ((Event.current.mousePosition - s_DragStartPos).sqrMagnitude > 16.0)
                            {
                                s_DragCandidateState = DragCandidateState.CurrentlyDragging;
                                GUIUtility.keyboardControl = id;
                            }

                            current.Use();
                            break;
                        case DragCandidateState.CurrentlyDragging:
                            value.doubleVal += HandleUtility.niceMouseDelta * s_DragSensitivity;
                            value.doubleVal = GuiUtility.RoundBasedOnMinimumDifference(value.doubleVal, s_DragSensitivity);

                            value.success = true;
                            GUI.changed = true;
                            current.Use();
                            break;
                    }

                    break;
                case EventType.KeyDown:
                    if (GUIUtility.hotControl != id || current.keyCode != KeyCode.Escape ||
                        s_DragCandidateState == 0)
                        break;
                    value.doubleVal = s_DragStartValue;
                    value.success = true;
                    s_DragCandidateState = DragCandidateState.DragCancelled;
                    GUI.changed = true;
                    GUIUtility.hotControl = 0;
                    current.Use();
                    break;
                case EventType.Repaint:
                    EditorGUIUtility.AddCursorRect(dragHotZone, MouseCursor.SlideArrow);
                    break;
            }
        }
        
        internal static object BetterEnumField(Rect rect, GUIContent label, Type enumType, object currentValue)
        {
            if (enumType == null || !enumType.IsEnum)
                throw new ArgumentException("enumType must be an enum", nameof(enumType));

            // 1) Normalize to a boxed enum instance of the correct runtime type
            Enum before = EnumExtensions.NormalizeToEnum(enumType, currentValue);

            // 2) Draw
            Enum after = EditorGUI.EnumPopup(rect, label, before);

            // 3) Convert back to underlying integral bits (safe for signed/unsigned)
            ulong bits = after.EnumToUInt64();

            // 4) Return as the *underlying* integral boxed, so callers can SetValue or write to SerializedProperty
            return EnumExtensions.UInt64ToUnderlying(bits, Enum.GetUnderlyingType(enumType));
        }

        internal static object BetterEnumFlagsField(Rect rect, GUIContent label, Type enumType, object currentValue,
            bool includeObsolete = false)
        {
            if (enumType == null || !enumType.IsEnum)
                throw new ArgumentException("enumType must be an enum", nameof(enumType));
            
            var before = EnumExtensions.NormalizeToEnum(enumType, currentValue);
            
            var after = includeObsolete
                ? EditorGUI.EnumFlagsField(rect, label, before, true, EditorStyles.popup)
                : EditorGUI.EnumFlagsField(rect, label, before, EditorStyles.popup);

            var bits = after.EnumToUInt64();
            return EnumExtensions.UInt64ToUnderlying(bits, Enum.GetUnderlyingType(enumType));
        }

        private static Action Create()
        {
            try
            {
                // EditorGUI.activeEditor?.EndEditing()
                var eguiType = typeof(EditorGUI);
                var activeEditorProp = eguiType.GetProperty("activeEditor",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (activeEditorProp != null)
                {
                    var editorType = activeEditorProp.PropertyType; // UnityEditor.TextEditor
                    var endEditing = editorType?.GetMethod("EndEditing",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (endEditing != null)
                    {
                        return () =>
                        {
                            var inst = activeEditorProp.GetValue(null, null);
                            if (inst != null) endEditing.Invoke(inst, null);
                        };
                    }
                }
            }
            catch
            {
                /* swallow – we’ll use the fallback */
            }

            // Fallback: stop text editing & drop focus
            return () =>
            {
                EditorGUIUtility.editingTextField = false;
                GUIUtility.keyboardControl = 0;
            };
        }

        internal struct NumberFieldValue
        {
            public double doubleVal;
            public bool success;
        }

        private enum DragCandidateState
        {
            NotDragging,
            InitiatedDragging,
            CurrentlyDragging,
            DragCancelled
        }
        
        internal class ColorScope : GUI.Scope
        {
            private readonly Color _color;
            private readonly Color _backgroundColor;
            private readonly Color _contentColor;

            public ColorScope(Color? color = null, Color? backgroundColor = null, Color? contentColor = null)
            {
                _color = GUI.color; 
                _backgroundColor = GUI.backgroundColor; 
                _contentColor = GUI.contentColor;
                
                if (color.HasValue) 
                    GUI.color = color.Value;
                
                if (backgroundColor.HasValue) 
                    GUI.backgroundColor = backgroundColor.Value;
                
                if (contentColor.HasValue) 
                    GUI.contentColor = contentColor.Value;
            }

            protected override void CloseScope()
            {                
                GUI.color = _color;
                GUI.backgroundColor = _backgroundColor;
                GUI.contentColor = _contentColor;
            }
        }
        
        /// <summary>
        /// Greys out and blocks input to everything drawn inside the scope, restoring the previous state
        /// on dispose.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="EditorGUI.BeginDisabledGroup"/>, nesting an enabled scope inside a disabled
        /// one re-enables its contents, because the previous value is stacked rather than combined.
        /// </remarks>
        public class DisabledScope : GUI.Scope
        {
            private static readonly Stack<bool> s_Stack = new();

            /// <summary>
            /// Opens the scope.
            /// </summary>
            /// <param name="disabled">True to disable the contents, false to leave them interactive.</param>
            public DisabledScope(bool disabled)
            {
                s_Stack.Push(GUI.enabled);
                GUI.enabled = !disabled;
            }
            
            /// <inheritdoc/>
            protected override void CloseScope() => GUI.enabled = s_Stack.Pop();
        }
        
        /// <summary>
        /// Draws the contents in Unity's "multiple values" state - the em-dash placeholder shown when a
        /// multi-object selection disagrees on a field - restoring the previous state on dispose.
        /// </summary>
        /// <remarks>
        /// Pass <c>Property.hasMultipleDifferentValues</c> to get the behaviour the built-in drawers
        /// have. Scopes nest, so an inner scope may clear the state for part of a compound control.
        /// </remarks>
        public class MixedValueScope : GUI.Scope
        {
            /// <summary>
            /// Opens the scope.
            /// </summary>
            /// <param name="mixed">True to draw the contents as having multiple differing values.</param>
            public MixedValueScope(bool mixed)
            {
                EditorGuiUtility.PushMixedValue(mixed);
            }
            
            /// <inheritdoc/>
            protected override void CloseScope()
            {
                EditorGuiUtility.PopMixedValue();
            }
        }
    }
}