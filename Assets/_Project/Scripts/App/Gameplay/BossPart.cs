using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>
    /// A boss's breakable part in the scene: a collider the player's shots hit, mapped to a real
    /// <see cref="KaijuBreaker.KaijuParts.PartStateSystem"/> part id. On a hit it publishes the value-struct
    /// weapon event (<see cref="LaserHit"/> heat from lasers, <see cref="MissileHit"/> break from missiles) —
    /// it NEVER touches part state directly (KaijuParts owns that, ADR-0002/§5 manifest). The heat→soften→break
    /// dual track, armor gating, stagger, and the <see cref="BossCoreBroke"/> win are all decided by the system.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class BossPart : MonoBehaviour
    {
        [Tooltip("Authored part id string in the KaijuDef (e.g. 'core', 'mandible_l'). Mapped to the runtime int id.")]
        [SerializeField] private string _partName = string.Empty;

        private int _partId = -1;
        private int _kaijuId;
        private IEventBus _bus;

        /// <summary>Authored part-id string (matches a PartDef.PartId in the boss's KaijuDef).</summary>
        public string PartName => _partName;

        /// <summary>Runtime int part id, resolved at boss start (−1 until configured).</summary>
        public int PartId => _partId;

        private void Awake()
        {
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.useFullKinematicContacts = true;
        }

        /// <summary>Bind this scene part to its runtime part id + the bus (called by <see cref="BossController"/>).</summary>
        public void Configure(int partId, int kaijuId, IEventBus bus)
        {
            _partId = partId;
            _kaijuId = kaijuId;
            _bus = bus;
        }

        /// <summary>Report a laser hit (heat, soften track).</summary>
        public void ReceiveLaser(float heatDelta)
        {
            if (_bus != null && _partId >= 0) _bus.Publish(new LaserHit(_partId, _kaijuId, heatDelta));
        }

        /// <summary>Report a missile hit (break track). The system's armor/heat gate decides if it lands.</summary>
        public void ReceiveMissile(float breakDeltaBase, WeaponId weapon)
        {
            if (_bus != null && _partId >= 0) _bus.Publish(new MissileHit(_partId, _kaijuId, breakDeltaBase, weapon));
        }

        /// <summary>Hide this part (called when it breaks).</summary>
        public void Hide() => gameObject.SetActive(false);
    }
}
