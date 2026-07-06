# NEXT STEPS — KAIJU BREAKER (ordered TODO)

*Last updated: 2026-07-05. Pair with `active.md`. Backlog detail: `production/epics/index.md` + each `production/epics/<slug>/story-*.md`.*

Legend: ⬜ todo · 🔒 blocked · 👤 director-only (needs Unity editor / GitHub / device)

---

## A. Implement Core systems (unblocked — pure C#, EditMode-testable, no DOTS)
Do in this order (dependency-safe). Pattern per system: implement in `Assets/_Project/Scripts/<Module>` (namespace `KaijuBreaker.<Module>`, asmdef references Core+Content), constructor-inject `IEventBus` + needed query interfaces, read all tuning from the injected SO (no hardcoded values), + EditMode tests in `Assets/_Project/Tests/EditMode/<Module>` using `ContentTestFactory` fixtures and a fake `IEventBus`/query for isolation. After each: director runs EditMode tests → if green, commit (+ push).

1. ✅ **kaiju-parts** (epic `kaiju-parts`, 6 stories) — **001–005 DONE (2026-07-02), verified GREEN via Unity MCP (now part of the 178-case EditMode suite)**; 006 = director Visual/Feel.
   `Assets/_Project/Scripts/KaijuParts/{BreakablePart,PartStateSystem}.cs` + EditMode tests in `Assets/_Project/Tests/EditMode/KaijuParts/`. Covers heat SM (INTACT↔SOFTENED hysteresis+decay), armor/stagger (`WaveHit`→strip+timer), break→`PartBroke`+`BossCoreBroke` (fixed order), adjacency graph + M3 Tier-3 chain (non-recursive via `IsChainBreak`). Implements `IPartStateQuery`. Committed & pushed to origin/main (a9e2453) — see `active.md` for the 3 reconciliations to review.
   Story **006** (readability VFX + 5-tester recognition study) still needs the Editor + a playtest (director). `.meta` files generate on next Editor import.
2. ✅ **weapons** (epic `weapons`, 10 stories) — **DONE (Session 4, 2026-07-03)**. Weapon SO → D₀ output, dual-track firing (lasers emit `LaserHit` heat / missiles emit `MissileHit` break + mag/reload), L3 charge→`WaveHit`, M3 heat-shock gate, Tier 0→3 knob application, loadout (1 primary+1 secondary). Injects `IPartStateQuery` + `IWeaponTierQuery`. Story 001 SO **.asset** authoring still 👤 (director/Editor). Equal-power retune (H.1/H.2/H.7) folded into feedback-point-3 balance pass — see `active.md`.
3. ✅ **economy** (epic `economy`, 5 stories — **DONE 2026-07-05, 210/210 GREEN**) — `EconomyService` (`Scripts/Economy`):
   - ✅ **001** per-break yield (shard + theme-core; `IKaijuThemeQuery`; theme→core + double-drop in `EconomyConfig`).
   - ✅ **002** full-clear essence (new Core `HuntEnded` event → `EssencePerFullClear` + `ShardCompletenessBonus`).
   - ✅ **004** Tier 0→3 `TryUpgrade` (atomic, one-way, data-driven costs; weapon→core via theme identity; tier read via `IWeaponTierQuery`, write via `ISaveService.SetWeaponTier`).
   - ✅ **005** anti-dominant TTB guard (shared `WeaponBalanceFixtures`+`WeaponBalanceModel`; per-weapon ≤15%/≤10% caps config-driven + ≤2.0× spread across all 3 part types). **AC-4 reconciliation**: literal "top-3 in all 3 part types" unsatisfiable for the closed-form model; §H.6 qualitative viability deferred to QA playtest (see commit `7caca3b`).
   - ✅ **003** persistence handoff — push-side (same-frame/no-loss/mid-fight-retain/interface-only) DONE in-memory. **Follow-up (Meta epic)**: true cross-session FILE round-trip serialize/deserialize (ADR-0004) → Meta PlayMode test.
   - **Meta/QA follow-ups spawned by economy**: (a) Meta implements full `ISaveService` (CreditMaterials/GetMaterialCount/SpendMaterials/SetWeaponTier/GetInitialLoadout) + `IWeaponTierQuery` over the JSON save; publishes `HuntEnded`. (b) QA: §H.5 progression-curve + §H.6 no-dominant-loadout playtests.
4. ✅ **difficulty** (epic `difficulty`, 4 stories — **DONE, 273/273 GREEN**) — `DifficultySystem`+`DifficultyScaling` (`Scripts/Difficulty`), `IDifficultyProvider` (committed property surface), `DifficultyConfig.asset` (§D.3). The two BLOCKING invariance suites done via **assembly-scan** (`AssemblyReferenceScanner`: KaijuParts/Weapons/Economy never ref `IDifficultyProvider`) + TTB(4×3)/output(4×8)/yield(36) matrices → `production/qa/evidence/`. Reconciliations in test comments (see `active.md`).
5. 🔨 **stage** (epic `stage`, 7 stories) ← **in progress**. ✅ **001 Run 狀態機 DONE (2026-07-06, 15/15 GREEN)** — `RunController` (`Scripts/Stage`), `RunState` enum (Core), transitions LOADOUT→STAGE→BOSS→RESULTS→LOADOUT, `RunStateChanged`/`HuntEnded` emit, autosave triggers, full-clear tracking via `EnterBoss(totalBreakableParts)`. New Core events: `RunStateChanged`/`LoadoutConfirmed`/`WeaponPodGrabbed`. **Next stage stories**: 003 segment recombination (Fisher-Yates + no-repeat, pure Logic — good next), 002 SO-driven prefab spawning, 004 elite+guaranteed pod, 005 **cycling weapon-pod (descend→dwell→cycle→pickup)**, 006 boss entrance, 007 onboarding. Note: stories 002/004 note enemy-firing integration awaits ADR-0001 (bullet-sim) — the spawn/movement/pod logic is doable now. Difficulty hooks ready: Stage reads `IDifficultyProvider` + calls `DifficultyScaling.ScaledEnemyCount`; H.5 content-accessibility test will bind to real `Stage.GetSelectableStages`.

## B. Then Feature + Presentation (mostly unblocked)
6. ⬜ **meta-save** (epic `meta-save`, 7 stories) — could also be done early (Foundation-ish): JSON schema/serializer, atomic write+backup, CRC32, migration, autosave-on-bank (subscribe `PartBroke`), ownership persistence. Implements `ISaveService`+`IWeaponTierQuery`.
7. ⬜ **game-feel** (epic `game-feel`, 7 stories) — hitstop/slow-mo/shake/softened-signature/break-payoff/reduce-motion. Subscribes part events. (reduce-motion multipliers live in the mutable save/settings layer, NOT the read-only GameFeelConfig SO.)
8. ⬜ **input** (epic `input`, 6 stories) — action map + schemes. Story-001 is the touch-feel spike (👤, see D).
9. ⬜ **hud-ui** (epic `hud-ui`, 11 stories) — world-space SpriteRenderer bars + UGUI HUD/meta screens (ADR-0006).
10. ⬜ **kaiju-roster** (epic `kaiju-roster`, 10 stories) — 7 Ready (KaijuDef/PartDef data + EmitterPattern defs for CARAPEX/LACERA/VOLTWYRM); 3 encounter-integration stories 🔒 (ADR-0001).

## C. 🔒 Blocked until ADR-0001 LOCKs (needs the perf spike, item D2)
- bullet-sim stories 002–009 (pooling, EmitterPattern runtime, Burst sim job, spatial-hash collision, DOTS↔Mono bridge impl, player-missile pool, readability guardrails).
- kaiju-roster stories 004/007/010 (per-boss encounter firing integration).

## D. 👤 Director tasks (Unity editor / device / GitHub — I can't do these)
1. **CI secret**: GitHub repo → Settings → Secrets → add `UNITY_LICENSE` (+ maybe UNITY_EMAIL/PASSWORD). Verify the Unity **6.3 editor image tag** in `.github/workflows/ci-tests.yml` matches the installed build.
2. **ADR-0001 perf spike** (`bullet-sim/story-001`): prototype DOTS bullets, measure ~1000 @60fps + 0 GC/frame on a mid-range mobile baseline → then run `/architecture-decision` to move ADR-0001 Proposed→Accepted (or reject → Mono-pool fallback). Unblocks section C.
3. **Touch-feel spike** (`input/story-001`): validate Sky-Force relative-drag + `touch_follow_lerp` on device → LOCK the touch scheme's feel values.
4. **Burst AV false-positive** (error 4551): keep `Jobs > Burst > Enable Compilation` OFF during dev; before the perf spike, add a Windows Defender exclusion for `C:\Game\kaiju-breaker` + the Unity editor, delete `Library/BurstCache`, re-enable Burst.
5. **.asset creation**: when wiring systems, create ScriptableObject instances via the `KaijuBreaker/Config/...` Create menus (WeaponBalanceConfig, 8 WeaponDef, PartSystemConfig, 3 KaijuDef, DifficultyConfig, GameFeelConfig, EconomyConfig, InputSettings, SaveConfig, ContentRegistry) and populate the ContentRegistry references.

## E. Open items / decisions (non-blocking)
- Sprint 1 (`production/sprints/sprint-01.md`): confirm people model (single dev vs dev+director-on-spikes), sprint length (2 vs 3 wk), `DF-001`↔`CC-003` DifficultyConfig SO dedup (recommend CC-003 authoritative), run `/qa-plan sprint`.
- `EmitterPatternType` enum may need spiral/wall/cross params fleshed out at bullet-sim implementation time.
- `tr-registry.yaml` formalized (95 reqs) — keep it updated as stories change; `BossKaijuId` in StageDef kept as string (could become a typed KaijuDef ref later).

## Quick status: 97 stories / 13 epics
Done ✅: core-foundation (6), content-config (9), kaiju-parts 001–005, weapons (10), economy (5), **difficulty (4)**, **stage 001**. EditMode suite: **288/288 GREEN** (Unity MCP). **Next code work: `stage` epic remaining (002–007) — start 003 segment recombination (pure Logic).** Blocked 🔒: bullet-sim impl (8) + kaiju encounters (3) on ADR-0001.

**Art / demo track (2026-07-05, session 6):** ✅ **3 boss kaiju bespoke art DONE** — director generated 16 parts via Gemini Pro (web), I green-screened + placed into `Assets/_Project/Art/Kaiju/` + **wired into the prototype boss assembly** (body-base + parts, intact↔stripped, idle motion, hit-flash silhouettes, soften/durability meters). ✅ Ark Pixel 16px font landed + assigned. ✅ **Armor-break bug fixed** (`MissileDamagePart` missed heat-softened bypass; `_parts.Tick` missing in boss phase). **Art TODO left**: (a) material/UI icon sprites (still PENDING — Gemini or fal); (b) LACERA limb **root-pivot** (currently hinges about center, wants sprite pivot at root); (c) optional free polish (enemy bullets / nebula bg). **Play-test TODO**: director confirm full CARAPEX break loop (laser-soften→missile-break) now bug-fixed. Follow-ups (code): stage epic; Meta (`ISaveService`/`IWeaponTierQuery` + save round-trip + publish `HuntEnded`); QA (economy §H.5/§H.6 playtests). **Debug rule**: never loosen balance to reproduce bugs (memory `no-balance-tweaks-for-debugging`).
