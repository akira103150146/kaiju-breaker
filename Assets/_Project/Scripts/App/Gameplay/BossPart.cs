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

        [Tooltip("Untuned placeholder parts (left at Unity's default 1×1 box collider) are scaled up by this so " +
                 "the block reads clearly and the hittable area is generous. Hand-authored parts, whose collider " +
                 "carries a real art-matched size (never exactly 1×1), are left untouched.")]
        [SerializeField] private float _placeholderPartScaleMult = 1.4f;

        private int _partId = -1;
        private int _kaijuId;
        private IEventBus _bus;
        private SpriteRenderer _sr;
        private Color _baseColor = Color.white;
        private float _flashRemaining;
        private bool _stripped;
        private bool _softened;

        // Per-part gauges (heat = orange soften track / break = red destroy track), like the prototype meters.
        private SpriteRenderer _heatFill, _breakFill;
        private const float BarW = 1.15f, BarH = 0.13f;
        // Hit juice: a quick scale pop on every hit (works even for placeholder parts with no white silhouette).
        private float _popRemaining;
        private Vector3 _popBaseScale = Vector3.one;
        private const float PopSeconds = 0.12f;

        private static Sprite _barSprite;
        private static Sprite BarSprite()
        {
            if (_barSprite == null)
            {
                var t = new Texture2D(1, 1); t.SetPixel(0, 0, Color.white); t.Apply();
                _barSprite = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            return _barSprite;
        }

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

            // Player shots register hits through OnTriggerEnter2D (see PlayerProjectile). A part collider left as
            // a solid (non-trigger) collider — as the procedurally-built new bosses were (m_IsTrigger: 0) — never
            // raises that event, so shots pass straight through and the part can never soften or break. Force
            // every part's collider to a trigger so hit detection is uniform across hand-authored and generated
            // bosses. (This is the root cause of the "new boss parts can't be broken" reports.)
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;

                // Placeholder parts were left at Unity's default 1×1 box collider (hand-tuned parts carry a real,
                // art-matched size that is never exactly 1×1). Grow the whole part so both the visible block and
                // the hittable area read clearly — the "new boss parts too small" reports. localScale scales the
                // collider and sprite together, keeping them matched, and runs once (Awake). Authored parts, whose
                // collider size ≠ 1×1, are untouched.
                // 0 = field absent on an old serialized instance → fall back to the default rather than skip.
                float mult = _placeholderPartScaleMult > 0f ? _placeholderPartScaleMult : 1.4f;
                if (col is BoxCollider2D box &&
                    Mathf.Approximately(box.size.x, 1f) && Mathf.Approximately(box.size.y, 1f) &&
                    mult > 1f)
                {
                    transform.localScale *= mult;
                }
            }

            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null)
            {
                if (_intactSprite == null) _intactSprite = _sr.sprite;
                _baseColor = _sr.color;
            }
            _popBaseScale = transform.localScale;
            BuildGauges();
        }

        // Two stacked gauge bars above the part: heat (orange, soften) + break (red, destroy), left-anchored.
        private void BuildGauges()
        {
            int order = (_sr != null ? _sr.sortingOrder : 0) + 10;
            MakeBar("gauge_heat_bg", 0.90f, order, new Color(0f, 0f, 0f, 0.5f));
            _heatFill = MakeBar("gauge_heat", 0.90f, order + 1, new Color(1f, 0.6f, 0.15f, 0.95f));
            MakeBar("gauge_break_bg", 0.75f, order, new Color(0f, 0f, 0f, 0.5f));
            _breakFill = MakeBar("gauge_break", 0.75f, order + 1, new Color(1f, 0.25f, 0.2f, 0.95f));
            SetFill(_heatFill, 0f); SetFill(_breakFill, 0f);
        }

        private SpriteRenderer MakeBar(string n, float y, int order, Color c)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, y, 0f);
            go.transform.localScale = new Vector3(BarW, BarH, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BarSprite(); sr.color = c; sr.sortingOrder = order;
            return sr;
        }

        // Left-anchor a fill bar to fraction f (0..1).
        private static void SetFill(SpriteRenderer fill, float f)
        {
            if (fill == null) return;
            f = Mathf.Clamp01(f);
            fill.transform.localScale = new Vector3(BarW * f, BarH, 1f);
            fill.transform.localPosition = new Vector3(-BarW * 0.5f * (1f - f), fill.transform.localPosition.y, 0f);
        }

        /// <summary>Update the heat + break gauge fractions (0..1). Called each frame by <see cref="BossController"/>.</summary>
        public void SetGauge(float heatFrac, float breakFrac)
        {
            SetFill(_heatFill, heatFrac);
            SetFill(_breakFill, breakFrac);
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
            _popRemaining = PopSeconds;
        }

        /// <summary>Report a missile hit (break track). The system's armor/heat gate decides if it lands.</summary>
        public void ReceiveMissile(float breakDeltaBase, WeaponId weapon)
        {
            if (_bus != null && _partId >= 0) _bus.Publish(new MissileHit(_partId, _kaijuId, breakDeltaBase, weapon));
            _flashRemaining = _hitFlashSeconds;
            _popRemaining = PopSeconds;
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

            // Hit juice: a quick scale pop on every hit (decays back to the authored scale).
            if (_popRemaining > 0f)
            {
                _popRemaining -= Time.deltaTime;
                float k = Mathf.Clamp01(_popRemaining / PopSeconds);
                transform.localScale = _popBaseScale * (1f + 0.20f * k);
                if (_popRemaining <= 0f) transform.localScale = _popBaseScale;
            }

            if (_flashRemaining > 0f)
            {
                _flashRemaining -= Time.deltaTime;
                // White silhouette if provided (real art); otherwise brighten the tint (placeholder blocks).
                if (_hitWhiteSprite != null) { _sr.sprite = _hitWhiteSprite; _sr.color = Color.white; }
                else _sr.color = Color.Lerp(_baseColor, Color.white, 0.8f);
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
