# Story 003: Secondary Weapon Ammo Pips & Reload Bar

> **Epic**: HUD / UI 系統
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: ~3–4h
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/hud-ui-system.md`
**Requirement**: `TR-ui-003`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002: Event Architecture (Primary); ADR-0006: UI Framework Selection (Secondary)
**ADR Decision Summary**: Ammo pip state and reload progress are driven by `IEventBus` weapon state events (ADR-0002 typed event bus); the display is UGUI `Image` components on `HUD_Dynamic` Canvas (ADR-0006 §2). `Image.enabled` for pip fill/empty state — no string allocation per frame; zero GC steady-state. M2 Tier-3 numeric display switches representation when `m2_t3_use_numeric_display` is true (UIConfig SO).

**Engine**: Unity 6.3 URP 2D | **Risk**: MEDIUM
**Engine Notes**: `Image.enabled` toggle and `Image.fillAmount` on `HUD_Dynamic` Canvas trigger Canvas dirty each frame during reload — this is expected and within the two-Canvas split budget (ADR-0006). [需查證 #1: Canvas.pixelPerfect + URP Screen Space Camera behaviour; spike result in `docs/architecture/tech-spikes/` before completing Story 002.] `TMP_Text.SetText(int)` avoids boxing — use this overload for numeric M2-T3 display.

**Control Manifest Rules (Presentation layer)**:
- Required: Subscribe weapon-state events in `OnEnable`/`OnDisable`; pip state driven by events, not per-frame query; object-pool floating-text elements (zero `Instantiate` per ammo change); config from `HudConfig` ScriptableObject
- Forbidden: Direct reference to `Weapons` assembly; hardcoded pip counts (read from `WeaponDef`); per-frame GC allocation
- Guardrail: Pip `Image` toggle (`enabled`) changes — verify these stay in `HUD_Dynamic` Canvas only, never in `HUD_Static`; bullet-readability not obstructed — D-area panel does not extend into play center

---

## Acceptance Criteria

*From GDD `design/gdd/hud-ui-system.md` §D.2, J.2, J.3, M.3:*

- [ ] On secondary weapon fire: the consumed pip switches from filled (solid) to empty (hollow) on the **same frame** as the `WeaponFired(Secondary)` event (no frame delay)
- [ ] When all pips empty: reload bar appears immediately and fills from 0 to 1 over the weapon's reload time
- [ ] When reload completes: all pips restore to filled; reload bar disappears
- [ ] On picking up a same-type weapon capsule (`WeaponPickedUp`): pips reset to full `maxAmmo`; any in-progress reload is cancelled (bar hidden, progress cleared)
- [ ] On weapon swap (different weapon equipped): pips reset to new weapon's `maxAmmo`; reload state cleared; bar hidden
- [ ] Ammo ≤ `ammo_pip_low_threshold` (default 1): last remaining pip(s) blink at `ammo_pip_blink_hz` (1.0 Hz) — blink uses shape (enabled/disabled toggle), not colour change (colorblind-safe per GDD §I.2)
- [ ] M2 Tier-3 (12-ammo): when `m2_t3_use_numeric_display` == true, pip icons are hidden and a `TMP_Text` displays "N/12" — switches on equip; reverts if weapon changes to non-T3
- [ ] Automated test covers the ammo state machine for all 4 secondary weapons (M1–M4): fire → deplete → reload → complete → pickup-replace cycle (M.3 automated test requirement)

---

## Implementation Notes

*Derived from ADR-0006 §2 (HUD_Dynamic Canvas) and ADR-0002 (event-driven UI update):*

`SecondaryAmmoDisplay : MonoBehaviour` on `HUD_Dynamic` Canvas, inside the `SecondaryWeaponSlot` node created by Story 002.

**State model** (internal, not game state):
```
_currentPipCount  : int   (tracks displayed fill count)
_maxPips          : int   (set on WeaponEquipped)
_reloading        : bool
_reloadProgress   : float
```

**Event subscriptions** (OnEnable/OnDisable via `IEventBus`):
```
Subscribe<WeaponFired>     filter slot==Secondary → DecrementPip()
Subscribe<WeaponReloadStarted>  filter slot==Secondary → ShowReloadBar()
Subscribe<WeaponReloadProgress> filter slot==Secondary → UpdateReloadBar(progress)
Subscribe<WeaponReloadCompleted> filter slot==Secondary → RefillAllPips()
Subscribe<WeaponPickedUp>        filter slot==Secondary → ResetAmmo(newWeaponDef)
Subscribe<WeaponEquipped>        filter slot==Secondary → RebuildPipLayout(weaponDef)
```

`RebuildPipLayout`: destroys/creates `Image` pip instances from pool; sets `_maxPips` from `WeaponDef.MagazineSize`; for M2 Tier-3 checks `weaponDef.UseNumericDisplay` flag (from `UIConfig.M2T3UseNumericDisplay` SO). Reuse pooled `Image` instances — no `Instantiate`/`Destroy` per equip.

**Low-ammo blink**: start `Coroutine` when `_currentPipCount <= ammo_pip_low_threshold`; toggle last pip's `Image.enabled` every `1f / ammo_pip_blink_hz` seconds; stop on pip count increase or weapon swap.

**M2 T3 numeric**: `TMP_Text.SetText("{0}/{1}", _currentPipCount, 12)` — use indexed overload to avoid string allocation; hide pip `Image` array, show `TMP_Text` object.

**Reload bar**: single `Image` with `fillAmount`; driven by `WeaponReloadProgress` event (`float progress` in payload); hide via `gameObject.SetActive(false)` when not reloading.

Config: `HudConfig.AmmoPipBlinkHz`, `HudConfig.AmmoPipLowThreshold`, `HudConfig.M2T3UseNumericDisplay` — all from `HudConfig` ScriptableObject.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 002**: Secondary weapon slot icon, name, and tier badge display (parent node)
- **Story 004**: L3 charge bar (different slot, different story)
- M2 Tier-3 pip layout change triggers a visual explanation text — that one-time UI tooltip is P4 Contextual and out of scope for MVP (GDD §J.2: "UI 說明文字提示玩家顯示模式變更" — deferred)

---

## QA Test Cases

*Written by qa-lead at story creation.*

- **AC-1**: Fire depletes pip on same frame
  - Given: M1 equipped (6 pips); `SecondaryAmmoDisplay` subscribed; all pips filled
  - When: `IEventBus.Publish(new WeaponFired { Slot = Secondary })`
  - Then: `_currentPipCount` == 5; pip[5] `Image.enabled` == false; pips[0–4] `Image.enabled` == true
  - Edge cases: Fire when _currentPipCount==0 → no underflow; state stays at 0; reload starts

- **AC-2**: Full depletion triggers reload bar
  - Given: M1 equipped; `_currentPipCount` == 1
  - When: `IEventBus.Publish(new WeaponFired { Slot = Secondary })`
  - Then: `_currentPipCount` == 0; `_reloading` == true; reload bar `gameObject.activeSelf` == true; `fillAmount` == 0.0f
  - Edge cases: Reload bar hidden before last shot → appears same frame as depletion

- **AC-3**: Reload progress updates fill
  - Given: M1 in reload state
  - When: `IEventBus.Publish(new WeaponReloadProgress { Slot = Secondary, Progress = 0.5f })`
  - Then: Reload bar `fillAmount` == 0.5f
  - Edge cases: Progress = 0.0f → bar visible at empty; Progress = 1.0f clamps bar (completion handled by separate event)

- **AC-4**: Reload completes — pips restore, bar hidden
  - Given: M1 in reload state; `_maxPips` == 6
  - When: `IEventBus.Publish(new WeaponReloadCompleted { Slot = Secondary })`
  - Then: All 6 pip `Image.enabled` == true; `_currentPipCount` == 6; reload bar `gameObject.activeSelf` == false
  - Edge cases: Completed event arrives with no reload in progress → idempotent

- **AC-5**: Pickup resets ammo and cancels reload
  - Given: M1; `_currentPipCount` == 2; `_reloading` == true; reload bar visible
  - When: `IEventBus.Publish(new WeaponPickedUp { Slot = Secondary, WeaponDef = m1Def })`
  - Then: `_currentPipCount` == 6 (full M1 magazine); `_reloading` == false; reload bar hidden
  - Edge cases: Pick up different weapon type during reload → `RebuildPipLayout` called for new weapon

- **AC-6**: M2 Tier-3 switches to numeric display
  - Given: M2 Tier-3 equipped; `HudConfig.M2T3UseNumericDisplay` == true
  - When: `IEventBus.Publish(new WeaponEquipped { Slot = Secondary, WeaponDef = m2T3Def })`
  - Then: All pip `Image` objects hidden; `TMP_Text` shows "12/12"; after 1 fire shows "11/12"
  - Edge cases: Switch to M1 → numeric text hidden; pip layout rebuilt for M1 (6 pips shown)

- **AC-7**: Low-ammo blink on M3 (3-pip)
  - Given: M3 equipped (`ammo_pip_low_threshold`=1); `_currentPipCount` == 1
  - When: Blink coroutine running; advance time by `1f/ammo_pip_blink_hz` (1.0s)
  - Then: Pip[0] `Image.enabled` toggles false→true each second
  - Edge cases: Ammo restored to 2 → blink stops; pip stable enabled

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Integration (BLOCKING): `Assets/_Project/Tests/UI/secondary_ammo_display_test.cs` — must exist and pass; covers M1–M4 ammo state machines per M.3

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002 DONE (`SecondaryWeaponSlot` node on `HUD_Dynamic` Canvas created); `WeaponFired`, `WeaponReloadStarted`, `WeaponReloadProgress`, `WeaponReloadCompleted`, `WeaponPickedUp`, `WeaponEquipped` event structs in Core; `WeaponDef` ScriptableObject with `MagazineSize`, `UseNumericDisplay` fields
- Unlocks: Story 010 (safe-area verification can include ammo pip area); Story 011 (accessibility colorblind check references pip blink as non-colour cue)
