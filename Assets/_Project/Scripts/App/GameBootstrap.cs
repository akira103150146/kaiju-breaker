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

        [Header("Camera Field Fit (keeps the whole vertical play field on-screen at any aspect)")]
        [Tooltip("Auto-size the orthographic camera so the full play field is always visible — fixes off-screen " +
                 "enemies on portrait phones vs a landscape-authored view.")]
        [SerializeField] private bool _fitCameraToField = true;

        [Tooltip("Half-width of the play field to guarantee visible (world units). Field is ±4; margin -> 4.5.")]
        [SerializeField] private float _fieldHalfWidth = 4.5f;

        [Tooltip("Half-height of the play field to guarantee visible (world units). Enemies spawn y=6, player to y=-6.")]
        [SerializeField] private float _fieldHalfHeight = 7.0f;

        private GameComposition _composition;
        private UnityTimeScaleControl _timeScale;
        private Vector3 _cameraBasePosition;
        private bool _hasCameraBase;
        private Texture2D _whiteTex;

        /// <summary>The live composed system graph (null until Awake, or if no ContentRegistry is assigned).</summary>
        public GameComposition Composition => _composition;

        /// <summary>The ContentRegistry this bootstrap composed from (for scene glue that needs config, e.g. WaveTiming).</summary>
        public ContentRegistry Content => _content;

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
            _composition = new GameComposition(_content, Application.persistentDataPath, _timeScale, new UnitySceneLoader());
            Debug.Log("[GameBootstrap] System graph composed (event bus + Meta/Difficulty/KaijuParts/Economy/GameFeel/Run).");
        }

        private void Update()
        {
            if (_composition == null) return;
            _composition.TickGameFeel(Time.unscaledDeltaTime); // feel on real time
            _composition.Stage?.Tick(Time.deltaTime);          // pre-boss lull on game time
            FitCameraToField();
            ApplyShake();
        }

        // Size the orthographic camera so the whole vertical play field fits on ANY aspect ratio: portrait phones
        // get a tall view, landscape PC pillarboxes the field. Guarantees enemies (spawn y=6) and the player
        // (down to y=-6, ±4 wide) are always on-screen + reachable — fixes off-screen enemies firing unanswerably.
        private void FitCameraToField()
        {
            if (!_fitCameraToField) return;
            var cam = _shakeCamera != null ? _shakeCamera : Camera.main;
            if (cam == null || !cam.orthographic || cam.aspect <= 0f) return;
            cam.orthographicSize = Mathf.Max(_fieldHalfHeight, _fieldHalfWidth / cam.aspect);
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

        // GameFeel flash adapter: a full-screen white overlay whose alpha follows FlashSystem (§D.3).
        // Capped at FlashMaxAlpha by the system, so the player's hitpoint stays identifiable at peak flash.
        private void OnGUI()
        {
            if (_composition == null) return;
            float alpha = _composition.Flash.Alpha;
            if (alpha <= 0.001f) return;
            if (_whiteTex == null)
            {
                _whiteTex = new Texture2D(1, 1);
                _whiteTex.SetPixel(0, 0, Color.white);
                _whiteTex.Apply();
            }
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _whiteTex);
            GUI.color = prev;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) _composition?.Meta.FlushSyncNow(); // mobile suspend safety net (ADR-0004)
        }

        private void OnApplicationQuit() => _composition?.Meta.FlushSyncNow();

        private void OnDestroy() => _composition?.Dispose();
    }
}
