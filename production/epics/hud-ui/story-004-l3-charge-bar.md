# Story 004: L3 Charge Bar

> **Epic**: HUD / UI 系統
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: ~2–3h
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/hud-ui-system.md`
**Requirement**: `TR-ui-004`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002: Event Architecture (Primary); ADR-0006: UI Framework Selection (Secondary)
**ADR Decision Summary**: L3 charge state is driven by `IEventBus` weapon state events (ADR-0002). The charge bar is a UGUI `Image.fillAmount` on `HUD_Dynamic` Canvas, visible only when L3 (Wave Cannon) is equipped (ADR-0006 §2). All timing config read from `WeaponDef` (L3 charge time, cooldown) via `IWeaponTierQuery`; visual knobs from `HudConfig` ScriptableObject.

**Engine**: Unity 6.3 URP 2D | **Risk**: MEDIUM
**Engine Notes**: `Image.fillAmount` update each frame during charge triggers `HUD_Dynamic` Canvas dirty — expected, within budget. Flash at ≥90% threshold: use `Animator` or `Coroutine` — avoid `DOTween` unless project has confirmed it in `Allowed Libraries` in `technical-preferences.md`. [需查證 #1 in ADR-0006: Canvas.pixelPerfect + PixelPerfectCamera in URP Screen Space Camera mode.]

**Control Manifest Rules (Presentation layer)**:
- Required: Subscribe `WeaponEquipped`, `L3ChargeStarted`, `L3ChargeUpdated`, `L3WaveFired` events in `OnEnable`/`OnDisable`; show/hide by setting `gameObject.SetActive(false)` (not `Canvas.enabled`) so layout collapses; config from `HudConfig` and `WeaponDef`
- Forbidden: Hardcoding `l3_charge_time = 1.5f`; reading charge progress by querying `Weapons` assembly directly; modifying `HUD_Static` Canvas
- Guardrail: Charge bar occupies no layout space when inactive (`SetActive(false)`); zero GC per frame during charge

---

## Acceptance Criteria

*From GDD `design/gdd/hud-ui-system.md` §D.1, M.4:*

- [ ] When L3 (Wave Cannon) is equipped: charge bar `GameObject` is active and visible in C area below the weapon icon
- [ ] When L1/L2/L4 is equipped: charge bar `GameObject` is inactive (`SetActive(false)`) — occupies no layout space
- [ ] Hold trigger: bar `Image.fillAmount` fills from 0 to 1 in exactly `l3_charge_time` (1.5s) ± 1 frame tolerance
- [ ] Charge ≥ `l3_charge_bar_flash_pct` (90%): flash animation activates; charge < 90% → flash inactive
- [ ] On L3 wave release: cooldown mask `Image` appears; its `fillAmount` decreases from 1.0 → 0.0 over `l3_charge_cooldown` (2.0s); mask hidden when complete
- [ ] Weapon swap from L3 to non-L3: bar immediately hides (`SetActive(false)`); in-progress charge/cooldown state cleared
- [ ] Weapon swap to L3 from non-L3: bar appears at `fillAmount` = 0 (empty, ready state); no leftover state from previous weapon

---

## Implementation Notes

*Derived from ADR-0006 §2 and ADR-0002:*

`L3ChargeBar : MonoBehaviour` on `HUD_Dynamic` Canvas, child of `PrimaryWeaponSlot` node (created by Story 002).

**Child components**:
- `ChargeBar` (`Image`, `fillAmount` = 0→1 during charge)
- `FlashOverlay` (`Animator` or blinking `Coroutine` — activates at ≥90%)
- `CooldownMask` (`Image`, `fillAmount` = 1→0 during cooldown; `Image.type = Filled, fillMethod = Horizontal, fillOrigin = Right`)

**Event subscriptions** (OnEnable/OnDisable):
```
Subscribe<WeaponEquipped>     → OnWeaponEquipped(evt)
Subscribe<L3ChargeStarted>    → BeginCharge()
Subscribe<L3ChargeUpdated>    → UpdateCharge(evt.Progress)  // progress: 0..1
Subscribe<L3WaveFired>        → BeginCooldown()
```

`OnWeaponEquipped`: if `evt.Slot == Primary` → `gameObject.SetActive(evt.WeaponDef.IsL3)`. Clear charge and cooldown state on any weapon equip.

`UpdateCharge(float progress)`: `chargeBar.fillAmount = progress`; if `progress >= HudConfig.L3ChargeBarFlashPct` → enable flash.

`BeginCooldown()`: `StartCoroutine(CooldownRoutine())` — drives `cooldownMask.fillAmount` from 1.0 to 0.0 over `WeaponDef.L3ChargeCooldown` seconds using `Time.deltaTime`. On complete: hide mask, reset charge bar to 0.

Config: `HudConfig.L3ChargeBarFlashPct` (0.90); charge timing from `WeaponDef.L3ChargeTime`, `WeaponDef.L3ChargeCooldown` (read via `IWeaponTierQuery` DI-injected, not hardcoded).

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 002**: Primary weapon slot icon, name, tier badge (parent node structure)
- **Story 003**: Secondary weapon ammo pips and reload bar
- Mobile L3 touch input scheme (long-press secondary button) — this is an open question in `weapon-system.md` §I; UI implementation deferred to Prototype milestone confirmation

---

## QA Test Cases

*Written by qa-lead at story creation.*

- **AC-1**: Show only when L3 equipped
  - Given: `L3ChargeBar` subscribed; L1 currently equipped
  - When: `IEventBus.Publish(new WeaponEquipped { Slot = Primary, WeaponDef = l3Def })`
  - Then: `gameObject.activeSelf` == true; `chargeBar.fillAmount` == 0.0f
  - Edge cases: L3 → L2 swap → `gameObject.activeSelf` == false; charge state cleared

- **AC-2**: Fill timing matches l3_charge_time (1.5s ± 1 frame)
  - Given: L3 equipped; charge starts at Progress=0
  - When: `L3ChargeUpdated` events published at simulated 60 FPS (progress increments of 1/90 per frame over 1.5s)
  - Then: `chargeBar.fillAmount` reaches 1.0f after 90 events (1.5s × 60 FPS); tolerance ±1 event
  - Edge cases: Charge released at Progress=0.5f → fillAmount stays at 0.5f; no auto-complete

- **AC-3**: Flash activates at exactly 90% threshold
  - Given: L3 equipped; `HudConfig.L3ChargeBarFlashPct` == 0.90f
  - When: `L3ChargeUpdated { Progress = 0.899f }` published, then `{ Progress = 0.900f }`
  - Then: At 0.899f → flash disabled; at 0.900f → flash enabled
  - Edge cases: Progress drops back to 0.889f → flash disabled (e.g., interrupted charge)

- **AC-4**: Cooldown mask covers bar and drains over l3_charge_cooldown
  - Given: L3 wave fired; `WeaponDef.L3ChargeCooldown` == 2.0f
  - When: `L3WaveFired` published; advance simulated time by 1.0f
  - Then: `cooldownMask.fillAmount` ≈ 0.5f (half-drained at 1.0s); after 2.0f total → `cooldownMask.gameObject.activeSelf` == false
  - Edge cases: Weapon swap during cooldown → cooldown cancelled, bar hidden

- **AC-5**: Weapon swap clears all state
  - Given: L3 in charge (Progress=0.7f); flash active
  - When: `IEventBus.Publish(new WeaponEquipped { Slot = Primary, WeaponDef = l1Def })`
  - Then: `gameObject.activeSelf` == false; `chargeBar.fillAmount` == 0.0f; flash inactive; no cooldown running
  - Edge cases: Swap back to L3 → `gameObject.activeSelf` == true; fillAmount == 0.0f (fresh start)

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Integration (BLOCKING): `Assets/_Project/Tests/UI/l3_charge_bar_test.cs` — must exist and pass; covers show/hide, fill timing, flash threshold, cooldown drain

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002 DONE (`PrimaryWeaponSlot` node on `HUD_Dynamic` Canvas created); `WeaponEquipped`, `L3ChargeStarted`, `L3ChargeUpdated`, `L3WaveFired` event structs in Core; `WeaponDef` with `IsL3`, `L3ChargeTime`, `L3ChargeCooldown` fields; `IWeaponTierQuery` interface in Core
- Unlocks: Story 010 (can verify charge bar layout in safe-area pass); Story 011 (Reduce-Motion: charge bar is functional, exempt from Reduce-Motion per GDD §I.3)
