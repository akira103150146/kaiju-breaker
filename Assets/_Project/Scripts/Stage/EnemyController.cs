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

        [Tooltip("Absolute X beyond which the enemy has left the play field sideways and is despawned — stops " +
                 "unreachable off-map mobs from firing at the player (who is clamped to ±4).")]
        [SerializeField] private float _despawnBeyondX = 5.2f;

        [Tooltip("Extra body-size multiplier applied to an elite variant so it reads as the bigger threat.")]
        [SerializeField] private float _eliteSizeMult = 1.35f;

        [Tooltip("Collider full-size as a fraction of the visual body size (slightly < 1 = fair hit box).")]
        [SerializeField] private float _hitboxBodyFraction = 0.82f;

        private BoxCollider2D _box;
        private Vector3 _bodyScale = Vector3.one;   // the resting scale set at Init; the hit-pop animates around it
        private float _popRemaining;                // hit squash-and-recover timer (seconds)
        private const float PopDuration = 0.13f;
        private int _maxHp;
        private float _flashRemaining;
        private Color _baseColor = Color.white;
        private bool _dead;
        private EnemyMovementState _moveState;
        private EnemyBulletPool _bulletPool;
        private Transform _playerTarget;
        private System.Action<Vector3, bool> _onKilled;
        private float _bulletDensityMult = 1f; // difficulty bullet-density scale (D1 = 1.0)
        private float _fireCooldown;
        private float _telegraphRemaining;
        private bool _telegraphing;
        private float _spinPhase; // Spiral emitters: accumulated rotation so the ring sweeps over time
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
            _box = GetComponent<BoxCollider2D>();
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

            // Visual identity (placeholder): a distinct silhouette + tint + size per type so mobs are told apart
            // at a glance even though they share one prefab. Elites take the aura colour and grow bigger.
            if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
            if (_sprite != null && def != null)
            {
                _sprite.sprite = EnemyShapeSprites.For(def.BodyShape);
                _sprite.color = isElite ? def.EliteAuraColor : def.BodyColor;
                _baseColor = _sprite.color;
            }
            else if (_sprite != null)
            {
                _sprite.color = Color.white;
                _baseColor = _sprite.color;
            }

            // Body size: the shape sprite is 1 world unit at scale 1, so scale == desired world diameter. Elites
            // scale up. The trigger box is normalised to a fair fraction of the body (kill zone < visible body).
            float body = def != null ? def.BodySize : 0.8f;
            if (isElite) body *= _eliteSizeMult;
            _bodyScale = new Vector3(body, body, 1f);
            transform.localScale = _bodyScale;
            _popRemaining = 0f;
            if (_box != null)
            {
                // localScale already multiplies the collider, so the local size is just the fraction (× scale = world).
                _box.size = new Vector2(_hitboxBodyFraction, _hitboxBodyFraction);
                _box.offset = Vector2.zero;
            }
            _flashRemaining = 0f;
        }

        /// <summary>
        /// Inject the run-scoped combat context (bullet pool + player target) so this enemy can fire its
        /// <see cref="EmitterPatternSO"/>. Called by <see cref="WaveSpawner"/> right after <see cref="Init"/>.
        /// </summary>
        public void SetCombatContext(EnemyBulletPool pool, Transform playerTarget,
                                     System.Action<Vector3, bool> onKilled = null, float bulletDensityMult = 1f)
        {
            _bulletPool = pool;
            _playerTarget = playerTarget;
            _onKilled = onKilled;
            _bulletDensityMult = bulletDensityMult > 0f ? bulletDensityMult : 1f;
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
            _popRemaining = PopDuration; // squash-and-recover so the player sees which enemy got hit
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
            if (Emitter.PatternType == EmitterPatternType.Spiral) _spinPhase += Emitter.SpinRateDegPerSec * dt;

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
            // Difficulty scales mob bullet DENSITY (design pillar 難度是門: density-only, D1 = ×1.0). This was the
            // missing wire — mobs previously fired the same count at every tier, so 道中 難度 only changed enemy
            // COUNT. Speed/shape stay tier-invariant. Elite multiplier stacks on top (§E.3).
            int count = KaijuBreaker.Difficulty.DifficultyScaling.ScaledBulletCount(Emitter.BulletCountBase, _bulletDensityMult);
            if (IsElite) count = Mathf.CeilToInt(count * Emitter.EliteDensityMult); // elites fire denser (§E.3)
            float speed = Emitter.BulletSpeedPxPerSec * EnemyMovement.PxToWorld;
            Vector2 aim = _playerTarget != null
                ? ((Vector2)_playerTarget.position - (Vector2)transform.position)
                : Vector2.down;
            var vels = EnemyEmission.Velocities(type, count, Emitter.SpreadAngleDeg, speed, aim, _spinPhase);
            float dmg = Def != null ? Def.ContactDamage : 10f;
            Color tint = BulletTint(type);
            for (int i = 0; i < vels.Length; i++)
                _bulletPool.Spawn(transform.position, vels[i], dmg, Emitter.BulletLifetimeSeconds, tint);
        }

        // Warm threat-palette tint per firing shape so different attacks read differently (enemy bullets stay
        // warm by rule — player shots are cold).
        private static Color BulletTint(EmitterPatternType type)
        {
            switch (type)
            {
                case EmitterPatternType.Aimed:     return new Color(1f, 0.33f, 0.28f); // red — aimed at you
                case EmitterPatternType.Linear:    return new Color(1f, 0.74f, 0.24f); // amber — wall
                case EmitterPatternType.Radial:    return new Color(1f, 0.52f, 0.14f); // orange — ring
                case EmitterPatternType.Spiral:    return new Color(1f, 0.36f, 0.72f); // magenta — spiral
                case EmitterPatternType.RingBurst: return new Color(1f, 0.88f, 0.42f); // hot yellow — death burst
                default:                           return new Color(1f, 0.5f, 0.3f);
            }
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

            // Escaped off the bottom OR sideways out of the play field — deactivate (no score/drop) so it can't
            // keep firing from a spot the player can't reach, and the wave can still clear.
            Vector3 pos = transform.position;
            if (pos.y < _despawnBelowY || Mathf.Abs(pos.x) > _despawnBeyondX) { gameObject.SetActive(false); return; }

            if (!_dead) TickEmitter(dt);
            UpdateSpriteState(dt);
            TickHitPop(dt);
        }

        // Quick squash-and-recover on hit: scale dips then returns to the resting body scale. Pure visual juice.
        private void TickHitPop(float dt)
        {
            if (_popRemaining <= 0f) return;
            _popRemaining -= dt;
            if (_popRemaining <= 0f) { transform.localScale = _bodyScale; return; }
            float u = 1f - _popRemaining / PopDuration;              // 0 → 1 over the pop
            float factor = 1f - 0.24f * Mathf.Sin(u * Mathf.PI);     // dip to ~0.76 then back to 1
            transform.localScale = _bodyScale * factor;
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
