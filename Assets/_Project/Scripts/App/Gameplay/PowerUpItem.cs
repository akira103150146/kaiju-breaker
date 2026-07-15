using KaijuBreaker.Content;
using TMPro;
using UnityEngine;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>The kind of in-run power-up (Raiden-style): firepower, missile, or a weapon-type switch.</summary>
    public enum PowerUpKind { Power, Missile, WeaponLaser, WeaponMissile }

    /// <summary>
    /// An in-stage power-up that drifts down for the player to collect (Raiden-style in-run strengthening).
    /// 「主」raises the primary firepower, 「副」raises the missile level, <b>W</b> pods switch the current weapon
    /// type — all of which boost KILLING POWER this run only (utility stats are the separate meta upgrades).
    /// Collection is player-owned trigger overlap.
    /// <para>
    /// Visually the item is built to read as a PICKUP, never as a bullet (director): a compact badge with a
    /// bright white frame, a coloured core, a glyph (主 / 副 / 雷 / 彈), and a soft halo — and it slowly spins and
    /// pulses. Enemy bullets are small, warm, and static, so there is no chance of confusing the two. The chip's
    /// visuals are generated in code (no art dependency); enemy-bullet art is untouched.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PowerUpItem : MonoBehaviour
    {
        [SerializeField] private float _fallSpeed = 2.2f;
        [SerializeField] private SpriteRenderer _sprite; // repurposed as the halo glow behind the chip

        private PowerUpKind _kind;
        private Transform _spin;          // chip pivot that rotates
        private SpriteRenderer _haloSr;   // pulsing glow
        private float _phase;             // per-item animation phase so a cluster of drops doesn't pulse in lockstep

        // Meta pickup-magnet (Crystal core): items within MagnetRadius of MagnetTarget home toward it. Set by the
        // scene director at run start; static so every spawned item shares one config. Radius 0 = no magnet (level 0).
        public static Transform MagnetTarget;
        public static float MagnetRadius;

        // Bright, high-chroma cores (clearly not the warm enemy-bullet palette). Frame is always white.
        private static readonly Color PowerCore = new Color(0.35f, 1f, 0.42f);       // P — vivid green
        private static readonly Color MissileCore = new Color(0.35f, 0.68f, 1f);     // M — vivid blue
        private static readonly Color LaserPodCore = new Color(0.72f, 0.45f, 1f);    // W laser — violet
        private static readonly Color MissilePodCore = new Color(1f, 0.80f, 0.30f);  // W missile — gold

        private const float ChipWorld = 0.29f;   // rendered chip size (world units) — small badge, still clearly not a bullet
        private const float HaloWorld = 0.52f;
        private const float SpinDegPerSec = 90f;
        private const float PulseHz = 2.2f;

        private void Awake()
        {
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.useFullKinematicContacts = true;
            if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();

            // Enlarge the trigger so the bigger chip is grabbed by its whole footprint.
            var box = GetComponent<BoxCollider2D>();
            if (box != null) box.size = new Vector2(ChipWorld, ChipWorld);

            BuildVisuals();
        }

        /// <summary>Set the kind (tint + glyph) after Instantiate.</summary>
        public void Init(PowerUpKind kind)
        {
            _kind = kind;
            Color core = CoreFor(kind);
            if (_haloSr != null) _haloSr.color = new Color(core.r, core.g, core.b, 0.35f);
            if (_chipSr != null) _chipSr.color = core;
            if (_glyph != null) { _glyph.text = GlyphFor(kind); _glyph.color = new Color(1f, 1f, 1f, 1f); }
        }

        private SpriteRenderer _chipSr;
        private TextMeshPro _glyph;

        // Build the chip once: halo (root renderer) → white frame → coloured core → glyph. All in the COLD/bright
        // pickup register, deliberately unlike the small warm enemy bullets.
        private void BuildVisuals()
        {
            int order = _sprite != null ? _sprite.sortingOrder : 6;

            // Root renderer = soft halo glow.
            if (_sprite != null)
            {
                _sprite.sprite = HaloSprite();
                _sprite.sortingOrder = order;                 // behind everything else on the chip
                _sprite.color = new Color(0.35f, 1f, 0.42f, 0.35f);
                _haloSr = _sprite;
                float hs = HaloWorld / SpriteWorldAt1(_sprite.sprite);
                _sprite.transform.localScale = new Vector3(hs, hs, 1f);
            }

            // Pulse pivot holds the frame + core so they breathe together. It is a DISC (not a rounded square):
            // a spinning square reads as a rotating diamond, which the director mistook for a bullet/menu marker —
            // a disc badge keeps one silhouette no matter what, so it can only read as a collectible medal.
            var spinGo = new GameObject("ChipPulse");
            _spin = spinGo.transform;
            _spin.SetParent(transform, false);

            // White disc = the medal body; the coloured core is smaller so the white shows as a bold ring border.
            MakeSprite("Frame", _spin, DiscSprite(), Color.white, order + 1, ChipWorld);
            _chipSr = MakeSprite("Core", _spin, DiscSprite(), PowerCore, order + 2, ChipWorld * 0.64f);

            // Upright glyph on the container (not the pulse pivot) so 「主」/「副」never distort. Uses the TMP default
            // font (Cubic 11 pixel font) so it matches the game's type. A small label (¼ of the old size) that sits
            // in the middle of the badge rather than filling it.
            var glyphGo = new GameObject("Glyph");
            glyphGo.transform.SetParent(transform, false);
            _glyph = glyphGo.AddComponent<TextMeshPro>();
            _glyph.text = "主";
            _glyph.alignment = TextAlignmentOptions.Center;
            _glyph.fontSize = 1.35f;
            _glyph.color = Color.white;
            _glyph.enableWordWrapping = false;
            _glyph.rectTransform.sizeDelta = new Vector2(0.9f, 0.9f);
            var glyphMr = glyphGo.GetComponent<MeshRenderer>();
            if (glyphMr != null) glyphMr.sortingOrder = order + 3;
        }

        private static SpriteRenderer MakeSprite(string name, Transform parent, Sprite sprite, Color color, int order, float world)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            float s = world / SpriteWorldAt1(sprite);
            go.transform.localScale = new Vector3(s, s, 1f);
            return sr;
        }

        private static float SpriteWorldAt1(Sprite s) => s != null ? s.bounds.size.x : 1f;

        private static Color CoreFor(PowerUpKind k)
        {
            switch (k)
            {
                case PowerUpKind.Missile: return MissileCore;
                case PowerUpKind.WeaponLaser: return LaserPodCore;
                case PowerUpKind.WeaponMissile: return MissilePodCore;
                default: return PowerCore;
            }
        }

        private static string GlyphFor(PowerUpKind k)
        {
            switch (k)
            {
                case PowerUpKind.Missile: return "副";
                case PowerUpKind.WeaponLaser: return "雷";
                case PowerUpKind.WeaponMissile: return "彈";
                default: return "主";
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _phase += dt;

            // Breathe (no spin) so the badge reads as a collectible, not a projectile. A disc badge does not need to
            // rotate — and rotating it was exactly what made it look like a diamond — so we pulse the scale instead.
            float pulse = 1f + 0.12f * Mathf.Sin((_phase) * PulseHz * Mathf.PI * 2f);
            if (_spin != null) _spin.localScale = new Vector3(pulse, pulse, 1f);
            if (_haloSr != null)
            {
                var c = _haloSr.color;
                c.a = 0.28f + 0.14f * (0.5f + 0.5f * Mathf.Sin(_phase * PulseHz * Mathf.PI));
                _haloSr.color = c;
            }

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
                // Two strengthen chips (director): 「主」raises ONLY the primary weapon's firepower, 「副」raises ONLY
                // the secondary (missile) level. The player chooses which to grab based on what they want to boost.
                case PowerUpKind.Power: weapon.AddWeaponPower(); break;
                case PowerUpKind.Missile: weapon.AddMissilePower(); break;
                case PowerUpKind.WeaponLaser: weapon.CyclePrimary(); break;
                case PowerUpKind.WeaponMissile: weapon.CycleSecondary(); break;
            }
            Destroy(gameObject);
        }

        // ── Procedural sprites (generated once, shared) ───────────────────────────────
        private static Sprite _roundedSquare, _halo, _disc;

        // Solid anti-aliased disc — the medal body/ring. A disc has one silhouette at any rotation, so the badge
        // can never be mistaken for a rotating diamond the way the old rounded square could.
        private static Sprite DiscSprite()
        {
            if (_disc != null) return _disc;
            const int N = 64; float c = (N - 1) * 0.5f; float r = N * 0.5f - 1f;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(r - d);   // 1 inside, soft 1px edge
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px); tex.Apply();
            _disc = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            return _disc;
        }

        private static Sprite RoundedSquareSprite()
        {
            if (_roundedSquare != null) return _roundedSquare;
            const int N = 64; const float radius = 16f;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    // Distance into the rounded-rect corner (0 inside, grows past the corner radius).
                    float dx = Mathf.Max(Mathf.Abs(x - (N - 1) * 0.5f) - (N * 0.5f - radius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y - (N - 1) * 0.5f) - (N * 0.5f - radius), 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radius - d);      // 1 inside, soft 1px edge
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px); tex.Apply();
            _roundedSquare = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            return _roundedSquare;
        }

        private static Sprite HaloSprite()
        {
            if (_halo != null) return _halo;
            const int N = 64; float c = (N - 1) * 0.5f; float r = N * 0.5f;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / r;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;                                // soft radial falloff
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px); tex.Apply();
            _halo = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            return _halo;
        }
    }
}
