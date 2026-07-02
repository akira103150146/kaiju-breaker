using UnityEngine;

namespace KaijuBreaker.Core
{
    // Read-only query interfaces (ADR-0002 §2). Non-event cross-system reads go
    // through these, injected by App (composition root) — never via events, direct
    // assembly references (ADR-0005), or singletons. Systems take fakes in tests.

    /// <summary>
    /// Read-only view of kaiju part state. Implemented by KaijuParts; injected into
    /// Weapons (M1 highest-heat targeting, tracking), UI (heat/break bars), GameFeel.
    /// </summary>
    public interface IPartStateQuery
    {
        /// <summary>Heat (soften) state of the part.</summary>
        HeatState GetHeatState(int partId);

        /// <summary>Armor-gate state (ARMORED parts only; others report <see cref="ArmorState.Intact"/>).</summary>
        ArmorState GetArmorState(int partId);

        /// <summary>Current heat value (HU) — for UI heat bars and M1 highest-heat targeting.</summary>
        float GetCurrentHeat(int partId);

        /// <summary>Heat capacity (H_max, HU) for this part.</summary>
        float GetMaxHeat(int partId);

        /// <summary>World position of the part (for missile tracking and drop spawns).</summary>
        Vector2 GetWorldPosition(int partId);

        /// <summary>False once the part is BROKEN (or does not exist).</summary>
        bool IsPartAlive(int partId);

        /// <summary>
        /// Runtime id of the ALIVE part with the highest current heat (HU), or −1 if none is alive.
        /// Ties break to the lowest part id. Used by M1 Tier-3 (third missile auto-locks the hottest
        /// part — weapon-system.md G.3). O(n) scan; called at most once per M1 T3 shot.
        /// </summary>
        int GetHottestAlivePartId();
    }

    /// <summary>
    /// Read-only difficulty multipliers. Implemented by Difficulty; injected into
    /// Stage and the BulletSim bridge. Scales density ONLY (pillar 難度是門).
    /// </summary>
    public interface IDifficultyProvider
    {
        DifficultyTier CurrentTier { get; }
        float BulletDensityMult { get; }
        float EnemyCountMult { get; }
    }

    /// <summary>
    /// Save/persistence abstraction so systems can bank progress without referencing
    /// Meta directly. Implemented by Meta; injected into Economy, Stage, UI.
    /// </summary>
    public interface ISaveService
    {
        /// <summary>Request an asynchronous autosave (e.g. after banking a part-break reward).</summary>
        void EnqueueAutosave();

        /// <summary>Synchronously flush pending saves to disk (called on app suspend/quit).</summary>
        void FlushSync();

        /// <summary>
        /// The player's persisted starting loadout (one primary + one secondary weapon), or null on a
        /// fresh save with no stored loadout — the caller then falls back to a data-driven default.
        /// Injected into Weapons' LoadoutController at run start (weapon-system.md — loadout system).
        /// </summary>
        (WeaponId Primary, WeaponId Secondary)? GetInitialLoadout();
    }

    /// <summary>
    /// Read-only weapon upgrade tier lookup (0..3). Provided by Meta/Economy;
    /// injected into Weapons so it applies the correct tier tuning at fire time.
    /// </summary>
    public interface IWeaponTierQuery
    {
        /// <summary>Current permanent upgrade tier (0 = base … 3 = unique mechanic) for a weapon.</summary>
        int GetTier(WeaponId weapon);
    }
}
