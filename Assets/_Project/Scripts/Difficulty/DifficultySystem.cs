using KaijuBreaker.Content;
using KaijuBreaker.Core;

namespace KaijuBreaker.Difficulty
{
    /// <summary>
    /// The single authority for the run's difficulty tier and its density multipliers
    /// (difficulty-system.md §C.1/§C.3; control-manifest §3 Difficulty). Implements the read-only
    /// <see cref="IDifficultyProvider"/> that Stage and the BulletSim bridge consume — they pull the
    /// current tier's multiplier through the interface and NEVER cache or duplicate a difficulty value.
    ///
    /// Multiplier values live solely on the injected <see cref="DifficultyConfig"/> (ADR-0003); this
    /// system holds only the selected tier + a run-lock. Constructor-injected by App (no singleton).
    ///
    /// Design rule (§C.2): tiers scale bullet DENSITY and enemy count ONLY — part TTB, weapon output,
    /// materials, and content availability are difficulty-invariant. This system exposes no knob that
    /// could touch those; the invariance is proven by the Story 003/004 architecture-scan tests.
    /// </summary>
    public sealed class DifficultySystem : IDifficultyProvider
    {
        private readonly DifficultyConfig _config;
        private bool _locked;

        /// <summary>
        /// Create the system over its config. The starting tier defaults to the config's
        /// first-launch default (§G.2); call <see cref="InitializeStartTier"/> at run-start to apply a
        /// remembered tier from the save. Not locked until <see cref="LockForRun"/>.
        /// </summary>
        public DifficultySystem(DifficultyConfig config)
        {
            _config = config != null ? config : throw new System.ArgumentNullException(nameof(config));
            CurrentTier = _config.DefaultDifficultyOnFirstLaunch;
        }

        /// <summary>The tier in effect for the current run (drives <see cref="BulletDensityMult"/> etc.).</summary>
        public DifficultyTier CurrentTier { get; private set; }

        /// <summary>True once <see cref="LockForRun"/> has frozen the tier for the in-progress run (§E.1).</summary>
        public bool IsLockedForRun => _locked;

        /// <summary>Bullet-density multiplier for the CURRENT tier (§G.1). Read by the BulletSim bridge.</summary>
        public float BulletDensityMult => _config.GetBulletDensityMult(CurrentTier);

        /// <summary>Enemy-count multiplier for the CURRENT tier (§G.1). Read by Stage wave spawning.</summary>
        public float EnemyCountMult => _config.GetEnemyCountMult(CurrentTier);

        /// <summary>
        /// Per-tier bullet-density lookup (any tier), delegating to the single-source config. Sibling of
        /// the current-tier <see cref="BulletDensityMult"/> property; used by difficulty-select UI previews.
        /// </summary>
        public float GetBulletDensityMult(DifficultyTier tier) => _config.GetBulletDensityMult(tier);

        /// <summary>Per-tier enemy-count lookup (any tier), delegating to the single-source config.</summary>
        public float GetEnemyCountMult(DifficultyTier tier) => _config.GetEnemyCountMult(tier);

        /// <summary>The per-scene enemy cap (single source; §G.2). Stage uses it to bound wave counts.</summary>
        public int EnemyCapPerScene => _config.EnemyCapPerScene;

        /// <summary>
        /// Select the run's difficulty. No-op once locked (§E.1 — mid-run change is forbidden), so a late
        /// call cannot alter an in-progress run. Returns true if the tier was applied.
        /// </summary>
        public bool SetTier(DifficultyTier tier)
        {
            if (_locked) return false;
            CurrentTier = tier;
            return true;
        }

        /// <summary>
        /// Resolve and apply the run-start tier from the (optional) remembered save value, per the
        /// config flags: if <see cref="DifficultyConfig.RememberLastDifficulty"/> and a value is present,
        /// use it; otherwise fall back to the first-launch default (§G.2). RunController passes the saved
        /// tier here — this system never reads the save directly (control-manifest DI; save I/O is Meta's).
        /// No-op once locked.
        /// </summary>
        public bool InitializeStartTier(DifficultyTier? lastSelected)
        {
            if (_locked) return false;
            CurrentTier = ResolveStartTier(_config, lastSelected);
            return true;
        }

        /// <summary>
        /// Pure resolution of the run-start tier from config flags + an optional remembered value.
        /// Exposed static for direct testability (no instance/lock state involved).
        /// </summary>
        public static DifficultyTier ResolveStartTier(DifficultyConfig config, DifficultyTier? lastSelected)
        {
            if (config.RememberLastDifficulty && lastSelected.HasValue)
                return lastSelected.Value;
            return config.DefaultDifficultyOnFirstLaunch;
        }

        /// <summary>Freeze the tier for the in-progress run — subsequent <see cref="SetTier"/> calls no-op (§C.1/§E.1).</summary>
        public void LockForRun() => _locked = true;

        /// <summary>Unfreeze once the run ends, so the next run's selection can change the tier again.</summary>
        public void UnlockForRunEnd() => _locked = false;
    }
}
