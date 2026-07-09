using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.KaijuParts;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.KaijuParts
{
    /// <summary>
    /// Cross-part gate execution (per-part-firing-schema.md §4). A part's hittability
    /// (HittableWhen) or breakability (BreakableWhen) can be gated on another part's state
    /// (broken / armor-stripped / softened). Verifies the guards on the laser, wave, missile
    /// and M3-chain paths, RequireAll vs any, dynamic (transient-state) gates, the
    /// unresolvable-id fallback (never soft-lock), and that ungated kaiju are unaffected.
    /// </summary>
    public sealed class KaijuPartsCrossPartGateTests
    {
        private const int Kaiju = 7;

        private static PartDef Gated(string id, PartGateKind kind, PartGateCond cond, string[] gateParts,
                                     bool requireAll = true, PartType type = PartType.Normal)
        {
            var pd = PartTestFactory.Part(id, type, dropTableId: id + "_drop");
            PartTestFactory.SetField(pd, "_gateKind", kind);
            PartTestFactory.SetField(pd, "_gateCond", cond);
            PartTestFactory.SetField(pd, "_gatePartIds", gateParts);
            PartTestFactory.SetField(pd, "_requireAllGates", requireAll);
            return pd;
        }

        private static PartStateSystem Build(out RecordingEventBus bus, params PartDef[] parts)
        {
            bus = new RecordingEventBus();
            var sys = new PartStateSystem(bus, PartTestFactory.Balance(), PartTestFactory.PartConfig());
            sys.InitializeParts(PartTestFactory.Kaiju("k", parts), Kaiju);
            return sys;
        }

        // ── HittableWhen: no hit of any kind lands while the gate is closed ────────

        [Test]
        public void HittableWhen_ClosedGate_IgnoresLaserWaveMissile()
        {
            var sys = Build(out var bus,
                PartTestFactory.Part("veil", PartType.Normal, dropTableId: "veil_drop"),
                Gated("inner", PartGateKind.HittableWhen, PartGateCond.GatePartBroken, new[] { "veil" }),
                PartTestFactory.Part("core", PartType.BossCore, dropTableId: "core_drop"));
            var inner = sys.Parts[sys.GetPartId("inner")];

            bus.Publish(new MissileHit(inner.Id, Kaiju, 1000f, WeaponId.M1));
            bus.Publish(new LaserHit(inner.Id, Kaiju, 500f));
            bus.Publish(new WaveHit(inner.Id, Kaiju));
            sys.Tick(1.0f);

            Assert.AreEqual(0f, inner.BCurrent, "missile break blocked while HittableWhen gate closed");
            Assert.AreEqual(0f, inner.HCurrent, "laser heat blocked while HittableWhen gate closed");
            Assert.AreEqual(BreakState.Alive, inner.BreakState);
            Assert.AreEqual(0, bus.CountOf<PartStaggered>(), "wave stagger blocked while gate closed");
            Assert.IsFalse(sys.IsPartCurrentlyHittable(inner.Id));
        }

        [Test]
        public void HittableWhen_OpensWhenGatePartBreaks_ThenHitsLand()
        {
            var sys = Build(out var bus,
                PartTestFactory.Part("veil", PartType.Normal, dropTableId: "veil_drop"),
                Gated("inner", PartGateKind.HittableWhen, PartGateCond.GatePartBroken, new[] { "veil" }),
                PartTestFactory.Part("core", PartType.BossCore, dropTableId: "core_drop"));
            var inner = sys.Parts[sys.GetPartId("inner")];

            // Break the veil → gate opens.
            bus.Publish(new MissileHit(sys.GetPartId("veil"), Kaiju, 1000f, WeaponId.M1));
            Assert.IsTrue(sys.IsPartCurrentlyHittable(inner.Id), "gate open once veil broken");

            bus.Publish(new MissileHit(inner.Id, Kaiju, 1000f, WeaponId.M1));
            Assert.AreEqual(BreakState.Broken, inner.BreakState, "inner breakable once gate open");
        }

        // ── BreakableWhen: heat still lands, only break is gated ───────────────────

        [Test]
        public void BreakableWhen_ClosedGate_AllowsHeatButBlocksBreak()
        {
            var sys = Build(out var bus,
                PartTestFactory.Part("plate", PartType.Armored, dropTableId: "plate_drop"),
                Gated("vent", PartGateKind.BreakableWhen, PartGateCond.GatePartArmorStripped, new[] { "plate" }),
                PartTestFactory.Part("core", PartType.BossCore, dropTableId: "core_drop"));
            var vent = sys.Parts[sys.GetPartId("vent")];

            // Laser heat is NOT blocked by a BreakableWhen gate — the vent can still soften.
            bus.Publish(new LaserHit(vent.Id, Kaiju, 10000f));
            sys.Tick(0.016f);
            Assert.Greater(vent.HCurrent, 0f, "heat lands through a BreakableWhen gate");
            Assert.AreEqual(HeatState.Softened, vent.HeatState);

            // But break fill is blocked while the plate armor is intact.
            bus.Publish(new MissileHit(vent.Id, Kaiju, 1000f, WeaponId.M1));
            Assert.AreEqual(0f, vent.BCurrent, "break blocked while gate closed");
            Assert.AreEqual(BreakState.Alive, vent.BreakState);
            Assert.IsTrue(sys.IsPartCurrentlyHittable(vent.Id), "BreakableWhen never disables the hitbox");
        }

        [Test]
        public void BreakableWhen_OpensWhenGatePartArmorStripped_ThenBreaks()
        {
            var sys = Build(out var bus,
                PartTestFactory.Part("plate", PartType.Armored, dropTableId: "plate_drop"),
                Gated("vent", PartGateKind.BreakableWhen, PartGateCond.GatePartArmorStripped, new[] { "plate" }),
                PartTestFactory.Part("core", PartType.BossCore, dropTableId: "core_drop"));
            var vent = sys.Parts[sys.GetPartId("vent")];
            vent.HeatState = HeatState.Softened; // opened for full break mult

            // Strip the plate's armor (WaveHit) → gate opens this frame.
            bus.Publish(new WaveHit(sys.GetPartId("plate"), Kaiju));
            bus.Publish(new MissileHit(vent.Id, Kaiju, 1000f, WeaponId.M1));

            Assert.AreEqual(BreakState.Broken, vent.BreakState, "vent breakable once plate armor stripped");
        }

        // ── Dynamic gate: transient softened state opens/closes live ──────────────

        [Test]
        public void BreakableWhen_GatePartSoftened_EvaluatedLive()
        {
            var sys = Build(out var bus,
                PartTestFactory.Part("facet", PartType.Normal, dropTableId: "facet_drop"),
                Gated("weak", PartGateKind.BreakableWhen, PartGateCond.GatePartSoftened, new[] { "facet" }),
                PartTestFactory.Part("core", PartType.BossCore, dropTableId: "core_drop"));
            var facet = sys.Parts[sys.GetPartId("facet")];
            var weak = sys.Parts[sys.GetPartId("weak")];
            weak.HeatState = HeatState.Softened;

            // facet intact → gate closed → break blocked.
            bus.Publish(new MissileHit(weak.Id, Kaiju, 1000f, WeaponId.M1));
            Assert.AreEqual(0f, weak.BCurrent, "closed while facet not softened");

            // facet softens → gate opens → break lands.
            facet.HeatState = HeatState.Softened;
            bus.Publish(new MissileHit(weak.Id, Kaiju, 1000f, WeaponId.M1));
            Assert.AreEqual(BreakState.Broken, weak.BreakState, "open once facet softened");
        }

        // ── RequireAll vs any ─────────────────────────────────────────────────────

        [Test]
        public void RequireAll_NeedsEveryGatePart()
        {
            var sys = Build(out var bus,
                PartTestFactory.Part("a", PartType.Normal, dropTableId: "a_drop"),
                PartTestFactory.Part("b", PartType.Normal, dropTableId: "b_drop"),
                Gated("g", PartGateKind.BreakableWhen, PartGateCond.GatePartBroken, new[] { "a", "b" }, requireAll: true),
                PartTestFactory.Part("core", PartType.BossCore, dropTableId: "core_drop"));
            var g = sys.Parts[sys.GetPartId("g")];

            sys.Parts[sys.GetPartId("a")].BreakState = BreakState.Broken; // only one of two
            bus.Publish(new MissileHit(g.Id, Kaiju, 1000f, WeaponId.M1));
            Assert.AreEqual(0f, g.BCurrent, "RequireAll: one broken is not enough");

            sys.Parts[sys.GetPartId("b")].BreakState = BreakState.Broken; // now both
            bus.Publish(new MissileHit(g.Id, Kaiju, 1000f, WeaponId.M1));
            Assert.AreEqual(BreakState.Broken, g.BreakState, "RequireAll: opens once all broken");
        }

        [Test]
        public void AnyGate_OneGatePartSuffices()
        {
            var sys = Build(out var bus,
                PartTestFactory.Part("a", PartType.Normal, dropTableId: "a_drop"),
                PartTestFactory.Part("b", PartType.Normal, dropTableId: "b_drop"),
                Gated("g", PartGateKind.BreakableWhen, PartGateCond.GatePartBroken, new[] { "a", "b" }, requireAll: false),
                PartTestFactory.Part("core", PartType.BossCore, dropTableId: "core_drop"));
            var g = sys.Parts[sys.GetPartId("g")];

            sys.Parts[sys.GetPartId("a")].BreakState = BreakState.Broken; // any → one is enough
            bus.Publish(new MissileHit(g.Id, Kaiju, 1000f, WeaponId.M1));
            Assert.AreEqual(BreakState.Broken, g.BreakState, "any: one broken opens the gate");
        }

        [Test]
        public void SoftenedOrStripped_EitherConditionOpensGate()
        {
            // PRISMSHELL weak_node: HittableWhen, open if ANY of {a,b} is softened OR armor-stripped.
            var sys = Build(out var bus,
                PartTestFactory.Part("a", PartType.Armored, dropTableId: "a_drop"),
                PartTestFactory.Part("b", PartType.Armored, dropTableId: "b_drop"),
                Gated("node", PartGateKind.HittableWhen, PartGateCond.GatePartSoftenedOrStripped, new[] { "a", "b" }, requireAll: false),
                PartTestFactory.Part("core", PartType.BossCore, dropTableId: "core_drop"));
            var node = sys.Parts[sys.GetPartId("node")];

            // Neither softened nor stripped -> closed.
            Assert.IsFalse(sys.IsPartCurrentlyHittable(node.Id));
            bus.Publish(new MissileHit(node.Id, Kaiju, 1000f, WeaponId.M1));
            Assert.AreEqual(0f, node.BCurrent, "closed while neither gate facet softened/stripped");

            // Softening one facet opens it.
            sys.Parts[sys.GetPartId("a")].HeatState = HeatState.Softened;
            Assert.IsTrue(sys.IsPartCurrentlyHittable(node.Id), "softened facet opens the seam");

            // Cool a, strip b instead -> still open via the stripped branch.
            sys.Parts[sys.GetPartId("a")].HeatState = HeatState.Intact;
            Assert.IsFalse(sys.IsPartCurrentlyHittable(node.Id));
            sys.Parts[sys.GetPartId("b")].ArmorState = ArmorState.Stripped;
            Assert.IsTrue(sys.IsPartCurrentlyHittable(node.Id), "armor-stripped facet also opens the seam");
            bus.Publish(new MissileHit(node.Id, Kaiju, 1000f, WeaponId.M1));
            Assert.AreEqual(BreakState.Broken, node.BreakState, "node breakable once a gate facet is stripped");
        }


        // ── Fallbacks / regressions ───────────────────────────────────────────────

        [Test]
        public void UnresolvableGatePart_TreatedAsUngated_NeverSoftLocks()
        {
            var sys = Build(out var bus,
                Gated("g", PartGateKind.HittableWhen, PartGateCond.GatePartBroken, new[] { "does_not_exist" }),
                PartTestFactory.Part("core", PartType.BossCore, dropTableId: "core_drop"));
            var g = sys.Parts[sys.GetPartId("g")];

            Assert.IsTrue(sys.IsPartCurrentlyHittable(g.Id), "bad gate id must not permanently disable the part");
            bus.Publish(new MissileHit(g.Id, Kaiju, 1000f, WeaponId.M1));
            Assert.AreEqual(BreakState.Broken, g.BreakState, "unresolvable gate → breaks normally");
        }

        [Test]
        public void UngatedKaiju_Unaffected()
        {
            var sys = Build(out var bus,
                PartTestFactory.Part("n", PartType.Normal, dropTableId: "n_drop"),
                PartTestFactory.Part("core", PartType.BossCore, dropTableId: "core_drop"));
            var n = sys.Parts[sys.GetPartId("n")];

            Assert.IsTrue(sys.IsPartCurrentlyHittable(n.Id));
            bus.Publish(new MissileHit(n.Id, Kaiju, 1000f, WeaponId.M1));
            Assert.AreEqual(BreakState.Broken, n.BreakState);
        }

        // ── M3 chain respects the gate ────────────────────────────────────────────

        [Test]
        public void ChainBreak_BlockedByClosedGate_AllowedWhenOpen()
        {
            // Case A — gate closed: the chain cannot fill a gated neighbour's break track.
            {
                var sys = Build(out var busA,
                    PartTestFactory.Part("trigger", PartType.Normal, dropTableId: "t_drop", adjacency: new[] { "nbr" }),
                    Gated("nbr", PartGateKind.BreakableWhen, PartGateCond.GatePartBroken, new[] { "locked" }),
                    PartTestFactory.Part("locked", PartType.Normal, dropTableId: "l_drop"),
                    PartTestFactory.Part("core", PartType.BossCore, dropTableId: "core_drop"));
                var nbr = sys.Parts[sys.GetPartId("nbr")];
                nbr.BCurrent = 50f;
                bus_publish_break(sys, busA, "trigger");
                Assert.AreEqual(50f, nbr.BCurrent, "closed gate: chain does not touch the protected neighbour");
                Assert.AreEqual(BreakState.Alive, nbr.BreakState);
            }

            // Case B — gate open: the chain fills the neighbour as usual.
            {
                var sys = Build(out var busB,
                    PartTestFactory.Part("trigger", PartType.Normal, dropTableId: "t_drop", adjacency: new[] { "nbr" }),
                    Gated("nbr", PartGateKind.BreakableWhen, PartGateCond.GatePartBroken, new[] { "locked" }),
                    PartTestFactory.Part("locked", PartType.Normal, dropTableId: "l_drop"),
                    PartTestFactory.Part("core", PartType.BossCore, dropTableId: "core_drop"));
                var nbr = sys.Parts[sys.GetPartId("nbr")];
                nbr.HeatState = HeatState.Softened;
                nbr.BCurrent = 50f;
                sys.Parts[sys.GetPartId("locked")].BreakState = BreakState.Broken; // open the gate
                bus_publish_break(sys, busB, "trigger");
                Assert.Greater(nbr.BCurrent, 50f, "open gate: chain fills the neighbour");
            }
        }

        private static void bus_publish_break(PartStateSystem sys, RecordingEventBus bus, string partName)
            => bus.Publish(new MissileHit(sys.GetPartId(partName), Kaiju, 1000f, WeaponId.M1));
    }
}
