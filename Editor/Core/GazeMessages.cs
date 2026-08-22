using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal static class GazeMessages
    {
        private static readonly Regex s_ArrayIndex = new(@"\.Array\.data\[\d+\]", RegexOptions.Compiled);
        private static readonly List<GazeMessage> _buffer = new();
        private static readonly HashSet<(string scope, InfoMessageType type, string text)> _drawn = new();
        private static readonly Comparison<GazeMessage> s_BySeverityDescending = (a, b) => (int) b.Type - (int) a.Type;

        internal static string ScopeOf(string propertyPath)
        {
            return s_ArrayIndex.Replace(propertyPath, ".Array.data[]");
        }

        internal static void ClearDrawn()
        {
            _drawn.Clear();
        }

        internal static void Draw(DrawerBase node)
        {
            _buffer.Clear();
            node.CollectMessages(_buffer);

            if (_buffer.Count == 0)
                return;

            _buffer.Sort(s_BySeverityDescending);

            var scope = node.MessageScope;

            foreach (var message in _buffer)
            {
                if (!_drawn.Add((scope, message.Type, message.Text)))
                    continue;

                DrawBox(message);
            }
        }

        internal static Rect DrawBox(GazeMessage message)
        {
            var type = GetMessageType(message.Type);
            var icon = EditorGuiStyles.GetHelpIcon(type);
            var content = GuiUtility.TempContent(message.Text, icon);

            var leftGutter = EditorGui.CurrentIndentAmount;
            var contentWidth = Mathf.Max(0f, EditorGUIUtility.currentViewWidth - leftGutter);

            var style = EditorGuiStyles.MessageBox;
            style.wordWrap = true;

            var boxHeight = style.CalcHeight(content, contentWidth) + EditorGuiStyles.SectionSpacing;

            var rect = EditorGUILayout.GetControlRect(false, boxHeight);
            GUI.Label(rect, content, style);
            return rect;
        }

        private static MessageType GetMessageType(InfoMessageType type)
        {
            return type switch
            {
                InfoMessageType.None => MessageType.None,
                InfoMessageType.Info => MessageType.Info,
                InfoMessageType.Warning => MessageType.Warning,
                InfoMessageType.Error => MessageType.Error,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
