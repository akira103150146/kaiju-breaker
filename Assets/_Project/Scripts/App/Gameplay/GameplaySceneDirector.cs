using System;
using KaijuBreaker.Core;
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

        [Tooltip("Begin the run automatically on Play. Off = call BeginRun() from a menu/START button.")]
        [SerializeField] private bool _autoStart = true;

        private GameComposition _comp;
        private PlayerShip _player;
        private PlayerWeaponController _playerWeapon;
        private SegmentSequenceRunner _waveRunner;
        private Action<RunStateChanged> _onRunStateChanged;
        private bool _defeated;

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

            SpawnPlayer();
            if (_autoStart) BeginRun();
        }

        private void OnDestroy()
        {
            if (_comp != null && _onRunStateChanged != null) _comp.Bus.Unsubscribe(_onRunStateChanged);
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
            _comp.Bus.Publish(new LoadoutConfirmed(default, default, default));

            var sequence = _comp.Stage?.CurrentSequence;
            if (sequence == null)
            {
                Debug.LogWarning("[GameplaySceneDirector] No stage sequence (ContentRegistry missing Stage config?) — no waves.");
                return;
            }

            var content = _bootstrap.Content;

            // Shared enemy-bullet pool for the whole run; enemies fire from it (aimed at the player).
            EnemyCombatContext combat = null;
            if (_enemyBulletPrefab != null)
            {
                var poolGo = new GameObject("EnemyBulletPool");
                var pool = poolGo.AddComponent<EnemyBulletPool>();
                pool.Configure(_enemyBulletPrefab);
                combat = new EnemyCombatContext(pool, _player != null ? _player.transform : null);
            }

            var runnerGo = new GameObject("WaveRunner");
            _waveRunner = runnerGo.AddComponent<SegmentSequenceRunner>();
            _waveRunner.Run(sequence, _comp.Difficulty, content.WaveTiming, _enemyPrefab,
                            new System.Random(), OnWavesCleared, combat);

            _playerWeapon?.SetFiring(true);
            Debug.Log($"[GameplaySceneDirector] Run started — {sequence.EscalatingSegments.Count} segments queued.");
        }

        private void OnWavesCleared()
        {
            Debug.Log("[GameplaySceneDirector] 道中 CLEAR — entering boss fight.");
            if (_bossController != null) _bossController.BeginBossFight(_comp);
            else Debug.Log("[GameplaySceneDirector] No BossController assigned — run ends after 道中.");
        }

        private void OnRunStateChanged(RunStateChanged evt)
        {
            if (_player != null) _player.SetBossPhase(evt.To == RunState.Boss);
        }

        private void OnPlayerDied()
        {
            if (_defeated) return;
            _defeated = true;
            _playerWeapon?.SetFiring(false);
            Debug.Log("[GameplaySceneDirector] DEFEAT — player HP reached 0. (Defeat→RESULTS transition is a Phase E follow-up.)");
        }
    }
}
