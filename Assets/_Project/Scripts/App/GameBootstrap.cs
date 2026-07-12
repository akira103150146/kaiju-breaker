using KaijuBreaker.Content;
using UnityEngine;
using UnityEngine.UI;

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

        [Header("Playfield boundary markers")]
        [Tooltip("Draw faint vertical bars at the left/right play-field edges so the player can see the reachable " +
                 "area. Mobs that pass beyond the edge are despawned (EnemyController) so they can't fire from off-map.")]
        [SerializeField] private bool _showBoundaries = true;

        [Tooltip("World-X of each boundary bar (player is clamped to ±4; bars sit just outside).")]
        [SerializeField] private float _boundaryX = 4.3f;

        [Tooltip("Boundary bar colour (kept subtle so it frames the field without distracting).")]
        [SerializeField] private Color _boundaryColor = new Color(0.45f, 0.65f, 1f, 0.5f);

        [Header("Frame rate")]
        [Tooltip("Upper cap for the mobile frame rate. On mobile Unity defaults to 30 FPS unless targetFrameRate " +
                 "is set; we unlock it to the device's display refresh (60Hz→60, 120Hz→120) clamped to [60, this]. " +
                 "Desktop is left on its quality-level vSync and is not touched.")]
        [SerializeField] private int _maxFrameRate = 120;

        private GameComposition _composition;
        private SfxPlayer _sfx;
        private UnityTimeScaleControl _timeScale;
        private Vector3 _cameraBasePosition;
        private bool _hasCameraBase;
        private Canvas _flashCanvas;
        private Image _flashImage;

        /// <summary>The live composed system graph (null until Awake, or if no ContentRegistry is assigned).</summary>
        public GameComposition Composition => _composition;

        /// <summary>The ContentRegistry this bootstrap composed from (for scene glue that needs config, e.g. WaveTiming).</summary>
        public ContentRegistry Content => _content;

        /// <summary>Shared SFX sink (null until the graph is composed). Scene glue plays shoot/explode through it.</summary>
        public SfxPlayer Sfx => _sfx;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            CreateBoundaries();

            // Unlock the mobile frame rate. On mobile Unity caps to 30 FPS unless Application.targetFrameRate is
            // set (QualitySettings vSync is ignored there), which is why the APK felt locked at 30. Match the
            // device's display refresh — a 60Hz phone runs 60, a 120Hz phone 120 — clamped to [60, _maxFrameRate].
            // Desktop is deliberately untouched (its quality-level vSync governs the frame rate).
            if (Application.isMobilePlatform)
            {
                int hz = (int)System.Math.Round(Screen.currentResolution.refreshRateRatio.value);
                Application.targetFrameRate = Mathf.Clamp(hz > 0 ? hz : 60, 60, Mathf.Max(60, _maxFrameRate));
            }

            if (_content == null)
            {
                Debug.LogWarning("[GameBootstrap] No ContentRegistry assigned — system graph not built. " +
                                 "Assign the ContentRegistry asset in the Bootstrap scene (see NEXT-STEPS §D.5).");
                return;
            }

            _timeScale = new UnityTimeScaleControl();
            _composition = new GameComposition(_content, Application.persistentDataPath, _timeScale, new UnitySceneLoader());

            // Presentation-layer SFX sink, owned by the composition root (not a singleton). Subscribes to the bus.
            var sfxGo = new GameObject("SfxPlayer");
            sfxGo.transform.SetParent(transform, false);
            _sfx = sfxGo.AddComponent<SfxPlayer>();
            _sfx.Init(_composition.Bus);

            BuildFlashUi();

            Debug.Log("[GameBootstrap] System graph composed (event bus + Meta/Difficulty/KaijuParts/Economy/GameFeel/Run + SFX).");
        }

        // Two faint vertical bars at ±_boundaryX mark the reachable field edges. Persistent (they sit behind
        // gameplay at a low sorting order). Paired with EnemyController's sideways despawn so nothing fires
        // unanswerably from beyond the frame.
        private void CreateBoundaries()
        {
            if (!_showBoundaries) return;
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            for (int side = -1; side <= 1; side += 2)
            {
                var go = new GameObject(side < 0 ? "PlayfieldBoundaryL" : "PlayfieldBoundaryR");
                DontDestroyOnLoad(go);
                go.transform.position = new Vector3(side * _boundaryX, 0f, 0f);
                go.transform.localScale = new Vector3(0.08f, _fieldHalfHeight * 2f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = _boundaryColor;
                sr.sortingOrder = -10; // behind enemies/bullets
            }
        }

        private void Update()
        {
            if (_composition == null) return;
            _composition.TickGameFeel(Time.unscaledDeltaTime); // feel on real time
            _composition.Stage?.Tick(Time.deltaTime);          // pre-boss lull on game time
            FitCameraToField();
            ApplyShake();
            UpdateFlash();
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

        // GameFeel flash adapter (ADR-0006, UGUI): a full-screen white overlay whose alpha follows FlashSystem
        // (§D.3). Highest sorting order so it sits over the HUD; never blocks input (raycastTarget off). Capped at
        // FlashMaxAlpha by the system, so the player's hitpoint stays identifiable at peak flash.
        private void BuildFlashUi()
        {
            var go = new GameObject("FlashOverlay", typeof(RectTransform));
            DontDestroyOnLoad(go);
            _flashCanvas = go.AddComponent<Canvas>();
            _flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _flashCanvas.sortingOrder = 200; // above menus/HUD (100) and touch controls (90)

            var imgGo = new GameObject("Flash", typeof(RectTransform));
            imgGo.transform.SetParent(go.transform, false);
            _flashImage = imgGo.AddComponent<Image>();
            _flashImage.color = new Color(1f, 1f, 1f, 0f);
            _flashImage.raycastTarget = false;
            var rt = _flashImage.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private void UpdateFlash()
        {
            if (_flashImage == null) return;
            float alpha = _composition.Flash.Alpha;
            var c = _flashImage.color;
            if (c.a == alpha) return;
            c.a = alpha; _flashImage.color = c;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) _composition?.Meta.FlushSyncNow(); // mobile suspend safety net (ADR-0004)
        }

        private void OnApplicationQuit() => _composition?.Meta.FlushSyncNow();

        private void OnDestroy() => _composition?.Dispose();
    }
}
