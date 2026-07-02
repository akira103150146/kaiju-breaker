# Story 005: Adjacency Graph Load & Tier-3 Chain Consumers

> **Epic**: 可破壞部位系統
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/kaiju-part-system.md`
**Requirement**: `TR-part-006`
*(TR-ID derived from GDD §H.6 — registry not yet formalised)*

**ADR Governing Implementation**: ADR-0003: 資料驅動調校 (primary); ADR-0002: 事件架構 (secondary)
**ADR Decision Summary**: `KaijuDef` SO declares adjacency edges per-part; `PartStateSystem.InitializeParts` builds an undirected graph (`Dictionary<string, HashSet<string>>`) at load time, then replaces each `part.AdjacencyList` with the bidirectional result. Graph is cached for O(1) neighbor lookup. M3 Tier-3 chain-break is handled **within** `PartStateSystem`: it subscribes to `PartBroke` and, when `IsChainBreak == false`, applies chain damage to up to `m3_t3_chain_max_targets` alive neighbors, calling `TriggerPartBreak(target, isChainBreak: true)` for knockon breaks — strictly non-recursive. L2 Tier-3 heat-ripple is consumed by the `Weapons` system after receiving `PartBroke`; KaijuParts processes the resulting `LaserHit` events normally through Story 002's `HandleLaserHit`.

**Engine**: Unity 6 | **Risk**: LOW
**Engine Notes**: `Dictionary<string, HashSet<string>>` is standard BCL; no Unity post-cutoff API risk. Graph built once per `InitializeParts` call; no per-frame allocation. `HashSet` deduplication avoids double-edge entries from symmetric declarations.

**Control Manifest Rules (this layer — KaijuParts)**:
- Required: adjacency graph is undirected — each declared edge A→B registers both A's neighbor set and B's neighbor set (§C.6 GDD)
- Required: `adjacency_max_neighbors` from `PartSystemConfig` enforced at graph-build time; excess neighbors silently capped (first N accepted) (§G.3 GDD)
- Required: M3 T3 chain is non-recursive — `PartBroke.IsChainBreak == true` skips chain dispatch entirely (§E.4 GDD, §3 KaijuParts manifest)
- Required: M3 T3 targets at most `m3_t3_chain_max_targets` (≤ 2) alive (non-BROKEN) neighbors; BROKEN neighbors skipped (§D.6 GDD)
- Required: ARMOR_INTACT neighbor deflects M3 chain — `LookupStateMult` returns 0, B_fill = 0 (§D.6 GDD)
- Required: `PartBroke.AdjacencyList` reflects bidirectional alive neighbors at break time (Story 004 reads from `part.AdjacencyList` which this story populates via graph build) (§3 KaijuParts manifest)
- KaijuParts OWNS all `PartBroke` emissions including chain-triggered ones; Weapons MUST NOT publish `PartBroke` (§4.2)
- Forbidden: recursive chain processing; Forbidden: M3 chain operating on `is_chain_break=true` events

---

## Acceptance Criteria

*From GDD §H.6, §D.5, §D.6, §C.6, §E.3, §E.4, scoped to this story:*

- [ ] C.6 — Adjacency graph built bidirectionally: if `PartDef(A).adjacency = ["B"]`, then `graph["A"]` contains B and `graph["B"]` contains A; duplicate declarations (A→B and B→A both declared) produce exactly one entry in each set
- [ ] C.6 — `adjacency_max_neighbors` cap enforced per part: if `PartDef` declares 5 neighbors and cap is 4, only first 4 are registered; `part.AdjacencyList` reflects the capped list
- [ ] C.6 — `part.AdjacencyList` for all parts updated to bidirectional graph result after `InitializeParts`; Story 004's `TriggerPartBreak` reads this updated list for `PartBroke.AdjacencyList`
- [ ] H.6 / D.6 — M3 T3 chain: on `PartBroke` with `IsChainBreak=false`, select up to `m3_t3_chain_max_targets` alive neighbors; for each: `B_chain = m3_t3_chain_dmg_mult × 10 × LookupStateMult(target)`; apply to `B_current`; if threshold reached → `TriggerPartBreak(target, isChainBreak: true)`
- [ ] H.6 / E.4 — Chain non-recursion: when `OnPartBroke` handler receives `IsChainBreak=true`, immediately return without dispatching M3 chain; confirm via test that A's neighbors are NOT damaged when A breaks via chain
- [ ] H.6 — ARMOR_INTACT neighbor: M3 chain `B_fill = 0` (LookupStateMult = 0); `B_current` unchanged
- [ ] H.6 / D.5 — L2 T3 heat ripple: after `PartBroke` published (with `AdjacencyList`), Weapons system fires `LaserHit(adj_id, heat_delta = adj.H_max × l2_t3_adjacent_heat_pct)` for each alive neighbor; KaijuParts processes via standard `HandleLaserHit`; if resulting `H_current >= theta_S` → `PartSoftened` fires in the same frame (§E.3 GDD)
- [ ] E.3 — If L2 heat ripple brings neighbor Q to `theta_S` in the same frame as `PartBroke`, `PartSoftened(Q)` is published in that same frame — not deferred
- [ ] H.6 — BROKEN neighbors skipped in M3 chain target selection (`GetAliveNeighbors` filters `break_state == BROKEN`)

---

## Implementation Notes

*Derived from ADR-0003 and ADR-0002:*

- `BuildAdjacencyGraph(KaijuDef kaijuDef)` — called at end of `InitializeParts` (after all parts created):
  ```csharp
  var graph = new Dictionary<string, HashSet<string>>();
  foreach (var partDef in kaijuDef.Parts)
  {
      if (!graph.ContainsKey(partDef.Id)) graph[partDef.Id] = new HashSet<string>();
      var neighbors = partDef.Adjacency.Take(_config.AdjacencyMaxNeighbors);
      foreach (var neighborId in neighbors)
      {
          graph[partDef.Id].Add(neighborId);
          if (!graph.ContainsKey(neighborId)) graph[neighborId] = new HashSet<string>();
          graph[neighborId].Add(partDef.Id);    // bidirectional
      }
  }
  // Update each part's AdjacencyList from graph
  foreach (var (id, neighbors) in graph)
      _parts[id].AdjacencyList = neighbors.ToArray();
  ```
- Subscribe to `PartBroke` on `IEventBus` **within** `PartStateSystem` for M3 chain handler:
  ```csharp
  void OnPartBroke(in PartBroke evt)
  {
      if (evt.IsChainBreak) return;     // NON-RECURSIVE GUARD — mandatory
      ApplyM3Chain(evt.PartId);
  }
  void ApplyM3Chain(string brokenPartId)
  {
      var targets = GetAliveNeighbors(brokenPartId)
                      .Take(_config.M3ChainMaxTargets);
      foreach (var target in targets)
          ApplyChainDamage(target);
  }
  void ApplyChainDamage(BreakablePart target)
  {
      float bChain = _config.M3ChainDmgMult * 10f * LookupStateMult(target);
      target.BCurrent = Mathf.Clamp(target.BCurrent + bChain, 0f, target.BMax);
      if (target.BCurrent >= GetBreakThreshold(target.PartType))
          TriggerPartBreak(target, isChainBreak: true);
  }
  ```
- `GetAliveNeighbors(string partId)` — returns `_parts[adj_id]` for each `adj_id` in `_parts[partId].AdjacencyList` where `break_state != BROKEN`.
- `LookupStateMult` and `TriggerPartBreak` are reused from Story 004 — no duplication; these are methods on `PartStateSystem`.
- L2 T3 heat ripple: this story does NOT implement the L2 ripple logic — that belongs to the Weapons system, which reads `PartBroke.AdjacencyList` and publishes `LaserHit` for each alive neighbor. This story ensures `AdjacencyList` in the payload is correctly populated (bidirectional graph result) and that `HandleLaserHit` from Story 002 processes the resulting events, including same-frame `PartSoftened` if heat threshold crossed.
- Integration test for L2 ripple: wire `FakeWeaponsSystem` stub that publishes `LaserHit` events on `PartBroke` receipt; verify `PartSoftened` fires for neighbor.
- Tests: fixture `KaijuDef` with 3-part graph (A—B—C chain); verify chain max-targets stops at 2, non-recursion, and ARMOR_INTACT deflection; use `FakeEventBus` to record event sequence.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: `KaijuDef.PartDef.adjacency` field declaration; `PartSystemConfig.adjacency_max_neighbors` knob definition; raw `AdjacencyList` field on `BreakablePart` (populated with KaijuDef raw values in Story 001; this story replaces with bidirectional values)
- Story 002: `HandleLaserHit` implementation — reused here; L2 heat ripple calls it but does not change it
- Story 004: `TriggerPartBreak`, `LookupStateMult`, and `PartBroke` emission — reused here by M3 chain handler
- Weapons epic: L2/M3 weapon-tier detection, L2 heat-ripple `LaserHit` publishing, M3 chain invocation flag — those are Weapons system responsibilities

---

## QA Test Cases

*Written by qa-lead at story creation. Implement against these — do not invent new test cases during implementation.*

- **AC-1**: Adjacency graph built bidirectionally; no duplicate edges
  - Given: `KaijuDef` with `PartDef(A).adjacency=["B"]`; `PartDef(B).adjacency=["A","C"]`; `PartDef(C).adjacency=[]`
  - When: `InitializeParts` (and thus `BuildAdjacencyGraph`) runs
  - Then: `parts["A"].AdjacencyList` = {B}; `parts["B"].AdjacencyList` = {A, C}; `parts["C"].AdjacencyList` = {B}; no duplicates in any set
  - Edge cases: `PartDef(A).adjacency=["B","B"]` (double-declared) → `parts["A"].AdjacencyList` = {B} (deduplicated); C not reached via B's original declaration still gets B entry via reverse-edge

- **AC-2**: `adjacency_max_neighbors` cap enforced
  - Given: `PartDef(A).adjacency=["B","C","D","E","F"]`; `adjacency_max_neighbors=4`
  - When: graph built
  - Then: `parts["A"].AdjacencyList` has exactly 4 entries (B, C, D, E); F not registered from A's side
  - Edge cases: `adjacency_max_neighbors=0` → no neighbors for any part; M3 chain produces 0 targets

- **AC-3**: M3 T3 chain targets up to 2 alive neighbors
  - Given: part P breaks (`IsChainBreak=false`); P has alive neighbors A (NORMAL), B (NORMAL), C (NORMAL) in `AdjacencyList`; `m3_t3_chain_max_targets=2`; all parts have `B_current=0`, threshold=100, mult=0.35
  - When: `OnPartBroke` M3 handler fires
  - Then: A and B each receive `B_chain = 1.5×10×0.35 = 5.25 BU`; C receives nothing
  - Edge cases: only 1 alive neighbor → 1 target processed; 0 alive neighbors → no-op, no error

- **AC-4**: M3 chain non-recursive — `IsChainBreak=true` prevents re-chain
  - Given: chain causes neighbor A to reach break threshold → `TriggerPartBreak(A, isChainBreak: true)` → publishes `PartBroke(A, IsChainBreak=true)`; A has alive neighbors B and C
  - When: `OnPartBroke` handler receives `PartBroke(A)` with `IsChainBreak=true`
  - Then: handler returns immediately at guard; B and C receive NO M3 chain damage; `FakeEventBus` shows no further `PartBroke` events for B or C
  - Edge cases: verify `PartBroke(A).IsChainBreak == true` in recorded events; verify B.BCurrent unchanged after A's chain break

- **AC-5**: ARMOR_INTACT neighbor deflects M3 chain BU
  - Given: part P breaks; neighbor B is ARMORED with `armor_state=ARMOR_INTACT`, `B_current=0`; `m3_t3_chain_dmg_mult=1.5`, chain_damage_base=15
  - When: M3 chain targets B: `LookupStateMult(B) = 0`
  - Then: `B_chain = 15 × 0 = 0`; `B.B_current` unchanged at 0; no `PartBroke` for B
  - Edge cases: B with `ARMOR_STRIPPED` (stagger>0) → `LookupStateMult=1.5`; `B_chain=22.5`; B_current increases

- **AC-6**: L2 T3 heat ripple triggers same-frame `PartSoftened` on neighbor (integration path)
  - Given: part P breaks; neighbor Q has `H_current=70`, `H_max=100`, `l2_t3_adjacent_heat_pct=0.30`; `theta_S=100`; `FakeWeaponsSystem` publishes `LaserHit(Q, heat_delta=30)` synchronously on `PartBroke` receipt
  - When: `HandleLaserHit(Q, heat_delta=30)` processes within same frame
  - Then: `Q.H_current = clamp(70+30, 0, 100) = 100`; `100 >= theta_S` → `PartSoftened(Q)` published; all events in same frame in `FakeEventBus` recorded list
  - Edge cases: heat_pulse clamped — `Q.H_current=95`, pulse=30 → `H_current=100` (not 125); if Q.H_current=80 and pulse=15 → 95 < theta_S=100 → no `PartSoftened`

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/KaijuParts/EditMode/kaijuparts_adjacency_chain_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (`KaijuDef` SO with adjacency fields, `PartSystemConfig` knobs, raw `AdjacencyList` on `BreakablePart`) must be DONE; Story 004 (`TriggerPartBreak`, `LookupStateMult`, `PartBroke` event) must be DONE
- Unlocks: Story 006 (full event pipeline including chain effects operational; readability verification can run end-to-end)
