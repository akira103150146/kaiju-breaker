using KaijuBreaker.Content;
using UnityEngine;

namespace KaijuBreaker.App
{
    /// <summary>
    /// Composition root MonoBehaviour (ADR-0005 §3). Lives on a persistent GameObject in the Bootstrap scene
    /// and builds the whole system graph once at startup via <see cref="GameComposition"/> — the pure-C#
    /// wiring — then supplies the Unity subsystem adapters and the per-frame drive loop. Holds NO game state
    /// and no static singletons; systems receive their dependencies through the composition and talk only via
    /// the event bus.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Tooltip("The ContentRegistry asset holding every tuning ScriptableObject. Assign in the Bootstrap scene.")]
        [SerializeField] private ContentRegistry _content;

        [Tooltip("Optional camera the screen-shake offset is applied to. Defaults to Camera.main.")]
        [SerializeField] private Camera _shakeCamera;

        private GameComposition _composition;
        private UnityTimeScaleControl _timeScale;
        private Vector3 _cameraBasePosition;
        private bool _hasCameraBase;

        /// <summary>The live composed system graph (null until Awake, or if no ContentRegistry is assigned).</summary>
        public GameComposition Composition => _composition;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (_content == null)
            {
                Debug.LogWarning("[GameBootstrap] No ContentRegistry assigned — system graph not built. " +
                                 "Assign the ContentRegistry asset in the Bootstrap scene (see NEXT-STEPS §D.5).");
                return;
            }

            _timeScale = new UnityTimeScaleControl();
            _composition = new GameComposition(_content, Application.persistentDataPath, _timeScale);
            Debug.Log("[GameBootstrap] System graph composed (event bus + Meta/Difficulty/KaijuParts/Economy/GameFeel/Run).");
        }

        private void Update()
        {
            if (_composition == null) return;
            _composition.TickGameFeel(Time.unscaledDeltaTime);
            ApplyShake();
        }

        private void ApplyShake()
        {
            var cam = _shakeCamera != null ? _shakeCamera : Camera.main;
            if (cam == null) return;

            if (!_hasCameraBase) { _cameraBasePosition = cam.transform.position; _hasCameraBase = true; }

            Vector2 offsetPx = _composition.Shake.ComputeOffset();
            // Convert the pixel offset to a small world nudge (orthographic): 1 unit ≈ camera height / pixel height.
            float unitsPerPixel = cam.orthographic && Screen.height > 0 ? (cam.orthographicSize * 2f) / Screen.height : 0.01f;
            var offset = new Vector3(offsetPx.x, offsetPx.y, 0f) * unitsPerPixel;
            cam.transform.position = _cameraBasePosition + offset;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) _composition?.Meta.FlushSyncNow(); // mobile suspend safety net (ADR-0004)
        }

        private void OnApplicationQuit() => _composition?.Meta.FlushSyncNow();

        private void OnDestroy() => _composition?.Dispose();
    }
}
