# Assets/_Project — 殲獸戰機 / KAIJU BREAKER

Full architecture reference: `docs/architecture/architecture.md`
Assembly boundary rules: `docs/architecture/adr/0005-project-structure-assemblies.md`

---

## Folder Map

```
Assets/_Project/
├── Scripts/          One subfolder = one .asmdef = one module
│   ├── Core/         KaijuBreaker.Core       — event bus, query interfaces, shared types
│   ├── Content/      KaijuBreaker.Content    — all ScriptableObject class definitions
│   ├── BulletSim/    KaijuBreaker.BulletSim  — DOTS/ECS bullet simulation (quarantined)
│   ├── Weapons/      KaijuBreaker.Weapons    — laser/missile systems
│   ├── KaijuParts/   KaijuBreaker.KaijuParts — destructible part state machine
│   ├── Economy/      KaijuBreaker.Economy    — material yield and upgrade costs
│   ├── Meta/         KaijuBreaker.Meta       — save/load, JSON, atomic write, migration
│   ├── Stage/        KaijuBreaker.Stage      — run state machine, wave spawning
│   ├── Difficulty/   KaijuBreaker.Difficulty — density/count multipliers (single source)
│   ├── Input/        KaijuBreaker.Input      — three-scheme abstract action mapping
│   ├── GameFeel/     KaijuBreaker.GameFeel   — hitlag, slow-mo, screen shake, flash
│   ├── UI/           KaijuBreaker.UI         — HUD, world-space part health bars
│   └── App/          KaijuBreaker.App        — composition root (only assembly that refs all)
├── Content/          ScriptableObject ASSETS (not class definitions — those are in Scripts/Content)
│   ├── Weapons/      WeaponDef.asset × 8, WeaponBalanceConfig.asset
│   ├── Parts/        PartSystemConfig.asset
│   ├── Kaiju/        KaijuDef.asset × 3, EmitterPatternSO.asset (per boss phase)
│   ├── Difficulty/   DifficultyConfig.asset (single source of truth for all multipliers)
│   ├── Economy/      EconomyConfig.asset
│   ├── Stages/       StageDef, SegmentDef, PodDropConfig assets
│   ├── GameFeel/     GameFeelConfig.asset
│   ├── Input/        InputSettings.asset (defaults; player overrides go to JSON save)
│   └── Meta/         SaveConfig.asset
├── Scenes/           Bootstrap, MetaHub, Run, StageArena_*, BossArena_* scenes
├── Art/              Sprites, atlases (single bullet atlas), animations
├── Audio/            BGM, SFX
├── Prefabs/          Player, minions, pods, UI, parts
├── VFX/              Particles, shaders (pulse glow, SOFTENED color shift)
├── Settings/         URP asset, Input Actions asset, Addressables settings, Quality settings
└── Tests/
    ├── EditMode/     Pure-logic tests: state machines, formulas, economy, save migration
    └── PlayMode/     Scene/ECS/time-dependent tests: bullet sim, hitlag input, run flow
```

---

## The One Rule

**No system assembly may reference another system assembly.**
Weapons does not reference KaijuParts. KaijuParts does not reference Economy.
Cross-system communication is events only (IEventBus, defined in Core).
Read-only cross-system queries use interfaces defined in Core (IPartStateQuery, IDifficultyProvider).
App (the composition root) is the only assembly that references all others.

Violation = compile error. That is intentional.

---

## ScriptableObject Content Location

All tuning knobs from the GDDs live as `.asset` files under `Content/`.
They replace the `assets/data/**/*.yaml` placeholder paths in GDD documents.
These assets are read-only at runtime. Player-mutable state (tier levels,
materials, records) goes to the JSON save file, never back into a ScriptableObject.
