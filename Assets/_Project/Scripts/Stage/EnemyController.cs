using System;
using KaijuBreaker.Content;
using UnityEngine;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// Runtime component on a spawned trash-enemy instance (stage-system.md §D.2). Receives its data from the
    /// <see cref="WaveSpawner"/> via <see cref="Init"/>: the <see cref="EnemyDef"/> plus the movement/emitter
    /// pattern SOs the def carries (the data wiring Story 002 asserts). HP is derived from the def's
    /// <see cref="HpTier"/>; elite instances tint to the def's aura colour.
    ///
    /// <para>Movement executes the enemy's <see cref="MovementPatternSO"/> via <see cref="EnemyMovement"/> (five
    /// distinct entrance/path archetypes); bullet emission executes the <see cref="EmitterPatternSO"/> via
    /// <see cref="EnemyEmitter"/>. This component owns the per-instance data, HP, and the motion/fire drivers.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyController : MonoBehaviour
    {
        [Tooltip("Shared tier→base-HP table (data-driven, ADR-0003). Required for real HP; falls back to a minimal " +
                 "constant if unassigned so the enemy still spawns.")]
        [SerializeField] private EnemyTierBalanceConfig _tierBalance;

        [Tooltip("SpriteRenderer flashed white on hit. Defaults to this object's SpriteRenderer.")]
        [SerializeField] private SpriteRenderer _sprite;

        [Tooltip("How long the white hit-flash lasts (seconds).")]
        [SerializeField] private float _hitFlashSeconds = 0.06f;

        [Tooltip("Y below which the enemy has escaped off the bottom and is despawned (no score).")]
        [SerializeField] private float _despawnBelowY = -8f;

        private int _maxHp;
        private float _flashRemaining;
        private Color _baseColor = Color.white;
        private bool _dead;
        private EnemyMovementState _moveState;
        private EnemyBulletPool _bulletPool;
        private Transform _playerTarget;
        private System.Action<Vector3, bool> _onKilled;
        private float _fireCooldown;
        private float _telegraphRemaining;
        private bool _telegraphing;
        private const float TelegraphSeconds = 0.3f; // telegraph floor (bullet-system.md readability)

        /// <summary>Raised once when this enemy is destroyed by damage (not when it flies off-screen).</summary>
        public event Action<EnemyController> Died;

        /// <summary>Maximum HP resolved at spawn (from tier + elite multiplier).</summary>
        public int MaxHp => _maxHp;

        /// <summary>True once destroyed by damage.</summary>
        public bool IsDead => _dead;

        /// <summary>The data definition this instance was spawned from (null only if never initialised).</summary>
        public EnemyDef Def { get; private set; }

        /// <summary>Movement pattern SO carried by <see cref="Def"/> (may be null if the def has none assigned).</summary>
        public MovementPatternSO Movement { get; private set; }

        /// <summary>Emitter (bullet) pattern SO carried by <see cref="Def"/>. Wired now; fired once ADR-0001 lands.</summary>
        public EmitterPatternSO Emitter { get; private set; }

        /// <summary>Current hit points, derived from the def's HP tier at spawn.</summary>
        public int Hp { get; private set; }

        /// <summary>Whether this instance is the elite of its wave.</summary>
        public bool IsElite { get; private set; }

        private void Awake()
        {
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.useFullKinematicContacts = true; // kinematic↔kinematic trigger events vs player + player projectiles
        }

        /// <summary>Inject the def + wire its pattern SOs. Called by <see cref="WaveSpawner"/> right after Instantiate.</summary>
        public void Init(EnemyDef def, bool isElite)
        {
            Def = def;
            Movement = def != null ? def.MovementPattern : null;
            Emitter = def != null ? def.EmitterPattern : null;
            IsElite = isElite;
            _dead = false;
            _moveState = default;

            // Elite instances scale HP by the def's elite_hp_mult (stage-system.md §E.3; data-driven).
            int baseHp = HpForTier(def);
            _maxHp = isElite && def != null ? Mathf.CeilToInt(baseHp * def.EliteHpMult) : baseHp;
            Hp = _maxHp;

            if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
            if (_sprite != null)
            {
                _sprite.color = isElite && def != null ? def.EliteAuraColor : Color.white;
                _baseColor = _sprite.color;
            }
            _flashRemaining = 0f;
        }

        /// <summary>
        /// Inject the run-scoped combat context (bullet pool + player target) so this enemy can fire its
        /// <see cref="EmitterPatternSO"/>. Called by <see cref="WaveSpawner"/> right after <see cref="Init"/>.
        /// </summary>
        public void SetCombatContext(EnemyBulletPool pool, Transform playerTarget,
                                     System.Action<Vector3, bool> onKilled = null)
        {
            _bulletPool = pool;
            _playerTarget = playerTarget;
            _onKilled = onKilled;
            _fireCooldown = Emitter != null ? Emitter.FireIntervalSeconds : 0f;
            _telegraphing = false;
            _telegraphRemaining = 0f;
        }

        /// <summary>Apply damage. Destroys the enemy (raising <see cref="Died"/>) when HP reaches 0.</summary>
        public void TakeDamage(float amount)
        {
            if (_dead || amount <= 0f) return;
            Hp = Mathf.Max(0, Hp - Mathf.CeilToInt(amount));
            _flashRemaining = _hitFlashSeconds;
            if (Hp <= 0) Die();
        }

        private void Die()
        {
            if (_dead) return;
            _dead = true;
            // RingBurst emitters fire their omnidirectional volley at the moment of death (bullet-system.md §4.2).
            if (Emitter != null && _bulletPool != null && Emitter.PatternType == EmitterPatternType.RingBurst)
                FireVolley(EmitterPatternType.RingBurst);
            _onKilled?.Invoke(transform.position, IsElite); // scene rolls in-run power-up drops
            Died?.Invoke(this);
            gameObject.SetActive(false);
        }

        // Periodic emitters: run down the interval, then telegraph, then fire (RingBurst is death-only, skipped here).
        private void TickEmitter(float dt)
        {
            if (Emitter == null || _bulletPool == null) return;
            if (Emitter.PatternType == EmitterPatternType.RingBurst) return;

            if (_telegraphing)
            {
                _telegraphRemaining -= dt;
                if (_telegraphRemaining <= 0f)
                {
                    _telegraphing = false;
                    FireVolley(Emitter.PatternType);
                    _fireCooldown = Emitter.FireIntervalSeconds;
                }
                return;
            }
            _fireCooldown -= dt;
            if (_fireCooldown <= 0f) { _telegraphing = true; _telegraphRemaining = TelegraphSeconds; }
        }

        private void FireVolley(EmitterPatternType type)
        {
            if (Emitter == null || _bulletPool == null) return;
            int count = Emitter.BulletCountBase;
            if (IsElite) count = Mathf.CeilToInt(count * Emitter.EliteDensityMult); // elites fire denser (§E.3)
            float speed = Emitter.BulletSpeedPxPerSec * EnemyMovement.PxToWorld;
            Vector2 aim = _playerTarget != null
                ? ((Vector2)_playerTarget.position - (Vector2)transform.position)
                : Vector2.down;
            var vels = EnemyEmission.Velocities(type, count, Emitter.SpreadAngleDeg, speed, aim);
            float dmg = Def != null ? Def.ContactDamage : 10f;
            for (int i = 0; i < vels.Length; i++)
                _bulletPool.Spawn(transform.position, vels[i], dmg, Emitter.BulletLifetimeSeconds);
        }

        private int HpForTier(EnemyDef def)
        {
            if (def == null) return 1;
            if (_tierBalance != null) return _tierBalance.BaseHpFor(def.HpTier);
            // Fallback (no config assigned): frozen tier baselines so the enemy is still killable.
            return def.HpTier == HpTier.T2 ? 70 : 30;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            transform.position = EnemyMovement.Advance(transform.position, Movement, ref _moveState, dt);

            // Escaped off the bottom — deactivate (no score/drop) so the wave can clear.
            if (transform.position.y < _despawnBelowY) { gameObject.SetActive(false); return; }

            if (!_dead) TickEmitter(dt);
            UpdateSpriteState(dt);
        }

        // White on a hit, bright warm while telegraphing an imminent volley, base colour otherwise.
        private void UpdateSpriteState(float dt)
        {
            if (_sprite == null) return;
            if (_flashRemaining > 0f)
            {
                _flashRemaining -= dt;
                _sprite.color = _flashRemaining > 0f ? Color.white : _baseColor;
            }
            else if (_telegraphing) _sprite.color = new Color(1f, 0.92f, 0.4f); // warm telegraph
            else _sprite.color = _baseColor;
        }
    }
}
