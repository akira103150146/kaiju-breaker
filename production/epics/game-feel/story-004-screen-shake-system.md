# Story 004: 螢幕震動系統 (Screen Shake System)

> **Epic**: 打擊感（VFX / SFX / Game Feel）
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Logic
> **Estimate**: M (3–4h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/game-feel.md`
**Requirement**: `TR-gamefeel-004`
*(TR-IDs derived from `game-feel.md §I` — not yet formally registered in `docs/architecture/tr-registry.yaml`; see EPIC.md note)*

**ADR Governing Implementation**:
- Primary: ADR-0002: 事件架構 — subscribe all shake-relevant events via `IEventBus`
- Secondary: ADR-0003: 資料驅動調校 — all G.1 shake knobs from `GameFeelConfig` SO; all per-event magnitudes looked up from config (not hardcoded)

**ADR Decision Summary**: `ShakeSystem` subscribes to typed event structs (`PartBroke`, `BossCoreBreak`, `PartSoftened`, `PartStaggered`, etc.) via `IEventBus`; no direct system references. Per-event shake magnitudes are data-driven from `GameFeelConfig`. The max-not-additive model and linear decay run on `Time.unscaledDeltaTime` (FX clock) so shake is unaffected by slow-mo and hitstop.

**Engine**: Unity 6.3 LTS | **Risk**: MEDIUM
**Engine Notes**:
- Shake offset applied to the game camera (e.g., `Camera.main` position offset or a camera shake component). Exact camera API usage to be determined by the engine programmer; do not assume API signature.
- `Random.Range(-1f, 1f)` in MonoBehaviour context. For deterministic unit tests, inject an `IRandom` abstraction that can be seeded.
- Shake runs on `Time.unscaledDeltaTime` — continues at normal rate during hitstop (`timeScale = 0`) and slow-mo (`timeScale = 0.12`), so camera motion stays visually crisp during the frozen/slowed moment.
- Integer-pixel offsets (floor the float) per GDD D.1 to avoid sub-pixel jitter.
- [需查證 6.3 API]: verify that camera position offset is stable in Unity 6.3 URP 2D (no post-process interaction with camera offset that could cause tearing).

**Control Manifest Rules (Presentation layer — §3 GameFeel)**:
- Required: Subscribe via `IEventBus.Subscribe<T>` for each relevant event
- Required: Decay and offset calculation on `Time.unscaledDeltaTime`
- Required: `shake_magnitude_cap = 24` enforced as hard ceiling — readability guardrail
- Required: All magnitude values from `GameFeelConfig` (per-event lookup)
- Forbidden: MUST NOT change game/part state
- Forbidden: Camera offset MUST NOT make bullets invisible — readability guardrail non-negotiable

---

## Acceptance Criteria

*From GDD `design/gdd/game-feel.md §I.4, §D.1, §C.6`, scoped to this story:*

- [ ] Any single event's effective shake magnitude does not exceed `shake_magnitude_cap` (24px): `current_shake = min(max(current_shake, event_magnitude), shake_magnitude_cap)`
- [ ] Consecutive events (e.g., M3 torpedo spam): `current_shake = max(current_shake, event_magnitude)` — NOT additive; magnitude never accumulates beyond cap
- [ ] Shake decays linearly each frame: `current_shake = max(0, current_shake - shake_decay_rate × unscaledDeltaTime)`
- [ ] Camera offset each frame: `offset_x = floor(Random(-1,1) × current_shake)`, `offset_y = floor(Random(-1,1) × current_shake)` (integer pixels)
- [ ] When `current_shake < shake_threshold` (0.3px), both offsets are zeroed (no micro-jitter)
- [ ] Shake direction is random each frame (not fixed-axis)
- [ ] Shake calculation runs on `Time.unscaledDeltaTime` — unaffected by slow-mo and hitstop
- [ ] At Nightmare difficulty + Boss death (magnitude 24): bullets remain in screen-visible range (QA manual verification — advisory)
- [ ] Automated test passes: `Assets/_Project/Tests/GameFeel/GameFeel_Shake_Test.cs`

---

## Implementation Notes

*Derived from ADR-0002 Decision (event bus), ADR-0003 Decision (SO config), GDD D.1 formula:*

1. Create `ShakeSystem` in `KaijuBreaker.GameFeel` assembly. Constructor receives `IEventBus bus`, `GameFeelConfig config`, `IRandom rng`.
2. Subscribe to all shake-triggering events and map to magnitude from `config`:
   - `PartSoftened` → `config.ShakeMagSoften` (3px)
   - `PartStaggered` where `armor_stripped=true` → `config.ShakeMagArmorStrip` (5px)
   - `L3WaveHit` → `config.ShakeMagL3Shockwave` (14px)
   - `MissileHit` with torpedo type → `config.ShakeMagM3TorpedoHit` (9px)
   - `HeatShockDetonation` → `config.ShakeMagM3HeatShock` (8px, max with torpedo)
   - `ClusterDetonation` → `config.ShakeMagM4Cluster` (7px)
   - `PartBroke` → `config.ShakeMagPartBreakBase + config.ShakeMagPartBreakEscalation × brokenPartCount` (capped)
   - `BossCoreBreak` → `config.ShakeMagBossDeath` (24px)
3. `TriggerShake(float magnitude)` internal method: `_currentShake = Mathf.Min(Mathf.Max(_currentShake, magnitude * config.ShakeAccessibilityMult), config.ShakeMagnitudeCap)`.
4. Per-frame update (unscaled):
   - `_currentShake = Mathf.Max(0f, _currentShake - config.ShakeDecayRate * Time.unscaledDeltaTime)`
   - If `_currentShake >= config.ShakeThreshold`: `offset = (floor(rng.Range(-1f,1f) * _currentShake), floor(rng.Range(-1f,1f) * _currentShake))` — apply to camera.
   - Else: `offset = (0, 0)`.
5. `brokenPartCount` is a state value tracked by `ShakeSystem` or passed from the event payload. Clamp the raw part-break magnitude to `ShakeMagnitudeCap` before the max operation.
6. `IRandom` interface: `float Range(float min, float max)` — inject a seeded fake for deterministic tests.
7. Accessibility multiplier (`ShakeAccessibilityMult`) is read at trigger time — Story 007 sets the field; this system reads it every call.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: `GameFeelConfig` SO with all G.1 shake knob definitions
- Story 006: Break payoff sequence (calls `ShakeSystem.TriggerShake()` as one step of D.4)
- Story 007: `ShakeAccessibilityMult = 0.25` under reduce-motion (multiplier already read here; Story 007 sets the value)
- Camera architecture decision (which camera component receives offset) → engine programmer to determine; this story only produces the offset values

---

## QA Test Cases

*Written by qa-lead at story creation. Automated — Logic story.*

- **AC-1**: Cap enforced on incoming magnitude
  - Given: `config.ShakeMagnitudeCap = 24`, `_currentShake = 0`, `ShakeAccessibilityMult = 1.0`
  - When: `TriggerShake(30f)` called
  - Then: `_currentShake == 24f` (capped at cap, not 30)
  - Edge cases: `magnitude = 0` → `_currentShake` unchanged; `magnitude = 24` (exact cap) → `_currentShake = 24`

- **AC-2**: Consecutive events use max, not sum
  - Given: `_currentShake = 14f` (from prior event)
  - When: `TriggerShake(9f)` called (M3 torpedo)
  - Then: `_currentShake == 14f` (max(14, 9) = 14; not 23)
  - Edge cases: New magnitude (18) > current (14) → `_currentShake = 18`

- **AC-3**: Linear decay correctness
  - Given: `_currentShake = 24f`, `config.ShakeDecayRate = 42f`, `unscaledDt = 0.1f`
  - When: One frame passes
  - Then: `_currentShake == Mathf.Max(0f, 24f - 42f × 0.1f) = 19.8f`
  - Edge cases: Decay drives `_currentShake` below 0 → clamped to 0; single frame large `unscaledDt` spike

- **AC-4**: Threshold zeroes the offset
  - Given: `_currentShake = 0.2f`, `config.ShakeThreshold = 0.3f`
  - When: Computing offset for camera
  - Then: `offset_x == 0`, `offset_y == 0` (below threshold, no jitter)
  - Edge cases: `_currentShake = 0.3f` (exactly at threshold) → GDD says "低於此值 (strictly less than)" → at 0.3 apply offset; at 0.299 zero offset

- **AC-5**: Part break escalation capped at shake_magnitude_cap
  - Given: `config.ShakeMagPartBreakBase = 11`, `config.ShakeMagPartBreakEscalation = 0.7`, `brokenPartCount = 20`, `config.ShakeMagnitudeCap = 24`
  - When: `TriggerShake(11 + 0.7 * 20 = 25)` with cap
  - Then: `_currentShake == 24` (capped, not 25)
  - Edge cases: `brokenPartCount = 0` → magnitude = 11 (base only)

- **AC-6**: Shake on unscaled time (no slow-mo interference)
  - Given: `Time.timeScale = 0.12` (slow-mo active), `_currentShake = 24f`
  - When: Frame passes with `Time.unscaledDeltaTime = 0.016f` (normal 60fps)
  - Then: Decay applies at 42 × 0.016 = 0.672px (same as without slow-mo)
  - Edge cases: `Time.timeScale = 0` (hitstop) — decay still occurs on unscaled clock

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- Logic: `Assets/_Project/Tests/GameFeel/GameFeel_Shake_Test.cs` — must exist and pass (BLOCKING)
- Visual (advisory): `production/qa/evidence/shake-system-evidence.md` — Nightmare density boss death screenshot showing bullets in frame

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (GameFeelConfig SO — must be DONE)
- Unlocks: Story 006 (Break Payoff Sequence — shake trigger is one step of D.4)
