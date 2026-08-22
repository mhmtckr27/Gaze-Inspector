using System;
using System.Collections.Generic;
using System.Linq;
using GrayWolf.Gaze.Collections;
using UnityEngine;

namespace GrayWolf.Gaze.Demo
{
    /// <summary>Enemy archetypes the director can call in.</summary>
    public enum HordeUnit { Grunt, Runner, Shielder, Spitter, Brute, Warden }

    /// <summary>Where a group enters the arena from.</summary>
    public enum SpawnGate { NorthStair, EastGallery, SouthCrypt, WestDrain, CeilingVents }

    /// <summary>One group of enemies released inside a wave.</summary>
    [Serializable]
    public struct SpawnGroup
    {
        public HordeUnit unit;

        [Min(1)] public int count;

        public SpawnGate gate;

        [LabelText("Enters At (s)")]
        [Min(0f)] public float delaySeconds;

        public override string ToString() => $"{count}x {unit} from {gate}";
    }

    /// <summary>
    /// One wave of the run. This is the type that holds a list, so the wave table is the nested case:
    /// expand a wave, then expand its groups.
    /// </summary>
    [Serializable]
    public class HordeWave
    {
        [Required] public string waveName = "Wave";

        [LabelText("Duration (s)")]
        [Min(5f)] public float durationSeconds = 40f;

        [Tooltip("Scales every group's body count for this wave.")]
        [Range(0.5f, 3f)] public float pressure = 1f;

        public bool bossWave;

        [ShowIf(nameof(bossWave))]
        public HordeUnit boss = HordeUnit.Warden;

        [ShowIf(nameof(bossWave))]
        [LabelText("Boss Entrance")]
        public SpawnGate bossGate = SpawnGate.SouthCrypt;

        public List<SpawnGroup> groups = new();

        /// <summary>Bodies this wave sends, after pressure.</summary>
        public int Bodies => groups.Sum(group => Mathf.Max(1, Mathf.RoundToInt(group.count * pressure)));
    }

    /// <summary>
    /// The wave table for a survival map. Long enough that the inspector pages it, and every row holds
    /// its own list of spawn groups.
    /// </summary>
    [AddComponentMenu("Gaze/Demo/GazeDemoWaveTable")]
    public sealed class GazeDemoWaveTable : MonoBehaviour
    {
        // ===== RUN =====
        [Header("Run")]
        [Required] public string runId = "crypt_survival_a";

        [Range(1, 5)] public int difficulty = 3;

        [Tooltip("The same seed rebuilds the same table, so a run can be reproduced.")]
        public int seed = 1373;

        [LabelText("Waves To Build")]
        [Range(10, 200)]
        public int wavesToBuild = 45;

        [ReadOnly] 
        [LabelText("Run Length")] 
        public string runLength = "-";
        
        [ReadOnly]
        [LabelText("Bodies")]
        public int totalBodies;

        // ===== THE LIST =====
        [SerializeField] 
        private List<HordeWave> waves = new();

        /// <summary>Beat names, picked by whichever unit carries the wave.</summary>
        private static readonly Dictionary<HordeUnit, string[]> Names = new()
        {
            { HordeUnit.Grunt, new[] { "Opening Sweep", "Stair Press", "Rabble", "Bone March", "Torchlight Push" } },
            { HordeUnit.Runner, new[] { "Drain Rush", "Loose Hounds", "Flanking Dash", "Sprint Line", "Scattered Pack" } },
            { HordeUnit.Shielder, new[] { "Shield Line", "Locked Ranks", "Bulwark", "Slow Advance", "Iron Wall" } },
            { HordeUnit.Spitter, new[] { "Vent Bloom", "Acid Gallery", "Corrosion", "Spit Volley", "Green Rain" } },
            { HordeUnit.Brute, new[] { "Meat Grinder", "Battering Ram", "Crypt Breaker", "Heavy Hand", "Ground Shake" } },
            { HordeUnit.Warden, new[] { "Warden Arena", "The Warden Returns", "Warden Unchained", "Final Watch" } },
        };

        // ===== BUTTONS =====
        [Button("Rebuild Wave Table")]
        public void RebuildWaveTable()
        {
            var random = new System.Random(seed);
            var nameCursor = new Dictionary<HordeUnit, int>();

            waves.Clear();

            for (var index = 1; index <= wavesToBuild; index++)
                waves.Add(BuildWave(index, random, nameCursor));

            Recount();
        }

        [Button("Log Unit Mix")]
        private void LogUnitMix()
        {
            if (waves.Count == 0)
            {
                Debug.LogWarning($"[Gaze Demo] {runId} has no waves to count.");
                return;
            }

            var bodies = Mathf.Max(1, totalBodies);

            var lines = waves
                .SelectMany(wave => wave.groups.Select(group => new
                {
                    group.unit,
                    bodies = Mathf.Max(1, Mathf.RoundToInt(group.count * wave.pressure)),
                }))
                .GroupBy(entry => entry.unit)
                .Select(group => new { unit = group.Key, bodies = group.Sum(entry => entry.bodies) })
                .OrderByDescending(row => row.bodies)
                .Select(row => $"{row.unit,-10} {row.bodies,5} bodies {row.bodies * 100f / bodies,6:0.0}%");

            Debug.Log($"[Gaze Demo] {runId}, {waves.Count} waves over {runLength}:\n{string.Join("\n", lines)}");
        }

        [Button("Recount")]
        private void Recount()
        {
            var seconds = waves.Sum(wave => wave.durationSeconds);

            totalBodies = waves.Sum(wave => wave.Bodies);
            runLength = $"{(int) seconds / 60}:{(int) seconds % 60:00}";
        }

        private HordeWave BuildWave(int index, System.Random random, Dictionary<HordeUnit, int> nameCursor)
        {
            var boss = index % 10 == 0;
            var ramp = index / (float) Mathf.Max(1, wavesToBuild);

            var wave = new HordeWave
            {
                durationSeconds = boss ? 90f : 35f + Mathf.Round(ramp * 25f),
                pressure = Mathf.Round((0.8f + ramp * (0.6f + difficulty * 0.25f)) * 20f) / 20f,
                bossWave = boss,
                bossGate = boss ? SpawnGate.SouthCrypt : SpawnGate.NorthStair,
            };

            var roster = Roster(index);
            var groupCount = boss ? 2 : 2 + random.Next(index < 8 ? 1 : 3);

            for (var i = 0; i < groupCount; i++)
            {
                var unit = roster[random.Next(roster.Count)];

                wave.groups.Add(new SpawnGroup
                {
                    unit = unit,
                    count = BodyCount(unit, random),
                    gate = (SpawnGate) random.Next(5),
                    delaySeconds = Mathf.Round(i * wave.durationSeconds / (groupCount + 1f)),
                });
            }

            wave.waveName = NextName(boss ? HordeUnit.Warden : Carries(wave), nameCursor);

            return wave;
        }

        /// <summary>The unit that sends the most bodies in a wave, which is what the wave gets named for.</summary>
        private static HordeUnit Carries(HordeWave wave) => wave.groups
            .GroupBy(group => group.unit)
            .OrderByDescending(group => group.Sum(entry => entry.count))
            .First()
            .Key;

        /// <summary>Walks a unit's name pool so two waves in a row never land on the same beat name.</summary>
        private static string NextName(HordeUnit unit, Dictionary<HordeUnit, int> cursor)
        {
            var pool = Names[unit];

            cursor.TryGetValue(unit, out var used);
            cursor[unit] = used + 1;

            return used < pool.Length ? pool[used] : $"{pool[used % pool.Length]} {used / pool.Length + 1}";
        }

        /// <summary>Units unlocked by this point in the run. Later waves keep everything earlier ones had.</summary>
        private List<HordeUnit> Roster(int index)
        {
            var roster = new List<HordeUnit> { HordeUnit.Grunt };

            if (index >= 3) roster.Add(HordeUnit.Runner);
            if (index >= 8 || difficulty >= 4) roster.Add(HordeUnit.Shielder);
            if (index >= 12) roster.Add(HordeUnit.Spitter);
            if (index >= 20 || difficulty == 5) roster.Add(HordeUnit.Brute);

            return roster;
        }

        private static int BodyCount(HordeUnit unit, System.Random random) => unit switch
        {
            HordeUnit.Grunt => 4 + random.Next(5),
            HordeUnit.Runner => 3 + random.Next(4),
            HordeUnit.Shielder => 2 + random.Next(3),
            HordeUnit.Spitter => 1 + random.Next(3),
            HordeUnit.Brute => 1 + random.Next(2),
            _ => 1,
        };

        private void Reset()
        {
            RebuildWaveTable();
        }
    }
}
