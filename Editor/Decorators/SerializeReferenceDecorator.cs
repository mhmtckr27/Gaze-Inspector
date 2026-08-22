using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class SerializeReferenceDecorator : AttributeDrawer<SerializeReference>, IContextMenuProvider
    {
        private GenericMenu _menu;
        private List<SerializeReferencePopupItem> _items;
        private Rect _lastDrawnRect;

        protected internal override void Initialize()
        {
            var concreteTypes = FetchConcreteTypes();
            BuildItems(concreteTypes);
        }
        
        private float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight;
        }
        
        public override void Draw(GUIContent label)
        {
            PropertyTreeGUI.BeginMeasuring(GUIStyle.none);
            Rect measured;
            
            // Property.serializedObject.targetObjects
            if (Property.hasMultipleDifferentValues)
            {
                if (!AreAllTypesSame(GetTargetTypes(Property)))
                {
                    ShowHeader(label, true, new GUIContent($"— ({Data.FieldType?.Name})"));

                    measured = PropertyTreeGUI.EndMeasuring();

                    if (Event.current.type == EventType.Repaint)
                        _lastDrawnRect = measured;
                    return;
                }
            }
            
            if (string.IsNullOrEmpty(Property.managedReferenceFullTypename))
            {
                ShowHeader(label);

                measured = PropertyTreeGUI.EndMeasuring();

                if (Event.current.type == EventType.Repaint)
                    _lastDrawnRect = measured;
                return;
            }
            
            if (!Property.IsListItem())
            {
                var rect = ShowHeader(GuiUtility.DummyLabel);
                if (PropertyTreeGUI.BeginFoldout(rect, Property, label))
                {
                    EditorGuiUtility.PushIndentLevel(EditorGUI.indentLevel + 1);
                    Inner.Draw(label);
                    EditorGuiUtility.PopIndentLevel();
                }

                PropertyTreeGUI.EndFoldout();
            }
            else
            {                
                GUILayout.BeginVertical();
                ShowHeader(null);
                Inner.Draw(label);
                GUILayout.EndVertical();
            }

            measured = PropertyTreeGUI.EndMeasuring();

            if (Event.current.type == EventType.Repaint)
                _lastDrawnRect = measured;
        }

        public Rect ContextClickArea { get; private set; }
        GenericMenu IContextMenuProvider.Provide(out Rect highlightArea)
        {
            var menu = new GenericMenu();
            PropertyClipboard.AddContextMenu(menu, Property);
            highlightArea = _lastDrawnRect;
            return menu;
        }
        
        private void CreateAndAssign(Type concreteType)
        {
            var targets = Property.serializedObject.targetObjects;
            
            // 1) Start a named undo group
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Change SerializeReference Type");

            // 2) Snapshot full state (SR changes are safest with CompleteObjectUndo)
            Undo.RegisterCompleteObjectUndo(targets, "Change SerializeReference Type");

            try
            {
                foreach (var target in targets)
                {
                    using var so = new SerializedObject(target);
                    var targetProperty = so.FindProperty(Property.propertyPath);
                    
                    var instance = (concreteType == null) ? null : Activator.CreateInstance(concreteType);
                    
                    targetProperty.managedReferenceValue = instance;

                    so.ApplyModifiedProperties();
                }
                
                Property.serializedObject.Update();
                Tree.RebuildNode(this.Root());
                Property.LayoutChanged();
                GUI.changed = true;
            }
            catch (Exception e)
            {
                Debug.LogException(new Exception($"[Gaze] {nameof(SerializeReferenceDecorator)} | " +
                                                 $"Could not change type! Details: {e}"));
            }
            finally
            {
                // 6) Collapse the group so the history shows a single, correct entry
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                InternalEditorUtility.RepaintAllViews();
            }
        }
        
        private static Type[] GetTargetTypes(SerializedProperty property)
        {
            var parents = property.serializedObject.targetObjects;
            
            var list = new List<Type>(parents.Length);
            foreach (var parent in parents)
            {
                using var so = new SerializedObject(parent);
                var p = so.FindProperty(property.propertyPath);
                list.Add(ReflectionCache.GetRuntimeType(p));
            }
            
            return list.ToArray();
        }

        private static bool AreAllTypesSame(Type[] types)
        {
            return types.Distinct().Count() == 1;
        }

        private Rect ShowHeader(GUIContent label, bool mixed = false, GUIContent selected = null)
        {
            PropertyTreeGUI.BeginHorizontalLayout(label, out var labelRect);
            var content = selected ?? _items[CalculateSelectedIndex()].Content;
            
            var dropdownRect = EditorGUILayout.GetControlRect(label != null, GetHeight(), EditorStyles.layerMaskField);
            if (EditorGUI.DropdownButton(dropdownRect, content, FocusType.Keyboard))
            {
                BuildTypeMenu(mixed);
                _menu.ShowAsContext();
            }
            PropertyTreeGUI.EndHorizontalLayout();
            ContextClickArea = labelRect;
            return labelRect;
        }

        private List<Type> FetchConcreteTypes()
        {
            var toReturn = Data.FieldType.GetConcreteDerivedTypes();
            toReturn.Insert(0, null);
            return toReturn;
        }
        
        private void BuildItems(List<Type> concreteTypes)
        {
            concreteTypes ??= FetchConcreteTypes();
            _items = new List<SerializeReferencePopupItem>(concreteTypes.Count);
            
            foreach (var concreteType in concreteTypes)
            {
                var popupDisplayName = concreteType?.Name ?? "None";
                var displayName = $"{popupDisplayName} ({Data.FieldType?.Name})";
                _items.Add(new SerializeReferencePopupItem(new GUIContent(displayName), new GUIContent(popupDisplayName), concreteType, CreateAndAssign));
            }
        }

        private int CalculateSelectedIndex()
        {
            return _items.IndexOf(_items.FirstOrDefault(i => Property.managedReferenceValue?.GetType() == i.ConcreteType));
        }
        
        private void BuildTypeMenu(bool mixed)
        {
            _menu = new GenericMenu();

            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                
                var isOn = Property.managedReferenceValue?.GetType() == item.ConcreteType;
                
                _menu.AddItem(_items[i].PopupContent, !mixed && isOn, _items[i].OnSelected);
                
                if(i == 0)
                    _menu.AddSeparator("");
            }
        }
        
        private class SerializeReferencePopupItem
        {
            internal readonly GUIContent Content;
            internal readonly GUIContent PopupContent;
            internal readonly Type ConcreteType;
            private readonly Action<Type> _callback;

            public SerializeReferencePopupItem(
                GUIContent content,
                GUIContent popupContent,
                Type concreteType, 
                Action<Type> callback)
            {
                Content = content;
                PopupContent = popupContent;
                ConcreteType = concreteType;
                _callback = callback;
            }

            public void OnSelected()
            {
                _callback?.Invoke(ConcreteType);
            }
        }
    }
}