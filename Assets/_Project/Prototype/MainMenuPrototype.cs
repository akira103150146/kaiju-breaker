using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace KaijuBreaker.Prototype
{
    /// <summary>
    /// THROWAWAY prototype main menu. Draws a title + prompt with IMGUI and loads the stage
    /// scene on any key / click / tap (also exposed as <see cref="StartGame"/> for a UGUI button).
    /// Delete the whole Prototype folder once the real MetaHub UI (hud-ui epic) exists.
    /// </summary>
    public sealed class MainMenuPrototype : MonoBehaviour
    {
        [SerializeField] private string _stageScene = "Stage01Prototype";

        // Optional pixel bitmap font (art-bible §7.2). Assign the same font asset as the stage HUD.
        // See design/assets/specs/hud-ui-assets.md for the recommended fonts + import settings.
        [SerializeField] private Font _pixelFont;

        private Texture2D _bgTex;

        private static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        private void Update()
        {
            bool pressed =
                (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);

            if (pressed) StartGame();
        }

        /// <summary>Load the stage scene (hook a UGUI Button's onClick here too).</summary>
        public void StartGame() => SceneManager.LoadScene(_stageScene);

        private GUIStyle Style(int size, Color color, FontStyle fs = FontStyle.Normal)
        {
            // Ark Pixel 16px is crisp ONLY at integer multiples (16/32/48). Snap the requested size to
            // the nearest multiple so the pixel font never renders blurry (hud-ui-assets §1.2). Applied
            // only when the pixel font is assigned; the built-in fallback keeps the original ad-hoc sizes.
            if (_pixelFont != null) size = Mathf.Max(16, Mathf.RoundToInt(size / 16f) * 16);
            var s = new GUIStyle(GUI.skin.label)
            { fontSize = size, alignment = TextAnchor.MiddleCenter, fontStyle = fs, wordWrap = false };
            if (_pixelFont != null) s.font = _pixelFont;      // art-bible §7.2 pixel font
            s.normal.textColor = color;
            return s;
        }

        private void OnGUI()
        {
            // Deep-blue-black arcade backdrop (art-bible §7.5 Meta background #0A0E1A).
            if (_bgTex == null) { _bgTex = new Texture2D(1, 1); _bgTex.SetPixel(0, 0, Hex("#0A0E1A")); _bgTex.Apply(); }
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _bgTex);

            float w = Screen.width, h = Screen.height;
            GUI.Label(new Rect(0, h * 0.30f, w, 80), "殲獸戰機 / KAIJU BREAKER", Style(48, Hex("#40F8FF"), FontStyle.Bold)); // cold-cyan title
            GUI.Label(new Rect(0, h * 0.45f, w, 40), "— prototype —", Style(22, Hex("#8AA0B0")));
            GUI.Label(new Rect(0, h * 0.60f, w, 40), "Press any key / tap to START", Style(22, Hex("#00C0E0")));
        }
    }
}
