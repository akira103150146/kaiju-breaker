# Story 006: Spatial Hash Broad-phase Collision

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
**Requirement**: `TR-bullet-005`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0001: 彈幕引擎後端（Bullet Engine Backend）
**ADR Decision Summary**: Spatial hash grid rebuilt each frame in a Burst Job from the active bullet array (cheap because bullets are already in a contiguous `NativeArray`). Player hit detection queries the grid for the player's single hit-point against its cell + 8 neighbours. Hits enqueued into `NativeQueue<HitEvent>` for the Bridge (Story 007) to consume on the main thread. Broad-phase collision cost must stay ≤3.5ms combined with the simulation Job at 1,000 bullets on mobile.

**Engine**: Unity 6.3 LTS | **Risk**: HIGH
**Engine Notes**:
- `NativeHashMap<int2, NativeList<int>>` or flat `NativeArray` grid (cell-index → bucket of bullet indices) — `NativeList` inside a `NativeHashMap` requires `NativeMultiHashMap` in older Collections; verify available container in Unity.Collections 2.x — **[需查證 6.3 API]**
- `[BurstCompile]` on grid-rebuild Job and query Job; verify that `NativeQueue<T>.AsParallelWriter()` is usable inside a Burst Job — **[需查證 6.3 API]**
- Grid cell width and max-per-cell from `BulletSimConfig` SO (ADR-0003); default: `spatial_grid_cell_px=32`, `spatial_grid_max_per_cell=48`
- Hit event struct must be a plain unmanaged value-type (`int bulletIndex`, `float2 hitPosition`) — no managed refs

**Control Manifest Rules (BulletSim — ⚠️ provisional, ADR-0001 Proposed)**:
- Required: Spatial grid rebuilt every frame in Burst Job; query limited to player's cell + 8 neighbours (not full sweep); hit results written to `NativeQueue<HitEvent>` only (sole DOTS↔Mono crossing point); grid cell and per-cell params from `BulletSimConfig` SO; DOTS types not exposed outside `BulletSim`
- Forbidden: O(N) full-array sweep per frame (must use grid); managed allocations in collision Job; direct cross-system function calls from within Jobs; `Entity` types in `HitEvent` struct
- Guardrail: Combined sim+collision ≤3.5ms on mobile at 1,000 bullets; `HitEvent` is a value-type struct (zero GC when enqueued)

---

## Acceptance Criteria

*From GDD `design/gdd/bullet-system.md` §6, §11.5, scoped to this story:*

- [ ] Player single-point hit detection: an enemy bullet registers a hit only when its radius overlaps the player's exact hit-point position; bullets whose sprite overlaps the player sprite but whose collision radius does not reach the hit-point produce no hit event
- [ ] Spatial hash grid rebuilds every frame in a Burst Job using the current active bullet positions; no managed allocations during rebuild
- [ ] Query checks only the player hit-point's grid cell + 8 neighbours (not all bullets)
- [ ] On hit: the bullet's `ShouldDespawn` flag is set (consistent with simulation Job flags) and a `HitEvent` is enqueued in `NativeQueue<HitEvent>` for Bridge consumption
- [ ] At 1,000 active bullets, combined simulation + collision cost ≤3.5ms per frame on mobile baseline (measured via Unity Profiler CPU timeline; validated by profiler run — not unit-test asserted)
- [ ] `spatial_grid_cell_px` and `spatial_grid_max_per_cell` read from `BulletSimConfig` (no hardcoded grid constants)
- [ ] GC Alloc = 0 B/frame for collision Job path in steady state

---

## Implementation Notes

*Derived from ADR-0001 Decision and bullet-system.md §6.2:*

**Grid layout**: Use a flat `NativeArray<int>` grid: `bucketArray[cellIndex * maxPerCell + slotIndex]` with a separate `NativeArray<int>` for bucket counts. Grid dimensions: `ceil(worldWidth / cellPx) × ceil(worldHeight / cellPx)`. Allocated once at load (fixed world bounds); reset each frame by zeroing bucket counts. Avoids dynamic `NativeMultiHashMap` allocation overhead. [Verify this approach is idiomatic for Entities 1.3 Collections — [需查證 6.3 API] for any alternative container]

**Grid rebuild Job**: One `IJobParallelFor` iterates active bullets, computes `(int2)floor(position / cellPx)`, increments bucket count (use `Interlocked.Increment` or `NativeArray.Increment` for thread safety), writes bullet index into slot. Max `maxPerCell` per bucket — overflow bullets silently skipped (cap is a safety bound; normal play should not exceed it). [Verify atomic increment in Burst Jobs — **[需查證 6.3 API]**]

**Query Job** (single-threaded or IJob): Compute player's cell, iterate cell + 8 neighbours, for each bullet index in those buckets check `distance(bulletPos, playerPoint) < bulletRadius`. On hit: `queue.Enqueue(new HitEvent { BulletIndex = i, HitPosition = bulletPos })`, set `despawnFlags[i] = true`.

**Bullet radius**: Constant across all enemy bullets for this game (all enemy bullets use same sprite size from atlas). Store `float bulletRadius` in `BulletSimConfig`. No per-bullet radius needed (reduces data footprint).

**Job ordering**: Grid rebuild Job depends on simulation Job completing (positions must be updated before grid is built). Query Job depends on rebuild Job. Total dependency chain: Simulate → Rebuild Grid → Query → Complete. Schedule all three before waiting.

**HitEvent struct**: `struct HitEvent { int BulletIndex; float2 HitPosition; }` — defined in `BulletSim` assembly, passed via `NativeQueue` to Bridge. Bridge (Story 007) reads queue on main thread and publishes `IGameEvent` to `IEventBus`.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 007**: `NativeQueue<HitEvent>` drain on main thread and IEventBus publish — this story fills the queue; Story 007 drains it
- **Story 008**: Player missile vs kaiju-part collision — player missiles use separate pool and separate collision logic (AABB vs few targets); not part of enemy-bullet spatial hash
- **Story 009**: Visual effects on hit; warm-colour enforcement — collision only enqueues the hit event

---

## QA Test Cases

*Written at story creation. The developer implements against these — do not invent new test cases during implementation.*

- **AC-1**: Hit registered only when bullet overlaps player hit-point (not player sprite)
  - Given: Player hit-point at (0, 0); bulletRadius = 4px; bullet at (3.9, 0) — just within radius
  - When: Collision query runs
  - Then: `HitEvent` enqueued for that bullet; `ShouldDespawn[bulletIndex]` = true
  - Edge cases: Bullet at (4.1, 0) — just outside radius → no hit; bullet at (0, 0) → definite hit; bullet at (2.83, 2.83) — diagonal at radius √(8) ≈ 4.0 → hit

- **AC-2**: Grid query limited to 9 cells (player cell + 8 neighbours)
  - Given: 1,000 bullets all placed in cells far from player cell (none in player cell or its neighbours)
  - When: Collision query runs
  - Then: Zero `HitEvent`s; zero `ShouldDespawn` flags set — confirms query did not sweep all bullets
  - Edge cases: Player at grid edge → query wraps or clamps to world bounds (no out-of-bounds access)

- **AC-3**: Correct hit when bullet is in neighbour cell (not player's own cell)
  - Given: Player hit-point at grid cell (5, 5); bullet at position just inside cell (6, 5) but within bulletRadius of player hit-point
  - When: Collision query runs
  - Then: Hit detected; demonstrates neighbour-cell query is functional
  - Edge cases: Bullet in cell (7, 5) — two cells away from player → no hit

- **AC-4**: Spatial grid bucket overflow does not crash or allocate
  - Given: `maxPerCell = 48`; 100 bullets placed into a single grid cell
  - When: Grid rebuild Job runs
  - Then: First 48 indices written; bullets 49–100 silently skipped; no exception; no managed allocation; bucket count clamped to 48
  - Edge cases: All 1,000 bullets in one cell → still no crash; 48 processed, 952 skipped

- **AC-5**: Zero GC in grid rebuild + query path
  - Given: Pool initialised with 1,000 active bullets; grid allocated at load
  - When: Grid rebuild + query Jobs run for 600 consecutive frames (EditMode test or PlayMode)
  - Then: `GC.GetTotalMemory` unchanged across all 600 frames
  - Edge cases: Frames with 0 hits and frames with hits both counted in the 600-frame window

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/_Project/Tests/EditMode/BulletSim/BulletSim_SpatialHash_Test.cs` — must exist and all tests pass in CI

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 DONE and ADR-0001 LOCKED; Story 004 DONE (simulation Job must run before grid rebuild so positions are current)
- Unlocks: Story 007 (Bridge drains the `NativeQueue<HitEvent>` that this story fills)
