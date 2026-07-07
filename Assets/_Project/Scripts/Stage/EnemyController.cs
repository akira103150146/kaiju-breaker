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
    /// <para>Movement here is a placeholder downward drift so instances are visible/moving; executing the full
    /// <see cref="MovementPatternSO"/> is a movement-system concern, and bullet emission from
    /// <see cref="EmitterPatternSO"/> is blocked by ADR-0001 — both are follow-ups. This component only owns
    /// the per-instance data + a minimal placeholder motion.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyController : MonoBehaviour
    {
        [Tooltip("Placeholder descent speed (world units/sec) until the movement system drives MovementPattern.")]
        [SerializeField] private float _placeholderDriftSpeed = 1.5f;

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
            Died?.Invoke(this);
            gameObject.SetActive(false);
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
            transform.position += Vector3.down * (_placeholderDriftSpeed * Time.deltaTime);

            // Escaped off the bottom — deactivate (no score/drop) so the wave can clear.
            if (transform.position.y < _despawnBelowY) { gameObject.SetActive(false); return; }

            if (_flashRemaining > 0f && _sprite != null)
            {
                _flashRemaining -= Time.deltaTime;
                _sprite.color = _flashRemaining > 0f ? Color.white : _baseColor;
            }
        }
    }
}
