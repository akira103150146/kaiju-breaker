using KaijuBreaker.Core;
using UnityEngine;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>
    /// A boss's breakable part in the scene: a collider the player's shots hit, mapped to a real
    /// <see cref="KaijuBreaker.KaijuParts.PartStateSystem"/> part id. On a hit it publishes the value-struct
    /// weapon event (<see cref="LaserHit"/> heat from lasers, <see cref="MissileHit"/> break from missiles) —
    /// it NEVER touches part state directly (KaijuParts owns that, ADR-0002/§5 manifest). It also owns its own
    /// readout: a white-silhouette hit flash, an intact↔stripped armor sprite swap, and a warm heated tint while
    /// softened — driven by <see cref="BossController"/> reading the part system each frame. (A white tint
    /// multiplies to nothing on coloured art, so the flash swaps to a pre-baked white silhouette, per §demo.)
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class BossPart : MonoBehaviour
    {
        [Tooltip("Authored part id string in the KaijuDef (e.g. 'core', 'mandible_l'). Mapped to the runtime int id.")]
        [SerializeField] private string _partName = string.Empty;

        [Header("Visual states (auto-filled from the SpriteRenderer if left null)")]
        [SerializeField] private Sprite _intactSprite;
        [Tooltip("Armored parts only: shown once the armor is stripped.")]
        [SerializeField] private Sprite _strippedSprite;
        [Tooltip("Pure-white silhouette flashed on a hit (a white tint is invisible on coloured art).")]
        [SerializeField] private Sprite _hitWhiteSprite;
        [SerializeField] private float _hitFlashSeconds = 0.06f;

        private int _partId = -1;
        private int _kaijuId;
        private IEventBus _bus;
        private SpriteRenderer _sr;
        private Color _baseColor = Color.white;
        private float _flashRemaining;
        private bool _stripped;
        private bool _softened;

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

            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null)
            {
                if (_intactSprite == null) _intactSprite = _sr.sprite;
                _baseColor = _sr.color;
            }
        }

        /// <summary>Bind this scene part to its runtime part id + the bus (called by <see cref="BossController"/>).</summary>
        public void Configure(int partId, int kaijuId, IEventBus bus)
        {
            _partId = partId;
            _kaijuId = kaijuId;
            _bus = bus;
            _stripped = false;
            _softened = false;
            RestoreVisual();
        }

        /// <summary>Report a laser hit (heat, soften track). Flashes the white silhouette.</summary>
        public void ReceiveLaser(float heatDelta)
        {
            if (_bus != null && _partId >= 0) _bus.Publish(new LaserHit(_partId, _kaijuId, heatDelta));
            _flashRemaining = _hitFlashSeconds;
        }

        /// <summary>Report a missile hit (break track). The system's armor/heat gate decides if it lands.</summary>
        public void ReceiveMissile(float breakDeltaBase, WeaponId weapon)
        {
            if (_bus != null && _partId >= 0) _bus.Publish(new MissileHit(_partId, _kaijuId, breakDeltaBase, weapon));
            _flashRemaining = _hitFlashSeconds;
        }

        /// <summary>Swap intact↔stripped art (armored parts) — called by the controller from the armor state.</summary>
        public void SetArmorStripped(bool stripped)
        {
            if (_stripped == stripped) return;
            _stripped = stripped;
            if (_flashRemaining <= 0f) RestoreVisual();
        }

        /// <summary>Toggle the warm heated tint (softened) — called by the controller from the heat state.</summary>
        public void SetSoftened(bool softened)
        {
            if (_softened == softened) return;
            _softened = softened;
            if (_flashRemaining <= 0f) RestoreVisual();
        }

        /// <summary>Hide this part (called when it breaks).</summary>
        public void Hide() => gameObject.SetActive(false);

        private void Update()
        {
            if (_sr == null) return;
            if (_flashRemaining > 0f)
            {
                _flashRemaining -= Time.deltaTime;
                if (_hitWhiteSprite != null) { _sr.sprite = _hitWhiteSprite; _sr.color = Color.white; }
                if (_flashRemaining <= 0f) RestoreVisual();
            }
        }

        private void RestoreVisual()
        {
            if (_sr == null) return;
            _sr.sprite = (_stripped && _strippedSprite != null) ? _strippedSprite : _intactSprite;
            _sr.color = _softened ? new Color(1f, 0.62f, 0.42f) : _baseColor; // warm = heated/softened
        }
    }
}
