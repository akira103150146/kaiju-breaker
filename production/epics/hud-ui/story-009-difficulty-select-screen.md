# Story 009: Difficulty Select Screen

> **Epic**: HUD / UI 系統
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: ~2–3h
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/hud-ui-system.md`
**Requirement**: `TR-ui-006`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002: Event Architecture (Primary); ADR-0006: UI Framework Selection (Secondary)
**ADR Decision Summary**: Difficulty UI state (first-launch default, last-difficulty memory, mid-run gray-out) is driven by `ISaveService` queries (ADR-0002 query interface). The screen is a UGUI Canvas Prefab implementing `IScreen` under `UIScreenManager` (ADR-0006 §3). No difficulty multipliers are displayed — only descriptive text per GDD §F.3 ("「難度是門，不是牆」"). Mid-run interactive lock is determined by querying `IRunStateQuery.IsRunInProgress()` (or equivalent `RunStateChanged` event subscription).

**Engine**: Unity 6.3 URP 2D | **Risk**: MEDIUM
**Engine Notes**: Four `DifficultyCard` Buttons laid out with `HorizontalLayoutGroup`; on mobile portrait switches to `VerticalLayoutGroup` (scrollable list of 4 cards). `difficulty_card_selected_scale` (1.05) applied via `RectTransform.localScale` on selection — no `Animator` required for MVP; avoid `DOTween` unless in Allowed Libraries. [需查證 #2 in ADR-0006: Explicit navigation routing in UGUI Selectable between 4 equal-width cards in Unity 6.3.]

**Control Manifest Rules (Presentation layer)**:
- Required: DI inject `ISaveService` and `IRunStateQuery`; on `OnShow()` read `ISaveService.GetLastDifficulty()` to pre-select; check `IRunStateQuery.IsRunInProgress()` to set card interactability; confirm button writes selection to `ISaveService`; all descriptive strings from `DifficultyConfig` ScriptableObject (no hardcoded strings)
- Forbidden: Displaying multiplier numbers (×2.0 bullet, etc.) — GDD §F.3 says descriptive feelings only; hardcoding D1 default value (read from `DifficultyConfig.DefaultDifficultyOnFirstLaunch`); direct reference to `Difficulty` system assembly
- Guardrail: All 4 cards same visual weight (no recommended-star bias); "下一輪可更改" text visible when cards are gray-out; confirm button triggers Pop() + difficulty write in same frame

---

## Acceptance Criteria

*From GDD `design/gdd/hud-ui-system.md` §F.3, M.6; difficulty-system.md G.2, E.1:*

- [ ] First launch (no saved difficulty): D1 card pre-selected by default (`DifficultyConfig.DefaultDifficultyOnFirstLaunch == D1`)
- [ ] After any completed or abandoned run: `ISaveService.GetLastDifficulty()` returns previous selection; that card is pre-selected on `OnShow()`
- [ ] Run in progress (mid-run pause): all 4 difficulty cards have `Button.interactable = false` (gray-out); "下一輪可更改" explanatory text visible; no selection change possible
- [ ] Confirm button: writes `ISaveService.SetSelectedDifficulty(selectedTier)` and triggers next screen flow step (e.g., `UIScreenManager.Pop()` back to Loadout, or signal to run start)
- [ ] [使用上次選擇] shortcut button: pre-fills last difficulty and confirms in one tap (calls same confirm path)
- [ ] Selected card shows scale = `difficulty_card_selected_scale` (1.05×); unselected cards at 1.0× — no color-only distinction (shape+size cue sufficient for colorblind safety)
- [ ] Mobile portrait layout: 4 cards in vertical scrollable list (not horizontal row)
- [ ] All 4 difficulty cards equal visual weight — no "recommended" star or highlight bias
- [ ] Automated test covers three states: first-launch D1, last-difficulty memory, mid-run gray-out (M.6 automated test requirement)

---

## Implementation Notes

*Derived from ADR-0006 §3 and ADR-0002 ISaveService query:*

`DifficultySelectScreen : MonoBehaviour, IScreen` — Canvas Prefab, Screen Space – Overlay.

**`OnShow()`**:
1. `_isRunInProgress = IRunStateQuery.IsRunInProgress()`
2. If `_isRunInProgress`: `SetAllCardsInteractable(false)`; show "下一輪可更改" label
3. Else: `SetAllCardsInteractable(true)`; hide gray-out label
4. `lastDifficulty = ISaveService.GetLastDifficulty()` → if null: `SelectCard(DifficultyConfig.DefaultDifficultyOnFirstLaunch)` else `SelectCard(lastDifficulty.Value)`

**`DifficultyCard : MonoBehaviour`** (×4, D1–D4):
- `Button button`; `TMP_Text titleLabel`; `TMP_Text descriptionLabel`; `RectTransform cardTransform`
- `OnSelected()`: `cardTransform.localScale = Vector3.one * HudConfig.DifficultyCardSelectedScale`; notify screen
- `OnDeselected()`: `cardTransform.localScale = Vector3.one`
- Description text sourced from `DifficultyConfig.Cards[tier].DescriptionText` (SO, no hardcode)

**Confirm button**: `ISaveService.SetSelectedDifficulty(_selectedTier)`; then `UIScreenManager.Pop()` (returns to Loadout which then triggers run start on its "確認出發" flow — or however `RunController` listens).

**[使用上次選擇]**: calls `SelectCard(ISaveService.GetLastDifficulty() ?? D1)` then fires confirm.

**Mobile portrait**: in `OnShow()`, check `Screen.height > Screen.width`; switch `cardsContainer.GetComponent<LayoutGroup>()` between `HorizontalLayoutGroup` and `VerticalLayoutGroup`, enable scroll on vertical. Or use a single `VerticalLayoutGroup` with a `ScrollRect` that is enabled only on portrait.

**Mid-run gray-out**: subscribe `RunStateChanged` event in `OnEnable`/`OnDisable` as backup — if run starts while screen is open (unlikely but possible in future), update interactability live.

Config: `DifficultyConfig.DefaultDifficultyOnFirstLaunch`, `DifficultyConfig.Cards[]`; `HudConfig.DifficultyCardSelectedScale`.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 006**: `UIScreenManager` (must be DONE)
- **Story 007**: Loadout screen that eventually pushes this screen
- **Story 010**: Safe-area fitting for this screen
- **Story 011**: Text scale applied to difficulty card description text; colorblind mode
- Actual difficulty multiplier application — that belongs to `Difficulty` system, not UI

---

## QA Test Cases

*Written by qa-lead at story creation.*

- **AC-1**: First launch defaults to D1
  - Given: `ISaveService` stub returns `GetLastDifficulty() == null`; `DifficultyConfig.DefaultDifficultyOnFirstLaunch == D1`
  - When: `DifficultySelectScreen.OnShow()` called
  - Then: D1 card has `localScale.x` ≈ 1.05f; D2–D4 cards at 1.0f; `_selectedTier == D1`
  - Edge cases: Save data cleared/reset → reverts to D1; no null reference on GetLastDifficulty()

- **AC-2**: Last-difficulty memory pre-selects previous choice
  - Given: `ISaveService.GetLastDifficulty()` returns D3
  - When: `DifficultySelectScreen.OnShow()` called
  - Then: D3 card has `localScale.x` ≈ 1.05f; D1/D2/D4 at 1.0f; `_selectedTier == D3`
  - Edge cases: Previous difficulty was D4 → D4 pre-selected; confirm button writes D4 if not changed

- **AC-3**: Mid-run gray-out disables all cards
  - Given: `IRunStateQuery.IsRunInProgress()` returns true
  - When: `DifficultySelectScreen.OnShow()` called (from pause menu)
  - Then: D1/D2/D3/D4 `Button.interactable` all == false; "下一輪可更改" label active
  - Edge cases: `RunStateChanged(inProgress: false)` event fires while screen open → cards re-enable; label hides

- **AC-4**: Confirm writes to ISaveService
  - Given: D2 selected; `ISaveService` spy/mock
  - When: Confirm button clicked
  - Then: `ISaveService.SetSelectedDifficulty(D2)` called exactly once; `UIScreenManager.Pop()` called
  - Edge cases: [使用上次選擇] shortcut → same ISaveService write with last value; no double-write

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Integration (BLOCKING): `Assets/_Project/Tests/UI/difficulty_ui_state_test.cs` — must exist and pass; covers first-launch D1, last-difficulty memory, mid-run gray-out per M.6

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 006 DONE (`UIScreenManager`); `ISaveService` with `GetLastDifficulty()`, `SetSelectedDifficulty(DifficultyTier)` in Core; `IRunStateQuery` (or `RunStateChanged` event) in Core; `DifficultyConfig` SO with `Cards[]` and `DefaultDifficultyOnFirstLaunch` fields; `HudConfig.DifficultyCardSelectedScale`
- Unlocks: Story 010 (safe-area fitting for this screen); Story 011 (text scale on difficulty card descriptions)
