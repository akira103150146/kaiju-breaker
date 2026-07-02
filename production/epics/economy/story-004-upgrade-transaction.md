# Story 004: Tier 0→3 Upgrade Transaction

> **Epic**: 素材經濟與永久升級
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M (3–4 h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/material-economy.md`
**Requirement**: `TR-economy-001`, `TR-economy-004`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0003 (primary): `EconomyConfig` SO as cost table carrier; ADR-0002 (secondary): `IWeaponTierQuery` read interface for downstream `Weapons` system
**ADR Decision Summary**: `EconomyService.TryUpgrade(weaponId, tierTransition)` reads all cost thresholds from `EconomyConfig` SO (never hard-coded). It checks affordability via `ISaveService` inventory read, atomically deducts materials via `ISaveService.SpendMaterials` on success, and writes the new tier via `ISaveService.SetWeaponTier`. The updated tier is immediately available via `IWeaponTierQuery` (implemented by `Meta`/`Economy`) so the `Weapons` system can read it. Upgrade is one-way permanent — no rollback. All cost values are data-driven; changing `EconomyConfig` changes the transaction without code changes.

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: No post-cutoff APIs required. `EconomyConfig` is a standard `ScriptableObject`. All transaction logic is pure C# testable without engine overhead. `IWeaponTierQuery` is a `Core` interface injected by `App`.

**Control Manifest Rules (Core layer)**:
- Required: All upgrade costs MUST come from `EconomyConfig` SO — zero hard-coded values (control-manifest §1.2, ADR-0003); cost table covers Tier 0→1, 1→2, 2→3 for `shard_common`, `weapon.core_type`, and `essence_kaiju` (GDD §C.4, §D.2).
- Required: Economy READS `break_quality` from events and MUST NOT recompute it — this story handles the spend side only, not yield computation (control-manifest §4.2).
- Required: `TryUpgrade` MUST check `weapon.current_tier == tier_transition.from_tier` before spending — cannot skip tiers or re-upgrade (GDD §D.2 `can_upgrade` formula).
- Required: `IWeaponTierQuery.GetTier(weaponId)` MUST return the new tier in the same frame as a successful `TryUpgrade` — in-memory update is immediate, background save is separate (control-manifest §4.3, §3 `Weapons`).
- Forbidden: MUST NOT hard-code any cost value (shard counts, core counts, essence count) — all from `EconomyConfig` (control-manifest §1.2, §5).
- Forbidden: Upgrade MUST NOT be reversible — no `UndoUpgrade` or rollback path exists; upgrade is one-way permanent (GDD §C.3, §E.1).
- Guardrail: `TryUpgrade` MUST be atomic — all-or-nothing spend. If any affordability check fails, zero materials are deducted and tier is unchanged.

---

## Acceptance Criteria

*From GDD `design/gdd/material-economy.md` §D.2 (upgrade formula), §C.3 (tier structure), §C.4 (cost table), §H.1, §H.7:*

- [ ] `can_upgrade(weapon, tier_transition)` is `true` only when: `inventory[shard_common] >= cost_shard[tier_transition]` AND `inventory[weapon.core_type] >= cost_core[tier_transition]` AND `inventory[essence_kaiju] >= cost_essence[tier_transition]` AND `weapon.current_tier == tier_transition.from_tier`.
- [ ] On successful `TryUpgrade`: `shard_common` deducted by `cost_shard`; `weapon.core_type` deducted by `cost_core`; `essence_kaiju` deducted by `cost_essence`; `weapon.current_tier` advanced by 1.
- [ ] On failed `TryUpgrade` (any cost unmet or wrong tier): zero materials deducted; tier unchanged; method returns `false`.
- [ ] Tier 0→1 costs (from `EconomyConfig`): 8 `shard_common`, 0 cores, 0 essence.
- [ ] Tier 1→2 costs (from `EconomyConfig`): 12 `shard_common`, 5 `weapon.core_type`, 0 essence.
- [ ] Tier 2→3 costs (from `EconomyConfig`): 25 `shard_common`, 8 `weapon.core_type`, 1 `essence_kaiju`.
- [ ] Each weapon uses the correct core type in affordability check: L1/M2/M4 → `core_carapace`; L2/L4/M1 → `core_limb`; L3/M3 → `core_energy`.
- [ ] After successful `TryUpgrade`, `IWeaponTierQuery.GetTier(weaponId)` returns the new tier in the same frame.
- [ ] Upgrade is one-way: attempting `TryUpgrade` on a weapon already at a higher tier returns `false` with zero deductions.
- [ ] All cost values are sourced from `EconomyConfig` SO — changing the SO changes the transaction without code changes.

---

## Implementation Notes

*Derived from ADR-0003 §1 (SO as config carrier, OnValidate ranges) and ADR-0002 §2 (IWeaponTierQuery DI):*

`EconomyService.TryUpgrade(WeaponId weaponId, TierTransition transition)`:
1. Read current tier: `int currentTier = _saveService.GetWeaponTier(weaponId)`.
2. Verify `currentTier == (int)transition.FromTier` — return `false` if mismatch (wrong tier or already upgraded).
3. Look up costs from `_config`: `costShard`, `costCore`, `costEssence` for `transition` enum value.
4. Determine `coreType = _config.WeaponCoreType[weaponId]` (weapon→core mapping from `EconomyConfig`).
5. Affordability check against `_saveService` inventory reads — if any check fails, return `false` immediately with zero deductions.
6. On all checks passing (atomic): `_saveService.SpendMaterials(MaterialId.ShardCommon, costShard)`, `_saveService.SpendMaterials(coreType, costCore)`, `_saveService.SpendMaterials(MaterialId.EssenceKaiju, costEssence)`, `_saveService.SetWeaponTier(weaponId, currentTier + 1)`.
7. Return `true`.

`IWeaponTierQuery.GetTier(weaponId)` is implemented by `Meta`/`Economy`; after `SetWeaponTier`, it must return the updated value immediately (synchronous in-memory update — the background save write is a separate concern in `Meta`).

`EconomyConfig` must carry all of: `UpgradeCostShardT0T1`, `UpgradeCostShardT1T2`, `UpgradeCostShardT2T3`, `UpgradeCostCoreT1T2`, `UpgradeCostCoreT2T3`, `UpgradeCostEssenceT2T3`, and `WeaponCoreType` map (weapon ID → core material ID). All with `OnValidate` range checks per GDD §G.2 safe ranges.

`ISaveService` must also declare: `SpendMaterials(MaterialId, int)` and `GetWeaponTier(WeaponId)` and `SetWeaponTier(WeaponId, int)` — confirm these are in `Core` before implementing.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: Per-break yield computation (earn side).
- Story 002: Essence award computation.
- Story 003: Inventory credit (earn side); this story handles the spend side only.
- Story 005: TTB matrix guard verifying upgrade effect correctness — this story handles transaction mechanics only.
- UI epic: Upgrade screen display, affordability visual feedback, tier unlock animation, "recommended hunt" pointer.

---

## QA Test Cases

*Written by qa-lead at story creation. The developer implements against these — do not invent new test cases during implementation.*

- **AC-1**: Tier 0→1 succeeds with exactly sufficient shards; fails with one fewer
  - Given: `EconomyConfig` fixture with `UpgradeCostShardT0T1=8`; L1 weapon at Tier 0; stub `ISaveService` with `inventory[shard_common]=8`
  - When: `TryUpgrade(L1, Tier0ToTier1)` called
  - Then: Returns `true`; `SpendMaterials(ShardCommon, 8)` called; `GetWeaponTier(L1)` returns 1; no core or essence deducted
  - Edge cases: `inventory[shard_common]=7` (one fewer) → returns `false`; `SpendMaterials` NOT called; tier unchanged at 0

- **AC-2**: Tier 1→2 requires correct core type per weapon — wrong core type fails
  - Given: L1 (needs `core_carapace`) at Tier 1; `inventory[shard_common]=12`, `inventory[core_carapace]=5`
  - When: `TryUpgrade(L1, Tier1ToTier2)` called
  - Then: Returns `true`; `shard_common` deducted by 12; `core_carapace` deducted by 5; tier → 2
  - Edge cases: L2 (needs `core_limb`) attempted with `inventory[core_carapace]=5`, `inventory[core_limb]=0` → returns `false`; neither shard nor core deducted; tests all 8 weapon→core mappings

- **AC-3**: Tier 2→3 requires all three material types including essence
  - Given: L3 (needs `core_energy`) at Tier 2; `inventory[shard_common]=25`, `inventory[core_energy]=8`, `inventory[essence_kaiju]=1`
  - When: `TryUpgrade(L3, Tier2ToTier3)` called
  - Then: Returns `true`; `shard_common` deducted by 25; `core_energy` deducted by 8; `essence_kaiju` deducted by 1; tier → 3
  - Edge cases: `essence_kaiju=0` with sufficient shards and cores → returns `false`; zero deductions; `essence_kaiju=2` (excess) → succeeds, deducts exactly 1, remaining 1 preserved

- **AC-4**: Cannot skip tiers or re-upgrade
  - Given: L1 at Tier 0; sufficient inventory for any tier
  - When: `TryUpgrade(L1, Tier1ToTier2)` called (skipping Tier 0→1)
  - Then: Returns `false`; zero deductions; tier remains 0
  - Edge cases: L1 already at Tier 3 → any upgrade call returns `false` (no Tier 3→4 transition); L1 at Tier 1 trying Tier 0→1 again → returns `false` (already past from-tier)

- **AC-5**: Partial affordability — atomic all-or-nothing; no partial deduction
  - Given: L2 at Tier 1; `inventory[shard_common]=12` (sufficient), `inventory[core_limb]=3` (need 5 — insufficient)
  - When: `TryUpgrade(L2, Tier1ToTier2)` called
  - Then: Returns `false`; `shard_common` NOT deducted (still 12); `core_limb` NOT deducted (still 3); tier unchanged — atomic failure
  - Edge cases: All three material types unaffordable → same result; first check to fail causes immediate `false` with no side effects

- **AC-6**: `IWeaponTierQuery.GetTier` returns new tier in same frame
  - Given: L1 at Tier 0; successful upgrade conditions met
  - When: `TryUpgrade(L1, Tier0ToTier1)` returns `true`
  - Then: Immediate call to `IWeaponTierQuery.GetTier(L1)` in the same frame returns 1 — no async wait required
  - Edge cases: Confirm `IWeaponTierQuery` is not backed by a file read (must be backed by in-memory state updated synchronously)

- **AC-7**: Cost values sourced from `EconomyConfig` — config change propagates without code change
  - Given: `EconomyConfig` fixture with `UpgradeCostShardT0T1=4` (non-default); `inventory[shard_common]=4`; L1 at Tier 0
  - When: `TryUpgrade(L1, Tier0ToTier1)` called
  - Then: Returns `true` using cost 4, not the GDD default of 8 — confirms config-driven costs

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/Economy/economy_upgrade_transaction_test.cs` — must exist and all cases must pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 003 DONE (`ISaveService` with `CreditMaterials`, `SpendMaterials`, `GetWeaponTier`, `SetWeaponTier` declared; inventory credits established); `IWeaponTierQuery` interface declared in `Core`; `EconomyConfig` has all upgrade cost fields and `WeaponCoreType` mapping with `OnValidate` range checks
- Unlocks: Story 005 (anti-degenerate guard uses `WeaponDef` Tier effects as test inputs; upgrade transaction must be correct first); UI epic (upgrade screen affordability display); `Weapons` system Tier-effect activation (reads `IWeaponTierQuery`)
