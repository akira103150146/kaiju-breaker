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
        [Tooltip("Show the on-screen joystick + fire button. Leave on for touch; harmless on PC (mouse-usable).")]
        [SerializeField] private bool _showTouchControls = true;

        private Vector2 _joyBase, _fireCenter;
        private float _joyRadius, _fireRadius;
        private Vector2 _joyAxis;   // computed each frame
        private bool _secondaryEdge; // fire pressed this frame (touch began in button / key down)

        private void Update()
        {
            LayoutControls();
            _joyAxis = Vector2.zero;
            _secondaryEdge = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1);

            // Touch first (mobile); fall back to mouse (editor/PC) so the same controls verify with a cursor.
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (InCircle(t.position, _joyBase, _joyRadius * 2.2f)) _joyAxis = AxisFrom(t.position);
                    if (t.phase == TouchPhase.Began && InCircle(t.position, _fireCenter, _fireRadius)) _secondaryEdge = true;
                }
            }
            else if (Input.GetMouseButton(0))
            {
                Vector2 m = Input.mousePosition;
                if (InCircle(m, _joyBase, _joyRadius * 2.2f)) _joyAxis = AxisFrom(m);
            }
            if (Input.GetMouseButtonDown(0) && InCircle(Input.mousePosition, _fireCenter, _fireRadius)) _secondaryEdge = true;
        }

        private void LayoutControls()
        {
            float w = Screen.width, h = Screen.height;
            _joyRadius = Mathf.Min(w, h) * 0.10f;
            _fireRadius = _joyRadius * 0.9f;
            _joyBase = new Vector2(w * 0.16f, h * 0.20f);
            _fireCenter = new Vector2(w * 0.84f, h * 0.20f);
        }

        private Vector2 AxisFrom(Vector2 screenPos)
        {
            Vector2 v = (screenPos - _joyBase) / _joyRadius;
            return v.magnitude > 1f ? v.normalized : v;
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
        public bool SecondaryPressedThisFrame => _secondaryEdge;

        private static bool Key(KeyCode k) => Input.GetKey(k);

        private Texture2D _tex;
        private void OnGUI()
        {
            if (!_showTouchControls) return;
            if (_tex == null) { _tex = new Texture2D(1, 1); _tex.SetPixel(0, 0, Color.white); _tex.Apply(); }
            // IMGUI y is top-down; screen coords are bottom-up.
            DrawRing(_joyBase, _joyRadius, new Color(0.4f, 0.85f, 1f, 0.25f));
            DrawRing(_joyBase + _joyAxis * _joyRadius, _joyRadius * 0.45f, new Color(0.5f, 0.95f, 1f, 0.5f));
            DrawRing(_fireCenter, _fireRadius, new Color(1f, 0.55f, 0.35f, 0.35f));
        }

        private void DrawRing(Vector2 screenCenter, float r, Color c)
        {
            var prev = GUI.color; GUI.color = c;
            GUI.DrawTexture(new Rect(screenCenter.x - r, Screen.height - screenCenter.y - r, r * 2f, r * 2f), _tex);
            GUI.color = prev;
        }
    }
}
