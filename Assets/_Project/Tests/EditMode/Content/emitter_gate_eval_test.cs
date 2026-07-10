using NUnit.Framework;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Tests.EditMode.Content
{
    /// <summary>
    /// Truth-table for <see cref="EmitterGateEval"/> — the per-part emitter fire gates
    /// (per-part-firing-schema.md §3). Covers the new SilenceWhenSoftenedOrStripped gate
    /// ("PRISMSHELL facet stops firing once softened OR its armor is stripped").
    /// </summary>
    public class EmitterGateEvalTests
    {
        // ── AliveOnly (default): always open here; break-silencing is the caller's job ──
        [Test]
        public void test_alive_only_always_open()
        {
            Assert.IsTrue(EmitterGateEval.IsOpen(PartFireGate.AliveOnly, HeatState.Intact, ArmorState.Intact, false));
            Assert.IsTrue(EmitterGateEval.IsOpen(PartFireGate.AliveOnly, HeatState.Softened, ArmorState.Stripped, false));
        }

        // ── SilenceWhenSoftened: open until softened, armor irrelevant ──
        [Test]
        public void test_silence_when_softened_open_while_intact()
        {
            Assert.IsTrue(EmitterGateEval.IsOpen(PartFireGate.SilenceWhenSoftened, HeatState.Intact, ArmorState.Intact, false));
            Assert.IsTrue(EmitterGateEval.IsOpen(PartFireGate.SilenceWhenSoftened, HeatState.Intact, ArmorState.Stripped, false));
        }

        [Test]
        public void test_silence_when_softened_closed_once_softened()
        {
            Assert.IsFalse(EmitterGateEval.IsOpen(PartFireGate.SilenceWhenSoftened, HeatState.Softened, ArmorState.Intact, false));
        }

        // ── SilenceWhenSoftenedOrStripped (new): closed if EITHER softened OR stripped ──
        [Test]
        public void test_softened_or_stripped_open_only_when_intact_and_armored()
        {
            Assert.IsTrue(EmitterGateEval.IsOpen(PartFireGate.SilenceWhenSoftenedOrStripped, HeatState.Intact, ArmorState.Intact, false));
        }

        [Test]
        public void test_softened_or_stripped_closed_when_softened()
        {
            Assert.IsFalse(EmitterGateEval.IsOpen(PartFireGate.SilenceWhenSoftenedOrStripped, HeatState.Softened, ArmorState.Intact, false));
        }

        [Test]
        public void test_softened_or_stripped_closed_when_stripped()
        {
            // The "剝甲也停火" case — armor stripped alone now silences the facet (previously it kept firing).
            Assert.IsFalse(EmitterGateEval.IsOpen(PartFireGate.SilenceWhenSoftenedOrStripped, HeatState.Intact, ArmorState.Stripped, false));
        }

        [Test]
        public void test_softened_or_stripped_closed_when_both()
        {
            Assert.IsFalse(EmitterGateEval.IsOpen(PartFireGate.SilenceWhenSoftenedOrStripped, HeatState.Softened, ArmorState.Stripped, false));
        }

        // ── RequireArmorStripped: only fires once armor is stripped ──
        [Test]
        public void test_require_armor_stripped()
        {
            Assert.IsFalse(EmitterGateEval.IsOpen(PartFireGate.RequireArmorStripped, HeatState.Intact, ArmorState.Intact, false));
            Assert.IsTrue(EmitterGateEval.IsOpen(PartFireGate.RequireArmorStripped, HeatState.Intact, ArmorState.Stripped, false));
        }

        // ── RequireGatePartBroken: gated on the referenced part, not owner state ──
        [Test]
        public void test_require_gate_part_broken()
        {
            Assert.IsFalse(EmitterGateEval.IsOpen(PartFireGate.RequireGatePartBroken, HeatState.Softened, ArmorState.Stripped, false));
            Assert.IsTrue(EmitterGateEval.IsOpen(PartFireGate.RequireGatePartBroken, HeatState.Intact, ArmorState.Intact, true));
        }
    }
}
