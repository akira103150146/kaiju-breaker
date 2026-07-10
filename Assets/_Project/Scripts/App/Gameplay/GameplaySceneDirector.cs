using System;
using System.Collections.Generic;
using KaijuBreaker.Core;
using KaijuBreaker.Meta;
using KaijuBreaker.Stage;
using UnityEngine;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>
    /// Scene-level orchestrator that turns the composed system graph into a playable run: it spawns the player,
    /// kicks off the run (publishes <see cref="LoadoutConfirmed"/> so <see cref="RunController"/> enters STAGE
    /// and <see cref="StageDirector"/> builds the segment sequence), then drives that sequence's waves through a
    /// <see cref="SegmentSequenceRunner"/> / <see cref="WaveSpawner"/>. It bridges the player's fate to the run:
    /// player death pauses the run (defeat), and phase changes retarget the player's clamp band.
    ///
    /// <para>All game rules live in the injected systems (this only wires scene objects to them, ADR-0005). The
    /// STAGE→BOSS handoff and a proper defeat→RESULTS transition are wired in later phases; for now clearing the
    /// waves and losing on 0 HP are surfaced as run-flow signals.</para>
    /// </summary>
    [DefaultExecutionOrder(-500)] // after GameBootstrap (-1000) builds the composition, before default scripts
    public sealed class GameplaySceneDirector : MonoBehaviour
    {
        [Tooltip("The bootstrap that owns the composed system graph. Defaults to the scene's GameBootstrap.")]
        [SerializeField] private GameBootstrap _bootstrap;

        [Tooltip("Player ship prefab (PlayerShip + input + weapon controller). Required.")]
        [SerializeField] private PlayerShip _playerPrefab;

        [Tooltip("Where the player spawns (world units, lower band).")]
        [SerializeField] private Vector3 _playerSpawn = new Vector3(0f, -4.5f, 0f);

        [Tooltip("Trash-enemy prefab the wave spawner instantiates. Required.")]
        [SerializeField] private GameObject _enemyPrefab;

        [Tooltip("Pooled enemy-bullet prefab (EnemyBullet). Enemies fire from a shared pool built at run start.")]
        [SerializeField] private EnemyBullet _enemyBulletPrefab;

        [Tooltip("Boss encounter controller (hidden until the 道中 clears). Optional — no boss if unassigned.")]
        [SerializeField] private BossController _bossController;

        [Tooltip("In-run power-up item prefab (PowerUpItem) that enemies drop. Optional — no drops if unassigned.")]
        [SerializeField] private PowerUpItem _powerUpPrefab;

        [Tooltip("Dwell-cycle weapon pod prefab (WeaponPodController) that elites drop. Optional.")]
        [SerializeField] private WeaponPodController _weaponPodPrefab;

        [Tooltip("Skip the title screen and begin immediately on Play (e.g. for tests).")]
        [SerializeField] private bool _autoStart = false;

        [Tooltip("Ark Pixel font for the placeholder UI (menus/HUD/results). Assign the project's Ark Pixel TTF.")]
        [SerializeField] private Font _uiFont;

        private GameComposition _comp;
        private PlayerShip _player;
        private PlayerWeaponController _playerWeapon;
        private EnemyBulletPool _bulletPool;
        private SegmentSequenceRunner _waveRunner;
        private Action<RunStateChanged> _onRunStateChanged;
        private Action<WeaponPodGrabbed> _onWeaponPodGrabbed;
        private bool _defeated;
        private bool _showTitle;
        private bool _showBossSelect;
        private bool _showUpgrades;
        private UtilityUpgrades _utility;
        private int _selBossIndex;
        private bool _showLoadout;
        private bool _skipToBoss; // DEV: skip the 道中 wave sequence and go straight to the boss
        private WeaponId _selPrimary = WeaponId.L1;
        private WeaponId _selSecondary = WeaponId.M1;
        private DifficultyTier _selDifficulty = DifficultyTier.D1;
        private RunState _runState = RunState.Loadout;

        /// <summary>The spawned player ship (null before BeginRun).</summary>
        public PlayerShip Player => _player;

        private void Start()
        {
            if (_bootstrap == null) _bootstrap = FindFirstObjectByType<GameBootstrap>();
            _comp = _bootstrap != null ? _bootstrap.Composition : null;
            if (_comp == null)
            {
                Debug.LogError("[GameplaySceneDirector] No composed system graph — assign a GameBootstrap with a ContentRegistry.");
                return;
            }

            _onRunStateChanged = OnRunStateChanged;
            _comp.Bus.Subscribe(_onRunStateChanged);
            _onWeaponPodGrabbed = evt => _playerWeapon?.SetWeapon(evt.Weapon); // pod pickup switches weapon type
            _comp.Bus.Subscribe(_onWeaponPodGrabbed);
            _utility = new UtilityUpgrades(_comp.Meta); // meta utility upgrades (fire rate / drop rate)

            SpawnPlayer();
            _playerWeapon?.SetFiring(false); // hold fire on the title screen
            if (_autoStart) BeginRun();
            else _showTitle = true;
        }

        private void Update()
        {
            if (_showTitle && (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.touchCount > 0))
                StartFromTitle();
        }

        private void StartFromTitle()
        {
            if (!_showTitle) return;
            _showTitle = false;
            _showBossSelect = true; // title → boss select (MMX-style hub) → loadout → play
        }

        private void ConfirmBossSelect()
        {
            if (!BossUnlocked[_selBossIndex]) return; // locked target — stay on the select screen
            _showBossSelect = false;
            _showLoadout = true;
            // (Only CARAPEX is wired to a KaijuDef today; future targets set the BossController's boss here.)
        }

        private void ConfirmLoadout()
        {
            _showLoadout = false;
            _comp?.Difficulty.SetTier(_selDifficulty); // difficulty is real (scales enemy count / bullet density)
            BeginRun();
        }

        private void OnDestroy()
        {
            if (_comp == null) return;
            if (_onRunStateChanged != null) _comp.Bus.Unsubscribe(_onRunStateChanged);
            if (_onWeaponPodGrabbed != null) _comp.Bus.Unsubscribe(_onWeaponPodGrabbed);
        }

        private void SpawnPlayer()
        {
            if (_playerPrefab == null) { Debug.LogError("[GameplaySceneDirector] No player prefab assigned."); return; }
            _player = Instantiate(_playerPrefab, _playerSpawn, Quaternion.identity);
            _player.ResetShip();
            _player.Died += OnPlayerDied;
            _playerWeapon = _player.GetComponent<PlayerWeaponController>();
        }

        /// <summary>Start a run: enter STAGE, build the sequence, and begin spawning waves.</summary>
        public void BeginRun()
        {
            if (_comp?.Run == null) return;

            // Publishing LoadoutConfirmed drives BOTH RunController (→STAGE) and StageDirector (builds the
            // sequence). Both handlers run synchronously inside Publish, so CurrentSequence is ready on return.
            _comp.Bus.Publish(new LoadoutConfirmed(_selPrimary, _selSecondary, _selDifficulty));

            var sequence = _comp.Stage?.CurrentSequence;
            if (sequence == null)
            {
                Debug.LogWarning("[GameplaySceneDirector] No stage sequence (ContentRegistry missing Stage config?) — no waves.");
                return;
            }

            var content = _bootstrap.Content;
            _playerWeapon?.ResetArsenal(_selPrimary, _selSecondary, _utility != null ? _utility.StartPowerLevel : 0); // Void-core head-start firepower
            _playerWeapon?.SetFireIntervalMult(_utility != null ? _utility.FireIntervalMult : 1f); // meta faster-fire
            _player?.SetUtilityMultipliers(
                _utility != null ? _utility.MoveSpeedMult : 1f,
                _utility != null ? _utility.IFrameMult : 1f); // Ember move-speed / Abyss i-frames
            _playerWeapon?.SetSecondaryCooldownMult(_utility != null ? _utility.SecondaryCooldownMult : 1f); // Swarm-core faster missiles
            PowerUpItem.MagnetTarget = _player != null ? _player.transform : null; // Crystal-core pickup magnet
            PowerUpItem.MagnetRadius = _utility != null ? (_utility.MagnetRadiusMult - 1f) * 2f : 0f; // 0 at level 0

            // Shared enemy-bullet pool + the drop callback (enemies roll in-run power-ups on death).
            _bulletPool = null;
            if (_enemyBulletPrefab != null)
            {
                var poolGo = new GameObject("EnemyBulletPool");
                _bulletPool = poolGo.AddComponent<EnemyBulletPool>();
                _bulletPool.Configure(_enemyBulletPrefab);
            }
            var combat = new EnemyCombatContext(_bulletPool, _player != null ? _player.transform : null, SpawnDrop,
                                                _comp != null && _comp.Difficulty != null ? _comp.Difficulty.BulletDensityMult : 1f);

            if (_skipToBoss)
            {
                // DEV shortcut: no wave runner — jump straight to the boss (pool/combat already set up for it).
                Debug.Log("[GameplaySceneDirector] DEV: skipping 道中 — straight to boss.");
                OnWavesCleared();
            }
            else
            {
                var runnerGo = new GameObject("WaveRunner");
                _waveRunner = runnerGo.AddComponent<SegmentSequenceRunner>();
                _waveRunner.Run(sequence, _comp.Difficulty, content.WaveTiming, _enemyPrefab,
                                new System.Random(), OnWavesCleared, combat);
                Debug.Log($"[GameplaySceneDirector] Run started — {sequence.EscalatingSegments.Count} segments queued.");
            }

            _playerWeapon?.SetFiring(true);
        }

        // Enemy death → roll an in-run power-up drop. Rates are placeholder (a meta upgrade tunes drop rate).
        private void SpawnDrop(Vector3 pos, bool isElite)
        {
            if (_powerUpPrefab == null) return;
            if (isElite)
            {
                // Elites yield a dwell-cycle weapon pod (the pool-typed target of §F.1) + a firepower chip.
                SpawnPod(pos);
                Spawn(pos + Vector3.right * 0.5f, PowerUpKind.Power);
                return;
            }
            float mult = _utility != null ? _utility.DropRateMult : 1f; // meta drop-rate upgrade
            float r = UnityEngine.Random.value;
            if (r < 0.16f * mult) Spawn(pos, PowerUpKind.Power);
            else if (r < 0.26f * mult) Spawn(pos, PowerUpKind.Missile);
        }

        private void Spawn(Vector3 pos, PowerUpKind kind)
        {
            var item = Instantiate(_powerUpPrefab, pos, Quaternion.identity);
            item.Init(kind);
        }

        // Elite drop: a dwell-cycle weapon pod that descends into the reachable band, cycles the pool (so the
        // player waits for the weapon they want), and on pickup switches the weapon type (WeaponPodGrabbed).
        private void SpawnPod(Vector3 pos)
        {
            if (_weaponPodPrefab == null || _comp == null || _bootstrap.Content?.PodDrop == null)
            {
                if (_powerUpPrefab != null) Spawn(pos, PowerUpKind.WeaponLaser); // fallback: simple switch item
                return;
            }
            bool laser = UnityEngine.Random.value < 0.5f;
            var poolL = new List<WeaponId> { WeaponId.L1, WeaponId.L2, WeaponId.L3, WeaponId.L4 };
            var poolM = new List<WeaponId> { WeaponId.M1, WeaponId.M2, WeaponId.M3, WeaponId.M4 };
            var pod = Instantiate(_weaponPodPrefab, pos, Quaternion.identity);
            pod.Init(_comp.Bus, _bootstrap.Content.PodDrop, laser ? poolL : poolM,
                     laser ? PodType.Primary : PodType.Secondary, bandMinY: -2f, bandMaxY: 1f);
        }

        private void OnWavesCleared()
        {
            Debug.Log("[GameplaySceneDirector] 道中 CLEAR — entering boss fight.");
            if (_bossController != null)
                _bossController.BeginBossFight(_comp, _selBossIndex, _bulletPool,
                                               _player != null ? _player.transform : null);
            else Debug.Log("[GameplaySceneDirector] No BossController assigned — run ends after 道中.");
        }

        private void OnRunStateChanged(RunStateChanged evt)
        {
            _runState = evt.To;
            if (_player != null) _player.SetBossPhase(evt.To == RunState.Boss);
            if (evt.To == RunState.Results) { _showResults = true; _resultWin = !_defeated; }
        }

        private void OnPlayerDied()
        {
            if (_defeated) return;
            _defeated = true;
            _playerWeapon?.SetFiring(false);
            _comp?.Run.Defeat(); // STAGE/BOSS → RESULTS (defeat), settles as non-full-clear
            Debug.Log("[GameplaySceneDirector] DEFEAT — player HP reached 0.");
        }

        private bool _showResults;
        private bool _resultWin;

        // Placeholder IMGUI front-end (title / HUD / results), themed via GameUiSkin (Ark Pixel + cold palette).
        // A UGUI + TMP pass is the ADR-0006 follow-up.
        private void OnGUI()
        {
            GameUiSkin.EnsureBuilt(_uiFont);
            float s = Mathf.Clamp(Screen.dpi > 0 ? Screen.dpi / 96f : 1f, 1f, 3f);
            var prev = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
            float w = Screen.width / s, h = Screen.height / s;

            if (_showTitle) DrawTitle(w, h);
            else if (_showUpgrades) DrawUpgrades(w, h);
            else if (_showBossSelect) DrawBossSelect(w, h);
            else if (_showLoadout) DrawLoadout(w, h);
            else if (_showResults) DrawResults(w, h);
            else DrawHud(w, h, s);

            GUI.matrix = prev;
        }

        private static readonly string[] PrimaryLabels = { "L1 散波", "L2 集束", "L3 波動", "L4 穿透" };
        private static readonly string[] SecondaryLabels = { "M1 追蹤", "M2 蜂群", "M3 魚雷", "M4 叢集" };
        private static readonly string[] DiffLabels = { "D1", "D2", "D3", "D4" };

        private void DrawLoadout(float w, float h)
        {
            var panel = new Rect(w * 0.5f - 216f, h * 0.5f - 190f, 432f, 360f);
            GUI.Box(panel, GUIContent.none, GameUiSkin.PanelStyle);
            GUI.Label(new Rect(panel.x, panel.y + 16f, panel.width, 34f), "選擇裝備  LOADOUT", GameUiSkin.HeadingStyle);

            Row(panel, 62f, "主武器 · 雷射", PrimaryLabels, (int)_selPrimary, i => _selPrimary = (WeaponId)i);
            Row(panel, 138f, "副武器 · 飛彈", SecondaryLabels, (int)_selSecondary - 4, i => _selSecondary = (WeaponId)(i + 4));
            Row(panel, 214f, "難度 · 彈幕密度", DiffLabels, (int)_selDifficulty, i => _selDifficulty = (DifficultyTier)i);

            // DEV: skip the 道中 and drop straight into the boss fight (fast boss iteration).
            var skip = new Rect(panel.x + 22f, panel.y + 266f, panel.width - 44f, 26f);
            var skipStyle = _skipToBoss ? GameUiSkin.SelectedButtonStyle : GameUiSkin.ButtonStyle;
            if (GUI.Button(skip, (_skipToBoss ? "☑" : "☐") + "  跳過道中 · 直達 BOSS (測試)", skipStyle)) _skipToBoss = !_skipToBoss;

            var start = new Rect(panel.x + panel.width * 0.5f - 95f, panel.y + 300f, 190f, 44f);
            if (GUI.Button(start, "出擊  START", GameUiSkin.ButtonStyle)) ConfirmLoadout();
        }

        private void Row(Rect panel, float y, string title, string[] labels, int selected, Action<int> onPick)
        {
            GUI.Label(new Rect(panel.x + 22f, panel.y + y, panel.width - 44f, 16f), title, GameUiSkin.SmallStyle);
            float bw = (panel.width - 44f - 18f) / 4f;
            for (int i = 0; i < labels.Length; i++)
            {
                var r = new Rect(panel.x + 22f + i * (bw + 6f), panel.y + y + 20f, bw, 40f);
                var style = i == selected ? GameUiSkin.SelectedButtonStyle : GameUiSkin.ButtonStyle;
                if (GUI.Button(r, labels[i], style)) onPick(i);
            }
        }

        private void DrawTitle(float w, float h)
        {
            var panel = new Rect(w * 0.5f - 190f, h * 0.32f, 380f, 210f);
            GUI.Box(panel, GUIContent.none, GameUiSkin.PanelStyle);
            GUI.Label(new Rect(panel.x, panel.y + 34f, panel.width, 50f), "殲獸戰機", GameUiSkin.TitleStyle);
            GUI.Label(new Rect(panel.x, panel.y + 92f, panel.width, 28f), "KAIJU BREAKER", GameUiSkin.HeadingStyle);
            bool blink = Mathf.Repeat(Time.unscaledTime, 1f) < 0.6f;
            if (blink) GUI.Label(new Rect(panel.x, panel.y + 150f, panel.width, 30f), "點擊開始  ·  TAP TO START", GameUiSkin.SmallStyle);
        }

        private void DrawHud(float w, float h, float s)
        {
            // HP bar (bottom-centre, above the touch controls) + phase tag (top-centre).
            if (_player != null)
            {
                float frac = _player.MaxHp > 0 ? (float)_player.Hp / _player.MaxHp : 0f;
                var bar = new Rect(w * 0.5f - 90f, h - 44f, 180f, 12f);
                Color fill = frac > 0.35f ? GameUiSkin.Cyan : GameUiSkin.Danger;
                GameUiSkin.Bar(bar, frac, fill);
                GUI.Label(new Rect(bar.x, bar.y - 18f, bar.width, 16f), "HP " + _player.Hp + " / " + _player.MaxHp, GameUiSkin.SmallStyle);

                // In-run arsenal readout (Raiden-style firepower / missile levels + weapon type).
                if (_playerWeapon != null)
                {
                    string arsenal = "火力 Lv" + _playerWeapon.WeaponPower + "   飛彈 Lv" + _playerWeapon.MissilePower +
                                     "   " + _playerWeapon.PrimaryType + "/" + _playerWeapon.SecondaryType;
                    var ar = new Rect(w * 0.5f - 130f, bar.y - 38f, 260f, 16f);
                    GUI.Label(ar, arsenal, GameUiSkin.SmallStyle);
                }
            }
            DrawPodLabels(s);

            string phase = _runState == RunState.Boss ? "頭目戰  BOSS" : _runState == RunState.Stage ? "道中  STAGE" : "";
            if (phase.Length > 0)
            {
                var tag = new Rect(w * 0.5f - 90f, 10f, 180f, 24f);
                GUI.Box(tag, GUIContent.none, GameUiSkin.PanelStyle);
                var st = _runState == RunState.Boss ? GameUiSkin.HeadingStyle : GameUiSkin.LabelStyle;
                var pc = st.normal.textColor; st.normal.textColor = _runState == RunState.Boss ? GameUiSkin.Warm : GameUiSkin.Ink;
                GUI.Label(tag, phase, st); st.normal.textColor = pc;
            }
        }

        // Float each active weapon pod's currently-offered weapon code over it (so the player can wait for
        // the one they want), tinted by pool (laser = cyan, missile = gold).
        private void DrawPodLabels(float s)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var pods = FindObjectsByType<WeaponPodController>(FindObjectsSortMode.None);
            for (int i = 0; i < pods.Length; i++)
            {
                var pod = pods[i];
                Vector3 sp = cam.WorldToScreenPoint(pod.transform.position);
                if (sp.z < 0f) continue;
                var box = new Rect(sp.x / s - 26f, (Screen.height - sp.y) / s - 34f, 52f, 20f);
                GUI.Box(box, GUIContent.none, GameUiSkin.PanelStyle);
                var st = GameUiSkin.SmallStyle;
                var pc = st.normal.textColor;
                st.normal.textColor = pod.PodType == PodType.Primary ? GameUiSkin.Cyan : new Color(1f, 0.82f, 0.35f);
                GUI.Label(box, pod.CurrentWeapon.ToString(), st);
                st.normal.textColor = pc;
            }
        }

        private static readonly string[] BossNames = { "甲殼獸", "利刃獸", "雷龍", "巢母", "稜殼獸", "潮顎", "燼使", "虛尖" };
        private static readonly string[] BossCodes = { "CARAPEX", "LACERA", "VOLTWYRM", "BROODCORE", "PRISMSHELL", "TIDEMAW", "EMBERWING", "NULLSPIRE" };
        private static readonly bool[] BossUnlocked = { true, true, true, true, true, true, true, true };
        private static readonly Color[] BossColors =
        {
            new Color(1f, 0.40f, 0.34f), new Color(0.72f, 0.45f, 1f), new Color(0.40f, 0.90f, 1f),
            new Color(1f, 0.70f, 0.30f), new Color(0.55f, 0.85f, 1f), new Color(0.30f, 0.72f, 0.72f),
            new Color(1f, 0.50f, 0.25f), new Color(0.62f, 0.40f, 0.85f)
        };

        // MMX-style target-select hub: pick which kaiju to hunt before choosing a loadout.
        private void DrawBossSelect(float w, float h)
        {
            // Dynamic grid (MMX-style): up to 4 per row, as many rows as the roster needs.
            int count = BossNames.Length;
            int cols = Mathf.Min(4, count);
            int rows = (count + cols - 1) / cols;
            float cw = 118f, ch = 142f, gap = 12f;
            float pw = cols * cw + (cols - 1) * gap + 40f;
            float ph2 = 66f + rows * (ch + gap) + 52f;
            var panel = new Rect(w * 0.5f - pw * 0.5f, h * 0.5f - ph2 * 0.5f, pw, ph2);
            GUI.Box(panel, GUIContent.none, GameUiSkin.PanelStyle);
            GUI.Label(new Rect(panel.x, panel.y + 14f, panel.width, 30f), "選擇獵物  SELECT TARGET", GameUiSkin.HeadingStyle);

            float gx = panel.x + (panel.width - (cw * cols + gap * (cols - 1))) * 0.5f;
            float gy = panel.y + 52f;
            for (int i = 0; i < count; i++)
            {
                var cell = new Rect(gx + (i % cols) * (cw + gap), gy + (i / cols) * (ch + gap), cw, ch);
                bool unlocked = BossUnlocked[i];
                var style = !unlocked ? GameUiSkin.ButtonStyle
                          : i == _selBossIndex ? GameUiSkin.SelectedButtonStyle : GameUiSkin.ButtonStyle;
                if (GUI.Button(cell, GUIContent.none, style) && unlocked) _selBossIndex = i;

                // portrait block (theme colour; greyed when locked) + names, drawn over the cell.
                var port = new Rect(cell.x + 16f, cell.y + 12f, cw - 32f, 72f);
                var pc = GUI.color;
                GUI.color = unlocked ? BossColors[i] : new Color(0.28f, 0.30f, 0.36f, 1f);
                GUI.DrawTexture(port, GameUiSkin.White);
                GUI.color = pc;
                GUI.Label(new Rect(cell.x, cell.y + 90f, cw, 20f), BossNames[i], GameUiSkin.LabelStyle);
                GUI.Label(new Rect(cell.x, cell.y + 110f, cw, 16f), unlocked ? BossCodes[i] : "開發中 LOCKED", GameUiSkin.SmallStyle);
            }

            var confirm = new Rect(panel.x + panel.width * 0.5f - 95f, panel.yMax - 46f, 190f, 40f);
            if (GUI.Button(confirm, "確定  ▶  裝備", GameUiSkin.ButtonStyle)) ConfirmBossSelect();

            var upg = new Rect(panel.xMax - 118f, panel.y + 12f, 104f, 28f);
            if (GUI.Button(upg, "強化 ⚙", GameUiSkin.ButtonStyle)) { _showBossSelect = false; _showUpgrades = true; }
        }

        // Meta utility upgrade shop (spend shards on faster fire / higher drop rate — killing power is in-run).
        // Meta utility upgrade shop. Shards buy faster fire / higher drop rate; the five theme cores buy the
        // mecha/utility tracks. Killing power stays in-run. (IMGUI placeholder — UGUI per ADR-0006 later.)
        private void DrawUpgrades(float w, float h)
        {
            var panel = new Rect(w * 0.5f - 220f, h * 0.5f - 245f, 440f, 490f);
            GUI.Box(panel, GUIContent.none, GameUiSkin.PanelStyle);
            GUI.Label(new Rect(panel.x, panel.y + 14f, panel.width, 30f), "強化 · UPGRADE", GameUiSkin.HeadingStyle);

            if (_utility != null)
            {
                GUI.Label(new Rect(panel.x + 22f, panel.y + 48f, panel.width - 44f, 16f), "碎片 Shards：" + _utility.Shards, GameUiSkin.SmallStyle);
                UpgradeRow(panel, 70f, "開火速度  FIRE RATE", _utility.FireRateLevel, _utility.CostFor(_utility.FireRateLevel), _utility.BuyFireRate);
                UpgradeRow(panel, 124f, "掉落率  DROP RATE", _utility.DropRateLevel, _utility.CostFor(_utility.DropRateLevel), _utility.BuyDropRate);

                GUI.Label(new Rect(panel.x + 22f, panel.y + 182f, panel.width - 44f, 16f), "頭目核心 · CORES", GameUiSkin.SmallStyle);
                CoreRow(panel, 202f, "副武射速  M-RATE", _utility.AmmoLevel, _utility.CoreCostFor(_utility.AmmoLevel), _utility.CoreBalance(MaterialId.CoreSwarm), _utility.BuyAmmo);
                CoreRow(panel, 250f, "道具吸取  MAGNET", _utility.MagnetLevel, _utility.CoreCostFor(_utility.MagnetLevel), _utility.CoreBalance(MaterialId.CoreCrystal), _utility.BuyMagnet);
                CoreRow(panel, 298f, "無敵時間  I-FRAME", _utility.IFrameLevel, _utility.CoreCostFor(_utility.IFrameLevel), _utility.CoreBalance(MaterialId.CoreAbyss), _utility.BuyIFrame);
                CoreRow(panel, 346f, "移動速度  SPEED", _utility.SpeedLevel, _utility.CoreCostFor(_utility.SpeedLevel), _utility.CoreBalance(MaterialId.CoreEmber), _utility.BuySpeed);
                CoreRow(panel, 394f, "開場火力  HEAD-START", _utility.HeadStartLevel, _utility.CoreCostFor(_utility.HeadStartLevel), _utility.CoreBalance(MaterialId.CoreVoid), _utility.BuyHeadStart);
            }

            var back = new Rect(panel.x + panel.width * 0.5f - 90f, panel.yMax - 42f, 180f, 34f);
            if (GUI.Button(back, "返回 BACK", GameUiSkin.ButtonStyle)) { _showUpgrades = false; _showBossSelect = true; }
        }

        // One core-funded utility row: title, level/owned-core readout, and a buy button showing the core cost.
        private void CoreRow(Rect panel, float y, string title, int level, int cost, int have, System.Func<bool> buy)
        {
            GUI.Label(new Rect(panel.x + 22f, panel.y + y, 230f, 18f), title, GameUiSkin.LabelStyle);
            GUI.Label(new Rect(panel.x + 22f, panel.y + y + 19f, 250f, 14f), "Lv " + level + "/" + UtilityUpgrades.MaxCoreLevel + "   核心 " + have, GameUiSkin.SmallStyle);
            var btn = new Rect(panel.xMax - 148f, panel.y + y + 2f, 128f, 34f);
            if (level >= UtilityUpgrades.MaxCoreLevel) GUI.Label(btn, "MAX", GameUiSkin.SmallStyle);
            else if (GUI.Button(btn, "升級 (" + cost + ")", GameUiSkin.ButtonStyle)) buy();
        }

        private void UpgradeRow(Rect panel, float y, string title, int level, int cost, System.Func<bool> buy)
        {
            GUI.Label(new Rect(panel.x + 22f, panel.y + y, 240f, 20f), title, GameUiSkin.LabelStyle);
            GUI.Label(new Rect(panel.x + 22f, panel.y + y + 22f, 200f, 16f), "Lv " + level + " / " + UtilityUpgrades.MaxLevel, GameUiSkin.SmallStyle);
            var btn = new Rect(panel.xMax - 150f, panel.y + y + 4f, 130f, 40f);
            if (level >= UtilityUpgrades.MaxLevel) GUI.Label(btn, "MAX", GameUiSkin.SmallStyle);
            else if (GUI.Button(btn, "升級 (" + cost + ")", GameUiSkin.ButtonStyle)) buy();
        }

        private void DrawResults(float w, float h)
        {
            var panel = new Rect(w * 0.5f - 170f, h * 0.5f - 110f, 340f, 220f);
            GUI.Box(panel, GUIContent.none, GameUiSkin.PanelStyle);
            var tc = GameUiSkin.TitleStyle.normal.textColor;
            GameUiSkin.TitleStyle.normal.textColor = _resultWin ? GameUiSkin.Cyan : GameUiSkin.Danger;
            GUI.Label(new Rect(panel.x, panel.y + 34f, panel.width, 50f), _resultWin ? "勝利！" : "敗北", GameUiSkin.TitleStyle);
            GameUiSkin.TitleStyle.normal.textColor = tc;
            GUI.Label(new Rect(panel.x, panel.y + 96f, panel.width, 24f), _resultWin ? "VICTORY" : "DEFEAT", GameUiSkin.SmallStyle);

            var btn = new Rect(panel.x + panel.width * 0.5f - 80f, panel.y + 150f, 160f, 44f);
            if (GUI.Button(btn, "重新開始  (R)", GameUiSkin.ButtonStyle) ||
                (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.R))
                Restart();
        }

        private void Restart()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
        }
    }
}
