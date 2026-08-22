using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal sealed class CollectionPageState
    {
        private const float BtnW = 16f;
        private const float PageFieldW = 30f;
        private const float TotalLabelW = 30f;
        private const float ControlsW = 142f;
        private const float MinLabelW = 80f;

        private static readonly GUIContent ShowAllLabel = new("▼", "Show every entry at once");
        private static readonly GUIContent ShowPagesLabel = new("▲", "Back to pages");

        private int _page;
        private int _pendingPage = -1;
        private int _pageCount = 1;
        private bool _showAll;
        private bool _showAllRequested;
        private float _headerWidth;
        private float _measuredHeaderWidth;

        internal bool Paginates { get; private set; }

        internal bool Active => Paginates && !_showAll;

        internal void MeasureHeader(float width) => _measuredHeaderWidth = width;

        internal bool ShowsCountLabel(float countLabelWidth) =>
            _headerWidth <= 0f || _headerWidth >= countLabelWidth + ControlsW + MinLabelW;

        internal void Recompute(int itemCount, int pageSize, List<int> destination)
        {
            Recompute(itemCount, pageSize);
            destination.Clear();

            var start = Active ? _page * pageSize : 0;
            var end = Active ? Mathf.Min(start + pageSize, itemCount) : itemCount;
            for (var i = start; i < end; i++)
                destination.Add(i);
        }

        internal void Recompute(List<int> source, int pageSize, List<int> destination)
        {
            Recompute(source.Count, pageSize);
            destination.Clear();

            var start = Active ? _page * pageSize : 0;
            var end = Active ? Mathf.Min(start + pageSize, source.Count) : source.Count;
            for (var i = start; i < end; i++)
                destination.Add(source[i]);
        }

        private void Recompute(int itemCount, int pageSize)
        {
            pageSize = Mathf.Max(1, pageSize);
            Paginates = itemCount > pageSize;
            _pageCount = Mathf.Max(1, Mathf.CeilToInt(itemCount / (float) pageSize));
            _headerWidth = _measuredHeaderWidth;

            if (_showAllRequested)
            {
                _showAll = !_showAll;
                _showAllRequested = false;
            }

            if (_pendingPage >= 0)
            {
                _page = _pendingPage;
                _pendingPage = -1;
            }

            _page = Mathf.Clamp(_page, 0, _pageCount - 1);
        }

        private void RequestPage(int page)
        {
            _pendingPage = page;
            GUI.changed = true;
        }

        internal void DrawHeaderControls()
        {
            using (new EditorGUI.DisabledScope(_showAll))
            {
                using (new EditorGUI.DisabledScope(_page == 0))
                {
                    if (EditorGui.HoverableButton(ReserveCentered(BtnW), GUIContent.none, EditorGuiStyles.PagePrevButton))
                        RequestPage(_page - 1);
                }

                GUILayout.Space(2);

                BeginCentered(PageFieldW, EditorGUIUtility.singleLineHeight);
                var fieldRect = GUILayoutUtility.GetRect(PageFieldW, EditorGUIUtility.singleLineHeight, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
                EndCentered();

                var newPage = EditorGUI.DelayedIntField(fieldRect, _page + 1);
                if (newPage != _page + 1)
                    RequestPage(Mathf.Clamp(newPage, 1, _pageCount) - 1);

                var totalRect = GUILayoutUtility.GetRect(TotalLabelW, 0f, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(false));
                EditorGUI.LabelField(totalRect, new GUIContent($"/ {_pageCount}"), EditorGuiStyles.LabelLeft);

                GUILayout.Space(2);

                using (new EditorGUI.DisabledScope(_page >= _pageCount - 1))
                {
                    if (EditorGui.HoverableButton(ReserveCentered(BtnW), GUIContent.none, EditorGuiStyles.PageNextButton))
                        RequestPage(_page + 1);
                }
            }

            GUILayout.Space(3);

            if (EditorGui.HoverableButton(ReserveCentered(BtnW), _showAll ? ShowPagesLabel : ShowAllLabel, EditorGuiStyles.PageShowAllButton))
            {
                _showAllRequested = true;
                GUI.changed = true;
            }

            GUILayout.Space(3);
        }

        private static Rect ReserveCentered(float size)
        {
            BeginCentered(size, size);
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
            EndCentered();
            return rect;
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
    }
}
