# ADR-0001 Bullet Performance Spike (THROWAWAY)

Purpose: prove the DOTS bullet path can do **~1000 bullets @ 60 fps with 0 GC/frame**
on a mid-tier phone. Passing this flips **ADR-0001 from Proposed → Accepted** and unlocks
the bullet-sim + kaiju-encounter stories.

**Delete this entire `PerfSpike/` folder once the ADR is decided.** It is isolated in its own
assembly (`KaijuBreaker.BulletSim.PerfSpike.asmdef`) so removing it touches nothing else.

## What it does

- `BulletSpawnSystem` spawns `BulletPerfSpikeConfig.Count` entities **once**, purely from code —
  no prefab, no SubScene, no Baker.
- `BulletMoveSystem` moves them all every frame via Burst + `IJobEntity` (parallel, 0 alloc).
- These are default-World `ISystem`s, so they run the instant you enter Play mode in **any** scene.

This is a **pure-simulation** test — it measures per-frame GC and job throughput. It does **not**
render the bullets (see "Optional: see the bullets" below to add rendering).

## Run it (no setup)

1. Open any scene, press **Play**. The systems auto-run and 1000 entities start moving.
2. Confirm they exist: `Window → Entities → Hierarchy` should show ~1000 entities.
3. Tune via `BulletPerfSpikeConfig.cs` (`Count`, `Speed`), save, re-enter Play.

## Measure it (the part that matters)

Editor Play mode has domain-reload / safety-check overhead, so its numbers are **not**
representative. For the real verdict:

1. **Enable Burst**: `Jobs → Burst → Enable Compilation` (must be ON while measuring).
2. **Profiler in editor for a first look**: `Window → Analysis → Profiler`.
   - **GC Alloc** column while `BulletMoveSystem` runs must read **0 B**. Non-zero = a bug to fix
     before trusting anything else.
   - Find `BulletMoveJob` on the timeline — confirm it runs on **worker threads**, not the main thread.
3. **Then build to a mid-tier phone** (this is the number ADR-0001 actually needs):
   - `Development Build` + `Autoconnect Profiler` in Build Settings.
   - On device, watch sustained **FPS ≥ 60** and **GC Alloc = 0/frame**.
   - Push `Count` to 2000 / 4000 to find where it breaks — record that ceiling in the ADR.

Record on the ADR: device model, bullet count, sustained FPS, GC/frame, and the job's main-thread cost.

## Optional: see the bullets (adds a package)

Pure sim is enough for the GC/throughput verdict. To visually confirm and to measure **render**
cost too:

1. Package Manager → Unity Registry → install **`com.unity.entities.graphics`**
   (adds the Hybrid Renderer; pairs with the URP already in this project).
2. Add `"Unity.Entities.Graphics"` and `"Unity.Rendering.Hybrid"` to this folder's `.asmdef` references.
3. Instead of code-spawning, bake a small quad prefab (URP/Unlit material) into an entity prefab via a
   SubScene + Baker, give it `BulletVelocity`, and instantiate that prefab in `BulletSpawnSystem`.
   The move job already drives `LocalTransform`, so rendered bullets will move with no other change.

> Keeping sim and render as two separate measurements is deliberate: it tells you whether a future
> ceiling is a simulation cost or a rendering cost.
