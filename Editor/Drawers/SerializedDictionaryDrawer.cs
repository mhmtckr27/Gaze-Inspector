using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GrayWolf.Gaze.Collections;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal sealed class SerializedDictionaryDrawer : PropertyDrawerBase, IContextMenuProvider
    {
        private const float HeaderH = 22f;
        private const float BtnW = 16f;
        private const float BtnAddW = 20f;
        private const float SeparatorW = 1f;
        private const float RowPadRight = 3f;
        private const float ValueColumnGap = 4f;
        private const float ValueLabelShare = 0.35f;
        private const float MinKeyColW = 60f;
        private const float MinValueColW = 60f;
        private const float AddRowH = 20f;
        private const float SearchH = 18f;
        private const float DividerHitW = 7f;
        private const float CountLabelW = 140f;

        private static readonly GUIContent AddEntryLabel = new("Add");

        private readonly List<int> _toRemoveIndices = new();
        private readonly List<int> _displayOrder = new();
        private readonly List<int> _pageIndices = new();
        private readonly CollectionPageState _pageState = new();
        private readonly HashSet<int> _duplicateIndices = new();
        private readonly List<string> _duplicateKeyLabels = new();
        private readonly Dictionary<string, DrawerBase> _elementDrawers = new();
        private readonly Dictionary<object, int> _seenKeys = new();

        private SerializedProperty _keyData;
        private SerializedProperty _valueData;
        private SerializedDictionaryAttribute _attribute;
        private Type _keyType;
        private Type _valueType;
        private MethodInfo _invalidateMethod;
        private bool _warnedMissingTitleField;
        private int _valueFieldCount = -1;
        private object _defaultKey;
        private object _defaultValue;
        private float _keyColWidth = 120f;
        private float _colAvailableWidth;
        private float _rowContentWidth;
        private float _dividerX;
        private Rect _columnHeaderRect;
        private bool _draggingDivider;
        private int _searchThreshold = 10;
        private string _focusControlName;
        private Rect _rect;

        private SerializedDictionaryViewState _state;

        private SerializedDictionaryViewState State =>
            _state ??= PropertyStateStore.GetOrCreate<SerializedDictionaryViewState>(Property, out _);

        protected internal override void Initialize()
        {
            _state = null;
            _elementDrawers.Clear();
            _valueFieldCount = -1;

            var (field, attrs) = ReflectionCache.GetFieldInfoAndAttrs(Property);
            _attribute = attrs?.OfType<SerializedDictionaryAttribute>().FirstOrDefault();

            for (var t = field?.FieldType; t != null && t != typeof(object); t = t.BaseType)
            {
                if (!t.IsGenericType || t.GetGenericTypeDefinition() != typeof(SerializedDictionary<,>))
                    continue;

                var args = t.GetGenericArguments();
                _keyType = args[0];
                _valueType = args[1];
                _invalidateMethod = t.GetMethod("Invalidate");
                break;
            }

            _defaultKey = DefaultValueFor(_keyType);
            _defaultValue = DefaultValueFor(_valueType);

            _focusControlName = $"SerializedDictionaryFocusKey_{Property.propertyPath}";
        }

        private static object DefaultValueFor(Type type)
        {
            if (type == null) return null;
            if (type == typeof(string)) return string.Empty;
            if (type.IsValueType) return Activator.CreateInstance(type);

            if (typeof(UnityEngine.Object).IsAssignableFrom(type)
                || type.IsAbstract
                || type.IsArray
                || type.GetConstructor(Type.EmptyTypes) == null)
                return null;

            try
            {
                return Activator.CreateInstance(type);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void ApplyDefault(SerializedProperty element, object value)
        {
            if (value == null && element.propertyType == SerializedPropertyType.Generic)
                return;

            element.boxedValue = value;
        }

        private string KeyLabel => string.IsNullOrEmpty(_attribute?.Key) ? "Key" : _attribute.Key;
        private string ValueLabel => string.IsNullOrEmpty(_attribute?.Value) ? "Value" : _attribute.Value;
        private int ElementCount => Math.Min(_keyData?.arraySize ?? 0, _valueData?.arraySize ?? 0);
        private bool IsMultiEditing => _keyData?.serializedObject?.isEditingMultipleObjects ?? false;

        protected internal override void CollectMessages(List<GazeMessage> messages)
        {
            var keyData = Property.GetKeyData();
            var valueData = Property.GetValueData();

            if (keyData.arraySize == valueData.arraySize)
            {
                if (_duplicateKeyLabels.Count == 0)
                    return;

                var noun = _duplicateKeyLabels.Count > 1 ? "keys" : "key";
                var message = $"Duplicate {noun}: {string.Join(", ", _duplicateKeyLabels)}";
                messages.Add(new GazeMessage(InfoMessageType.Error, message));
                return;
            }

            var shown = Math.Min(keyData.arraySize, valueData.arraySize);
            messages.Add(new GazeMessage(InfoMessageType.Warning,
                $"Key/value count mismatch ({keyData.arraySize} keys, {valueData.arraySize} values). Showing the first {shown}."));
        }

        public override void Draw(GUIContent label)
        {
            _keyData = Property.GetKeyData();
            _valueData = Property.GetValueData();

            if (Event.current.type == EventType.Layout)
            {
                RecomputeKeyColumnWidth();
                RecomputeDuplicates();
                RecomputeDisplayOrder();
                _pageState.Recompute(_displayOrder, GazeInspectorSettings.instance.PageSize, _pageIndices);

                // The focused row only clears FocusIndex when it actually draws, so a new entry that
                // landed off-page would keep the request alive and steal focus on some later page.
                if (State.FocusIndex >= 0 && !_pageIndices.Contains(State.FocusIndex))
                    State.FocusIndex = -1;
            }

            var toolbar = EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.Height(HeaderH + 2));
            var header = GUILayoutUtility.GetRect(0, HeaderH + 2, GUILayout.ExpandWidth(true));

            using (new EditorGui.ColorScope(EditorGuiStyles.EvenRowColor))
            {
                EditorGUI.DrawRect(toolbar, Color.white);
            }

            var isExpanded = DrawHeader(header);
            EditorGUILayout.EndHorizontal();

            // Measured from the toolbar, which is drawn before the foldout and so is never inside the fade
            // group. Taking this from a row rect instead made every column measurement depend on whether an
            // expand/collapse animation was running, drifting the divider and the value column's label
            // width for the length of the tween and snapping them back at the end.
            if (Event.current.type == EventType.Repaint)
            {
                _rowContentWidth = toolbar.width - EditorGuiStyles.CollectionDrawerBox.padding.horizontal;
                _pageState.MeasureHeader(toolbar.width);
            }

            PropertyTreeGUI.BeginMeasuring(EditorGuiStyles.CollectionDrawerBox);

            var hasRows = _pageIndices.Count > 0;

            if (PropertyTreeGUI.BeginFoldout(Property, isExpanded))
            {
                if (ElementCount >= _searchThreshold)
                    DrawSearch();

                if (hasRows)
                    DrawColumnHeaders();

                for (var row = 0; row < _pageIndices.Count; row++)
                {
                    DrawRow(_pageIndices[row], row);
                }

                PropertyTreeGUI.EndFoldout();
                _rect = PropertyTreeGUI.EndMeasuring();

                if (hasRows)
                    HandleDividerDrag();

                EditorGui.DrawBorder(toolbar, EditorGuiStyles.BorderColor, Vector4.one, drawShadow: hasRows,
                    shadowColor: EditorGuiStyles.EvenRowColor * EditorGuiStyles.ShadowColor);

                if (hasRows)
                    EditorGui.DrawBorder(_rect, EditorGuiStyles.BorderColor, new Vector4(1, 0, 1, 1));

                GUILayoutUtility.GetRect(0, 2);
            }
            else
            {
                EditorGui.DrawBorder(toolbar, EditorGuiStyles.BorderColor, Vector4.one);
                PropertyTreeGUI.EndFoldout();
                _rect = PropertyTreeGUI.EndMeasuring();
                GUILayoutUtility.GetRect(0, 1);
            }

            if (Event.current.type != EventType.Repaint)
                return;

            ContextClickArea = toolbar;
            var area = toolbar;
            area.yMax = _rect.yMax;
            PropertyArea = area;

            if (_toRemoveIndices.Count == 0)
                return;

            _toRemoveIndices.Sort();
            for (var i = _toRemoveIndices.Count - 1; i >= 0; i--)
            {
                RemoveEntry(_toRemoveIndices[i]);
            }
            _toRemoveIndices.Clear();
            _elementDrawers.Clear();

            GUI.changed = true;
            Property.LayoutChanged();
            Invalidate();
            Tree.RebuildNode(this);
        }

        private bool DrawHeader(Rect rect)
        {
            EditorGuiUtility.PushHierarchyMode(false);

            // Read the shared state, not Property.isExpanded: each Inspector keeps its own copy of
            // isExpanded, and two Inspectors on one object kept flipping the shared animation state between
            // their two copies. Read before Foldout, not from what it returns: BeginFoldout writes the new
            // state after this runs, so the control count stays identical across the frame that toggles it.
            var expanded = PropertyTreeGUI.IsExpanded(Property);
            var showPagination = _pageState.Paginates && expanded;

            var newExpanded = expanded;
            if (ElementCount > 0)
            {
                newExpanded = EditorGUI.Foldout(rect, expanded, Data.Label, true, EditorGuiStyles.Foldout);
            }
            else
            {
                GUILayout.Space(10);
                GUI.Label(rect, Data.Label, EditorGuiStyles.LabelLeft);
            }

            using (new EditorGUI.DisabledScope(true))
            {
                // Always reserve this rect, even when ShowsCountLabel says not to draw into it: that
                // decision depends on a header width measured one pass and consumed the next, so on a
                // window resize Layout and Repaint can disagree about it. Making the GetRect call itself
                // unconditional keeps both passes allocating the same number of controls regardless, so a
                // stale width can no longer desync them into a "Getting control N's position in a group
                // with only M controls" abort mid-collection.
                var itemsCountRect = GUILayoutUtility.GetRect(CountLabelW, 0f, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(false));
                if (!showPagination || _pageState.ShowsCountLabel(CountLabelW))
                    EditorGUI.LabelField(itemsCountRect, new GUIContent(GetEntryCountText()), EditorGuiStyles.LabelRight);
            }

            GUILayout.Space(3);
            var separatorRect = GUILayoutUtility.GetRect(1, 0, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(separatorRect, new Color(0, 0, 0, 0.375f));

            if (showPagination)
                _pageState.DrawHeaderControls();

            BeginCentered(BtnAddW, BtnAddW);
            var plusRect = GUILayoutUtility.GetRect(BtnAddW, BtnAddW, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
            EndCentered();

            using (new EditorGUI.DisabledScope(IsMultiEditing))
            {
                if (EditorGui.HoverableButton(plusRect, GUIContent.none, EditorGuiStyles.AddButton))
                {
                    AppendEntry();
                    // Adding to a collapsed dictionary used to hide the new entry. Returned rather than
                    // written to isExpanded, so BeginFoldout opens it through the shared state.
                    newExpanded = true;
                }
            }

            EditorGuiUtility.PopHierarchyMode();

            return newExpanded;
        }

        private void RecomputeKeyColumnWidth()
        {
            // _rowContentWidth is the width the rows were actually laid out at, captured on the previous
            // Repaint. currentViewWidth is only correct for a top-level dictionary - it ignores indentation
            // and any enclosing drawer, which desynced the column width from the divider's hit rect.
            var contentWidth = _rowContentWidth > 0f ? _rowContentWidth : EditorGUIUtility.currentViewWidth;

            var available = contentWidth
                            - EditorGuiStyles.DictionaryItemEvenIndex.padding.horizontal
                            - BtnW - RowPadRight - SeparatorW;

            _colAvailableWidth = Mathf.Max(MinKeyColW + MinValueColW, available);

            var max = _colAvailableWidth - MinValueColW;
            _keyColWidth = Mathf.Clamp(_colAvailableWidth * State.KeyColumnFraction, MinKeyColW, max);
        }

        /// <summary>
        /// The value column is a table cell, not a row. Deriving its label width from the inspector's
        /// global labelWidth - the way ListDrawer does - only works when content starts near the left
        /// edge; here the column begins behind the key column, at roughly where the global label column
        /// ends, so the subtraction collapses to nothing on a narrow inspector. Sizing the label against
        /// the column itself scales instead of hitting a floor, and the share is self-limiting: the field
        /// always keeps the remaining fraction, so a label can never push it out of the cell.
        /// </summary>
        private float ValueColumnLabelWidth()
        {
            var valueColumnWidth = Mathf.Max(MinValueColW, _colAvailableWidth - _keyColWidth - ValueColumnGap);

            // The floor yields to the column when the column is smaller than the floor, so it can only
            // raise a uselessly small label, never overflow the cell.
            var floor = Mathf.Min(EditorGuiStyles.MinLabelWidth, valueColumnWidth * 0.5f);

            return Mathf.Max(valueColumnWidth * ValueLabelShare, floor);
        }

        private void HandleDividerDrag()
        {
            if (_columnHeaderRect.height <= 0f || _colAvailableWidth <= 0f)
                return;

            var hit = new Rect(_dividerX - DividerHitW * 0.5f, _columnHeaderRect.y,
                DividerHitW, Mathf.Max(_columnHeaderRect.height, _rect.yMax - _columnHeaderRect.y));

            var e = Event.current;

            if (e.type == EventType.Repaint)
            {
                EditorGUIUtility.AddCursorRect(hit, MouseCursor.ResizeHorizontal);
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && hit.Contains(e.mousePosition))
            {
                _draggingDivider = true;
                GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                e.Use();
                return;
            }

            if (!_draggingDivider)
                return;

            if (e.type == EventType.MouseDrag)
            {
                var offset = e.mousePosition.x - _columnHeaderRect.x;
                var min = MinKeyColW / _colAvailableWidth;
                var max = (_colAvailableWidth - MinValueColW) / _colAvailableWidth;

                State.KeyColumnFraction = Mathf.Clamp(offset / _colAvailableWidth, min, max);

                GUI.changed = true;
                e.Use();
                return;
            }

            if (e.type is EventType.MouseUp or EventType.Ignore)
            {
                _draggingDivider = false;
                GUIUtility.hotControl = 0;
                e.Use();
            }
        }

        private void RecomputeDuplicates()
        {
            _duplicateIndices.Clear();
            _duplicateKeyLabels.Clear();
            _seenKeys.Clear();

            for (var i = 0; i < ElementCount; i++)
            {
                var key = _keyData.GetArrayElementAtIndex(i).GetValue();
                if (key == null)
                    continue;

                if (!_seenKeys.TryGetValue(key, out var firstIndex))
                {
                    _seenKeys[key] = i;
                    continue;
                }

                if (_duplicateIndices.Add(firstIndex))
                    _duplicateKeyLabels.Add(key.ToString());

                _duplicateIndices.Add(i);
            }
        }

        private void RecomputeDisplayOrder()
        {
            _displayOrder.Clear();

            for (var i = 0; i < ElementCount; i++)
            {
                if (MatchesSearch(i))
                    _displayOrder.Add(i);
            }

            if (State.SortEnabled)
                _displayOrder.Sort(CompareByKey);
        }

        private bool MatchesSearch(int index)
        {
            var searchText = State.SearchText;

            if (string.IsNullOrEmpty(searchText))
                return true;

            var keyText = _keyData.GetArrayElementAtIndex(index).GetValue()?.ToString() ?? string.Empty;
            if (keyText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            var valueText = _valueData.GetArrayElementAtIndex(index).GetValue()?.ToString() ?? string.Empty;
            return valueText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private int CompareByKey(int a, int b)
        {
            var keyA = _keyData.GetArrayElementAtIndex(a).GetValue();
            var keyB = _keyData.GetArrayElementAtIndex(b).GetValue();

            if (keyA is IComparable comparable)
                return comparable.CompareTo(keyB);

            return string.Compare(keyA?.ToString(), keyB?.ToString(), StringComparison.Ordinal);
        }

        private string GetEntryCountText()
        {
            if (ElementCount == 0)
                return "Empty";

            var text = ElementCount == 1 ? "1 item" : $"{ElementCount} items";

            if (_duplicateIndices.Count > 0)
                text += $", {_duplicateIndices.Count} duplicate{(_duplicateIndices.Count > 1 ? "s" : "")}";

            return text;
        }

        private void AppendEntry()
        {
            // Append at each array's own end. ElementCount is the *min* of the two, so using it on an
            // already-mismatched dictionary would insert into the middle of the longer array and shift
            // every entry past that point onto a different partner.
            var keyIndex = _keyData.arraySize;
            var valueIndex = _valueData.arraySize;

            _keyData.InsertArrayElementAtIndex(keyIndex);
            _valueData.InsertArrayElementAtIndex(valueIndex);

            ApplyDefault(_keyData.GetArrayElementAtIndex(keyIndex), _defaultKey);
            ApplyDefault(_valueData.GetArrayElementAtIndex(valueIndex), _defaultValue);

            _keyData.serializedObject.ApplyModifiedProperties();

            ElementIdDatabase.OnInsert(_keyData, keyIndex);
            ElementIdDatabase.OnInsert(_valueData, valueIndex);

            Invalidate();

            _elementDrawers.Clear();

            State.SearchText = string.Empty;
            State.FocusIndex = Math.Min(keyIndex, valueIndex);
            GUI.changed = true;
        }

        private void DrawSearch()
        {
            var hasText = !string.IsNullOrEmpty(State.SearchText);
            var fieldStyle = (GUIStyle) "SearchTextField";
            var cancelStyle = (GUIStyle) (hasText ? "SearchCancelButton" : "SearchCancelButtonEmpty");

            var cancelW = cancelStyle.fixedWidth > 0f ? cancelStyle.fixedWidth : 14f;
            var row = EditorGUILayout.GetControlRect(false, SearchH, GUILayout.ExpandWidth(true));

            var fieldRect = new Rect(row.x, row.y, row.width - cancelW, row.height);
            var cancelRect = new Rect(fieldRect.xMax, row.y, cancelW, row.height);

            State.SearchText = EditorGUI.TextField(fieldRect, State.SearchText, fieldStyle);

            if (GUI.Button(cancelRect, GUIContent.none, cancelStyle) && hasText)
            {
                State.SearchText = string.Empty;
                GUI.FocusControl(null);
            }
        }

        private void DrawAddRow()
        {
            var style = EditorGuiStyles.ListHeaderLabel;
            var row = EditorGUILayout.BeginHorizontal(style, GUILayout.Height(AddRowH));
            var rect = GUILayoutUtility.GetRect(0, AddRowH, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(IsMultiEditing))
            {
                if (EditorGui.HoverableButton(rect, AddEntryLabel, EditorGuiStyles.AddButtonFull))
                    AppendEntry();
            }

            if (Event.current.type == EventType.Repaint && ElementCount > 0)
            {
                var border = new Rect(
                    row.x - style.margin.left,
                    row.y - style.margin.top,
                    row.width + style.margin.horizontal,
                    row.height + style.margin.vertical);
                EditorGui.DrawBorder(border, EditorGuiStyles.BorderColor, new Vector4(0, 1, 0, 0));
            }
        }

        private void DrawColumnHeaders()
        {
            var style = EditorGuiStyles.ListHeaderLabel;

            var row = EditorGUILayout.BeginHorizontal(style);
            GUILayout.Label(KeyLabel, EditorGuiStyles.LabelCenteredSmall, GUILayout.Width(_keyColWidth));

            // Only the x of this rect is used - the divider below is drawn from the header's border rect -
            // so the old ExpandHeight served no purpose beyond making this row height-stretchable.
            var separatorRect = GUILayoutUtility.GetRect(SeparatorW, 0, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

            GUILayout.Label(ValueLabel, EditorGuiStyles.LabelCenteredSmall, GUILayout.ExpandWidth(true));
            GUILayout.Space(BtnW + RowPadRight);
            EditorGUILayout.EndHorizontal();

            if (Event.current.type != EventType.Repaint)
                return;

            // row excludes margin but includes padding; expand to the strip the background actually occupies
            var border = new Rect(
                row.x - style.margin.left,
                row.y - style.margin.top,
                row.width + style.margin.horizontal,
                row.height + style.margin.vertical);

            var upperBorder = new Rect(
                border.x,
                border.y - border.height,
                border.width,
                border.height);
            
            var divider = new Rect(separatorRect.x, border.y, SeparatorW, border.height);

            EditorGUI.DrawRect(EditorGui.SnapToPixelGrid(divider), EditorGuiStyles.BorderColor);
            EditorGui.DrawBorder(border, EditorGuiStyles.BorderColor, new Vector4(0, 0, 0, 1));
            EditorGui.DrawBorder(upperBorder, EditorGuiStyles.BorderColor, new Vector4(0, 0, 0, 1));

            _columnHeaderRect = border;
            _dividerX = separatorRect.x;
        }

        private void DrawRow(int index, int row)
        {
            var keyElement = _keyData.GetArrayElementAtIndex(index);
            var valueElement = _valueData.GetArrayElementAtIndex(index);

            var listItem = PropertyTreeGUI.BeginListItem(row, EditorGuiStyles.DictionaryItemEvenIndex, EditorGuiStyles.DictionaryItemOddIndex);

            if (Event.current.type == EventType.Repaint && _duplicateIndices.Contains(index))
                EditorGUI.DrawRect(listItem, EditorGuiStyles.DuplicateKeyTint);

            using (new EditorGUI.DisabledScope(IsMultiEditing))
            {
                BeginColumn(_keyColWidth);
                EditorGuiUtility.PushLabelWidth(1f);
                DrawKeyElement(keyElement, index);
                EditorGuiUtility.PopLabelWidth();
                EndColumn();

                // Reserve the divider's width only. Its height comes from the row rect, because asking
                // layout for a full-height rect here (ExpandHeight) is what made the row stretchable.
                var separatorRect = GUILayoutUtility.GetRect(SeparatorW, 0, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

                if (Event.current.type == EventType.Repaint)
                {
                    var padding = EditorGuiStyles.DictionaryItemEvenIndex.padding;
                    var divider = new Rect(
                        separatorRect.x + 2,
                        listItem.y + padding.top - 4,
                        SeparatorW,
                        listItem.height - padding.vertical + 8);

                    // x derives from _keyColWidth, which is a fraction of the row width and so is almost
                    // never whole - without snapping this hairline blurs or drops out entirely.
                    EditorGUI.DrawRect(EditorGui.SnapToPixelGrid(divider), EditorGuiStyles.BorderColor);
                }

                GUILayout.Space(ValueColumnGap);
                BeginColumnExpanding();
                EditorGuiUtility.PushLabelWidth(ValueColumnLabelWidth());
                DrawValueElement(valueElement, index);
                EditorGuiUtility.PopLabelWidth();
                EndColumn();

                GUILayout.Space(BtnW + RowPadRight);

                var deleteRect = new Rect(listItem.xMax - BtnW - RowPadRight, listItem.y, BtnW, listItem.height);
                DrawDeleteButton(deleteRect, index);
            }

            PropertyTreeGUI.EndListItem();
        }

        private void DrawKeyElement(SerializedProperty keyElement, int index)
        {
            if (index == State.FocusIndex)
                GUI.SetNextControlName(_focusControlName);

            EditorGUI.BeginChangeCheck();
            DrawElement(keyElement, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                // Invalidate() rebuilds the runtime lookup by reflecting onto the live instance, so the
                // edit has to be flushed to it first - otherwise the lookup is rebuilt from the old key.
                _keyData.serializedObject.ApplyModifiedProperties();
                Invalidate();
            }

            if (index == State.FocusIndex && Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl(_focusControlName);
                State.FocusIndex = -1;
            }
        }

        private void DrawValueElement(SerializedProperty valueElement, int index)
        {
            if (!DrawsValueAsFoldout(valueElement))
            {
                DrawElement(valueElement, GUIContent.none);
                return;
            }

            EditorGuiUtility.PushIndentLevel(EditorGUI.indentLevel + 1);
            if (PropertyTreeGUI.BeginFoldout(valueElement, ValueLabelFor(valueElement, index), out _))
                DrawElement(valueElement, GUIContent.none);
            EditorGuiUtility.PopIndentLevel();
            PropertyTreeGUI.EndFoldout();
        }

        private bool DrawsValueAsFoldout(SerializedProperty valueElement)
        {
            if (valueElement.propertyType != SerializedPropertyType.Generic &&
                valueElement.propertyType != SerializedPropertyType.Bounds)
                return false;

            if (!string.IsNullOrEmpty(_attribute?.ValueTitleFieldName))
                return true;

            if (_valueFieldCount < 0)
                _valueFieldCount = valueElement.Children().Count();

            return _valueFieldCount > GazeInspectorSettings.instance.DictionaryFoldoutThreshold;
        }

        private void DrawElement(SerializedProperty element, GUIContent label)
        {
            // BuildNode re-runs InitializeInternal (reflection, attribute lookup, a fresh GUIContent) on
            // every event. Resolve each element once and reuse it; the cache is dropped whenever the
            // arrays change, since propertyPath-keyed drawers would then describe a different element.
            var path = element.propertyPath;

            if (!_elementDrawers.TryGetValue(path, out var drawer))
            {
                drawer = Tree.BuildNode(element);
                _elementDrawers[path] = drawer;
            }

            PropertyTreeGUI.DrawNode(drawer, label);
        }

        private GUIContent ValueLabelFor(SerializedProperty valueElement, int index)
        {
            var title = TitleTextFor(valueElement);

            return new GUIContent(string.IsNullOrEmpty(title) ? $"Element {index}" : title);
        }

        private string TitleTextFor(SerializedProperty valueElement)
        {
            if (string.IsNullOrEmpty(_attribute?.ValueTitleFieldName))
                return null;

            var titleProp = valueElement.FindPropertyRelative(_attribute.ValueTitleFieldName);
            if (titleProp == null)
            {
                if (!_warnedMissingTitleField)
                {
                    _warnedMissingTitleField = true;
                    Debug.LogWarning($"[Gaze] {nameof(SerializedDictionaryDrawer)} | " +
                                     $"'{Property.propertyPath}' names " +
                                     $"{nameof(SerializedDictionaryAttribute.ValueTitleFieldName)} " +
                                     $"'{_attribute.ValueTitleFieldName}', which is not a serialized field " +
                                     $"of {_valueType?.Name ?? "the value type"}. Rows fall back to their " +
                                     $"element number.");
                }

                return null;
            }

            if (titleProp.propertyType == SerializedPropertyType.Enum)
            {
                var names = titleProp.enumDisplayNames;
                var enumIndex = titleProp.enumValueIndex;

                return enumIndex >= 0 && enumIndex < names.Length ? names[enumIndex] : null;
            }

            var value = titleProp.GetValue();

            if (value is UnityEngine.Object unityObject && unityObject == null)
                return null;

            return value?.ToString();
        }

        private void DrawDeleteButton(Rect rect, int index)
        {
            rect.y += (rect.height - BtnW) / 2;
            rect.height = BtnW;

            if (EditorGui.HoverableButton(rect, GUIContent.none, EditorGuiStyles.RemoveButton))
                _toRemoveIndices.Add(index);
        }

        private void ClearDictionary()
        {
            for (var i = 0; i < ElementCount; i++)
            {
                _toRemoveIndices.Add(i);
            }
        }

        private static void DeleteElement(SerializedProperty arrayProperty, int index)
        {
            var oldSize = arrayProperty.arraySize;
            if (oldSize <= index)
                return;

            arrayProperty.DeleteArrayElementAtIndex(index);

            if (arrayProperty.arraySize == oldSize)
                arrayProperty.DeleteArrayElementAtIndex(index); // object refs need two calls
        }

        private void RemoveEntry(int index)
        {
            DeleteElement(_keyData, index);
            DeleteElement(_valueData, index);

            ElementIdDatabase.OnDelete(_keyData, index);
            ElementIdDatabase.OnDelete(_valueData, index);
        }

        private void Invalidate()
        {
            if (_invalidateMethod == null)
                return;

            var target = ResolveTargetInstance();
            if (target != null)
                _invalidateMethod.Invoke(target, null);
        }

        private object ResolveTargetInstance()
        {
            object current = Property.serializedObject.targetObject;
            var segments = Property.propertyPath.Split('.');

            for (var i = 0; i < segments.Length && current != null; i++)
            {
                var segment = segments[i];

                if (segment == "Array" && i + 1 < segments.Length && segments[i + 1].StartsWith("data["))
                {
                    i++;
                    var indexText = segments[i];
                    var index = int.Parse(indexText.Substring(5, indexText.Length - 6));
                    current = ((IList) current)[index];
                    continue;
                }

                var field = ReflectionCache.GetFieldInTypeHierarchy(current.GetType(), segment);
                current = field?.GetValue(current);
            }

            return current;
        }

        private static void BeginCentered(float width, float height)
        {
            GUILayout.BeginVertical(EditorGuiStyles.None, GUILayout.Width(width));
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(EditorGuiStyles.None, GUILayout.Height(height));
        }

        private static void EndCentered()
        {
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        // These used to bracket the cell with FlexibleSpace to centre it vertically in the row. In IMGUI a
        // group's exported stretchHeight can only be cleared by giving it a non-zero maxHeight, which in
        // turn inflates topLevel.maxHeight and makes the root allocate the full window height - so the
        // stretch cannot be capped at the drawer boundary and has to be absent at the source. Without it,
        // a row whose cells differ in height top-aligns the shorter one instead of centring it.
        private static void BeginColumn(float width)
        {
            GUILayout.BeginVertical(EditorGuiStyles.None, GUILayout.Width(width));
        }

        private static void BeginColumnExpanding()
        {
            GUILayout.BeginVertical(EditorGuiStyles.None, GUILayout.ExpandWidth(true));
        }

        private static void EndColumn()
        {
            GUILayout.EndVertical();
        }

        public Rect ContextClickArea { get; private set; }

        public Rect PropertyArea { get; private set; }

        GenericMenu IContextMenuProvider.Provide(out Rect highlightArea)
        {
            var menu = new GenericMenu();
            PropertyClipboard.AddContextMenu(menu, Property);
            UnityPropertyMenu.AddUnityItems(menu, Property);

            if (ElementCount == 0)
                menu.AddDisabledItem(new GUIContent("Clear Dictionary"), false);
            else
                menu.AddItem(new GUIContent("Clear Dictionary"), false, ClearDictionary);

            highlightArea = PropertyArea;
            return menu;
        }
    }
}