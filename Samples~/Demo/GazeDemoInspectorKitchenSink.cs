using System;
using System.Collections.Generic;
using GrayWolf.Gaze.Collections;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_MATHEMATICS
using Unity.Mathematics;
#endif

namespace GrayWolf.Gaze.Demo
{
    public enum GazeDemoState { Idle, Running, Paused, Finished }

    [Flags]
    public enum GazeDemoStateWithFlags
    {
        Idle = 1 << 0,
        Walking = 1 << 1,
        Running = 1 << 2,
        Moving = Walking | Running,
        Paused = 1 << 3,
        Finished = 1 << 4,
    }

    [Serializable]
    public class GazeDemoStats
    {
        [Tooltip("Character level")]
        [Min(0)] public int level = 1;

        [Range(0f, 1f)] public float progress = 0.35f;
        public Vector3 spawnPoint = new(1, 2, 3);
        public LayerMask collidesWith;

        [Button("Log Stats")]
        private void LogStats()
        {
            Debug.Log($"[Gaze Demo] level {level}, progress {progress:0.00}");
        }
    }

    [Serializable]
    public class GazeDemoInventoryItem
    {
        public string itemName = "Item";
        [Min(0)] public int amount = 1;
        public Vector2 size = new(1, 1);

        [SerializeReference] public GazeDemoEffect effect;
    }

    [Serializable]
    public class GazeDemoItemGroup
    {
        public string groupName = "Group";
        public List<GazeDemoInventoryItem> items = new();
    }

    public sealed class GazeDemoInspectorKitchenSink : MonoBehaviour
    {
        // ========= BASICS =========
        [Header("Basics")]
        public bool boolField;

        [LabelText("Renamed By [LabelText]")] public bool relabelledBool;

        [Required] public string requiredString = "Clear me to see the validation error";

        [InfoBox("An informational note attached to the field below.", InfoMessageType.Info)]
        public int intField = 42;

        [InfoBox("A warning note.", InfoMessageType.Warning)]
        [Range(-10, 10)] public int intRanged = 5;

        [InfoBox("An error note.", InfoMessageType.Error)]
        [Min(0f)] public float floatNonNegative = 2f;

        [InfoBox("A plain note, drawn with no icon.", InfoMessageType.None)]
        [OnValueChanged(nameof(OnFloatFieldChanged))]
        public float floatField = 3.14159f;

        [Range(0f, 1f)] public float floatRanged = 0.5f;

        [Space]
        [Tooltip("Hover the label to read this tooltip")]
        public string singleLine = "Hello world";

        [Multiline] public string multiLine = "Line 1\nLine 2\nLine 3";
        [TextArea(2, 5)] public string textArea = "Resizable text area\nTry resizing me";
        [Delayed] public string delayedApply = "Applies on focus loss, not per keystroke";

        [Space]
        [HideInInspector] public int hiddenButSerialized = 1234;
        [FormerlySerializedAs("oldName")] public string renamedField = "Formerly oldName";

        // ========= CONDITIONALS =========
        [Header("Conditionals")]
        public bool conditionDriver = true;

        public GazeDemoStateWithFlags stateDriver = GazeDemoStateWithFlags.Idle;
        [ShowIf(nameof(stateDriver), GazeDemoStateWithFlags.Paused)] public string shownWhenDriverIsTrue = "[ShowIf]";
        [HideIf(nameof(conditionDriver))] public string hiddenWhenDriverIsTrue = "[HideIf]";
        [EnableIf(nameof(conditionDriver))] public string enabledWhenDriverIsTrue = "[EnableIf]";
        [DisableIf(nameof(conditionDriver))] public string disabledWhenDriverIsTrue = "[DisableIf]";
        [ReadOnly] public string neverEditable = "[ReadOnly]";

        // ========= VECTORS & VALUE TYPES =========
        [Header("Vectors & Value Types")]
        public Vector2 v2 = new(5, 6);
        public Vector3 v3 = new(1, 2, 3);
        public Vector4 v4 = new(1, 2, 3, 4);
        public Vector2Int v2Int = new(5, 6);
        public Vector3Int v3Int = new(5, 6, 7);

        [VectorRange(0f, 0f, 8f, 8f)] public Vector2 v2Clamped = Vector2.one;
        [VectorRange(0f, 0f, 0f, 8f, 8f, 8f)] public Vector3 v3Clamped = Vector3.one;
        [VectorRange(0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f, true)] public Vector4 v4Sliders = Vector4.one;
        [VectorRange(0f, 0f, 16f, 16f)] public Vector2Int v2IntClamped = Vector2Int.one;

        public Quaternion rotation = Quaternion.Euler(15, 30, 0);
        public Rect rect = new(10, 10, 128, 64);
        public RectInt rectInt = new(10, 10, 128, 64);
        public Bounds bounds = new(new Vector3(0, 1, 0), new Vector3(2, 2, 2));
        public BoundsInt boundsInt = new(new Vector3Int(0, 1, 0), new Vector3Int(2, 2, 2));
        public Hash128 hash = new(0, 0, 0, 0);
        public char character = 'G';

#if UNITY_MATHEMATICS
        public float3 mathFloat3 = new float3(0.1f, 0.2f, 0.3f);
        public float3 mathDirection = new float3(1, 2, 3);
#endif

        // ========= COLORS / CURVES / GRADIENTS =========
        [Header("Colors / Curves / Gradients")]
        public Color color = Color.green;

        [ColorUsage(true, true)] public Color hdrColor = new(3f, 2f, 1f, 1f);

        public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);

        [CurveRange(0f, 1f, 0f, 1f, 0.25f, 0.75f, 1f)]
        public AnimationCurve boundedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public Gradient gradient = NewDemoGradient();

        // ========= OBJECT REFERENCES =========
        [Header("Object References")]
        [Required("Assign a GameObject to clear this error")] public GameObject goRef;
        public Transform transformRef;
        public Material materialRef;
        public Texture2D textureRef;
        public AudioClip audioClipRef;
        [SerializeField] private GazeDemoAsset scriptableObjRef;
        public ExposedReference<GameObject> exposedRef;

        // ========= ENUMS & MASKS =========
        [Header("Enums & Masks")]
        public GazeDemoState state = GazeDemoState.Idle;
        public GazeDemoStateWithFlags stateWithFlags = GazeDemoStateWithFlags.Idle;
        public LayerMask layerMask;

        // ========= NESTED CLASS =========
        [Header("Nested Class")]
        public GazeDemoStats stats = new();

        // ========= COLLECTIONS =========
        [Header("Collections")]
        public int[] intArray = { 1, 2, 3, 5, 8 };

        [LabelText("Floats (relabelled list)")]
        public List<float> floatList = new() { 0.1f, 0.25f, 0.5f };

        public GazeDemoInventoryItem[] inventoryArray =
        {
            new() { itemName = "Sword", amount = 1, size = new Vector2(1, 2), effect = new GazeDemoDamageEffect { damage = 12 } },
            new() { itemName = "Potion", amount = 3, size = new Vector2(1, 1), effect = new GazeDemoHealEffect { hitPoints = 25 } },
        };

        public List<GazeDemoInventoryItem> inventory = new()
        {
            new GazeDemoInventoryItem { itemName = "Shield", amount = 1, size = new Vector2(2, 2) },
            new GazeDemoInventoryItem { itemName = "Bow", amount = 1, size = new Vector2(2, 1), effect = new GazeDemoDamageEffect { damage = 8, critical = false } },
        };

        [Header("Nested Lists (list of a type that holds a list)")]
        public List<GazeDemoItemGroup> groups = new();

        [Header("Pagination")]
        [InfoBox("Long enough to page. Use the pager in the list header.", InfoMessageType.Info)]
        public List<GazeDemoInventoryItem> paginatedInventory = new();

        // ========= DICTIONARY SMOKE =========
        [Header("Dictionary")]
        [SerializeField] private SerializedDictionary<string, int> scoresByPlayer = new();

        // ========= SERIALIZEREFERENCE =========
        [Header("SerializeReference Polymorphism")]
        [SerializeReference] public GazeDemoEffect effect = new GazeDemoDamageEffect { damage = 99, critical = true };

        // Missing type demo: assign it, save, rename GazeDemoRenamableEffect, then return to the inspector.
        [SerializeReference] public GazeDemoEffect renamableEffect = new GazeDemoRenamableEffect();

        [SerializeReference] public List<GazeDemoEffect> effects = new();

        // The picker resolves implementers of an interface just as it resolves subclasses of a base type.
        [SerializeReference] public List<IGazeDemoEffect> effectsByInterface = new();

        // ========= BUTTONS =========
        [Button]
        private void SpawnOne()
        {
            Debug.Log($"[Gaze Demo] {nameof(GazeDemoInspectorKitchenSink)}.{nameof(SpawnOne)} invoked");
        }

        [Button("Spawn All")]
        private void SpawnAll()
        {
            Debug.Log($"[Gaze Demo] {nameof(GazeDemoInspectorKitchenSink)}.{nameof(SpawnAll)} invoked");
        }

        private void OnFloatFieldChanged(float oldValue, float newValue)
        {
            Debug.Log($"[Gaze Demo] {nameof(floatField)} changed: {oldValue} -> {newValue}");
        }

        // ======== QUICK POPULATION ========
        [ContextMenu("Populate Demo Data")]
        [Button("Populate Demo Data")]
        public void PopulateDemo()
        {
            if (floatList.Count < 6)
            {
                for (var i = 0; i < 3; i++)
                    floatList.Add(UnityEngine.Random.value);
            }

            if (inventory.Count < 5)
            {
                inventory.Add(new GazeDemoInventoryItem { itemName = "Arrow", amount = 20, size = new Vector2(1, 1) });
                inventory.Add(new GazeDemoInventoryItem { itemName = "Torch", amount = 2, size = new Vector2(1, 2) });
            }

            if (groups.Count < 2)
            {
                groups.Clear();
                groups.Add(new GazeDemoItemGroup
                {
                    groupName = "Starter",
                    items = new List<GazeDemoInventoryItem>
                    {
                        new() { itemName = "Apple", amount = 2 },
                        new() { itemName = "Rope", amount = 1 },
                    }
                });
                groups.Add(new GazeDemoItemGroup
                {
                    groupName = "Loot",
                    items = new List<GazeDemoInventoryItem>
                    {
                        new() { itemName = "Gem", amount = 5, effect = new GazeDemoHealEffect { hitPoints = 5 } },
                    }
                });
            }

            if (paginatedInventory.Count < 137)
            {
                paginatedInventory.Clear();
                for (var i = 0; i < 137; i++)
                {
                    GazeDemoEffect itemEffect = (i % 3) switch
                    {
                        0 => new GazeDemoHealEffect { hitPoints = i },
                        1 => new GazeDemoDamageEffect { damage = i, critical = i % 2 == 0 },
                        _ => null
                    };
                    paginatedInventory.Add(new GazeDemoInventoryItem { itemName = $"Item {i}", amount = i + 1, effect = itemEffect });
                }
            }

            if (scoresByPlayer.Count == 0)
            {
                scoresByPlayer.Add("ada", 120);
                scoresByPlayer.Add("grace", 340);
                scoresByPlayer.Add("alan", 275);
            }

            if (effects.Count == 0)
            {
                effects.Add(new GazeDemoHealEffect { hitPoints = 10 });
                effects.Add(new GazeDemoDamageEffect { damage = 4, critical = false });
                effects.Add(new GazeDemoBuffEffect());
            }

            curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            gradient = NewDemoGradient();
            stats.level = UnityEngine.Random.Range(1, 10);
        }

        private static Gradient NewDemoGradient() => new()
        {
            colorKeys = new[]
            {
                new GradientColorKey(Color.magenta, 0f),
                new GradientColorKey(Color.cyan, 1f),
            },
            alphaKeys = new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        };

        private void Reset()
        {
            PopulateDemo();
        }
    }
}
