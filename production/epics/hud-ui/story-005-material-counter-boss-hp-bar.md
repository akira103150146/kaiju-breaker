# Story 005: Material Counter & Boss HP Bar

> **Epic**: HUD / UI 系統
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: ~3h
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/hud-ui-system.md`
**Requirement**: `TR-ui-001`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0002: Event Architecture (Primary); ADR-0006: UI Framework Selection (Secondary)
**ADR Decision Summary**: Material counter and Boss HP bar are driven by `IEventBus` events (ADR-0002 typed bus). Both display on `HUD_Dynamic` Canvas (ADR-0006 §2). The material counter bounce and "+N" float text use object pools on `HUD_Dynamic` (zero `Instantiate` per pickup). Boss HP bar reads `BOSS_CORE` state only, not aggregate part health.

**Engine**: Unity 6.3 URP 2D | **Risk**: MEDIUM
**Engine Notes**: Floating "+N" text pool uses `TMP_Text.SetText(int)` — no string allocation. Object pool sized to `maxConcurrentFloatTexts` (suggest 6) from `HudConfig` SO. Boss HP bar `Image.fillAmount` update is event-driven, not per-frame — zero dirty overhead outside boss events. [需查證 #1 in ADR-0006: Canvas.pixelPerfect confirmation in URP 6.3.]

**Control Manifest Rules (Presentation layer)**:
- Required: Subscribe events in `OnEnable`/`OnDisable`; float-text pool pre-allocated in `Awake`; Boss HP bar hidden outside boss phase (subscribe `BossPhaseStarted`/`BossPhaseEnded`); all animation durations from `HudConfig` SO
- Forbidden: Direct reference to `Economy` or `Stage` assemblies; per-frame material count polling; hardcoded animation durations; `Instantiate`/`Destroy` per material pickup
- Guardrail: Float-text pool size from SO (not hardcoded); Boss HP bar does not aggregate non-BOSS_CORE parts — only BOSS_CORE `B_current/B_max`; counter bounce does not obstruct bullet reading in B area (top-right)

---

## Acceptance Criteria

*From GDD `design/gdd/hud-ui-system.md` §D.3, D.4, M.1:*

- [ ] Material counter (B area, top-right) displays: common shard count (icon + number) and core count (icon + number); updates on `MaterialCollected` event
- [ ] On material pickup: counter bounces (animation duration = `material_counter_bounce_duration_s` = 0.3s); "+N" float text fades in then out over `material_float_text_duration_s` (0.5s)
- [ ] Float "+N" text uses pooled `TMP_Text` objects — no `Instantiate` per pickup; pool returns object on animation complete
- [ ] Essence flash indicator: appears once on `EssenceGranted` event (full-destruction trigger); auto-hides after animation; otherwise absent from layout (no persistent icon)
- [ ] Score display (A area, top-left): 6-digit fixed format "000000" right-aligned; updates on `ScoreChanged` event via `TMP_Text.SetText(int)` (no string allocation)
- [ ] Boss HP bar (A area): hidden during non-boss phases; appears on `BossPhaseStarted` event; driven exclusively by `BOSS_CORE` `B_current/B_max` (not aggregate part health); hides on `BossPhaseEnded` or `on_boss_core_break`
- [ ] HUD elements in A and B areas do not occlude bullet outlines in the play area center (M.1 layout constraint)

---

## Implementation Notes

*Derived from ADR-0006 §2 (HUD_Dynamic Canvas) and ADR-0002:*

**`MaterialCounterDisplay : MonoBehaviour`** on `HUD_Dynamic`, B area:
```
Subscribe<MaterialCollected>  → UpdateCount(shard: evt.ShardAmount, core: evt.CoreAmount)
Subscribe<EssenceGranted>     → PlayEssenceFlash()
```
`UpdateCount`: update `TMP_Text` values; start bounce `Coroutine` (scale oscillation over `material_counter_bounce_duration_s`); dequeue float-text from pool, set "+N" text, play fade animation, return to pool.

**Pool** (in `Awake`): instantiate `maxConcurrentFloatTexts` (default 6, from `HudConfig.MaxFloatTexts`) `TMP_Text` GameObjects as children of the counter; disable them; return to pool on animation complete via callback.

**`BossHpBar : MonoBehaviour`** on `HUD_Dynamic`, A area:
```
Subscribe<BossPhaseStarted>      → gameObject.SetActive(true); _bossPartId = evt.BossCorePartId
Subscribe<BossPhaseEnded>        → gameObject.SetActive(false)
Subscribe<PartStateChanged>      → if evt.PartId == _bossPartId: UpdateFill(query.B_current, query.B_max)
Subscribe<PartBroke>             → if evt.PartId == _bossPartId && evt.PartType == BOSS_CORE: gameObject.SetActive(false)
```
Or alternatively: subscribe `on_boss_core_break` to hide. Call `IPartStateQuery.GetBreakFill(_bossPartId)` to get fill ratio.

**Score display** (`ScoreDisplay : MonoBehaviour` on `HUD_Static` — static element, updates on `ScoreChanged` event only):
```
Subscribe<ScoreChanged> → _label.SetText("{0:D6}", evt.Score)
```
Note: score display goes on `HUD_Static` (score frame/label decoration) but the value `TMP_Text` should be on `HUD_Dynamic` to avoid dirtying `HUD_Static`. Separate the frame decoration from the live number. Use `TMP_Text.SetText("{0:D6}", score)` with `int` arg to avoid string allocation.

Config: `HudConfig.MaterialCounterBounceDurationS`, `HudConfig.MaterialFloatTextDurationS`, `HudConfig.MaxFloatTexts`.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 002**: `HUD_Dynamic` Canvas setup; A/B area layout frame decorations on `HUD_Static`
- **Story 010**: Safe-area fitting of the A/B areas on mobile portrait
- Detailed results-screen material breakdown (post-combat; that is a separate screen not covered by this epic's in-combat stories)
- game-feel.md orbit ball fly-in animation (game-feel system triggers the orbit ball; this story listens to the `MaterialCollected` event it fires when the ball lands)

---

## QA Test Cases

*Written by qa-lead at story creation.*

- **AC-1**: Counter updates on MaterialCollected
  - Given: `MaterialCounterDisplay` subscribed; shard count = 0, core count = 0
  - When: `IEventBus.Publish(new MaterialCollected { ShardAmount = 5, CoreAmount = 1 })`
  - Then: Shard TMP shows "5"; core TMP shows "1"; bounce coroutine started; pool object "+5" float text animating
  - Edge cases: Rapid 3 events in same frame → pool correctly handles 3 concurrent float texts

- **AC-2**: Float text uses pool, no Instantiate
  - Given: Pool pre-allocated with 6 items; `MaterialCollected` fired 6 times in quick succession
  - When: Observing Profiler or object count during playback
  - Then: No new GameObjects instantiated; pool returns items after `material_float_text_duration_s` elapsed
  - Edge cases: 7th concurrent event → oldest pool item recycled (or clamp if pool exhausted — log warning, no crash)

- **AC-3**: Boss HP bar tracks BOSS_CORE only
  - Given: Boss with 3 parts; BOSS_CORE at B_current=50, B_max=100; another part at B_current=10
  - When: `BossPhaseStarted` event published; `IPartStateQuery.GetBreakFill(bossCorePart)` queried
  - Then: `bossHpBar.fillAmount` == 0.5f (from BOSS_CORE only; not affected by the other part at 10%)
  - Edge cases: BOSS_CORE B_current=0 → bar empty; BOSS_CORE PartBroke → bar hides same frame

- **AC-4**: Boss HP bar hidden outside boss phase
  - Given: `BossHpBar` initialized; no boss phase active
  - When: Scene loaded, minion wave in progress
  - Then: `bossHpBar.gameObject.activeSelf` == false; recheck after `BossPhaseEnded` event
  - Edge cases: `BossPhaseStarted` then `BossPhaseEnded` immediately → bar hidden again

- **Manual check AC-5**: Counter bounce animation readable
  - Setup: Trigger a series of material pickups during combat; observe B area counter
  - Verify: Counter bounces visibly on each pickup; "+N" float text appears and fades smoothly over 0.5s; no z-fighting with other HUD elements
  - Pass condition: Bounce animation completes without jitter; text legible during full duration; no overlap with weapon slot area

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Integration (BLOCKING): `Assets/_Project/Tests/UI/material_counter_test.cs` — must exist and pass; covers MaterialCollected count update, pool behavior, Boss HP bar show/hide and BOSS_CORE-only tracking

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002 DONE (`HUD_Dynamic` Canvas and A/B area layout nodes created); `MaterialCollected`, `EssenceGranted`, `ScoreChanged`, `BossPhaseStarted`, `BossPhaseEnded`, `PartBroke`, `PartStateChanged` event structs in Core; `IPartStateQuery` with `GetBreakFill(partId)` in Core
- Unlocks: Story 010 (B/A area layout can be included in safe-area pass); Story 011 (Reduce-Motion: counter direct number jump verified here)
