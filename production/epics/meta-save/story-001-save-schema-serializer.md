# Story 001: SaveData Schema & Canonical JSON Serializer

> **Epic**: 元進度與存檔系統
> **Status**: ✅ Complete (2026-07-06 — 11/11 EditMode GREEN, part of 307-case suite)
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: 3h
> **Manifest Version**: 2026-07-02
> **Last Updated**: 2026-07-06

## Context

**GDD**: `design/gdd/meta-progression-system.md`
**Requirement**: `TR-meta-004`, `TR-meta-005`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time. Note: tr-registry.yaml not yet formalized; TR-IDs inferred from GDD §H.)*

**ADR Governing Implementation**: ADR-0004: 存檔系統（JSON + 原子寫入 + CRC32）
**ADR Decision Summary**: Single-slot JSON save at `Application.persistentDataPath`; serialization uses a deterministic canonical form (keys alphabetically sorted, no whitespace, fixed-format floats) because CRC32 correctness and regression tests depend on byte-identical output across platforms. `JsonUtility` dictionary support is unverified — serializer choice is deferred to implementation [需查證 6.3 API].

Secondary ADR: ADR-0002 (IEventBus / ISaveService interface placement in `Core`)

**Engine**: Unity 6.3 | **Risk**: MEDIUM
**Engine Notes**:
- `JsonUtility` does not natively support `Dictionary<K,V>` — the nested `weapons{}`, `materials{}`, `kaiju_records{}` maps likely require explicit DTO structs OR `System.Text.Json` / Newtonsoft.Json via Package Manager. **Verify before choosing a serializer** [需查證 6.3 API]; document the choice in the implementation commit.
- Canonical float formatting must be round-trip safe across .NET runtime versions (use `R` or `G9` format specifiers; verify output determinism).

**Control Manifest Rules (Feature layer — Meta)**:
- Required: `ISaveService` interface belongs in `KaijuBreaker.Core`; no implementation logic in `Core`
- Required: All balance/config values (starting weapons, max migration generations) in `SaveConfig` ScriptableObject under `Assets/_Project/Content/`; `OnValidate` range checks enforced
- Required: PascalCase for all classes, methods, properties; private fields `_camelCase`
- Forbidden: No `PlayerPrefs`, `BinaryFormatter`, magic numbers, or hardcoded weapon IDs in code
- Forbidden: Meta assembly must not reference any other Feature assembly; only `Core` + `Content`

---

## Acceptance Criteria

*From GDD `design/gdd/meta-progression-system.md` §C.3 + §D.2, scoped to this story:*

- [ ] `SaveData` C# class (and nested types: `WeaponSaveData`, `KaijuRecordData`, `MetaSaveData`, `SettingsData`, `StatsData`) defined with all fields matching GDD §C.3 schema exactly — field names, types, and valid ranges per the field spec table
- [ ] `SaveConfig` ScriptableObject in `Content` exposes all §G tuning knobs (`StartingOwnedWeapons`, `SaveBackupEnabled`, `SaveMaxMigrationGenerations`, `SaveWorkerIdleMs`, `IntegrityAlgorithm`, `IntegrityFailAction`, `SaveAsyncQueueDepth`, `ActiveWeaponIds`, `ActiveKaijuIds`) with `OnValidate` range assertions
- [ ] `ISaveService` interface in `KaijuBreaker.Core` declares read-query methods: `IsWeaponOwned(WeaponId)`, `GetWeaponTier(WeaponId)`, `GetMaterialCount(MaterialType)`, `GetKaijuRecord(string kaijuId)`, `GetLastLoadout()`, `GetLastDifficulty()`, `GetSettings()`
- [ ] `ICanonicalSerializer` interface in `KaijuBreaker.Core` declares `string Serialize(SaveData data)` and `SaveData Deserialize(string json)`
- [ ] `CanonicalJsonSerializer` in `KaijuBreaker.Meta` implements `ICanonicalSerializer`: all JSON object keys sorted alphabetically at every nesting level; no indentation or extraneous whitespace; floats serialized in round-trip format; `null` values for `best_time_per_difficulty` entries encoded as JSON `null`
- [ ] `CRC32Calculator` static utility in `KaijuBreaker.Meta` (or `Core`): IEEE 802.3 polynomial; input UTF-8 string; output 8-character uppercase hex string (e.g. `"A4B3C2D1"`)
- [ ] Canonical serializer regression: serialize the §C.3 example `SaveData` object → output matches a pre-computed reference JSON string byte-for-byte (guards against serializer drift)

---

## Implementation Notes

*Derived from ADR-0004 Decision §1 and control-manifest.md §3 Meta rules:*

**DTO design**: Model each JSON object as a dedicated C# class (not anonymous dict). Nested dictionaries in the GDD schema (`weapons{}`, `materials{}`, `kaiju_records{}`) are defined as fixed-key structs or arrays of key-value pairs, depending on the chosen serializer's capabilities. All fields public with `{ get; set; }` or public fields — required by most Unity-compatible serializers.

**Serializer selection** (defer final choice to implementation, document in commit):
- Option A: `System.Text.Json` with a custom `JsonSerializerOptions` that sorts keys — available in Unity 6 via .NET 8 runtime; confirm package availability [需查證 6.3 API].
- Option B: Newtonsoft Json.NET via Unity Package Manager — proven, full dictionary support, custom `IContractResolver` for sorted keys.
- Option C: Hand-rolled serializer for the fixed-schema `SaveData` — guarantees canonical form without external dependencies; higher initial cost but zero runtime overhead.
- **MUST NOT** use `JsonUtility` for dictionary fields without verification.

**Canonical form requirements (CRC32 correctness depends on these)**:
```
- All object keys sorted alphabetically (A–Z, case-sensitive, lowercase before uppercase)
- No whitespace between tokens (compact form)
- Floats: use round-trip format; never scientific notation in the range [0.0, ∞)
- null values: literal JSON null (not omitted)
- Boolean: lowercase true / false
- Integers: no leading zeros
```

**CRC32**: Use IEEE 802.3 (same polynomial as zlib / Ethernet). The `integrity_hash` field stores the hex of the CRC32 over the canonical JSON of `SaveData` with `integrity_hash` excluded (GDD §D.2 formula). The field is computed during the write sequence — not during model initialization.

**Assembly placement**:
- `SaveData`, `WeaponSaveData`, `KaijuRecordData`, `MetaSaveData`, `SettingsData`, `StatsData` → `KaijuBreaker.Meta` (implementation types)
- `ISaveService`, `ICanonicalSerializer` → `KaijuBreaker.Core` (contracts only, no implementation)
- `SaveConfig` → `KaijuBreaker.Content` (ScriptableObject, no logic)

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: File I/O, temp-then-rename write sequence, Save Worker thread
- Story 003: CRC32 hash embedding into write, load flow, corruption recovery
- Story 004: Version migration chain, `MIGRATIONS[]` registry
- Story 005: New-game default initialization, last_loadout fallback at load time
- Story 006: Event bus subscriptions, material credit, `EnqueueSave` implementation
- Story 007: Weapon ownership state machine, `WeaponPodPickup` handler

---

## QA Test Cases

*Authored by lead-programmer (lean mode — no qa-lead spawn). Developer implements against these.*

**AC-1**: Canonical serializer produces alphabetically ordered keys at all nesting levels

- Given: a `SaveData` object constructed with fields in arbitrary insertion order
- When: `ICanonicalSerializer.Serialize(saveData)` is called
- Then: the output JSON string has all keys within each object sorted A–Z
- Edge cases: nested objects (`weapons`, `materials`, `kaiju_records`, `meta`, `settings`, `stats`) each independently sorted; `kaiju_records[id]` sub-object keys also sorted

**AC-2**: Canonical serializer is deterministic across multiple calls and platforms

- Given: the same `SaveData` object (same field values)
- When: `Serialize()` called twice in the same run, and again after a domain reload
- Then: byte-identical output on all three calls
- Edge cases: float values `1.0`, `0.12`, `185.3` — verify no trailing zeros differ; `null` best_time entries must serialize as JSON `null` not `"null"` string

**AC-3**: CRC32 produces reference value for known input

- Given: the canonical JSON string from GDD §D.2 example (simplified `{"materials":{"core_carapace":5,...},"version":1,...}`)
- When: `CRC32Calculator.Compute(jsonString)` called
- Then: output equals the pre-computed reference hex string (record this reference in the test fixture)
- Edge cases: empty string input; single-character input; known CRC32 test vector `"123456789"` → `"CBF43926"` (IEEE 802.3 standard vector)

**AC-4**: `SaveConfig.OnValidate` rejects out-of-range values in the editor

- Given: a `SaveConfig` asset
- When: `SaveAsyncQueueDepth` set to 0 (below minimum 1)
- Then: `OnValidate` throws or logs an error; asset does not save with invalid value
- Edge cases: `SaveMaxMigrationGenerations` below 2 or above 5; `SaveWorkerIdleMs` below 50 or above 500

**AC-5**: Round-trip serialize/deserialize preserves all field values

- Given: a fully-populated `SaveData` matching the GDD §C.3 example
- When: serialized to JSON string then deserialized back to `SaveData`
- Then: all fields equal the originals (weapons tier/owned, all 5 material counts, 3 kaiju records with full sub-fields, meta, settings, stats)
- Edge cases: `best_time_per_difficulty` entries with `null` values survive round-trip as null (not 0 or missing)

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/_Project/Tests/Meta/save_schema_serializer_test.cs` — must exist and all tests pass
*(ADR-0005 overrides coding-standards path: Unity Test Framework requires tests inside `Assets/_Project/Tests/<Module>/`; EditMode test assembly.)*

**Status**: [x] ✅ 11/11 GREEN (`Assets/_Project/Tests/EditMode/Meta/save_schema_serializer_test.cs`, Unity MCP, 2026-07-06). Covers AC-1 (sorted keys), AC-2 (determinism), AC-3 (CRC32 std vector CBF43926 + edges), AC-4 (SaveConfig.OnValidate ranges via reflection+LogAssert), AC-5 (round-trip incl null best-time), AC-6 (byte-exact canonical reference).

**Reconciliations vs story text** (surfaced for review):
1. **`ICanonicalSerializer` lives in `KaijuBreaker.Meta`, not Core** — it references `SaveData` (a Meta type) and Core is the zero-dependency base (ADR-0005); Core cannot reference Meta. Interface + data type sit together in Meta; nothing outside Meta consumes it.
2. **`ISaveService` read-query surface NOT (re)declared here** — the committed `ISaveService` (economy epic) already has a different, in-use surface. Extending it with `IsWeaponOwned`/`GetKaijuRecord`/`GetLastLoadout`/etc. belongs to the Meta service implementation (Story 006), added additively there. Story 001's actual unlocking value = schema + canonical serializer + CRC32 (Story 002/003 depend on those, not on ISaveService reads).
3. **Serializer = hand-rolled (ADR-0004 Option C)** — dependency-free, guarantees canonical form. Canonical floats use invariant round-trip `"R"` → integral values render as `1` (not `1.0`). `SaveData` uses `long` for materials/stats (overflow safety, §H.2).
4. `SaveConfig` extended with §G knobs: `IntegrityAlgorithm`, `SaveAsyncQueueDepth` [1,3], `SaveWorkerIdleMs` [50,500], `SaveBackupEnabled`, `SaveMaxMigrationGenerations` [2,5], `StartingOwnedWeapons`, `ActiveWeaponIds`, `ActiveKaijuIds`.

---

## Dependencies

- Depends on: None — this is the foundational story for the entire epic
- Unlocks: Story 002 (atomic write uses CanonicalSerializer + CRC32Calculator), Story 003 (load uses ICanonicalSerializer + Deserialize), all other Meta stories
