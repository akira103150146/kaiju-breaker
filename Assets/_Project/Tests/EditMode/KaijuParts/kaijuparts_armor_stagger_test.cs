using KaijuBreaker.Core;
using KaijuBreaker.KaijuParts;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.KaijuParts
{
    /// <summary>
    /// Story 003 — Armor gate &amp; stagger timer. Verifies L3 wave strips ARMORED armor and
    /// starts the stagger window, NORMAL/BOSS_CORE stagger without armor change, per-frame
    /// countdown with clamp, armor restore + B_current preservation at expiry, non-additive
    /// overlap reset, and BROKEN immunity (kaiju-part-system.md D.4/E.2).
    /// </summary>
    public sealed class KaijuPartsArmorStaggerTests
    {
        private const int Kaiju = 3;
        private const float Dur = 2.0f; // WeaponBalanceConfig default StaggerDuration

        private static PartStateSystem Build(out RecordingEventBus bus)
        {
            bus = new RecordingEventBus();
            var sys = new PartStateSystem(bus, PartTestFactory.Balance(), PartTestFactory.PartConfig());
            sys.InitializeParts(PartTestFactory.Kaiju("k",
                PartTestFactory.Part("normal", PartType.Normal),
                PartTestFactory.Part("armored", PartType.Armored),
                PartTestFactory.Part("core", PartType.BossCore)), Kaiju);
            return sys;
        }

        [Test]
        public void ArmoredPart_WaveHit_StripsArmor_StartsStagger()
        {
            var sys = Build(out var bus);
            int id = sys.GetPartId("armored");

            bus.Publish(new WaveHit(id, Kaiju));

            Assert.AreEqual(ArmorState.Stripped, sys.Parts[id].ArmorState);
            Assert.AreEqual(Dur, sys.Parts[id].StaggerTimer, 1e-4f);
            Assert.AreEqual(1, bus.CountOf<PartStaggered>());
            var e = bus.Events<PartStaggered>()[0];
            Assert.IsTrue(e.ArmorStripped);
            Assert.AreEqual(Dur, e.Duration, 1e-4f);
        }

        [Test]
        public void ArmoredPart_Broken_WaveHitIgnored()
        {
            var sys = Build(out var bus);
            int id = sys.GetPartId("armored");
            sys.Parts[id].BreakState = BreakState.Broken;

            bus.Publish(new WaveHit(id, Kaiju));

            Assert.AreEqual(ArmorState.Intact, sys.Parts[id].ArmorState, "no change");
            Assert.AreEqual(0f, sys.Parts[id].StaggerTimer);
            Assert.AreEqual(0, bus.CountOf<PartStaggered>());
        }

        [Test]
        public void NormalAndBossCore_Stagger_NoArmorChange()
        {
            var sys = Build(out var bus);
            foreach (var name in new[] { "normal", "core" })
            {
                int id = sys.GetPartId(name);
                bus.Publish(new WaveHit(id, Kaiju));
                Assert.AreEqual(Dur, sys.Parts[id].StaggerTimer, 1e-4f, $"{name} staggered");
                Assert.AreEqual(ArmorState.Intact, sys.Parts[id].ArmorState, $"{name} no armor change");
            }
            foreach (var e in bus.Events<PartStaggered>())
                Assert.IsFalse(e.ArmorStripped, "non-armored → ArmorStripped=false");
        }

        [Test]
        public void StaggerTimer_CountsDown_ClampsAtZero()
        {
            var sys = Build(out var bus);
            int id = sys.GetPartId("armored");
            sys.Parts[id].StaggerTimer = 2.0f;

            sys.TickStagger(0.016f);
            Assert.AreEqual(1.984f, sys.Parts[id].StaggerTimer, 1e-4f);
            Assert.AreEqual(0, bus.CountOf<PartStaggerEnd>());

            sys.Parts[id].StaggerTimer = 0.01f;
            sys.Parts[id].ArmorState = ArmorState.Stripped;
            sys.TickStagger(0.016f);
            Assert.AreEqual(0f, sys.Parts[id].StaggerTimer, "Mathf.Max floors at 0, never negative");
            Assert.AreEqual(1, bus.CountOf<PartStaggerEnd>());
        }

        [Test]
        public void ArmorRestoresAtExpiry_BCurrentPreserved()
        {
            var sys = Build(out var bus);
            int id = sys.GetPartId("armored");
            sys.Parts[id].ArmorState = ArmorState.Stripped;
            sys.Parts[id].StaggerTimer = 0.01f;
            sys.Parts[id].BCurrent = 75f;

            sys.TickStagger(0.016f);

            Assert.AreEqual(0f, sys.Parts[id].StaggerTimer);
            Assert.AreEqual(ArmorState.Intact, sys.Parts[id].ArmorState, "armor restored");
            Assert.AreEqual(75f, sys.Parts[id].BCurrent, 1e-4f, "B_current NOT reset on armor restore");
            var e = bus.Events<PartStaggerEnd>()[0];
            Assert.IsTrue(e.ArmorRestored);
        }

        [Test]
        public void NormalPart_Expiry_ArmorRestoredFalse()
        {
            var sys = Build(out var bus);
            int id = sys.GetPartId("normal");
            sys.Parts[id].StaggerTimer = 0.01f;

            sys.TickStagger(0.016f);

            Assert.AreEqual(1, bus.CountOf<PartStaggerEnd>());
            Assert.IsFalse(bus.Events<PartStaggerEnd>()[0].ArmorRestored);
        }

        [Test]
        public void OverlappingWave_ResetsTimer_NotAdditive()
        {
            var sys = Build(out _);
            int id = sys.GetPartId("armored");
            var bus2 = new RecordingEventBus();
            // rebuild on a fresh bus to isolate counts
            sys = new PartStateSystem(bus2, PartTestFactory.Balance(), PartTestFactory.PartConfig());
            sys.InitializeParts(PartTestFactory.Kaiju("k",
                PartTestFactory.Part("armored", PartType.Armored),
                PartTestFactory.Part("core", PartType.BossCore)), Kaiju);
            id = sys.GetPartId("armored");

            sys.Parts[id].StaggerTimer = 1.0f;       // mid-stagger
            sys.Parts[id].ArmorState = ArmorState.Stripped;

            bus2.Publish(new WaveHit(id, Kaiju));     // overlap
            Assert.AreEqual(2.0f, sys.Parts[id].StaggerTimer, 1e-4f, "reset to duration, not 3.0");
            Assert.AreEqual(ArmorState.Stripped, sys.Parts[id].ArmorState);

            bus2.Publish(new WaveHit(id, Kaiju));     // same-frame second hit — idempotent
            Assert.AreEqual(2.0f, sys.Parts[id].StaggerTimer, 1e-4f);
        }

        [Test]
        public void CrossWindow_BuNotZeroed_BetweenWindows()
        {
            var sys = Build(out var bus);
            int id = sys.GetPartId("armored");

            // Window 1 ends with accumulated BU and armor restored.
            sys.Parts[id].BCurrent = 40f;
            sys.Parts[id].ArmorState = ArmorState.Intact;

            bus.Publish(new WaveHit(id, Kaiju)); // window 2 opens
            Assert.AreEqual(ArmorState.Stripped, sys.Parts[id].ArmorState);
            Assert.AreEqual(40f, sys.Parts[id].BCurrent, 1e-4f, "BU carried across windows (no decay)");
        }
    }
}
