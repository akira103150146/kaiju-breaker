# Story 004: Burst Simulation Job (Position Integration & Offscreen Culling)

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
**Requirement**: `TR-bullet-001`, `TR-bullet-002`, `TR-bullet-004`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0001: 彈幕引擎後端（Bullet Engine Backend）
**ADR Decision Summary**: Every frame, a Burst-compiled `IJobParallelFor` integrates position += velocity × deltaTime, decrements lifetime, and marks out-of-bounds bullets as inactive. `timeScale` is injected as a value-type struct so that `timeScale=0` (hitlag) freezes enemy bullets. No `MonoBehaviour.Update()` per bullet. Active set kept contiguous via swap-back (from Story 002 pool).

**Engine**: Unity 6.3 LTS | **Risk**: HIGH
**Engine Notes**:
- `[BurstCompile]` on `IJobParallelFor` struct — verify attribute and `Execute(int index)` signature in Entities 1.3 / Burst 1.8+ — **[需查證 6.3 API]**
- Delta time injection into Jobs: do NOT read managed `Time.deltaTime` inside a Burst Job. Pass `(float)SystemAPI.Time.DeltaTime` (or equivalent) as a field in the Job struct before scheduling. **[需查證 6.3 API]** — verify `SystemAPI.Time` availability in Entities 1.3 context vs. a standalone IJobParallelFor outside an ISystem.
- `Time.timeScale` injection for hitlag freeze: multiply deltaTime by timeScale before passing to Job. [Verify that `Time.timeScale` is readable from the main thread at Job schedule time — **[需查證 6.3 API]**]
- `JobHandle.Complete()` timing relative to collision Job (Story 006): ensure Job dependency chain is correctly ordered — **[需查證 6.3 API]**
- Offscreen cull margin (`offscreen_cull_margin_pct`) read from `BulletSimConfig` SO (ADR-0003)

**Control Manifest Rules (BulletSim — ⚠️ provisional, ADR-0001 Proposed)**:
- Required: `[BurstCompile]` on simulation Job; deltaTime and timeScale injected as value-type struct fields before scheduling; no `MonoBehaviour.Update()` per bullet; offscreen cull commits despawn by writing idle-flag into pool (swap-back deferred to main thread or secondary Job); active set stays contiguous for SIMD efficiency
- Forbidden: Managed `Time.deltaTime` read inside Job; `Instantiate`/`Destroy` per cull event; boxing in Job body; DOTS types leaking outside `BulletSim`
- Guardrail: Frame budget ≤3.5ms (bullet sim + collision combined) on mobile; 0 B/frame GC; simulation Job must respect timeScale=0 for hitlag

---

## Acceptance Criteria

*From GDD `design/gdd/bullet-system.md` §3.2, §3.3, §5, §11.1, §11.2, scoped to this story:*

- [ ] Burst Job integrates all active enemy bullet positions each frame: `position += velocity × scaledDeltaTime` (scaledDeltaTime = deltaTime × timeScale)
- [ ] Lifetime decrements each frame; when lifetime ≤ 0, bullet flagged for despawn
- [ ] Out-of-bounds bullets (position outside viewport + `offscreen_cull_margin_pct` expansion) flagged for despawn in the same Job pass; margin read from `BulletSimConfig.OffscreenCullMarginPct` (no hardcoding)
- [ ] When `timeScale = 0` (hitlag), scaled deltaTime = 0 → all bullet positions frozen that frame
- [ ] At 1,000 active bullets on mobile baseline, simulation Job completes within ≤3.5ms combined with collision (TR-bullet-001 performance budget — measured via profiler, not unit test)
- [ ] GC Alloc = 0 B/frame for Job scheduling and completion (no managed allocations in hot path)
- [ ] Spiral bullets: Job applies per-bullet angular velocity (`spiralAngularSpeed`) to rotate velocity vector each frame (supports VOLTWYRM A pattern — GDD §4.2 `spiral_angular_speed`)

---

## Implementation Notes

*Derived from ADR-0001 Decision and bullet-system.md §3.3:*

**Job struct layout**: All inputs are `[ReadOnly] NativeArray<T>` or scalar value fields (deltaTime, timeScale, viewport bounds, cull margin). Output: `NativeArray<BulletFlags>` with a `ShouldDespawn` bit per index. The pool's active-count and swap-back happen on the main thread after `JobHandle.Complete()`, reading the flags array.

**Scalar injection pattern**: Before scheduling, read `Time.deltaTime * Time.timeScale` on main thread, store in `float scaledDeltaTime`, assign to Job struct field. Similarly, read camera viewport bounds into a `Rect` value field. Schedule the Job. This is the only safe pattern for injecting managed engine state into a Burst Job.

**Spiral rotation**: `velocity = Rotate2D(velocity, spiralAngularSpeed * scaledDeltaTime)`. Rotation matrix is inlined inside the Burst Job (no managed math calls). Uses `Unity.Mathematics.math.sincos`. [Verify `Unity.Mathematics` availability in Entities 1.3 — standard dependency, but confirm package version — **[需查證 6.3 API]**]

**Offscreen margin**: Viewport in world-space units read each frame (camera may zoom or shake). Cull boundary = viewport expanded by `offscreen_cull_margin_pct` (e.g., 8% → viewport × 1.08 each side). Bullets flagged when `abs(x) > halfWidth * (1 + margin) || abs(y) > halfHeight * (1 + margin)`.

**Scheduling and dependency**: This Job reads pool arrays (write is handled by pool system on main thread between frames). Collision Job (Story 006) reads same position arrays — they must share a `JobHandle` dependency chain. Simulation Job completes before collision Job reads positions. [Verify correct `JobHandle` chaining with `IJobParallelFor` — **[需查證 6.3 API]**]

**Despawn flush**: After `JobHandle.Complete()`, main thread iterates the flags array, calls pool's despawn (swap-back + idle-push) for each flagged index, and resets the flag. This is a tiny O(despawn_count) loop, not per-bullet — only runs for bullets that died this frame.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 002**: Pool pre-allocation and spawn/despawn O(1) mechanics (this story reads those arrays; it does not own them)
- **Story 003**: EmitterPatternSO baking — this story consumes the baked Blob but does not define it
- **Story 005**: Density scaling — how many bullets are spawned is controlled by the Emitter system; this story only integrates whatever is already active
- **Story 006**: Spatial hash rebuild and player-point collision query (separate Job)

---

## QA Test Cases

*Written at story creation. The developer implements against these — do not invent new test cases during implementation.*

- **AC-1**: Position integration is correct over multiple frames
  - Given: One bullet at position (0, 0), velocity (10, 5), lifetime=10s; scaledDeltaTime=0.016s
  - When: Simulation Job runs for 3 consecutive frames (test drives deltaTime manually)
  - Then: After frame 1: position ≈ (0.16, 0.08); after frame 2 ≈ (0.32, 0.16); after frame 3 ≈ (0.48, 0.24); lifetime ≈ 9.952s
  - Edge cases: floating-point precision — allow ±0.001 tolerance per component

- **AC-2**: Lifetime expiry flags despawn
  - Given: Bullet with lifetime=0.01s; scaledDeltaTime=0.016s
  - When: Job executes
  - Then: `ShouldDespawn` flag = true; position unchanged (despawn happens post-Job on main thread)
  - Edge cases: lifetime=0.016 (exactly equals delta) → still flags; lifetime=0.017 → does not flag

- **AC-3**: Offscreen cull flags despawn
  - Given: Viewport half-size (400, 300) world units; cull margin 8%; bullet at position (433, 0) — just inside cull boundary at 432
  - When: Job executes with cull margin 0.08
  - Then: Position (433, 0) → `ShouldDespawn` = true; position (431, 0) → false
  - Edge cases: Bullet exactly at boundary (432.0) → false (boundary inclusive); bullet at (0, 325) → true (y-axis cull)

- **AC-4**: timeScale=0 freezes all positions
  - Given: 100 active bullets with various velocities; scaledDeltaTime = deltaTime × 0 = 0
  - When: Job executes
  - Then: All position arrays identical to pre-Job values; no despawn flags set (lifetimes not decremented)
  - Edge cases: timeScale=0.12 (slow-motion) → positions advance at 12% rate; verify formula `scaledDelta = delta × timeScale` is applied

- **AC-5**: Spiral angular velocity rotates bullet trajectory
  - Given: One bullet with velocity (0, 100), `spiralAngularSpeed = 1.0 rad/s`, scaledDeltaTime = 0.016s
  - When: Job executes
  - Then: Velocity vector rotated by ≈0.016 radians CCW; new velocity ≈ (-1.6, 99.99) (trigonometric, allow ±0.01 tolerance)
  - Edge cases: `spiralAngularSpeed = 0` → velocity unchanged

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/_Project/Tests/EditMode/BulletSim/BulletSim_SimulationJob_Test.cs` — must exist and all tests pass in CI

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 DONE and ADR-0001 LOCKED; Story 002 DONE (pool arrays must exist for Job to read)
- Unlocks: Story 006 (collision Job reads position arrays populated by this Job)
