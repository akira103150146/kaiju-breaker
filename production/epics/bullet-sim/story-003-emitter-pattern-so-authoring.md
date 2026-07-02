# Story 003: EmitterPatternSO Authoring Layer & Blob Baking

> **Epic**: 子彈/彈幕引擎（DOTS）
> **Status**: Blocked
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: [fill before sprint planning]
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

> **BLOCKED: ADR-0001 Proposed — run `/architecture-decision` to LOCK after the perf-prototype spike (Story 001).**

---

## Context

**GDD**: `design/gdd/bullet-system.md`
**Requirement**: `TR-bullet-003`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0001 (primary — Blob baking, authoring–backend decoupling); ADR-0003 (secondary — ScriptableObject as authoring vehicle)
**ADR Decision Summary**: Designer-facing `EmitterPatternSO` (ScriptableObject) defines all bullet patterns via Inspector; at load time it bakes into an immutable Burst-compatible `BlobAssetReference<EmitterPatternBlob>` (read-only at runtime). Authoring assets know nothing about whether the backend is DOTS or Mono — this decoupling is the reversibility guarantee for ADR-0001.

**Engine**: Unity 6.3 LTS | **Risk**: HIGH
**Engine Notes**:
- `BlobBuilder` / `BlobAssetReference<T>` in Entities 1.3 for baking `EmitterPatternSO` → `EmitterPatternBlob` — **[需查證 6.3 API]**: verify `BlobBuilder.Allocate`, `BlobBuilder.SetPointer`, `BlobAssetReference.Create`, and disposal API in the Unity.Entities 1.3 package before writing baking code
- `OnValidate` on ScriptableObject for range-checking emitter params (ADR-0003 mandate)
- Blob must be immutable and value-type only (no managed references inside Blob struct) — **[需查證 6.3 API]**
- `ScriptableObject` asset creation in `Assets/_Project/Content/BulletSim/Patterns/` per ADR-0003 directory convention

**Control Manifest Rules (BulletSim — ⚠️ provisional, ADR-0001 Proposed)**:
- Required: `EmitterPatternSO` fields match GDD §4.2 parameter set (shape, bullet_count, spread_deg, aim_mode, spiral_arm_count, spiral_angular_speed, bullet_speed, fire_interval, charge_telegraph_s, color_id, spawn_origin); Blob baked once at load, read-only at runtime; authoring layer must not reference any DOTS types (decoupling)
- Forbidden: Managed references inside Blob; runtime mutation of baked Blob; any DOTS import in the `Content` assembly (SO definitions live in `Content`, not `BulletSim`)
- Guardrail: `color_id` restricted to warm-colour palette enum at SO level (enforced in `OnValidate`); `bullet_speed` and `charge_telegraph_s` must not be zero (OnValidate); `readability_cap_priority` knob must be present and defaulting true

---

## Acceptance Criteria

*From GDD `design/gdd/bullet-system.md` §4, §11.3, scoped to this story:*

- [ ] `EmitterPatternSO` exposes all 11 parameters from GDD §4.2 with Inspector-friendly labels and tooltips; no code changes required for a designer to add a new pattern asset
- [ ] All 10 existing boss patterns (CARAPEX A/B/C, LACERA A/B/C, VOLTWYRM A/B/C + Phase3 核心直射) can be authored from the 5 shape enum values (`AIMED_FAN`, `RING`, `SPIRAL`, `WALL`, `CROSS`) without adding new shape types or code
- [ ] `OnValidate` enforces GDD §10 safe ranges: `bullet_speed > 0`, `fire_interval > 0`, `charge_telegraph_s >= telegraph_min_s (0.3s)`, `color_id` is warm-palette only, `bullet_count` within 1–512
- [ ] `EmitterPatternSO.BakeToBlob()` (or equivalent at load) produces a valid `BlobAssetReference<EmitterPatternBlob>` containing all field values; Blob is immutable value-type (no managed refs)
- [ ] Baking occurs once at level load and is not called per-frame
- [ ] `Content` assembly (where SO is defined) has zero references to `Unity.Entities`, `Unity.Burst`, or `Unity.Jobs` packages — decoupling is assembly-enforced

---

## Implementation Notes

*Derived from ADR-0001 §Decision and ADR-0003 §Decision:*

**Two-layer split**: `EmitterPatternSO` lives in the `Content` assembly (no DOTS dependency). The `BulletSim` assembly owns the `EmitterPatternBlob` struct and the baking call. The SO has a method (or the baking is invoked by BulletSim's load system) that passes raw field values; BulletSim assembly wraps them into a Blob. This keeps `Content` DOTS-free — if DOTS is swapped out, only `BulletSim` changes.

**`EmitterPatternBlob` struct**: Pure unmanaged value-type fields mirroring all SO params. No `string`, no class references. Enum values stored as `int`. [Verify that BlobAssetReference<T> works with a flat struct of primitive fields in Entities 1.3 — [需查證 6.3 API]]

**shape enum**: Define in `Core` assembly as `BulletShape { AimedFan, Ring, Spiral, Wall, Cross }` — shared between Content (SO authoring) and BulletSim (runtime). `Core` has no DOTS dependency.

**AimMode enum**: `BulletAimMode { FixedDir, AimAtPlayer, Radial }` — also in `Core`.

**Warm colour palette**: Define `BulletColorId` enum in `Core` with values matching GDD §4.2 (Orange, Yellow, DeepRed, etc.). `OnValidate` on SO rejects any value not in this enum. This is the hard guard that enforces game-concept.md visual rule.

**spawn_origin**: Reference to a `Transform` (MonoBehaviour side) at authoring time. At runtime (ECS side), this must be resolved to a world position each frame (e.g., via a query on ALIVE kaiju part position). Story 003 establishes the authoring-time reference; the runtime resolution is Story 004's responsibility.

**Asset location**: `Assets/_Project/Content/BulletSim/Patterns/CARAPEX_A.asset`, etc. One asset per boss pattern per GDD §4.3.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 004**: Runtime Burst Job that reads the baked Blob each frame to spawn bullets according to fire_interval / pattern shape — this story only defines and bakes the SO
- **Story 005**: Density scaling — `bullet_count` in the SO is the base value; the density multiplier is applied at runtime (Story 005), not stored in the SO
- **Story 006**: Collision detection — no collision logic in authoring layer

---

## QA Test Cases

*Written at story creation. The developer implements against these — do not invent new test cases during implementation.*

- **AC-1**: All 10 boss patterns authorable without code
  - Given: A designer with access to Unity Inspector only (no code changes)
  - When: Attempting to recreate each of the 10 boss patterns from GDD §4.3 using existing shape enum values and `EmitterPatternSO` fields
  - Then: All 10 patterns can be represented; no new shape enum value or script modification required
  - Edge cases: LACERA C (halved fire_interval surviving limb) — verify fire_interval can be independently set per Emitter asset; CARAPEX C Phase3 8-way — verify CROSS + RADIAL can be expressed with existing aim_mode + bullet_count

- **AC-2**: OnValidate blocks invalid authoring
  - Given: An `EmitterPatternSO` asset in Inspector
  - When: Designer sets `bullet_speed = 0`, `charge_telegraph_s = 0.1` (below 0.3s min), `color_id = ColdBlue` (non-warm)
  - Then: `OnValidate` logs Inspector error for each violation; asset cannot be saved with invalid values
  - Edge cases: Boundary exactly at min (0.3s) must PASS; boundary just below (0.29s) must FAIL

- **AC-3**: BakeToBlob produces correct values
  - Given: An `EmitterPatternSO` with known field values (EditMode test fixture: BulletShape.Ring, bullet_count=12, spread_deg=360, aim_mode=Radial, bullet_speed=90, fire_interval=2.0, charge_telegraph_s=0.5, color_id=Orange)
  - When: `BakeToBlob()` called
  - Then: Returned `BlobAssetReference<EmitterPatternBlob>` contains identical field values to the SO; no managed references in Blob; Blob is valid (not default)
  - Edge cases: Second call to `BakeToBlob` on same SO produces equal Blob (deterministic); Blob must be disposed after test to avoid memory leak

- **AC-4**: Content assembly has zero DOTS references
  - Given: `KaijuBreaker.Content.asmdef`
  - When: Built in Unity (assembly compilation)
  - Then: No compilation errors or warnings referencing `Unity.Entities`, `Unity.Burst`, `Unity.Jobs` — confirmed by asmdef references list
  - Edge cases: `EmitterPatternBlob` struct (owned by BulletSim) must NOT appear in Content assembly

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `Assets/_Project/Tests/EditMode/BulletSim/BulletSim_EmitterPattern_Test.cs` — must exist and all tests pass in CI; OR documented playtest showing all 10 patterns fire correctly in-game

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 DONE and ADR-0001 LOCKED
- Unlocks: Story 004 (runtime Burst Job reads baked Blob), Story 005 (density scaling reads base bullet_count from Blob)
