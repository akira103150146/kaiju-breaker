using System;
using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.KaijuParts
{
    /// <summary>
    /// The authoritative dual-track soften→break state machine (kaiju-part-system.md).
    /// Owns every <see cref="BreakablePart"/> for the active kaiju, consumes the weapon
    /// hit events (<see cref="LaserHit"/> heat / <see cref="WaveHit"/> armor-strip+stagger /
    /// <see cref="MissileHit"/> break), and is the SOLE emitter of the part-state and break
    /// events (<see cref="PartSoftened"/>, <see cref="PartSoftenedExit"/>, <see cref="PartStaggered"/>,
    /// <see cref="PartStaggerEnd"/>, <see cref="PartBroke"/>, <see cref="BossCoreBroke"/>).
    /// Implements <see cref="IPartStateQuery"/> for Weapons/UI/GameFeel reads.
    ///
    /// This is the combat-chain hub (ADR-0002 §3): break_quality is computed on the break
    /// frame and carried in the payload so no consumer must re-query. Breaks are irreversible;
    /// parts never regenerate within a run.
    ///
    /// Tuning is data-driven (ADR-0003): heat/break/stagger knobs come from
    /// <see cref="WeaponBalanceConfig"/> (single source), part/chain/adjacency knobs from
    /// <see cref="PartSystemConfig"/>. No hardcoded balance values, no DOTS types (§5 manifest).
    ///
    /// Threading: main-thread only. Call <see cref="Tick"/> once per scaled-time game frame.
    /// </summary>
    public sealed class PartStateSystem : IPartStateQuery, IDisposable
    {
        private readonly IEventBus _bus;
        private readonly WeaponBalanceConfig _balance;
        private readonly PartSystemConfig _partConfig;

        private readonly Dictionary<int, BreakablePart> _parts = new Dictionary<int, BreakablePart>(16);
        private readonly Dictionary<string, int> _partIdByName = new Dictionary<string, int>(16);
        private readonly Dictionary<string, int> _dropTableIdByName = new Dictionary<string, int>(16);

        // Heat deltas accumulated this frame per part; applied and cleared in TickHeat so that
        // fill and decay are mutually exclusive per frame (kaiju-part-system.md D.1).
        private readonly Dictionary<int, float> _pendingHeatDeltas = new Dictionary<int, float>(16);

        // Per-part break-gauge regen (per-part-firing-schema.md §5 / TIDEMAW). Disabled for parts whose
        // PartDef.ArmorRegen.Enabled is false (the default) — so existing kaiju are unaffected.
        private readonly Dictionary<int, KaijuBreaker.Content.ArmorRegen> _armorRegen = new Dictionary<int, KaijuBreaker.Content.ArmorRegen>(16);
        private readonly Dictionary<int, float> _timeSinceBreakHit = new Dictionary<int, float>(16);

        // Cross-part gate (per-part-firing-schema.md §4): another part's state gates this part's
        // hittability (HittableWhen) or breakability (BreakableWhen). Resolved at InitializeParts;
        // only gated parts are stored, so ungated kaiju are unaffected.
        private struct GateRuntime { public PartGateKind Kind; public PartGateCond Cond; public int[] GateParts; public bool RequireAll; }
        private readonly Dictionary<int, GateRuntime> _gates = new Dictionary<int, GateRuntime>(8);

        private readonly Action<LaserHit> _onLaserHit;
        private readonly Action<WaveHit> _onWaveHit;
        private readonly Action<MissileHit> _onMissileHit;
        private readonly Action<PartBroke> _onPartBroke;

        public PartStateSystem(IEventBus bus, WeaponBalanceConfig balance, PartSystemConfig partConfig)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _balance = balance ? balance : throw new ArgumentNullException(nameof(balance));
            _partConfig = partConfig ? partConfig : throw new ArgumentNullException(nameof(partConfig));

            _onLaserHit = HandleLaserHit;
            _onWaveHit = HandleWaveHit;
            _onMissileHit = HandleMissileHit;
            _onPartBroke = OnPartBroke;

            _bus.Subscribe(_onLaserHit);
            _bus.Subscribe(_onWaveHit);
            _bus.Subscribe(_onMissileHit);
            _bus.Subscribe(_onPartBroke); // M3 Tier-3 chain handler (kaiju-part-system.md E.4)
        }

        public void Dispose()
        {
            _bus.Unsubscribe(_onLaserHit);
            _bus.Unsubscribe(_onWaveHit);
            _bus.Unsubscribe(_onMissileHit);
            _bus.Unsubscribe(_onPartBroke);
        }

        /// <summary>Live view of the parts (read-only) — for tests and diagnostics.</summary>
        public IReadOnlyDictionary<int, BreakablePart> Parts => _parts;

        /// <summary>Runtime int id for a part's authored string id (−1 if unknown).</summary>
        public int GetPartId(string partName) => _partIdByName.TryGetValue(partName, out int id) ? id : -1;

        // ── Load / reset (Story 001, Story 005 graph build) ──────────────────────

        /// <summary>
        /// (Re)initialise all parts for a kaiju to a fresh ALIVE state and build the
        /// bidirectional adjacency graph. Calling it again (new round) fully resets state —
        /// no carry-over from prior BROKEN parts (kaiju-part-system.md H.5). The caller
        /// supplies the runtime <paramref name="kaijuId"/> int (the SO carries a string id).
        /// </summary>
        public void InitializeParts(KaijuDef def, int kaijuId)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));

            _parts.Clear();
            _partIdByName.Clear();
            _dropTableIdByName.Clear();
            _pendingHeatDeltas.Clear();
            _armorRegen.Clear();
            _timeSinceBreakHit.Clear();
            _gates.Clear();

            var parts = def.Parts;
            for (int i = 0; i < parts.Length; i++)
            {
                PartDef pd = parts[i];
                if (pd == null) continue;

                int id = i; // stable within this kaiju load — part id = declaration index
                float hMax = pd.HMaxUseOverride ? pd.HMaxOverride : GlobalHMax(pd.PartType);
                float bMax = pd.BMaxUseOverride ? pd.BMaxOverride : GlobalBMax(pd.PartType);
                int dropTableId = ResolveDropTableId(pd.DropTableId);

                var part = new BreakablePart(
                    id, pd.PartId, kaijuId, pd.PartType, hMax, bMax,
                    dropTableId, pd.DropTableId, pd.Adjacency);

                _parts[id] = part;
                if (!string.IsNullOrEmpty(pd.PartId))
                    _partIdByName[pd.PartId] = id;
                if (pd.ArmorRegen.Enabled)
                {
                    _armorRegen[id] = pd.ArmorRegen;
                    _timeSinceBreakHit[id] = 0f;
                }
            }

            BuildAdjacencyGraph();
            BuildGates(def);
        }

        /// <summary>
        /// Advance all per-frame timers for one scaled-time game frame: heat fill/decay +
        /// soften evaluation, then stagger countdown. Hit events are handled synchronously
        /// as they arrive (mid-frame); this drives the time-based decay/countdown only.
        /// </summary>
        public void Tick(float deltaTime)
        {
            TickHeat(deltaTime);
            TickStagger(deltaTime);
            TickArmorRegen(deltaTime);
        }

        /// <summary>Assign the part's world position (called by the scene wiring / Stage). Defaults to zero.</summary>
        public void SetWorldPosition(int partId, Vector2 worldPosition)
        {
            if (_parts.TryGetValue(partId, out var part)) part.WorldPosition = worldPosition;
        }

        private float GlobalHMax(PartType type) => type switch
        {
            PartType.Armored => _balance.HMaxArmored,
            PartType.BossCore => _balance.HMaxBossCore,
            _ => _balance.HMaxNormal
        };

        private float GlobalBMax(PartType type) => type switch
        {
            PartType.Armored => _balance.BMaxArmored,
            PartType.BossCore => _balance.BMaxBossCore,
            _ => _balance.BMaxNormal
        };

        private float GetBreakThreshold(PartType type) => type switch
        {
            PartType.Armored => _balance.RequiredBreakThresholdArmored,
            PartType.BossCore => _balance.RequiredBreakThresholdBossCore,
            _ => _balance.RequiredBreakThresholdNormal
        };

        // Map each distinct authored drop-table string to a stable positive int (0 = none/empty).
        private int ResolveDropTableId(string dropTableName)
        {
            if (string.IsNullOrEmpty(dropTableName)) return 0;
            if (_dropTableIdByName.TryGetValue(dropTableName, out int id)) return id;
            id = _dropTableIdByName.Count + 1;
            _dropTableIdByName[dropTableName] = id;
            return id;
        }

        // ── Adjacency graph (Story 005) ──────────────────────────────────────────

        private void BuildAdjacencyGraph()
        {
            var graph = new Dictionary<int, HashSet<int>>(_parts.Count);
            foreach (var part in _parts.Values)
                graph[part.Id] = new HashSet<int>();

            int cap = _partConfig.AdjacencyMaxNeighbors;
            foreach (var part in _parts.Values)
            {
                int registered = 0;
                var names = part.AdjacencyNames;
                for (int i = 0; i < names.Length && registered < cap; i++)
                {
                    if (!_partIdByName.TryGetValue(names[i], out int neighborId)) continue; // unknown name — skip
                    if (neighborId == part.Id) continue;                                    // no self-edges
                    graph[part.Id].Add(neighborId);
                    graph[neighborId].Add(part.Id); // bidirectional
                    registered++;
                }
            }

            foreach (var part in _parts.Values)
            {
                var set = graph[part.Id];
                var ids = new int[set.Count];
                set.CopyTo(ids);
                Array.Sort(ids); // deterministic chain-target ordering
                part.AdjacencyIds = ids;
            }
        }

        // ── Cross-part gate (per-part-firing-schema.md §4) ───────────────────────

        /// <summary>
        /// Resolve each gated part's authored gate-part names to runtime ids (after the id map is built).
        /// A gate whose source parts don't resolve is dropped (treated as ungated) rather than left permanently
        /// closed, so a bad id can't soft-lock a boss. Only gated parts are stored — ungated kaiju pay nothing.
        /// </summary>
        private void BuildGates(KaijuDef def)
        {
            var parts = def.Parts;
            for (int i = 0; i < parts.Length; i++)
            {
                PartDef pd = parts[i];
                if (pd == null || pd.GateKind == PartGateKind.None) continue;
                if (!_partIdByName.TryGetValue(pd.PartId, out int selfId)) continue;

                var srcNames = pd.GatePartIds;
                var resolved = new List<int>(srcNames.Length);
                for (int j = 0; j < srcNames.Length; j++)
                    if (_partIdByName.TryGetValue(srcNames[j], out int gid) && gid != selfId && !resolved.Contains(gid))
                        resolved.Add(gid);
                if (resolved.Count == 0) continue; // no resolvable gate part → ungated (never soft-lock)

                _gates[selfId] = new GateRuntime
                {
                    Kind = pd.GateKind,
                    Cond = pd.GateCond,
                    GateParts = resolved.ToArray(),
                    RequireAll = pd.RequireAllGates
                };
            }
        }

        /// <summary>
        /// True when a part's cross-part gate condition is currently satisfied (or it has no gate). Evaluated
        /// live per hit, so gates keyed on transient states (softened / armor-stripped) open and close dynamically.
        /// </summary>
        private bool IsGateOpen(int partId)
        {
            if (!_gates.TryGetValue(partId, out var g)) return true; // ungated
            bool all = true, any = false;
            for (int i = 0; i < g.GateParts.Length; i++)
            {
                if (!_parts.TryGetValue(g.GateParts[i], out var gp)) { all = false; continue; }
                bool sat = g.Cond switch
                {
                    PartGateCond.GatePartBroken => gp.BreakState == BreakState.Broken,
                    PartGateCond.GatePartArmorStripped => gp.ArmorState == ArmorState.Stripped,
                    PartGateCond.GatePartSoftened => gp.HeatState == HeatState.Softened,
                    _ => false
                };
                if (sat) any = true; else all = false;
            }
            return g.RequireAll ? all : any;
        }

        // A closed HittableWhen gate makes the part inert to EVERY hit (laser/wave/missile/chain); a closed
        // BreakableWhen gate blocks only break-track fill (missile + chain), letting laser heat still soften it.
        // Both default open (no gate) so existing kaiju are unaffected.
        private bool IsHitBlocked(int partId)
            => _gates.TryGetValue(partId, out var g) && g.Kind == PartGateKind.HittableWhen && !IsGateOpen(partId);

        private bool IsBreakBlocked(int partId)
            => _gates.TryGetValue(partId, out var g)
               && (g.Kind == PartGateKind.HittableWhen || g.Kind == PartGateKind.BreakableWhen)
               && !IsGateOpen(partId);

        /// <summary>False while a HittableWhen cross-part gate is closed (the scene may disable the collider). True otherwise.</summary>
        public bool IsPartCurrentlyHittable(int partId) => !IsHitBlocked(partId);

        // ── Heat track (Story 002) ───────────────────────────────────────────────

        private void HandleLaserHit(LaserHit evt)
        {
            if (!_parts.TryGetValue(evt.PartId, out var part)) return;
            if (part.BreakState == BreakState.Broken) return;
            if (IsHitBlocked(evt.PartId)) return; // HittableWhen gate closed — the hitbox is inert
            if (evt.HeatDelta <= 0f) return; // heat delta must be > 0 (GDD D.1)

            _pendingHeatDeltas.TryGetValue(evt.PartId, out float acc);
            _pendingHeatDeltas[evt.PartId] = acc + evt.HeatDelta;
        }

        /// <summary>
        /// Apply this frame's heat: parts that received laser fire this frame fill (decay
        /// suppressed); all others decay. Then evaluate the INTACT↔SOFTENED hysteresis.
        /// </summary>
        public void TickHeat(float deltaTime)
        {
            foreach (var part in _parts.Values)
            {
                if (part.BreakState == BreakState.Broken) continue;

                if (_pendingHeatDeltas.TryGetValue(part.Id, out float delta))
                    part.HCurrent = Mathf.Clamp(part.HCurrent + delta, 0f, part.HMax);
                else
                    part.HCurrent = Mathf.Clamp(part.HCurrent - _balance.HDecayRate * deltaTime, 0f, part.HMax);

                EvaluateHeatState(part);
            }
            _pendingHeatDeltas.Clear();
        }

        private void EvaluateHeatState(BreakablePart part)
        {
            if (part.HeatState == HeatState.Intact && part.HCurrent >= _balance.ThetaS)
            {
                part.HeatState = HeatState.Softened;
                _bus.Publish(new PartSoftened(part.Id, part.KaijuId, part.HCurrent, part.HMax));
            }
            else if (part.HeatState == HeatState.Softened && part.HCurrent < _balance.ThetaSExit)
            {
                part.HeatState = HeatState.Intact;
                _bus.Publish(new PartSoftenedExit(part.Id, part.KaijuId));
            }
        }

        // ── Armor gate & stagger (Story 003) ─────────────────────────────────────

        private void HandleWaveHit(WaveHit evt)
        {
            if (!_parts.TryGetValue(evt.PartId, out var part)) return;
            if (part.BreakState == BreakState.Broken) return;
            if (IsHitBlocked(evt.PartId)) return; // HittableWhen gate closed — no strip/stagger lands

            part.StaggerTimer = _balance.StaggerDuration; // reset, never additive
            bool armorStripped = part.PartType == PartType.Armored;
            if (armorStripped) part.ArmorState = ArmorState.Stripped;

            _bus.Publish(new PartStaggered(part.Id, part.KaijuId, _balance.StaggerDuration, armorStripped));
        }

        /// <summary>Count down active stagger windows; restore armor and emit PartStaggerEnd at expiry.</summary>
        public void TickStagger(float deltaTime)
        {
            foreach (var part in _parts.Values)
            {
                if (part.StaggerTimer <= 0f || part.BreakState == BreakState.Broken) continue;

                part.StaggerTimer = Mathf.Max(part.StaggerTimer - deltaTime, 0f);
                if (part.StaggerTimer == 0f)
                {
                    bool armorRestored = part.PartType == PartType.Armored;
                    if (armorRestored) part.ArmorState = ArmorState.Intact; // B_current preserved (E.2)
                    _bus.Publish(new PartStaggerEnd(part.Id, part.KaijuId, armorRestored));
                }
            }
        }

        // ── Break track & event emission (Story 004) ─────────────────────────────

        private void HandleMissileHit(MissileHit evt)
        {
            if (!_parts.TryGetValue(evt.PartId, out var part)) return;
            if (part.BreakState == BreakState.Broken) return;
            if (IsBreakBlocked(evt.PartId)) return; // Hittable/BreakableWhen gate closed — break track is protected
            if (evt.BreakDeltaBase <= 0f) return;

            float mult = LookupStateMult(part);
            float bFill = evt.BreakDeltaBase * mult;
            part.BCurrent = Mathf.Clamp(part.BCurrent + bFill, 0f, part.BMax);
            if (_timeSinceBreakHit.ContainsKey(part.Id)) _timeSinceBreakHit[part.Id] = 0f; // break input resets regen grace

            if (part.BCurrent >= GetBreakThreshold(part.PartType))
                TriggerPartBreak(part, isChainBreak: false);
        }

        /// <summary>
        /// Break-gauge regen (per-part-firing-schema.md §5 / TIDEMAW): a part whose break track received no
        /// input for <c>GraceSeconds</c> decays its accumulated break units at <c>RegenRatePerSec</c> BU/s,
        /// clamped at 0 and never resurrecting a BROKEN part. Only runs for parts whose PartDef enabled it, so
        /// existing kaiju (regen disabled) are unaffected. Laser heat does NOT reset the grace — break-track only.
        /// </summary>
        public void TickArmorRegen(float deltaTime)
        {
            if (_armorRegen.Count == 0) return;
            foreach (var kv in _armorRegen)
            {
                if (!_parts.TryGetValue(kv.Key, out var part) || part.BreakState == BreakState.Broken) continue;
                float t = (_timeSinceBreakHit.TryGetValue(kv.Key, out float v) ? v : 0f) + deltaTime;
                _timeSinceBreakHit[kv.Key] = t;
                if (t >= kv.Value.GraceSeconds && part.BCurrent > 0f)
                    part.BCurrent = Mathf.Max(0f, part.BCurrent - kv.Value.RegenRatePerSec * deltaTime);
            }
        }

        /// <summary>
        /// The D.3 break-fill state multiplier. Priority: (1) ARMORED+ARMOR_INTACT deflects (0);
        /// (2) any stagger window open → stagger mult (covers SOFTENED+STAGGERED in one lookup,
        /// not a double-multiply); (3) SOFTENED → 1.0; (4) unsoftened → B_unsoftened_mult.
        /// </summary>
        public float LookupStateMult(BreakablePart part)
        {
            // Armor gate: an ARMORED part deflects break fill ONLY while its armor is intact AND it
            // has not been heat-softened yet. EVERY weapon can break armor — any laser can heat an
            // armored part to SOFTENED to open it; the L3 Wave Cannon is just the fast path (its
            // WaveHit strips armor + opens the stagger window instantly). This removes the old
            // L3-exclusive gate that soft-locked non-L3 loadouts against armored bosses.
            if (part.PartType == PartType.Armored && part.ArmorState == ArmorState.Intact
                && part.HeatState != HeatState.Softened)
                return 0f;
            if (part.StaggerTimer > 0f)
                return _balance.StaggerBreakMult;
            if (part.HeatState == HeatState.Softened)
                return 1.0f;
            return _balance.BUnsoftenedMult;
        }

        private BreakQuality ComputeBreakQuality(BreakablePart part)
        {
            if (part.HeatState == HeatState.Softened && part.StaggerTimer > 0f)
                return BreakQuality.SoftenedStaggered;
            if (part.HeatState == HeatState.Softened)
                return BreakQuality.Softened;
            return BreakQuality.Normal;
        }

        /// <summary>
        /// Break a part: snapshot break_quality from live state, guard the drop table, flip to
        /// terminal BROKEN (zeroing both bars), emit <see cref="PartBroke"/>, and for a boss core
        /// emit <see cref="BossCoreBroke"/> immediately after in the same synchronous call stack
        /// (fixed order — kaiju-part-system.md E.6). <paramref name="isChainBreak"/> is true only
        /// for M3 Tier-3 knock-on breaks, which must not chain again.
        /// </summary>
        public void TriggerPartBreak(BreakablePart part, bool isChainBreak)
        {
            if (string.IsNullOrEmpty(part.DropTableName))
                throw new InvalidOperationException(
                    $"[PartStateSystem] Part '{part.Name}' (kaiju {part.KaijuId}) broke with an empty drop_table_id — " +
                    "invalid KaijuDef. Every breakable part must declare a non-empty drop table (kaiju-part-system.md H.8).");

            BreakQuality quality = ComputeBreakQuality(part);
            part.BreakState = BreakState.Broken;
            part.HCurrent = 0f;
            part.BCurrent = 0f;

            _bus.Publish(new PartBroke(
                part.Id, part.KaijuId, part.PartType, part.WorldPosition,
                part.DropTableId, quality, part.AdjacencyIds, isChainBreak));

            if (part.PartType == PartType.BossCore)
                _bus.Publish(new BossCoreBroke(part.KaijuId, part.WorldPosition));
            else if (part.PartType == PartType.MidCore)
                _bus.Publish(new MidCoreBroke(part.KaijuId, part.WorldPosition));
        }

        // ── M3 Tier-3 adjacency chain (Story 005) ────────────────────────────────

        private void OnPartBroke(PartBroke evt)
        {
            if (evt.IsChainBreak) return; // NON-RECURSIVE GUARD (E.4) — chains never chain again
            ApplyM3Chain(evt.PartId);
        }

        private void ApplyM3Chain(int brokenPartId)
        {
            if (!_parts.TryGetValue(brokenPartId, out var broken)) return;

            int max = _partConfig.M3T3ChainMaxTargets;
            int hit = 0;
            var neighbors = broken.AdjacencyIds; // sorted ascending → deterministic selection
            for (int i = 0; i < neighbors.Length && hit < max; i++)
            {
                if (!_parts.TryGetValue(neighbors[i], out var target)) continue;
                if (target.BreakState == BreakState.Broken) continue; // skip already-broken neighbours
                ApplyChainDamage(target);
                hit++;
            }
        }

        private void ApplyChainDamage(BreakablePart target)
        {
            if (IsBreakBlocked(target.Id)) return; // gated neighbour — chain can't fill its protected break track
            float bChain = _partConfig.M3T3ChainDmgMult * _partConfig.M3ChainDamageBase * LookupStateMult(target);
            if (bChain <= 0f) return; // ARMOR_INTACT neighbour deflects (mult = 0) — no state change
            target.BCurrent = Mathf.Clamp(target.BCurrent + bChain, 0f, target.BMax);
            if (target.BCurrent >= GetBreakThreshold(target.PartType))
                TriggerPartBreak(target, isChainBreak: true);
        }

        // ── IPartStateQuery (Story 001) ──────────────────────────────────────────
        // Unknown part ids throw KeyNotFoundException (documented sentinel), except
        // IsPartAlive which returns false ("or does not exist" per the interface contract).

        public HeatState GetHeatState(int partId) => Require(partId).HeatState;
        public ArmorState GetArmorState(int partId) => Require(partId).ArmorState;
        public float GetCurrentHeat(int partId) => Require(partId).HCurrent;
        public float GetMaxHeat(int partId) => Require(partId).HMax;
        public Vector2 GetWorldPosition(int partId) => Require(partId).WorldPosition;

        public bool IsPartAlive(int partId) =>
            _parts.TryGetValue(partId, out var part) && part.BreakState == BreakState.Alive;

        /// <summary>
        /// Highest-heat ALIVE part id (M1 Tier-3 auto-lock). Returns −1 when no part is alive.
        /// Ties break to the lowest id: iterate ascending and use strict &gt; so the first (lowest-id)
        /// part at the max heat wins.
        /// </summary>
        public int GetHottestAlivePartId()
        {
            int bestId = -1;
            float bestHeat = float.NegativeInfinity;
            foreach (var part in _parts.Values)
            {
                if (part.BreakState != BreakState.Alive) continue;
                if (part.HCurrent > bestHeat || (part.HCurrent == bestHeat && part.Id < bestId))
                {
                    bestHeat = part.HCurrent;
                    bestId = part.Id;
                }
            }
            return bestId;
        }

        /// <summary>Highest-heat ALIVE + SOFTENED part id (M2 Tier-3 saturation callout); −1 if none softened. Ties → lowest id.</summary>
        public int GetHottestSoftenedPartId()
        {
            int bestId = -1;
            float bestHeat = float.NegativeInfinity;
            foreach (var part in _parts.Values)
            {
                if (part.BreakState != BreakState.Alive || part.HeatState != HeatState.Softened) continue;
                if (part.HCurrent > bestHeat || (part.HCurrent == bestHeat && part.Id < bestId))
                {
                    bestHeat = part.HCurrent;
                    bestId = part.Id;
                }
            }
            return bestId;
        }

        private BreakablePart Require(int partId)
        {
            if (_parts.TryGetValue(partId, out var part)) return part;
            throw new KeyNotFoundException($"[PartStateSystem] No part with id {partId} is loaded.");
        }
    }
}
