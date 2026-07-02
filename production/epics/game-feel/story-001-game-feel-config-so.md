# Story 001: GameFeelConfig ScriptableObject

> **Epic**: 打擊感（VFX / SFX / Game Feel）
> **Status**: Ready
> **Layer**: Foundation (Content assembly)
> **Type**: Config/Data
> **Estimate**: S (2–3h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/game-feel.md`
**Requirement**: `TR-gamefeel-007`
*(TR-IDs derived from `game-feel.md §I` — not yet formally registered in `docs/architecture/tr-registry.yaml`; see EPIC.md note)*

**ADR Governing Implementation**: ADR-0003: 資料驅動調校 — ScriptableObject 為唯一調校資料來源
**ADR Decision Summary**: All GDD tuning knobs are expressed as ScriptableObject assets in `Assets/_Project/Content/`; read-only at runtime; replaces every YAML/JSON placeholder path from the GDD. Tests inject fixture SO instances.

**Engine**: Unity 6.3 LTS | **Risk**: MEDIUM
**Engine Notes**: SO loaded once at runtime (read-only); `[CreateAssetMenu]` and `OnValidate` are stable Unity APIs, but verify behavior unchanged in 6.3 before shipping. No DOTS/ECS API used in this story. Per `docs/engine-reference/unity/VERSION.md` — do not assume API signatures; cross-reference before writing.

**Control Manifest Rules (Content layer)**:
- Required: `GameFeelConfig : ScriptableObject` belongs in `KaijuBreaker.Content` assembly; all SO definitions live in `Content`
- Required: `OnValidate` must assert GDD safe ranges for every knob; Inspector warning on out-of-range value
- Forbidden: No runtime behavior logic in the SO — pure data + validation only
- Forbidden: No hardcoded magic numbers in any system that has a corresponding SO field

---

## Acceptance Criteria

*From GDD `design/gdd/game-feel.md §I.8`, scoped to this story:*

- [ ] `GameFeelConfig : ScriptableObject` exists at `Assets/_Project/Content/Config/GameFeelConfig.asset` with all 31 knobs from GDD §G.1–G.5:
  - G.1 Shake (13 knobs): `ShakeMagSoften`, `ShakeMagArmorStrip`, `ShakeMagL3Shockwave`, `ShakeMagM3TorpedoHit`, `ShakeMagM3HeatShock`, `ShakeMagM4Cluster`, `ShakeMagPartBreakBase`, `ShakeMagPartBreakEscalation`, `ShakeMagBossDeath`, `ShakeMagnitudeCap`, `ShakeDecayRate`, `ShakeThreshold`, `ShakeAccessibilityMult`
  - G.2 Slow-Mo (6 knobs): `SlowmoPartBreakTimescale`, `SlowmoPartBreakHoldS`, `SlowmoBossDeathTimescale`, `SlowmoBossDeathHoldS`, `SlowmoRampRate`, `SlowmoAccessibilityMult`
  - G.3 Hitstop (3 knobs): `HitstopPartBreakMs`, `HitstopBossDeathMs`, `HitstopAccessibilityMult`
  - G.4 SOFTENED (6 knobs): `SoftenedColorHue`, `SoftenedPulseFrequencyHz`, `SoftenedGlowRadiusPct`, `SoftenedVisualOnsetMaxS`, `SoftenedSfxMaxPerFrame`, `SoftenedIconEnabled`
  - G.5 Flash (3 knobs): `FlashDecayRate`, `FlashMaxAlpha`, `FlashAccessibilityMult`
- [ ] `OnValidate` enforces GDD safe ranges (e.g., `HitstopPartBreakMs` in 80–150, `ShakeMagnitudeCap` ≤ 24); Unity Inspector shows a warning if any value is out of range
- [ ] After modifying any knob value in the Inspector and re-entering the stage, gameplay behavior changes accordingly — no hardcoded bypasses exist in any consuming system
- [ ] No consuming system (Stories 002–007) contains inline magic numbers for values covered by this SO

---

## Implementation Notes

*Derived from ADR-0003 Decision:*

1. Define `GameFeelConfig : ScriptableObject` in the `KaijuBreaker.Content` assembly.
2. Annotate with `[CreateAssetMenu(menuName = "KaijuBreaker/Config/GameFeelConfig", fileName = "GameFeelConfig")]`.
3. All fields are `public` (Inspector-settable) and treated as read-only at runtime by convention. Systems receive the SO via constructor/method injection — they never `FindObjectOfType` it.
4. `OnValidate` implementation: for each knob, assert within its GDD safe range and call `Debug.LogWarning($"[GameFeelConfig] {nameof(field)} = {value} is outside safe range [{min}, {max}]")`. Do not throw exceptions — warnings only.
5. GDD `assets/data/balance/game-feel.yaml` is the GDD placeholder path; this SO **replaces** it entirely. The YAML path is not used in the Unity project.
6. Unit tests inject fixture instances created via `ScriptableObject.CreateInstance<GameFeelConfig>()` with explicitly set field values — no disk `.asset` I/O in tests.
7. Default field values in the SO definition must match GDD §G defaults (115ms, 0.12 timescale, 42px/s decay, etc.).

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: HitstopSystem reading `HitstopPartBreakMs`
- Story 003: SlowMoSystem reading slow-mo knobs
- Story 004: ShakeSystem reading shake knobs
- Story 005: SOFTENED visual systems reading G.4 knobs
- Story 006: Break payoff sequence reading flash + debris knobs
- Story 007: Reduce-motion logic reading accessibility multiplier knobs

---

## QA Test Cases

*Config/Data story — manual smoke check.*

- **AC-1**: All G-section knobs present and default-populated
  - Setup: Open `Assets/_Project/Content/Config/GameFeelConfig.asset` in Unity Inspector
  - Verify: All 31 fields visible with default values matching GDD §G (e.g., `HitstopPartBreakMs = 115`, `ShakeMagnitudeCap = 24`, `SoftenedColorHue = #FF6600`, `FlashDecayRate = 2.6`)
  - Pass condition: No field missing; every G-section knob present; default values match GDD

- **AC-2**: `OnValidate` safe-range warning fires
  - Setup: Set `HitstopPartBreakMs = 200` (above 150 safe max); trigger re-import or enter Play Mode
  - Verify: Unity Console shows a warning referencing `HitstopPartBreakMs` and the safe range
  - Pass condition: Warning appears; no exception; value is not auto-corrected (warning only)

- **AC-3**: Knob change reflects in gameplay
  - Setup: Set `ShakeMagBossDeath = 0`; enter stage; trigger boss death
  - Verify: No screen shake occurs on boss death
  - Pass condition: Zero shake observed; no error log; confirms no hardcoded 24px value in ShakeSystem

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**:
- Config/Data: `production/qa/smoke-game-feel-config.md` — smoke check pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None
- Unlocks: Story 002 (Hitstop), Story 003 (Slow-Mo), Story 004 (Screen Shake), Story 005 (SOFTENED Signature), Story 007 (Reduce-Motion)
