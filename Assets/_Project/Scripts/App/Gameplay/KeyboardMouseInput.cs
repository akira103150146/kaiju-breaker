using UnityEngine;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>
    /// PC input provider (<see cref="IPlayerInput"/>): WASD/arrow keys drive the move axis, holding the left
    /// mouse button drags the ship toward the cursor, and Space (or right mouse) fires the secondary. Uses the
    /// legacy Input class (project has Active Input Handling = Both); the new Input System action map is the
    /// input epic's job. Mobile controls are a separate provider (Phase E), so this stays PC-only and simple.
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
                float x = (Key(KeyCode.D) || Key(KeyCode.RightArrow) ? 1f : 0f) -
                          (Key(KeyCode.A) || Key(KeyCode.LeftArrow) ? 1f : 0f);
                float y = (Key(KeyCode.W) || Key(KeyCode.UpArrow) ? 1f : 0f) -
                          (Key(KeyCode.S) || Key(KeyCode.DownArrow) ? 1f : 0f);
                return new Vector2(x, y);
            }
        }

        public bool HasPointerTarget => Input.GetMouseButton(0);

        public Vector2 PointerWorld
        {
            get
            {
                var cam = _camera != null ? _camera : Camera.main;
                if (cam == null) return Vector2.zero;
                Vector3 w = cam.ScreenToWorldPoint(Input.mousePosition);
                return new Vector2(w.x, w.y);
            }
        }

        public bool SecondaryPressedThisFrame =>
            Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1);

        // Left mouse is drag-to-move here, so the charge input is a key (J or Z) on this provider.
        public bool PrimaryHeld => Key(KeyCode.J) || Key(KeyCode.Z);

        private static bool Key(KeyCode k) => Input.GetKey(k);
    }
}
