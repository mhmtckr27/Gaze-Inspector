using System;
using System.Collections.Generic;
using GrayWolf.Gaze.Collections;
using UnityEngine;

namespace GrayWolf.Gaze.Demo
{
    public sealed class GazeDemoDictionary : MonoBehaviour
    {
        [Serializable]
        public struct GazeDemoAbilityStats
        {
            [Range(1, 10)] public float cooldown;
            [Min(0)] public int manaCost;
            public string description;
        }

        [Header("Primitive values")]
        [SerializeField] private SerializedDictionary<string, float> stringToFloat = new();
        [SerializeField] private SerializedDictionary<string, int> stringToInt = new();
        [SerializeField] private SerializedDictionary<int, string> intToString = new();

        [Header("Object and enum values")]
        [SerializeField] private SerializedDictionary<string, GameObject> prefabsByName = new();
        [SerializeField] private SerializedDictionary<GazeDemoState, Color> colorByState = new();

        [Header("Generic values - each row's value is a foldout")]
        [SerializeField] private SerializedDictionary<string, GazeDemoAbilityStats> abilityStats = new();

        [Header("Custom column labels")]
        [SerializedDictionary("Ability", "Cooldown")]
        [SerializeField] private SerializedDictionary<string, float> customLabels = new();

        [Header("Custom labels + a value title pulled from a field")]
        [SerializedDictionary("Ability", "Stats", nameof(GazeDemoAbilityStats.description))]
        [SerializeField] private SerializedDictionary<string, GazeDemoAbilityStats> titledValues = new();

        [Header("Duplicate keys - the offending rows are tinted")]
        [SerializeField] private SerializedDictionary<string, int> duplicateKeys = new();

        [Header("Pagination")]
        [InfoBox("137 entries. Page through them with the pager in the header.", InfoMessageType.Info)]
        [SerializeField] private SerializedDictionary<string, int> paginated = new();

        [Header("Several dictionaries on one object")]
        [InfoBox("Three more dictionaries so multiple drawers share one editor - the case that used " +
                 "to corrupt state across inspector windows.", InfoMessageType.Warning)]
        [SerializeField] private SerializedDictionary<float, string> parallelA = new();
        [SerializeField] private SerializedDictionary<float, string> parallelB = new();
        [SerializeField] private SerializedDictionary<float, string> parallelC = new();

        [Button("Populate Dictionary Demo")]
        public void PopulateDemo()
        {
            stringToFloat = new SerializedDictionary<string, float>(new Dictionary<string, float>
            {
                { "gravity", -9.81f }, { "drag", 0.02f }, { "friction", 0.4f }
            });

            stringToInt = new SerializedDictionary<string, int>(new Dictionary<string, int>
            {
                { "alpha", 1 }, { "bravo", 2 }, { "charlie", 3 }, { "delta", 4 }, { "echo", 5 },
                { "foxtrot", 6 }, { "golf", 7 }, { "hotel", 8 }, { "india", 9 }, { "juliet", 10 }
            });

            intToString = new SerializedDictionary<int, string>(new Dictionary<int, string>
            {
                { 100, "hundred" }, { 200, "two hundred" }, { 300, "three hundred" }
            });

            colorByState = new SerializedDictionary<GazeDemoState, Color>(new Dictionary<GazeDemoState, Color>
            {
                { GazeDemoState.Idle, Color.grey },
                { GazeDemoState.Running, Color.green },
                { GazeDemoState.Paused, Color.yellow },
                { GazeDemoState.Finished, Color.red }
            });

            abilityStats = new SerializedDictionary<string, GazeDemoAbilityStats>(new Dictionary<string, GazeDemoAbilityStats>
            {
                { "Fireball", new GazeDemoAbilityStats { cooldown = 4f, manaCost = 30, description = "Ranged burst" } },
                { "Heal", new GazeDemoAbilityStats { cooldown = 8f, manaCost = 45, description = "Restores health" } }
            });

            titledValues = new SerializedDictionary<string, GazeDemoAbilityStats>(new Dictionary<string, GazeDemoAbilityStats>
            {
                { "Dash", new GazeDemoAbilityStats { cooldown = 2f, manaCost = 10, description = "Short burst of speed" } },
                { "Shield", new GazeDemoAbilityStats { cooldown = 6f, manaCost = 25, description = "Absorbs one hit" } }
            });

            customLabels = new SerializedDictionary<string, float>(new Dictionary<string, float>
            {
                { "Fireball", 4f }, { "Heal", 8f }
            });

            paginated = new SerializedDictionary<string, int>();
            for (var i = 0; i < 137; i++)
                paginated.Add($"key_{i:D3}", i);

            SeedDuplicateKeys();
        }

        private void SeedDuplicateKeys()
        {
            duplicateKeys = new SerializedDictionary<string, int>();
            duplicateKeys.Add("unique", 3);

#if UNITY_EDITOR
            var serializedObject = new UnityEditor.SerializedObject(this);
            var keys = serializedObject.FindProperty($"{nameof(duplicateKeys)}.keyData");
            var values = serializedObject.FindProperty($"{nameof(duplicateKeys)}.valueData");

            if (keys == null || values == null)
                return;

            AppendEntry(keys, values, "shared", 1);
            AppendEntry(keys, values, "shared", 2);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
#endif
        }

#if UNITY_EDITOR
        private static void AppendEntry(UnityEditor.SerializedProperty keys, UnityEditor.SerializedProperty values, string key, int value)
        {
            keys.InsertArrayElementAtIndex(keys.arraySize);
            keys.GetArrayElementAtIndex(keys.arraySize - 1).stringValue = key;

            values.InsertArrayElementAtIndex(values.arraySize);
            values.GetArrayElementAtIndex(values.arraySize - 1).intValue = value;
        }
#endif

        private void Reset()
        {
            PopulateDemo();
        }
    }
}
