using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal static class EditorGuiUtility
    {
        private static Stack<float> s_LabelWidthStack = new();
        private static Stack<float> s_FieldWidthStack = new();
        private static Stack<int> s_IndentLevelStack = new();
        private static Stack<bool> s_HierarchyModeStack = new();
        private static Stack<Color> s_ColorStack = new();
        private static Stack<bool> s_MixedValueStack = new();

        internal static void PushLabelWidth(float width)
        {
            s_LabelWidthStack.Push(EditorGUIUtility.labelWidth);
            EditorGUIUtility.labelWidth = width;
        }

        internal static void PopLabelWidth()
        {
            EditorGUIUtility.labelWidth = s_LabelWidthStack.Pop();
        }

        internal static void PushFieldWidth(float width)
        {
            s_FieldWidthStack.Push(EditorGUIUtility.fieldWidth);
            EditorGUIUtility.fieldWidth = width;
        }

        internal static void PopFieldWidth()
        {
            EditorGUIUtility.fieldWidth = s_FieldWidthStack.Pop();
        }

        public static void PushIndentLevel(int indentLevel)
        {
            s_IndentLevelStack.Push(EditorGUI.indentLevel);
            EditorGUI.indentLevel = indentLevel;
        }

        public static void PopIndentLevel()
        {
            EditorGUI.indentLevel = s_IndentLevelStack.Pop();
        }

        public static void PushColor(Color color)
        {
            s_ColorStack.Push(GUI.color);
            GUI.color = color;
        }

        public static void PopColor()
        {
            GUI.color = s_ColorStack.Pop();
        }

        public static void PushHierarchyMode(bool enabled)
        {
            s_HierarchyModeStack.Push(EditorGUIUtility.hierarchyMode);
            EditorGUIUtility.hierarchyMode = enabled;
        }

        public static void PopHierarchyMode()
        {
            EditorGUIUtility.hierarchyMode = s_HierarchyModeStack.Pop();
        }

        public static void PushMixedValue(bool showMixedValue)
        {
            s_MixedValueStack.Push(EditorGUI.showMixedValue);
            EditorGUI.showMixedValue = showMixedValue;
        }

        public static void PopMixedValue()
        {
            EditorGUI.showMixedValue = s_MixedValueStack.Pop();
        }
    }
}