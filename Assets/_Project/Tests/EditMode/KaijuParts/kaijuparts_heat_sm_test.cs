using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.KaijuParts;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.KaijuParts
{
    /// <summary>
    /// Story 002 — Heat state machine (INTACT ↔ SOFTENED). Verifies fill-vs-decay mutual
    /// exclusion per frame, clamping, the hysteresis band [θ_S_exit, θ_S), single-fire
    /// transitions, and that BROKEN parts freeze. Laser hits arrive via the event bus and
    /// are applied at TickHeat (kaiju-part-system.md D.1/D.2).
    /// </summary>
    public sealed class KaijuPartsHeatStateMachineTests
    {
        private const int Kaiju = 1;

        private static PartStateSystem Single(out RecordingEventBus bus, out int partId, PartType type = PartType.Normal)
        {
            bus = new RecordingEventBus();
            var sys = new PartStateSystem(bus, PartTestFactory.Balance(), PartTestFactory.PartConfig());
            sys.InitializeParts(PartTestFactory.Kaiju("k",
                PartTestFactory.Part("p", type),
                PartTestFactory.Part("core", PartType.BossCore)), Kaiju);
            partId = sys.GetPartId("p");
            return sys;
        }

        [Test]
        public void LaserHit_FillsHeat_DecaySuppressedSameFrame()
        {
            var sys = Single(out var bus, out int id);
            sys.Parts[id].HCurrent = 50f;

            bus.Publish(new LaserHit(id, Kaiju, 10f));
            sys.TickHeat(1.0f);

            Assert.AreEqual(60f, sys.Parts[id].HCurrent, 1e-4f, "fill applied, decay NOT applied");
            Assert.AreEqual(0, bus.CountOf<PartSoftened>(), "60 < theta_S");
        }

        [Test]
        public void NonPositiveHeatDelta_Ignored()
        {
            var sys = Single(out var bus, out int id);
            sys.Parts[id].HCurrent = 50f;

            bus.Publish(new LaserHit(id, Kaiju, 0f));   // rejected at source (delta must be > 0)
            bus.Publish(new LaserHit(id, Kaiju, -5f));
            sys.TickHeat(1.0f);

            Assert.AreEqual(47f, sys.Parts[id].HCurrent, 1e-4f, "no fill queued → decays instead");
        }

        [Test]
        public void NoLaser_HeatDecays_ClampedAtZero()
        {
            var sys = Single(out _, out int id);
            sys.Parts[id].HCurrent = 50f;
            sys.TickHeat(1.0f);
            Assert.AreEqual(47f, sys.Parts[id].HCurrent, 1e-4f);

            sys.Parts[id].HCurrent = 2f;
            sys.TickHeat(1.0f);
            Assert.AreEqual(0f, sys.Parts[id].HCurrent, 1e-4f, "clamped to 0, not -1");

            sys.TickHeat(1.0f);
            Assert.AreEqual(0f, sys.Parts[id].HCurrent, 1e-4f, "stays at 0");
        }

        [Test]
        public void IntactToSoftened_FiresOnce_WithHeatPayload()
        {
            var sys = Single(out var bus, out int id);
            sys.Parts[id].HCurrent = 99f;

            bus.Publish(new LaserHit(id, Kaiju, 2f));
            sys.TickHeat(1.0f);

            Assert.AreEqual(100f, sys.Parts[id].HCurrent, 1e-4f, "clamped to H_max");
            Assert.AreEqual(HeatState.Softened, sys.Parts[id].HeatState);
            Assert.AreEqual(1, bus.CountOf<PartSoftened>());
            var soft = bus.Events<PartSoftened>()[0];
            Assert.AreEqual(100f, soft.CurrentHeat, 1e-4f);
            Assert.AreEqual(100f, soft.MaxHeat, 1e-4f);

            // Stays softened next frame (still >= theta_S) → no duplicate.
            bus.Publish(new LaserHit(id, Kaiju, 50f));
            sys.TickHeat(1.0f);
            Assert.AreEqual(1, bus.CountOf<PartSoftened>(), "no duplicate PartSoftened");
        }

        [Test]
        public void HysteresisBand_NoEvent_WhenBetweenExitAndEntry()
        {
            var sys = Single(out var bus, out int id);
            sys.Parts[id].HeatState = HeatState.Softened;
            sys.Parts[id].HCurrent = 90f;

            sys.TickHeat(1.0f); // decays to 87, still in [80, 100)
            Assert.AreEqual(87f, sys.Parts[id].HCurrent, 1e-4f);
            Assert.AreEqual(HeatState.Softened, sys.Parts[id].HeatState);
            Assert.AreEqual(0, bus.CountOf<PartSoftenedExit>());

            // Boundary: H == theta_S_exit (80) is NOT < 80 → remains softened.
            sys.Parts[id].HCurrent = 80f;
            sys.TickHeat(0f);
            Assert.AreEqual(HeatState.Softened, sys.Parts[id].HeatState);
            Assert.AreEqual(0, bus.CountOf<PartSoftenedExit>());
        }

        [Test]
        public void SoftenedToIntact_FiresOnce()
        {
            var sys = Single(out var bus, out int id);
            sys.Parts[id].HeatState = HeatState.Softened;
            sys.Parts[id].HCurrent = 79f;

            sys.TickHeat(0.016f); // 79 - 3*0.016 = ~78.95 < 80
            Assert.AreEqual(HeatState.Intact, sys.Parts[id].HeatState);
            Assert.AreEqual(1, bus.CountOf<PartSoftenedExit>());

            sys.TickHeat(0.016f); // now intact, no second exit
            Assert.AreEqual(1, bus.CountOf<PartSoftenedExit>());
        }

        [Test]
        public void BrokenPart_IgnoresLaser()
        {
            var sys = Single(out var bus, out int id);
            sys.Parts[id].HCurrent = 50f;
            sys.Parts[id].BreakState = BreakState.Broken;

            bus.Publish(new LaserHit(id, Kaiju, 40f));
            sys.TickHeat(1.0f);

            Assert.AreEqual(50f, sys.Parts[id].HCurrent, 1e-4f, "broken part frozen — no fill, no decay");
            Assert.AreEqual(0, bus.CountOf<PartSoftened>());
            Assert.AreEqual(HeatState.Intact, sys.Parts[id].HeatState);
        }
    }
}
