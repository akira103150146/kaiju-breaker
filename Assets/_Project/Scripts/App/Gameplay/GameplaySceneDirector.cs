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
    /// front-end is a code-built UGUI + TextMeshPro layer (<see cref="GameUiView"/>, ADR-0006); this director owns
    /// the menu/HUD state machine and forwards button presses to game logic.</para>
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

        [Tooltip("Number of strengthen chips an ELITE drops on death (each raises the whole current arsenal by 1). " +
                 "Director tuning — a meaningful reward for the tougher kill.")]
        [SerializeField, Range(1, 5)] private int _eliteStrengthenCount = 2;

        [Tooltip("Chance [0..1] that an ordinary TRASH kill drops one strengthen chip. Gives the player far more " +
                 "frequent strengthen chances than elite-only drops, without carpeting the field. Director tuning.")]
        [SerializeField, Range(0f, 1f)] private float _trashStrengthenChance = 0.14f;

        [Tooltip("Dwell-cycle weapon pod prefab (WeaponPodController) — legacy, no longer dropped (session 15). Optional.")]
        [SerializeField] private WeaponPodController _weaponPodPrefab;

        [Tooltip("Skip the title screen and begin immediately on Play (e.g. for tests).")]
        [SerializeField] private bool _autoStart = false;

        [Tooltip("Unused since the UGUI+TMP migration (kept to avoid scene churn); TMP uses its own default font asset.")]
        [SerializeField] private Font _uiFont;

        private GameComposition _comp;
        private PlayerShip _player;
        private PlayerWeaponController _playerWeapon;
        private PlayerInputRouter _inputRouter; // cached at run start; drives the 集氣 button glow from charge fill
        private EnemyBulletPool _bulletPool;
        private SegmentSequenceRunner _waveRunner;
        private Action<RunStateChanged> _onRunStateChanged;
        private Action<WeaponPodGrabbed> _onWeaponPodGrabbed;
        private bool _defeated;
        private UtilityUpgrades _utility;
        private int _selBossIndex;
        private bool _skipToBoss; // DEV: skip the 道中 wave sequence and go straight to the boss
        private WeaponId _selPrimary = WeaponId.L1;
        private WeaponId _selSecondary = WeaponId.M1;
        private DifficultyTier _selDifficulty = DifficultyTier.D1;
        private RunState _runState = RunState.Loadout;

        private GameUiView _ui;
        private GameUiView.Screen _screen = GameUiView.Screen.None;
        private bool _resultWin;

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

            BuildUi();
            SpawnPlayer();
            _playerWeapon?.SetFiring(false); // hold fire on the title screen

            if (_autoStart) { BeginRun(); ShowScreen(GameUiView.Screen.Hud); }
            else ShowScreen(GameUiView.Screen.Title);
        }

        private void BuildUi()
        {
            var uiGo = new GameObject("GameUi");
            uiGo.transform.SetParent(transform, false);
            _ui = uiGo.AddComponent<GameUiView>();
            _ui.OnTitleTap = StartFromTitle;
            _ui.OnBossPicked = OnBossPicked;
            _ui.OnBossConfirm = ConfirmBossSelect;
            _ui.OnUpgradesOpen = OpenUpgrades;
            _ui.OnUpgradesClose = CloseUpgrades;
            _ui.OnPrimaryPicked = i => { _selPrimary = (WeaponId)i; RefreshLoadout(); };
            _ui.OnSecondaryPicked = i => { _selSecondary = (WeaponId)(i + 4); RefreshLoadout(); };
            _ui.OnDifficultyPicked = i => { _selDifficulty = (DifficultyTier)i; RefreshLoadout(); };
            _ui.OnSkipToggled = () => { _skipToBoss = !_skipToBoss; RefreshLoadout(); };
            _ui.OnStart = ConfirmLoadout;
            _ui.OnRestart = Restart;
            _ui.OnBuyUpgrade = BuyUpgrade;
            _ui.Build(BossNames, BossCodes, BossColors, BossUnlocked, PrimaryLabels, SecondaryLabels, DiffLabels);
            RefreshLoadout();
            _ui.SetBossSelected(_selBossIndex);
        }

        private void ShowScreen(GameUiView.Screen s)
        {
            _screen = s;
            _ui?.Show(s);
        }

        private void Update()
        {
            if (_screen == GameUiView.Screen.Title)
            {
                _ui?.TickTitleBlink();
                // The full-screen tap-catcher button covers mouse/touch; keyboard uses Space/Enter (a broad
                // anyKeyDown would skip the title on stray input).
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    StartFromTitle();
            }
            else if (_screen == GameUiView.Screen.Hud)
            {
                if (_player != null && _playerWeapon != null)
                {
                    _ui?.SetHud(_player.Hp, _player.MaxHp, _playerWeapon.WeaponPower, _playerWeapon.MissilePower,
                                _playerWeapon.PrimaryType.ToString(), _playerWeapon.SecondaryType.ToString(), _runState);
                    _ui?.SetCharge(_playerWeapon.ChargeActive, _playerWeapon.ChargeFraction01); // 波動 L3 charge bar
                    if (_inputRouter != null) _inputRouter.ChargeFill = _playerWeapon.ChargeFraction01;
                }
            }
            else if (_screen == GameUiView.Screen.Results)
            {
                if (Input.GetKeyDown(KeyCode.R)) Restart();
            }
        }

        private void StartFromTitle()
        {
            if (_screen != GameUiView.Screen.Title) return;
            ShowScreen(GameUiView.Screen.BossSelect); // title → boss select (MMX-style hub) → loadout → play
            _ui?.SetBossSelected(_selBossIndex);
        }

        private void OnBossPicked(int index)
        {
            if (index < 0 || index >= BossUnlocked.Length || !BossUnlocked[index]) return;
            _selBossIndex = index;
            _ui?.SetBossSelected(_selBossIndex);
        }

        private void ConfirmBossSelect()
        {
            if (!BossUnlocked[_selBossIndex]) return; // locked target — stay on the select screen
            ShowScreen(GameUiView.Screen.Loadout);
            RefreshLoadout();
            // (Only CARAPEX is wired to a KaijuDef today; future targets set the BossController's boss here.)
        }

        private void OpenUpgrades() { ShowScreen(GameUiView.Screen.Upgrades); RefreshUpgrades(); }
        private void CloseUpgrades() { ShowScreen(GameUiView.Screen.BossSelect); _ui?.SetBossSelected(_selBossIndex); }

        private void ConfirmLoadout()
        {
            _comp?.Difficulty.SetTier(_selDifficulty); // difficulty is real (scales enemy count / bullet density)
            ShowScreen(GameUiView.Screen.Hud);
            BeginRun();
        }

        private void RefreshLoadout()
        {
            _ui?.SetLoadout((int)_selPrimary, (int)_selSecondary - 4, (int)_selDifficulty, _skipToBoss);
        }

        // Meta utility shop: two shard-funded rows + five core-funded rows. Killing power stays in-run.
        private void RefreshUpgrades()
        {
            if (_ui == null || _utility == null) return;
            _ui.SetShards(_utility.Shards);
            int ml = UtilityUpgrades.MaxLevel, mc = UtilityUpgrades.MaxCoreLevel;
            _ui.SetUpgradeRow((int)GameUiView.UpgradeRowId.FireRate, _utility.FireRateLevel, ml, _utility.CostFor(_utility.FireRateLevel), _utility.FireRateLevel >= ml, "");
            _ui.SetUpgradeRow((int)GameUiView.UpgradeRowId.DropRate, _utility.DropRateLevel, ml, _utility.CostFor(_utility.DropRateLevel), _utility.DropRateLevel >= ml, "");
            _ui.SetUpgradeRow((int)GameUiView.UpgradeRowId.Ammo, _utility.AmmoLevel, mc, _utility.CoreCostFor(_utility.AmmoLevel), _utility.AmmoLevel >= mc, "   核心 " + _utility.CoreBalance(MaterialId.CoreSwarm));
            _ui.SetUpgradeRow((int)GameUiView.UpgradeRowId.Magnet, _utility.MagnetLevel, mc, _utility.CoreCostFor(_utility.MagnetLevel), _utility.MagnetLevel >= mc, "   核心 " + _utility.CoreBalance(MaterialId.CoreCrystal));
            _ui.SetUpgradeRow((int)GameUiView.UpgradeRowId.IFrame, _utility.IFrameLevel, mc, _utility.CoreCostFor(_utility.IFrameLevel), _utility.IFrameLevel >= mc, "   核心 " + _utility.CoreBalance(MaterialId.CoreAbyss));
            _ui.SetUpgradeRow((int)GameUiView.UpgradeRowId.Speed, _utility.SpeedLevel, mc, _utility.CoreCostFor(_utility.SpeedLevel), _utility.SpeedLevel >= mc, "   核心 " + _utility.CoreBalance(MaterialId.CoreEmber));
            _ui.SetUpgradeRow((int)GameUiView.UpgradeRowId.HeadStart, _utility.HeadStartLevel, mc, _utility.CoreCostFor(_utility.HeadStartLevel), _utility.HeadStartLevel >= mc, "   核心 " + _utility.CoreBalance(MaterialId.CoreVoid));
        }

        private void BuyUpgrade(int id)
        {
            if (_utility == null) return;
            switch ((GameUiView.UpgradeRowId)id)
            {
                case GameUiView.UpgradeRowId.FireRate: _utility.BuyFireRate(); break;
                case GameUiView.UpgradeRowId.DropRate: _utility.BuyDropRate(); break;
                case GameUiView.UpgradeRowId.Ammo: _utility.BuyAmmo(); break;
                case GameUiView.UpgradeRowId.Magnet: _utility.BuyMagnet(); break;
                case GameUiView.UpgradeRowId.IFrame: _utility.BuyIFrame(); break;
                case GameUiView.UpgradeRowId.Speed: _utility.BuySpeed(); break;
                case GameUiView.UpgradeRowId.HeadStart: _utility.BuyHeadStart(); break;
            }
            RefreshUpgrades();
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
            _playerWeapon?.SetSfx(_bootstrap != null ? _bootstrap.Sfx : null); // shoot blips
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
            // Show the mobile 集氣 button only when this run's primary is the 波動 charge weapon (L3). The primary
            // is fixed for the whole run (no in-run switching), so this is set once here.
            _inputRouter = _player != null ? _player.GetComponent<PlayerInputRouter>() : null;
            if (_inputRouter != null) _inputRouter.ChargeControlVisible = _selPrimary == WeaponId.L3;
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
                                                _comp != null && _comp.Difficulty != null ? _comp.Difficulty.BulletDensityMult : 1f,
                                                () => _bootstrap?.Sfx?.PlayEnemyHit()); // per-hit mob blip


            if (_skipToBoss)
            {
                // DEV shortcut: no wave runner — jump straight to the boss (pool/combat already set up for it).
                Debug.Log("[GameplaySceneDirector] DEV: skipping 道中 — straight to boss.");
                OnWavesCleared();
            }
            else
            {
                _bootstrap?.Sfx?.PlayMusic("Music/bgm_stage", 0.5f); // 道中 BGM
                var runnerGo = new GameObject("WaveRunner");
                _waveRunner = runnerGo.AddComponent<SegmentSequenceRunner>();
                _waveRunner.Run(sequence, _comp.Difficulty, content.WaveTiming, _enemyPrefab,
                                new System.Random(), OnWavesCleared, combat);
                Debug.Log($"[GameplaySceneDirector] Run started — {sequence.EscalatingSegments.Count} segments queued.");
            }

            _playerWeapon?.SetFiring(true);
        }

        // Enemy death → in-run strengthen drop (session 15). There is ONE generic strengthen chip (PowerUpKind.Power)
        // that raises the player's CURRENT loadout — both primary firepower and missile level — so no drop is ever
        // "the wrong weapon". Elites drop several (a real reward); ordinary trash has a modest per-kill chance, which
        // gives the player far more frequent strengthen chances than the old elite-only rule while staying readable.
        // The old L1→L4 type-switching pod is gone: you keep the loadout you picked and just make it stronger.
        private void SpawnDrop(Vector3 pos, bool isElite)
        {
            _bootstrap?.Sfx?.PlayEnemyExplode(isElite ? 0.9f : 0.6f); // every kill goes boom (trash quieter)
            if (_powerUpPrefab == null) return;

            if (isElite)
            {
                int count = Mathf.Max(1, _eliteStrengthenCount);
                for (int i = 0; i < count; i++)
                {
                    float x = count == 1 ? 0f : Mathf.Lerp(-0.6f, 0.6f, i / (float)(count - 1));
                    Spawn(pos + new Vector3(x, 0f, 0f), PowerUpKind.Power);
                }
            }
            else if (UnityEngine.Random.value < _trashStrengthenChance)
            {
                Spawn(pos, PowerUpKind.Power);
            }
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
            _bootstrap?.Sfx?.PlayMusic("Music/bgm_boss", 0.55f); // switch to the boss loop
            if (_bossController != null)
                _bossController.BeginBossFight(_comp, _selBossIndex, _bulletPool,
                                               _player != null ? _player.transform : null);
            else Debug.Log("[GameplaySceneDirector] No BossController assigned — run ends after 道中.");
        }

        private void OnRunStateChanged(RunStateChanged evt)
        {
            _runState = evt.To;
            if (_player != null) _player.SetBossPhase(evt.To == RunState.Boss);
            if (evt.To == RunState.Results)
            {
                _resultWin = !_defeated;
                _ui?.SetResults(_resultWin);
                ShowScreen(GameUiView.Screen.Results);
                _bootstrap?.Sfx?.StopMusic(); // silence the loop on the results screen (win or lose)
            }
        }

        private void OnPlayerDied()
        {
            if (_defeated) return;
            _defeated = true;
            _playerWeapon?.SetFiring(false);
            _comp?.Run.Defeat(); // STAGE/BOSS → RESULTS (defeat), settles as non-full-clear
            Debug.Log("[GameplaySceneDirector] DEFEAT — player HP reached 0.");
        }

        private void Restart()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
        }

        // ── Static menu data (labels / roster) passed to the UGUI view ───────────
        private static readonly string[] PrimaryLabels = { "L1 散波", "L2 集束", "L3 波動", "L4 穿透" };
        private static readonly string[] SecondaryLabels = { "M1 追蹤", "M2 蜂群", "M3 魚雷", "M4 叢集" };
        private static readonly string[] DiffLabels = { "D1", "D2", "D3", "D4" };

        private static readonly string[] BossNames = { "甲殼獸", "利刃獸", "雷龍", "巢母", "稜殼獸", "潮顎", "燼使", "虛尖" };
        private static readonly string[] BossCodes = { "CARAPEX", "LACERA", "VOLTWYRM", "BROODCORE", "PRISMSHELL", "TIDEMAW", "EMBERWING", "NULLSPIRE" };
        private static readonly bool[] BossUnlocked = { true, true, true, true, true, true, true, true };
        private static readonly Color[] BossColors =
        {
            new Color(1f, 0.40f, 0.34f), new Color(0.72f, 0.45f, 1f), new Color(0.40f, 0.90f, 1f),
            new Color(1f, 0.70f, 0.30f), new Color(0.55f, 0.85f, 1f), new Color(0.30f, 0.72f, 0.72f),
            new Color(1f, 0.50f, 0.25f), new Color(0.62f, 0.40f, 0.85f)
        };
    }
}
