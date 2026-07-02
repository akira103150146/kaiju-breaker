# Story 007: Loadout Screen

> **Epic**: HUD / UI 系統
> **Status**: Ready
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: ~3–4h
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/hud-ui-system.md`
**Requirement**: `TR-ui-010` (Loadout 畫面；tr-registry.yaml 已正式化) — entry point to the screen-flow loop and to the M.5/M.6/M.7/M.8 screens.

**ADR Governing Implementation**: ADR-0006: UI Framework Selection (Primary); ADR-0002: Event Architecture (Secondary)
**ADR Governing Implementation (detail)**: Loadout Screen is a UGUI Screen Space – Overlay Canvas Prefab implementing `IScreen` (ADR-0006 §3). Managed by `UIScreenManager` (Story 006). Inventory display reads `ISaveService.GetInventory()` (ADR-0002 query interface). Weapon tier display reads `IWeaponTierQuery`. No direct reference to `Economy` or `Meta` assemblies.

**Engine**: Unity 6.3 URP 2D | **Risk**: MEDIUM
**Engine Notes**: Weapon card grid (4 primary + 4 secondary) uses UGUI `GridLayoutGroup`. Gamepad navigation: set `Navigation.mode = Explicit` on each weapon card `Button` to define D-Pad routing between the two grids. [需查證 #2 in ADR-0006: confirm UGUI Selectable Explicit navigation routes correctly in Unity 6.3 between two separate GridLayoutGroups.] MVP locked slots display "更多武器開發中" — no dedicated locked-slot Prefab needed in MVP; use a disabled-state variant of the WeaponCard Prefab.

**Control Manifest Rules (Presentation layer)**:
- Required: Implement `IScreen` (OnShow refreshes all data from ISaveService + IWeaponTierQuery); DI inject `ISaveService` and `IWeaponTierQuery`; weapon card selection state tracked locally (not written to SO or save until run confirmed); `Navigation.mode = Explicit` for gamepad routing
- Forbidden: Direct reference to `Economy`, `Meta`, or `Weapons` assemblies; writing selection to save until player confirms "確認出發"; singletons
- Guardrail: `OnShow()` must read fresh inventory and tier data on every open; weapon name TMP ≤4 Chinese characters per GDD §D.1 constraint; locked slot shows lock icon, not empty

---

## Acceptance Criteria

*From GDD `design/gdd/hud-ui-system.md` §F.1, G, J.6, M.7, M.8:*

- [ ] On `OnShow()`: inventory bar displays current shard count, core counts (甲殼/四肢/能量), and essence count read from `ISaveService.GetInventory()`; values refresh on every open
- [ ] Weapon card grid: 4 primary (L1–L4) and 4 secondary (M1–M4) weapon cards displayed; each card shows weapon icon (16×16 px Image), Chinese name, Tier badge (T0–T3), and 1-line flavor text (≤8 characters)
- [ ] Selected weapon card shows highlight border + ✓ indicator; exactly one primary and one secondary must be selected at all times (no empty selection allowed)
- [ ] Tap/click on unselected card: previous card deselects; new card selects; matchup suggestion panel updates on right (max 3 lines)
- [ ] "升級武器" button: pushes Upgrade Screen via `UIScreenManager.Push`; player can skip (return to Loadout without upgrade)
- [ ] "確認出發 →" button: writes confirmed weapon selection and triggers screen flow to Difficulty Select (`UIScreenManager.Push(DifficultyScreen)`)
- [ ] MVP locked weapon slots (weapons not in `EconomyConfig.EnabledWeaponTiers`): show lock icon + "更多武器開發中" text; card is non-interactive (`Button.interactable = false`)
- [ ] Mobile portrait layout: weapon cards use 2×2 grid layout (not 4-column) when `Screen.height > Screen.width` (portrait detection)
- [ ] Gamepad D-Pad navigates between all interactable weapon cards without getting stuck; explicit navigation routing configured

---

## Implementation Notes

*Derived from ADR-0006 §3 and ADR-0002 query interfaces:*

`LoadoutScreen : MonoBehaviour, IScreen` — Canvas Prefab, Screen Space – Overlay.

**`OnShow()`**:
1. `ISaveService.GetInventory()` → populate inventory bar TMP texts
2. `IWeaponTierQuery.GetTiers()` → update each `WeaponCard` tier badge
3. Restore previous selection state (last confirmed selection from `ISaveService.GetLastLoadout()`) if available

**`WeaponCard : MonoBehaviour`** (Prefab × 8):
- Fields: `Image icon`, `TMP_Text name`, `Image tierBadge`, `TMP_Text flavorText`, `GameObject selectedIndicator`, `Button button`
- `Button.onClick`: notify `LoadoutScreen.OnCardSelected(this, slot)`
- `Button.interactable = false` for locked slots (read `EconomyConfig.EnabledWeaponTiers` to determine lock)

**Selection state**: `_selectedPrimary : WeaponCard`, `_selectedSecondary : WeaponCard`. Validate non-null before enabling "確認出發" button.

**Gamepad navigation**: after all cards are initialized, call `BuildExplicitNavigation()` — iterate the 4-card primary grid and 4-card secondary grid, set `Button.navigation.selectOnRight/Left/Up/Down` explicitly. "升級武器" and "確認出發" buttons are reachable via D-Pad down from the card grids.

**Mobile portrait layout**: check `Screen.height > Screen.width` in `OnShow()`; switch `primaryGridLayout.constraintCount` between 1 (portrait, 2×2 within each half) and 2 (landscape, 4×1). Or use `ScreenOrientation` changed event.

**Sprite references**: weapon icons from `HUD_Atlas`; tier badge sprites from `HudConfig` SO (T0/T1/T2/T3 badge sprites).

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 006**: `UIScreenManager` stack itself — must be DONE before this story
- **Story 008**: Upgrade screen content and cost display (pushed from this screen's "升級武器" button)
- **Story 009**: Difficulty select screen (pushed from this screen's "確認出發" button)
- **Story 010**: `SafeAreaFitter` application to this screen's root `RectTransform`
- **Story 011**: Text scale (150%) and colorblind mode applied to this screen's text elements

---

## QA Test Cases

*Written by qa-lead at story creation.*

- **Manual check AC-1**: Inventory bar shows ISaveService values
  - Setup: Set ISaveService to shard=72, 甲殼=5, 四肢=3, 能量=0, 精魄=2 in test setup; open Loadout screen
  - Verify: Inventory bar text shows exactly those values
  - Pass condition: All 5 inventory values match; values refresh when screen reopened after a simulated hunt (new values from ISaveService)

- **Manual check AC-2**: Weapon card grid displays all 8 cards correctly
  - Setup: Full weapon roster configured; all T0; open Loadout screen
  - Verify: 8 cards visible (4 primary, 4 secondary); each shows correct icon, name, tier badge T0, flavor text; one primary and one secondary pre-selected with highlight + ✓
  - Pass condition: All 8 cards readable; no missing sprites; selection indicators visible

- **Manual check AC-3**: Card selection updates UI
  - Setup: L1 selected as primary; click L2 card
  - Verify: L1 card loses highlight and ✓; L2 card gains highlight and ✓; matchup panel text updates to L2 context
  - Pass condition: Only one card highlighted per slot at any time; no transient double-highlight

- **Manual check AC-4**: MVP locked slots non-interactive
  - Setup: MVP config (`EnabledWeaponTiers` contains only L1+M2); open Loadout screen
  - Verify: L2/L3/L4/M1/M3/M4 cards show lock icon + "更多武器開發中"; `Button.interactable` == false for each
  - Pass condition: Locked cards do not respond to click/tap/D-Pad selection; no crash

- **Manual check AC-5**: Gamepad D-Pad traverses all interactable cards
  - Setup: Connect gamepad; open Loadout screen; start from first primary card
  - Verify: D-Pad navigates right across primary row; down to secondary row; up back to primary; Right/left wraps within row
  - Pass condition: No dead-ends (D-Pad never gets stuck); navigation arrives at "升級武器" and "確認出發" from the card grids

---

## Test Evidence

**Story Type**: UI
**Required evidence**:
- UI (ADVISORY): `production/qa/evidence/loadout-screen-evidence.md` — screenshots of inventory bar, card grid (all 8 cards), card selection state, MVP locked slots, gamepad navigation walkthrough notes

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 006 DONE (`UIScreenManager` and `IScreen` available); `ISaveService` with `GetInventory()`, `GetLastLoadout()` in Core; `IWeaponTierQuery` in Core; `WeaponDef` SOs for all 8 weapons; `EconomyConfig.EnabledWeaponTiers` field; `HUD_Atlas` with weapon icons and tier badge sprites
- Unlocks: Story 008 (Upgrade Screen pushed from this story's "升級武器" button); Story 009 (Difficulty Screen pushed from "確認出發"); Story 010 (safe-area fitting applies to this screen's Canvas root); Story 011 (text scale and colorblind applied here)
