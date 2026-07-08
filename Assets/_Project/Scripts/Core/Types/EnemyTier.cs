namespace KaijuBreaker.Core
{
    /// <summary>
    /// Coarse combat-weight tier for an enemy, orthogonal to the HP tier (T1/T2 band) and to
    /// the elite stat-multiplier flag. Lets systems (wave density, difficulty scaling, roster
    /// filtering) reason about "how big a deal is this enemy" without re-deriving it from
    /// HP/isElite each time. See per-part-firing-schema.md §1.5.
    /// </summary>
    public enum EnemyTier
    {
        /// <summary>Ordinary trash-mob — the roster default. Density-scaled, no special handling.</summary>
        Trash = 0,

        /// <summary>Elite variant of a trash enemy — must agree with the owning EnemyDef's IsElite flag.</summary>
        Elite = 1,

        /// <summary>Mid-tier mini-boss encounter (breaks a MidCore part, ends the encounter but not the run).</summary>
        Mid = 2,

        /// <summary>Full boss kaiju — drives a KaijuDef encounter, not a trash wave.</summary>
        Boss = 3
    }
}
