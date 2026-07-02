# Story 002: Bullet Pool Pre-allocation (ECS World + NativeArray)

> **Epic**: 子彈/彈幕引擎（DOTS）
> **Status**: Blocked
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: [fill before sprint planning]
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

> **BLOCKED: ADR-0001 Proposed — run `/architecture-decision` to LOCK after the perf-prototype spike (Story 001).**

---

## Context

**GDD**: `design/gdd/bullet-system.md`
**Requirement**: `TR-bullet-001`, `TR-bullet-002`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0001: 彈幕引擎後端（Bullet Engine Backend）
**ADR Decision Summary**: Enemy bullets run in DOTS/ECS + Burst + Jobs isolated inside `KaijuBreaker.BulletSim`. All bullet capacity pre-allocated at level load (`NativeArray`); spawn = write initial state into idle slot; despawn = mark idle + swap-back. Zero `Instantiate`/`Destroy`/`new`/boxing in hot path. Enemy pool and player-missile pool allocated separately.

**Engine**: Unity 6.3 LTS | **Risk**: HIGH
**Engine Notes**:
- `NativeArray<T>` allocation with `Allocator.Persistent` at load time — verify `Allocator` enum values in Entities 1.3 / Collections package — **[需查證 6.3 API]**
- Idle-slot tracking: `NativeQueue<int>` or `NativeStack<int>` for O(1) index acquisition — verify availability in Unity.Collections 2.x — **[需查證 6.3 API]**
- `World` lifecycle: when to create / dispose the BulletSim `World` relative to scene load — **[需查證 6.3 API]**
- Entities 1.3 post-LLM-cutoff; do NOT fabricate API signatures; check `docs/engine-reference/unity/VERSION.md`

**Control Manifest Rules (BulletSim — ⚠️ provisional, ADR-0001 Proposed)**:
- Required: `NativeArray` pre-allocated once at load (手機 1536 / PC 2560 enemy bullets × 1.3 margin; player missile pool separate 手機 128 / PC 256); spawn O(1) index pop; despawn O(1) swap-back + index push; DOTS types confined to `BulletSim` assembly
- Forbidden: `Instantiate`/`Destroy` per bullet; `new` or boxing in hot path; managed references inside ECS; `Entity` types leaking out of `BulletSim`
- Guardrail: Zero GC in steady state (0 B/frame); pool pre-allocation must complete during scene load, not during gameplay

---

## Acceptance Criteria

*From GDD `design/gdd/bullet-system.md` §3.2 and §5.2, scoped to this story:*

- [ ] Enemy bullet `NativeArray` pre-allocated at scene load: capacity 1,536 on mobile / 2,560 on PC (read from `BulletSimConfig` SO — no hardcoded literals)
- [ ] Player missile `NativeArray` pre-allocated separately: capacity 128 on mobile / 256 on PC (from `BulletSimConfig`)
- [ ] Spawn operation: pops an idle index in O(1), writes `position / velocity / bullet_type / color_id / lifetime` into that slot — no allocation
- [ ] Despawn operation: swap-back active slot to fill gap, pushes freed index in O(1) — no allocation, active-set stays contiguous
- [ ] Pre-allocation completes during scene load; zero pool-triggered allocations during gameplay (Unity Profiler GC Alloc = 0 B/frame in steady state for pool operations)
- [ ] Pool capacity values read from `BulletSimConfig` ScriptableObject (`Assets/_Project/Content/BulletSim/BulletSimConfig.asset`); no magic numbers in code

---

## Implementation Notes

*Derived from ADR-0001 Decision and bullet-system.md §3.2:*

**Pool data layout**: Separate `NativeArray` slabs for enemy bullets and player missiles. Each slab stores parallel arrays: `positions (float3[])`, `velocities (float3[])`, `lifetimes (float[])`, `colorIds (int[])`, `bulletTypes (int[])`. Parallel arrays (struct-of-arrays) preserve cache locality for the Burst integration Job (Story 004).

**Idle index management**: `NativeQueue<int>` (or `NativeStack<int>`) populated with all indices [0..capacity-1] at load. Spawn = Dequeue/Pop; despawn = active-set swap-back then Enqueue/Push. [Verify NativeQueue<int> constructor and Allocator.Persistent in Unity.Collections 2.x — [需查證 6.3 API]]

**Active set tracking**: One `int activeCount` counter. Active bullets occupy indices [0..activeCount-1] in the arrays. Despawn swaps the removed index with the last active slot before decrementing counter — keeps active region dense for SIMD Jobs.

**Platform capacity selection**: Read at load from `BulletSimConfig.PoolCapacityEnemyMobile` / `PoolCapacityEnemyPC` (SO, `OnValidate` range 1024–4096). Detect platform via `Application.isMobilePlatform`. No `#if` preprocessor directives — runtime branch at init only.

**World/System lifetime**: Pool `NativeArray`s allocated in `OnCreate` of the owning `ISystem` (or equivalent); disposed in `OnDestroy`. Do not allocate in update loop. [Verify ISystem vs SystemBase for Entities 1.3 — [需查證 6.3 API]]

**`BulletSimConfig` SO**: Defined in `Assets/_Project/Content/BulletSim/BulletSimConfig.asset`. ADR-0003 governs; `OnValidate` must assert capacity within safe range. This story creates the SO class definition; the asset itself is created here.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 004**: Burst simulation job (position integration / offscreen cull) that consumes the pool arrays
- **Story 005**: Density scaling from `IDifficultyProvider` — pool capacity is fixed; density controls how many slots are filled per frame
- **Story 006**: Spatial hash collision — reads pool arrays but is not part of pool setup
- **Story 007**: DOTS↔Mono Bridge and `NativeQueue<HitEvent>`
- **Story 008**: Player missile pool runtime behaviour (tracking, hit dispatch) — this story only pre-allocates the player missile slab

---

## QA Test Cases

*Written at story creation. The developer implements against these — do not invent new test cases during implementation.*

- **AC-1**: Enemy pool pre-allocated at correct capacity
  - Given: A `BulletSimConfig` SO fixture with `PoolCapacityEnemyMobile = 100` and `PoolCapacityEnemyPC = 200`; running on a simulated mobile platform
  - When: BulletSim system initialises (EditMode or PlayMode test, dependency-injected config)
  - Then: `NativeArray` length for enemy pool == 100; idle queue depth == 100; active count == 0
  - Edge cases: PC platform → length == 200; invalid capacity (below 1024) → `OnValidate` logs error in Editor

- **AC-2**: Spawn is O(1) and allocation-free
  - Given: Pool initialised with capacity 100; all slots idle
  - When: `SpawnBullet(position, velocity, lifetime, colorId, type)` called 50 times
  - Then: Active count == 50; idle queue depth == 50; GC.GetTotalMemory unchanged between first and 50th call; each slot has the correct written values
  - Edge cases: Spawn when pool is full → returns invalid index (-1 or similar sentinel); no allocation, no exception

- **AC-3**: Despawn is O(1), swap-back keeps active region contiguous
  - Given: 50 active bullets at indices [0..49]
  - When: Despawn index 10
  - Then: Active count == 49; slot 10 now contains data that was previously in slot 49; idle queue depth == 51; array indices [0..48] all valid active bullets
  - Edge cases: Despawn last active slot → active count 0; despawn index out of active range → no-op + warning log

- **AC-4**: Zero GC in steady-state spawn/despawn
  - Given: Pool initialised; 1,000 spawn+despawn cycles in a loop
  - When: Executed inside `GC.Collect()` / `GC.GetTotalMemory` brackets
  - Then: Memory delta = 0 B across all 1,000 iterations
  - Edge cases: First call (cold path JIT) excluded; 999 warm iterations must be clean

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/_Project/Tests/EditMode/BulletSim/BulletSim_Pool_Test.cs` — must exist and all tests pass in CI

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 DONE and ADR-0001 LOCKED
- Unlocks: Story 004 (Burst simulation job consumes pool arrays), Story 008 (player missile pool runtime)
