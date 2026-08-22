using System;
using UnityEngine;

namespace GrayWolf.Gaze.Demo
{
    /// <summary>Lets the demo show the picker resolving an interface, not only a base class.</summary>
    public interface IGazeDemoEffect
    {
    }

    [Serializable]
    public abstract class GazeDemoEffect : IGazeDemoEffect
    {
        public string note = "base";
    }

    [Serializable]
    public class GazeDemoHealEffect : GazeDemoEffect
    {
        [Min(0)] public int hitPoints = 25;

        [Button("Log This Effect")]
        private void LogEffect()
        {
            Debug.Log($"[Gaze Demo] {nameof(GazeDemoHealEffect)} heals {hitPoints} hp");
        }
    }

    [Serializable]
    public class GazeDemoDamageEffect : GazeDemoEffect
    {
        [Min(0)] public int damage = 10;
        public bool critical = true;

        [Button("Log This Effect")]
        private void LogEffect()
        {
            Debug.Log($"[Gaze Demo] {nameof(GazeDemoDamageEffect)} deals {damage} damage, critical: {critical}");
        }
    }

    [Serializable]
    public class GazeDemoBuffEffect : GazeDemoEffect
    {
        public GazeDemoStatModifier modifier = new();
        [Min(0f)] public float duration = 5f;
    }

    [Serializable]
    public class GazeDemoStatModifier
    {
        public GazeDemoStatKind stat = GazeDemoStatKind.Strength;
        [Range(-5f, 5f)] public float delta = 1f;
        public bool stacks;
    }

    public enum GazeDemoStatKind { Strength, Agility, Intellect, Luck }

    [Serializable]
    public class GazeDemoRenamableEffect : GazeDemoEffect
    {
        public int amount = 25;
    }
}
