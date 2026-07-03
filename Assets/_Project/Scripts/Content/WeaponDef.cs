using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Weapon pool classification. Primary pool fires lasers (heat track);
    /// Secondary pool fires missiles (break track). See weapon-system.md C.1.
    /// </summary>
    public enum WeaponType
    {
        /// <summary>Primary pool — laser family (L1–L4). Fills the heat gauge.</summary>
        Laser = 0,

        /// <summary>Secondary pool — missile family (M1–M4). Fills the armor-break gauge.</summary>
        Missile = 1
    }

    /// <summary>
    /// Per-weapon tuning data. One asset per weapon (8 total).
    /// Fields are flat — all laser and missile knobs live together; fields
    /// irrelevant to this weapon's type are left at 0 / false.
    /// Tier resolution logic lives in KaijuBreaker.Weapons (ADR-0002/0003).
    /// See weapon-system.md G.2 (laser knobs) and G.3 (missile knobs).
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/Config/WeaponDef", fileName = "WeaponDef")]
    public sealed class WeaponDef : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable key used by ContentRegistry and Economy to look up this weapon.")]
        [SerializeField] private WeaponId _id = WeaponId.L1;

        [Tooltip("Pool classification: Laser fills heat; Missile fills armor-break.")]
        [SerializeField] private WeaponType _type = WeaponType.Laser;

        // ── L1 散波雷射 Spread Laser ─────────────────────────────────────────

        [Header("L1 Spread Laser")]
        [Tooltip("Full-spread (3-beam) heat rate (HU/s). Safe range [18, 35]. weapon-system.md G.2.")]
        [SerializeField] private float _l1HRateFull = 25f;

        [Tooltip("Center-beam-only heat rate (HU/s). Safe range [6, 12]. weapon-system.md G.2.")]
        [SerializeField] private float _l1HRateCenter = 8.3f;

        [Tooltip("T3 residual flame H-rate multiplier relative to L1HRateFull. Safe range [0.30, 0.60]. weapon-system.md G.2.")]
        [SerializeField] private float _l1T3ResidualRateMult = 0.40f;

        [Tooltip("T3 residual flame duration (s). Safe range [1.0, 2.5]. weapon-system.md G.2.")]
        [SerializeField] private float _l1T3ResidualDuration = 1.5f;

        // ── L2 集束雷射 Focus Beam ───────────────────────────────────────────

        [Header("L2 Focus Beam")]
        [Tooltip("Center-beam heat rate (HU/s). Safe range [28, 50]. weapon-system.md G.2.")]
        [SerializeField] private float _l2HRate = 37.5f;

        [Tooltip("T3 auto-track trigger threshold (fraction of H_max). Safe range [0.70, 0.90]. " +
                 "weapon-system.md G.2 (l2_t3_autotrack_heat_pct = 80 %).")]
        [SerializeField] private float _l2T3AutotrackHeatPct = 0.80f;

        [Tooltip("T3 auto-track max offset range (±px). Safe range [10, 25]. weapon-system.md G.2.")]
        [SerializeField] private float _l2T3AutotrackRangePx = 15f;

        [Tooltip("T3 heat ripple: heat injected into each live neighbour on part break " +
                 "(fraction of neighbour H_max). Safe range [0.20, 0.50]. weapon-system.md G.2.")]
        [SerializeField] private float _l2T3AdjacentHeatPct = 0.30f;

        // ── L3 波動砲 Wave Cannon ────────────────────────────────────────────

        [Header("L3 Wave Cannon")]
        [Tooltip("Tap-mode output multiplier (× D0). Safe range [0.40, 0.80]. weapon-system.md G.2.")]
        [SerializeField] private float _l3TapOutputMult = 0.60f;

        [Tooltip("Hold duration before shockwave triggers (s). Safe range [1.2, 2.0]. weapon-system.md G.2.")]
        [SerializeField] private float _l3ChargeTime = 1.5f;

        [Tooltip("Shockwave output multiplier (× D0). Safe range [2.0, 3.0]. weapon-system.md G.2.")]
        [SerializeField] private float _l3ChargeOutputMult = 2.50f;

        [Tooltip("Cooldown after shockwave release (s). Safe range [1.5, 2.5]. weapon-system.md G.2.")]
        [SerializeField] private float _l3ChargeCooldown = 2.0f;

        [Tooltip("T3 resonance diffusion: instant heat injected into hit part " +
                 "(fraction of part H_max). Safe range [0.30, 0.70]. weapon-system.md G.2.")]
        [SerializeField] private float _l3T3HeatInjectPct = 0.50f;

        // ── L4 穿透雷射 Pierce Beam ──────────────────────────────────────────

        [Header("L4 Pierce Beam")]
        [Tooltip("Fire interval between piercing pulses (s). Safe range [0.3, 0.6]. weapon-system.md G.2.")]
        [SerializeField] private float _l4FireInterval = 0.4f;

        [Tooltip("Per-part heat rate for each pierced part (HU/s). Safe range [18, 35]. weapon-system.md G.2.")]
        [SerializeField] private float _l4HRate = 25f;

        [Tooltip("T3 heat afterimage H-rate multiplier. Safe range [0.25, 0.55]. weapon-system.md G.2.")]
        [SerializeField] private float _l4T3AfterimageRateMult = 0.40f;

        [Tooltip("T3 heat afterimage duration (s). Safe range [1.5, 3.0]. weapon-system.md G.2.")]
        [SerializeField] private float _l4T3AfterimageRateMultDuration = 2.0f;

        // ── M1 追蹤飛彈 Homing Missile ───────────────────────────────────────

        [Header("M1 Homing Missile")]
        [Tooltip("Missiles launched per shot. Fixed = 2. weapon-system.md G.3.")]
        [SerializeField] private int _m1MissilesPerShot = 2;

        [Tooltip("Break value per missile (× D0). Safe range [0.40, 0.60]. weapon-system.md G.3.")]
        [SerializeField] private float _m1DmgPerMissileMult = 0.50f;

        [Tooltip("Magazine capacity (missiles). weapon-system.md G.3.")]
        [SerializeField] private int _m1MagSize = 6;

        [Tooltip("Reload time (s). Safe range [2.5, 4.0]. weapon-system.md G.3.")]
        [SerializeField] private float _m1ReloadTime = 3.0f;

        [Tooltip("Maximum tracking deflection angle (degrees, ±). Safe range [45, 75]. weapon-system.md G.3.")]
        [SerializeField] private float _m1TrackingAngleDeg = 60f;

        [Tooltip("T3 missiles per shot. Fixed = 3 (third missile auto-locks highest-heat part). weapon-system.md G.3.")]
        [SerializeField] private int _m1T3MissilesPerShot = 3;

        // ── M2 蜂群飛彈 Swarm Launcher ───────────────────────────────────────

        [Header("M2 Swarm Launcher")]
        [Tooltip("Micro-missiles per salvo. Fixed = 8. weapon-system.md G.3.")]
        [SerializeField] private int _m2MicroCount = 8;

        [Tooltip("Salvo cone width (fraction of screen width). Safe range [0.60, 0.80]. weapon-system.md G.3.")]
        [SerializeField] private float _m2ConeWidthPct = 0.70f;

        [Tooltip("Reload time (s). Safe range [4.0, 6.5]. weapon-system.md G.3.")]
        [SerializeField] private float _m2ReloadTime = 5.0f;

        [Tooltip("T3 total mag size (12, split into 2× 6-burst). Fixed. weapon-system.md G.3.")]
        [SerializeField] private int _m2T3MagCount = 12;

        [Tooltip("T3 micro-cooldown between the two bursts (s). Safe range [0.5, 1.5]. weapon-system.md G.3.")]
        [SerializeField] private float _m2T3BurstMicroCd = 1.0f;

        // ── M3 穿甲魚雷 AP Torpedo ───────────────────────────────────────────

        [Header("M3 AP Torpedo")]
        [Tooltip("Break value per torpedo when part is unsoftened (× D0). Safe range [2.5, 3.5]. " +
                 "weapon-system.md G.3.")]
        [SerializeField] private float _m3DmgUnsoftenedMult = 3.0f;

        [Tooltip("Heat-shock detonation break multiplier relative to unsoftened base. " +
                 "Safe range [1.8, 2.5]. Triggers only when part is SOFTENED. weapon-system.md G.3.")]
        [SerializeField] private float _m3HeatShockFillMult = 2.0f;

        [Tooltip("Magazine capacity (torpedoes). Fixed = 3. weapon-system.md G.3.")]
        [SerializeField] private int _m3MagSize = 3;

        [Tooltip("Reload time (s). Safe range [3.5, 5.0]. weapon-system.md G.3.")]
        [SerializeField] private float _m3ReloadTime = 4.0f;

        [Tooltip("T3 AP chain: break value per chained neighbour (× D0). Safe range [1.0, 2.0]. " +
                 "Owned here; consumed by KaijuParts for chain-break resolution. weapon-system.md G.3.")]
        [SerializeField] private float _m3T3ChainDmgMult = 1.5f;

        [Tooltip("T3 AP chain: max neighbours to chain to. Fixed = 2. weapon-system.md G.3.")]
        [SerializeField] private int _m3T3ChainMaxTargets = 2;

        // ── M4 叢集炸彈 Cluster Bomb ─────────────────────────────────────────

        [Header("M4 Cluster Bomb")]
        [Tooltip("AoE radius as fraction of screen height. Safe range [0.10, 0.20]. weapon-system.md G.3.")]
        [SerializeField] private float _m4AoeRadiusPct = 0.15f;

        [Tooltip("Drop impact Y minimum offset from top of screen (fraction). Safe range [0.20, 0.35]. weapon-system.md G.3.")]
        [SerializeField] private float _m4DropYMinPct = 0.25f;

        [Tooltip("Drop impact Y maximum offset from top of screen (fraction). Safe range [0.35, 0.55]. weapon-system.md G.3.")]
        [SerializeField] private float _m4DropYMaxPct = 0.40f;

        [Tooltip("Total AoE break output cap (× D0). Safe range [0.8, 1.2]. weapon-system.md G.3.")]
        [SerializeField] private float _m4TotalOutputCapMult = 1.0f;

        [Tooltip("Single-target output multiplier (× D0, when N=1 in AoE). Safe range [1.5, 2.5]. weapon-system.md G.3.")]
        [SerializeField] private float _m4SingleTargetMult = 2.0f;

        [Tooltip("Magazine capacity (bombs). Safe range [3, 5]. weapon-system.md G.3.")]
        [SerializeField] private int _m4MagSize = 4;

        [Tooltip("Reload time (s). Safe range [3.0, 4.5]. weapon-system.md G.3.")]
        [SerializeField] private float _m4ReloadTime = 3.5f;

        [Tooltip("T3 child bomb count per cluster. Fixed = 6. weapon-system.md G.3.")]
        [SerializeField] private int _m4T3ChildCount = 6;

        [Tooltip("T3 each child bomb's break output (fraction of D0). Safe range [0.15, 0.30]. weapon-system.md G.3.")]
        [SerializeField] private float _m4T3ChildDmgPct = 0.20f;

        // ── Tiering & equal-power (feedback point 3) ─────────────────────────
        // Data-driven knobs for the equal-power retune + per-tier growth. All placeholder
        // defaults (re-tunable) — see design/gdd/weapon-tiering-and-equal-power.md.

        [Header("Equal-Power / Tiering (feedback point 3)")]
        [Tooltip("Per-weapon hit-rate correction applied to sustained output in the H.1/H.2 model. " +
                 "1.0 = perfect uptime; lower for hard-to-land weapons (L2≈0.65, M3≈0.80). Safe range (0, 1].")]
        [SerializeField] private float _effectiveHitRate = 1.0f;

        [Tooltip("L1 spread base beam count at Tier 0. Beams = base + tier → 2/3/4/5 at Tier 0-3. " +
                 "Per-beam heat = L1HRateFull / beamCount so total heat stays constant. Safe range [1, 4].")]
        [SerializeField] private int _l1BaseBeamCount = 2;

        [Tooltip("Seconds between M1 homing shots (Mag_Duration = shots × interval). Placeholder path-A. Safe range [0.08, 0.40].")]
        [SerializeField] private float _m1ShotInterval = 0.10f;

        [Tooltip("Seconds between M3 torpedo shots. Formalises GDD D.1's implied ~1s. Safe range [0.8, 1.5].")]
        [SerializeField] private float _m3ShotInterval = 1.0f;

        [Tooltip("Seconds between M4 cluster drops. Placeholder path-A. Safe range [0.15, 0.50].")]
        [SerializeField] private float _m4ShotInterval = 0.20f;

        [Tooltip("M2 'Chain Hive' salvos per magazine cycle (Option B — many small missiles). Safe range [1, 4].")]
        [SerializeField] private int _m2SalvoCount = 3;

        [Tooltip("M2 break value per micro-missile (× D₀; ×BuPerD0 → BU). Default D₀/8. Safe range [0.05, 0.60].")]
        [SerializeField] private float _m2DmgPerMissileMult = 0.125f;

        [Tooltip("Seconds between M2 salvos in a Chain Hive burst. Safe range [0.5, 1.5].")]
        [SerializeField] private float _m2InterSalvoInterval = 0.8f;

        // ── Public read-only properties ──────────────────────────────────────

        /// <summary>Stable identifier for this weapon. Used as ContentRegistry key.</summary>
        public WeaponId Id => _id;

        /// <summary>Weapon pool: Laser (primary) or Missile (secondary).</summary>
        public WeaponType Type => _type;

        // L1
        /// <summary>L1 full-spread heat rate (HU/s). weapon-system.md G.2.</summary>
        public float L1HRateFull => _l1HRateFull;
        /// <summary>L1 center-beam-only heat rate (HU/s). weapon-system.md G.2.</summary>
        public float L1HRateCenter => _l1HRateCenter;
        /// <summary>L1 T3 residual-flame H-rate multiplier. weapon-system.md G.2.</summary>
        public float L1T3ResidualRateMult => _l1T3ResidualRateMult;
        /// <summary>L1 T3 residual-flame duration (s). weapon-system.md G.2.</summary>
        public float L1T3ResidualDuration => _l1T3ResidualDuration;

        // L2
        /// <summary>L2 center-beam heat rate (HU/s). weapon-system.md G.2.</summary>
        public float L2HRate => _l2HRate;
        /// <summary>L2 T3 auto-track trigger (fraction of H_max). weapon-system.md G.2.</summary>
        public float L2T3AutotrackHeatPct => _l2T3AutotrackHeatPct;
        /// <summary>L2 T3 auto-track max offset (±px). weapon-system.md G.2.</summary>
        public float L2T3AutotrackRangePx => _l2T3AutotrackRangePx;
        /// <summary>L2 T3 heat ripple fraction per live neighbour on break. weapon-system.md G.2.</summary>
        public float L2T3AdjacentHeatPct => _l2T3AdjacentHeatPct;

        // L3
        /// <summary>L3 tap-mode output (× D0). weapon-system.md G.2.</summary>
        public float L3TapOutputMult => _l3TapOutputMult;
        /// <summary>L3 hold duration before shockwave (s). weapon-system.md G.2.</summary>
        public float L3ChargeTime => _l3ChargeTime;
        /// <summary>L3 shockwave output (× D0). weapon-system.md G.2.</summary>
        public float L3ChargeOutputMult => _l3ChargeOutputMult;
        /// <summary>L3 post-shockwave cooldown (s). weapon-system.md G.2.</summary>
        public float L3ChargeCooldown => _l3ChargeCooldown;
        /// <summary>L3 T3 resonance diffusion heat fraction. weapon-system.md G.2.</summary>
        public float L3T3HeatInjectPct => _l3T3HeatInjectPct;

        // L4
        /// <summary>L4 fire interval (s). weapon-system.md G.2.</summary>
        public float L4FireInterval => _l4FireInterval;
        /// <summary>L4 per-part heat rate (HU/s). weapon-system.md G.2.</summary>
        public float L4HRate => _l4HRate;
        /// <summary>L4 T3 afterimage H-rate multiplier. weapon-system.md G.2.</summary>
        public float L4T3AfterimageRateMult => _l4T3AfterimageRateMult;
        /// <summary>L4 T3 afterimage duration (s). weapon-system.md G.2.</summary>
        public float L4T3AfterimageRateMultDuration => _l4T3AfterimageRateMultDuration;

        // M1
        /// <summary>M1 missiles per shot. weapon-system.md G.3.</summary>
        public int M1MissilesPerShot => _m1MissilesPerShot;
        /// <summary>M1 break value per missile (× D0). weapon-system.md G.3.</summary>
        public float M1DmgPerMissileMult => _m1DmgPerMissileMult;
        /// <summary>M1 magazine size. weapon-system.md G.3.</summary>
        public int M1MagSize => _m1MagSize;
        /// <summary>M1 reload time (s). weapon-system.md G.3.</summary>
        public float M1ReloadTime => _m1ReloadTime;
        /// <summary>M1 max tracking angle (degrees, ±). weapon-system.md G.3.</summary>
        public float M1TrackingAngleDeg => _m1TrackingAngleDeg;
        /// <summary>M1 T3 missiles per shot. weapon-system.md G.3.</summary>
        public int M1T3MissilesPerShot => _m1T3MissilesPerShot;

        // M2
        /// <summary>M2 micro-missiles per salvo. weapon-system.md G.3.</summary>
        public int M2MicroCount => _m2MicroCount;
        /// <summary>M2 salvo cone width (fraction of screen width). weapon-system.md G.3.</summary>
        public float M2ConeWidthPct => _m2ConeWidthPct;
        /// <summary>M2 reload time (s). weapon-system.md G.3.</summary>
        public float M2ReloadTime => _m2ReloadTime;
        /// <summary>M2 T3 total magazine size. weapon-system.md G.3.</summary>
        public int M2T3MagCount => _m2T3MagCount;
        /// <summary>M2 T3 inter-burst micro-cooldown (s). weapon-system.md G.3.</summary>
        public float M2T3BurstMicroCd => _m2T3BurstMicroCd;

        // M3
        /// <summary>M3 unsoftened break value (× D0). weapon-system.md G.3.</summary>
        public float M3DmgUnsoftenedMult => _m3DmgUnsoftenedMult;
        /// <summary>M3 heat-shock detonation multiplier. weapon-system.md G.3.</summary>
        public float M3HeatShockFillMult => _m3HeatShockFillMult;
        /// <summary>M3 magazine size. weapon-system.md G.3.</summary>
        public int M3MagSize => _m3MagSize;
        /// <summary>M3 reload time (s). weapon-system.md G.3.</summary>
        public float M3ReloadTime => _m3ReloadTime;
        /// <summary>M3 T3 chain break value per neighbour (× D0). weapon-system.md G.3.</summary>
        public float M3T3ChainDmgMult => _m3T3ChainDmgMult;
        /// <summary>M3 T3 chain max neighbour count. weapon-system.md G.3.</summary>
        public int M3T3ChainMaxTargets => _m3T3ChainMaxTargets;

        // M4
        /// <summary>M4 AoE radius (fraction of screen height). weapon-system.md G.3.</summary>
        public float M4AoeRadiusPct => _m4AoeRadiusPct;
        /// <summary>M4 drop Y-min (fraction of screen from top). weapon-system.md G.3.</summary>
        public float M4DropYMinPct => _m4DropYMinPct;
        /// <summary>M4 drop Y-max (fraction of screen from top). weapon-system.md G.3.</summary>
        public float M4DropYMaxPct => _m4DropYMaxPct;
        /// <summary>M4 total AoE output cap (× D0). weapon-system.md G.3.</summary>
        public float M4TotalOutputCapMult => _m4TotalOutputCapMult;
        /// <summary>M4 single-target output multiplier when N=1 (× D0). weapon-system.md G.3.</summary>
        public float M4SingleTargetMult => _m4SingleTargetMult;
        /// <summary>M4 magazine size. weapon-system.md G.3.</summary>
        public int M4MagSize => _m4MagSize;
        /// <summary>M4 reload time (s). weapon-system.md G.3.</summary>
        public float M4ReloadTime => _m4ReloadTime;
        /// <summary>M4 T3 child bomb count. weapon-system.md G.3.</summary>
        public int M4T3ChildCount => _m4T3ChildCount;
        /// <summary>M4 T3 each child's output (fraction of D0). weapon-system.md G.3.</summary>
        public float M4T3ChildDmgPct => _m4T3ChildDmgPct;

        // Tiering & equal-power (feedback point 3)
        /// <summary>Per-weapon hit-rate correction for the equal-power model (0,1]. weapon-tiering-and-equal-power.md.</summary>
        public float EffectiveHitRate => _effectiveHitRate;
        /// <summary>L1 base beam count at Tier 0; beams = base + tier (2→3→4→5).</summary>
        public int L1BaseBeamCount => _l1BaseBeamCount;
        /// <summary>Seconds between M1 shots.</summary>
        public float M1ShotInterval => _m1ShotInterval;
        /// <summary>Seconds between M3 torpedo shots.</summary>
        public float M3ShotInterval => _m3ShotInterval;
        /// <summary>Seconds between M4 drops.</summary>
        public float M4ShotInterval => _m4ShotInterval;
        /// <summary>M2 Chain Hive salvos per cycle.</summary>
        public int M2SalvoCount => _m2SalvoCount;
        /// <summary>M2 break value per micro-missile (×D₀).</summary>
        public float M2DmgPerMissileMult => _m2DmgPerMissileMult;
        /// <summary>Seconds between M2 salvos.</summary>
        public float M2InterSalvoInterval => _m2InterSalvoInterval;

#if UNITY_EDITOR
        private void OnValidate()
        {
            switch (_id)
            {
                case WeaponId.L1:
                    Validate("L1HRateFull",          _l1HRateFull,          18f,   35f);
                    Validate("L1HRateCenter",         _l1HRateCenter,        6f,    12f);
                    Validate("L1T3ResidualRateMult",  _l1T3ResidualRateMult, 0.30f, 0.60f);
                    Validate("L1T3ResidualDuration",  _l1T3ResidualDuration, 1.0f,  2.5f);
                    break;

                case WeaponId.L2:
                    Validate("L2HRate",               _l2HRate,               28f,   50f);
                    Validate("L2T3AutotrackHeatPct",  _l2T3AutotrackHeatPct,  0.70f, 0.90f);
                    Validate("L2T3AutotrackRangePx",  _l2T3AutotrackRangePx,  10f,   25f);
                    Validate("L2T3AdjacentHeatPct",   _l2T3AdjacentHeatPct,   0.20f, 0.50f);
                    break;

                case WeaponId.L3:
                    Validate("L3TapOutputMult",       _l3TapOutputMult,       0.40f, 0.80f);
                    Validate("L3ChargeTime",           _l3ChargeTime,          1.2f,  2.0f);
                    Validate("L3ChargeOutputMult",     _l3ChargeOutputMult,    2.0f,  3.0f);
                    Validate("L3ChargeCooldown",       _l3ChargeCooldown,      1.5f,  2.5f);
                    Validate("L3T3HeatInjectPct",      _l3T3HeatInjectPct,     0.30f, 0.70f);
                    break;

                case WeaponId.L4:
                    Validate("L4FireInterval",             _l4FireInterval,             0.3f,  0.6f);
                    Validate("L4HRate",                    _l4HRate,                    18f,   35f);
                    Validate("L4T3AfterimageRateMult",     _l4T3AfterimageRateMult,     0.25f, 0.55f);
                    Validate("L4T3AfterimageRateMultDuration", _l4T3AfterimageRateMultDuration, 1.5f, 3.0f);
                    break;

                case WeaponId.M1:
                    Validate("M1DmgPerMissileMult",  _m1DmgPerMissileMult, 0.40f, 0.60f);
                    Validate("M1ReloadTime",          _m1ReloadTime,        2.5f,  4.0f);
                    Validate("M1TrackingAngleDeg",    _m1TrackingAngleDeg,  45f,   75f);
                    break;

                case WeaponId.M2:
                    Validate("M2ConeWidthPct",     _m2ConeWidthPct,    0.60f, 0.80f);
                    Validate("M2ReloadTime",        _m2ReloadTime,      4.0f,  6.5f);
                    Validate("M2T3BurstMicroCd",    _m2T3BurstMicroCd,  0.5f,  1.5f);
                    break;

                case WeaponId.M3:
                    Validate("M3DmgUnsoftenedMult",  _m3DmgUnsoftenedMult,  2.5f, 3.5f);
                    Validate("M3HeatShockFillMult",  _m3HeatShockFillMult,  1.8f, 2.5f);
                    Validate("M3ReloadTime",          _m3ReloadTime,         3.5f, 5.0f);
                    Validate("M3T3ChainDmgMult",     _m3T3ChainDmgMult,     1.0f, 2.0f);
                    break;

                case WeaponId.M4:
                    Validate("M4AoeRadiusPct",       _m4AoeRadiusPct,       0.10f, 0.20f);
                    Validate("M4DropYMinPct",         _m4DropYMinPct,        0.20f, 0.35f);
                    Validate("M4DropYMaxPct",         _m4DropYMaxPct,        0.35f, 0.55f);
                    Validate("M4TotalOutputCapMult",  _m4TotalOutputCapMult, 0.8f,  1.2f);
                    Validate("M4SingleTargetMult",    _m4SingleTargetMult,   1.5f,  2.5f);
                    Validate("M4ReloadTime",          _m4ReloadTime,         3.0f,  4.5f);
                    Validate("M4T3ChildDmgPct",       _m4T3ChildDmgPct,      0.15f, 0.30f);
                    if (_m4MagSize < 3 || _m4MagSize > 5)
                        Debug.LogError($"[WeaponDef:{_id}] M4MagSize = {_m4MagSize} is outside safe range [3, 5].", this);
                    break;
            }

            if (_type == WeaponType.Laser && (int)_id >= 4)
                Debug.LogError($"[WeaponDef] Id {_id} is a missile weapon but Type is set to Laser.", this);
            if (_type == WeaponType.Missile && (int)_id < 4)
                Debug.LogError($"[WeaponDef] Id {_id} is a laser weapon but Type is set to Missile.", this);
        }

        private void Validate(string field, float value, float min, float max)
        {
            if (value < min || value > max)
                Debug.LogError(
                    $"[WeaponDef:{_id}] {field} = {value} is outside safe range [{min}, {max}].", this);
        }
#endif
    }
}
