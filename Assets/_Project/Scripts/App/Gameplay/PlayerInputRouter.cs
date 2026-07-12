using UnityEngine;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>
    /// Cross-platform player input (<see cref="IPlayerInput"/>): PC keyboard (WASD/arrows + Space) and a mobile
    /// on-screen fixed virtual joystick (bottom-left, move) + secondary-fire button (bottom-right), both driven
    /// by screen-region touch/mouse polling — no UGUI Canvas/EventSystem needed. The two paths merge so the same
    /// ship code serves both platforms at parity (technical-preferences); the joystick overrides the keyboard
    /// only while actively held. IMGUI draws the controls so they are visible and usable on touch and with a
    /// mouse. (A polished UGUI control set is an art/UX follow-up.)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInputRouter : MonoBehaviour, IPlayerInput
    {
        [Tooltip("Master switch for the on-screen joystick + fire button. Only takes effect on a mobile platform " +
                 "(or when _forceTouchControlsInEditor is on); PC/desktop always uses keyboard/mouse only.")]
        [SerializeField] private bool _showTouchControls = true;

        [Tooltip("Editor-only: force the touch controls on in the editor for testing (ignored in real builds).")]
        [SerializeField] private bool _forceTouchControlsInEditor = false;

        // Touch UI is a MOBILE-only affordance (director: PC needs no virtual joystick). isMobilePlatform is true
        // on iOS/Android builds and false on desktop + editor; the editor flag lets us still test the layout.
        private bool TouchUiActive =>
            _showTouchControls && (Application.isMobilePlatform || (Application.isEditor && _forceTouchControlsInEditor));

        [Tooltip("Finger travel (in joystick radii) needed to reach full speed. Higher = LESS sensitive / more precise. 1 = old twitchy behaviour.")]
        [Range(1f, 3f)]
        [SerializeField] private float _joyTravelMult = 1.9f;

        [Tooltip("Ignore joystick displacement below this fraction of full travel (kills centre jitter / drift). 0 = none.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _joyDeadzone = 0.12f;

        private Vector2 _joyBase, _fireCenter, _chargeCenter;
        private float _joyRadius, _fireRadius, _chargeRadius;
        private Vector2 _joyAxis;   // computed each frame
        private bool _secondaryHeld; // fire input held this frame (touch in button region / key held)
        private bool _primaryHeld;   // charge (集氣) input held this frame

        /// <summary>Show + poll the on-screen charge (集氣) button. Set true only when the run's primary is the
        /// 波動 charge weapon (L3); other primaries auto-fire and need no charge control. Set by the scene at run
        /// start (the primary is fixed for the whole run — no in-run weapon switching).</summary>
        public bool ChargeControlVisible { get; set; }

        private void Update()
        {
            _joyAxis = Vector2.zero;
            // Hold-to-fire: true while the secondary input is HELD (keyboard/mouse on PC; the on-screen button on mobile).
            _secondaryHeld = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(1);

            if (!TouchUiActive)
            {
                // PC: no on-screen controls. Charge (集氣) = hold left mouse or J (left mouse is free here —
                // PlayerInputRouter is axis-based, HasPointerTarget is false — unlike KeyboardMouseInput).
                _primaryHeld = Input.GetMouseButton(0) || Input.GetKey(KeyCode.J);
                return;
            }

            _primaryHeld = false;
            LayoutControls();
            // Touch first (mobile); mouse fallback lets the layout be tested in-editor when forced on.
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) continue;
                    if (InCircle(t.position, _joyBase, _joyRadius * (_joyTravelMult + 0.4f))) _joyAxis = AxisFrom(t.position);
                    if (InCircle(t.position, _fireCenter, _fireRadius)) _secondaryHeld = true; // any phase = held
                    if (ChargeControlVisible && InCircle(t.position, _chargeCenter, _chargeRadius)) _primaryHeld = true;
                }
            }
            else if (Input.GetMouseButton(0))
            {
                Vector2 m = Input.mousePosition;
                if (InCircle(m, _joyBase, _joyRadius * (_joyTravelMult + 0.4f))) _joyAxis = AxisFrom(m);
                else if (InCircle(m, _fireCenter, _fireRadius)) _secondaryHeld = true; // click-hold the on-screen button
                else if (ChargeControlVisible && InCircle(m, _chargeCenter, _chargeRadius)) _primaryHeld = true;
            }
        }

        private void LayoutControls()
        {
            float w = Screen.width, h = Screen.height;
            _joyRadius = Mathf.Min(w, h) * 0.10f;
            _fireRadius = _joyRadius * 0.9f;
            _chargeRadius = _joyRadius * 0.82f;
            _joyBase = new Vector2(w * 0.16f, h * 0.20f);
            _fireCenter = new Vector2(w * 0.84f, h * 0.18f);
            // Charge button sits just above the secondary-fire button (both right-thumb, stacked — director layout).
            _chargeCenter = new Vector2(w * 0.84f, h * 0.18f + (_fireRadius + _chargeRadius) * 1.25f);
        }

        private Vector2 AxisFrom(Vector2 screenPos)
        {
            // Full speed needs _joyTravelMult radii of finger travel (lower sensitivity than a raw 1-radius map).
            Vector2 v = (screenPos - _joyBase) / (_joyRadius * _joyTravelMult);
            float m = v.magnitude;
            if (m <= _joyDeadzone) return Vector2.zero;             // centre deadzone: no drift
            if (m > 1f) return v / m;                               // clamp to full speed
            // Rescale so the axis ramps 0→1 across [deadzone, 1] (no speed jump at the deadzone edge).
            return v * ((m - _joyDeadzone) / (1f - _joyDeadzone) / m);
        }

        private static bool InCircle(Vector2 p, Vector2 c, float r) => (p - c).sqrMagnitude <= r * r;

        // ── IPlayerInput ──────────────────────────────────────────────────────────
        public Vector2 MoveAxis
        {
            get
            {
                if (_joyAxis.sqrMagnitude > 0.0001f) return _joyAxis; // joystick overrides keyboard while held
                float x = (Key(KeyCode.D) || Key(KeyCode.RightArrow) ? 1f : 0f) - (Key(KeyCode.A) || Key(KeyCode.LeftArrow) ? 1f : 0f);
                float y = (Key(KeyCode.W) || Key(KeyCode.UpArrow) ? 1f : 0f) - (Key(KeyCode.S) || Key(KeyCode.DownArrow) ? 1f : 0f);
                return new Vector2(x, y);
            }
        }

        public bool HasPointerTarget => false; // axis-based movement (keyboard / joystick); no drag-to-point
        public Vector2 PointerWorld => Vector2.zero;
        public bool SecondaryPressedThisFrame => _secondaryHeld;
        public bool PrimaryHeld => _primaryHeld;

        private static bool Key(KeyCode k) => Input.GetKey(k);

        private Texture2D _tex;
        private void OnGUI()
        {
            if (!TouchUiActive) return;
            if (_tex == null) { _tex = new Texture2D(1, 1); _tex.SetPixel(0, 0, Color.white); _tex.Apply(); }
            var disc = GameUiSkin.Ring != null ? GameUiSkin.Ring : _tex; // soft radial disc from the shared skin
            // IMGUI y is top-down; screen coords are bottom-up.
            DrawDisc(disc, _joyBase, _joyRadius, new Color(0.35f, 0.85f, 1f, 0.30f));                 // joystick base
            DrawDisc(disc, _joyBase + _joyAxis * _joyRadius, _joyRadius * 0.5f, new Color(0.5f, 0.97f, 1f, 0.85f)); // handle
            DrawDisc(disc, _fireCenter, _fireRadius, new Color(1f, 0.55f, 0.32f, 0.55f));             // fire button
            // Charge (集氣) button — only when the run's primary is the 波動 charge weapon. Brightens while held.
            if (ChargeControlVisible)
            {
                var cc = _primaryHeld ? new Color(0.6f, 0.95f, 1f, 0.85f) : new Color(0.4f, 0.8f, 1f, 0.5f);
                DrawDisc(disc, _chargeCenter, _chargeRadius, cc);
                DrawChargeLabel(_chargeCenter, _chargeRadius);
            }
        }

        // Small "集氣" caption centred on the charge button so its purpose is unambiguous on touch.
        private void DrawChargeLabel(Vector2 screenCenter, float r)
        {
            var style = GameUiSkin.LabelStyle != null ? GameUiSkin.LabelStyle : GUI.skin.label;
            var prev = style.alignment; var prevColor = GUI.color;
            style.alignment = TextAnchor.MiddleCenter; GUI.color = Color.white;
            GUI.Label(new Rect(screenCenter.x - r, Screen.height - screenCenter.y - r, r * 2f, r * 2f), "集氣", style);
            style.alignment = prev; GUI.color = prevColor;
        }

        private void DrawDisc(Texture2D tex, Vector2 screenCenter, float r, Color c)
        {
            var prev = GUI.color; GUI.color = c;
            GUI.DrawTexture(new Rect(screenCenter.x - r, Screen.height - screenCenter.y - r, r * 2f, r * 2f), tex);
            GUI.color = prev;
        }
    }
}
