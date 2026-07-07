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

        /// <summary>
        /// Runtime id of the ALIVE, currently-SOFTENED part with the highest heat, or −1 if none is
        /// softened. Ties break to the lowest part id. Sibling of <see cref="GetHottestAlivePartId"/>
        /// used by M2 Tier-3 "飽和點名" to prioritise already-softened parts (weapon-tiering-and-equal-power.md).
        /// </summary>
        int GetHottestSoftenedPartId();
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
        /// <summary>
        /// Bank <paramref name="amount"/> units of a material into the player's persistent inventory
        /// (material-economy.md §F.5). Called by Economy after it computes a part-break yield; Meta owns
        /// storage and persistence. <paramref name="amount"/> is expected to be positive — a break always
        /// yields at least one shard and one core (no zero-drop, §D.1). Meta must accumulate, not overwrite.
        /// </summary>
        void CreditMaterials(MaterialId id, int amount);

        /// <summary>
        /// The player's current persisted count of a material (0 if never credited). Read by the upgrade
        /// transaction (Economy Story 004) and UI; NOT read by the per-break yield path (Economy is a
        /// push-only producer there). Backed by in-memory state, not a synchronous file read.
        /// </summary>
        int GetMaterialCount(MaterialId id);

        /// <summary>
        /// Deduct <paramref name="amount"/> of a material as part of an upgrade purchase (material-economy.md
        /// §C.4). The caller (Economy) MUST have already verified affordability — Meta subtracts unconditionally
        /// and enqueues an autosave. <paramref name="amount"/> is expected non-negative.
        /// </summary>
        void SpendMaterials(MaterialId id, int amount);

        /// <summary>
        /// Persist a weapon's new permanent upgrade tier (0..3). The matching <see cref="IWeaponTierQuery.GetTier"/>
        /// MUST reflect the new value in the SAME frame (in-memory update; background save is separate) so
        /// Weapons applies the correct tuning immediately (material-economy.md §C.3; control-manifest §4.3).
        /// </summary>
        void SetWeaponTier(WeaponId weapon, int tier);

        /// <summary>Request an asynchronous autosave (e.g. after banking a part-break reward).</summary>
        void EnqueueAutosave();

        /// <summary>
        /// Read a persistent one-time boolean flag (false if never set). Used for one-shot UI/onboarding
        /// state such as "first_pod_pickup_shown" (stage-system.md §H.2). Backed by the JSON save (ADR-0004),
        /// NOT PlayerPrefs.
        /// </summary>
        bool GetFlag(string key);

        /// <summary>Set a persistent one-time boolean flag and enqueue an autosave. See <see cref="GetFlag"/>.</summary>
        void SetFlag(string key, bool value);

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

    /// <summary>
    /// Read-only lookup from a runtime kaiju id to its material <see cref="KaijuTheme"/>
    /// (material-economy.md §C.1 層級二). Injected into Economy so it can map an incoming
    /// <c>PartBroke.KaijuId</c> → theme → core WITHOUT referencing the KaijuParts assembly.
    /// The runtime int kaiju id is assigned by the composition root when a kaiju is initialised;
    /// the backing theme lives on <c>KaijuDef</c>. Many kaiju may share a theme.
    /// </summary>
    public interface IKaijuThemeQuery
    {
        /// <summary>
        /// The material theme of the kaiju with the given runtime id. MUST throw
        /// <see cref="System.ArgumentException"/> for an unregistered id — a wrong core must never be
        /// silently awarded (material-economy.md §H.2/§H.4 "fail loud, fail fast").
        /// </summary>
        KaijuTheme GetTheme(int kaijuId);
    }
}
