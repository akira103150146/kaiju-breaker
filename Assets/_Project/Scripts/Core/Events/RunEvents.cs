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

    /// <summary>
    /// The run flow advanced from one <see cref="RunState"/> to another (stage-system.md §Run 狀態機,
    /// TR-stage-007). Emitted by the run controller on EVERY legal transition
    /// (LOADOUT→STAGE→BOSS→RESULTS→LOADOUT). UI drives scene/HUD swaps from this; other systems may
    /// gate behaviour on the current phase without referencing Stage directly.
    /// </summary>
    public readonly struct RunStateChanged : IGameEvent
    {
        public readonly RunState From;
        public readonly RunState To;

        public RunStateChanged(RunState from, RunState to)
        {
            From = from;
            To = to;
        }
    }

    /// <summary>
    /// on_loadout_confirmed — the player locked in their loadout (1 primary + 1 secondary + difficulty +
    /// boss target) and pressed start. Drives the run controller's LOADOUT→STAGE transition and the first
    /// autosave of the run (stage-system.md TR-stage-007). Published by the loadout screen / meta hub.
    /// </summary>
    public readonly struct LoadoutConfirmed : IGameEvent
    {
    }

    /// <summary>
    /// on_weapon_pod_grabbed — the player collected a cycling weapon pod during the STAGE phase
    /// (stage-system.md §L.2, Stage Story 005). Carries the weapon the pod granted. The run controller
    /// subscribes only to enqueue an autosave at the pickup point; Weapons/UI consume the weapon id.
    /// Story 005 owns publishing this; declared here in Core so subscribers exist ahead of the pod system.
    /// </summary>
    public readonly struct WeaponPodGrabbed : IGameEvent
    {
        /// <summary>The weapon granted by the collected pod.</summary>
        public readonly WeaponId Weapon;

        public WeaponPodGrabbed(WeaponId weapon)
        {
            Weapon = weapon;
        }
    }
}
