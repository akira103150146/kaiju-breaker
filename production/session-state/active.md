# Active Session State — 殲獸戰機 / KAIJU BREAKER

*Last updated: 2026-07-02*
*Resume anchor: read THIS + `NEXT-STEPS.md` (same folder) first. Backlog entry point: `production/epics/index.md`.*

## Where we are
- **Stage**: Pre-Production → **implementing**. Design frozen & consistent; architecture locked; Unity project live; Foundation code done.
- **Engine**: Unity 6.3 LTS, C#. Project opens; packages resolved (URP, 2D feature, Input System, Addressables, DOTS Entities/Burst/Collections/Mathematics, Test Framework).
- **Git**: everything committed & **pushed** to `github.com/akira103150146/kaiju-breaker` (origin/main). Working tree clean.
- **Review mode**: lean. **Director authorization standing**: autonomous design/implementation + direct commit (see memory [[user-autonomy-commit]]).

## Done (implemented, compiled, EditMode tests GREEN)
- **core-foundation** (6 stories): `Assets/_Project/Scripts/Core` — `IEventBus`/`TypedEventBus` (sync same-frame, zero-GC, re-entrant, deferred sub/unsub), shared enums (`Types/`), query interfaces (`IPartStateQuery`/`IDifficultyProvider`/`ISaveService`/`IWeaponTierQuery`), Bridge contract (`HitEvent`/`IBulletSimBridge`), `App/GameBootstrap`. Tests: CoreSharedTypes (6) + TypedEventBus (7).
- **content-config** (9 stories): `Assets/_Project/Scripts/Content` — 15 tuning ScriptableObjects (WeaponDef/WeaponBalanceConfig, PartSystemConfig/KaijuDef+PartDef, DifficultyConfig, GameFeelConfig, EmitterPattern/Movement/EnemyDef, Stage/Segment/PodDrop, Economy/Input/Save) + `ContentRegistry` + `ContentTestFactory` (reflection fixtures). Tests: ContentConfig (4).

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
