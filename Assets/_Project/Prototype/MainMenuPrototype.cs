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

        private void OnGUI()
        {
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 48, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold
            };
            var subStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };

            float w = Screen.width, h = Screen.height;
            GUI.Label(new Rect(0, h * 0.30f, w, 80), "殲獸戰機 / KAIJU BREAKER", titleStyle);
            GUI.Label(new Rect(0, h * 0.45f, w, 40), "— prototype —", subStyle);
            GUI.Label(new Rect(0, h * 0.60f, w, 40), "Press any key / tap to START", subStyle);
        }
    }
}
