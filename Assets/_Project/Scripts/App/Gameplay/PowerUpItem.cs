using KaijuBreaker.Content;
using UnityEngine;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>The kind of in-run power-up (Raiden-style): firepower, missile, or a weapon-type switch.</summary>
    public enum PowerUpKind { Power, Missile, WeaponLaser, WeaponMissile }

    /// <summary>
    /// An in-stage power-up that drifts down for the player to collect (Raiden-style in-run strengthening).
    /// <b>P</b> raises firepower, <b>M</b> raises the missile level, <b>W</b> pods switch the current weapon
    /// type — all of which boost KILLING POWER this run only (utility stats are the separate meta upgrades).
    /// Collection is player-owned trigger overlap; the item tints itself by kind so the type reads at a glance.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PowerUpItem : MonoBehaviour
    {
        [SerializeField] private float _fallSpeed = 2.2f;
        [SerializeField] private SpriteRenderer _sprite;

        private PowerUpKind _kind;

        // Meta pickup-magnet (Crystal core): items within MagnetRadius of MagnetTarget home toward it. Set by the
        // scene director at run start; static so every spawned item shares one config. Radius 0 = no magnet (level 0).
        public static Transform MagnetTarget;
        public static float MagnetRadius;

        private static readonly Color PowerColor = new Color(0.45f, 1f, 0.5f);      // P — green
        private static readonly Color MissileColor = new Color(0.45f, 0.7f, 1f);    // M — blue
        private static readonly Color LaserPodColor = new Color(0.75f, 0.5f, 1f);   // W laser — purple
        private static readonly Color MissilePodColor = new Color(1f, 0.82f, 0.35f); // W missile — gold

        private void Awake()
        {
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.useFullKinematicContacts = true;
            if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
        }

        /// <summary>Set the kind (and tint) after Instantiate.</summary>
        public void Init(PowerUpKind kind)
        {
            _kind = kind;
            if (_sprite != null) _sprite.color = ColorFor(kind);
        }

        private static Color ColorFor(PowerUpKind k)
        {
            switch (k)
            {
                case PowerUpKind.Missile: return MissileColor;
                case PowerUpKind.WeaponLaser: return LaserPodColor;
                case PowerUpKind.WeaponMissile: return MissilePodColor;
                default: return PowerColor;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Crystal-core magnet: once the player is within range, the item homes in (a convenience QoL pull — it
            // never adds power, only makes the P/M/W items easier to grab).
            if (MagnetTarget != null && MagnetRadius > 0f)
            {
                Vector3 to = MagnetTarget.position - transform.position;
                if (to.sqrMagnitude <= MagnetRadius * MagnetRadius)
                {
                    transform.position += to.normalized * (Mathf.Max(_fallSpeed * 2.5f, 5f) * dt);
                    return;
                }
            }

            transform.position += Vector3.down * (_fallSpeed * dt);
            if (transform.position.y < -8f) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var weapon = other.GetComponentInParent<PlayerWeaponController>();
            if (weapon == null) return;
            switch (_kind)
            {
                // Power is the single generic strengthen chip — it raises the player's CURRENT loadout (both the
                // primary firepower and the missile level), so it's never "the wrong weapon" (session 15).
                case PowerUpKind.Power: weapon.AddArsenalPower(); break;
                case PowerUpKind.Missile: weapon.AddMissilePower(); break;
                case PowerUpKind.WeaponLaser: weapon.CyclePrimary(); break;
                case PowerUpKind.WeaponMissile: weapon.CycleSecondary(); break;
            }
            Destroy(gameObject);
        }
    }
}
