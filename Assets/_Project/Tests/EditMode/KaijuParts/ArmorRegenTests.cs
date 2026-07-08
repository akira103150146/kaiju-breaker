using NUnit.Framework;
using UnityEngine;
using KaijuBreaker.Core;
using KaijuBreaker.Content;
using KaijuBreaker.KaijuParts;
using KaijuBreaker.Tests.EditMode.Helpers;

namespace KaijuBreaker.Tests.EditMode.KaijuParts
{
    /// <summary>
    /// per-part-firing-schema.md §5 / TIDEMAW — per-part break-gauge regen: a part whose break track received
    /// no input for GraceSeconds decays its accumulated break units, clamped at 0, never resurrecting a broken
    /// part. Disabled by default, so existing kaiju (all regen-off) are unaffected.
    /// </summary>
    public sealed class ArmorRegenTests
    {
        private static ArmorRegen Regen(float grace, float rate) =>
            JsonUtility.FromJson<ArmorRegen>(
                "{\"_enabled\":true,\"_graceSeconds\":" + grace + ",\"_regenRatePerSec\":" + rate + "}");

        private static PartStateSystem BuildWithRegenMaw(out RecordingEventBus bus, float grace, float rate)
        {
            var maw = PartTestFactory.Part("maw", PartType.Normal, "boss_drop", bMaxOverride: 100f);
            PartTestFactory.SetField(maw, "_armorRegen", Regen(grace, rate));
            var core = PartTestFactory.Part("core", PartType.BossCore, "boss_drop");
            bus = new RecordingEventBus();
            var sys = new PartStateSystem(bus, PartTestFactory.Balance(), PartTestFactory.PartConfig());
            sys.InitializeParts(PartTestFactory.Kaiju("tidemaw", maw, core), 0); // maw = part id 0
            return sys;
        }

        [Test]
        public void Regen_HoldsWithinGrace_ThenDecays()
        {
            var sys = BuildWithRegenMaw(out var bus, grace: 2.0f, rate: 5.0f);
            bus.Publish(new MissileHit(0, 0, 15f, WeaponId.M1)); // fill the maw's break gauge
            float filled = sys.Parts[0].BCurrent;
            Assert.Greater(filled, 0f, "missile filled the break gauge");

            sys.Tick(1.0f); // within grace (1.0 < 2.0) — no decay
            Assert.AreEqual(filled, sys.Parts[0].BCurrent, 0.001f, "no decay within the grace window");

            sys.Tick(1.5f); // t = 2.5 >= grace — decays rate*dt = 7.5
            Assert.Less(sys.Parts[0].BCurrent, filled, "gauge decayed past the grace window");
            Assert.GreaterOrEqual(sys.Parts[0].BCurrent, 0f, "never below zero");
        }

        [Test]
        public void Regen_ResetsGraceOnBreakHit()
        {
            var sys = BuildWithRegenMaw(out var bus, grace: 2.0f, rate: 5.0f);
            bus.Publish(new MissileHit(0, 0, 15f, WeaponId.M1));
            sys.Tick(1.9f);                       // t = 1.9, still within grace
            bus.Publish(new MissileHit(0, 0, 5f, WeaponId.M1)); // another break hit -> resets grace timer
            float after = sys.Parts[0].BCurrent;
            sys.Tick(1.0f);                       // t = 1.0 after reset < grace — must NOT decay
            Assert.AreEqual(after, sys.Parts[0].BCurrent, 0.001f, "break hit reset the grace timer");
        }

        [Test]
        public void Regen_DisabledPart_NeverDecays()
        {
            // Default part has ArmorRegen disabled — gauge must persist indefinitely.
            var maw = PartTestFactory.Part("maw", PartType.Normal, "boss_drop", bMaxOverride: 100f);
            var core = PartTestFactory.Part("core", PartType.BossCore, "boss_drop");
            var bus = new RecordingEventBus();
            var sys = new PartStateSystem(bus, PartTestFactory.Balance(), PartTestFactory.PartConfig());
            sys.InitializeParts(PartTestFactory.Kaiju("k", maw, core), 0);
            bus.Publish(new MissileHit(0, 0, 15f, WeaponId.M1));
            float filled = sys.Parts[0].BCurrent;
            sys.Tick(10f); // long tick, way past any grace
            Assert.AreEqual(filled, sys.Parts[0].BCurrent, 0.001f, "regen-off part keeps its gauge");
        }
    }
}
