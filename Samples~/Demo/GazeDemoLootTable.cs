using System;
using System.Collections.Generic;
using System.Linq;
using GrayWolf.Gaze.Collections;
using UnityEngine;

namespace GrayWolf.Gaze.Demo
{
    /// <summary>Drop tiers, ordered so the rarest reads last in the row title.</summary>
    public enum LootRarity { Common, Uncommon, Rare, Epic, Legendary }

    /// <summary>What one item id is worth when this table rolls.</summary>
    [Serializable]
    public struct LootEntry
    {
        public LootRarity rarity;

        [Tooltip("Relative weight. Chance is this over the sum of every weight in the table.")]
        [Min(0)] public int weight;

        [LabelText("Stack (min / max)")]
        [VectorRange(1f, 1f, 99f, 99f)] public Vector2Int stack;

        /// <summary>Read by the drawer's search box, which matches keys and this text.</summary>
        public override string ToString() => $"{rarity} x{stack.x}-{stack.y}";
    }

    /// <summary>
    /// A loot table as a project asset, which is the case a serialized dictionary is really for: the
    /// whole content of the file is one table, so the drawer gets the full inspector to itself.
    /// </summary>
    [CreateAssetMenu(menuName = "Gaze/Demo/Loot Table", fileName = "GoblinCampLoot", order = 901)]
    public sealed class GazeDemoLootTable : ScriptableObject
    {
        [SerializedDictionary("Item ID", "Drop", nameof(LootEntry.rarity))] 
        [SerializeField]
        private SerializedDictionary<string, LootEntry> drops;
        
        // [SerializedDictionary("Item ID", "Drop", nameof(LootEntry.rarity))]
        // [SerializeField]
        // private SerializedDictionary<string, LootEntry> drops = new()
        // {
        //     { "copper_coin",      new LootEntry { rarity = LootRarity.Common,    weight = 220, stack = new Vector2Int(5, 40) } },
        //     { "rusted_dagger",    new LootEntry { rarity = LootRarity.Common,    weight = 140, stack = new Vector2Int(1, 1) } },
        //     { "goblin_ear",       new LootEntry { rarity = LootRarity.Common,    weight = 120, stack = new Vector2Int(1, 3) } },
        //     { "torn_hide",        new LootEntry { rarity = LootRarity.Common,    weight = 110, stack = new Vector2Int(1, 4) } },
        //     { "crude_arrow",      new LootEntry { rarity = LootRarity.Common,    weight =  95, stack = new Vector2Int(6, 24) } },
        //     { "stale_ration",     new LootEntry { rarity = LootRarity.Common,    weight =  80, stack = new Vector2Int(1, 2) } },
        //     { "sinew_rope",       new LootEntry { rarity = LootRarity.Common,    weight =  70, stack = new Vector2Int(1, 2) } },
        //     { "oak_shortbow",     new LootEntry { rarity = LootRarity.Uncommon,  weight =  45, stack = new Vector2Int(1, 1) } },
        //     { "iron_sword",       new LootEntry { rarity = LootRarity.Uncommon,  weight =  38, stack = new Vector2Int(1, 1) } },
        //     { "healing_draught",  new LootEntry { rarity = LootRarity.Uncommon,  weight =  32, stack = new Vector2Int(1, 3) } },
        //     { "chieftain_totem",  new LootEntry { rarity = LootRarity.Rare,      weight =  14, stack = new Vector2Int(1, 1) } },
        //     { "banded_mail",      new LootEntry { rarity = LootRarity.Rare,      weight =  11, stack = new Vector2Int(1, 1) } },
        //     { "ember_ring",       new LootEntry { rarity = LootRarity.Rare,      weight =   7, stack = new Vector2Int(1, 1) } },
        //     { "warlord_helm",     new LootEntry { rarity = LootRarity.Epic,      weight =   3, stack = new Vector2Int(1, 1) } },
        //     { "soulbound_axe",    new LootEntry { rarity = LootRarity.Epic,      weight =   2, stack = new Vector2Int(1, 1) } },
        //     { "crown_of_the_pit", new LootEntry { rarity = LootRarity.Legendary, weight =   1, stack = new Vector2Int(1, 1) } },
        // };

        /// <summary>Rolls one item id, or null while every weight is zero.</summary>
        public string Roll()
        {
            var total = TotalWeight();

            if (total <= 0)
                return null;

            var pick = UnityEngine.Random.Range(0, total);

            foreach (var drop in drops)
            {
                pick -= drop.Value.weight;

                if (pick < 0)
                    return drop.Key;
            }

            return null;
        }

        [Button("Log Drop Chances")]
        private void LogDropChances()
        {
            var total = TotalWeight();

            if (total <= 0)
            {
                Debug.LogWarning($"[Gaze Demo] {name} has no weight to roll against.");
                return;
            }

            var lines = drops
                .OrderByDescending(drop => drop.Value.weight)
                .Select(drop => $"{drop.Key,-18} {drop.Value.weight * 100f / total,5:0.0}%");

            Debug.Log($"[Gaze Demo] {name} drop chances:\n{string.Join("\n", lines)}");
        }

        private int TotalWeight()
        {
            var total = 0;

            foreach (var drop in drops)
                total += drop.Value.weight;

            return total;
        }
    }
}
