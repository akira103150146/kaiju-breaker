# Story 009: Readability Guardrails

> **Epic**: 子彈/彈幕引擎（DOTS）
> **Status**: Blocked
> **Layer**: Foundation
> **Type**: Visual/Feel
> **Estimate**: [fill before sprint planning]
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

> **BLOCKED: ADR-0001 Proposed — run `/architecture-decision` to LOCK after the perf-prototype spike (Story 001).**

---

## Context

**GDD**: `design/gdd/bullet-system.md`
**Requirement**: `TR-bullet-006`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0001 (primary — single atlas, warm-colour enforcement, high-density signal); ADR-0003 (secondary — all colour palette and readability config values in `BulletSimConfig` SO)
**ADR Decision Summary**: Readability is enforced by **hard mechanism, not design discipline**: enemy bullets draw from a single warm-colour SpriteAtlas with per-variant pixel outlines; the player hit-point sprite draws at the highest sort layer (never occluded); BulletSim emits a `HighBulletDensity` event when active bullet count exceeds a configurable threshold, signalling GameFeel to cap screen-shake. All readability knobs are in `BulletSimConfig` SO — no hardcoded colours or thresholds.

**Engine**: Unity 6.3 LTS | **Risk**: HIGH
**Engine Notes**:
- SpriteAtlas and SpriteRenderer sorting layer order for player hit-point guaranteed-on-top — verify `SpriteRenderer.sortingOrder` vs `Canvas.sortingOrder` interaction in Unity 6.3 URP 2D — **[需查證 6.3 API]**
- GPU instancing with SpriteAtlas for 1,000 enemy bullets: confirm `SpriteAtlas` + single material + `Graphics.DrawMeshInstanced` (or Entities Graphics) achieves single-digit draw calls in URP 2D — **[需查證 6.3 API]**
- `HighBulletDensity` event is a `Core` event struct published via `IEventBus`; `GameFeel` system subscribes (separate GDD — not authored yet)
- Pixel-art outline shader: pixel outlines implemented as sprite sheet art (1-pixel border baked into atlas sprites), not a runtime shader effect — avoids per-bullet overdraw; confirm with technical-artist that outline is baked, not runtime

**Control Manifest Rules (BulletSim — ⚠️ provisional, ADR-0001 Proposed)**:
- Required: All enemy bullets from single warm-colour SpriteAtlas + single material (target single-digit draw calls); `color_id` restricted to warm-palette at authoring time (`OnValidate`) and at runtime (atlas lookup by color_id enum); player hit-point sprite on highest sort layer — permanently visible; `charge_telegraph_s >= telegraph_min_s (0.3s)` enforced per-emitter; `HighBulletDensity` event published when `activeCount > highDensityThreshold` (from config)
- Forbidden: Cold-colour or greyscale enemy bullets (`color_id` values blocked at SO validation); effects or particles occluding the player hit-point sprite; `readability_cap_priority` disabled; telegraph duration scaled by density multiplier (GDD §7 — density does not shorten telegraphs)
- Guardrail: D4 maximum density: ≥70% of 5 testers identify enemy bullets vs player hit-point in a static screenshot (GDD §11.6); hitpoint sprite must not be occluded by any VFX layer; draw calls from enemy bullets ≤ individual budget (individual target: single-digit)

---

## Acceptance Criteria

*From GDD `design/gdd/bullet-system.md` §7, §11.6, scoped to this story:*

- [ ] D4 maximum density static screenshot: ≥5 testers, ≥70% correctly identify enemy bullets vs player hit-point without instruction beyond "find the player" (GDD §11.6 readability test)
- [ ] All enemy bullets rendered with warm-colour tones (orange #FF8000, yellow #FFCC00, deep red #CC2200, or SO-configured equivalents) with high-contrast pixel outline; no cold or neutral enemy bullet colours pass at runtime
- [ ] Player hit-point sprite renders at the highest configured sort order (value from `BulletSimConfig.HitpointSortOrder`); no enemy bullet sprite, VFX layer, or particle system renders above it
- [ ] Every Emitter fires a visible telegraph flash lasting ≥`telegraph_min_s` (0.3s default from `BulletSimConfig`) before spawning bullets; telegraph duration is not reduced by density multiplier (invariant across D1–D4)
- [ ] `HighBulletDensity` event published via `IEventBus` on the frame when `activeEnemyBullets > config.HighDensityThreshold`; event not spammed (published once on entry, once on exit, not every frame at high density)
- [ ] Enemy bullet rendering: single `SpriteAtlas`, single material; draw calls from enemy bullets remain in single digits (measured via Unity Frame Debugger or RenderDoc on mobile)

---

## Implementation Notes

*Derived from ADR-0001 Consequences, bullet-system.md §7, and control-manifest §3 BulletSim:*

**Warm-colour atlas**: One `SpriteAtlas` (`Assets/_Project/Content/BulletSim/EnemyBulletAtlas.spriteatlas`) containing sprite variants keyed by `BulletColorId` enum (Orange, Yellow, DeepRed, …). Each variant has a 1-pixel high-contrast black or bright outline baked into the sprite (not a runtime shader). Atlas packed tight; all enemy bullet sprites share the single atlas material → one draw call (or one draw call per Burst-instanced batch). [Coordinate with technical-artist that outline is baked, not a shader effect, before implementation]

**Runtime color_id lookup**: When setting up a bullet for rendering, map `colorId (int)` to the corresponding sprite in the atlas. Since all bullets are static sprites (no animation), the sprite reference is resolved once at spawn and stored as a sprite index — not a managed reference inside ECS. [Rendering strategy: if using `Graphics.DrawMeshInstanced`, store a per-bullet material property block or use a texture index — verify URP 2D batching approach — **[需查證 6.3 API]**]

**Player hit-point sort layer**: `PlayerHitpoint` SpriteRenderer `sortingOrder` = `BulletSimConfig.HitpointSortOrder` (default very high, e.g., 9999). Set at startup; never modified at runtime. No VFX layer may render above this value — this is a project-wide convention; coordinate with game-feel and VFX systems to respect the reserved top layer.

**Telegraph flash**: Each `Emitter` (on the MonoBehaviour/Emitter authoring side, not in ECS) runs a coroutine or timer: when fire cycle begins, enable a flash `SpriteRenderer` on the emitter source for `charge_telegraph_s` seconds before calling the spawn path. Flash duration NOT scaled by density mult (invariant). [Consider emitting a `TelegraphStarted` event from BulletSim so future GameFeel GDD can add audio/VFX reactions without coupling to BulletSim]

**`HighBulletDensity` event**: `Core` struct `HighBulletDensityChanged { bool IsHigh; }`. `BulletSimService` tracks whether `activeCount > config.HighDensityThreshold` changed this frame — only publishes on state change (entry/exit), not every frame. Threshold default from `BulletSimConfig.HighDensityThreshold` (suggest 600 as midpoint between 0 and 1000 cap; designer-tunable).

**No screen-shake here**: `HighBulletDensity` event is published here; screen-shake cap implementation lives in `GameFeel` system (separate GDD). BulletSim only sends the signal; it does not implement the shake cap.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Art pipeline**: Enemy bullet sprite creation, warm-colour palette, pixel outline art — technical-artist deliverable; this story wires the atlas in code but does not create the art assets
- **GameFeel screen-shake cap**: `GameFeel` system subscribes to `HighBulletDensity` and applies its own cap logic — that system is not yet designed; this story only emits the event
- **Readability at game-feel/VFX layer**: hit flash VFX, SOFTENED glow, part-break effects — separate GameFeel epic; this story only handles bullet and hitpoint readability

---

## QA Test Cases

*Written at story creation. The developer implements against these — do not invent new test cases during implementation.*

- **AC-1**: D4 static screenshot readability ≥70%
  - Setup: Open D4-density scene (1,000 enemy bullets active, all three boss patterns firing); take a screenshot with player ship and hit-point visible; show still image to 5 testers
  - Verify: Each tester asked "where is the player's hit-point?" and "point to an enemy bullet" without prior instruction on colours
  - Pass condition: ≥4 of 5 testers (80%) correctly identify both — target is 70%, aiming for 80% to have margin; record results in `production/qa/evidence/readability_d4_[date].md` with tester count and success rate

- **AC-2**: No cold-colour enemy bullet passes at runtime
  - Setup: Attempt to set `colorId = ColdBlue` (hypothetical invalid value) on a bullet slot manually in a test scene
  - Verify: Bullet renders as the fallback warm colour (Orange); console logs an error about invalid color_id; no cold-coloured sprite rendered
  - Pass condition: In a 10-second playthrough at D4, zero cold-coloured enemy bullets visible to testers in spot-check

- **AC-3**: Player hit-point always on top
  - Setup: Enable 1,000 enemy bullets + all active VFX layers (hit sparks, telegraph flashes, full GameFeel effects active if available)
  - Verify: Player hit-point sprite visible in every frame of a 30-second playthrough; no other sprite or effect renders above it
  - Pass condition: Spot-check at 10 frames scattered across 30s — hit-point sprite found in render order above all others via Frame Debugger in every sampled frame

- **AC-4**: Telegraph fires for ≥0.3s before spawn, not shortened at D4
  - Setup: Time Emitter telegraph at D1 and at D4 using Unity Timeline or script timer
  - Verify: Flash duration measured at both difficulties for CARAPEX A, VOLTWYRM A, LACERA B patterns
  - Pass condition: All measured durations ≥0.3s at both D1 and D4 (±0.016s tolerance for frame timing); D4 not shorter than D1 for any pattern

- **AC-5**: HighBulletDensity event published on entry/exit only
  - Setup: PlayMode test or manual test; IDifficultyProvider stub set to D4; active bullet count crosses `HighDensityThreshold` (600) going up and then coming back down
  - Verify: `FakeEventBus` (or event log) shows exactly 2 publications: one `IsHigh=true` on threshold crossing up, one `IsHigh=false` on crossing down; NOT one per frame
  - Pass condition: Event count in a 600-frame run = exactly 2 (one entry, one exit)

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**:
- `production/qa/evidence/readability_d4_[date].md` — screenshot + tester results (≥5 testers, ≥70% pass) + lead sign-off
- `production/qa/evidence/telegraph_timing_[date].md` — screenshot/recording of telegraph timing at D1 and D4 for all three boss patterns

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 DONE and ADR-0001 LOCKED; Story 005 DONE (density cap must be enforced before readability test is meaningful); enemy bullet art assets delivered by technical-artist (SpriteAtlas with warm-colour variants + pixel outlines)
- Unlocks: Epic Definition of Done — completing this story + all prior integration tests = bullet-sim epic ready for `/story-done`
