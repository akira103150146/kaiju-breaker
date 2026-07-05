namespace KaijuBreaker.Core
{
    // Run/hunt lifecycle events. Published by the run controller (Stage epic) as a hunt
    // resolves; consumed by Economy (settlement rewards), UI (results screen), Meta (autosave).

    /// <summary>
    /// on_hunt_end — a hunt (single stage run against a kaiju) has ended and is settling.
    /// <see cref="IsAllPartsBroken"/> is true only when EVERY breakable part was destroyed
    /// (a full clear), which gates the settlement rewards: Economy awards essence + the
    /// completeness shard bonus only on a full clear (material-economy.md §C.2, §D.1).
    /// Essence NEVER drops from <see cref="PartBroke"/> — it is a hunt-end reward exclusively.
    /// Published by the run controller; Economy/UI/Meta subscribe.
    /// </summary>
    public readonly struct HuntEnded : IGameEvent
    {
        /// <summary>True iff all breakable parts of the kaiju were destroyed this hunt (full clear).</summary>
        public readonly bool IsAllPartsBroken;

        public HuntEnded(bool isAllPartsBroken)
        {
            IsAllPartsBroken = isAllPartsBroken;
        }
    }
}
