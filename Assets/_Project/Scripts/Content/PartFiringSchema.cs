using System;
using UnityEngine;

namespace KaijuBreaker.Content
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Per-part firing & movement schema (per-part-firing-schema.md §2–§6).
    // Pure static authoring data appended to PartDef so a boss part can be a named
    // emission source (Raiden-style "different parts fire different bullets") and/or
    // move independently. All types default to the inert value so existing KaijuDef
    // assets and the 448 EditMode tests are unaffected (backward-compatible).
    // Runtime execution lives in KaijuBreaker.App.Gameplay (BossController) + KaijuParts.
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>How a boss part moves relative to the kaiju body (§2). None = static (default).</summary>
    public enum PartMovementType
    {
        /// <summary>Static — the part holds its authored local position.</summary>
        None = 0,
        /// <summary>Revolves around <see cref="PartMovement.PivotOffset"/> at <see cref="PartMovement.RadiusWorld"/> (PRISMSHELL facets, NULLSPIRE satellites).</summary>
        Orbit = 1,
        /// <summary>Hinges back and forth through ±<see cref="PartMovement.ArcHalfDeg"/> about the pivot (LACERA limbs).</summary>
        SweepArc = 2,
        /// <summary>Gentle angular oscillation about its rest angle (EMBERWING wings, LACERA tail).</summary>
        Oscillate = 3,
        /// <summary>Rotates in place about its own centre (NULLSPIRE shields).</summary>
        Spin = 4
    }

    /// <summary>
    /// Per-part movement descriptor (§2). Value struct — defaults to <see cref="PartMovementType.None"/>
    /// (all-zero), so a part with no authored movement stays static.
    /// </summary>
    [Serializable]
    public struct PartMovement
    {
        [Tooltip("Locomotion archetype. None = static (default).")]
        [SerializeField] private PartMovementType _type;

        [Tooltip("Orbit/SweepArc pivot, relative to the kaiju root (world units).")]
        [SerializeField] private Vector2 _pivotOffset;

        [Tooltip("Orbit radius (world units). Ignored by other types.")]
        [SerializeField] private float _radiusWorld;

        [Tooltip("Angular speed (deg/s): Orbit revolution / Spin self-rotation / SweepArc·Oscillate sweep rate.")]
        [SerializeField] private float _angularSpeedDeg;

        [Tooltip("SweepArc / Oscillate half-amplitude (deg). Ignored by Orbit/Spin.")]
        [SerializeField] private float _arcHalfDeg;

        [Tooltip("Initial phase offset (deg) — lets multiple parts move anti-phase (e.g. LACERA limbs at 0/180/90/270).")]
        [SerializeField] private float _phaseDeg;

        /// <summary>Locomotion archetype. None = static.</summary>
        public PartMovementType Type => _type;
        /// <summary>Orbit/SweepArc pivot relative to the kaiju root (world units).</summary>
        public Vector2 PivotOffset => _pivotOffset;
        /// <summary>Orbit radius in world units.</summary>
        public float RadiusWorld => _radiusWorld;
        /// <summary>Angular speed in deg/s (revolution / spin / sweep rate).</summary>
        public float AngularSpeedDeg => _angularSpeedDeg;
        /// <summary>SweepArc/Oscillate half-amplitude in degrees.</summary>
        public float ArcHalfDeg => _arcHalfDeg;
        /// <summary>Initial phase offset in degrees.</summary>
        public float PhaseDeg => _phaseDeg;
    }

    /// <summary>Gate deciding when a part's emitter is allowed to fire (§3). AliveOnly = always while alive (default).</summary>
    public enum PartFireGate
    {
        /// <summary>Fires whenever the part is alive; silenced when it breaks (default — the core reward loop).</summary>
        AliveOnly = 0,
        /// <summary>Pauses while the part is heat-softened (PRISMSHELL facet stops refracting, exposing its seam).</summary>
        SilenceWhenSoftened = 1,
        /// <summary>Only fires once this (armored) part's armor has been stripped.</summary>
        RequireArmorStripped = 2,
        /// <summary>Only fires once the part named by <see cref="PartEmitter.GatePartId"/> is broken (BROODCORE core after the veil breaks).</summary>
        RequireGatePartBroken = 3
    }

    /// <summary>
    /// One emission source on a part (§3). A part may carry 0..N of these. When
    /// <see cref="SpawnEnemyId"/> is set the "emitter" instead periodically spawns that trash
    /// enemy (BROODCORE sacs birth spore_mite) capped at <see cref="SpawnCap"/>.
    /// </summary>
    [Serializable]
    public struct PartEmitter
    {
        [Tooltip("Bullet pattern fired by this part (may be a Spiral). Null when this slot is a pure spawner.")]
        [SerializeField] private EmitterPatternSO _pattern;

        [Tooltip("When this emitter is allowed to fire.")]
        [SerializeField] private PartFireGate _gate;

        [Tooltip("RequireGatePartBroken only: the part id whose break unlocks this emitter. Empty otherwise.")]
        [SerializeField] private string _gatePartId;

        [Tooltip("Non-empty = this slot periodically SPAWNS this trash-enemy id instead of firing bullets (BROODCORE sac -> spore_mite).")]
        [SerializeField] private string _spawnEnemyId;

        [Tooltip("Spawner only: max simultaneous minions this part keeps alive.")]
        [SerializeField] private int _spawnCap;

        /// <summary>Bullet pattern fired (null for a pure spawner slot).</summary>
        public EmitterPatternSO Pattern => _pattern;
        /// <summary>Fire gate condition.</summary>
        public PartFireGate Gate => _gate;
        /// <summary>Part id whose break unlocks this emitter (RequireGatePartBroken only).</summary>
        public string GatePartId => _gatePartId;
        /// <summary>Trash-enemy id this slot spawns, if any.</summary>
        public string SpawnEnemyId => _spawnEnemyId;
        /// <summary>Max simultaneous spawned minions.</summary>
        public int SpawnCap => _spawnCap;

        /// <summary>True when this slot spawns minions instead of firing bullets.</summary>
        public bool IsSpawner => !string.IsNullOrEmpty(_spawnEnemyId);
    }

    /// <summary>Cross-part gate kind (§4): does another part's state control this part's hittability or breakability?</summary>
    public enum PartGateKind
    {
        /// <summary>No cross-part gate (default).</summary>
        None = 0,
        /// <summary>This part's hitbox is disabled until the gate condition holds (TIDEMAW core behind the dorsal plate).</summary>
        HittableWhen = 1,
        /// <summary>This part cannot take break damage until the gate condition holds (EMBERWING outer vent behind its wing-root).</summary>
        BreakableWhen = 2
    }

    /// <summary>The state a gate part must be in to satisfy a <see cref="PartGateKind"/> (§4).</summary>
    public enum PartGateCond
    {
        /// <summary>Gate part(s) broken.</summary>
        GatePartBroken = 0,
        /// <summary>Gate part(s) armor-stripped.</summary>
        GatePartArmorStripped = 1,
        /// <summary>Gate part(s) heat-softened (PRISMSHELL weak_node exposed by softened facets).</summary>
        GatePartSoftened = 2,
        /// <summary>Gate part(s) heat-softened OR armor-stripped — either exposes the seam (PRISMSHELL weak_node).</summary>
        GatePartSoftenedOrStripped = 3
    }

    /// <summary>
    /// Per-part break-gauge regeneration (§5, TIDEMAW). When enabled, a part whose break track
    /// received no input for <see cref="GraceSeconds"/> decays its accumulated break units at
    /// <see cref="RegenRatePerSec"/> BU/s (never resurrecting an already-broken part). Defaults to
    /// disabled (all-zero struct).
    /// </summary>
    [Serializable]
    public struct ArmorRegen
    {
        [Tooltip("Enable per-part break-gauge regen (TIDEMAW). Off by default.")]
        [SerializeField] private bool _enabled;

        [Tooltip("Seconds without a break-track hit before the gauge starts decaying.")]
        [SerializeField] private float _graceSeconds;

        [Tooltip("Break-unit decay rate (BU/s) once the grace window elapses.")]
        [SerializeField] private float _regenRatePerSec;

        /// <summary>True when this part regenerates its break gauge.</summary>
        public bool Enabled => _enabled;
        /// <summary>Grace window (s) after the last break hit before decay begins.</summary>
        public float GraceSeconds => _graceSeconds;
        /// <summary>Break-unit decay rate (BU/s) after the grace window.</summary>
        public float RegenRatePerSec => _regenRatePerSec;
    }

    /// <summary>
    /// Whole-kaiju idle body motion (§6) — a data-driven replacement for the hard-coded
    /// BossController breathing/drift. Defaults to zero (no motion).
    /// </summary>
    [Serializable]
    public struct BodyMovement
    {
        [Tooltip("Horizontal drift amplitude (world units).")]
        [SerializeField] private float _driftAmpX;
        [Tooltip("Vertical drift amplitude (world units).")]
        [SerializeField] private float _driftAmpY;
        [Tooltip("Horizontal drift frequency (Hz).")]
        [SerializeField] private float _driftFreqX;
        [Tooltip("Vertical drift frequency (Hz).")]
        [SerializeField] private float _driftFreqY;

        /// <summary>Horizontal drift amplitude (world units).</summary>
        public float DriftAmpX => _driftAmpX;
        /// <summary>Vertical drift amplitude (world units).</summary>
        public float DriftAmpY => _driftAmpY;
        /// <summary>Horizontal drift frequency (Hz).</summary>
        public float DriftFreqX => _driftFreqX;
        /// <summary>Vertical drift frequency (Hz).</summary>
        public float DriftFreqY => _driftFreqY;
    }
}
