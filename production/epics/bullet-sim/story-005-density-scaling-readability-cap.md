# Story 005: Density Scaling & Readability Cap Enforcement

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
**Requirement**: `TR-bullet-004`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0001 (primary — density hook via Bridge/emitter runtime); ADR-0003 (secondary — `DifficultyConfig` SO as single source of truth for `bullet_density_mult`)
**ADR Decision Summary**: BulletSim reads `IDifficultyProvider.BulletDensityMult` (injected; never polled directly from `DifficultyConfig`). Applies `ceil(base_count × mult)` or `ceil(base_arm_count × mult)` at spawn time. Only scales bullet count, spiral arm count, and fire interval — speed, spread, shape, and telegraph duration are invariant. After scaling, enforces `max_concurrent_bullets` hard cap (`readability_cap_priority = true`; non-disableable).

**Engine**: Unity 6.3 LTS | **Risk**: HIGH
**Engine Notes**:
- `IDifficultyProvider` is a `Core` interface; `Difficulty` system implements it; BulletSim receives it via constructor/method injection — verify Unity DI pattern (no static singleton) — **[需查證 6.3 API]** for Entities 1.3 ISystem injection
- `Mathf.CeilToInt` for integer bullet count from float multiplication — standard managed math, safe to call on main thread before Job scheduling (not inside Burst Job)
- `max_concurrent_bullets` read from `BulletSimConfig` SO; `readability_cap_priority` is a boolean flag on the SO with `OnValidate` that refuses `false`

**Control Manifest Rules (BulletSim — ⚠️ provisional, ADR-0001 Proposed)**:
- Required: Read `IDifficultyProvider.BulletDensityMult` via injected interface (not direct SO reference); `bullet_density_mult` has single authoritative owner (`DifficultyConfig`); only bullet_count / spiral_arm_count / fire_interval scaled; all other params invariant; density-scaled count capped by `max_concurrent_bullets` hard cap before any spawn call
- Forbidden: Copying or caching difficulty values locally in BulletSim; scaling `bullet_speed`, `spread_deg`, `spiral_angular_speed`, or `charge_telegraph_s`; disabling `readability_cap_priority`
- Guardrail: Readability cap enforced before spawn — density can never cause screen to exceed `max_concurrent_bullets_mobile` (1000 default) or `max_concurrent_bullets_pc` (2000 default)

---

## Acceptance Criteria

*From GDD `design/gdd/bullet-system.md` §4.4, §11.4, scoped to this story:*

- [ ] `actual_bullet_count = ceil(base_count × bullet_density_mult[tier])` — correct for all four tiers (D1=×1.00, D2=×1.25, D3=×1.50, D4=×2.00 per difficulty-system.md D.2); allow ±1 for ceil rounding
- [ ] `actual_spiral_arms = ceil(base_arm_count × bullet_density_mult[tier])` — same formula and tolerance
- [ ] `bullet_speed`, `spread_deg`, `spiral_angular_speed`, `charge_telegraph_s` are **not** scaled — measured values identical across D1–D4 for the same pattern
- [ ] When `actual_bullet_count` would exceed `max_concurrent_bullets` (platform), truncate to `max_concurrent_bullets`; `readability_cap_priority` active (default true); no exception, no log spam
- [ ] `readability_cap_priority = false` is blocked: `BulletSimConfig.OnValidate` sets it back to `true` and logs an Editor error
- [ ] Difficulty multiplier read exclusively via injected `IDifficultyProvider` — no direct `DifficultyConfig` field access in `BulletSim` assembly; verified by code review (no `DifficultyConfig` type import in `BulletSim.asmdef` references)

---

## Implementation Notes

*Derived from ADR-0001 Decision, bullet-system.md §4.4, and control-manifest §3 BulletSim + §3 Difficulty:*

**Density hook call site**: In the Emitter spawn path (called from main thread, before Job scheduling), compute: `int scaled = Mathf.CeilToInt(pattern.BulletCount * _difficultyProvider.BulletDensityMult)`. Then apply cap: `int capped = Mathf.Min(scaled, _config.MaxConcurrentBullets - activeCount)`. `capped` is passed to the pool spawn request.

**`IDifficultyProvider`**: Defined in `Core`; has `float BulletDensityMult { get; }`. `Difficulty` system implements it. `BulletSim` receives it at construction (DI from `App` composition root). Never import `DifficultyConfig` SO type in `BulletSim.asmdef`.

**Kaiju-override table**: GDD §4.4 states "if a kaiju document explicitly specifies per-tier arm counts, use those exact values instead of the formula." At runtime, the baked `EmitterPatternBlob` may carry per-tier override arrays (optional; if present, use them; if absent, apply formula). [Story 003 defines the authoring model; this story wires the runtime override path.]

**Fire interval scaling**: `actual_fire_interval = base_fire_interval / bullet_density_mult` (higher density → shoots more often). Only `fire_interval` is scaled (GDD §4.4 says "射頻可縮放"); `bullet_speed` and others are invariant. Apply same `ceil` approach as count (or clamp to minimum interval from `BulletSimConfig.MinFireIntervalS`).

**Hard cap logic**: Maintain `int currentActiveEnemyBullets` counter on main thread (updated by pool spawn/despawn). Before any spawn batch: `remaining_capacity = max_concurrent - currentActive`. If ≤0, skip spawn entirely (emit `IDensityCapEvent` for GameFeel to consume — high-density signal for screen-shake coordination per GDD §7). If >0 but less than requested, truncate to remaining.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 003**: `EmitterPatternSO` authoring fields (base_count, base_arm_count stored in Blob) — this story reads those values, does not define them
- **Story 006**: Collision detection — density cap has no collision logic
- **Story 009**: Readability guardrails visual enforcement (warm colour, telegraph) — this story only enforces the numeric cap

---

## QA Test Cases

*Written at story creation. The developer implements against these — do not invent new test cases during implementation.*

- **AC-1**: Density formula correct for all four tiers
  - Given: A pattern with `base_count = 3`; `IDifficultyProvider` stub returning mults {1.00, 1.25, 1.50, 2.00} for D1–D4
  - When: Density scaling computed for each tier
  - Then: D1 → ceil(3.00) = 3; D2 → ceil(3.75) = 4; D3 → ceil(4.50) = 5; D4 → ceil(6.00) = 6 (±1 allowed for ceil boundary)
  - Edge cases: base_count=1, D4 → 2; base_count=5, D4 → 10; mult=2.00 on exact integer → no ceil artifact

- **AC-2**: Invariant params unchanged across all tiers
  - Given: Pattern with bullet_speed=120, spread_deg=50, spiral_angular_speed=0.8, charge_telegraph_s=0.4; IDifficultyProvider stub returning D4 mult=2.00
  - When: Density scaling applied
  - Then: All four values unchanged (equal to SO values); only bullet_count and fire_interval altered
  - Edge cases: If future code accidentally scales speed → test catches it immediately

- **AC-3**: Readability cap truncates excess bullets
  - Given: `max_concurrent_bullets_mobile=1000`; `currentActive=990`; `actual_bullet_count` after scaling = 50
  - When: Spawn batch requested
  - Then: Only 10 bullets spawned (remaining capacity); no exception
  - Edge cases: `currentActive=1000` → 0 bullets spawned; `currentActive=0`, `actual=5000` → exactly 1000 spawned

- **AC-4**: `readability_cap_priority=false` blocked by OnValidate
  - Given: `BulletSimConfig` SO; `readability_cap_priority` field set to false
  - When: `OnValidate` called (on save in Inspector or on load)
  - Then: Field set back to true; Editor console shows error message containing "readability_cap_priority cannot be disabled"
  - Edge cases: In builds (no `OnValidate`), config validation at runtime init should also enforce and throw/log

- **AC-5**: `IDifficultyProvider` injected — no direct `DifficultyConfig` import
  - Given: `KaijuBreaker.BulletSim.asmdef`
  - When: Assembly compiled
  - Then: No reference to `DifficultyConfig` type (search for type name in BulletSim source files); only `IDifficultyProvider` interface used
  - Edge cases: Stub `FakeDifficultyProvider` used in tests returns constant mult — confirms DI is the only coupling

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/_Project/Tests/EditMode/BulletSim/BulletSim_DensityScaling_Test.cs` — must exist and all tests pass in CI

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 DONE and ADR-0001 LOCKED; Story 003 DONE (baked Blob carries base_count that density formula reads)
- Unlocks: Story 009 (readability guardrail system reads the active-bullet count and high-density signal produced here)
