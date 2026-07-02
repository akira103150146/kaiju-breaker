using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Global configuration for Cycling Weapon Pod drop guarantees and pod lifecycle behaviour.
    /// Single shared asset — stage-specific weapon pools live in <see cref="StageDef"/>.
    /// <para>
    /// Covers all knobs from stage-system.md §K.3 (guaranteed drop counts, flash interval)
    /// and §F.2 (pod descent, dwell, cycle interval, bob, despawn).
    /// </para>
    /// <para>
    /// <b>Pure static data container</b> — no runtime pod-placement or despawn logic.
    /// </para>
    /// See stage-system.md §F.2, §F.3, §K.3, ADR-0003.
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/PodDropConfig", fileName = "PodDropConfig")]
    public sealed class PodDropConfig : ScriptableObject
    {
        [Header("Drop Guarantee Knobs (K.3)")]
        [Tooltip("Minimum Primary (laser) Cycling Pods guaranteed to spawn each run. " +
                 "Enforced by the pre-boss lull fallback mechanic (stage-system.md §F.3). " +
                 "Must be >= 1.")]
        [SerializeField] private int _guaranteedPrimaryPerStage = 1;

        [Tooltip("Minimum Secondary (missile) Cycling Pods guaranteed to spawn each run. Must be >= 1.")]
        [SerializeField] private int _guaranteedSecondaryPerStage = 1;

        [Tooltip("Number of additional Cycling Pods spawned during the fixed pre-boss lull. " +
                 "Used to fill any remaining pool gap before the boss. " +
                 "stage-system.md §K.3 pre_boss_lull_pod_count default: 1.")]
        [SerializeField] private int _preBossLullPodCount = 1;

        [Tooltip("Flash interval in seconds for the carrier enemy's top-of-sprite icon blink. " +
                 "Provides visual affordance for 'this enemy drops a pod'. " +
                 "stage-system.md §K.3 pod_carrier_flash_interval default: 0.5 s. Range: (0.0, 2.0].")]
        [SerializeField] private float _podCarrierFlashIntervalSeconds = 0.5f;

        [Header("Pod Lifecycle Knobs (F.2)")]
        [Tooltip("Seconds between weapon display cycles while the pod hovers in the reachable band. " +
                 "stage-system.md §F.2.1 pod_cycle_interval default: 3.0 s. " +
                 "PodDwellTimeSeconds must be > this value.")]
        [SerializeField] private float _podCycleIntervalSeconds = 3.0f;

        [Tooltip("Seconds the pod dwells in the player-reachable band before despawning. " +
                 "stage-system.md §F.2.2 pod_dwell_time default: 12.0 s. " +
                 "Must be > PodCycleIntervalSeconds (guarantees at least one full weapon cycle). " +
                 "At 12 s / 3 s per cycle = 4 full cycles → player sees every weapon ≥ 4 times.")]
        [SerializeField] private float _podDwellTimeSeconds = 12.0f;

        [Tooltip("Speed at which the pod descends from the elite death position into the reachable band (px/s). " +
                 "stage-system.md §F.2.1 pod_descend_speed.")]
        [SerializeField] private float _podDescendSpeedPxPerSec = 200f;

        [Tooltip("Reachable Y band as a fraction of screen height [0, 1]. " +
                 "Defines the lower portion of the screen into which pods descend, " +
                 "guaranteeing the player can always reach them. " +
                 "stage-system.md §F.2.1 pod_reachable_band_y.")]
        [SerializeField] private float _podReachableBandYPct = 0.5f;

        [Tooltip("Half-amplitude of the gentle sinusoidal bob while the pod dwells (px). " +
                 "stage-system.md §F.2.2 pod_bob_amplitude. ~2 s oscillation period.")]
        [SerializeField] private float _podBobAmplitudePx = 8f;

        [Tooltip("Total pod lifetime in seconds from spawn before forced despawn " +
                 "(safety cap covering both descent and dwell phases). " +
                 "stage-system.md §F.2.3 pod_despawn_after.")]
        [SerializeField] private float _podDespawnAfterSeconds = 20f;

        // ── Public read-only properties ───────────────────────────────────────────

        /// <summary>Guaranteed minimum Primary Pod spawns per run. Enforced by lull fallback.</summary>
        public int GuaranteedPrimaryPerStage => _guaranteedPrimaryPerStage;

        /// <summary>Guaranteed minimum Secondary Pod spawns per run.</summary>
        public int GuaranteedSecondaryPerStage => _guaranteedSecondaryPerStage;

        /// <summary>Additional Cycling Pods spawned during the pre-boss lull to fill pool gaps.</summary>
        public int PreBossLullPodCount => _preBossLullPodCount;

        /// <summary>Carrier enemy icon flash interval in seconds for pod-drop affordance.</summary>
        public float PodCarrierFlashIntervalSeconds => _podCarrierFlashIntervalSeconds;

        /// <summary>Seconds between weapon display cycles while the pod hovers.</summary>
        public float PodCycleIntervalSeconds => _podCycleIntervalSeconds;

        /// <summary>Seconds the pod dwells in the reachable band before despawning.</summary>
        public float PodDwellTimeSeconds => _podDwellTimeSeconds;

        /// <summary>Descent speed in px/s from drop origin to the reachable band.</summary>
        public float PodDescendSpeedPxPerSec => _podDescendSpeedPxPerSec;

        /// <summary>Reachable Y band as a fraction of screen height [0–1].</summary>
        public float PodReachableBandYPct => _podReachableBandYPct;

        /// <summary>Gentle hover bob half-amplitude in px while pod dwells.</summary>
        public float PodBobAmplitudePx => _podBobAmplitudePx;

        /// <summary>Total pod lifetime from spawn (descent + dwell safety cap).</summary>
        public float PodDespawnAfterSeconds => _podDespawnAfterSeconds;

        // ── Editor validation ─────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (_guaranteedPrimaryPerStage < 1)
                Debug.LogError(
                    $"[PodDropConfig] '{name}': GuaranteedPrimaryPerStage must be >= 1. " +
                    $"Current: {_guaranteedPrimaryPerStage}.", this);

            if (_guaranteedSecondaryPerStage < 1)
                Debug.LogError(
                    $"[PodDropConfig] '{name}': GuaranteedSecondaryPerStage must be >= 1. " +
                    $"Current: {_guaranteedSecondaryPerStage}.", this);

            if (_podDwellTimeSeconds <= _podCycleIntervalSeconds)
                Debug.LogError(
                    $"[PodDropConfig] '{name}': PodDwellTimeSeconds ({_podDwellTimeSeconds} s) " +
                    $"must be greater than PodCycleIntervalSeconds ({_podCycleIntervalSeconds} s) " +
                    $"to guarantee at least one complete weapon cycle before despawn.", this);

            if (_podCarrierFlashIntervalSeconds <= 0f || _podCarrierFlashIntervalSeconds > 2.0f)
                Debug.LogError(
                    $"[PodDropConfig] '{name}': PodCarrierFlashIntervalSeconds must be in (0.0, 2.0]. " +
                    $"Current: {_podCarrierFlashIntervalSeconds}.", this);
        }
    }
}
