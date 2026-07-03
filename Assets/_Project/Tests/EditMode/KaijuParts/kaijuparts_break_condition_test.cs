using System;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.KaijuParts;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.KaijuParts
{
    /// <summary>
    /// Story 004 — Break condition &amp; event emission. Verifies the D.3 state-multiplier
    /// table, B_fill + clamp, break_quality snapshot at the break frame, the DropTableId
    /// guard, the fixed PartBroke → BossCoreBroke order for boss cores, and BROKEN terminality
    /// (kaiju-part-system.md D.3/H.1/H.4/H.8).
    /// </summary>
    public sealed class KaijuPartsBreakConditionTests
    {
        private const int Kaiju = 4;

        private static PartStateSystem Build(out RecordingEventBus bus, params (string id, PartType type)[] parts)
        {
            bus = new RecordingEventBus();
            var defs = new PartDef[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                defs[i] = PartTestFactory.Part(parts[i].id, parts[i].type, dropTableId: parts[i].id + "_drop");
            var sys = new PartStateSystem(bus, PartTestFactory.Balance(), PartTestFactory.PartConfig());
            sys.InitializeParts(PartTestFactory.Kaiju("k", defs), Kaiju);
            return sys;
        }

        [Test]
        public void LookupStateMult_AllSixRows()
        {
            var sys = Build(out _, ("normal", PartType.Normal), ("armored", PartType.Armored), ("core", PartType.BossCore));
            var normal = sys.Parts[sys.GetPartId("normal")];
            var armored = sys.Parts[sys.GetPartId("armored")];
            var core = sys.Parts[sys.GetPartId("core")];

            // ARMORED + ARMOR_INTACT → 0
            armored.ArmorState = ArmorState.Intact;
            Assert.AreEqual(0f, sys.LookupStateMult(armored), 1e-5f, "armored intact deflects");

            // ARMORED + ARMOR_STRIPPED (stagger>0) → 1.5
            armored.ArmorState = ArmorState.Stripped; armored.StaggerTimer = 1.0f;
            Assert.AreEqual(1.5f, sys.LookupStateMult(armored), 1e-5f);

            // NORMAL + INTACT + stagger 0 → 0.35
            normal.HeatState = HeatState.Intact; normal.StaggerTimer = 0f;
            Assert.AreEqual(0.35f, sys.LookupStateMult(normal), 1e-5f);

            // NORMAL + SOFTENED + stagger 0 → 1.0
            normal.HeatState = HeatState.Softened; normal.StaggerTimer = 0f;
            Assert.AreEqual(1.0f, sys.LookupStateMult(normal), 1e-5f);

            // NORMAL + stagger>0 (INTACT heat) → 1.5
            normal.HeatState = HeatState.Intact; normal.StaggerTimer = 0.001f;
            Assert.AreEqual(1.5f, sys.LookupStateMult(normal), 1e-5f, "barely-positive stagger takes stagger branch");

            // NORMAL + SOFTENED + stagger>0 → 1.5 (direct lookup, not 1.0 × 1.5)
            normal.HeatState = HeatState.Softened; normal.StaggerTimer = 1.0f;
            Assert.AreEqual(1.5f, sys.LookupStateMult(normal), 1e-5f);

            // BOSS_CORE follows NORMAL rules
            core.HeatState = HeatState.Softened; core.StaggerTimer = 0f;
            Assert.AreEqual(1.0f, sys.LookupStateMult(core), 1e-5f);
        }

        [Test]
        public void ArmoredPart_HeatSoftened_IsBreakableByAnyWeapon()
        {
            // Feedback fix (2026-07-03): EVERY weapon can break armor. Heating an ARMORED part to
            // SOFTENED opens it (mult 1.0) even without the L3 Wave Cannon stripping the armor —
            // this removes the old L3-exclusive gate that soft-locked non-L3 loadouts.
            var sys = Build(out _, ("armored", PartType.Armored), ("core", PartType.BossCore));
            var a = sys.Parts[sys.GetPartId("armored")];

            a.ArmorState = ArmorState.Intact; a.HeatState = HeatState.Intact;
            Assert.AreEqual(0f, sys.LookupStateMult(a), 1e-5f, "cold armored still deflects (must open it first)");

            a.HeatState = HeatState.Softened; // any laser can heat it here
            Assert.AreEqual(1.0f, sys.LookupStateMult(a), 1e-5f, "heat-softened armored breaks with any weapon");
        }

        [Test]
        public void BFill_ComputedAndClamped_TriggersBreakAtThreshold()
        {
            var sys = Build(out var bus, ("normal", PartType.Normal), ("core", PartType.BossCore));
            var p = sys.Parts[sys.GetPartId("normal")];
            p.HeatState = HeatState.Softened; // mult 1.0
            p.BCurrent = 80f;

            bus.Publish(new MissileHit(p.Id, Kaiju, 30f, WeaponId.M1)); // 80 + 30 = 110 clamp 100 ≥ threshold

            Assert.AreEqual(BreakState.Broken, p.BreakState);
            Assert.AreEqual(0f, p.BCurrent, "B_current reset to 0 on break");
            Assert.AreEqual(1, bus.CountOf<PartBroke>());
        }

        [Test]
        public void ZeroMult_And_ZeroBase_NoBreak()
        {
            var sys = Build(out var bus, ("armored", PartType.Armored), ("core", PartType.BossCore));
            var a = sys.Parts[sys.GetPartId("armored")];
            a.ArmorState = ArmorState.Intact; // mult 0

            bus.Publish(new MissileHit(a.Id, Kaiju, 50f, WeaponId.M1));
            Assert.AreEqual(0f, a.BCurrent, "armor-intact deflects — B unchanged");

            a.ArmorState = ArmorState.Stripped; a.StaggerTimer = 1f;
            bus.Publish(new MissileHit(a.Id, Kaiju, 0f, WeaponId.M1)); // base 0 → early return
            Assert.AreEqual(0f, a.BCurrent);
            Assert.AreEqual(0, bus.CountOf<PartBroke>());
        }

        [Test]
        public void SoftenedStaggered_UsesStaggerBranch_NotDoubleMultiplied()
        {
            var sys = Build(out _, ("normal", PartType.Normal), ("core", PartType.BossCore));
            var p = sys.Parts[sys.GetPartId("normal")];
            p.HeatState = HeatState.Softened; p.StaggerTimer = 1.0f;
            Assert.AreEqual(1.5f, sys.LookupStateMult(p), 1e-5f, "stagger branch, not softened(1.0)×stagger(1.5)");
        }

        [Test]
        public void BreakQuality_ComputedAtBreakFrame()
        {
            // Force a break regardless of mult by using a huge base; assert the recorded quality.
            void Case(HeatState heat, float stagger, BreakQuality expected)
            {
                var sys = Build(out var bus, ("n", PartType.Normal), ("core", PartType.BossCore));
                var p = sys.Parts[sys.GetPartId("n")];
                p.HeatState = heat; p.StaggerTimer = stagger;
                bus.Publish(new MissileHit(p.Id, Kaiju, 1000f, WeaponId.M1));
                Assert.AreEqual(expected, bus.Events<PartBroke>()[0].Quality, $"{heat}/stagger {stagger}");
            }

            Case(HeatState.Softened, 1.5f, BreakQuality.SoftenedStaggered);
            Case(HeatState.Softened, 0f, BreakQuality.Softened);
            Case(HeatState.Intact, 0f, BreakQuality.Normal);
            Case(HeatState.Intact, 1.0f, BreakQuality.Normal); // stagger alone does not upgrade quality
        }

        [Test]
        public void DropTableId_NonEmptyGuard()
        {
            var bus = new RecordingEventBus();
            var sys = new PartStateSystem(bus, PartTestFactory.Balance(), PartTestFactory.PartConfig());
            sys.InitializeParts(PartTestFactory.Kaiju("k",
                PartTestFactory.Part("bad", PartType.Normal, dropTableId: ""), // invalid KaijuDef
                PartTestFactory.Part("core", PartType.BossCore)), Kaiju);
            int id = sys.GetPartId("bad");

            Assert.Throws<InvalidOperationException>(
                () => bus.Publish(new MissileHit(id, Kaiju, 1000f, WeaponId.M1)));

            // Valid drop table passes and surfaces a positive int id.
            int good = sys.GetPartId("core");
            bus.Clear();
            bus.Publish(new MissileHit(good, Kaiju, 1000f, WeaponId.M1));
            Assert.Greater(bus.Events<PartBroke>()[0].DropTableId, 0);
        }

        [Test]
        public void BossCore_FiresPartBrokeThenBossCoreBroke_InOrder()
        {
            var sys = Build(out var bus, ("core", PartType.BossCore));
            int id = sys.GetPartId("core");

            bus.Publish(new MissileHit(id, Kaiju, 1000f, WeaponId.M1));

            Assert.AreEqual(1, bus.CountOf<PartBroke>());
            Assert.AreEqual(1, bus.CountOf<BossCoreBroke>());
            // PartBroke must precede BossCoreBroke in the recorded stream.
            int iPart = -1, iBoss = -1;
            for (int i = 0; i < bus.Recorded.Count; i++)
            {
                if (bus.Recorded[i] is PartBroke) iPart = i;
                if (bus.Recorded[i] is BossCoreBroke) iBoss = i;
            }
            Assert.Less(iPart, iBoss, "PartBroke published before BossCoreBroke");
            Assert.AreEqual(Kaiju, bus.Events<BossCoreBroke>()[0].KaijuId);
        }

        [Test]
        public void NormalPartBreak_DoesNotFireBossCoreBroke()
        {
            var sys = Build(out var bus, ("n", PartType.Normal), ("core", PartType.BossCore));
            bus.Publish(new MissileHit(sys.GetPartId("n"), Kaiju, 1000f, WeaponId.M1));
            Assert.AreEqual(1, bus.CountOf<PartBroke>());
            Assert.AreEqual(0, bus.CountOf<BossCoreBroke>());
        }

        [Test]
        public void BrokenPart_IgnoresAllHitEvents()
        {
            var sys = Build(out var bus, ("n", PartType.Normal), ("core", PartType.BossCore));
            var p = sys.Parts[sys.GetPartId("n")];
            p.BreakState = BreakState.Broken;
            p.BCurrent = 0f; p.HCurrent = 0f;

            bus.Publish(new MissileHit(p.Id, Kaiju, 1000f, WeaponId.M1));
            bus.Publish(new LaserHit(p.Id, Kaiju, 50f));
            bus.Publish(new WaveHit(p.Id, Kaiju));
            sys.Tick(1.0f);

            Assert.AreEqual(0f, p.BCurrent);
            Assert.AreEqual(0f, p.HCurrent);
            Assert.AreEqual(0, bus.CountOf<PartBroke>());
            Assert.AreEqual(0, bus.CountOf<PartStaggered>());
            Assert.AreEqual(0, bus.CountOf<PartSoftened>());
        }
    }
}
