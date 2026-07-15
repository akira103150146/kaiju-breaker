using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

        /// <summary>波動 charge fill [0,1] pushed in by the scene each frame — brightens the 集氣 button as it fills.</summary>
        public float ChargeFill { get; set; }

        private void Update()
        {
            _joyAxis = Vector2.zero;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            // Hold-to-fire: true while the secondary input is HELD (keyboard/mouse on PC; the on-screen button on mobile).
            _secondaryHeld = (kb != null && kb.spaceKey.isPressed) || (mouse != null && mouse.rightButton.isPressed);

            if (!TouchUiActive)
            {
                // PC: no on-screen controls. Charge (集氣) = hold left mouse, J, or Z (left mouse is free here —
                // PlayerInputRouter is axis-based, HasPointerTarget is false — unlike KeyboardMouseInput). Z sits next
                // to the movement keys so the charge weapon is one-handed on keyboard (director request).
                _primaryHeld = (mouse != null && mouse.leftButton.isPressed) || KeyHeld(Key.J) || KeyHeld(Key.Z);
                return;
            }

            _primaryHeld = false;
            LayoutControls();
            // Touch first (mobile); mouse fallback lets the layout be tested in-editor when forced on.
            var ts = Touchscreen.current;
            bool hadTouch = false;
            if (ts != null)
            {
                foreach (var t in ts.touches)
                {
                    if (!t.press.isPressed) continue; // active contact only (skips ended/canceled)
                    hadTouch = true;
                    Vector2 p = t.position.ReadValue();
                    if (InCircle(p, _joyBase, _joyRadius * (_joyTravelMult + 0.4f))) _joyAxis = AxisFrom(p);
                    if (InCircle(p, _fireCenter, _fireRadius)) _secondaryHeld = true; // any active touch = held
                    if (ChargeControlVisible && InCircle(p, _chargeCenter, _chargeRadius)) _primaryHeld = true;
                }
            }
            if (!hadTouch && mouse != null && mouse.leftButton.isPressed)
            {
                Vector2 m = mouse.position.ReadValue();
                if (InCircle(m, _joyBase, _joyRadius * (_joyTravelMult + 0.4f))) _joyAxis = AxisFrom(m);
                else if (InCircle(m, _fireCenter, _fireRadius)) _secondaryHeld = true; // click-hold the on-screen button
                else if (ChargeControlVisible && InCircle(m, _chargeCenter, _chargeRadius)) _primaryHeld = true;
            }

            EnsureTouchUi();
            UpdateTouchUi();
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
                float x = (KeyHeld(Key.D) || KeyHeld(Key.RightArrow) ? 1f : 0f) - (KeyHeld(Key.A) || KeyHeld(Key.LeftArrow) ? 1f : 0f);
                float y = (KeyHeld(Key.W) || KeyHeld(Key.UpArrow) ? 1f : 0f) - (KeyHeld(Key.S) || KeyHeld(Key.DownArrow) ? 1f : 0f);
                return new Vector2(x, y);
            }
        }

        public bool HasPointerTarget => false; // axis-based movement (keyboard / joystick); no drag-to-point
        public Vector2 PointerWorld => Vector2.zero;
        public bool SecondaryPressedThisFrame => _secondaryHeld;
        public bool PrimaryHeld => _primaryHeld;

        private static bool KeyHeld(Key k)
        {
            var kb = Keyboard.current;
            return kb != null && kb[k].isPressed;
        }

        // ── On-screen touch controls (UGUI + TMP, ADR-0006) ─────────────────────────
        private Canvas _touchCanvas;
        private Image _joyBaseImg, _joyHandleImg, _fireImg, _chargeImg;
        private TextMeshProUGUI _chargeLabel;
        private static Sprite _discSprite;
        private bool _touchUiBuilt;

        // Build the touch widgets once (mobile only). A dedicated overlay canvas with NO CanvasScaler, so 1 unit
        // == 1 screen pixel and the discs can be placed/sized directly in the same screen coordinates the input
        // polling already uses (bottom-left origin, matching Mouse.position / Touchscreen touch position).
        private void EnsureTouchUi()
        {
            if (_touchUiBuilt || !TouchUiActive) return;
            _touchUiBuilt = true;

            var go = new GameObject("TouchControls", typeof(RectTransform));
            _touchCanvas = go.AddComponent<Canvas>();
            _touchCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _touchCanvas.sortingOrder = 90; // below the menu/HUD canvas (100)

            var sprite = DiscSprite();
            _joyBaseImg = MakeDisc(go.transform, sprite, new Color(0.35f, 0.85f, 1f, 0.30f));
            _joyHandleImg = MakeDisc(go.transform, sprite, new Color(0.5f, 0.97f, 1f, 0.85f));
            _fireImg = MakeDisc(go.transform, sprite, new Color(1f, 0.55f, 0.32f, 0.55f));
            _chargeImg = MakeDisc(go.transform, sprite, new Color(0.4f, 0.8f, 1f, 0.5f));

            var lgo = new GameObject("Label", typeof(RectTransform));
            lgo.transform.SetParent(_chargeImg.transform, false);
            _chargeLabel = lgo.AddComponent<TextMeshProUGUI>();
            _chargeLabel.text = "集氣";
            _chargeLabel.alignment = TextAlignmentOptions.Center;
            _chargeLabel.color = Color.white;
            _chargeLabel.enableAutoSizing = true;
            _chargeLabel.fontSizeMin = 8f; _chargeLabel.fontSizeMax = 40f;
            _chargeLabel.raycastTarget = false;
            var lrt = _chargeLabel.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        }

        private void UpdateTouchUi()
        {
            if (!_touchUiBuilt) return;
            Place(_joyBaseImg, _joyBase, _joyRadius * 2f);
            Place(_joyHandleImg, _joyBase + _joyAxis * _joyRadius, _joyRadius);
            Place(_fireImg, _fireCenter, _fireRadius * 2f);
            _chargeImg.gameObject.SetActive(ChargeControlVisible);
            if (ChargeControlVisible)
            {
                Place(_chargeImg, _chargeCenter, _chargeRadius * 2f);
                // Brighten with charge fill so the button itself reads as a charge meter; flash white-hot at full.
                float f = Mathf.Clamp01(ChargeFill);
                _chargeImg.color = f >= 0.999f
                    ? new Color(0.8f, 1f, 1f, 0.95f)
                    : Color.Lerp(new Color(0.4f, 0.8f, 1f, 0.5f), new Color(0.6f, 0.95f, 1f, 0.9f), f);
            }
        }

        // Overlay canvas (no scaler): RectTransform.position is in screen pixels, so place discs directly.
        private static void Place(Image img, Vector2 screenPos, float diameter)
        {
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(diameter, diameter);
            rt.position = new Vector3(screenPos.x, screenPos.y, 0f);
        }

        private static Image MakeDisc(Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject("Disc", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite; img.color = color; img.raycastTarget = false;
            img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            return img;
        }

        // Soft radial disc sprite (bright centre → transparent edge), generated once and shared.
        private static Sprite DiscSprite()
        {
            if (_discSprite != null) return _discSprite;
            const int size = 64; float r = size * 0.5f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - r + 0.5f) * (x - r + 0.5f) + (y - r + 0.5f) * (y - r + 0.5f)) / r;
                    float a = d > 1f ? 0f : Mathf.SmoothStep(0f, 1f, 1f - d) * 0.7f + (d > 0.82f && d <= 1f ? 0.3f : 0f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                }
            tex.Apply(); tex.filterMode = FilterMode.Bilinear;
            _discSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _discSprite;
        }
    }
}
