namespace KaijuBreaker.Content
{
    /// <summary>
    /// Formation a wave's enemies spawn in (stage-system.md §D.2 spawn layout). A pure layout descriptor —
    /// the concrete world coordinates are resolved by <c>KaijuBreaker.Stage.SpawnLayoutHelper</c>. Data-only
    /// enum so <see cref="WaveTimingConfig"/> and segment data can name a formation without embedding math.
    /// </summary>
    public enum SpawnLayout
    {
        /// <summary>All enemies at the horizontal centre (they fan out via their own movement pattern).</summary>
        Center = 0,

        /// <summary>Evenly spread across the playfield width.</summary>
        HorizontalSpread = 1,

        /// <summary>A single vertical column at centre, staggered downward.</summary>
        Column = 2
    }
}
