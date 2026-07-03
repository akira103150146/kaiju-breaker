# Prototype — Playable Stage 01 (THROWAWAY)

**Hypothesis:** Does the full 4-phase loop (LOADOUT → STAGE/道中 → BOSS → RESULTS) with
real weapon choice, mob waves, and the authoritative dual-track part system feel like a
cohesive game — at parity with `prototypes/vision-slice/prototype.html`?

**Status:** In progress. Compiles clean; all 4 phases render & run (verified via Unity MCP).

## How to run
Open `Assets/_Project/Scenes/MainMenu.unity` (or `Stage01Prototype.unity`), press **Play**
with the Game view focused. From `MainMenu`, any key → stage. The stage opens on the LOADOUT
screen.

**Controls**
- LOADOUT: `1-4` primary laser · `5-8` secondary missile · `Q/E` difficulty · `Z/X/C` boss · `Enter`/click START
- STAGE/BOSS: mouse/touch drag = move (laser auto-fires up) · click/tap/`Space` = missile · `Z` hold+release = L3 charge shockwave (strips ARMORED) · `1-4`/`5-8` hot-swap · `R` = abort
- RESULTS: `R` retry · `M` loadout

## Design
- BOSS parts run on the REAL `KaijuBreaker.KaijuParts.PartStateSystem` (dual-track heat→soften→break,
  armor gate, stagger) via the Core event bus — the driver only publishes `LaserHit`/`WaveHit`/
  `MissileHit` and reacts to `PartSoftened`/`PartBroke`/`BossCoreBroke` etc. Weapons use the HTML's
  simplified stat model (heatPerHit/fireRate, mag/reload). Mob (道中) enemies are plain HP ints.
- Everything is code-built (configs via `ScriptableObject.CreateInstance` + reflection); the scene is
  just a Camera + a `StageDriver` GameObject.

## Notes
- Isolated `KaijuBreaker.Prototype` asmdef — DELETE this whole folder once the real stage/hud-ui
  epics land. Findings feed the production stage/UX design, per prototype standards.
- `runInBackground` is enabled so it plays when the editor window is unfocused.
