using System.Collections.Generic;
using KaijuBreaker.Content;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// The immutable per-run stage layout produced by <see cref="SegmentRecombinator"/>
    /// (stage-system.md §D.1): a fixed intro, N escalating segments ordered lightest-first,
    /// a fixed pre-boss lull, then the boss arena reference. Consumed by the Stage flow
    /// scheduler (Story 002/006) and the onboarding override (Story 007).
    /// </summary>
    public sealed class SegmentSequence
    {
        /// <summary>Fixed introduction segment played first (from <see cref="StageDef.IntroSegment"/>). May be null.</summary>
        public SegmentDef IntroSegment { get; }

        /// <summary>
        /// The N drawn segments, ordered by <see cref="SegmentDef.DifficultyWeight"/> ascending
        /// (lightest first). Never null; may be shorter than the draw count only if the pool is smaller.
        /// </summary>
        public IReadOnlyList<SegmentDef> EscalatingSegments { get; }

        /// <summary>Fixed pre-boss lull segment (from <see cref="StageDef.PreBossLullSegment"/>). May be null.</summary>
        public SegmentDef PreBossLullSegment { get; }

        /// <summary>String id of the boss kaiju for this stage (from <see cref="StageDef.BossKaijuId"/>).</summary>
        public string BossKaijuId { get; }

        public SegmentSequence(
            SegmentDef introSegment,
            IReadOnlyList<SegmentDef> escalatingSegments,
            SegmentDef preBossLullSegment,
            string bossKaijuId)
        {
            IntroSegment = introSegment;
            EscalatingSegments = escalatingSegments ?? System.Array.Empty<SegmentDef>();
            PreBossLullSegment = preBossLullSegment;
            BossKaijuId = bossKaijuId;
        }
    }
}
