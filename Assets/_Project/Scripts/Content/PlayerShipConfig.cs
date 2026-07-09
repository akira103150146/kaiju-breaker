using UnityEngine;

namespace KaijuBreaker.Content
{
    /// <summary>
    /// Data-driven tuning for the player ship's movement, survivability, and the always-on primary auto-fire
    /// (ADR-0003 — no magic numbers in the ship code). All values are in world units, matching the stage
    /// coordinate space (field width 8: x ∈ [-4, 4]; enemies spawn at y ≈ 6 and drift down; player fires up).
    ///
    /// <para>The primary weapon here is the trash-clearing auto-fire the player always has; the equippable
    /// laser/missile families (WeaponDef) layer their boss heat/break behaviour on top later. Secondary-fire
    /// tuning lives here too so the on-screen mobile button (Phase E) and PC input drive the same knobs.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "KaijuBreaker/Config/PlayerShipConfig", fileName = "PlayerShipConfig")]
    public sealed class PlayerShipConfig : ScriptableObject
    {
        [Header("Movement (world units)")]
        [Tooltip("Keyboard/stick movement speed in world units per second.")]
        [SerializeField] private float _moveSpeed = 9f;

        [Tooltip("How fast the ship eases toward a pointer-drag / joystick target (higher = snappier). Per-second lerp factor.")]
        [SerializeField] private float _pointerFollowLerp = 18f;

        [Header("Playfield clamp (world units)")]
        [SerializeField] private float _minX = -4f;
        [SerializeField] private float _maxX = 4f;
        [SerializeField] private float _minY = -6f;
        [Tooltip("Upper Y clamp during the 道中 stage (keeps the ship in the lower band).")]
        [SerializeField] private float _stageMaxY = 1.5f;
        [Tooltip("Upper Y clamp during the boss fight (lets the player move higher to reach parts).")]
        [SerializeField] private float _bossMaxY = 4f;

        [Header("Survivability")]
        [Tooltip("Maximum hull HP. Reaching 0 ends the run in defeat (win/lose is real, not a soft shield).")]
        [SerializeField] private int _maxHp = 100;

        [Tooltip("Invulnerability window (seconds) after taking a hit — i-frames.")]
        [SerializeField] private float _invulnSeconds = 0.9f;

        [Tooltip("Player hit-detection box, in WORLD units (full width, applied independent of the ship's visual " +
                 "scale). Bullet-hell fairness rule: the kill zone must be far smaller than the ship sprite. " +
                 "0.24 world ≈ a ~0.2 world effective kill radius once the bullet's own radius is added — dodgeable " +
                 "even in dense patterns. Set ≤ 0 to keep whatever the prefab authored.")]
        [SerializeField] private float _hitboxWorldSize = 0.24f;

        [Header("Primary auto-fire (trash-clearing)")]
        [Tooltip("Seconds between primary shots.")]
        [SerializeField] private float _primaryFireInterval = 0.12f;

        [Tooltip("Primary projectile speed (world units/sec, travelling up).")]
        [SerializeField] private float _primaryProjectileSpeed = 16f;

        [Tooltip("Damage a single primary projectile deals to a trash enemy's HP.")]
        [SerializeField] private float _primaryDamage = 12f;

        [Tooltip("Heat delta a primary projectile contributes to a boss part on hit (drives the soften track).")]
        [SerializeField] private float _primaryHeatDelta = 17f;

        [Header("Secondary manual-fire (missile — breaks parts)")]
        [Tooltip("Minimum seconds between secondary shots.")]
        [SerializeField] private float _secondaryCooldown = 0.45f;

        [Tooltip("Secondary projectile speed (world units/sec).")]
        [SerializeField] private float _secondaryProjectileSpeed = 13f;

        [Tooltip("Damage a secondary projectile deals to a trash enemy's HP.")]
        [SerializeField] private float _secondaryTrashDamage = 45f;

        [Tooltip("Break delta (break units) a secondary projectile applies to a boss part (the break track).")]
        [SerializeField] private float _secondaryBreakDamage = 30f;

        [Header("In-run firepower (Raiden-style, resets each run)")]
        [Tooltip("Max primary firepower level reachable by collecting P items.")]
        [SerializeField] private int _maxWeaponPower = 8;

        [Tooltip("Max missile level reachable by collecting M items.")]
        [SerializeField] private int _maxMissilePower = 8;

        [Tooltip("Angle (deg) between adjacent bullets in the primary spread fan.")]
        [SerializeField] private float _primarySpreadPerBulletDeg = 7f;

        [Tooltip("Hard cap on primary bullets per shot (readability).")]
        [SerializeField] private int _maxPrimaryBullets = 9;

        [Tooltip("Hard cap on missiles per secondary shot.")]
        [SerializeField] private int _maxSecondaryMissiles = 5;

        // ── Movement ────────────────────────────────────────────────────────────────
        /// <summary>Keyboard/stick move speed (world units/sec).</summary>
        public float MoveSpeed => _moveSpeed;
        /// <summary>Per-second ease factor toward a pointer-drag / joystick target.</summary>
        public float PointerFollowLerp => _pointerFollowLerp;

        // ── Clamp ───────────────────────────────────────────────────────────────────
        public float MinX => _minX;
        public float MaxX => _maxX;
        public float MinY => _minY;
        /// <summary>Upper Y clamp during the 道中 stage.</summary>
        public float StageMaxY => _stageMaxY;
        /// <summary>Upper Y clamp during the boss fight.</summary>
        public float BossMaxY => _bossMaxY;

        // ── Survivability ─────────────────────────────────────────────────────────────
        /// <summary>Maximum hull HP; 0 = defeat.</summary>
        public int MaxHp => _maxHp;
        /// <summary>I-frame window after a hit, seconds.</summary>
        public float InvulnSeconds => _invulnSeconds;
        /// <summary>Player hit box full width in world units, applied independent of ship scale (≤ 0 = keep prefab).</summary>
        public float HitboxWorldSize => _hitboxWorldSize;

        // ── Primary fire ──────────────────────────────────────────────────────────────
        /// <summary>Seconds between primary auto-fire shots.</summary>
        public float PrimaryFireInterval => _primaryFireInterval;
        /// <summary>Primary projectile speed (world units/sec).</summary>
        public float PrimaryProjectileSpeed => _primaryProjectileSpeed;
        /// <summary>Primary projectile damage vs a trash enemy's HP.</summary>
        public float PrimaryDamage => _primaryDamage;
        /// <summary>Heat delta a primary projectile adds to a boss part.</summary>
        public float PrimaryHeatDelta => _primaryHeatDelta;

        // ── Secondary fire ────────────────────────────────────────────────────────────
        /// <summary>Minimum seconds between secondary (missile) shots.</summary>
        public float SecondaryCooldown => _secondaryCooldown;
        /// <summary>Secondary projectile speed (world units/sec).</summary>
        public float SecondaryProjectileSpeed => _secondaryProjectileSpeed;
        /// <summary>Secondary projectile damage vs a trash enemy's HP.</summary>
        public float SecondaryTrashDamage => _secondaryTrashDamage;
        /// <summary>Break-unit delta a secondary projectile applies to a boss part.</summary>
        public float SecondaryBreakDamage => _secondaryBreakDamage;

        // ── In-run firepower ──────────────────────────────────────────────────────────
        /// <summary>Max primary firepower level (P items).</summary>
        public int MaxWeaponPower => _maxWeaponPower;
        /// <summary>Max missile level (M items).</summary>
        public int MaxMissilePower => _maxMissilePower;
        /// <summary>Degrees between adjacent bullets in the primary spread fan.</summary>
        public float PrimarySpreadPerBulletDeg => _primarySpreadPerBulletDeg;
        /// <summary>Hard cap on primary bullets per shot.</summary>
        public int MaxPrimaryBullets => _maxPrimaryBullets;
        /// <summary>Hard cap on missiles per secondary shot.</summary>
        public int MaxSecondaryMissiles => _maxSecondaryMissiles;

        private void OnValidate()
        {
            if (_moveSpeed <= 0f) Debug.LogError($"[PlayerShipConfig] '{name}': MoveSpeed must be > 0.", this);
            if (_maxHp <= 0) Debug.LogError($"[PlayerShipConfig] '{name}': MaxHp must be > 0.", this);
            if (_primaryFireInterval <= 0f) Debug.LogError($"[PlayerShipConfig] '{name}': PrimaryFireInterval must be > 0.", this);
            if (_maxX < _minX) Debug.LogError($"[PlayerShipConfig] '{name}': MaxX must be >= MinX.", this);
        }
    }
}
