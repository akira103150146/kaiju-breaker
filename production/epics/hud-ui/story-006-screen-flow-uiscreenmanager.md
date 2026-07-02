# Story 006: Screen Flow / UIScreenManager Stack

> **Epic**: HUD / UI 系統
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: ~2–3h
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/hud-ui-system.md`
**Requirement**: `TR-ui-???` — no standalone M.x criterion; foundational component required by M.5, M.6, M.7, M.8 (upgrade/difficulty/platform/a11y screens all depend on UIScreenManager). *(Warn: no TR-ID in tr-registry.yaml for screen-flow infrastructure; add TR-ui-009 when registry is formalized.)*

**ADR Governing Implementation**: ADR-0006: UI Framework Selection (Primary); ADR-0002: Event Architecture (Secondary)
**ADR Decision Summary**: Meta screens (Loadout, Upgrade, Difficulty, Results) are managed by `UIScreenManager` — a screen stack (`Push`/`Pop`/`Replace`/`ClearTo`) operating on `IScreen`-implementing UGUI Canvas Prefabs (ADR-0006 §3). Back/B/Escape always calls `Pop()`. `EventSystem.SetSelectedGameObject` is called on every `Push` for gamepad navigation. No `IEventBus` events are needed for screen transitions — `UIScreenManager` is a direct call contract (called by game flow, not event-driven).

**Engine**: Unity 6.3 URP 2D | **Risk**: MEDIUM
**Engine Notes**: `EventSystem.SetSelectedGameObject` must be called after Canvas is enabled and layout rebuilt — call in next frame via `StartCoroutine` with single-frame yield if `selectedObject` reports null immediately (known Unity UGUI timing quirk [需查證 6.3 behaviour]). `UIScreenManager` itself resides in `KaijuBreaker.UI` assembly (ADR-0005); only `App` (composition root) wires it. Back-button hook via Unity Input System `InputAction` "Cancel" — [需查證: Input System 3.x behaviour for UI cancel action under Unity 6.3.]

**Control Manifest Rules (Presentation layer)**:
- Required: `UIScreenManager` in `KaijuBreaker.UI` assembly; `IScreen` interface in `KaijuBreaker.Core`; each screen Canvas Prefab implements `IScreen`; DI — `UIScreenManager` injected into flow controllers, not a singleton; `EventSystem` focus set on every `Push`
- Forbidden: Singleton `UIScreenManager`; direct Canvas `gameObject.SetActive` from outside `UIScreenManager`; screens directly calling each other's methods
- Guardrail: Stack must handle empty-stack `Pop()` gracefully (no crash); `ClearTo` calls `OnHide()` on all stacked screens in reverse order

---

## Acceptance Criteria

*From GDD `design/gdd/hud-ui-system.md` §G (Screen Flow), F.3 mid-run gray, ADR-0006 §3:*

- [ ] `IScreen` interface defined in `KaijuBreaker.Core`: `OnShow()`, `OnHide()`, `OnFocus()`, `GameObject FirstSelectable { get; }` property
- [ ] `UIScreenManager.Push(IScreen)`: activates new screen (`OnShow()`); calls previous screen's `OnHide()`; sets `EventSystem.currentSelectedGameObject = screen.FirstSelectable`
- [ ] `UIScreenManager.Pop()`: deactivates current screen (`OnHide()`); calls previous screen's `OnFocus()`; sets EventSystem focus to restored screen's first selectable
- [ ] `UIScreenManager.Pop()` on empty stack: returns without crash; logs warning
- [ ] `UIScreenManager.Replace(IScreen)`: hides current screen, shows replacement; does not grow stack depth
- [ ] `UIScreenManager.ClearTo(IScreen)`: calls `OnHide()` on all screens in reverse stack order; then `OnShow()` on target screen
- [ ] Back/B/Escape input action fires `Pop()` when `UIScreenManager` is active (not during combat HUD)
- [ ] Screen flow path: Loadout Hub → (optional) Upgrade Screen → Difficulty Select → Combat → Results → (optional) Upgrade → Loadout Hub (see GDD §G)
- [ ] "Fast retry": after run failure, `ClearTo(LoadoutScreen)` skips difficulty — uses last confirmed difficulty (no re-select required)

---

## Implementation Notes

*Derived from ADR-0006 §3 — UIScreenManager and IScreen:*

```csharp
// KaijuBreaker.Core
public interface IScreen
{
    GameObject FirstSelectable { get; }
    void OnShow();
    void OnHide();
    void OnFocus();
}

// KaijuBreaker.UI
public class UIScreenManager : MonoBehaviour
{
    private readonly Stack<IScreen> _stack = new();

    public void Push(IScreen screen) { ... }
    public void Pop()                { ... }
    public void Replace(IScreen s)   { Pop(); Push(s); }
    public void ClearTo(IScreen s)   { while(_stack.Count > 0) Pop(); Push(s); }
}
```

`Push` implementation:
1. If stack non-empty: `_stack.Peek().OnHide()`
2. Activate screen's Canvas (`screen.Canvas.gameObject.SetActive(true)`)
3. `screen.OnShow()`
4. `_stack.Push(screen)`
5. `StartCoroutine(SetFocusNextFrame(screen.FirstSelectable))` — one-frame delay to allow layout rebuild

`Pop` implementation:
1. If stack empty: `Debug.LogWarning("UIScreenManager.Pop called on empty stack"); return;`
2. `current = _stack.Pop(); current.OnHide(); current.Canvas.gameObject.SetActive(false)`
3. If stack non-empty: `_stack.Peek().OnFocus()`; set EventSystem focus

**Back-button hook**: `InputAction.performed` callback for "UI/Cancel" action → `Pop()`. Enable only when `UIScreenManager.IsActive` (not during combat HUD phase).

**DI wiring**: `App` (composition root) instantiates `UIScreenManager` and passes it to `RunController` and any screen that needs to trigger navigation. No other system holds a reference.

**Screen Prefab**: each meta screen is a Canvas Prefab (`ScreenSpace - Overlay`) with a root MonoBehaviour implementing `IScreen`. `OnShow()` refreshes data; `OnHide()` cleans up coroutines; `OnFocus()` re-syncs dynamic data (e.g., upgrade screen refreshes inventory after returning from hunt).

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 007**: Loadout screen content and weapon card logic
- **Story 008**: Upgrade screen cost display and tier-3 blur
- **Story 009**: Difficulty select screen cards and state machine
- **Story 010**: Safe-area fitting applied to individual screen Canvas root RectTransforms
- Combat HUD Canvas management (HUD_Static/Dynamic/Hitbox_Overlay) — those are managed by the combat scene directly, not UIScreenManager

---

## QA Test Cases

*Written by qa-lead at story creation.*

- **AC-1**: Push adds to stack and fires lifecycle hooks
  - Given: `UIScreenManager` initialized; mock `IScreen` A and B
  - When: `Push(screenA)`, then `Push(screenB)`
  - Then: `screenA.OnShow()` called once; `screenA.OnHide()` called once (when B pushed); `screenB.OnShow()` called once; `_stack.Count` == 2
  - Edge cases: Push same screen twice → stack has 2 entries (allowed; OnHide/OnShow called correctly)

- **AC-2**: Pop restores previous screen
  - Given: Stack = [screenA, screenB]; screenB active
  - When: `Pop()`
  - Then: `screenB.OnHide()` called; `screenA.OnFocus()` called; `_stack.Count` == 1; `_stack.Peek()` == screenA
  - Edge cases: `Pop()` when stack has 1 item → stack empty; `Pop()` when empty → warning logged, no exception

- **AC-3**: ClearTo empties stack and shows target
  - Given: Stack = [screenA, screenB, screenC]
  - When: `ClearTo(screenD)`
  - Then: `screenC.OnHide()`, `screenB.OnHide()`, `screenA.OnHide()` all called (in reverse order); `screenD.OnShow()` called; `_stack.Count` == 1
  - Edge cases: `ClearTo` called with already-showing screen → stack empties, screen re-shows

- **AC-4**: Replace does not grow stack
  - Given: Stack = [screenA]; screenA active
  - When: `Replace(screenB)`
  - Then: `screenA.OnHide()` called; `screenB.OnShow()` called; `_stack.Count` == 1
  - Edge cases: `Replace` on empty stack → `Pop()` is no-op; `Push(screenB)` adds depth 1

- **AC-5**: Back input fires Pop
  - Given: `UIScreenManager` active with stack = [screenA, screenB]
  - When: "UI/Cancel" `InputAction.performed` fires
  - Then: `Pop()` executes; screenB hidden; screenA focused
  - Edge cases: Cancel input during combat HUD phase (no UIScreenManager active) → no action; no crash

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Integration (BLOCKING): `Assets/_Project/Tests/UI/ui_screen_manager_test.cs` — must exist and pass; covers Push/Pop/Replace/ClearTo stack behavior and lifecycle hooks

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `IScreen` interface in `KaijuBreaker.Core` (must be defined before this story); Unity Input System "UI/Cancel" action configured in Input Action Asset (engine-programmer or input story DONE)
- Unlocks: Story 007 (Loadout Screen needs UIScreenManager to push/pop); Story 008 (Upgrade Screen needs UIScreenManager); Story 009 (Difficulty Select needs UIScreenManager)
