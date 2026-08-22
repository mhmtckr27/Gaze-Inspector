using System;
using GrayWolf.Gaze.Collections;
using UnityEngine;

namespace GrayWolf.Gaze.Demo
{
    /// <summary>Ammo families a weapon can draw a reserve from.</summary>
    public enum AmmoType { Ballistic, Energy, Explosive }

    /// <summary>One reserve pool carried into a mission.</summary>
    [Serializable]
    public struct AmmoReserve
    {
        public string note;
        [Min(0)] public int rounds;
        [Min(0)] public int capacity;
    }

    /// <summary>How the trigger behaves. Swapped in the inspector by the subclass picker.</summary>
    [Serializable]
    public abstract class FireMode
    {
        /// <summary>One-line description, not serialized, so it costs no inspector row.</summary>
        public abstract string Summary { get; }
    }

    [Serializable]
    public sealed class SemiAutoFire : FireMode
    {
        [Min(0f)] public float triggerResetSeconds = 0.12f;

        public override string Summary => $"semi-auto, {triggerResetSeconds:0.00}s reset";
    }

    [Serializable]
    public sealed class BurstFire : FireMode
    {
        [Range(2, 5)] public int roundsPerBurst = 3;
        [Min(0f)] public float secondsBetweenBursts = 0.28f;

        public override string Summary => $"{roundsPerBurst}-round burst every {secondsBetweenBursts:0.00}s";

        [Button("Test Burst")]
        private void TestBurst() => Debug.Log($"[Gaze Demo] {Summary}");
    }

    [Serializable]
    public sealed class ChargedFire : FireMode
    {
        [Range(0.1f, 3f)] public float chargeSeconds = 1.2f;
        [Range(1f, 4f)] public float overchargeMultiplier = 2.5f;
        [LabelText("Fire On Full Charge")] public bool releaseWhenCharged = true;

        public override string Summary => $"charged, {chargeSeconds:0.00}s to x{overchargeMultiplier:0.0}";
    }

    /// <summary>
    /// A weapon config that puts every Gaze attribute on screen at once. Each header below is a
    /// self-contained group: validation, clamped values, the subclass picker, conditionals, and the
    /// serialized dictionary.
    /// </summary>
    [AddComponentMenu("Gaze/Demo/GazeDemoWeapon")]
    public sealed class GazeDemoWeapon : MonoBehaviour
    {
        // ===== VALIDATION =====
        [Header("Identity")]
        [Required]
        public string weaponId = "kestrel_mk2";

        [Required("Assign the muzzle that projectiles spawn from.")]
        public Transform muzzle;

        [ReadOnly]
        [LabelText("Sustained DPS")]
        public float sustainedDps = 362.67f;

        // ===== CLAMPED VALUES =====
        [Header("Ballistics")]
        [Range(1f, 200f)]
        [OnValueChanged(nameof(RecalculateDps))]
        public float damagePerRound = 34f;

        [LabelText("Fire Rate (RPM)")]
        [Min(1f)]
        [OnValueChanged(nameof(RecalculateDps))]
        [OnValueChanged(nameof(OnFireRateChanged))]
        public float roundsPerMinute = 640f;

        [Tooltip("Damage multiplier over distance, in metres.")]
        [CurveRange(0f, 120f, 0f, 1f, 0.95f, 0.45f, 0.2f)]
        public AnimationCurve damageFalloff = AnimationCurve.Linear(0f, 1f, 120f, 0.35f);

        [LabelText("Spread, Min / Max (deg)")]
        [VectorRange(0f, 0f, 8f, 8f, true)]
        public Vector2 spreadDegrees = new(0.4f, 3.5f);

        [Tooltip("Per-shot camera kick. Y is pitch-up only.")]
        [VectorRange(-2f, 0f, -2f, 2f, 4f, 2f)]
        public Vector3 recoilKick = new(0.2f, 1.4f, 0.35f);

        // ===== POLYMORPHISM =====
        [Header("Fire Mode")]
        [SerializeReference] public FireMode fireMode = new BurstFire();

        // ===== CONDITIONALS =====
        [Header("Optics")]
        public bool hasScope = true;

        [ShowIf(nameof(hasScope))]
        [Range(1f, 12f)]
        public float scopeZoom = 4f;

        [HideIf(nameof(hasScope))]
        [Range(0f, 1f)]
        public float ironSightAccuracy = 0.62f;

        [EnableIf(nameof(hasScope))]
        [Min(0f)]
        public float holdBreathSeconds = 3.5f;

        [DisableIf(nameof(hasScope))]
        [Range(0f, 1f)]
        public float hipFireBonus = 0.15f;

        // ===== DICTIONARY =====
        [Header("Ammunition")]
        public AmmoType ammoType = AmmoType.Energy;

        [InfoBox("Energy weapons vent instead of reloading.", InfoMessageType.Info)]
        [DisableIf(nameof(ammoType), AmmoType.Energy)]
        [Min(0f)]
        public float reloadSeconds = 2.1f;

        [SerializedDictionary("Ammo", "Reserve", nameof(AmmoReserve.note))]
        public SerializedDictionary<AmmoType, AmmoReserve> reserves = new();

        // ===== BUTTONS =====
        [Button("Recalculate DPS")]
        private void RecalculateDps() => sustainedDps = damagePerRound * roundsPerMinute / 60f;

        [Button]
        private void FillAllReserves()
        {
            foreach (var key in new[] { AmmoType.Ballistic, AmmoType.Energy, AmmoType.Explosive })
            {
                if (!reserves.TryGetValue(key, out var reserve))
                    continue;

                reserve.rounds = reserve.capacity;
                reserves[key] = reserve;
            }

            Debug.Log($"[Gaze Demo] {weaponId} topped up, firing {fireMode?.Summary ?? "nothing"}");
        }

        private void OnFireRateChanged(float oldValue, float newValue) =>
            Debug.Log($"[Gaze Demo] fire rate {oldValue:0} -> {newValue:0} RPM");

        private void Reset()
        {
            reserves = new SerializedDictionary<AmmoType, AmmoReserve>
            {
                { AmmoType.Ballistic, new AmmoReserve { note = "Standard issue", rounds = 180, capacity = 240 } },
                { AmmoType.Energy, new AmmoReserve { note = "Swappable cell", rounds = 4, capacity = 6 } },
                { AmmoType.Explosive, new AmmoReserve { note = "Underbarrel only", rounds = 2, capacity = 4 } },
            };

            RecalculateDps();
        }
    }
}
