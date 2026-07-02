# Story 001: Perf-Prototype Spike — DOTS/ECS Mobile Baseline

> **Epic**: 子彈/彈幕引擎（DOTS）
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: 4–6 hours (spike; time-boxed)
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/bullet-system.md`
**Requirement**: `TR-bullet-001`, `TR-bullet-002`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0001: 彈幕引擎後端（Bullet Engine Backend）
**ADR Decision Summary**: Adopt DOTS/ECS + Burst + Jobs (Entities 1.3+) for enemy bullet simulation isolated in `KaijuBreaker.BulletSim`; MonoBehaviour + pool for rest of game; EmitterPatternSO authoring layer decoupled from backend so it survives a backend swap; single Bridge translates `NativeQueue<HitEvent>` to IEventBus events. **Status: Proposed — this story is the validation gate. DoD = perf evidence that LOCKs or rejects ADR-0001.**

**Engine**: Unity 6.3 LTS | **Risk**: HIGH
**Engine Notes**:
- Entities 1.3 World creation, ISystem / IJobParallelFor, Burst compilation — **[需查證 6.3 API]**; do NOT fabricate API signatures; cross-reference `docs/engine-reference/unity/VERSION.md` before coding.
- `SystemAPI.Time.DeltaTime` / `WorldTime` injection to freeze bullets on `timeScale=0` — **[需查證 6.3 API]**.
- `BlobBuilder` / `BlobAssetReference` for Blob baking — **[需查證 6.3 API]**.
- `Graphics.DrawMeshInstanced` vs Entities Graphics (Hybrid Renderer) for 1000 bullet sprites — verify which path Unity 6.3 Entities 1.3 uses for SpriteAtlas instancing — **[需查證 6.3 API]**.
- Unity 6.3 Knowledge Risk: HIGH (post-LLM-cutoff); all Entities API usage must be verified against official docs before submission.

**Control Manifest Rules (BulletSim — ⚠️ provisional, ADR-0001 Proposed)**:
- Required: DOTS/ECS + Burst + Jobs confined to `KaijuBreaker.BulletSim` assembly; `NativeArray` pre-allocated at load (手機 1536 / PC 2560 × 1.3 margin); hot path zero `Instantiate`/`Destroy`/boxing; single sprite atlas + single material for enemy bullets; `NativeQueue<HitEvent>` as sole DOTS↔Mono crossing point
- Forbidden: DOTS `Entity` or managed references leaking outside `BulletSim`; per-bullet `MonoBehaviour.Update()`; GC allocations in steady-state hot path
- Guardrail: ≤3.5ms bullet+collision per frame on mobile baseline; 0 B/frame GC (steady-state); `readability_cap_priority` non-negotiable

---

## Acceptance Criteria

*From GDD `design/gdd/bullet-system.md` §11.1 and §11.2, scoped to spike validation:*

- [ ] Mobile baseline device: sustain 1,000 enemy bullets (with collision + rendering) at stable 60fps for ≥60 continuous seconds; no frame exceeds 33.3ms (Unity Profiler frame-time histogram)
- [ ] Unity Profiler GC Alloc column shows 0 B/frame for every frame in steady-state window (frames 61–3600; initial pool allocation on load excluded)
- [ ] Spatial hash collision job cost ≤3.5ms per frame at 1,000 bullets on mobile baseline (Unity Profiler CPU timeline measurement)
- [ ] Spike outcome documented in `production/qa/evidence/bullet_gc_profile_[date].md`: device spec, profiler screenshots, frame-time histogram, GC Alloc chart, collision cost measurement, PASS or FAIL verdict
- [ ] If PASS: ADR-0001 advanced from Proposed → Accepted via `/architecture-decision`; Stories 002–009 unblocked
- [ ] If FAIL: fallback plan documented (achieved bullet count, proposed path: lower D4 density → tighten cap → last resort visual; never sacrifice readability); ADR-0001 rejection recorded

---

## Implementation Notes

*Derived from ADR-0001 Decision and Required Validation Gate:*

This is a **time-boxed spike** — implement the minimum needed to measure perf on the target device. Production-quality EmitterPatternSO authoring, full density scaling, and full Bridge are out of scope. Hardcode a test emitter that spawns exactly 1,000 bullets at load.

**DOTS World setup**: Create a minimal `World` scoped to `BulletSim`. Register only the components needed: position (`float3`), velocity (`float3`), lifetime (`float`). Use `NativeArray` pre-allocation (not per-bullet entity instantiation at runtime). [Verify Entities 1.3 idiomatic approach — `IComponentData` structs vs `NativeArray` direct — against engine-reference docs before committing to approach.]

**Burst Job**: Write one `IJobParallelFor` (Burst-compiled) that integrates position += velocity × deltaTime, decrements lifetime, marks out-of-bounds bullets as inactive. Inject delta time as a value-type struct — do not read managed `Time.deltaTime` inside a Job. [Verify `SystemAPI.Time` availability in Entities 1.3 — [需查證 6.3 API]]

**Spatial hash**: `NativeArray`-backed fixed-size grid. Cell width ~32px; max 48 bullets/cell per config (`spatial_grid_max_per_cell`). Rebuild grid in a Job each frame (bullets are already in a contiguous array — rebuild is cheap). Query player point (single `float2`) against player cell + 8 neighbours.

**Rendering**: Single `SpriteAtlas`, single material. All 1,000 bullets rendered in as few draw calls as possible. Verify GPU instancing path for Entities 1.3. Target: single-digit draw calls.

**Profiler capture**: Run on physical mobile baseline hardware (not Editor, not simulator). Capture minimum 600-frame window in steady state. Export profiler data and screenshot for evidence doc.

**DOTS↔Mono boundary**: Even in the spike, do not let `Entity` types or Burst Job types surface to any assembly outside `BulletSim`. Bridge exists as a stub (`NativeQueue<HitEvent>` → log hits to console) — not wired to IEventBus (that is Story 007).

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 003**: EmitterPatternSO ScriptableObject definition and Blob baking — use hardcoded spawn params in spike
- **Story 004**: Full Burst simulation job with density-driven patterns (spike uses fixed 1000 bullets)
- **Story 005**: Density scaling from `IDifficultyProvider` / `DifficultyConfig`
- **Story 007**: Full `NativeQueue<HitEvent>` → `IEventBus` Bridge (stub log output is sufficient for spike)
- **Story 008**: Player missile pool and hit event dispatch
- **Story 009**: Readability guardrails (warm-colour enforcement, hitpoint overlay, telegraph system)
- Laser weapons: continuous raycast path belongs to `Weapons` system, not `BulletSim`

---

## QA Test Cases

*Written at story creation. The developer implements against these — do not invent new test cases during implementation.*

- **AC-1**: Mobile sustain 1,000 bullets at 60fps for ≥60s
  - Given: Physical mobile baseline device (spec recorded in evidence doc); BulletSim `World` initialized; 1,000 bullets spawned from pre-allocated `NativeArray`; all bullets have infinite lifetime (no despawn during spike); Burst position-integration job runs each frame; spatial hash rebuilt each frame; single player-point collision query per frame
  - When: Scene runs uninterrupted for ≥60 seconds under Unity Profiler capture (deep profile OFF for perf accuracy)
  - Then: Unity Profiler frame-time chart shows every frame in the 60–3600 window (frames 61–3600) at ≤16.6ms; no single frame ≥33.3ms
  - Edge cases: Frames 1–60 warm-up excluded; if any single spike >33.3ms in steady-state, verdict is FAIL regardless of average

- **AC-2**: GC Alloc = 0 B/frame in steady state
  - Given: Same scene as AC-1; Unity Profiler GC Alloc column active
  - When: Any 600-frame window captured after frame 60
  - Then: Every row in GC Alloc column shows 0 B; no allocation in `KaijuBreaker.BulletSim` assembly namespace
  - Edge cases: Load-time pre-allocation excluded (one-time); only steady-state frames evaluated; even 1 B in any steady-state frame = FAIL

- **AC-3**: Spatial hash collision cost ≤3.5ms on mobile
  - Given: 1,000 active bullets; worst-case cluster scenario (500 bullets concentrated within 2 grid cells adjacent to player point)
  - When: Collision query job executes for one frame, measured via Unity Profiler CPU timeline
  - Then: Job execution time ≤3.5ms on mobile baseline
  - Edge cases: Uniform spread (typical) should be faster; only worst-case cluster must pass the 3.5ms gate

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `production/qa/evidence/bullet_gc_profile_[date].md` — device spec, profiler screenshot (frame-time histogram + GC Alloc column + CPU thread breakdown), collision cost measurement, PASS/FAIL verdict
- Optional headless smoke: `Assets/_Project/Tests/PlayMode/BulletSim/BulletSim_PerfSpike_GCAlloc_Test.cs` — 600-frame PlayMode loop asserting `GC.GetTotalMemory` does not grow (frame-time assertion on device hardware only; headless is not authoritative for fps)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None — this is the first story; it validates ADR-0001
- Unlocks: Stories 002–009 (all blocked on ADR-0001 Proposed; Story 001 DONE + ADR LOCKED removes the block)
