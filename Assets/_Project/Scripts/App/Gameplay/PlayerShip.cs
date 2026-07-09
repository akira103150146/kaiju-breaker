using System;
using KaijuBreaker.Content;
using KaijuBreaker.Stage;
using UnityEngine;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>
    /// The player ship: movement (via an <see cref="IPlayerInput"/> provider so PC and mobile share one code
    /// path), hull HP with i-frames, and incoming-damage resolution from enemy contact and enemy bullets. All
    /// tuning comes from <see cref="PlayerShipConfig"/> (ADR-0003). Reaching 0 HP raises <see cref="Died"/> —
    /// a real lose condition, not a soft shield. This is scene-presentation glue (ADR-0005 App layer); it holds
    /// no game-rule state beyond the ship itself and talks to the run flow through C# events.
    ///
    /// <para>Collision uses Unity 2D trigger overlap (kinematic bodies): the ship carries a small trigger and a
    /// kinematic Rigidbody2D so <c>OnTriggerEnter2D</c> fires against enemies and enemy bullets.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerShip : MonoBehaviour
    {
        [Tooltip("Ship tuning (movement, HP, i-frames). Required.")]
        [SerializeField] private PlayerShipConfig _config;

        [Tooltip("Sprite tinted on hurt / blinked during i-frames. Defaults to the ship's own SpriteRenderer.")]
        [SerializeField] private SpriteRenderer _sprite;

        private IPlayerInput _input;
        private int _hp;
        private float _invulnRemaining;
        private bool _bossPhase;
        private Color _baseColor = Color.white;

        /// <summary>Current hull HP.</summary>
        public int Hp => _hp;
        /// <summary>Maximum hull HP (from config).</summary>
        public int MaxHp => _config != null ? _config.MaxHp : 0;
        /// <summary>True while HP &gt; 0.</summary>
        public bool IsAlive => _hp > 0;

        /// <summary>Raised whenever HP changes: (current, max).</summary>
        public event Action<int, int> HpChanged;
        /// <summary>Raised once when HP first reaches 0 (lose condition).</summary>
        public event Action Died;

        private void Awake()
        {
            _input = GetComponent<IPlayerInput>();
            if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
            if (_sprite != null) _baseColor = _sprite.color;
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.useFullKinematicContacts = true; // kinematic↔kinematic trigger events (enemies/bullets are kinematic too)

            // Bullet-hell fairness: the ship's authored trigger box was ~0.62 world — nearly the whole sprite — so
            // the effective kill zone was 5–8× a normal danmaku hitbox and dense patterns became undodgeable. Shrink
            // it to the data-driven world size (independent of the ship's visual scale, so the sprite stays big while
            // the kill dot stays small). ADR-0003: the size lives in PlayerShipConfig, not hardcoded here.
            if (_config != null && _config.HitboxWorldSize > 0f && GetComponent<Collider2D>() is BoxCollider2D box)
            {
                float s = Mathf.Abs(transform.localScale.x) > 1e-4f ? Mathf.Abs(transform.localScale.x) : 1f;
                float local = _config.HitboxWorldSize / s;
                box.size = new Vector2(local, local);
            }

            ResetShip();
        }

        /// <summary>Restore full HP and clear i-frames (call at run start).</summary>
        public void ResetShip()
        {
            _hp = _config != null ? _config.MaxHp : 1;
            _invulnRemaining = 0f;
            HpChanged?.Invoke(_hp, MaxHp);
        }

        /// <summary>Switch the upper-Y clamp between the stage band and the boss band.</summary>
        public void SetBossPhase(bool boss) => _bossPhase = boss;

        private void Update()
        {
            if (_config == null) return;
            float dt = Time.deltaTime;
            Move(dt);
            TickInvuln(dt);
        }

        private void Move(float dt)
        {
            Vector3 pos = transform.position;

            if (_input != null && _input.HasPointerTarget)
            {
                Vector2 target = _input.PointerWorld;
                float t = Mathf.Min(1f, dt * _config.PointerFollowLerp);
                pos.x = Mathf.Lerp(pos.x, target.x, t);
                pos.y = Mathf.Lerp(pos.y, target.y, t);
            }
            else if (_input != null)
            {
                Vector2 axis = _input.MoveAxis;
                if (axis.sqrMagnitude > 1f) axis = axis.normalized;
                pos += (Vector3)axis * (_config.MoveSpeed * dt);
            }

            float maxY = _bossPhase ? _config.BossMaxY : _config.StageMaxY;
            pos.x = Mathf.Clamp(pos.x, _config.MinX, _config.MaxX);
            pos.y = Mathf.Clamp(pos.y, _config.MinY, maxY);
            transform.position = pos;
        }

        private void TickInvuln(float dt)
        {
            if (_invulnRemaining <= 0f) return;
            _invulnRemaining -= dt;
            if (_sprite != null)
            {
                bool blinkOff = Mathf.Repeat(Time.unscaledTime, 0.14f) < 0.07f;
                var c = _baseColor;
                c.a = blinkOff ? 0.35f : 1f;
                _sprite.color = c;
            }
            if (_invulnRemaining <= 0f && _sprite != null) _sprite.color = _baseColor;
        }

        /// <summary>Apply <paramref name="amount"/> damage unless dead or within i-frames. Triggers i-frames on a hit.</summary>
        public void TakeDamage(float amount)
        {
            if (!IsAlive || _invulnRemaining > 0f || amount <= 0f) return;
            _hp = Mathf.Max(0, _hp - Mathf.CeilToInt(amount));
            _invulnRemaining = _config.InvulnSeconds;
            HpChanged?.Invoke(_hp, MaxHp);
            if (_hp <= 0) Died?.Invoke();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Enemy body contact — damage from the enemy def, then the enemy is not consumed (it rams through).
            var enemy = other.GetComponentInParent<EnemyController>();
            if (enemy != null && enemy.Def != null)
            {
                TakeDamage(enemy.Def.ContactDamage);
                return;
            }
            // Enemy bullet — take its damage and despawn it (the player owns hit detection; App sees Stage).
            var bullet = other.GetComponentInParent<EnemyBullet>();
            if (bullet != null && bullet.IsActive)
            {
                TakeDamage(bullet.Damage);
                bullet.Despawn();
            }
        }
    }
}
