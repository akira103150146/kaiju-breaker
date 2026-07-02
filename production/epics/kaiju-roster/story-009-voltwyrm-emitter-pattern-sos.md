# Story 009: VOLTWYRM EmitterPatternSO Definitions (Patterns A / B / C + Core Direct-Fire)

> **Epic**: 巨獸內容（CARAPEX / LACERA / VOLTWYRM）
> **Status**: Ready
> **Layer**: Feature
> **Type**: Config/Data
> **Estimate**: S
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/kaiju/03-voltwyrm.md` §5 (Attack Patterns), §8 (Difficulty Scaling)
**Requirement**: `TR-kaiju-009` (EmitterPatternSO expressiveness), `TR-kaiju-005` (bullet readability at max density)
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0003: ScriptableObjects (primary) + ADR-0001: Bullet Engine Backend (expressiveness reference — Proposed)
**ADR Decision Summary**: All 4 VOLTWYRM attack patterns are authored as `EmitterPatternSO` assets (ADR-0003, Accepted). VOLTWYRM is the expressiveness stress-test: Pattern A (multi-arm spiral) requires BulletSim's rotational multi-arm emitter shape; Pattern C (radial burst ring) requires a 12-bullet radial shape; Pattern B (energy aimed wall) requires a horizontal spread with convergence. This story confirms all patterns are expressible using existing shape vocabulary — if any shape is missing, surface as a blocker before Story 010. Runtime execution is Story 010.

**Engine**: Unity 6.3 | **Risk**: LOW (data authoring); ADR-0001 expressiveness risk flagged below
**Engine Notes**: ADR-0001 §Validation gate 11.3 requires "all 3 boss patterns expressible without new shape". If any VOLTWYRM shape does NOT exist in the current `EmitterShape` enum, that is a blocker for both this story and ADR-0001 LOCK — surface immediately. No DOTS dependency in this data story.

**Control Manifest Rules (Feature layer — Content tier)**:
- Required: Pattern SOs at `Assets/_Project/Content/Kaiju/VOLTWYRM/Patterns/`; warm-color bullets (orange/gold/white with black pixel border); per-difficulty parameter arrays length=4; Pattern A arm counts per difficulty per phase authored as `int[3][4]` (3 phases × 4 difficulty tiers).
- Forbidden: Cool-color bullets; game state logic in SO; new EmitterShape types (if required, surface as ADR-0001 blocker before writing new enum value).
- Guardrail: Pattern C terminal ring (20 bullets, triggered on shield BROKEN) must be distinct from the regular hit-triggered ring (12 bullets) — authored as separate `RingVariant` entries in the SO, not as hardcoded special-case logic.

---

## Acceptance Criteria

*From `design/gdd/kaiju/03-voltwyrm.md` §5 and §8 difficulty tables:*

**Pattern A — Serpent Spiral (`Voltwyrm_PatternA_SerpentSpiral.asset`)**
- [ ] `emitter_source`: all alive neck segments emit together (multi-source, same pattern — authored as `emitter_source_group = "neck_segs"` or equivalent SO flag)
- [ ] Bullet shape: multi-arm rotation; rotation period = 0.8 s (authored as `rotation_period_s = 0.8`)
- [ ] Arm count per difficulty, per phase — authored as a 3×4 matrix in SO (`armCountByPhaseAndDifficulty[3][4]`):
  - Phase 1: D1=1, D2=2, D3=3, D4=4
  - Phase 2: D1=2, D2=3, D3=4, D4=4 (Phase 2 D4: same arm count + speed flag)
  - Phase 3: D1=3, D2=4, D3=4, D4=4 (Phase 3 D3/D4: speed +15% / +30% via `phase3_speed_mult` field)
- [ ] `phase3_speed_mult` float array [D1=1.0, D2=1.0, D3=1.15, D4=1.30] — authored in SO, not hardcoded
- [ ] Arm gap width fixed (= 360° ÷ arm_count); not a tunable field (gap consistency is the readability guarantee)
- [ ] Bullet color: orange-gold warm palette (e.g., #FFAA00 / #FF8800); black pixel border
- [ ] OnValidate: rotation_period_s ∈ [0.5, 2.0]; armCount[phase][difficulty] ≥ 1 for all cells

**Pattern B — Energy Aimed Wall (`Voltwyrm_PatternB_EnergyAimedWall.asset`)**
- [ ] `emitter_source`: `shield_left` and `shield_right` fire independently (per-shield instances)
- [ ] Bullet shape: horizontal converging wall with fixed scatter angle toward player position (non-perfect-aim; authored as `aim_scatter_deg = 15` or similar field)
- [ ] Row count per difficulty: D1=1, D2=2, D3=3, D4=3 (authored as `rowCountByDifficulty[4]`)
- [ ] Fire interval per shield: D1=3.0s, D2=2.0s, D3=1.5s, D4=1.0s (authored as `fireIntervalByDifficulty[4]`)
- [ ] Telegraph: `telegraph_duration = 0.3 s`; `telegraph_type = charge_flash`; `telegraph_color = bright_yellow` (yellow charge on shield before fire)
- [ ] `trigger_condition = ShieldAlive` (fires when shield is ALIVE regardless of armor state — even ARMOR_STRIPPED shields fire Pattern B; shield BROKEN = permanent stop)
- [ ] Bullet color: horizontal energy bolts — warm (orange #FF6600 or similar); distinct from Pattern A spiral but still in warm spectrum
- [ ] OnValidate: fireIntervalByDifficulty[3] (D4) ≥ 0.5s; telegraph > 0; rowCount ≤ 5

**Pattern C — Shield Burst Ring (`Voltwyrm_PatternC_ShieldBurstRing.asset`)**
- [ ] Bullet shape: radial ring; bullets spread uniformly (= 360° / bullet_count)
- [ ] `trigger_type = OnHit` (fires when shield receives any `LaserHit` or `MissileHit` — per GDD §5)
- [ ] Per-difficulty trigger behavior (authored as `ringCountOnHitByDifficulty[4]`):
  - D1: rings per hit = 0 (only fires on BROKEN); trigger only on shield BROKEN
  - D2: rings per hit = 1 (each hit = 1 ring)
  - D3: rings per hit = 1; L3WaveHit = 2 rings (delay 0.4s between rings; `l3_wave_ring_count = 2`)
  - D4: rings per hit = 2; L3WaveHit = 3 rings (`l3_wave_ring_count = 3`)
- [ ] Ring bullet count: regular hit ring = 12 bullets/ring; L3WaveHit ring = same 12 bullets
- [ ] **Terminal ring** (shield BROKEN): 20 bullets, larger ring radius — authored as separate `terminalRingBulletCount = 20` and `terminalRingRadiusMult = 1.4` fields
- [ ] Ring expansion speed: authored as `ringExpansionSpeedPxPerS` field (constant, not per-difficulty)
- [ ] Bullet color: orange-red `#FF4500`; black pixel border
- [ ] OnValidate: regular ring bullet count = 12; terminal = 20; terminal radius > regular radius; ringCountOnHit D1 = 0 (verified)

**Phase 3 Core Direct-Fire (`Voltwyrm_PatternD_CoreDirectFire.asset`)**
- [ ] `emitter_source`: `core_node`
- [ ] Bullet shape: narrow aimed burst (6 bullets per volley in tight cluster, all aimed at player position)
- [ ] `phase_restriction = Phase3Only` (active only when Phase 3 condition met)
- [ ] `difficulty_restriction = D2Plus` (disabled at D1 per GDD §8 table)
- [ ] Fire interval: D1=disabled, D2=0.8s, D3=0.6s, D4=0.5s (authored as `fireIntervalByDifficulty[4]` with D1=0 meaning disabled)
- [ ] Bullet color: orange-white (#FFCC88 or similar high-brightness warm); represents core energy
- [ ] OnValidate: `phase_restriction = Phase3Only` field present; D1 interval = 0 (disabled sentinel)

**Cross-pattern**
- [ ] All 4 SOs confirm no new `EmitterShape` enum values required — multi-arm spiral, radial ring, converging wall, aimed burst all must already exist in `EmitterShape` vocab; if any is missing, log as **BLOCKER** in smoke-check doc and notify engine-programmer before closing story
- [ ] All 4 SOs pass OnValidate with zero Inspector errors

---

## Implementation Notes

*Derived from ADR-0003 and GDD §5/§8:*

**Pattern A 3×4 arm matrix**: Author as a 2D serialized array or a flat int[12] array with `GetArmCount(int phase, int difficulty)` helper. Unity doesn't serialize jagged arrays natively — use a wrapper struct:
```csharp
[Serializable]
public struct ArmCountMatrix {
    [SerializeField] int[] data; // length = 3 * 4 = 12; index = phase*4 + difficultyIndex
    public int Get(int phase, int diff) => data[phase * 4 + diff];
}
```

**Pattern C per-difficulty trigger**: Author `ringCountOnHitByDifficulty` as int[4] = {0, 1, 1, 2}. The L3WaveHit ring count is a separate field `l3WaveRingCount[4]` = {0, 0, 2, 3}. Both arrays length=4. BulletSim reads the current difficulty tier from `IDifficultyProvider` at runtime to select the right index.

**Pattern D disabled at D1**: `fireIntervalByDifficulty[0] = 0f` as sentinel. BulletSim skips emission if interval = 0. Alternatively, use a `bool[4] enabledByDifficulty = {false, true, true, true}` flag — choose whichever matches existing `EmitterPatternSO` schema.

**Expressiveness check**: Before closing this story, verify each of these shape names exists in the `BulletShapeType` enum (or equivalent):
- `MultiArmSpiral` (or `RotatingMultiArm`) — for Pattern A
- `RadialRing` (or `CircleBurst`) — for Pattern C
- `ConvergingWall` (or `HorizontalSpread`) — for Pattern B
- `AimedBurst` — for Pattern D
If any are missing, this story's smoke check is a **BLOCKER** and must be resolved before ADR-0001 LOCK.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 008: `Voltwyrm.asset` KaijuDef (part composition, adjacency)
- Story 010: Runtime encounter — shield armor gate, L4 100 HU/s validation, phase transitions, Pattern A/B/C firing wired to BulletSim

---

## QA Test Cases

*Config/Data story — smoke check (advisory):*

- **AC-1**: Expressiveness gate — all shapes exist
  - Setup: Check `EmitterShape` / `BulletShapeType` enum in `Content` assembly
  - Verify: MultiArmSpiral, RadialRing, ConvergingWall, AimedBurst (or equivalents) present
  - Pass condition: Zero new enum values needed; if any missing → log BLOCKER in smoke doc

- **AC-2**: Pattern A arm count matrix populated
  - Setup: Inspect `ArmCountMatrix` (or equivalent) in `Voltwyrm_PatternA_SerpentSpiral.asset`
  - Verify: 12 entries; Phase 1 row = [1,2,3,4]; Phase 2 row = [2,3,4,4]; Phase 3 row = [3,4,4,4]
  - Pass condition: All entries positive; no zeros in Phase 1+ rows

- **AC-3**: Pattern C ring counts per difficulty
  - Setup: Inspect `ringCountOnHitByDifficulty` and `l3WaveRingCount` arrays
  - Verify: regular hit rings = [0,1,1,2]; L3 wave rings = [0,0,2,3]
  - Pass condition: D1 regular ring = 0 (BROKEN only); terminal ring bullet count = 20

- **AC-4**: Warm bullet color compliance (all 4 patterns)
  - Setup: Inspect bullet color field on all 4 SOs
  - Verify: All colors in warm spectrum (hue ≤ 60° red-orange-yellow or white-hot for core fire); no cool colors
  - Pass condition: OnValidate color checks pass for all 4 assets

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**:
- `production/qa/smoke-voltwyrm-patterns.md` — smoke check pass doc; must include expressiveness gate result (PASS or BLOCKER)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `EmitterPatternSO` schema with `ArmCountMatrix`, `ringCountOnHitByDifficulty`, `l3WaveRingCount`, `terminalRingBulletCount`, `phase_restriction`, `difficulty_restriction` fields; all relevant `BulletShapeType` enum values present
- Unlocks: Story 010 (VOLTWYRM encounter integration)
