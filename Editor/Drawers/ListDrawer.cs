using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal sealed class ListDrawer : PropertyDrawerBase, IContextMenuProvider
    {
        const float HeaderH = 22f;
        const float BtnW = 16f;
        const float BtnAddW = 20f;
        const float GripW = 14;
        const float CountLabelW = 80f;
        private Rect rect;
        private Vector2 mouseDragPos;
        
        List<Rect> _rowRects = new();   // last Repaint's rects
        List<Rect> _gripRects = new();

        private readonly List<int> _toRemoveIndices = new();
        private readonly List<int> _pageIndices = new();
        private readonly Dictionary<string, DrawerBase> _elementDrawers = new();
        private readonly CollectionPageState _pageState = new();
        private readonly ListItemDragState _dragState = new();
        private bool _endDragPending;

        // Matches SerializedDictionaryDrawer: structural edits are resolved against the first target's
        // SerializedObject, so applying them across a mixed selection corrupts the other targets.
        private bool IsMultiEditing => Property?.serializedObject?.isEditingMultipleObjects ?? false;

        public override void Draw(GUIContent label)
        {
            var pageSize = GazeInspectorSettings.instance.PageSize;

            if (Event.current.type == EventType.Layout)
            {
                _pageState.Recompute(Property.arraySize, pageSize, _pageIndices);
                SyncPageRects();
            }

            var toolbar = EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.Height(HeaderH + 2));
            var header = GUILayoutUtility.GetRect(0, HeaderH + 2, GUILayout.ExpandWidth(true));
            
            using (new EditorGui.ColorScope(EditorGuiStyles.EvenRowColor))
            {
                EditorGUI.DrawRect(toolbar, Color.white);
            }

            if (Event.current.type == EventType.Repaint)
                _pageState.MeasureHeader(toolbar.width);

            var isExpanded = DrawHeader(header);
            EditorGUILayout.EndHorizontal();
            PropertyTreeGUI.BeginMeasuring(EditorGuiStyles.CollectionDrawerBox);
            
            if (PropertyTreeGUI.BeginFoldout(Property, isExpanded))
            {
                for (var p = 0; p < _pageIndices.Count; p++)
                {
                    var arrayIndex = _pageIndices[p];
                    var element = Property.GetArrayElementAtIndex(arrayIndex);
                    var child = ResolveChild(element);

                    var r = _rowRects[p];
                    var g = _gripRects[p];

                    HandleDrag(rect, r, g, _dragState, Property, arrayIndex, toolbar.yMax);

                    if (_dragState.active && _dragState.from == arrayIndex)
                    {
                        OverlayDrawer.Begin(new Vector2(r.width, r.height));
                        DrawChild(child, arrayIndex, p);
                        OverlayDrawer.End();

                        OverlayDrawer.Push(() =>
                        {
                            OverlayDrawer.Draw(new Vector2(mouseDragPos.x - _dragState.grabOffset.x,
                                mouseDragPos.y - _dragState.grabOffset.y));
                        });
                    }
                    else
                    {
                        var (listItem, grip) = DrawChild(child, arrayIndex, p);

                        if (Event.current.type == EventType.Repaint)
                        {
                            _rowRects[p] = listItem;
                            _gripRects[p] = grip;
                        }
                    }

                }

                PropertyTreeGUI.EndFoldout();
                rect = PropertyTreeGUI.EndMeasuring();

                var drawHeaderShadow = Property.arraySize > 0;
                EditorGui.DrawBorder(toolbar, EditorGuiStyles.BorderColor, Vector4.one, drawShadow: drawHeaderShadow,
                    shadowColor:
                    _dragState.active && _dragState.over == 0
                        ? EditorGuiStyles.BorderColor
                        : EditorGuiStyles.EvenRowColor * EditorGuiStyles.ShadowColor);
                
                if(Property.arraySize > 0)
                    EditorGui.DrawBorder(rect, EditorGuiStyles.BorderColor, new Vector4(1, 0, 1, 1));

                GUILayoutUtility.GetRect(0, 2);
            }
            else
            {
                EditorGui.DrawBorder(toolbar, EditorGuiStyles.BorderColor, Vector4.one);
                PropertyTreeGUI.EndFoldout();
                rect = PropertyTreeGUI.EndMeasuring();
                GUILayoutUtility.GetRect(0, 1);
            }

            // Every row has had its say, so the layout groups this pass opened are all closed and the
            // drag can be torn down without leaving one behind.
            if (_endDragPending)
            {
                EndDrag(Property, _dragState);
                _endDragPending = false;
            }

            if(Event.current.type != EventType.Repaint)
                return;
            
            ContextClickArea = toolbar;
            PropertyArea = toolbar;
            var area = PropertyArea;
            area.yMax = rect.yMax;
            PropertyArea = area;
            
            if(_toRemoveIndices.Count == 0)
                return;

            _toRemoveIndices.Sort();

            for (var i =  _toRemoveIndices.Count - 1; i >= 0; i--)
            {
                DeleteChild(Property, _toRemoveIndices[i]);
            }

            for (var i = _toRemoveIndices.Count - 1; i >= 0; i--)
            {
                ElementIdDatabase.OnDelete(Property, _toRemoveIndices[i]);
            }

            _toRemoveIndices.Clear();
            _elementDrawers.Clear();

            GUI.changed = true;
            Property.LayoutChanged();
            Tree.RebuildNode(this);
        }

        private DrawerBase ResolveChild(SerializedProperty element)
        {
            var path = element.propertyPath;

            if (!_elementDrawers.TryGetValue(path, out var drawer))
            {
                drawer = Tree.BuildNode(element);
                drawer.Parent = this;
                _elementDrawers[path] = drawer;
            }

            return drawer;
        }

        private void SyncPageRects()
        {
            while (_rowRects.Count < _pageIndices.Count)
            {
                _rowRects.Add(default);
                _gripRects.Add(default);
            }

            if (_rowRects.Count > _pageIndices.Count)
            {
                _rowRects.RemoveRange(_pageIndices.Count, _rowRects.Count - _pageIndices.Count);
                _gripRects.RemoveRange(_pageIndices.Count, _gripRects.Count - _pageIndices.Count);
            }
        }

        /// <summary>
        /// The area a drag counts as being inside. Per-row hit tests leave gaps: the row the drag came
        /// from is drawn as an overlay rather than in the flow, and nothing covers the padding between
        /// rows, so a pointer that is plainly over the list still misses every cached rect.
        /// </summary>
        private Rect DragArea(Rect measuredBody)
        {
            var area = measuredBody;

            for (var k = 0; k < _rowRects.Count; k++)
            {
                var row = _rowRects[k];

                if (row.width <= 0f || row.height <= 0f)
                    continue;

                area = area.width <= 0f || area.height <= 0f ? row : Rect.MinMaxRect(
                    Mathf.Min(area.xMin, row.xMin),
                    Mathf.Min(area.yMin, row.yMin),
                    Mathf.Max(area.xMax, row.xMax),
                    Mathf.Max(area.yMax, row.yMax));
            }

            return area;
        }

        private (Rect, Rect) DrawChild(DrawerBase child, int index, int row)
        {
            var listItem = PropertyTreeGUI.BeginListItem(row);
                        
            EditorGuiUtility.PushLabelWidth(EditorGUIUtility.labelWidth - EditorGuiStyles.ListItemEvenIndex.padding.left - 2);
            PropertyTreeGUI.DrawNode(child, GUIContent.none);
            EditorGuiUtility.PopLabelWidth();

            var grip = new Rect(listItem.x + 5, listItem.y, GripW, listItem.height);
            DrawGrip(grip);
                        
            var deleteRect = new Rect(listItem.xMax - BtnW - 3, listItem.y, BtnW, listItem.height);
            DrawDeleteButton(deleteRect, index);
                        
            PropertyTreeGUI.EndListItem();
            return (listItem, grip);
        }
        
        private static Rect ReserveDeleteButtonRect()
        {
            return EditorGUILayout.GetControlRect(false, GUILayout.Height(BtnW), GUILayout.Width(BtnW), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
        }
        
        private void DrawDeleteButton(Rect rect, int index)
        {
            rect.y += (rect.height - BtnW) / 2;
            rect.height = BtnW;

            using (new EditorGUI.DisabledScope(IsMultiEditing))
            {
                if (EditorGui.HoverableButton(rect, GUIContent.none, EditorGuiStyles.RemoveButton))
                {
                    _toRemoveIndices.Add(index);
                }
            }
        }

        private void ClearCollection()
        {
            for (var i = 0; i < Property.arraySize; i++)
            {
                _toRemoveIndices.Add(i);
            }
        }
        
        public static void RemoveFocusControl()
        {
            GUIUtility.hotControl = 0;
            DragAndDrop.activeControlID = 0;
            GUIUtility.keyboardControl = 0;
        }

        private void HandleDrag(Rect totalRect, Rect row, Rect grip, ListItemDragState dragState, SerializedProperty listProperty, int index, float listBodyTop)
        {
            var e = Event.current;

            // DisabledScope only gates GUI controls, not raw event handling, so reordering needs its own
            // guard - MoveArrayElement would otherwise reorder every selected target against one layout.
            if (IsMultiEditing)
                return;

            if (dragState.active && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                CommitDrag(listProperty, dragState.startIndex, dragState);
                _endDragPending = true;
                e.Use();
            }

            if (e.type == EventType.MouseDown && grip.Contains(e.mousePosition) && e.button == 0)
            {
                dragState.startIndex = index;
                dragState.from = index;
                dragState.over = index;
                dragState.grabOffset = new Vector2(e.mousePosition.x - row.x, e.mousePosition.y - row.y);
                mouseDragPos = e.mousePosition;
                dragState.active = true;
                GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                e.Use();
            }

            if (dragState.active)
            {
                mouseDragPos = Event.current.mousePosition;
            }

            // MOUSE DRAG (update target)
            if (dragState.active && e.type == EventType.MouseDrag)
            {
                var fromPageLocal = _pageIndices.IndexOf(dragState.from);
                int target = -1;

                for (int k = 0; k < _rowRects.Count; k++)
                {
                    var childRect = _rowRects[k];

                    if (childRect.Contains(e.mousePosition))
                    {
                        if (fromPageLocal > k)
                        {
                            if (e.mousePosition.y < (childRect.yMax - childRect.yMin) / 2 + childRect.y)
                            {
                                target = _pageIndices[k];
                                break;
                            }
                        }
                        else if (fromPageLocal < k)
                        {
                            if (e.mousePosition.y > (childRect.yMax - childRect.yMin) / 2 + childRect.y)
                            {
                                target = _pageIndices[k];
                                break;
                            }
                        }
                    }
                }

                if (target == -1)
                {
                    target = dragState.from;
                }

                dragState.over = Mathf.Clamp(target, 0, listProperty.arraySize);

                CommitDrag(listProperty, target, dragState);
                e.Use();
            }

            
            // rawType too: a release outside the inspector arrives as Ignore, and missing it left the
            // drag stuck to the cursor with the mouse still captured.
            if (dragState.active && (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp))
            {
                // The reorder is already in the array - MouseDrag commits it live - so a release keeps
                // it wherever it landed. Escape is what puts an element back. Ending the drag waits
                // until the pass is over: clearing it here drops the gap row's GetRect mid-loop and
                // leaves the layout groups mismatched against the Layout pass.
                _endDragPending = true;
                e.Use();
            }
            
            if(dragState.active && index == dragState.from)
                row = GUILayoutUtility.GetRect(row.width, row.height, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            
            if (Event.current.type == EventType.Repaint && (dragState?.active ?? false))
            {
                EditorGUIUtility.AddCursorRect(DragArea(totalRect), MouseCursor.MoveArrow);

                if (index == dragState.from)
                {
                    EditorGUI.DrawRect(new Rect(row.x, row.y, row.width, row.height), EditorGuiStyles.BorderColor);
                }
            }
        }

        /// <summary>Releases the mouse and writes the drag through, whatever ended it.</summary>
        private static void EndDrag(SerializedProperty listProperty, ListItemDragState dragState)
        {
            listProperty.serializedObject.ApplyModifiedProperties();
            listProperty.serializedObject.UpdateIfRequiredOrScript();

            dragState.Reset();
            GUIUtility.hotControl = 0;
        }

        private  void CommitDrag(SerializedProperty listProperty, int to, ListItemDragState dragState)
        {
            if (to == dragState.from)
                return;

            Undo.RecordObject(listProperty.serializedObject.targetObject, "Drag Item");
            var dirtyProps = new List<SerializedProperty>();
            if (dragState.from < to)
            {
                for (var i = dragState.from; i <= to; i++)
                {
                    dirtyProps.Add(listProperty.GetArrayElementAtIndex(i));
                }
            }
            else
            {
                for (var i = to; i <= dragState.from; i++)
                {
                    dirtyProps.Add(listProperty.GetArrayElementAtIndex(i));
                }
            }

            var pageStart = _pageIndices.Count > 0 ? _pageIndices[0] : 0;
            var fromLocal = dragState.from - pageStart;
            var toLocal = to - pageStart;

            // Both ends have to be on the page the rects were captured for, or the bookkeeping below
            // throws and takes the move with it.
            if (fromLocal >= 0 && toLocal >= 0 && fromLocal < _rowRects.Count && toLocal < _rowRects.Count)
            {
                var a = _gripRects[fromLocal];
                _gripRects.RemoveAt(fromLocal);
                _gripRects.Insert(toLocal, a);

                a = _rowRects[fromLocal];
                _rowRects.RemoveAt(fromLocal);
                _rowRects.Insert(toLocal, a);
            }

            listProperty.MoveArrayElement(dragState.from, to);
            //
            foreach (var dirtyProp in dirtyProps)
            {
                ApplyExpandedRelative(dirtyProp);
            }

            var bef = dragState.from;
            var aft = to;
            Property.serializedObject.ApplyModifiedProperties();
            dragState.from = to;
            ElementIdDatabase.OnReorder(listProperty, bef, aft);
            Property.LayoutChanged();
            GUI.changed = true;

            // No ExitGUI here. It throws, so every statement after a CommitDrag call was unreachable:
            // the drag event was never used, Escape never reset the state, and the mouse stayed
            // captured. A move leaves arraySize alone, so the rest of the pass is safe to finish.
        }

        private static void ApplyExpandedRelative(SerializedProperty elementRoot)
        {
            foreach (var child in elementRoot.Children(false))
            {
                var st = PropertyStateStore.GetFoldoutAnimState(child);
                child.isExpanded = st.expanded;
            }
        }

        private static void DeleteChild(SerializedProperty arrayProperty, int childIndex)
        {
            var old = arrayProperty.arraySize;
            if(old <= childIndex)
                return;
            
            arrayProperty.DeleteArrayElementAtIndex(childIndex);
            
            if (arrayProperty.arraySize == old) 
                arrayProperty.DeleteArrayElementAtIndex(childIndex); // object refs need two calls
        }

        private bool DrawHeader(Rect rect)
        {
            EditorGuiUtility.PushHierarchyMode(false);

            // Read from the property, not from what Foldout returns: BeginFoldout writes the new state
            // after this runs, so the control count stays identical across the frame that toggles it.
            var showPagination = _pageState.Paginates && Property.isExpanded;

            var newExpanded = Property.isExpanded;
            if (Property.arraySize > 0)
            {
                newExpanded = EditorGUI.Foldout(rect, Property.isExpanded, Data.Label, true, EditorGuiStyles.Foldout);
            }
            else
            {
                GUILayout.Space(10);
                GUI.Label(rect, Data.Label, EditorGuiStyles.LabelLeft);
            }

            if (!showPagination || _pageState.ShowsCountLabel(CountLabelW))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    var itemsCountRect = GUILayoutUtility.GetRect(CountLabelW, 0f, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(false));
                    EditorGUI.LabelField(itemsCountRect, new GUIContent(GetEntryCountText()), EditorGuiStyles.LabelRight);
                }
            }

            GUILayout.Space(3);
            var separatorRect = GUILayoutUtility.GetRect(1, 0, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(separatorRect, new Color(0, 0, 0, 0.375f));

            if (showPagination)
                _pageState.DrawHeaderControls();

            BeginCentered(BtnAddW, BtnAddW);
            var plusRect = GUILayoutUtility.GetRect(BtnAddW, BtnAddW, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
            EndCentered();

            using var addDisabled = new EditorGUI.DisabledScope(IsMultiEditing);

            if (EditorGui.HoverableButton(plusRect, GUIContent.none, EditorGuiStyles.AddButton))
            {
                var idx = Property.arraySize;
                Property.InsertArrayElementAtIndex(idx);
                Property.serializedObject.ApplyModifiedProperties();
                _elementDrawers.Clear();
                // Matches the dictionary: adding to a collapsed collection used to hide the new entry.
                Property.isExpanded = true;
                GUI.changed = true;
                ElementIdDatabase.OnInsert(Property, idx);
            }

            EditorGuiUtility.PopHierarchyMode();

            return newExpanded;
        }

        private string GetEntryCountText()
        {
            if (Property.arraySize == 0)
                return "Empty";

            if (Property.arraySize == 1)
                return "1 item";
            
            return $"{Property.arraySize} items";
        }

        private static Rect ReserveGripRect()
        {
            return EditorGUILayout.GetControlRect(
                false, 
                GUILayout.Height(GripW),
                GUILayout.Width(GripW), 
                GUILayout.ExpandWidth(false),
                GUILayout.ExpandHeight(false));
        }

        private static void DrawGrip(Rect rect)
        {
            rect.y += (rect.height - GripW) / 2;
            rect.height = GripW;
            GUI.DrawTexture(rect, EditorGuiStyles._listItemGripIcon);
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

        public Rect ContextClickArea { get; private set; }

        public Rect PropertyArea { get; private set; }

        GenericMenu IContextMenuProvider.Provide(out Rect highlightArea)
        {
            var menu = new GenericMenu();
            PropertyClipboard.AddContextMenu(menu, Property);

            if (Property.arraySize == 0)
                menu.AddDisabledItem(new GUIContent("Clear Collection"), false);
            else
                menu.AddItem(new GUIContent("Clear Collection"), false, ClearCollection);

            highlightArea = PropertyArea;
            return menu;
        }
    }

    internal class ListItemDragState
    {
        // public string path;         // list propertyPath
        public int from;            // grabbed index
        public int over;            // current target index (drop before)
        public Vector2 grabOffset;    // mouseY - row.y
        public bool active;
        public int startIndex;

        internal void Reset()
        {
            from = -1;
            over = -1;
            active = false;
            startIndex = -1;
            grabOffset = Vector2.zero;
        }
    }
}