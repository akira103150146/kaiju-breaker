using System;
using System.Linq;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Stage;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace KaijuBreaker.Tests.EditMode.Stage
{
    /// <summary>
    /// Stage Story 002 (logic core) — pool-model wave planning + spawn layout (stage-system.md §D.2/§L.1/§L.3;
    /// ADR-0003). Verifies the difficulty-scaled counts, timing offsets, layout geometry, elite flagging, and
    /// determinism that any WaveSpawner sits on — all in EditMode (no scene/prefab).
    ///
    /// <para><b>Scope reconciliation:</b> the committed <see cref="SegmentDef"/> is the pool model (not the
    /// story's authored <c>waves[]</c>); pacing/geometry come from <see cref="WaveTimingConfig"/> (ADR-0003).
    /// The runtime Instantiate/EnemyController/PlayMode part + enemy prefabs are a follow-up (need art +
    /// PlayMode infra); enemy bullet emission is blocked by ADR-0001.</para>
    /// </summary>
    [TestFixture]
    public sealed class WavePlannerTests
    {
        /// <summary>Minimal in-test <see cref="IDifficultyProvider"/> with a settable enemy-count multiplier.</summary>
        private sealed class FakeDifficulty : IDifficultyProvider
        {
            public DifficultyTier CurrentTier { get; set; } = DifficultyTier.D1;
            public float BulletDensityMult { get; set; } = 1f;
            public float EnemyCountMult { get; set; } = 1f;
        }

        private static EnemyDef Enemy(string id) => ContentTestFactory.Create<EnemyDef>(("_enemyId", id));

        private static WaveTimingConfig Timing(int baseCount = 4, float interval = 6f,
            SpawnLayout layout = SpawnLayout.HorizontalSpread, float width = 8f) =>
            ContentTestFactory.Create<WaveTimingConfig>(
                ("_enemiesPerWaveBase", baseCount), ("_waveIntervalSeconds", interval),
                ("_defaultLayout", layout), ("_fieldWidth", width), ("_spawnY", 6f), ("_columnSpacing", 1.2f));

        private static SegmentDef Segment(EnemyDef[] pool, int waveCount, int eliteWaveIndex) =>
            ContentTestFactory.Create<SegmentDef>(
                ("_segmentId", "s1_01"), ("_difficultyWeight", 1),
                ("_enemyPool", pool), ("_waveCount", waveCount), ("_eliteWaveIndex", eliteWaveIndex));

        private static EnemyDef[] FourMvpMobs() =>
            new[] { Enemy("ram_grub"), Enemy("tri_shot"), Enemy("aimed_gun"), Enemy("ring_burst") };

        // ── AC-1: D1 baseline count ───────────────────────────────────────────

        [Test]
        public void test_d1_baseline_spawns_base_count_per_wave()
        {
            var planner = new WavePlanner(Timing(baseCount: 4), new FakeDifficulty { EnemyCountMult = 1f }, new Random(1));
            var plan = planner.Plan(Segment(FourMvpMobs(), waveCount: 2, eliteWaveIndex: 1));

            Assert.AreEqual(8, plan.Count, "2 waves × 4 = 8 at D1");
            Assert.AreEqual(4, plan.Count(i => i.WaveIndex == 0), "wave 0 has 4");
        }

        // ── AC-2: difficulty multiplier + CeilToInt ───────────────────────────

        [Test]
        public void test_d3_multiplier_scales_count_with_ceil()
        {
            var planner = new WavePlanner(Timing(baseCount: 4), new FakeDifficulty { EnemyCountMult = 1.5f }, new Random(1));
            var plan = planner.Plan(Segment(FourMvpMobs(), waveCount: 2, eliteWaveIndex: 1));

            Assert.AreEqual(12, plan.Count, "ceil(4×1.5)=6 per wave × 2 = 12");
        }

        [Test]
        public void test_fractional_multiplier_uses_ceiling_not_round()
        {
            var planner = new WavePlanner(Timing(baseCount: 4), new FakeDifficulty { EnemyCountMult = 1.1f }, new Random(1));
            var plan = planner.Plan(Segment(FourMvpMobs(), waveCount: 1, eliteWaveIndex: -1));

            Assert.AreEqual(5, plan.Count, "ceil(4×1.1=4.4)=5, not round(4)");
        }

        // ── AC-4: waves grouped by index + intra-wave stagger ─────────────────

        [Test]
        public void test_enemies_are_grouped_by_wave_index()
        {
            var planner = new WavePlanner(Timing(baseCount: 2, interval: 4f), new FakeDifficulty(), new Random(1));
            var plan = planner.Plan(Segment(FourMvpMobs(), waveCount: 2, eliteWaveIndex: -1));

            Assert.AreEqual(2, plan.Count(i => i.WaveIndex == 0), "wave 0 holds its enemies");
            Assert.AreEqual(2, plan.Count(i => i.WaveIndex == 1), "wave 1 holds its enemies");
            Assert.IsFalse(plan.Any(i => i.WaveIndex > 1), "no phantom waves");
        }

        [Test]
        public void test_spawn_time_is_intra_wave_stagger_not_absolute()
        {
            // Each wave's own enemies are staggered from 0; the SpawnTime is a within-wave offset, NOT wave×interval
            // (between-wave timing is clear-gated at runtime).
            var planner = new WavePlanner(Timing(baseCount: 3), new FakeDifficulty(), new Random(1));
            var plan = planner.Plan(Segment(FourMvpMobs(), waveCount: 2, eliteWaveIndex: -1));

            Assert.AreEqual(0f, plan.Where(i => i.WaveIndex == 0).Min(i => i.SpawnTime), "wave 0 starts a spawn at t=0");
            Assert.AreEqual(0f, plan.Where(i => i.WaveIndex == 1).Min(i => i.SpawnTime), "wave 1 ALSO starts at its own t=0");
            Assert.Less(plan.Max(i => i.SpawnTime), 6f, "no absolute wave×interval offsets remain");
        }

        // ── Elite interspersion (mixed into a wave at a random slot, not leading it) ──

        [Test]
        public void test_one_elite_seeded_on_the_elite_wave_only()
        {
            var planner = new WavePlanner(Timing(baseCount: 3), new FakeDifficulty(), new Random(1));
            var plan = planner.Plan(Segment(FourMvpMobs(), waveCount: 3, eliteWaveIndex: 1));

            Assert.AreEqual(1, plan.Count(i => i.IsElite), "exactly one elite across the segment");
            var elite = plan.First(i => i.IsElite);
            Assert.AreEqual(1, elite.WaveIndex, "the elite is on wave index 1");
        }

        [Test]
        public void test_no_elite_when_index_is_negative()
        {
            var planner = new WavePlanner(Timing(), new FakeDifficulty(), new Random(1));
            var plan = planner.Plan(Segment(FourMvpMobs(), waveCount: 2, eliteWaveIndex: -1));
            Assert.AreEqual(0, plan.Count(i => i.IsElite));
        }

        // ── Determinism + guards ──────────────────────────────────────────────

        [Test]
        public void test_same_seed_yields_identical_enemy_sequence()
        {
            var pool = FourMvpMobs();
            string[] Draw() => new WavePlanner(Timing(), new FakeDifficulty(), new Random(7))
                .Plan(Segment(pool, 2, 1)).Select(i => i.Enemy.EnemyId).ToArray();

            CollectionAssert.AreEqual(Draw(), Draw());
        }

        [Test]
        public void test_empty_pool_yields_empty_plan_without_crashing()
        {
            var planner = new WavePlanner(Timing(), new FakeDifficulty(), new Random(1));
            var plan = planner.Plan(Segment(Array.Empty<EnemyDef>(), waveCount: 3, eliteWaveIndex: 0));
            Assert.AreEqual(0, plan.Count);
        }

        // ── SpawnLayoutHelper geometry ────────────────────────────────────────

        [Test]
        public void test_horizontal_spread_is_symmetric_and_within_field()
        {
            var pos = SpawnLayoutHelper.Positions(SpawnLayout.HorizontalSpread, 5, fieldWidth: 8f, spawnY: 6f, columnSpacing: 1.2f);
            Assert.AreEqual(5, pos.Length);
            Assert.AreEqual(-4f, pos[0].x, 0.001f);
            Assert.AreEqual(4f, pos[4].x, 0.001f);
            Assert.AreEqual(0f, pos[2].x, 0.001f, "middle enemy centred");
            foreach (var p in pos) Assert.LessOrEqual(Mathf.Abs(p.x), 4.001f, "within ±fieldWidth/2");
        }

        [Test]
        public void test_center_layout_places_all_at_origin_x()
        {
            var pos = SpawnLayoutHelper.Positions(SpawnLayout.Center, 3, 8f, 6f, 1.2f);
            foreach (var p in pos) Assert.AreEqual(0f, p.x, 0.001f);
        }

        [Test]
        public void test_column_layout_descends_by_spacing()
        {
            var pos = SpawnLayoutHelper.Positions(SpawnLayout.Column, 3, 8f, 6f, columnSpacing: 1.5f);
            Assert.AreEqual(6f, pos[0].y, 0.001f);
            Assert.AreEqual(4.5f, pos[1].y, 0.001f);
            Assert.AreEqual(3f, pos[2].y, 0.001f);
        }

        [Test]
        public void test_layout_zero_count_returns_empty()
        {
            Assert.AreEqual(0, SpawnLayoutHelper.Positions(SpawnLayout.HorizontalSpread, 0, 8f, 6f, 1.2f).Length);
        }
    }
}
