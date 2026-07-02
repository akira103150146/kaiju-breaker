# Story 003: CARAPEX EmitterPatternSO Definitions (Patterns A / B / C)

> **Epic**: 巨獸內容（CARAPEX / LACERA / VOLTWYRM）
> **Status**: Ready
> **Layer**: Feature
> **Type**: Config/Data
> **Estimate**: S
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/kaiju/01-carapex.md` §5 (Attack Patterns), §8 (Difficulty Scaling)
**Requirement**: `TR-kaiju-009` (EmitterPatternSO expressiveness), `TR-kaiju-005` (bullet readability)
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0003: ScriptableObjects (primary) + ADR-0001: Bullet Engine Backend (reference — expressiveness constraint)
**ADR Decision Summary**: Attack patterns are authored as `EmitterPatternSO` ScriptableObject assets (ADR-0003, Accepted). At runtime they are baked into Burst-friendly Blobs and executed by BulletSim (ADR-0001, **Proposed**). This story covers DATA AUTHORING ONLY — the SO asset definitions and their Inspector-level configuration. The SO assets must be writable without new EmitterShape types (ADR-0001 §Validation gate 11.3). Runtime firing integration is Story 004.

**Engine**: Unity 6.3 | **Risk**: LOW (data authoring only; runtime risk lives in Story 004 / ADR-0001)
**Engine Notes**: `EmitterPatternSO` is a standard SO authored in Inspector. No DOTS/Burst dependency in this story. OnValidate validates bullet counts, colors, and speed values against GDD safe ranges.

**Control Manifest Rules (Feature layer — Content tier)**:
- Required: 3 SO assets per CARAPEX pattern (A, B, C) in `Assets/_Project/Content/Kaiju/CARAPEX/Patterns/`; warm-color bullet palette enforced via OnValidate (color must pass hue range check: orange/yellow/deep-red spectrum); per-difficulty bullet-count arrays length = 4 (D1–D4).
- Forbidden: Hardcoded bullet speed constants in C# — all speeds authored in SO fields. Cool-color (blue, green, purple) bullets — violation of visual iron rule.
- Guardrail: `EmitterPatternSO` MUST NOT contain runtime logic; purely declarative data. No new `EmitterShape` enum values may be added — existing shapes only (validate expressiveness requirement).

---

## Acceptance Criteria

*From `design/gdd/kaiju/01-carapex.md` §5 Attack Patterns and §8 Difficulty Scaling:*

**Pattern A — Mandible Cross-fire (`Carapex_PatternA_MandibleCrossfire.asset`)**
- [ ] `emitter_source`: parameterized for `left_mandible` / `right_mandible` (alternating); each mandible fires independently
- [ ] Bullet shape: 3-bullet fan (D1), center aimed at player ±25° spread; fan count per difficulty: D1=3, D2=3, D3=5, D4=5
- [ ] Bullet speed: 120 px/s (constant across D1–D4)
- [ ] Bullet color: `#FF8000` (orange); pixel-border width correct per project style
- [ ] Fire interval per mandible: D1=2.5s, D2=2.0s, D3=2.0s, D4=1.5s (per GDD §8 table)
- [ ] Telegraph duration field: 0.5 s (amber mandible pulse before firing)
- [ ] OnValidate: speed=120 confirmed in range [80, 200]; color passes warm-hue check

**Pattern B — Dorsal Gravel Spray (`Carapex_PatternB_DorsalGravelSpray.asset`)**
- [ ] `emitter_source`: `dorsal_cannon`
- [ ] Bullet shape: downward horizontal wide fan; bullet counts: D1=5, D2=7, D3=9, D4=11; fan coverage ~50% screen width
- [ ] Bullet speed: 100 px/s (constant)
- [ ] Bullet color: `#FFCC00` (yellow)
- [ ] Fire interval: D1=9s, D2=8s, D3=7s, D4=6s
- [ ] Telegraph duration: 0.8 s (downward spotlight effect cue)
- [ ] `armor_active_only = true` flag (this pattern only fires when `dorsal_cannon` is ARMOR_INTACT; ARMOR_STRIPPED suppresses 2.0 s per stagger window — stagger suppression modeled via SO flag, not hardcoded logic)
- [ ] OnValidate: speed=100 in range; color warm; per-difficulty counts array length=4

**Pattern C — Core Pulse (`Carapex_PatternC_CorePulse.asset`)**
- [ ] `emitter_source`: `chest_reactor_core`
- [ ] Phase 1–2 shape: 1 aimed bullet (single, player-tracking direction)
- [ ] Phase 3 shape (D1/D2): 4-way cross (up/down/left/right, fixed direction, non-tracking)
- [ ] Phase 3 shape (D3/D4): 8-way (4-way + 4 diagonal at 45°, fixed, non-tracking)
- [ ] Bullet speed: 90 px/s (constant all phases/difficulties)
- [ ] Bullet color: `#CC2200` (deep red)
- [ ] Fire intervals — Phase 1–2: D1=4.0s, D2=3.5s, D3=3.0s, D4=2.5s; Phase 3: D1=3.0s, D2=2.5s, D3=2.0s, D4=1.5s (per GDD §8)
- [ ] Telegraph duration: 0.6 s (core pulse visual is the telegraph itself — cue type = `core_pulse`)
- [ ] Phase variant data authored within a single SO using `PhaseVariant[]` sub-array (one entry per phase) — no separate SO files per phase
- [ ] OnValidate: speed=90 in range; no cool-color bullets; phase variant count = 2

**Cross-pattern (all 3 SOs)**
- [ ] No new `EmitterShape` enum values required — all patterns expressible with existing shape vocabulary (fan, cross, radial, aimed) per ADR-0001 §11.3 expressiveness gate
- [ ] All 3 SOs pass OnValidate with zero Inspector errors

---

## Implementation Notes

*Derived from ADR-0003 SO schema and GDD §5/§8:*

`EmitterPatternSO` is a ScriptableObject with approximately the following top-level fields (verify against existing schema in `Assets/_Project/Content/` before authoring):

```
EmitterPatternSO {
  string emitterSourcePartId;
  BulletShapeType shapeType;           // Fan, Cross, Aimed, Radial, Multi-Arm
  AnimationCurve bulletCountByDifficulty;  // OR int[4] bulletCountPerTier
  float bulletSpeedPxPerS;
  Color bulletColor;
  float fireIntervalS;               // OR float[4] per difficulty
  float telegraphDurationS;
  bool armorActiveOnly;              // suppress if part is ARMOR_STRIPPED
  PhaseVariant[] phaseVariants;      // per-phase overrides (shape, fire interval)
}
```

For Pattern C's phase variants, use `PhaseVariant[]` rather than separate assets, since the GDD specifies a single emitter source (`chest_reactor_core`) with behavior that varies by phase. The `KaijuBehaviorController` (Story 001) will select the correct `PhaseVariant` index when activating the pattern.

Pattern B's ARMOR_STRIPPED suppression: the `armorActiveOnly` flag signals BulletSim Bridge to pause this emitter when the source part is in ARMOR_STRIPPED state. The 2.0s duration is governed by `PartSystemConfig.stagger_duration` — do not duplicate it in Pattern B's SO.

Color values must be authored as Unity `Color` fields (HDR=false). OnValidate hue check: hue ∈ [0°, 60°] (red/orange/yellow) for all bullet colors. Any hue outside this range triggers an Inspector error.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 004: Runtime firing integration — wiring Pattern SOs to BulletSim, armor gate BU=0 enforcement, phase-driven pattern activation
- Story 002: KaijuDef SO (part composition, adjacency, drops)
- Story 001: Boss Phase Controller (which phase to activate which pattern variant)

---

## QA Test Cases

*Config/Data story — smoke check (advisory):*

- **AC-1**: Bullet color warm-hue check (all 3 patterns)
  - Setup: Open each of the 3 pattern SO assets in Inspector
  - Verify: Pattern A color #FF8000 hue ≈ 30°; Pattern B #FFCC00 hue ≈ 48°; Pattern C #CC2200 hue ≈ 6° — all within warm-orange-red range
  - Pass condition: OnValidate logs zero color warnings; visual preview shows warm tones

- **AC-2**: Per-difficulty bullet counts present
  - Setup: Inspect `bulletCountPerTier` or `bulletCountByDifficulty` array on each SO
  - Verify: Array length = 4; Pattern A values = [3,3,5,5]; Pattern B values = [5,7,9,11]; Pattern C Phase1 = [1,1,1,1] (1 aimed per tier)
  - Pass condition: All arrays populated; no null/zero entries

- **AC-3**: Bullet speed constants correct
  - Setup: Inspect `bulletSpeedPxPerS` on each SO
  - Verify: Pattern A = 120; Pattern B = 100; Pattern C = 90
  - Pass condition: Values match GDD §5 exactly; OnValidate range check passes

- **AC-4**: No new EmitterShape types required
  - Setup: Review `EmitterShape` (or equivalent) enum in `Content` assembly
  - Verify: Pattern A uses Fan shape; Pattern B uses Fan (wide, downward); Pattern C uses Aimed (phase 1-2) and Cross/MultiWay (phase 3) — all pre-existing shape types
  - Pass condition: No new enum values were added to satisfy CARAPEX patterns; expressiveness gate met

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**:
- `production/qa/smoke-carapex-patterns.md` — smoke check pass doc

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `EmitterPatternSO` class must exist in `Content` assembly with `PhaseVariant[]` sub-structure; `EmitterShape` enum must include Fan, Cross, Aimed, Radial shapes
- Unlocks: Story 004 (CARAPEX encounter integration will reference these SO assets by path)
