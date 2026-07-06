using System;
using System.Collections.Generic;
using System.Linq;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.Stage;
using KaijuBreaker.Tests.EditMode.Helpers;
using NUnit.Framework;

namespace KaijuBreaker.Tests.EditMode.Stage
{
    /// <summary>
    /// Stage Story 003 — 波段池隨機重組 (stage-system.md §D.1, TR-stage-001; ADR-0005/0003).
    /// Drives the pure-C# <see cref="SegmentRecombinator"/> with seeded <see cref="System.Random"/> for
    /// determinism and <see cref="ContentTestFactory"/>-built <see cref="StageDef"/> / <see cref="SegmentDef"/>
    /// fixtures — no scene or Unity runtime needed.
    ///
    /// <para><b>Reconciliations vs story text (surfaced for review):</b>
    /// (1) Two Content fields were missing and are added by this story: <c>SegmentDef.DifficultyWeight</c>
    /// (int 1–5, needed for the ascending order) and <c>StageDef.IntroSegment</c>/<c>PreBossLullSegment</c>
    /// (the fixed bookends the SegmentSequence carries). (2) The recombinator ctor takes a
    /// <see cref="DifficultyTier"/> enum rather than a bare int — type-safe and matching
    /// <c>IDifficultyProvider.CurrentTier</c>; the gate compares enum ordering, sidestepping the story's
    /// 1-based tier numbering. (3) Step-2 relaxation restores the FULL pool per the story's literal 還原至全池.</para>
    /// </summary>
    [TestFixture]
    public sealed class SegmentRecombinationTests
    {
        // ── Fixture builders ──────────────────────────────────────────────────

        private static SegmentDef Seg(string id, int weight, DifficultyTier minTier = DifficultyTier.D1) =>
            ContentTestFactory.Create<SegmentDef>(
                ("_segmentId", id),
                ("_difficultyWeight", weight),
                ("_minDifficultyTier", minTier));

        private static StageDef Stage(int drawCount, SegmentDef[] pool,
                                      SegmentDef intro = null, SegmentDef lull = null, string boss = "carapex") =>
            ContentTestFactory.Create<StageDef>(
                ("_stageId", "stage_test"),
                ("_bossKaijuId", boss),
                ("_segmentDrawCount", drawCount),
                ("_segmentPool", pool),
                ("_introSegment", intro),
                ("_preBossLullSegment", lull));

        /// <summary>Weights 1,2,2,3,3 with ids s1_01..s1_05 — the §L.1 canonical 5-pool.</summary>
        private static SegmentDef[] CanonicalPool() => new[]
        {
            Seg("s1_01", 1), Seg("s1_02", 2), Seg("s1_03", 2), Seg("s1_04", 3), Seg("s1_05", 3),
        };

        private static void AssertNonDecreasingWeights(IReadOnlyList<SegmentDef> segs)
        {
            for (int i = 1; i < segs.Count; i++)
                Assert.LessOrEqual(segs[i - 1].DifficultyWeight, segs[i].DifficultyWeight,
                    $"segment {i - 1}→{i} weight must be non-decreasing (lightest-first)");
        }

        // ── AC-1: count + ascending order + no-repeat ─────────────────────────

        [Test]
        public void test_recombine_draws_n_ascending_and_excludes_norepeat()
        {
            // Arrange
            var stage = Stage(3, CanonicalPool());
            var rec = new SegmentRecombinator(stage, DifficultyTier.D1, new Random(42));

            // Act — s1_02 played last run should be excluded (pool 5 > draw 3)
            var seq = rec.Recombine(new[] { "s1_02" });

            // Assert
            Assert.AreEqual(3, seq.EscalatingSegments.Count);
            CollectionAssert.DoesNotContain(seq.EscalatingSegments.Select(s => s.SegmentId), "s1_02");
            AssertNonDecreasingWeights(seq.EscalatingSegments);
            Assert.AreEqual("carapex", seq.BossKaijuId);
        }

        // ── AC-2: difficulty gate ─────────────────────────────────────────────

        [Test]
        public void test_recombine_difficulty_gate_excludes_higher_tier_segments()
        {
            // Arrange — pool minTiers [D1,D1,D3]; player at D1; draw 2 (the two D1 segments fill it)
            var pool = new[] { Seg("a", 1, DifficultyTier.D1), Seg("b", 2, DifficultyTier.D1), Seg("c", 3, DifficultyTier.D3) };
            var rec = new SegmentRecombinator(Stage(2, pool), DifficultyTier.D1, new Random(1));

            // Act
            var seq = rec.Recombine(Array.Empty<string>());

            // Assert — the D3-gated segment must never appear at D1 when D1 content can fill the draw
            var ids = seq.EscalatingSegments.Select(s => s.SegmentId).ToList();
            Assert.AreEqual(2, ids.Count);
            CollectionAssert.DoesNotContain(ids, "c");
        }

        [Test]
        public void test_recombine_difficulty_gate_relaxes_to_full_pool_when_starved()
        {
            // Arrange — only 1 segment passes the D1 gate but draw=2 → must relax to the full pool
            var pool = new[] { Seg("a", 1, DifficultyTier.D1), Seg("b", 2, DifficultyTier.D3), Seg("c", 3, DifficultyTier.D3) };
            var rec = new SegmentRecombinator(Stage(2, pool), DifficultyTier.D1, new Random(3));

            // Act
            var seq = rec.Recombine(Array.Empty<string>());

            // Assert — a full draw of 2 is only possible by relaxing the gate (couldn't reach 2 from 1)
            Assert.AreEqual(2, seq.EscalatingSegments.Count);
            AssertNonDecreasingWeights(seq.EscalatingSegments);
        }

        // ── AC-3: pool <= N skips no-repeat ───────────────────────────────────

        [Test]
        public void test_recombine_skips_norepeat_when_pool_equals_drawcount()
        {
            // Arrange — pool 3, draw 3; s1_03 was played last run but must still appear (can't drop it)
            var pool = new[] { Seg("s1_01", 1), Seg("s1_02", 2), Seg("s1_03", 3) };
            var rec = new SegmentRecombinator(Stage(3, pool), DifficultyTier.D1, new Random(5));

            // Act
            var seq = rec.Recombine(new[] { "s1_03" });

            // Assert
            var ids = seq.EscalatingSegments.Select(s => s.SegmentId).ToList();
            Assert.AreEqual(3, ids.Count);
            CollectionAssert.Contains(ids, "s1_03");
        }

        // ── AC-4: determinism under a fixed seed ──────────────────────────────

        [Test]
        public void test_recombine_is_deterministic_for_fixed_seed()
        {
            // Arrange — same StageDef; a fresh Random(7) each iteration must reproduce the exact draw
            var stage = Stage(3, CanonicalPool());
            var last = new[] { "s1_01" };

            List<string> Draw() =>
                new SegmentRecombinator(stage, DifficultyTier.D2, new Random(7))
                    .Recombine(last).EscalatingSegments.Select(s => s.SegmentId).ToList();

            var baseline = Draw();

            // Act + Assert — 100 rebuilds are byte-for-byte identical
            for (int i = 0; i < 100; i++)
                CollectionAssert.AreEqual(baseline, Draw(), $"iteration {i} diverged from the seeded baseline");
        }

        // ── AC-5: 100 seeds — validity + coverage ─────────────────────────────

        [Test]
        public void test_recombine_100_seeds_valid_and_covers_every_segment()
        {
            // Arrange
            var stage = Stage(3, CanonicalPool());
            var seen = new HashSet<string>();

            // Act
            for (int seed = 0; seed < 100; seed++)
            {
                var seq = new SegmentRecombinator(stage, DifficultyTier.D1, new Random(seed))
                    .Recombine(Array.Empty<string>());

                Assert.AreEqual(3, seq.EscalatingSegments.Count, $"seed {seed}: draw count");
                AssertNonDecreasingWeights(seq.EscalatingSegments);
                foreach (var s in seq.EscalatingSegments) seen.Add(s.SegmentId);
            }

            // Assert — every pool segment surfaced at least once across 100 seeds (uniformity smoke test)
            foreach (var id in new[] { "s1_01", "s1_02", "s1_03", "s1_04", "s1_05" })
                CollectionAssert.Contains(seen, id, $"segment {id} never drawn in 100 seeds");
        }

        // ── Bookends + constructor guards ─────────────────────────────────────

        [Test]
        public void test_recombine_carries_fixed_intro_and_lull_bookends()
        {
            // Arrange
            var intro = Seg("intro", 1);
            var lull = Seg("lull", 5);
            var rec = new SegmentRecombinator(Stage(3, CanonicalPool(), intro, lull), DifficultyTier.D1, new Random(9));

            // Act
            var seq = rec.Recombine(Array.Empty<string>());

            // Assert — bookends pass through untouched and are NOT part of the drawn escalation
            Assert.AreSame(intro, seq.IntroSegment);
            Assert.AreSame(lull, seq.PreBossLullSegment);
            CollectionAssert.DoesNotContain(seq.EscalatingSegments, intro);
            CollectionAssert.DoesNotContain(seq.EscalatingSegments, lull);
        }

        [Test]
        public void test_recombinator_rejects_null_and_empty_args()
        {
            var pool = CanonicalPool();
            Assert.Throws<ArgumentNullException>(() => new SegmentRecombinator(null, DifficultyTier.D1, new Random(0)));
            Assert.Throws<ArgumentNullException>(() => new SegmentRecombinator(Stage(3, pool), DifficultyTier.D1, null));
            Assert.Throws<ArgumentException>(() => new SegmentRecombinator(Stage(1, Array.Empty<SegmentDef>()), DifficultyTier.D1, new Random(0)));
        }
    }
}
