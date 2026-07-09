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

        [Tooltip("Target WORLD size (units) for untuned placeholder parts (those left at Unity's default 1×1 box " +
                 "collider). They shipped with Unity's tiny built-in sprite (~0.16 world), so scaling the transform " +
                 "left the visible block bullet-sized while the collider grew mismatched. Instead we give the part a " +
                 "1-unit square sprite (colour preserved) and size the whole part to this, so sprite AND hitbox both " +
                 "equal it. ~2.0 reads like a real boss part (≈10× a bullet). Hand-authored parts (collider ≠ 1×1) " +
                 "are untouched. Lower it if a many-part boss's blocks overlap into a blob.")]
        [SerializeField] private float _placeholderWorldSize = 2.0f;

        [Tooltip("Optional 'destroyed' sprite (e.g. a broken limb stub). When the part breaks, instead of vanishing " +
                 "it swaps to this and its collider is disabled — so a severed leg leaves a visible stump. Leave null " +
                 "to hide the part on break (the default).")]
        [SerializeField] private Sprite _brokenSprite;

        [Tooltip("Optional child stub object (a severed-limb stump the artist positions/scales freely in the scene). " +
                 "When set, breaking the part ACTIVATES this child and hides the intact sprite + collider instead of " +
                 "swapping _brokenSprite in place — so the stump has its own independent, draggable transform. Leave " +
                 "null to fall back to _brokenSprite (in-place swap) or hiding the part.")]
        [SerializeField] private GameObject _brokenStub;

        private int _partId = -1;
        private int _kaijuId;
        private IEventBus _bus;
        private SpriteRenderer _sr;
        private Color _baseColor = Color.white;
        private float _flashRemaining;
        private bool _stripped;
        private bool _softened;
        private bool _broken;

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

            _sr = GetComponent<SpriteRenderer>();

            // Player shots register hits through OnTriggerEnter2D (see PlayerProjectile). A part collider left as
            // a solid (non-trigger) collider — as the procedurally-built new bosses were (m_IsTrigger: 0) — never
            // raises that event, so shots pass straight through and the part can never soften or break. Force
            // every part's collider to a trigger so hit detection is uniform across hand-authored and generated
            // bosses. (This is the root cause of the "new boss parts can't be broken" reports.)
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;

                // Untuned placeholder parts shipped with Unity's tiny built-in sprite (~0.16 world) on a default
                // 1×1 collider — so the visible block came out bullet-sized while the collider grew mismatched.
                // Give the part a 1-world-unit square sprite (the part's colour is preserved as a tint) and size
                // the whole part to a readable world size, so the SPRITE and the COLLIDER both equal it. Scaling a
                // 1-unit sprite (not the ~0.16 built-in) also keeps the child gauge bars sane. Hand-authored parts
                // (collider ≠ 1×1) keep their real art and tuned size.
                if (col is BoxCollider2D box &&
                    Mathf.Approximately(box.size.x, 1f) && Mathf.Approximately(box.size.y, 1f))
                {
                    float target = _placeholderWorldSize > 0f ? _placeholderWorldSize : 2.0f;
                    if (_sr != null) _sr.sprite = BarSprite(); // shared 1-unit white square, tinted by _sr.color
                    box.size = Vector2.one;                    // 1-unit box → collider world size == target
                    transform.localScale = new Vector3(target, target, 1f);
                }
            }

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
            _broken = false;
            var col0 = GetComponent<Collider2D>();
            if (col0 != null) col0.enabled = true; // new fight — part is whole and hittable again
            if (_sr != null) _sr.enabled = true;    // undo a prior break's stub-path renderer disable
            if (_brokenStub != null) _brokenStub.SetActive(false); // hide the severed stump again
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

        /// <summary>
        /// Enable/disable the hitbox for a cross-part HittableWhen gate (per-part-firing-schema.md §4):
        /// while the gate is closed the part is un-hittable and shots pass through it. No-op once the part is
        /// broken/severed (its collider is already off and must stay off).
        /// </summary>
        public void SetHittable(bool hittable)
        {
            if (_broken) return;
            var col = GetComponent<Collider2D>();
            if (col != null && col.enabled != hittable) col.enabled = hittable;
        }


        /// <summary>
        /// Called when the part breaks. If a <see cref="_brokenSprite"/> is authored (e.g. a severed-limb stub),
        /// swap to it and disable the collider so the stump stays visible but can't be hit again; otherwise hide
        /// the part entirely (the default for cores/armour with no stub art).
        /// </summary>
        public void Hide()
        {
            _broken = true;

            // Preferred: a dedicated child stub object the artist has positioned + scaled in the scene.
            // Activate it and hide the intact part (sprite + collider), so the severed stump has its own
            // independent, draggable transform instead of reusing the intact leg's scale/pivot.
            if (_brokenStub != null)
            {
                _flashRemaining = 0f;
                if (_sr != null) _sr.enabled = false;
                var colStub = GetComponent<Collider2D>();
                if (colStub != null) colStub.enabled = false; // severed — no longer a target
                _brokenStub.SetActive(true);
                SetGauge(0f, 0f);
                return;
            }

            // Fallback: swap this part's own sprite to a stub in place, keeping its transform.
            if (_brokenSprite != null && _sr != null)
            {
                _flashRemaining = 0f;
                _sr.sprite = _brokenSprite;
                _sr.color = _baseColor;
                var col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false; // severed — no longer a target
                SetGauge(0f, 0f);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

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
            if (_broken) // a severed part keeps its stub — never restore intact/stripped art over it
            {
                // Child-stub path: the intact renderer is disabled and the stub child shows instead — leave it.
                if (_brokenStub != null) return;
                if (_brokenSprite != null) { _sr.sprite = _brokenSprite; _sr.color = _baseColor; }
                return;
            }
            _sr.sprite = (_stripped && _strippedSprite != null) ? _strippedSprite : _intactSprite;
            _sr.color = _softened ? new Color(1f, 0.62f, 0.42f) : _baseColor; // warm = heated/softened
        }
    }
}
