namespace KaijuBreaker.Core
{
    /// <summary>
    /// Which weapon pool a cycling weapon-pod belongs to (stage-system.md §F). A pod grants a weapon from
    /// either the laser (Primary) or missile (Secondary) pool; <see cref="Random"/> = pick either at spawn
    /// when both pools have already been guaranteed this stage.
    /// </summary>
    public enum PodType
    {
        /// <summary>Laser-family (primary) weapon pod.</summary>
        Primary = 0,

        /// <summary>Missile-family (secondary) weapon pod.</summary>
        Secondary = 1,

        /// <summary>Either pool, chosen at spawn (both already guaranteed).</summary>
        Random = 2
    }

    /// <summary>
    /// A segment's requested pod pool for its elite drop (stage-system.md §F.3). <see cref="Auto"/> lets the
    /// <c>PodDropTracker</c> fill whichever pool has not yet dropped this stage (Primary first).
    /// </summary>
    public enum PodPoolPreference
    {
        /// <summary>Always request a Primary (laser) pod.</summary>
        Primary = 0,

        /// <summary>Always request a Secondary (missile) pod.</summary>
        Secondary = 1,

        /// <summary>Fill the not-yet-dropped pool (Primary first), else Random.</summary>
        Auto = 2
    }
}
