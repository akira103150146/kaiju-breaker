using UnityEngine;
using UnityEngine.InputSystem;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>
    /// PC input provider (<see cref="IPlayerInput"/>): WASD/arrow keys drive the move axis, holding the left
    /// mouse button drags the ship toward the cursor, and Space (or right mouse) fires the secondary. Reads the
    /// new Input System device polling API (Active Input Handling = Input System Package); a full action map is
    /// the input epic's job. Mobile controls are a separate provider (Phase E), so this stays PC-only and simple.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KeyboardMouseInput : MonoBehaviour, IPlayerInput
    {
        [Tooltip("Camera used to unproject the mouse to world space. Defaults to Camera.main.")]
        [SerializeField] private Camera _camera;

        public Vector2 MoveAxis
        {
            get
            {
                float x = (KeyHeld(Key.D) || KeyHeld(Key.RightArrow) ? 1f : 0f) -
                          (KeyHeld(Key.A) || KeyHeld(Key.LeftArrow) ? 1f : 0f);
                float y = (KeyHeld(Key.W) || KeyHeld(Key.UpArrow) ? 1f : 0f) -
                          (KeyHeld(Key.S) || KeyHeld(Key.DownArrow) ? 1f : 0f);
                return new Vector2(x, y);
            }
        }

        public bool HasPointerTarget => Mouse.current != null && Mouse.current.leftButton.isPressed;

        public Vector2 PointerWorld
        {
            get
            {
                var cam = _camera != null ? _camera : Camera.main;
                if (cam == null || Mouse.current == null) return Vector2.zero;
                Vector2 sp = Mouse.current.position.ReadValue();
                Vector3 w = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, 0f));
                return new Vector2(w.x, w.y);
            }
        }

        public bool SecondaryPressedThisFrame =>
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);

        // Left mouse is drag-to-move here, so the charge input is a key (J or Z) on this provider.
        public bool PrimaryHeld => KeyHeld(Key.J) || KeyHeld(Key.Z);

        private static bool KeyHeld(Key k)
        {
            var kb = Keyboard.current;
            return kb != null && kb[k].isPressed;
        }
    }
}
