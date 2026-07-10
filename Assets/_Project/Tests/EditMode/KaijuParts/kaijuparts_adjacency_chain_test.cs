using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.KaijuParts;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.KaijuParts
{
    /// <summary>
    /// Story 005 — Adjacency graph load &amp; Tier-3 chain consumers. Verifies bidirectional
    /// graph construction with dedup + neighbour cap, the M3 chain (≤ 2 alive targets, ARMOR
    /// deflection, non-recursion), and the L2 heat-ripple same-frame softened path via a fake
    /// Weapons stub (kaiju-part-system.md C.6/D.5/D.6/E.3/E.4).
    /// </summary>
    public sealed class KaijuPartsAdjacencyChainTests
    {
        private const int Kaiju = 5;

        private static PartStateSystem Build(out RecordingEventBus bus, PartSystemConfig cfg,
            params PartDef[] parts)
        {
            bus = new RecordingEventBus();
            var sys = new PartStateSystem(bus, PartTestFactory.BalanceClassicBreak(), cfg);
            sys.InitializeParts(PartTestFactory.Kaiju("k", parts), Kaiju);
            return sys;
        }

        private static bool Contains(int[] ids, int value)
        {
            for (int i = 0; i < ids.Length; i++) if (ids[i] == value) return true;
            return false;
        }

        [Test]
        public void AdjacencyGraph_BuiltBidirectionally_NoDuplicates()
        {
            var sys = Build(out _, PartTestFactory.PartConfig(),
                PartTestFactory.Part("A", PartType.Normal, adjacency: new[] { "B", "B" }), // double-declared
                PartTestFactory.Part("B", PartType.Normal, adjacency: new[] { "A", "C" }),
                PartTestFactory.Part("C", PartType.BossCore));

            int a = sys.GetPartId("A"), b = sys.GetPartId("B"), c = sys.GetPartId("C");

            Assert.AreEqual(1, sys.Parts[a].AdjacencyIds.Length, "A dedup → {B}");
            Assert.IsTrue(Contains(sys.Parts[a].AdjacencyIds, b));
            Assert.AreEqual(2, sys.Parts[b].AdjacencyIds.Length, "B → {A, C}");
            Assert.IsTrue(Contains(sys.Parts[b].AdjacencyIds, a));
            Assert.IsTrue(Contains(sys.Parts[b].AdjacencyIds, c));
            Assert.AreEqual(1, sys.Parts[c].AdjacencyIds.Length, "C gets reverse edge from B");
            Assert.IsTrue(Contains(sys.Parts[c].AdjacencyIds, b));
        }

        [Test]
        public void AdjacencyMaxNeighbors_CapEnforced()
        {
            var cfg = PartTestFactory.PartConfig(("_adjacencyMaxNeighbors", 4));
            var sys = Build(out _, cfg,
                PartTestFactory.Part("A", PartType.Normal, adjacency: new[] { "B", "C", "D", "E", "F" }),
                PartTestFactory.Part("B", PartType.Normal),
                PartTestFactory.Part("C", PartType.Normal),
                PartTestFactory.Part("D", PartType.Normal),
                PartTestFactory.Part("E", PartType.Normal),
                PartTestFactory.Part("F", PartType.BossCore));

            int a = sys.GetPartId("A"), f = sys.GetPartId("F");
            Assert.AreEqual(4, sys.Parts[a].AdjacencyIds.Length, "capped to first 4 (B,C,D,E)");
            Assert.IsFalse(Contains(sys.Parts[a].AdjacencyIds, f), "F beyond cap — not registered from A");
        }

        [Test]
        public void M3Chain_TargetsUpToTwoAliveNeighbors()
        {
            // Order [P, A, B, C] → ids 0..3; P's neighbours sorted ascending = A,B,C; max 2 → A,B.
            var sys = Build(out var bus, PartTestFactory.PartConfig(),
                PartTestFactory.Part("P", PartType.Normal, dropTableId: "d", adjacency: new[] { "A", "B", "C" }),
                PartTestFactory.Part("A", PartType.Normal, dropTableId: "d"),
                PartTestFactory.Part("B", PartType.Normal, dropTableId: "d"),
                PartTestFactory.Part("C", PartType.Normal, dropTableId: "d"),
                PartTestFactory.Part("core", PartType.BossCore, dropTableId: "d"));

            bus.Publish(new MissileHit(sys.GetPartId("P"), Kaiju, 1000f, WeaponId.M3)); // breaks P → chain

            // B_chain = 1.5 (dmg mult) × 10 (base) × 0.35 (unsoftened) = 5.25
            Assert.AreEqual(5.25f, sys.Parts[sys.GetPartId("A")].BCurrent, 1e-4f);
            Assert.AreEqual(5.25f, sys.Parts[sys.GetPartId("B")].BCurrent, 1e-4f);
            Assert.AreEqual(0f, sys.Parts[sys.GetPartId("C")].BCurrent, "3rd neighbour beyond max targets");
        }

        [Test]
        public void M3Chain_NonRecursive_ChainBreakDoesNotReChain()
        {
            // P→A; A→{P,D,E}. A is primed to break from the chain; its own neighbours D,E must NOT be chained.
            var sys = Build(out var bus, PartTestFactory.PartConfig(),
                PartTestFactory.Part("P", PartType.Normal, dropTableId: "d", adjacency: new[] { "A" }),
                PartTestFactory.Part("A", PartType.Normal, dropTableId: "d", adjacency: new[] { "D", "E" }),
                PartTestFactory.Part("D", PartType.Normal, dropTableId: "d"),
                PartTestFactory.Part("E", PartType.Normal, dropTableId: "d"),
                PartTestFactory.Part("core", PartType.BossCore, dropTableId: "d"));

            sys.Parts[sys.GetPartId("A")].BCurrent = 96f; // 96 + 5.25 ≥ 100 → A breaks via chain

            bus.Publish(new MissileHit(sys.GetPartId("P"), Kaiju, 1000f, WeaponId.M3));

            Assert.AreEqual(BreakState.Broken, sys.Parts[sys.GetPartId("A")].BreakState, "A broke via chain");
            Assert.AreEqual(0f, sys.Parts[sys.GetPartId("D")].BCurrent, "D not re-chained");
            Assert.AreEqual(0f, sys.Parts[sys.GetPartId("E")].BCurrent, "E not re-chained");

            // The A break carries IsChainBreak == true.
            bool foundChainBreakForA = false;
            int aId = sys.GetPartId("A");
            foreach (var pb in bus.Events<PartBroke>())
                if (pb.PartId == aId) { Assert.IsTrue(pb.IsChainBreak); foundChainBreakForA = true; }
            Assert.IsTrue(foundChainBreakForA, "A's PartBroke recorded");
        }

        [Test]
        public void M3Chain_ArmorIntactNeighbor_Deflects()
        {
            var sys = Build(out var bus, PartTestFactory.PartConfig(),
                PartTestFactory.Part("P", PartType.Normal, dropTableId: "d", adjacency: new[] { "B" }),
                PartTestFactory.Part("B", PartType.Armored, dropTableId: "d"),
                PartTestFactory.Part("core", PartType.BossCore, dropTableId: "d"));
            int b = sys.GetPartId("B");
            sys.Parts[b].ArmorState = ArmorState.Intact; // mult 0

            bus.Publish(new MissileHit(sys.GetPartId("P"), Kaiju, 1000f, WeaponId.M3));
            Assert.AreEqual(0f, sys.Parts[b].BCurrent, "armor-intact deflects chain BU");

            // Re-run with the neighbour stripped: mult 1.5 → 1.5 × 10 × 1.5 = 22.5
            var sys2 = Build(out var bus2, PartTestFactory.PartConfig(),
                PartTestFactory.Part("P", PartType.Normal, dropTableId: "d", adjacency: new[] { "B" }),
                PartTestFactory.Part("B", PartType.Armored, dropTableId: "d"),
                PartTestFactory.Part("core", PartType.BossCore, dropTableId: "d"));
            int b2 = sys2.GetPartId("B");
            sys2.Parts[b2].ArmorState = ArmorState.Stripped;
            sys2.Parts[b2].StaggerTimer = 1.0f;

            bus2.Publish(new MissileHit(sys2.GetPartId("P"), Kaiju, 1000f, WeaponId.M3));
            Assert.AreEqual(22.5f, sys2.Parts[b2].BCurrent, 1e-4f, "stripped neighbour takes chain BU");
        }

        [Test]
        public void L2HeatRipple_TriggersSameFrameSoftened_OnNeighbor()
        {
            var cfg = PartTestFactory.PartConfig(); // L2T3AdjacentHeatPct default 0.30
            var sys = Build(out var bus, cfg,
                PartTestFactory.Part("P", PartType.Normal, dropTableId: "d", adjacency: new[] { "Q" }),
                PartTestFactory.Part("Q", PartType.Normal, dropTableId: "d"),
                PartTestFactory.Part("core", PartType.BossCore, dropTableId: "d"));
            int q = sys.GetPartId("Q");
            sys.Parts[q].HCurrent = 70f;

            // Fake Weapons: on PartBroke, ripple heat = neighbour H_max × pct to each alive neighbour.
            bus.Subscribe<PartBroke>(evt =>
            {
                foreach (int nid in evt.AdjacencyIds)
                {
                    if (!sys.IsPartAlive(nid)) continue;
                    float delta = sys.GetMaxHeat(nid) * cfg.L2T3AdjacentHeatPct; // 100 × 0.30 = 30
                    bus.Publish(new LaserHit(nid, Kaiju, delta));
                }
            });

            bus.Publish(new MissileHit(sys.GetPartId("P"), Kaiju, 1000f, WeaponId.M3)); // breaks P → ripple queues heat on Q
            sys.TickHeat(0.016f); // same frame: apply queued heat → Q crosses theta_S

            Assert.AreEqual(100f, sys.Parts[q].HCurrent, 1e-4f, "70 + 30 = 100 (clamped)");
            Assert.AreEqual(HeatState.Softened, sys.Parts[q].HeatState);
            Assert.AreEqual(1, bus.CountOf<PartSoftened>(), "PartSoftened(Q) fired this frame");
            Assert.AreEqual(q, bus.Events<PartSoftened>()[0].PartId);
        }

        [Test]
        public void L2HeatRipple_ClampsAtHMax_NoSoftenBelowThreshold()
        {
            var cfg = PartTestFactory.PartConfig();
            var sys = Build(out var bus, cfg,
                PartTestFactory.Part("P", PartType.Normal, dropTableId: "d", adjacency: new[] { "Q" }),
                PartTestFactory.Part("Q", PartType.Normal, dropTableId: "d"),
                PartTestFactory.Part("core", PartType.BossCore, dropTableId: "d"));
            int q = sys.GetPartId("Q");
            sys.Parts[q].HCurrent = 95f;

            bus.Subscribe<PartBroke>(evt =>
            {
                foreach (int nid in evt.AdjacencyIds)
                    if (sys.IsPartAlive(nid))
                        bus.Publish(new LaserHit(nid, Kaiju, sys.GetMaxHeat(nid) * cfg.L2T3AdjacentHeatPct));
            });

            bus.Publish(new MissileHit(sys.GetPartId("P"), Kaiju, 1000f, WeaponId.M3));
            sys.TickHeat(0.016f);

            Assert.AreEqual(100f, sys.Parts[q].HCurrent, 1e-4f, "95 + 30 clamped to 100, not 125");
        }
    }
}
