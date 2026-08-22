using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal class QuaternionDrawer : PropertyDrawerBase, IContextMenuProvider
    {
        private Rect _lastDrawnRect;
        
        public override void Draw(GUIContent label)
        {
            var hint = PropertyStateStore.GetOrCreate<QuaternionHint>(Property, out var isNew);

            var q = Property.quaternionValue;

            Vector3 disp;
            if (!isNew && Quaternion.Angle(q, Quaternion.Euler(hint.value)) < 0.01f)
            {
                // The quaternion still matches what our cached triple implies, so keep showing
                // that triple. Re-deriving from q.eulerAngles on every draw is what makes Y/Z
                // jump when only X is edited: Euler decomposition isn't unique, and Unity's
                // eulerAngles getter isn't guaranteed to stay continuous with numbers just typed in.
                disp = hint.value;
            }
            else
            {
                // First draw, or the rotation changed by something other than this drawer
                // (script, Reset, Undo, multi-object edit, ...) - there's no "right" continuous
                // triple to preserve, so fall back to Unity's own decomposition.
                var raw = q.eulerAngles; // 0..360 from Unity
                disp = new Vector3(
                    ClosestToHint(raw.x, hint.value.x),
                    ClosestToHint(raw.y, hint.value.y),
                    ClosestToHint(raw.z, hint.value.z));
                hint.value = disp;
            }

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var newDisp = PropertyTreeGUI.Vector3Field(label, disp);

                if (scope.changed)
                {
                    Property.quaternionValue = Quaternion.Euler(newDisp);
                    hint.value = newDisp;
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                ContextClickArea = GUILayoutUtility.GetLastRect();
                _lastDrawnRect = ContextClickArea;
            }
        }
        
        static float ClosestToHint(float raw0to360, float hint)
        {
            return GuiUtility.ClampToFloat(GuiUtility.RoundBasedOnMinimumDifference(Mathf.DeltaAngle(hint, raw0to360) + hint, 0.01));
        }

        class QuaternionHint
        {
            internal Vector3 value;
        }

        public Rect ContextClickArea { get; private set; }
        public GenericMenu Provide(out Rect highlightArea)
        {
            var menu = new GenericMenu();
            PropertyClipboard.AddContextMenu(menu, Property);
            highlightArea = _lastDrawnRect;
            return menu;
        }
    }
}