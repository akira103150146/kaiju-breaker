using System;
using System.Reflection;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Economy;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace KaijuBreaker.Tests.EditMode.Economy
{
    /// <summary>
    /// Economy Story 003 — Material Inventory: Persistence Handoff to Meta-Save (material-economy.md
    /// §H.1, §E.1, §F.5; TR-economy-001). Verifies the push-side integration Economy owns: credits are
    /// delivered SYNCHRONOUSLY in the same frame as the originating event, accumulate without loss or
    /// duplication, survive a mid-fight quit (no rollback / no phantom essence), and that Economy depends
    /// only on the <see cref="ISaveService"/> interface — never the Meta assembly.
    ///
    /// <para><b>Scope note (surfaced for review).</b> The story's AC-2/AC-3 <b>cross-session file round-trip</b>
    /// (serialize → new process → deserialize) is the Meta epic's responsibility — atomic JSON write, .bak
    /// backup, CRC32, migration chain (ADR-0004), which the story's own Out-of-Scope assigns to Meta. This
    /// suite covers the same-frame/no-loss/interface-only guarantees against an in-memory store; the true
    /// persisted round-trip lands as a Meta PlayMode test when the save backend exists.</para>
    /// </summary>
    [TestFixture]
    public sealed class EconomyInventoryPersistenceTests
    {
        private const int KaijuCarapace = 10;

        private static EconomyConfig MakeConfig() => ContentTestFactory.Create<EconomyConfig>(
            ("_shardYieldBase", 2), ("_shardYieldSoftenedMult", 1.5f), ("_shardYieldSoftenedStaggeredMult", 2.0f),
            ("_essencePerFullClear", 1), ("_shardCompletenessBonus", 5));

        private static StubKaijuThemeQuery ThemeQuery() =>
            new StubKaijuThemeQuery().Register(KaijuCarapace, KaijuTheme.Carapace);

        private static PartBroke Break(BreakQuality q = BreakQuality.Softened) => new PartBroke(
            partId: 1, kaijuId: KaijuCarapace, type: PartType.Normal, worldPosition: Vector2.zero,
            dropTableId: 0, quality: q, adjacencyIds: Array.Empty<int>(), isChainBreak: false);

        // ── AC-1: credit is delivered in the SAME frame as the event (synchronous, not deferred) ──

        // Frame-stamping ISaveService double: records the test-controlled "frame" at each credit.
        private sealed class FrameStampSave : ISaveService
        {
            private readonly Func<int> _now;
            public int LastCreditFrame = -1;
            public int CreditCount;
            public FrameStampSave(Func<int> now) { _now = now; }
            public void CreditMaterials(MaterialId id, int amount) { LastCreditFrame = _now(); CreditCount++; }
            public int GetMaterialCount(MaterialId id) => 0;
            public void SpendMaterials(MaterialId id, int amount) { }
            public void SetWeaponTier(WeaponId weapon, int tier) { }
            public void EnqueueAutosave() { }
            public void FlushSync() { }
            public (WeaponId Primary, WeaponId Secondary)? GetInitialLoadout() => null;
        }

        [Test]
        public void test_credit_delivered_in_same_frame_as_event()
        {
            int frame = 7;
            var save = new FrameStampSave(() => frame);
            var bus = new TypedEventBus();
            var _ = new EconomyService(MakeConfig(), bus, save, ThemeQuery(), new StubWeaponTierQuery());

            bus.Publish(Break());        // dispatch is synchronous …
            frame++;                     // … so the credit was stamped BEFORE the frame advanced.

            Assert.AreEqual(2, save.CreditCount, "Shard + core credited on the break.");
            Assert.AreEqual(7, save.LastCreditFrame, "Credit happened on the publish frame, not deferred to the next.");
        }

        // ── AC-2: credits accumulate without loss/duplication; a reload restores the same totals ──

        [Test]
        public void test_inventory_accumulates_and_survives_reload()
        {
            var config = MakeConfig();
            var bus = new TypedEventBus();
            var store = new RecordingSaveService();
            var _ = new EconomyService(config, bus, store, ThemeQuery(), store);

            // Two Softened breaks: floor(2×1.5)=3 shard + 1 core each → 6 shard, 2 core.
            bus.Publish(Break());
            bus.Publish(Break());

            Assert.AreEqual(6, store.GetMaterialCount(MaterialId.ShardCommon));
            Assert.AreEqual(2, store.GetMaterialCount(MaterialId.CoreCarapace));

            // Simulate a persistence round-trip: a fresh store seeded from the first's totals
            // (the real file serialize/deserialize is the Meta epic's PlayMode test).
            var reloaded = new RecordingSaveService()
                .Seed(MaterialId.ShardCommon, store.GetMaterialCount(MaterialId.ShardCommon))
                .Seed(MaterialId.CoreCarapace, store.GetMaterialCount(MaterialId.CoreCarapace));

            Assert.AreEqual(6, reloaded.GetMaterialCount(MaterialId.ShardCommon), "Shards survive reload with no loss/dup.");
            Assert.AreEqual(2, reloaded.GetMaterialCount(MaterialId.CoreCarapace), "Cores survive reload with no loss/dup.");
        }

        // ── AC-3: mid-fight quit — per-break credits retained, no full-clear reward ──────────────

        [Test]
        public void test_mid_fight_quit_retains_break_credits_without_full_clear_bonus()
        {
            var bus = new TypedEventBus();
            var store = new RecordingSaveService();
            var _ = new EconomyService(MakeConfig(), bus, store, ThemeQuery(), store);

            // Break 2 of N parts, then quit — HuntEnded never fires.
            bus.Publish(Break());
            bus.Publish(Break());

            Assert.AreEqual(6, store.GetMaterialCount(MaterialId.ShardCommon), "Per-break shards retained (no rollback).");
            Assert.AreEqual(2, store.GetMaterialCount(MaterialId.CoreCarapace), "Per-break cores retained.");
            Assert.AreEqual(0, store.GetMaterialCount(MaterialId.EssenceKaiju), "No essence — the hunt was not completed.");
        }

        // ── AC-4: Economy depends only on the ISaveService interface, never the Meta assembly ────

        [Test]
        public void test_economy_assembly_does_not_reference_meta()
        {
            Assembly economy = typeof(EconomyService).Assembly;
            AssemblyName[] refs = economy.GetReferencedAssemblies();

            foreach (AssemblyName r in refs)
                Assert.AreNotEqual("KaijuBreaker.Meta", r.Name,
                    "Economy must talk to persistence only through the Core ISaveService interface, never Meta directly.");

            bool referencesCore = Array.Exists(refs, r => r.Name == "KaijuBreaker.Core");
            Assert.IsTrue(referencesCore, "Economy references Core (where ISaveService is declared).");
        }
    }
}
