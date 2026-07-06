using System;
using System.Collections.Generic;
using System.Linq;
using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// Builds a per-run <see cref="SegmentSequence"/> by randomly recombining a stage's hand-crafted
    /// segment pool (stage-system.md §D.1; TR-stage-001; ADR-0005/0003). Pure C# — no Unity API — with an
    /// injected <see cref="System.Random"/> so every draw is deterministic under a fixed seed and fully
    /// EditMode-testable. References only Core + Content (never another Feature system) and reads every knob
    /// from <see cref="StageDef"/> / <see cref="SegmentDef"/> (no hardcoded tuning).
    ///
    /// <para><b>Algorithm (§D.1, six steps):</b>
    /// (1) if pool &gt; N, drop candidates whose id is in the caller's no-repeat window;
    /// (2) drop candidates gated above <c>currentTier</c>, restoring the FULL pool if that leaves &lt; N;
    /// (3) Fisher-Yates shuffle with the injected rng; (4) take the first N; (5) order those N by
    /// <see cref="SegmentDef.DifficultyWeight"/> ascending; (6) assemble intro + escalating + lull + boss.</para>
    ///
    /// <para><b>Reconciliation (surfaced for review):</b> the ctor takes a <see cref="DifficultyTier"/> enum
    /// (type-safe, matches <see cref="IDifficultyProvider.CurrentTier"/>) rather than the story's bare
    /// <c>int currentDifficultyTier</c> — the gate compares enum ordering directly, avoiding the story's
    /// 1-based-vs-0-based tier ambiguity. The full-pool relaxation in step 2 follows the story's literal
    /// "還原至全池" (guarantee a full draw beats honouring the difficulty gate / no-repeat window).</para>
    /// </summary>
    public sealed class SegmentRecombinator
    {
        private readonly StageDef _stageDef;
        private readonly DifficultyTier _currentTier;
        private readonly Random _rng;

        /// <summary>
        /// Construct a recombinator for one stage at one difficulty tier.
        /// </summary>
        /// <param name="stageDef">The stage config (segment pool, draw count, bookends). Must not be null and must have a non-empty pool.</param>
        /// <param name="currentTier">The player's current difficulty tier — segments gated above it are filtered out.</param>
        /// <param name="rng">Injected RNG (use <c>new System.Random(seed)</c> for deterministic tests). Must not be null.</param>
        public SegmentRecombinator(StageDef stageDef, DifficultyTier currentTier, Random rng)
        {
            _stageDef = stageDef ?? throw new ArgumentNullException(nameof(stageDef));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            if (_stageDef.SegmentPool == null || _stageDef.SegmentPool.Length == 0)
                throw new ArgumentException("StageDef.SegmentPool must not be empty.", nameof(stageDef));
            if (_stageDef.SegmentDrawCount < 1)
                throw new ArgumentException("StageDef.SegmentDrawCount must be >= 1.", nameof(stageDef));
            _currentTier = currentTier;
        }

        /// <summary>
        /// Draw and order this run's escalating segments and assemble the full <see cref="SegmentSequence"/>.
        /// </summary>
        /// <param name="lastRunPlayedSegmentIds">
        /// Segment ids to exclude for cross-run variety (the no-repeat window). Applied only when the pool
        /// is larger than the draw count (otherwise skipped so the draw can still fill — §D.1 step 1, AC-3).
        /// Null is treated as empty.
        /// </param>
        /// <returns>The immutable per-run sequence: intro → N escalating (lightest-first) → lull → boss.</returns>
        public SegmentSequence Recombine(IReadOnlyList<string> lastRunPlayedSegmentIds)
        {
            SegmentDef[] pool = _stageDef.SegmentPool;
            int n = _stageDef.SegmentDrawCount;

            // Step 1 — no-repeat window (skipped when pool <= N so we can always fill the draw).
            var candidates = new List<SegmentDef>(pool);
            if (pool.Length > n && lastRunPlayedSegmentIds != null && lastRunPlayedSegmentIds.Count > 0)
            {
                var excluded = new HashSet<string>(lastRunPlayedSegmentIds);
                candidates.RemoveAll(s => excluded.Contains(s.SegmentId));
            }

            // Step 2 — difficulty gate; relax to the full pool if it would starve the draw.
            var gated = candidates.Where(s => (int)s.MinDifficultyTier <= (int)_currentTier).ToList();
            candidates = gated.Count >= n ? gated : new List<SegmentDef>(pool);

            // Step 3 — Fisher-Yates shuffle with the injected rng.
            Shuffle(candidates);

            // Step 4 — take the first N (or all, if the pool is genuinely smaller).
            int take = Math.Min(n, candidates.Count);
            var drawn = candidates.GetRange(0, take);

            // Step 5 — order lightest-first by difficulty weight (OrderBy is a stable sort).
            var escalating = drawn.OrderBy(s => s.DifficultyWeight).ToList();

            // Step 6 — assemble.
            return new SegmentSequence(
                _stageDef.IntroSegment, escalating, _stageDef.PreBossLullSegment, _stageDef.BossKaijuId);
        }

        /// <summary>In-place Fisher-Yates shuffle using the injected <see cref="System.Random"/>.</summary>
        private void Shuffle(IList<SegmentDef> list)
        {
            for (int i = list.Count - 1; i >= 1; i--)
            {
                int j = _rng.Next(0, i + 1); // 0..i inclusive
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
