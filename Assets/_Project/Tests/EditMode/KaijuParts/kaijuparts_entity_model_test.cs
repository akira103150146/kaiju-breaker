using System.Collections.Generic;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.KaijuParts;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.KaijuParts
{
    /// <summary>
    /// Story 001 — Part entity &amp; two-bar data model. Verifies config defaults, H_max
    /// override resolution, clean initial state for all three part types, part_regen always
    /// false, and IPartStateQuery reads. Note (reconciliation): heat/break/stagger knobs are
    /// owned by WeaponBalanceConfig (single source), not PartSystemConfig — see PartStateSystem.
    /// </summary>
    public sealed class KaijuPartsEntityModelTests
    {
        private const int KaijuId = 7;

        private static PartStateSystem NewSystem(out RecordingEventBus bus, out WeaponBalanceConfig bal,
            out PartSystemConfig cfg)
        {
            bus = new RecordingEventBus();
            bal = PartTestFactory.Balance();
            cfg = PartTestFactory.PartConfig();
            return new PartStateSystem(bus, bal, cfg);
        }

        [Test]
        public void Config_Defaults_MatchGdd()
        {
            var bal = PartTestFactory.Balance();
            Assert.AreEqual(100f, bal.HMaxNormal, "HMaxNormal");
            Assert.AreEqual(150f, bal.HMaxArmored, "HMaxArmored");
            Assert.AreEqual(200f, bal.HMaxBossCore, "HMaxBossCore");
            Assert.AreEqual(3f, bal.HDecayRate, "HDecayRate");
            Assert.AreEqual(100f, bal.ThetaS, "ThetaS");
            Assert.AreEqual(80f, bal.ThetaSExit, "ThetaSExit");
            Assert.AreEqual(100f, bal.BMaxNormal, "BMaxNormal");
            Assert.AreEqual(0.35f, bal.BUnsoftenedMult, 1e-5f, "BUnsoftenedMult");
            Assert.AreEqual(1.5f, bal.StaggerBreakMult, 1e-5f, "StaggerBreakMult");
            Assert.AreEqual(2.0f, bal.StaggerDuration, 1e-5f, "StaggerDuration");

            var cfg = PartTestFactory.PartConfig();
            Assert.IsFalse(cfg.PartRegenEnabled, "PartRegenEnabled must default false");
            Assert.IsFalse(cfg.ChainBreakIsRecursive, "ChainBreakIsRecursive must default false");
            Assert.AreEqual(4, cfg.AdjacencyMaxNeighbors, "AdjacencyMaxNeighbors");
            Assert.AreEqual(1.5f, cfg.M3T3ChainDmgMult, 1e-5f, "M3T3ChainDmgMult");
            Assert.AreEqual(2, cfg.M3T3ChainMaxTargets, "M3T3ChainMaxTargets");
            Assert.AreEqual(10f, cfg.M3ChainDamageBase, 1e-5f, "M3ChainDamageBase");
            Assert.AreEqual(0.30f, cfg.L2T3AdjacentHeatPct, 1e-5f, "L2T3AdjacentHeatPct");
        }

        [Test]
        public void HMaxOverride_AppliedWhenSet_GlobalWhenNull()
        {
            var sys = NewSystem(out _, out _, out _);
            var kaiju = PartTestFactory.Kaiju("k",
                PartTestFactory.Part("over", PartType.Normal, hMaxOverride: 200f),
                PartTestFactory.Part("norm", PartType.Normal),
                PartTestFactory.Part("armor", PartType.Armored),
                PartTestFactory.Part("core", PartType.BossCore));
            sys.InitializeParts(kaiju, KaijuId);

            Assert.AreEqual(200f, sys.Parts[sys.GetPartId("over")].HMax, "override applies");
            Assert.AreEqual(100f, sys.Parts[sys.GetPartId("norm")].HMax, "normal uses global");
            Assert.AreEqual(150f, sys.Parts[sys.GetPartId("armor")].HMax, "armored uses global");
            Assert.AreEqual(200f, sys.Parts[sys.GetPartId("core")].HMax, "boss core uses global");
        }

        [Test]
        public void InitializeParts_ProducesCleanInitialState_ForAllPartTypes()
        {
            var sys = NewSystem(out _, out _, out _);
            var kaiju = PartTestFactory.Kaiju("k",
                PartTestFactory.Part("n", PartType.Normal),
                PartTestFactory.Part("a", PartType.Armored),
                PartTestFactory.Part("c", PartType.BossCore));
            sys.InitializeParts(kaiju, KaijuId);

            foreach (var name in new[] { "n", "a", "c" })
            {
                var p = sys.Parts[sys.GetPartId(name)];
                Assert.AreEqual(0f, p.HCurrent, $"{name} HCurrent");
                Assert.AreEqual(0f, p.BCurrent, $"{name} BCurrent");
                Assert.AreEqual(BreakState.Alive, p.BreakState, $"{name} BreakState");
                Assert.AreEqual(0f, p.StaggerTimer, $"{name} StaggerTimer");
                Assert.AreEqual(HeatState.Intact, p.HeatState, $"{name} HeatState");
                Assert.AreEqual(ArmorState.Intact, p.ArmorState, $"{name} ArmorState defaults Intact");
                Assert.AreEqual(KaijuId, p.KaijuId, $"{name} KaijuId");
            }
        }

        [Test]
        public void InitializeParts_SecondCall_FullyResetsPriorModifiedState()
        {
            var sys = NewSystem(out _, out _, out _);
            var kaiju = PartTestFactory.Kaiju("k", PartTestFactory.Part("n", PartType.Normal));
            sys.InitializeParts(kaiju, KaijuId);

            // Dirty the state as if a round was played.
            var p = sys.Parts[sys.GetPartId("n")];
            p.HCurrent = 90f; p.BCurrent = 75f; p.HeatState = HeatState.Softened;
            p.StaggerTimer = 1.2f; p.BreakState = BreakState.Broken;

            sys.InitializeParts(kaiju, KaijuId); // new round
            var fresh = sys.Parts[sys.GetPartId("n")];
            Assert.AreEqual(0f, fresh.HCurrent);
            Assert.AreEqual(0f, fresh.BCurrent);
            Assert.AreEqual(HeatState.Intact, fresh.HeatState);
            Assert.AreEqual(0f, fresh.StaggerTimer);
            Assert.AreEqual(BreakState.Alive, fresh.BreakState, "prior BROKEN must not carry over");
        }

        [Test]
        public void PartRegenEnabled_AlwaysFalse()
        {
            var cfg = PartTestFactory.PartConfig();
            Assert.IsFalse(cfg.PartRegenEnabled);
            // No difficulty-parameterised path can flip it: PartSystemConfig exposes only a getter.
            Assert.IsNull(typeof(PartSystemConfig).GetProperty("PartRegenEnabled").SetMethod,
                "PartRegenEnabled must have no public setter");
        }

        [Test]
        public void PartStateQuery_ReturnsInitialValues()
        {
            var sys = NewSystem(out _, out _, out _);
            var kaiju = PartTestFactory.Kaiju("k",
                PartTestFactory.Part("left_wing", PartType.Normal),
                PartTestFactory.Part("core", PartType.BossCore));
            sys.InitializeParts(kaiju, KaijuId);
            int id = sys.GetPartId("left_wing");

            Assert.AreEqual(0f, sys.GetCurrentHeat(id));
            Assert.AreEqual(100f, sys.GetMaxHeat(id));
            Assert.AreEqual(HeatState.Intact, sys.GetHeatState(id));
            Assert.AreEqual(ArmorState.Intact, sys.GetArmorState(id));
            Assert.IsTrue(sys.IsPartAlive(id));
        }

        [Test]
        public void PartStateQuery_UnknownId_ThrowsOrReportsNotAlive()
        {
            var sys = NewSystem(out _, out _, out _);
            sys.InitializeParts(PartTestFactory.Kaiju("k", PartTestFactory.Part("c", PartType.BossCore)), KaijuId);

            Assert.Throws<KeyNotFoundException>(() => sys.GetCurrentHeat(999));
            Assert.IsFalse(sys.IsPartAlive(999), "unknown part is not alive");
        }
    }
}
