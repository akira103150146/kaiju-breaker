# Story 008: Player Missile Pool & Hit Events

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
**Requirement**: `TR-bullet-005`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0001: 彈幕引擎後端（Bullet Engine Backend）
**ADR Decision Summary**: Player missiles are pooled projectiles (discrete, flight-time, some with tracking) running in a separate pool from enemy bullets. The pool uses the same pre-allocated `NativeArray` infrastructure as Story 002. On hit, the Bridge publishes `on_missile_hit(part_id, B_fill, state_mult)` via IEventBus — the event contract is defined in `weapon-system.md F.1` (authoritative); BulletSim does not define consequences (heat, break, stagger). Player laser weapons (L1–L4) are continuous raycasts handled by the `Weapons` system, not BulletSim.

**Engine**: Unity 6.3 LTS | **Risk**: HIGH
**Engine Notes**:
- Tracking missile steering (M1/M2): rotate velocity vector toward `targetPartWorldPosition` by `trackingAngularSpeed × scaledDeltaTime` per frame — must read part world position via `IPartStateQuery.GetWorldPosition(partId)` injected on main thread and passed into Job as value-type; do NOT call interface from inside Burst Job — **[需查證 6.3 API]** for NativeArray<float2> part positions snapshot pattern
- Part AABBs for collision: ≤8 parts, large hitboxes — simple AABB check per missile per part; no spatial hash needed for player missiles (low count ≤128/256) — **[needs engine-API verification for Math.AABB]**
- `on_missile_hit` struct requires `part_id (int)`, `B_fill (float)`, `state_mult (float)` per weapon-system.md F.1 — `B_fill` and `state_mult` determined by part system (KaijuParts); BulletSim only provides `part_id` from hit detection; remainder filled by KaijuParts when it consumes the event

**Control Manifest Rules (BulletSim — ⚠️ provisional, ADR-0001 Proposed)**:
- Required: Player missile pool allocated separately from enemy pool (capacity 手機128 / PC256, from `BulletSimConfig`); missile simulation uses same Burst Job infrastructure (position integration + lifetime); tracking missiles snapshot part positions as `NativeArray<float2>` on main thread before Jobs; hit events published via `IEventBus` as `MissileHit` (Core struct) with `part_id`; payload fields `B_fill` / `state_mult` left for `KaijuParts` to compute on receipt
- Forbidden: Player laser weapons entering BulletSim (raycasts owned by `Weapons`); missile pool mixed with enemy pool (separate NativeArrays required); managed references inside missile Job; direct `KaijuParts` assembly reference from `BulletSim`
- Guardrail: Player missile pool does not consume enemy bullet budget; missile collision (AABB vs ≤8 parts) must be negligible cost compared to enemy bullet spatial hash

---

## Acceptance Criteria

*From GDD `design/gdd/bullet-system.md` §8, §11.5, scoped to this story:*

- [ ] Player missile `NativeArray` pre-allocated separately at load (capacity 128 mobile / 256 PC from `BulletSimConfig`); zero allocations at missile spawn/despawn
- [ ] Burst Job integrates missile position each frame using same pattern as enemy bullets (velocity × scaledDeltaTime); timeScale respected
- [ ] M1 / M2 tracking missiles steer toward target part's current world position by `trackingAngularSpeed` per second; part positions snapshot as `NativeArray<float2>` before Job (value-type, no managed refs in Job)
- [ ] Missile vs kaiju-part collision: AABB check against ≤8 active part bounding boxes per missile; on hit, `MissileHit { PartId = partId }` published via `IEventBus`; missile despawned
- [ ] L4-equivalent missiles (M3 penetrating): single missile may hit multiple parts in one pass; each part hit generates a separate `MissileHit` event; missile not despawned on first hit (continues until out-of-bounds or lifetime expired)
- [ ] Laser weapons (L1–L4) do **not** pass through BulletSim; any laser-handling code in `Weapons` is confirmed by asmdef — `Weapons` has no dependency on `BulletSim`
- [ ] GC Alloc = 0 B/frame for missile pool hot path in steady state

---

## Implementation Notes

*Derived from ADR-0001 Decision and bullet-system.md §8:*

**Separate missile pool**: Reuse the pool infrastructure from Story 002. `BulletSimService` owns two pools: `_enemyPool` and `_missilePool`. The missile pool `NativeArray` slots hold: `position (float3)`, `velocity (float3)`, `lifetime (float)`, `targetPartIndex (int)` (−1 = no tracking), `penetrating (bool)`.

**Tracking job**: On main thread before scheduling, snapshot current world positions of all ≤8 kaiju parts via `IPartStateQuery.GetWorldPosition(partId)` into `NativeArray<float2>` length 8 (pre-allocated, zeroed for inactive parts). Pass as `[ReadOnly]` into the missile simulation `IJobParallelFor`. Inside Job: if `targetPartIndex >= 0`, compute direction to `partPositions[targetPartIndex]`, rotate velocity vector by `min(trackingAngularSpeed × dt, angle_to_target)`. [Tracking angle cap prevents overshooting in one frame — see weapon-system.md for per-weapon tracking params.]

**Missile vs part AABB collision**: In a separate `IJob` (single-threaded, count is small): iterate active missiles × ≤8 part AABBs. Part AABBs snapshot as `NativeArray<Rect>` (pre-allocated length 8) on main thread before scheduling. On AABB overlap: enqueue `MissileHitEvent { MissileIndex, PartIndex }` into a `NativeQueue<MissileHitEvent>`. Penetrating missiles (`penetrating=true`): do not set despawn on first hit; collect all overlapping parts.

**Bridge extension**: Story 007 Bridge gains a second drain path: after draining `NativeQueue<HitEvent>` (enemy bullets → player), also drain `NativeQueue<MissileHitEvent>` (player missiles → parts). For each: publish `MissileHit { PartId = e.PartIndex }` via `IEventBus`. KaijuParts subscribes to `MissileHit` and computes `B_fill` / `state_mult` from its own state (it owns those values).

**`MissileHit` event struct** (in `Core`): `readonly struct MissileHit : IGameEvent { public int PartId; }`. Matches weapon-system.md F.1 contract (BulletSim sends part_id; KaijuParts enriches on receipt).

**No laser handling**: Confirm `KaijuBreaker.BulletSim.asmdef` has no reference to `Weapons` assembly; laser raycast code stays in `Weapons`. L4 penetrating path in `Weapons` is a single raycast collecting all parts — separate from M3 missile penetration implemented here.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 007**: The Bridge drain loop itself — this story adds the `MissileHitEvent` queue and extends Bridge.DrainAndPublish; Story 007 establishes the Bridge class and pattern
- Laser weapons (L1–L4 raycasts): `Weapons` system, not BulletSim — confirmed non-pooled continuous judgment
- Break/heat/stagger consequences of missile hits: `KaijuParts` consumes `MissileHit` and applies `B_fill`; BulletSim only reports the collision

---

## QA Test Cases

*Written at story creation. The developer implements against these — do not invent new test cases during implementation.*

- **AC-1**: Missile pool allocates separately from enemy pool and is alloc-free
  - Given: `BulletSimConfig` fixture with `PoolCapacityPlayerMissileMobile=10`; running on mobile
  - When: BulletSim initialises; 5 missiles spawned; 2 despawned
  - Then: Enemy pool NativeArray.Length unchanged; missile NativeArray.Length == 10; active missile count == 3; GC delta == 0 B across all spawn/despawn calls
  - Edge cases: Spawn when missile pool full → returns invalid sentinel; no exception

- **AC-2**: Tracking missile steers toward target part
  - Given: Missile at (0, 0), velocity (0, 100), trackingAngularSpeed=2.0 rad/s, targetPartIndex=0, partPositions[0]=(50, 100); scaledDeltaTime=0.016
  - When: Tracking Job runs one frame
  - Then: Velocity vector has rotated CCW by min(2.0×0.016, angle_to_target) radians; new velocity has positive X component (steering right toward part at x=50)
  - Edge cases: targetPartIndex=−1 → no steering; partPositions[0] directly ahead → no rotation needed (angle_to_target=0)

- **AC-3**: Non-penetrating missile despawns on first part hit
  - Given: Missile at (10, 10), `penetrating=false`; partAABBs[0] = Rect(5,5,20,20) containing missile position
  - When: Collision Job runs
  - Then: `MissileHitEvent { MissileIndex=m, PartIndex=0 }` enqueued; missile `ShouldDespawn` flag set
  - Edge cases: AABB does not contain missile → no event, no despawn; multiple overlapping AABBs on non-penetrating missile → only first overlap processed (or all — confirm with weapon-system.md)

- **AC-4**: Penetrating missile hits multiple parts, continues flying
  - Given: Missile at position overlapping both partAABBs[0] and partAABBs[2]; `penetrating=true`
  - When: Collision Job runs
  - Then: Two `MissileHitEvent`s enqueued (PartIndex=0 and PartIndex=2); missile `ShouldDespawn` flag NOT set
  - Edge cases: Penetrating missile past all parts → despawn via lifetime/offscreen-cull only

- **AC-5**: Bridge publishes MissileHit with correct part_id
  - Given: `NativeQueue<MissileHitEvent>` with event {MissileIndex=0, PartIndex=3}; FakeEventBus spy
  - When: Bridge.DrainAndPublish() called
  - Then: `FakeEventBus` received `MissileHit { PartId=3 }`; no other event type published for missile hits
  - Edge cases: Zero missile hits in a frame → zero MissileHit publishes; no exception on empty queue

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `Assets/_Project/Tests/PlayMode/BulletSim/BulletSim_MissilePool_Test.cs` — must exist and pass in CI

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 DONE and ADR-0001 LOCKED; Story 002 DONE (pool infrastructure reused); Story 007 DONE (Bridge extended with missile drain path)
- Unlocks: Epic Definition of Done — this is the last implementation story; Story 009 (readability guardrails) can proceed in parallel with this story
