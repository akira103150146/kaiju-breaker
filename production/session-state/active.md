# Active Session State — 殲獸戰機 / KAIJU BREAKER

*Last updated: 2026-07-03 (session 4 — weapons epic complete, 7-point design specs, playable prototype)*
*Resume anchor: read THIS + `NEXT-STEPS.md` (same folder) first. Backlog entry point: `production/epics/index.md`.*
*Obsidian mirror: `C:\Users\User\Documents\Note\Kaiju-Breaker\` — full done/todo in `進度結算-2026-07-03.md`.*

## Session 5 (2026-07-05) — art / rough-demo pass (on top of the economy epic below)
- **UI/HUD art pass** (`896b9b1`, pushed): HUD/UI spec `design/assets/specs/hud-ui-assets.md`; prototype IMGUI restyled to art-bible cold palette + a `_pixelFont` hook (one chokepoint). **Font**: default Ark Pixel 16px, then director steered to **more tech-feel** → interactive font-decision **Artifact** published (live HUD mockup + canvas pixel-text + shortlist: Ark Pixel Mono / GNU Unifont / Galmuri), source `design/assets/font-demo.html` (untracked → committing now). Director still to pick + drop a TTF → assign `_pixelFont`.
- **Master asset manifest** (`dd1645a`): `design/assets/MANIFEST.md` — 56 sprite/VFX + font + UI icons batch worklist, all PENDING.
- **AI-art pipeline confirmed + tested**: Unity MCP `generate_image` routes to **fal.ai / OpenRouter** (bring-your-own-key, imports straight to Assets as sprites). Director added a fal key → **auth verified through MCP**, BUT fal account balance is **$0 ("exhausted balance")** → needs a **~$5 top-up** at fal.ai/dashboard/billing before generating. Cost: FLUX **schnell $0.003/MP**, **dev $0.025/MP** → whole MVP ≈ $1–12; validated schnell is the cheap workhorse. (`generate_model` = Tripo/Meshy 3D — unused.)
- **Rough-demo art via FREE CC0 (Kenney Pixel Shmup)** (`7d06e12`, `7d603eb`): pack committed under `Assets/_Project/Art/Kenny/`. Prototype ships wired — **player = blue, mob = red, elite = grey heavy**, enemies rotated 180°; `LoadArt()` editor-loads sprites with **procedural fallback (plan B)** so nothing breaks. Ships enlarged (player ~2×, mob 28×26, elite 48×44; player hitbox is a fixed point so difficulty unchanged). Compiles clean.
- **Still needs bespoke art (no free/off-the-shelf equivalent)**: the 3 Boss kaiju + breakable parts (SOFTENED/BROKEN states) + material/core icons → AI-gen (needs fal $5) or custom. Kenney terrain tiles deliberately NOT used (top-down grass/dirt doesn't fit the space/kaiju vertical shmup → keep the procedural starfield).
- **Open art todo**: (a) optional — Kenney enemy-bullet swap / procedural nebula-parallax bg (both free); (b) fal $5 top-up → schnell-generate boss kaiju + icons from the specs; (c) drop the chosen pixel TTF → assign `_pixelFont`. **Director to Play `Stage01Prototype` and confirm ship size / overall feel.**
- **Code track**: economy epic complete (below); next unblocked = **`difficulty` epic** (4 stories, pure C#, EditMode-testable).

## Session 5 progress (2026-07-05) — WHOLE economy epic + art specs (committed locally, NOT pushed)
- **economy epic COMPLETE — all 5 stories, 210/210 EditMode GREEN** (was 144). `EconomyService` in `Scripts/Economy`:
  - 001 (`f384674`) per-break yield · 002 (`c39960e`) full-clear essence · 004 (`acaf442`) Tier 0→3 `TryUpgrade` · 005 (`7caca3b`) anti-dominant TTB guard · 003 (`b96d5a9`) persistence push-side.
- **New Core surface (review)**: `MaterialId`/`KaijuTheme`/`TierTransition` enums; `IKaijuThemeQuery`; `HuntEnded` event; `ISaveService` +`CreditMaterials`/`GetMaterialCount`/`SpendMaterials`/`SetWeaponTier`. `EconomyConfig` +theme→core map, double-drop, full-clear knobs, upgrade cost table, weapon→core (via theme identity), `MaxTtbImprovementPct`/`Tier0To2CapPct`. `KaijuDef.Theme`.
- **Reconciliations (review)**: (1) theme→core on config + kaijuId→theme via `IKaijuThemeQuery` (runtime int ids can't be config keys). (2) weapon→core = fixed weapon→theme identity → data-driven theme→core (not an editable map, per GDD C.1). (3) tier read via existing `IWeaponTierQuery`, not a dup `ISaveService.GetWeaponTier`. (4) 005 AC-4 "top-3 in all 3 part types" unsatisfiable for the closed-form model → asserted §D.4/§H.3 ≤2.0× + ≤15% caps across all 3 part types; §H.6 viability deferred to QA playtest. (5) 003 file round-trip deferred to Meta (ADR-0004).
- **Follow-ups spawned**: Meta implements full `ISaveService`+`IWeaponTierQuery` over JSON save + publishes `HuntEnded` + save round-trip PlayMode test; QA runs economy §H.5/§H.6 playtests.
- **Art track (background art-director)**: 8 MVP asset specs committed (`856d2c0`, `design/assets/specs/`) — player ship, 3 kaiju (+SOFTENED/BROKEN states), 8 weapons, enemy bullets, material icons, break-VFX; all PENDING (await API key).
- **UI/HUD art pass (`896b9b1`, pushed)**: closed the HUD-spec gap — `design/assets/specs/hud-ui-assets.md` (art-bible §07 → font + palette + component spec). **Font decided: 方舟像素 Ark Pixel 16px** (OFL, 繁體 CJK+Latin, native 16px). Restyled the prototype (MainMenu + Stage IMGUI) to the cold-family palette (`#0A0E1A` bg, white/`#40F8FF`/`#00C0E0` text, blue buttons, warm only for kaiju/threat) + a single `_pixelFont` hook. **DIRECTOR STEP**: download Ark Pixel 16px TTF → `Assets/_Project/Art/Fonts/` → assign to `_pixelFont` on both prototype scripts (+ build a TMP bitmap asset for production). **Remaining art GAPS**: master asset manifest (index/worklist) still not written; UI icon sprites + all sprites PENDING (AI-art key).
- **NEXT (unblocked, pure C#)**: `difficulty` epic (4 stories) → then stage/meta-save/game-feel/input/hud-ui/kaiju-roster (NEXT-STEPS order). Still parked: ADR-0001 perf spike (bullets), art API key.

## Session 4 结算 (2026-07-03) — all committed & PUSHED to origin/main (HEAD 7452d74)
**DONE**: Unity MCP live (144/144 EditMode GREEN). ADR-0001 desktop smoke verified (status still Proposed — phone gate parked, PC-first). **Weapons epic logic COMPLETE**: stories 003–010 + Story 002 balance suite H.1/H.2/H.3/H.7; fixed 100× missile break-unit bug; point-3 mechanics (MidCore, GetHottestSoftenedPartId, data-driven knobs, M2 Chain Hive, L1 beam ladder 2→3→4→5). **7 feedback points**: all design specs authored (`design/gdd/{weapon-tiering-and-equal-power,enemy-tier-system,hit-feel-tiering,bullet-pattern-diversity}.md`, `design/art/scrolling-background-parallax.md`, `design/narrative/story-and-zone-structure.md`, `design/quick-specs/player-firing-direction-vertical.md`) + director decisions (`design/decisions/2026-07-03-director-decisions.md`). **Design fixes**: armor breakable by ANY weapon (heat-soften opens it; code+GDD+tests); no free mid-run weapon swap. **Playable prototype** (`Assets/_Project/Prototype/`, throwaway): full LOADOUT→道中(waves+elite+drops+pod)→BOSS(3 bosses)→RESULTS, uses real PartStateSystem, MCP-verified. Scenes MainMenu + Stage01Prototype.

**TODO (see Obsidian `進度結算-2026-07-03.md` for detail)**:
- Director/Editor: AI-art API key (fal.ai/OpenRouter) for real sprites; Story 001 config `.asset` authoring; ADR-0001 phone perf gate.
- Pure C# (unblocked): economy, difficulty, stage, meta-save, game-feel, input, hud-ui, kaiju-roster (NEXT-STEPS order); WeaponDef SO default→spec sync (cosmetic).
- Blocked/confirm: 7-point IMPLEMENTATIONS mostly gated on ADR-0001 (bullets) or Editor (art/VFX); point-5 story → /brainstorm→/team-narrative (protagonist GENESIS/珍, heavy narrative, biological-alien kaiju); fold new GDDs into systems-index.

## Session 3 progress (2026-07-02, Unity MCP live)
- **User directives this session**: (1) dev/test **PC-first** — cannot freely connect a phone remotely, so phone-gated spikes (ADR-0001 phone FPS, touch-feel) are parked, not blocking pure-logic work. (2) After weapons testing is roughly done, integrate the 7 gameplay-feedback points (`design/feedback/2026-07-02-*`). (3) Back up upcoming artifacts to the Obsidian vault above.
- **ADR-0001 perf spike — DESKTOP smoke PASS** (Unity MCP): compile clean, Play spawns 1000 `BulletVelocity` entities, Burst `IJobEntity` moves them each frame. Recorded in ADR-0001 (status stays **Proposed** — phone gate open). Commit `afacd58`.
- **weapons epic — IN PROGRESS**. Verified GREEN via Unity MCP `run_tests`:
  - Phase 0 shared contracts (commit `a0eec88`): `WeaponBalanceConfig.BuPerD0(10)/HuPerD0(25, inferred)/DefaultPrimary/DefaultSecondary`; `IPartStateQuery.GetHottestAlivePartId()`; `ISaveService.GetInitialLoadout()`; `WeaponEquipped` event.
  - Phase 1 base + Story 003 (`a0eec88`): `WeaponBehaviourBase`/`LaserWeaponBase`/`MissileWeaponBase` (ctor-DI, PartBroke→ClearCollider, magazine SM), stubs `StubPartStateQuery`/`StubWeaponTierQuery`, 9 base tests. **64/64 EditMode GREEN**.
  - Story 010 loadout (`LoadoutController` + tests) implemented, awaiting combined compile.
  - **DONE (2026-07-03): laser + missile families + loadout (004–010) → 121/121 EditMode GREEN** (`139d28a`). Fixed a real **100× missile break-unit bug** (D0Reference misuse; M3 was 3000/6000 instead of 30/60 BU — would instant-break 100-BU parts) — `0605c44`. H.3 M3-gate test at default config green — `49921b6`.
  - **Story 002 equal-power (H.1/H.2) + H.7 FOLDED INTO feedback-point-3 balance pass** (director decision "合併進第3點一次弄"). Balance analysis `design/balance/weapon-d0-equal-power-analysis.md` (`7f62721`) found **6 of 8 weapons outside ±10%** (M2 -80%, L2 +50%, M3 +29%, M1/M4/L3 low) — needs a real retune + new SO fields (EffectiveHitRate, per-weapon ShotInterval, M2DmgPerMissileMult). NOT a test-writing task.
  - **Weapons epic remaining**: Story 001 SO **asset** authoring (needs Editor — director/`.asset` creation); H.1/H.2/H.7 + the equal-power retune (in the point-3 balance pass).
- **NEXT: integrate the 7 gameplay-feedback points** (`design/feedback/2026-07-02-*`). Point 3 (weapon tiering / 散彈 2→3→4→5) absorbs the equal-power retune. Point 5 (story) direction greenlit for expansion. Point 1 (bullet-pattern diversity) gated on ADR-0001 (phone perf — parked). Points 2/4/6/7 → design specs.
- **Key weapons reconciliations** (like the kaiju-parts ones — for review): skip Weapons-side M3-T3 chain (KaijuParts already owns it, avoids double-count); `WaveHit` has no StaggerDuration field; ripple % read from `PartSystemConfig` not `WeaponDef`; M4 AoE uses corrected piecewise formula; tests live in `Tests/EditMode/Weapons/` not `Tests/Weapons/`. **HuPerD0=25 is inferred from G.2 laser defaults — wants an eventual design nod.**

## Where we are
- **Stage**: Pre-Production → **implementing**. Design frozen & consistent; architecture locked; Unity project live; Foundation + kaiju-parts done; weapons in progress.
- **Engine**: Unity 6.3 LTS, C#. Project opens; packages resolved (URP, 2D feature, Input System, Addressables, DOTS Entities/Burst/Collections/Mathematics, Test Framework).
- **Git**: everything committed & **pushed** to `github.com/akira103150146/kaiju-breaker` (origin/main). Working tree clean.
- **Review mode**: lean. **Director authorization standing**: autonomous design/implementation + direct commit (see memory [[user-autonomy-commit]]).

## Done (implemented, compiled, EditMode tests GREEN)
- **core-foundation** (6 stories): `Assets/_Project/Scripts/Core` — `IEventBus`/`TypedEventBus` (sync same-frame, zero-GC, re-entrant, deferred sub/unsub), shared enums (`Types/`), query interfaces (`IPartStateQuery`/`IDifficultyProvider`/`ISaveService`/`IWeaponTierQuery`), Bridge contract (`HitEvent`/`IBulletSimBridge`), `App/GameBootstrap`. Tests: CoreSharedTypes (6) + TypedEventBus (7).
- **content-config** (9 stories): `Assets/_Project/Scripts/Content` — 15 tuning ScriptableObjects (WeaponDef/WeaponBalanceConfig, PartSystemConfig/KaijuDef+PartDef, DifficultyConfig, GameFeelConfig, EmitterPattern/Movement/EnemyDef, Stage/Segment/PodDrop, Economy/Input/Save) + `ContentRegistry` + `ContentTestFactory` (reflection fixtures). Tests: ContentConfig (4).

## Done 2026-07-02 — kaiju-parts stories 001–005 (✅ 56/56 EditMode tests GREEN)
- **kaiju-parts** Logic stories 001–005: `Assets/_Project/Scripts/KaijuParts/` — `BreakablePart` (runtime two-bar model) + `PartStateSystem` (heat SM, armor gate/stagger, break+event emission, adjacency graph, M3 Tier-3 chain; implements `IPartStateQuery`, subscribes Laser/Wave/Missile/PartBroke). EditMode tests: `Assets/_Project/Tests/EditMode/KaijuParts/` (5 files) + helpers `RecordingEventBus`, `PartTestFactory`.
- **Reconciliations vs. story text** (followed committed Core/Content contracts — surfaced for review):
  1. **int IDs**: Core events use `int` part/kaiju/dropTable ids; `PartStateSystem` maps SO string ids → int at load (part id = declaration index; kaiju id passed to `InitializeParts(def, kaijuId)`; drop-table string→int table). Guard still validates the *string* drop id non-empty (throws `InvalidOperationException`).
  2. **config ownership**: heat/break/stagger knobs read from **`WeaponBalanceConfig`** (single source, per the CC-003 dedup), NOT `PartSystemConfig`. Added chain/adjacency knobs to `PartSystemConfig` (M3T3ChainDmgMult, M3T3ChainMaxTargets, M3ChainDamageBase, L2T3AdjacentHeatPct) — story 001 required them; content-config had omitted them.
  3. **event payloads extended**: `PartSoftened`(+CurrentHeat/MaxHeat), `PartStaggered`(+Duration/ArmorStripped), `PartStaggerEnd`(+ArmorRestored) — the story ACs assert these; only KaijuParts constructs them. Added Core `BreakState` enum. Kept committed names `BossCoreBroke`, `PartBroke.Type/Quality/AdjacencyIds`.
- **✅ Verified**: 56/56 EditMode tests pass (headless Unity 6000.3.0f1 runner, `test-results.xml`) — 31 KaijuParts cases covering every story 001–005 QA case + 25 pre-existing. One compile fix applied (test files needed `using KaijuBreaker.Content;` — bare `Content.` shadowed to the sibling test namespace). `.meta` files for the new scripts/folders will be generated on next Editor import (director).
- Story **006** (Softened/Broken readability) left to director: Visual/Feel — VFX onset + 5-tester recognition study (needs the Editor + playtest).

## Design & planning artifacts (all in repo)
- Design: `design/gdd/*.md` (12 systems + 3 kaiju), `design/art-bible.md`, `design/systems-index.md`, `design/registry/entities.yaml`.
- Architecture: `docs/architecture/architecture.md`, `adr/0001-0006`, `control-manifest.md`, `tr-registry.yaml`, `architecture-review-2026-07-02.md`.
- Backlog: `production/epics/` — 13 epics, 97 stories (index.md is the map). `production/sprints/sprint-01.md`.
- CI: `.github/workflows/ci-tests.yml`. Prototypes (throwaway, HTML): `prototypes/weapon-feel-concept`, `prototypes/vision-slice`.

## Key locked decisions
- Weapons: 2 pools (雷射×4 / 飛彈×4), dual-track **蓄熱軟化→衝擊擊破**, D₀ equal-power sidegrades, Tier 0–3.
- Materials: **kaiju-theme core sourcing = every part of a kaiju drops its theme core** (Option A). shard=universal, essence=full-clear.
- Stage drops: **cycling weapon-pod** (pool-typed, ~3s cycle) that **dwells in the reachable band ~12s** so the player waits for the weapon they want; elites are the pod source; mobs are prefabs (Movement+Emitter SO).
- Difficulty: 4 tiers scale bullet density ONLY (pillar 難度是門); TTB/output/materials invariant.
- Architecture: typed struct event bus + DI query interfaces; **hybrid DOTS(BulletSim)+MonoBehaviour** (ADR-0001 **Proposed**, pending perf spike); ScriptableObject config (ADR-0003); UI = SpriteRenderer bars + UGUI (ADR-0006).

## Blocked (need the Unity editor / director's machine)
- **ADR-0001 perf spike** (`bullet-sim/story-001`: 1000 bullets @60fps, 0 GC) → LOCKs ADR-0001 → unblocks 8 bullet-sim + 3 kaiju-encounter stories.
- **Touch-feel spike** (`input/story-001`).

## How to resume implementation
Next unblocked work = **Core systems** (pure C#, EditMode-testable, no DOTS). Recommended order in `NEXT-STEPS.md`. Start with **kaiju-parts** (dual-track state machine + `on_part_break(break_quality)` emitter — the hub the whole combat chain hangs off). Each system: implement in its `Assets/_Project/Scripts/<Module>` + EditMode tests in `Assets/_Project/Tests/EditMode/<Module>`, using `ContentTestFactory` for config fixtures and a fake `IEventBus`/query for isolation.
