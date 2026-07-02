# Story 001: World-Space Part HEAT/BREAK Bars (PartBarController)

> **Epic**: HUD / UI 系統
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: ~3–4h
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/hud-ui-system.md`
**Requirement**: `TR-ui-001`, `TR-ui-002`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0006: UI Framework Selection (Primary); ADR-0002: Event Architecture (Secondary)
**ADR Decision Summary**: World-space HEAT/BREAK bars use SpriteRenderer + MaterialPropertyBlock as child objects on each part Prefab — no Canvas enters the bullet play area (ADR-0006 §1). `PartBarController` subscribes to `IEventBus` events and queries `IPartStateQuery` (injected); zero direct cross-system references (ADR-0002).

**Engine**: Unity 6.3 URP 2D | **Risk**: MEDIUM
**Engine Notes**: `MaterialPropertyBlock` is GPU-instancing safe — does not break URP 2D Sprite Batcher batching. Sorting Layer `PartsUI` must be defined in Unity Project Settings (above `Bullets`, below `GameFeel`) and confirmed with art director before first playtest. [需查證: URP 2D Sprite Batcher batch-merge behaviour with per-instance MaterialPropertyBlock under Unity 6.3; see `docs/engine-reference/unity/` for current confirmed API.]

**Control Manifest Rules (Presentation layer)**:
- Required: Subscribe `IEventBus` events in `OnEnable`; unsubscribe in `OnDisable`; read part state via injected `IPartStateQuery`; all knob values from `HudConfig` ScriptableObject (`Assets/_Project/Content/UI/HudConfig.asset`)
- Forbidden: Canvas in the bullet play area; direct reference to `KaijuParts` assembly; hardcoded gameplay values; modifying game state
- Guardrail: ≤2 additional draw calls in combat heavy scenario (16 SpriteRenderers on shared atlas); zero GC steady-state; `MaterialPropertyBlock` set per-instance — never on the shared `Material` directly; bullet-readability not obstructed — bars must not overlap bullet sprites

---

## Acceptance Criteria

*From GDD `design/gdd/hud-ui-system.md` §E.1–E.3, M.1, M.2:*

- [ ] HEAT bar (above part Sprite, offset `part_bar_offset_top_px` = 2px) fills left→right proportional to H_current/H_max on each relevant event update
- [ ] BREAK bar (below part Sprite, offset `part_bar_offset_bottom_px` = 2px) fills left→right proportional to B_current/B_max on each relevant event update
- [ ] Bar width equals parent part Sprite width: `PartBarController.ResizeBars()` reads `SpriteRenderer.bounds.size.x` → sets `localScale.x`; height = `part_bar_height_px` (3px)
- [ ] On `PartSoftened`: HEAT bar switches to full-orange pulse on the **same frame** as event receipt (no cross-frame delay); pulse at `softened_heat_bar_pulse_hz`; BREAK bar unaffected
- [ ] `softened_heat_bar_pulse_hz` in `HudConfig` exactly matches `kaiju-part-system.md` G.3 `softened_pulse_frequency_hz` in `KaijuPartConfig` — verified by automated config test
- [ ] On `PartSoftenedExit`: HEAT bar stops pulse same frame; reverts to fill-proportion display
- [ ] ARMOR_INTACT state: `ArmorMask` SpriteRenderer enabled, alpha = `armor_mask_opacity` (0.65); BreakBar `_FillAmount` frozen at 0
- [ ] ARMOR_STRIPPED + STAGGERED: `ArmorMask` disabled; `WeakFrame` SpriteRenderer enabled; WeakFrame alpha decays linearly 1.0→0.0 over `stagger_duration` at rate `stagger_weak_frame_decay_rate`
- [ ] On `PartBroke`: all four child SpriteRenderers (HeatBar, BreakBar, ArmorMask, WeakFrame) disabled on the same frame as event
- [ ] SOFTENED orange on-part glow (#FF6600) is **not reproduced** here — game-feel.md owns that; this story owns bar fill, position, and state only (division enforced)
- [ ] World-space bars remain anchored to part Sprite bounds during Boss high-speed movement — no visual drift (M.1 target)

---

## Implementation Notes

*Derived from ADR-0006 §1 — SpriteRenderer Implementation Detail:*

Each part Prefab contains a `PartBarController : MonoBehaviour` managing four child `SpriteRenderer` objects:

| Child SR | Shader property | Data source |
|---|---|---|
| `HeatBar` | `_FillAmount` = H_current/H_max | `IPartStateQuery.H_current`, `.H_max` |
| `BreakBar` | `_FillAmount` = B_current/B_max | `IPartStateQuery.B_current`, `.B_max` |
| `ArmorMask` | `material.color.a` = `armor_mask_opacity` | `IPartStateQuery.armor_state` |
| `WeakFrame` | `color.a` decays linearly | `stagger_timer` countdown |

All four share `BarFill.shader` (or similar fill shader). Use `MaterialPropertyBlock` per instance — never `renderer.material` (which clones the material).

`ResizeBars()`: call on `Awake()` and whenever the part's Sprite changes; reads `GetComponentInParent<SpriteRenderer>().bounds.size.x`.

**Sorting Layer**: `PartsUI` — art director must confirm the layer exists in Project Settings above `Bullets` and below `GameFeel` before first playtest.

**Event subscriptions** (wire in `OnEnable`, unwire in `OnDisable`):
```
_bus.Subscribe<PartSoftened>(OnPartSoftened);
_bus.Subscribe<PartSoftenedExit>(OnPartSoftenedExit);
_bus.Subscribe<PartStaggered>(OnPartStaggered);
_bus.Subscribe<PartStaggerEnd>(OnPartStaggerEnd);
_bus.Subscribe<PartBroke>(OnPartBroke);
_bus.Subscribe<ArmorStripped>(OnArmorStripped);
```

Filter each handler by `evt.PartId == _partId` — do not update other parts' bars.

**SOFTENED pulse**: drive via `_PulseHz` shader property on `HeatBar`'s `MaterialPropertyBlock`. Read `HudConfig.SoftenedHeatBarPulseHz`; do not hardcode 2.0.

**Config SO**: `Assets/_Project/Content/UI/HudConfig.asset` — fields `PartBarHeightPx`, `PartBarOffsetTopPx`, `PartBarOffsetBottomPx`, `ArmorMaskOpacity`, `SoftenedHeatBarPulseHz`, `StaggerWeakFrameDecayRate`. All with `OnValidate` range checks per GDD L.1.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 002**: In-combat UGUI canvas setup (HUD_Static / HUD_Dynamic / Hitbox_Overlay); weapon slot displays
- **game-feel system**: SOFTENED orange on-part glow (#FF6600), sfxSoften, STAGGERED white flash + cool-blue spark particles — game-feel owns all of those
- **Story 005**: Boss HP bar; material counter
- **Story 010**: Safe-area fitting and integer-scaling for mobile portrait layout

---

## QA Test Cases

*Written by qa-lead at story creation.*

- **AC-1**: HEAT bar fill proportion
  - Given: `PartBarController` initialized; `IPartStateQuery` stub returns H_current=50, H_max=100
  - When: `UpdateBars()` called
  - Then: `HeatBar` `MaterialPropertyBlock._FillAmount` == 0.5f (±0.001f)
  - Edge cases: H_current=0 → 0.0f; H_current=H_max → 1.0f; H_max=0 → guard divide-by-zero, return 0.0f

- **AC-2**: SOFTENED switches same frame
  - Given: Part in INTACT state; `PartBarController` subscribed
  - When: `IEventBus.Publish(new PartSoftened { PartId = _partId })`
  - Then: Same frame — HeatBar `_FillAmount` == 1.0f; pulse shader enabled; BreakBar unchanged
  - Edge cases: `PartSoftenedExit` published → fill reverts to H_current/H_max ratio; pulse disabled same frame

- **AC-3**: Config pulse-hz sync (automated settings read test)
  - Given: `HudConfig.asset` and `KaijuPartConfig.asset` loaded from `Assets/_Project/Content/`
  - When: Test reads `HudConfig.SoftenedHeatBarPulseHz` and `KaijuPartConfig.SoftenedPulseFrequencyHz`
  - Then: `Assert.AreEqual(hudConfig.SoftenedHeatBarPulseHz, partConfig.SoftenedPulseFrequencyHz)`
  - Edge cases: Either SO null → test fails with message identifying missing asset path

- **AC-4**: ARMOR_INTACT mask enabled, BreakBar frozen
  - Given: `armor_state` = ARMOR_INTACT; `PartBarController` initialized
  - When: `UpdateArmorState(ARMOR_INTACT)` called
  - Then: `ArmorMask.enabled` == true; ArmorMask alpha == `armorMaskOpacity` (0.65f ±0.01f); BreakBar `_FillAmount` == 0.0f
  - Edge cases: `ArmorStripped` event → `ArmorMask.enabled` == false; BreakBar resumes normal fill

- **AC-5**: STAGGERED WeakFrame alpha decays linearly
  - Given: ArmorStripped + PartStaggered published; stagger_duration=3.0s; decay_rate=1.0
  - When: `UpdateStaggerDecay(elapsed=1.5f)` called
  - Then: `WeakFrame` alpha == 0.5f; `WeakFrame.enabled` == true
  - Edge cases: elapsed >= stagger_duration → alpha == 0.0f; `WeakFrame.enabled` == false

- **AC-6**: PartBroke disables all child renderers
  - Given: All four child SRs enabled
  - When: `IEventBus.Publish(new PartBroke { PartId = _partId })`
  - Then: HeatBar.enabled == BreakBar.enabled == ArmorMask.enabled == WeakFrame.enabled == false
  - Edge cases: Duplicate PartBroke events → idempotent, no NullReference

- **Manual check AC-7**: No bar drift during Boss high-speed movement
  - Setup: Enter Boss fight; trigger high-speed movement phase; observe 3+ parts simultaneously
  - Verify: HEAT and BREAK bars remain anchored to respective part Sprite bounds throughout all movement frames
  - Pass condition: Zero visible bar drift in a 30-second movement session; confirmed in screenshot at frame of maximum Boss velocity

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Integration (BLOCKING): `Assets/_Project/Tests/UI/world_space_part_bars_test.cs` — must exist and pass
- Visual (ADVISORY): `production/qa/evidence/world-space-part-bars-evidence.md` — screenshot of bars during Boss movement; lead sign-off

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `IPartStateQuery` interface defined in `KaijuBreaker.Core` (core-foundation story must be DONE); `IEventBus` with `PartSoftened`, `PartSoftenedExit`, `PartStaggered`, `PartStaggerEnd`, `PartBroke`, `ArmorStripped` event structs registered in Core; game-feel team confirms `on_part_softened` event consumption has no race condition with this story's subscription
- Unlocks: Story 002 (can begin in parallel — different rendering layer); Story 010 (safe-area story can reference bar visibility on mobile)
