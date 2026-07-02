# Story 008: Permanent Upgrade Screen

> **Epic**: HUD / UI 系統
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: ~3–4h
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/hud-ui-system.md`
**Requirement**: `TR-ui-005`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002: Event Architecture (Primary); ADR-0006: UI Framework Selection (Secondary)
**ADR Decision Summary**: The upgrade screen reads upgrade costs and current inventory from `ISaveService` (ADR-0002 query interface, DI-injected). It subscribes to `MaterialCollected` events for real-time inventory sync without closing and reopening the screen. The screen is a UGUI Canvas Prefab implementing `IScreen` under `UIScreenManager` (ADR-0006 §3). Tier-3 blur preview uses a blur `Material` on the description `TMP_Text` or a separate Sprite overlay — implementation detail for `ui-programmer`. The hunt pointer reads `KaijuDef` SO to get kaiju name; no reference to kaiju-part system at runtime (data-only).

**Engine**: Unity 6.3 URP 2D | **Risk**: MEDIUM
**Engine Notes**: Blur effect for Tier-3 preview: UGUI does not have built-in blur; options include a custom URP RenderFeature (RenderTexture crop), a blurred Sprite overlay, or pixel-shader on `TMP_Text`'s material. `upgrade_tier3_blur_radius` (default 4px) from `HudConfig` SO. [需查證: preferred blur approach for text on URP 2D in Unity 6.3 without excessive draw call overhead — file spike result in `docs/architecture/tech-spikes/` before implementing blur.] `TMP_Text.SetText(int)` for cost numbers — no string allocation.

**Control Manifest Rules (Presentation layer)**:
- Required: DI inject `ISaveService`; subscribe `MaterialCollected` in `OnEnable`/`OnDisable`; call `OnFocus()` refresh on return from hunt; Tier-3 blur radius from `HudConfig` SO; hunt pointer kaiju name from `KaijuDef` SO (read by weapon matcher, no runtime query to kaiju system); `Button.interactable` driven by all-resources-sufficient bool
- Forbidden: Direct reference to `Economy`, `KaijuParts`, or `Meta` assemblies; hardcoded cost values (read from `WeaponDef` upgrade cost tables); caching ISaveService values beyond the current `OnShow()`/`OnFocus()` refresh cycle
- Guardrail: Upgrade button disabled (not hidden) when insufficient materials — player must see the full cost; M.5 is BLOCKING acceptance criterion

---

## Acceptance Criteria

*From GDD `design/gdd/hud-ui-system.md` §F.2, M.5:*

- [ ] When any required material is insufficient: that cost row displays red ✗ icon; upgrade button `Button.interactable = false` (disabled, NOT hidden)
- [ ] When all materials sufficient: all rows display green ✓; upgrade button enabled (blue)
- [ ] When weapon is already at Tier-3: display "已達最高等級"; upgrade button absent
- [ ] Real-time inventory sync: when `MaterialCollected` event fires while screen is open, cost rows update without closing/reopening screen
- [ ] On `OnFocus()` (returning from another screen): refresh all cost rows from `ISaveService.GetInventory()` — covers return-from-hunt case
- [ ] Tier-3 preview: when weapon Tier < 3, the Tier-3 mechanic description is visually obscured (blur / pixel mask); when weapon reaches Tier-3 the blur is fully removed and description is legible
- [ ] Blur radius controlled by `HudConfig.UpgradeTier3BlurRadius` (default 4px); not hardcoded
- [ ] Hunt pointer: when any required material is at zero, "核心不足？前往狩獵" section is visible with the correct kaiju name and core type string (read from `KaijuDef` SO matching the insufficient core type); multiple insufficient materials → show the first (highest-tier) missing item
- [ ] [← 選擇其他武器] button: calls `UIScreenManager.Pop()` to return to Loadout Screen

---

## Implementation Notes

*Derived from ADR-0006 §3 and ADR-0002 ISaveService query pattern:*

`UpgradeScreen : MonoBehaviour, IScreen` — Canvas Prefab, Screen Space – Overlay.

**`OnShow(weapon: WeaponDef)`**: set `_currentWeapon = weapon`; call `RefreshCostRows()`.

**`OnFocus()`**: call `RefreshCostRows()` (handles post-hunt return without re-push).

**`RefreshCostRows()`**:
1. `ISaveService.GetInventory()` → `_inventory`
2. `_currentWeapon.GetUpgradeCost(currentTier + 1)` → cost breakdown (shard, core types, essence)
3. Per cost row: compare `_inventory[resourceType]` vs `requiredAmount`; set row color/icon (red ✗ / green ✓); set count text via `TMP_Text.SetText("{0}", amount)` (int overload)
4. `_allSufficient = !costRows.Any(r => r.insufficient)`; `upgradeButton.interactable = _allSufficient`
5. Refresh hunt pointer visibility

**Tier-3 blur**:
- If `currentWeapon.Tier < 3`: enable blur overlay on `tier3Description` text (implementation: blurred Sprite overlay or shader material on TMP; spike required)
- If `currentWeapon.Tier >= 3`: disable blur overlay; show full description text
- Blur `_BlurRadius` = `HudConfig.UpgradeTier3BlurRadius` (MaterialPropertyBlock on blur material)

**Hunt pointer**: find `KaijuDef` where `KaijuDef.CoreType == insufficientCoreType`; set `huntPointerLabel.SetText("{0} （{1}）── {2}来源", kaiju.DisplayName, kaiju.EnglishName, coreTypeName)`.

**MaterialCollected event subscription** (OnEnable/OnDisable): call `RefreshCostRows()` on each event while screen is open.

**`OnHide()`**: unsubscribe events; stop any running coroutines.

Cost data: `WeaponDef.UpgradeCosts[tier]` — a struct array per tier with `(ResourceType, Amount)` pairs; defined in `WeaponDef` ScriptableObject (ADR-0003). Zero hardcoded costs.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 006**: `UIScreenManager` (must be DONE); Pop() on "← 選擇其他武器" uses UIScreenManager
- **Story 007**: Loadout screen "升級武器" button that pushes this screen
- **Story 010**: Safe-area fitting applied to this screen's Canvas root
- **Story 011**: Text scale (150%) applied to upgrade cost and description text elements here; colorblind mode for red/green ✗✓ icons (non-colour redundant cues — shape already differs: ✗ vs ✓)
- Actual upgrade logic (consuming materials, bumping tier) — that belongs to `Economy`/`Meta` systems, not UI

---

## QA Test Cases

*Written by qa-lead at story creation.*

- **AC-1**: Insufficient material shows red ✗ and disables button
  - Given: `ISaveService` stub returns energy_core_count=0; upgrade requires energy_core × 8
  - When: `UpgradeScreen.OnShow(l3Def)` called
  - Then: Energy core row shows red ✗ text; `upgradeButton.interactable` == false; other sufficient rows show green ✓
  - Edge cases: All rows insufficient → all show ✗; button disabled; single partial sufficiency does not enable button

- **AC-2**: Real-time sync via MaterialCollected event
  - Given: UpgradeScreen open; energy_core_count=0 (button disabled)
  - When: `IEventBus.Publish(new MaterialCollected { EnergyCoreCount = 8 })`
  - Then: Energy core row updates to show count=8; row switches to green ✓; if now all sufficient → `upgradeButton.interactable == true`
  - Edge cases: Screen hidden (OnHide called) → MaterialCollected has no effect; reopen → RefreshCostRows reads fresh ISaveService

- **AC-3**: OnFocus refreshes data
  - Given: UpgradeScreen pushed; screen goes to background (another screen pushed on top); hunt completes; screen regains focus (Pop returns here)
  - When: `OnFocus()` called by UIScreenManager
  - Then: Cost rows reflect post-hunt inventory from fresh `ISaveService.GetInventory()` call
  - Edge cases: OnFocus called with no inventory change → rows unchanged; no flicker

- **AC-4**: Tier-3 blur applied when tier < 3
  - Given: Weapon at Tier-2
  - When: `UpgradeScreen.OnShow(tier2Weapon)`
  - Then: `tier3Description` blur overlay enabled; description text not legible through blur
  - Edge cases: Upgrade completes (weapon now Tier-3) → screen refreshed; blur removed; full description visible and legible

- **AC-5**: Hunt pointer shows correct kaiju
  - Given: Upgrade requires energy_core × 8; ISaveService energy_core_count=0; `KaijuDef` for VOLTWYRM has CoreType = Energy
  - When: `RefreshCostRows()` called
  - Then: Hunt pointer section visible; pointer label text contains "VOLTWYRM" and "能量核心"
  - Edge cases: Multiple insufficient materials → highest-priority (first in cost list) determines pointer kaiju; no pointer when all sufficient

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Integration (BLOCKING): `Assets/_Project/Tests/UI/permanent_upgrade_screen_test.cs` — must exist and pass; covers insufficient/sufficient rows, real-time sync, OnFocus refresh, hunt pointer, blur state

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 006 DONE (`UIScreenManager`); Story 007 DONE (Loadout screen that pushes here); `ISaveService` with `GetInventory()`, `EnqueueSave()` in Core; `MaterialCollected` event struct in Core; `WeaponDef` with `UpgradeCosts[tier]` array; `KaijuDef` SO with `CoreType` field; `HudConfig.UpgradeTier3BlurRadius`; tech spike on blur implementation approach (filed as [需查證] in ADR-0006)
- Unlocks: Story 010 (safe-area fitting for this screen); Story 011 (text scale + colorblind ✗✓ shape cues verified here)
