# Story 007: DOTS↔Mono Bridge (NativeQueue → IEventBus)

> **Epic**: 子彈/彈幕引擎（DOTS）
> **Status**: Blocked
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: [fill before sprint planning]
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

> **BLOCKED: ADR-0001 Proposed — run `/architecture-decision` to LOCK after the perf-prototype spike (Story 001).**

---

## Context

**GDD**: `design/gdd/bullet-system.md`
**Requirement**: `TR-bullet-002`, `TR-bullet-005`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0001: 彈幕引擎後端（Bullet Engine Backend）
**ADR Decision Summary**: A single `BulletSimBridge` MonoBehaviour (or plain C# main-thread service) drains `NativeQueue<HitEvent>` after `JobHandle.Complete()` each frame, translates each entry into the appropriate `Core` event struct, and publishes via `IEventBus`. This is the **only** crossing point between the DOTS world and the Mono game. No managed references enter the ECS World; no `Entity` types leak out. Cross-boundary data is exclusively value-type structs (POD): player point (`float2`), part AABBs, density mult, timeScale in; `HitEvent` out.

**Engine**: Unity 6.3 LTS | **Risk**: HIGH
**Engine Notes**:
- `NativeQueue<T>.Dequeue` and `TryDequeue` on main thread after `JobHandle.Complete()` — verify thread-safety contract in Unity.Collections 2.x — **[需查證 6.3 API]**
- Timing: Bridge must run after all BulletSim Jobs have completed; if using `ISystem`, verify ordering relative to MonoBehaviour `Update` — **[需查證 6.3 API]**
- `IEventBus.Publish<T>(in T evt)` — `in` keyword avoids struct copy; verify that the event struct `PlayerHitByBullet` is in `Core` and implements `IGameEvent` — standard pattern per ADR-0002
- `App` composition root wires Bridge to IEventBus and provides player-point/part-AABB accessors to BulletSim; no direct cross-system references in BulletSim itself

**Control Manifest Rules (BulletSim — ⚠️ provisional, ADR-0001 Proposed)**:
- Required: Bridge is the **sole** DOTS↔Mono crossing point; only value-type structs cross the boundary (in or out); Bridge runs on main thread after all BulletSim Jobs complete; `NativeQueue<HitEvent>` fully drained each frame; events published synchronously via `IEventBus`; Bridge publishes `PlayerHitByBullet` event (defined in `Core`)
- Forbidden: Managed references (`object`, `MonoBehaviour`, `Component`) passing into any Job or NativeContainer; `Entity` types referenced anywhere outside `BulletSim`; cross-system direct calls from Bridge (use IEventBus publish only); per-hit GC allocation
- Guardrail: 0 B/frame GC in Bridge drain loop; synchronous event dispatch (same frame as collision detection, per ADR-0002 §4.1)

---

## Acceptance Criteria

*From GDD `design/gdd/bullet-system.md` §6.3, §11.2, §11.5, scoped to this story:*

- [ ] Each frame, after BulletSim Jobs complete, Bridge drains `NativeQueue<HitEvent>` to empty; no events left pending across frames
- [ ] For each `HitEvent`: Bridge publishes `PlayerHitByBullet` (defined in `Core`) via `IEventBus.Publish<PlayerHitByBullet>(in evt)` on the same frame as the collision detection
- [ ] Zero managed allocations in the Bridge drain loop (0 B/frame GC in steady state per Unity Profiler)
- [ ] No `Entity` type, DOTS type, or managed reference crosses into or out of the Bridge beyond value-type structs
- [ ] Inputs to BulletSim from Mono side (player hit-point `float2`, per-frame `timeScale float`) are written to shared `NativeArray<float2>` / scalar fields before Job scheduling — no managed refs inside Jobs
- [ ] `BulletSim` assembly compiles with zero references to any system assembly other than `Core` + `Content` + Unity DOTS packages (confirmed by asmdef)

---

## Implementation Notes

*Derived from ADR-0001 Decision §4 (Bridge) and control-manifest §3 BulletSim + §4.2 event chain:*

**Bridge class**: `BulletSimBridge` — plain C# class (not MonoBehaviour; no `Update()`). Called explicitly by the `BulletSimService` main-thread coordinator each frame after `JobHandle.Complete()`. Constructor-injected: `IEventBus _bus`, `NativeQueue<HitEvent> _hitQueue` (reference to the queue owned by BulletSim Job chain).

**Drain loop**: `while (_hitQueue.TryDequeue(out HitEvent h)) { _bus.Publish(new PlayerHitByBullet { HitPosition = h.HitPosition }); }` — no allocation per iteration (struct publish via `in`).

**Input feeding (Mono → DOTS)**: Before scheduling Jobs each frame, `BulletSimService` writes current player hit-point into a `NativeReference<float2>` (or `NativeArray<float2>` length 1) — `[ReadOnly]` inside Jobs. Similarly passes `timeScale` and part AABBs (for player-missile collision, Story 008). These are pre-allocated at load; no per-frame allocation.

**`PlayerHitByBullet` event struct** (to be defined in `Core` assembly if not already present): `readonly struct PlayerHitByBullet : IGameEvent { public float2 HitPosition; }`. Downstream systems (player health, game-feel) subscribe via `IEventBus.Subscribe<PlayerHitByBullet>`.

**Ordering**: `BulletSimService.LateUpdate()` (or equivalent frame-end callback): (1) schedule Simulate Job, (2) schedule Grid Rebuild Job (depends on 1), (3) schedule Collision Query Job (depends on 2), (4) `allJobs.Complete()`, (5) call `Bridge.DrainAndPublish()`, (6) call `Pool.FlushDespawnFlags()`. All six steps happen within one frame; events published synchronously step 5.

**Inputs requiring cross-boundary injection** (value-type only, confirmed at Bridge):
- Player hit-point `float2` — read from `IPlayerPointQuery` (Core interface, Mono implements) each frame on main thread before scheduling
- `Time.timeScale` (float) — read on main thread before scheduling
- Part AABBs for player-missile queries (Story 008) — `NativeArray<Rect>` length ≤8 pre-allocated

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 006**: Collision detection that fills the `NativeQueue<HitEvent>` — this story drains it
- **Story 008**: Player missile hit events — `on_missile_hit` dispatch is a separate event path; this story only handles the enemy-bullet → player-point hit chain
- Player health damage application, screen-shake, game-feel responses — consumed by downstream systems via `IEventBus`; this story only publishes

---

## QA Test Cases

*Written at story creation. The developer implements against these — do not invent new test cases during implementation.*

- **AC-1**: Bridge drains queue completely each frame
  - Given: `NativeQueue<HitEvent>` pre-filled with 5 hit events; `FakeEventBus` spy (integration test)
  - When: `Bridge.DrainAndPublish()` called once
  - Then: Queue empty after call; `FakeEventBus.PublishCount<PlayerHitByBullet>()` == 5
  - Edge cases: Empty queue → 0 publishes, no exception; queue with 1 hit → exactly 1 publish

- **AC-2**: Published event carries correct hit position
  - Given: `HitEvent { BulletIndex=7, HitPosition=float2(42.5f, -12.0f) }` enqueued
  - When: `Bridge.DrainAndPublish()` called
  - Then: `FakeEventBus` received `PlayerHitByBullet { HitPosition=float2(42.5f, -12.0f) }`
  - Edge cases: Multiple events with different positions → each publish carries its respective position

- **AC-3**: Zero GC in drain loop
  - Given: Queue with 50 events; integration test measuring `GC.GetTotalMemory`
  - When: Drain loop executes
  - Then: `GC.GetTotalMemory` before == after (delta = 0 B); no boxing, no string creation
  - Edge cases: 0 events → still no allocation; 100 events → still no allocation

- **AC-4**: No DOTS types outside BulletSim — asmdef check
  - Given: `KaijuBreaker.BulletSim.asmdef` and `KaijuBreaker.Core.asmdef`
  - When: Compiled
  - Then: `Core` assembly contains no `Unity.Entities` / `Unity.Burst` import; `PlayerHitByBullet` struct has no DOTS fields; only `float2` (Unity.Mathematics — confirm if Mathematics is acceptable in Core — **[需查證 6.3 API]**)
  - Edge cases: If `Unity.Mathematics` not allowed in `Core`, use `Vector2` (System.Numerics or UnityEngine) for hit position

- **AC-5**: Same-frame event publication
  - Given: PlayMode integration test; BulletSimService schedules and completes Jobs; Bridge.DrainAndPublish() called in same LateUpdate
  - When: A collision triggers in frame N
  - Then: `PlayerHitByBullet` received by subscriber in frame N (same frame); not deferred to frame N+1
  - Edge cases: Verifies synchronous dispatch contract (ADR-0002 §4.1)

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `Assets/_Project/Tests/PlayMode/BulletSim/BulletSim_Bridge_Test.cs` — integration test running in PlayMode (requires Jobs and NativeQueue to be live); must exist and pass in CI

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 DONE and ADR-0001 LOCKED; Story 006 DONE (Bridge drains the NativeQueue filled by the collision story)
- Unlocks: Story 008 (player missile hit events use the same Bridge pattern)
