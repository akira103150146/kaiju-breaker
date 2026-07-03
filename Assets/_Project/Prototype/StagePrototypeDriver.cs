// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does the full 4-phase loop (LOADOUT -> STAGE -> BOSS -> RESULTS) with real
//           weapon choice, mob waves, and the authoritative dual-track part system feel
//           like a cohesive game the director can see and feel?
// Date: 2026-07-03
// Parity target: prototypes/vision-slice/prototype.html (read that file first — it is the spec).
//
// Architecture note: BOSS parts are driven by the REAL KaijuBreaker.KaijuParts.PartStateSystem
// (dual-track heat/break, armor gate, stagger) via the Core event bus — this file never touches
// part state directly, only publishes LaserHit / WaveHit / MissileHit and subscribes to the
// resulting PartSoftened / PartStaggered / PartStaggerEnd / PartBroke / BossCoreBroke events.
// Mob (wave) enemies are plain hp ints — no part system — matching the HTML's makeEnemy().
//
// Controls:
//   LOADOUT : 1-4 primary, 5-8 secondary, Q/E difficulty, Z/X/C boss target, Enter/click start
//   STAGE/BOSS : mouse/touch drag = move (auto-fires primary) - click/tap/Space = secondary
//                Z hold+release = L3 charge shockwave - R = abort to loadout
//                (1-4/5-8 mid-run hot-swap is DEBUG-only — _debugFreeWeaponSwap; real weapons change via the POD)
//   RESULTS : R = retry, M = back to loadout
using System.Collections.Generic;
using System.Reflection;
using KaijuBreaker.Content;
using KaijuBreaker.Core;
using KaijuBreaker.KaijuParts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KaijuBreaker.Prototype
{
    public sealed class StagePrototypeDriver : MonoBehaviour
    {
        // ── Canvas-space constants (mirrors the HTML's 320x480 logical canvas) ───
        private const float IW = 320f;
        private const float IH = 480f;
        private const float WorldScale = 0.05f; // world units per canvas px
        private const int KaijuId = 1;
        private const float PlayerMaxHp = 120f;

        private enum Phase { Loadout, Stage, Boss, Results }
        private Phase _phase = Phase.Loadout;

        // TEST/DEBUG ONLY. In the real game a run's weapons are fixed at LOADOUT and only change via
        // the in-run weapon POD pickup (design) — free mid-run hot-swap would make upgrades pointless.
        // Toggle in the Inspector to cheat-swap (1-4/5-8) while playtesting. Default OFF.
        [SerializeField] private bool _debugFreeWeaponSwap = false;

        // ── Weapon data tables (mirrors HTML PRIMARIES / SECONDARIES / DIFFICULTIES) ──
        private struct PrimaryDef { public WeaponId Id; public string Name; public string Niche; public float HeatDelta; public float FireRate; }
        private struct SecondaryDef { public WeaponId Id; public string Name; public string Niche; public int Mag; public float Reload; public float DmgBase; }
        private struct DifficultyDef { public string Label; public string Desc; public float Mult; }

        // HeatDelta values are the HTML's 0..1 heatPerHit scaled x100 into the real
        // system's HU scale (HMaxNormal=100 default) so relative pacing (L2 fastest,
        // L3 slowest) is preserved. See report for rationale.
        private static readonly PrimaryDef[] Primaries =
        {
            new PrimaryDef{ Id=WeaponId.L1, Name="L1 散波",   Niche="廣覆蓋・同時蓄熱多部位",     HeatDelta=17f, FireRate=0.11f },
            new PrimaryDef{ Id=WeaponId.L2, Name="L2 集束",   Niche="靜止目標最快蓄熱",           HeatDelta=48f, FireRate=0.058f },
            new PrimaryDef{ Id=WeaponId.L3, Name="L3 波動砲", Niche="Hold蓄能・唯一裝甲破除",      HeatDelta=9f,  FireRate=0.44f },
            new PrimaryDef{ Id=WeaponId.L4, Name="L4 穿透",   Niche="單發穿透縱列・同打多段",       HeatDelta=20f, FireRate=0.38f },
        };

        private static readonly SecondaryDef[] Secondaries =
        {
            new SecondaryDef{ Id=WeaponId.M1, Name="M1 追蹤", Niche="自動追蹤移動部位",         Mag=6, Reload=3.0f, DmgBase=24f },
            new SecondaryDef{ Id=WeaponId.M2, Name="M2 蜂群", Niche="廣域齊射・護盾窗口洗傷",   Mag=8, Reload=5.0f, DmgBase=14f },
            new SecondaryDef{ Id=WeaponId.M3, Name="M3 魚雷", Niche="軟化時高倍爆傷・靜止巨傷", Mag=3, Reload=4.0f, DmgBase=70f },
            new SecondaryDef{ Id=WeaponId.M4, Name="M4 叢集", Niche="頂部AoE・範圍覆蓋",         Mag=4, Reload=3.5f, DmgBase=30f },
        };

        private static readonly DifficultyDef[] Difficulties =
        {
            new DifficultyDef{ Label="D1 普通", Desc="低壓・學習循環",  Mult=1.0f },
            new DifficultyDef{ Label="D2 困難", Desc="需主動讀彈",      Mult=1.5f },
            new DifficultyDef{ Label="D3 極限", Desc="持續高壓",        Mult=2.2f },
            new DifficultyDef{ Label="D4 惡夢", Desc="子彈覆蓋",        Mult=3.2f },
        };

        private static readonly string[] BossIds = { "CARAPEX", "LACERA", "VOLTWYRM" };

        // ── Boss content (mirrors HTML makeBossDef) ───────────────────────────────
        private sealed class PartVisDef
        {
            public string Key, Name; public PartType Type;
            public float Bx, By, W, H; public Color Hue;
            public bool Sweep; public float SweepAmp, SweepSpd, SweepPhase;
        }
        private sealed class BossDef
        {
            public string Id, Name, NameEn, ShineWeapon, CoreName, PatternType;
            public Color BgColor;
            public PartVisDef[] Parts;
        }

        private static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        private static BossDef MakeBossDef(string id)
        {
            if (id == "CARAPEX")
            {
                return new BossDef
                {
                    Id = "CARAPEX", Name = "鎧殼獸", NameEn = "CARAPEX",
                    ShineWeapon = "L2×M3 推薦", CoreName = "甲殼核心", BgColor = Hex("#0d0802"),
                    PatternType = "carapex",
                    Parts = new[]
                    {
                        new PartVisDef{ Key="core", Name="胸口核心", Type=PartType.BossCore, Bx=160,By=100,W=44,H=40, Hue=Hex("#ffd23f") },
                        new PartVisDef{ Key="mL",   Name="左大顎",   Type=PartType.Normal,   Bx=88, By=124,W=36,H=26, Hue=Hex("#9b6030") },
                        new PartVisDef{ Key="mR",   Name="右大顎",   Type=PartType.Normal,   Bx=232,By=124,W=36,H=26, Hue=Hex("#9b6030") },
                        new PartVisDef{ Key="dc",   Name="背甲炮",   Type=PartType.Armored,  Bx=160,By=52, W=38,H=26, Hue=Hex("#3f6080") },
                    }
                };
            }
            if (id == "LACERA")
            {
                return new BossDef
                {
                    Id = "LACERA", Name = "刃肢獸", NameEn = "LACERA",
                    ShineWeapon = "M1 追蹤推薦", CoreName = "四肢核心", BgColor = Hex("#060a02"),
                    PatternType = "lacera",
                    Parts = new[]
                    {
                        new PartVisDef{ Key="core", Name="頭部核心", Type=PartType.BossCore, Bx=160,By=78, W=40,H=36, Hue=Hex("#ffd23f") },
                        new PartVisDef{ Key="fL", Name="左前肢", Type=PartType.Normal, Bx=82, By=112,W=32,H=24, Hue=Hex("#6a9030"), Sweep=true, SweepAmp=50, SweepSpd=1.4f, SweepPhase=0f },
                        new PartVisDef{ Key="fR", Name="右前肢", Type=PartType.Normal, Bx=238,By=112,W=32,H=24, Hue=Hex("#6a9030"), Sweep=true, SweepAmp=50, SweepSpd=1.4f, SweepPhase=Mathf.PI },
                        new PartVisDef{ Key="hL", Name="左後肢", Type=PartType.Normal, Bx=62, By=148,W=28,H=22, Hue=Hex("#5a8028"), Sweep=true, SweepAmp=62, SweepSpd=0.85f, SweepPhase=Mathf.PI/2f },
                        new PartVisDef{ Key="hR", Name="右後肢", Type=PartType.Normal, Bx=258,By=148,W=28,H=22, Hue=Hex("#5a8028"), Sweep=true, SweepAmp=62, SweepSpd=0.85f, SweepPhase=3f*Mathf.PI/2f },
                    }
                };
            }
            // VOLTWYRM
            return new BossDef
            {
                Id = "VOLTWYRM", Name = "熾蛇", NameEn = "VOLTWYRM",
                ShineWeapon = "L4 穿透推薦", CoreName = "能量核心", BgColor = Hex("#090806"),
                PatternType = "voltwyrm",
                Parts = new[]
                {
                    new PartVisDef{ Key="core", Name="核心節",  Type=PartType.BossCore, Bx=160,By=46, W=38,H=34, Hue=Hex("#ffd23f") },
                    new PartVisDef{ Key="sL", Name="左能量盾", Type=PartType.Armored, Bx=104,By=48, W=30,H=28, Hue=Hex("#2a2a80") },
                    new PartVisDef{ Key="sR", Name="右能量盾", Type=PartType.Armored, Bx=216,By=48, W=30,H=28, Hue=Hex("#2a2a80") },
                    new PartVisDef{ Key="n1", Name="頸段一",   Type=PartType.Normal,  Bx=160,By=90, W=28,H=22, Hue=Hex("#b87020") },
                    new PartVisDef{ Key="n2", Name="頸段二",   Type=PartType.Normal,  Bx=160,By=116,W=28,H=22, Hue=Hex("#b87020") },
                    new PartVisDef{ Key="n3", Name="頸段三",   Type=PartType.Normal,  Bx=160,By=142,W=28,H=22, Hue=Hex("#b87020") },
                }
            };
        }

        // ── Core systems ───────────────────────────────────────────────────────
        private IEventBus _bus;
        private PartStateSystem _parts;
        private WeaponBalanceConfig _balance;
        private PartSystemConfig _partConfig;

        // ── Scene ──────────────────────────────────────────────────────────────
        private Camera _cam;
        private Sprite _sprite;
        private Sprite _circle;
        private Sprite _diamondSprite; // basic mob tier silhouette
        private Sprite _hexSprite;     // elite mob tier silhouette
        private Transform _worldRoot;
        private Transform _playerT;
        private SpriteRenderer _playerSr;
        private Texture2D _guiBgTex;

        // ── Loadout choice (persisted across runs) ────────────────────────────
        private int _choicePrimary, _choiceSecondary, _choiceDifficulty;
        private string _choiceBoss = "CARAPEX";

        // ── Active run state ───────────────────────────────────────────────────
        private BossDef _bossDef;
        private float _diffMult = 1f;
        private int _pidx, _sidx;
        private sealed class AmmoState { public int Ammo; public bool Reloading; public float ReloadT; }
        private AmmoState[] _ws;
        private bool _l3Charging; private float _l3Charge, _l3Cooldown;
        private float _px, _py, _ptx, _pty, _phpCur, _pInv, _pHurtFlash, _pFireT;
        private bool _over, _won;
        private float _winDelayT = -1f;
        private float _t, _elapsed;
        private int _score, _kills, _matCount, _partsBrokenCount, _totalNonCoreParts;

        // stage sub-state
        private enum StageSub { Wave1, Wave2, Elite, PodWindow }
        private StageSub _stageSub;
        private float _stageT;
        private bool _wave2Spawned; private float _wave2DelayT = -1f;
        private bool _eliteSpawned; private float _eliteDelayT = -1f;
        private bool _podSpawned; private float _podT; private bool _podCollectedOrTimeout;
        private bool _bossEnterTriggered; private float _bossEnterDelayT = -1f;
        private float _bossEnterAnim;

        // fx
        private float _shake, _flash, _freeze, _slowMo = 1f, _slowT;

        // ── Runtime entities ───────────────────────────────────────────────────
        private sealed class PartRuntime
        {
            public int Id; public PartVisDef Def;
            public float Cx, Cy;
            public GameObject Go; public SpriteRenderer Body; public Transform HeatBar, BreakBar;
            public float FireT = 1f; public float FlashT; public float StaggerRemaining;
        }
        private readonly Dictionary<int, PartRuntime> _partsVis = new Dictionary<int, PartRuntime>(8);

        private sealed class Enemy
        {
            public float X, Y, Vx, Vy, W = 18, H = 14, Hp = 45, Max = 45, FireT; public bool Alive = true;
            public bool IsElite; public float HitFlash; public Color BaseColor = Color.white;
            public GameObject Go; public SpriteRenderer Sr;
        }
        private readonly List<Enemy> _enemies = new List<Enemy>(8);

        private sealed class PBullet { public float X, Y, Vx, Vy, HeatDelta, Dmg; public bool Pierce; public HashSet<int> HitParts; public HashSet<Enemy> HitEnemies; public GameObject Go; }
        private readonly List<PBullet> _pbullets = new List<PBullet>(64);

        private sealed class EBullet { public float X, Y, Vx, Vy, R = 2.5f; public Color Color; public GameObject Go; }
        private readonly List<EBullet> _ebullets = new List<EBullet>(128);

        private sealed class Missile { public float X, Y, Vx, Vy, Life, Dmg; public bool NoHome; public WeaponId Weapon; public GameObject Go; }
        private readonly List<Missile> _missiles = new List<Missile>(32);

        private sealed class Torpedo { public float X, Y, Vy, Life, Dmg, R = 7; public GameObject Go; }
        private readonly List<Torpedo> _torpedoes = new List<Torpedo>(8);

        private sealed class ClusterBomb { public float X, Y, Vx, Vy, Grav = 185, Life, Dmg, R = 5; public bool Detonated; public GameObject Go; }
        private readonly List<ClusterBomb> _clusters = new List<ClusterBomb>(8);

        private sealed class Particle { public float X, Y, Vx, Vy, Life, Max, Grav; public Color Color; public float Size = 0.14f; public GameObject Go; }
        private readonly List<Particle> _particles = new List<Particle>(256);

        private sealed class MatShard { public float X, Y, Vx, Vy, T; public bool Bursting; public GameObject Go; }
        private readonly List<MatShard> _mats = new List<MatShard>(64);

        private sealed class FloatText { public float X, Y, Vy, Life, Max; public string Text; public Color Color; public bool Bold; }
        private readonly List<FloatText> _floats = new List<FloatText>(16);

        private sealed class WeaponPod { public float X, Y, Vy = 120, BobT; public bool IsPrimary; public int Idx; public GameObject Go; }
        private WeaponPod _pod;

        private readonly List<Transform> _stars = new List<Transform>(40);

        private static readonly Vector2 CounterAnchorCanvas = new Vector2(305, 14);

        // ═════════════════════════════════════════════════════════════════════
        // Unity lifecycle
        // ═════════════════════════════════════════════════════════════════════

        private void Start()
        {
            SetupCamera();
            _sprite = MakeSprite();
            _circle = MakeCircle();
            _diamondSprite = MakeDiamond();
            _hexSprite = MakeHexagon();
            _guiBgTex = new Texture2D(1, 1); _guiBgTex.SetPixel(0, 0, new Color(0.03f, 0.035f, 0.07f, 1f)); _guiBgTex.Apply();

            _worldRoot = new GameObject("WorldRoot").transform;

            _balance = ScriptableObject.CreateInstance<WeaponBalanceConfig>();
            _partConfig = ScriptableObject.CreateInstance<PartSystemConfig>();
            _bus = new TypedEventBus();
            _bus.Subscribe<PartStaggered>(OnPartStaggered);
            _bus.Subscribe<PartStaggerEnd>(OnPartStaggerEnd);
            _bus.Subscribe<PartBroke>(OnPartBroke);
            _bus.Subscribe<BossCoreBroke>(OnBossCoreBroke);
            _parts = new PartStateSystem(_bus, _balance, _partConfig);

            SpawnPlayer();
            BuildStarfield();
            _worldRoot.gameObject.SetActive(false);

            _phase = Phase.Loadout;
        }

        private void Update()
        {
            switch (_phase)
            {
                case Phase.Loadout: UpdateLoadoutInput(); break;
                case Phase.Stage:
                case Phase.Boss: UpdateGameplay(); break;
                case Phase.Results: UpdateResultsInput(); break;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // LOADOUT
        // ═════════════════════════════════════════════════════════════════════

        private void UpdateLoadoutInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame) _choicePrimary = 0;
            if (kb.digit2Key.wasPressedThisFrame) _choicePrimary = 1;
            if (kb.digit3Key.wasPressedThisFrame) _choicePrimary = 2;
            if (kb.digit4Key.wasPressedThisFrame) _choicePrimary = 3;
            if (kb.digit5Key.wasPressedThisFrame) _choiceSecondary = 0;
            if (kb.digit6Key.wasPressedThisFrame) _choiceSecondary = 1;
            if (kb.digit7Key.wasPressedThisFrame) _choiceSecondary = 2;
            if (kb.digit8Key.wasPressedThisFrame) _choiceSecondary = 3;
            if (kb.qKey.wasPressedThisFrame) _choiceDifficulty = Mathf.Max(0, _choiceDifficulty - 1);
            if (kb.eKey.wasPressedThisFrame) _choiceDifficulty = Mathf.Min(3, _choiceDifficulty + 1);
            if (kb.zKey.wasPressedThisFrame) _choiceBoss = "CARAPEX";
            if (kb.xKey.wasPressedThisFrame) _choiceBoss = "LACERA";
            if (kb.cKey.wasPressedThisFrame) _choiceBoss = "VOLTWYRM";
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) StartRun();
        }

        private void StartRun()
        {
            ResetGame();
            _worldRoot.gameObject.SetActive(true);
            _phase = Phase.Stage;
        }

        private void ResetGame()
        {
            _bossDef = MakeBossDef(_choiceBoss);
            _diffMult = Difficulties[_choiceDifficulty].Mult;

            var kaiju = BuildKaijuDef(_bossDef);
            _parts.InitializeParts(kaiju, KaijuId);
            _totalNonCoreParts = 0;
            foreach (var p in kaiju.Parts) if (p.PartType != PartType.BossCore) _totalNonCoreParts++;

            ClearAllRuntimeEntities();
            BuildPartVisuals();

            _pidx = _choicePrimary; _sidx = _choiceSecondary;
            _ws = new AmmoState[Secondaries.Length];
            for (int i = 0; i < Secondaries.Length; i++) _ws[i] = new AmmoState { Ammo = Secondaries[i].Mag };
            _l3Charging = false; _l3Charge = 0f; _l3Cooldown = 0f;

            _px = IW * 0.5f; _py = 400f; _ptx = _px; _pty = _py;
            _phpCur = PlayerMaxHp; _pInv = 0f; _pHurtFlash = 0f; _pFireT = 0f;
            _playerT.gameObject.SetActive(true);

            _score = 0; _kills = 0; _matCount = 0; _partsBrokenCount = 0;
            _over = false; _won = false; _winDelayT = -1f;
            _t = 0f; _elapsed = 0f;
            _shake = 0f; _flash = 0f; _freeze = 0f; _slowMo = 1f; _slowT = 0f;

            _stageT = 0f; _stageSub = StageSub.Wave1;
            _wave2Spawned = false; _wave2DelayT = -1f;
            _eliteSpawned = false; _eliteDelayT = -1f;
            _podSpawned = false; _podT = 0f; _podCollectedOrTimeout = false;
            _bossEnterTriggered = false; _bossEnterDelayT = -1f; _bossEnterAnim = 0f;

            SpawnWave(1);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Gameplay frame (STAGE + BOSS share one loop, like the HTML)
        // ═════════════════════════════════════════════════════════════════════

        private void UpdateGameplay()
        {
            HandlePointerAndFireInput();

            float fxDt = Mathf.Min(Time.deltaTime, 0.05f);
            TickFx(fxDt);

            if (_winDelayT >= 0f)
            {
                _winDelayT -= fxDt;
                if (_winDelayT <= 0f) { FinishWin(); return; }
            }

            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) { GoToLoadout(); return; }
            if (_debugFreeWeaponSwap) HandleHotSwapKeys(); // real game: weapons change only via the POD pickup

            float dt = fxDt * _slowMo;
            if (_freeze > 0f) { _freeze -= fxDt * 1000f; dt = 0f; }
            if (_slowT > 0f) _slowT -= fxDt; else _slowMo = Mathf.Min(1f, _slowMo + fxDt * 3.8f);

            if (dt <= 0f) return;

            _t += dt;
            if (!_over) _elapsed += dt;

            MovePlayer(dt);
            if (_pidx == 2) HandleL3Charge(dt); else { _l3Charging = false; _l3Charge = 0f; }
            if (_l3Cooldown > 0f) _l3Cooldown -= dt;

            if (!_over) { _pFireT -= dt; if (_pFireT <= 0f) { _pFireT = Primaries[_pidx].FireRate; FirePrimary(); } }
            TickAmmoReload(dt);

            if (_phase == Phase.Stage) UpdateStagePhase(dt); else UpdateBossPhase(dt);

            MovePlayerBullets(dt);
            MoveMissiles(dt);
            MoveTorpedoes(dt);
            MoveClusters(dt);
            MoveEnemyBullets(dt);
            MoveParticles(dt);
            MoveMats(dt);
            UpdatePod(dt);
            ScrollBackground(dt);

            if (_phase == Phase.Boss) RefreshPartVisuals();
        }

        // ── Player ─────────────────────────────────────────────────────────────

        private void HandlePointerAndFireInput()
        {
            bool held = false; Vector2 screenPos = default; bool pressedEdge = false;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            { held = true; screenPos = Touchscreen.current.primaryTouch.position.ReadValue(); pressedEdge = Touchscreen.current.primaryTouch.press.wasPressedThisFrame; }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            { held = true; screenPos = Mouse.current.position.ReadValue(); pressedEdge = Mouse.current.leftButton.wasPressedThisFrame; }

            if (held)
            {
                var c = ScreenToCanvas(screenPos);
                _ptx = c.x; _pty = c.y;
            }
            bool spaceEdge = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            if (!_over && (pressedEdge || spaceEdge)) FireSecondary();
        }

        private Vector2 ScreenToCanvas(Vector2 screenPos)
        {
            var w = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_cam.transform.position.z));
            return new Vector2(w.x / WorldScale + IW * 0.5f, IH * 0.5f - w.y / WorldScale);
        }

        private void HandleHotSwapKeys()
        {
            var kb = Keyboard.current; if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame) _pidx = 0;
            if (kb.digit2Key.wasPressedThisFrame) _pidx = 1;
            if (kb.digit3Key.wasPressedThisFrame) _pidx = 2;
            if (kb.digit4Key.wasPressedThisFrame) _pidx = 3;
            if (kb.digit5Key.wasPressedThisFrame) _sidx = 0;
            if (kb.digit6Key.wasPressedThisFrame) _sidx = 1;
            if (kb.digit7Key.wasPressedThisFrame) _sidx = 2;
            if (kb.digit8Key.wasPressedThisFrame) _sidx = 3;
        }

        private void MovePlayer(float dt)
        {
            _px += (_ptx - _px) * Mathf.Min(1f, dt * 18f);
            _py += (_pty - _py) * Mathf.Min(1f, dt * 18f);
            _px = Mathf.Clamp(_px, 10f, IW - 10f);
            float minY = _phase == Phase.Boss ? 200f : 290f;
            _py = Mathf.Clamp(_py, minY, IH - 12f);
            if (_pInv > 0f) _pInv -= dt;
            if (_pHurtFlash > 0f) _pHurtFlash -= dt;
            _playerT.position = ToWorld(_px, _py);
            bool blink = _pInv > 0f && Mathf.Repeat(_t, 0.2f) < 0.1f;
            _playerSr.color = blink ? new Color(1f, 1f, 1f, 0.35f) : (_pHurtFlash > 0f ? new Color(1f, 0.2f, 0.2f) : new Color(0.22f, 0.9f, 1f));
        }

        private void HandleL3Charge(float dt)
        {
            var kb = Keyboard.current; if (kb == null) return;
            if (kb.zKey.wasPressedThisFrame && !_l3Charging && _l3Cooldown <= 0f && !_over) { _l3Charging = true; _l3Charge = 0f; }
            if (_l3Charging) _l3Charge += dt;
            if (kb.zKey.wasReleasedThisFrame && _l3Charging)
            {
                if (_l3Charge >= 1.5f) ReleaseShockwave();
                else
                {
                    SpawnPBullet(_px, _py - 9, 0f, -360f, Primaries[2].HeatDelta, 3f, false);
                    _l3Charging = false; _l3Charge = 0f;
                }
            }
        }

        private void TakeDamage()
        {
            if (_pInv > 0f) return;
            _phpCur -= 12f; _pInv = 0.82f; _pHurtFlash = 0.45f; ShakeAdd(4f);
            if (_phpCur <= 0f)
            {
                // HTML behaviour: no lose state — shield resets to full and the run continues.
                _phpCur = PlayerMaxHp; FlashAdd(0.42f);
                PushFloat(_px, _py - 14, "護盾重啟", new Color(0.22f, 0.9f, 1f), false);
            }
        }

        // ── Primary (laser) ────────────────────────────────────────────────────

        private void FirePrimary()
        {
            var w = Primaries[_pidx];
            switch (w.Id)
            {
                case WeaponId.L1:
                    SpawnPBullet(_px, _py - 9, Mathf.Sin(-0.32f) * 310f, -445f, w.HeatDelta, 4f, false);
                    SpawnPBullet(_px, _py - 9, 0f, -445f, w.HeatDelta, 4f, false);
                    SpawnPBullet(_px, _py - 9, Mathf.Sin(0.32f) * 310f, -445f, w.HeatDelta, 4f, false);
                    break;
                case WeaponId.L2:
                    SpawnPBullet(_px, _py - 9, 0f, -530f, w.HeatDelta, 6f, false);
                    break;
                case WeaponId.L3:
                    if (!_l3Charging)
                    {
                        SpawnPBullet(_px - 9, _py - 9, 0f, -360f, w.HeatDelta, 3f, false);
                        SpawnPBullet(_px + 9, _py - 9, 0f, -360f, w.HeatDelta, 3f, false);
                    }
                    break;
                case WeaponId.L4:
                    SpawnPBullet(_px, _py - 9, 0f, -620f, w.HeatDelta, 5f, true);
                    break;
            }
        }

        private void ReleaseShockwave()
        {
            _l3Charging = false; _l3Charge = 0f; _l3Cooldown = 2.0f;
            ShakeAdd(14f); FlashAdd(0.65f);
            foreach (var kv in _parts.Parts)
            {
                if (kv.Value.BreakState == BreakState.Broken) continue;
                _bus.Publish(new WaveHit(kv.Key, KaijuId));
                _bus.Publish(new LaserHit(kv.Key, KaijuId, 52f));
                if (_partsVis.TryGetValue(kv.Key, out var pr)) { pr.FlashT = 0.45f; SpawnSparks(pr.Cx, pr.Cy, new Color(0.6f, 0.92f, 1f), 9); }
            }
            PushFloat(160, 200, "波動砲！", new Color(0.6f, 0.92f, 1f), true);
        }

        // ── Secondary (missiles) ───────────────────────────────────────────────

        private void FireSecondary()
        {
            if (_over || _ws == null) return;
            var wd = Secondaries[_sidx]; var ws = _ws[_sidx];
            if (ws.Ammo <= 0 || ws.Reloading) return;
            ws.Ammo--; if (ws.Ammo <= 0) { ws.Reloading = true; ws.ReloadT = wd.Reload; }

            switch (wd.Id)
            {
                case WeaponId.M1:
                    SpawnMissile(_px - 6, _py - 9, -40, -310, wd.DmgBase, wd.Id, false);
                    SpawnMissile(_px + 6, _py - 9, 40, -310, wd.DmgBase, wd.Id, false);
                    break;
                case WeaponId.M2:
                    for (int i = 0; i < 8; i++)
                    {
                        float ang = -0.56f + i * 0.16f;
                        SpawnMissile(_px, _py - 9, Mathf.Sin(ang) * 350f, -390f * Mathf.Abs(Mathf.Cos(ang * 0.3f)), wd.DmgBase, wd.Id, true);
                    }
                    break;
                case WeaponId.M3:
                    _torpedoes.Add(NewTorpedo(_px, _py - 13, -285f, wd.DmgBase));
                    break;
                case WeaponId.M4:
                    _clusters.Add(NewCluster(_px, _py - 9, (Random.value - 0.5f) * 55f, -285f, wd.DmgBase));
                    break;
            }
        }

        private void TickAmmoReload(float dt)
        {
            for (int i = 0; i < _ws.Length; i++)
            {
                var ws = _ws[i];
                if (!ws.Reloading) continue;
                ws.ReloadT -= dt;
                if (ws.ReloadT <= 0f) { ws.Reloading = false; ws.Ammo = Secondaries[i].Mag; }
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // STAGE (mob waves + weapon pod)
        // ═════════════════════════════════════════════════════════════════════

        private void SpawnWave(int wave)
        {
            _stageSub = wave == 1 ? StageSub.Wave1 : wave == 2 ? StageSub.Wave2 : StageSub.Elite;
            foreach (var e in _enemies) if (e.Go != null) Destroy(e.Go);
            _enemies.Clear();

            if (wave <= 2)
            {
                int count = wave == 1 ? 3 : 5;
                Color baseColor = new Color(0.75f, 0.38f, 0.16f);
                for (int i = 0; i < count; i++)
                {
                    float x = 36 + i * 54 + Random.value * 18;
                    float y = -28 - i * 30;
                    var e = new Enemy { X = x, Y = y, Vx = (Random.value - 0.5f) * 30f, Vy = 70f + Random.value * 30f, FireT = 1.8f + Random.value * 2.0f, BaseColor = baseColor };
                    e.Go = NewQuad("Enemy", baseColor, 2);
                    e.Go.transform.SetParent(_worldRoot, false);
                    e.Go.transform.localScale = ToWorldSize(e.W, e.H);
                    e.Sr = e.Go.GetComponent<SpriteRenderer>();
                    e.Sr.sprite = _diamondSprite; // basic-tier silhouette
                    e.Go.transform.position = ToWorld(e.X, e.Y);
                    _enemies.Add(e);
                }
            }
            else // Elite wave — fewer, tougher, distinct hex silhouette + denser aimed fire + rich drops
            {
                int count = Random.value < 0.5f ? 1 : 2;
                Color baseColor = new Color(0.85f, 0.15f, 0.55f);
                for (int i = 0; i < count; i++)
                {
                    float x = 90 + i * 120 + Random.value * 20;
                    float y = -40 - i * 55;
                    var e = new Enemy
                    {
                        X = x, Y = y, W = 34, H = 30, Hp = 45f * 5f, Max = 45f * 5f,
                        Vx = (Random.value - 0.5f) * 20f, Vy = 46f + Random.value * 16f,
                        FireT = 1.2f + Random.value * 0.8f, IsElite = true, BaseColor = baseColor
                    };
                    e.Go = NewQuad("Enemy", baseColor, 2);
                    e.Go.transform.SetParent(_worldRoot, false);
                    e.Go.transform.localScale = ToWorldSize(e.W, e.H);
                    e.Sr = e.Go.GetComponent<SpriteRenderer>();
                    e.Sr.sprite = _hexSprite; // elite-tier silhouette
                    e.Go.transform.position = ToWorld(e.X, e.Y);
                    _enemies.Add(e);
                }
            }
        }

        private void UpdateStagePhase(float dt)
        {
            int alive = 0;
            foreach (var e in _enemies)
            {
                if (!e.Alive) continue;
                alive++;
                e.X += e.Vx * dt; e.Y += e.Vy * dt;
                if (e.X < 12f || e.X > IW - 12f) e.Vx = -e.Vx;
                e.Go.transform.position = ToWorld(e.X, e.Y);
                if (e.HitFlash > 0f) // per-hit white flash so a landed hit is unmistakable
                {
                    e.HitFlash -= dt;
                    e.Sr.color = e.HitFlash > 0f ? Color.white : e.BaseColor;
                }
                if (e.Y > IH + 30f) { e.Alive = false; e.Go.SetActive(false); continue; } // escaped — no score
                if (!_over)
                {
                    e.FireT -= dt;
                    if (e.FireT <= 0f)
                    {
                        float ang = Mathf.Atan2(_py - e.Y, _px - e.X);
                        if (e.IsElite)
                        {
                            e.FireT = (1.0f + Random.value * 0.6f) / Mathf.Sqrt(_diffMult);
                            int n = Mathf.Max(3, Mathf.RoundToInt(_diffMult * 2.5f));
                            for (int k = 0; k < n; k++)
                            {
                                float a = ang + (k - (n - 1) * 0.5f) * 0.16f;
                                SpawnEBullet(e.X, e.Y + e.H * 0.5f, Mathf.Cos(a) * 96f, Mathf.Sin(a) * 96f, 3f, new Color(1f, 0.15f, 0.55f));
                            }
                        }
                        else
                        {
                            e.FireT = 1.8f + Random.value * 1.4f;
                            int n = Mathf.Max(1, Mathf.RoundToInt(_diffMult));
                            for (int k = 0; k < n; k++)
                            {
                                float a = ang + (k - (n - 1) * 0.5f) * 0.28f;
                                SpawnEBullet(e.X, e.Y + e.H * 0.5f, Mathf.Cos(a) * 68f, Mathf.Sin(a) * 68f, 2.5f, new Color(1f, 0.35f, 0.24f));
                            }
                        }
                    }
                }
            }

            _stageT += dt;

            // Wave1 -> Wave2
            if (_stageSub == StageSub.Wave1 && !_wave2Spawned)
            {
                if ((alive == 0 && _stageT > 1f) || _stageT > 11f) { _wave2Spawned = true; _wave2DelayT = 0.6f; }
            }
            if (_wave2DelayT >= 0f)
            {
                _wave2DelayT -= dt;
                if (_wave2DelayT <= 0f) { _wave2DelayT = -1f; SpawnWave(2); }
            }

            // Wave2 -> Elite
            if (_stageSub == StageSub.Wave2 && !_eliteSpawned)
            {
                if ((alive == 0 && _stageT > 14f) || _stageT > 22f) { _eliteSpawned = true; _eliteDelayT = 0.7f; }
            }
            if (_eliteDelayT >= 0f)
            {
                _eliteDelayT -= dt;
                if (_eliteDelayT <= 0f)
                {
                    _eliteDelayT = -1f;
                    SpawnWave(3);
                    PushFloat(160, 210, "菁英出現！", new Color(1f, 0.2f, 0.55f), true);
                    ShakeAdd(8f); FlashAdd(0.3f);
                }
            }

            // Elite -> guaranteed weapon POD dwell (Elite may also drop a bonus pod on death — see KillEnemy)
            if (_stageSub == StageSub.Elite && !_podSpawned)
            {
                if (alive == 0 || _stageT > 40f)
                {
                    _podSpawned = true; _podT = 0f;
                    SpawnGuaranteedPod(80 + Random.value * 160, -24);
                }
            }
            if (_podSpawned && _stageSub == StageSub.Elite) _stageSub = StageSub.PodWindow;

            if (_podSpawned && !_bossEnterTriggered) _podT += dt;
            bool podDone = _podCollectedOrTimeout || (_podSpawned && (_pod == null || _podT > 7f));
            if (!_bossEnterTriggered && ((_podSpawned && podDone) || _stageT > 48f))
            {
                _bossEnterTriggered = true; _podCollectedOrTimeout = true;
                if (_pod != null) { Destroy(_pod.Go); _pod = null; }
                foreach (var e in _enemies) if (e.Alive) { e.Alive = false; e.Go.SetActive(false); } // despawn stragglers, no score
                foreach (var b in _ebullets) Destroy(b.Go); _ebullets.Clear();
                PushFloat(160, 200, _bossDef.Name + "  出現！", new Color(1f, 0.25f, 0.19f), true);
                ShakeAdd(14f); FlashAdd(0.7f);
                _bossEnterDelayT = 1.0f;
            }
            if (_bossEnterDelayT >= 0f)
            {
                _bossEnterDelayT -= dt;
                if (_bossEnterDelayT <= 0f)
                {
                    _bossEnterDelayT = -1f;
                    _phase = Phase.Boss;
                    _bossEnterAnim = 1.0f;
                    // Fix #1: boss part visuals are hidden throughout STAGE — reveal them only now, as the boss enters.
                    foreach (var pr in _partsVis.Values) { pr.Go.SetActive(true); pr.Cy = pr.Def.By - 180f; }
                }
            }
        }

        // Guaranteed weapon POD spawn — shared by the scheduled Elite->PodWindow dwell and the
        // occasional bonus drop from an Elite kill (see KillEnemy).
        private void SpawnGuaranteedPod(float x, float y)
        {
            bool isPrimary = Random.value < 0.5f;
            int idx;
            if (isPrimary) { do { idx = Random.Range(0, 4); } while (idx == _pidx); }
            else { do { idx = Random.Range(0, 4); } while (idx == _sidx); }
            _pod = new WeaponPod { X = x, Y = y, IsPrimary = isPrimary, Idx = idx };
            _pod.Go = NewQuad("Pod", isPrimary ? new Color(0.22f, 0.9f, 1f) : new Color(1f, 0.56f, 0.25f), 2);
            _pod.Go.transform.SetParent(_worldRoot, false);
            _pod.Go.transform.localScale = ToWorldSize(20, 16);
        }

        private void UpdatePod(float dt)
        {
            if (_pod == null) return;
            _pod.BobT += dt;
            if (_pod.Vy > 0f) { _pod.Y += _pod.Vy * dt; if (_pod.Y > 330f) _pod.Vy = 0f; }
            float bob = Mathf.Sin(_pod.BobT * 3f) * 3f;
            _pod.Go.transform.position = ToWorld(_pod.X, _pod.Y + bob);
            if (Vector2.Distance(new Vector2(_px, _py), new Vector2(_pod.X, _pod.Y)) < 30f)
            {
                if (_pod.IsPrimary) { _pidx = _pod.Idx; PushFloat(_pod.X, _pod.Y - 10, "換裝：" + Primaries[_pod.Idx].Name, new Color(0.25f, 0.8f, 1f), true); }
                else { _sidx = _pod.Idx; PushFloat(_pod.X, _pod.Y - 10, "換裝：" + Secondaries[_pod.Idx].Name, new Color(1f, 0.56f, 0.25f), true); }
                Destroy(_pod.Go); _pod = null; _podCollectedOrTimeout = true;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // BOSS
        // ═════════════════════════════════════════════════════════════════════

        private void UpdateBossPhase(float dt)
        {
            if (_bossEnterAnim > 0f)
            {
                _bossEnterAnim = Mathf.Max(0f, _bossEnterAnim - dt * 1.4f);
                float off = _bossEnterAnim * -180f;
                foreach (var pr in _partsVis.Values) { pr.Cy = pr.Def.By + off; pr.Go.transform.position = ToWorld(pr.Cx, pr.Cy); }
                return;
            }

            if (_bossDef.PatternType == "lacera")
            {
                foreach (var pr in _partsVis.Values)
                {
                    if (!pr.Def.Sweep || !_parts.IsPartAlive(pr.Id)) continue;
                    float ph = pr.Def.SweepPhase + _t * pr.Def.SweepSpd;
                    pr.Cx = pr.Def.Bx + Mathf.Sin(ph) * pr.Def.SweepAmp;
                    pr.Cy = pr.Def.By + Mathf.Cos(ph * 0.5f) * 8f;
                }
            }

            foreach (var pr in _partsVis.Values)
            {
                if (!_parts.IsPartAlive(pr.Id)) continue;
                if (pr.FlashT > 0f) pr.FlashT -= dt;
                if (pr.StaggerRemaining > 0f) pr.StaggerRemaining = Mathf.Max(0f, pr.StaggerRemaining - dt);
                pr.FireT -= dt;
                if (pr.FireT <= 0f)
                {
                    bool isCore = pr.Def.Type == PartType.BossCore;
                    float basePeriod = isCore ? 1.8f : 2.0f + Random.value * 0.6f;
                    pr.FireT = basePeriod / Mathf.Sqrt(_diffMult);

                    bool armorIntactSkip = pr.Def.Type == PartType.Armored && _parts.GetArmorState(pr.Id) == ArmorState.Intact
                                            && (pr.Def.Key == "dc" || pr.Def.Key == "sL" || pr.Def.Key == "sR");
                    if (!armorIntactSkip)
                        foreach (var b in BossFireBullets(pr))
                            SpawnEBullet(pr.Cx, pr.Cy + pr.Def.H * 0.5f, b.vx, b.vy, b.r, b.color);
                }
                pr.Go.transform.position = ToWorld(pr.Cx, pr.Cy);
            }
        }

        private struct EShot { public float vx, vy, r; public Color color; }

        private List<EShot> BossFireBullets(PartRuntime pr)
        {
            var shots = new List<EShot>(8);
            float ang = Mathf.Atan2(_py - pr.Cy, _px - pr.Cx);
            float dm = _diffMult;

            if (_bossDef.PatternType == "carapex")
            {
                if (pr.Def.Key == "core")
                {
                    int n = Mathf.Min(6, Mathf.CeilToInt(1.5f * dm));
                    for (int k = 0; k < n; k++) { float a = ang + (k - (n - 1) * 0.5f) * 0.28f; shots.Add(new EShot { vx = Mathf.Cos(a) * 88f, vy = Mathf.Sin(a) * 88f, r = 3f, color = new Color(0.8f, 0.13f, 0f) }); }
                }
                else if (pr.Def.Key == "mL" || pr.Def.Key == "mR")
                {
                    int n = Mathf.Min(8, Mathf.CeilToInt(3f * dm));
                    for (int k = 0; k < n; k++) { float a = ang + (k - (n - 1) * 0.5f) * 0.26f; shots.Add(new EShot { vx = Mathf.Cos(a) * 76f, vy = Mathf.Sin(a) * 76f, r = 2.5f, color = new Color(1f, 0.5f, 0f) }); }
                }
                else if (pr.Def.Key == "dc")
                {
                    int n = Mathf.Min(12, Mathf.CeilToInt(5f * dm));
                    for (int k = 0; k < n; k++) { float a = -Mathf.PI / 2f + (k - (n - 1) * 0.5f) * 0.20f; shots.Add(new EShot { vx = Mathf.Cos(a) * 95f, vy = Mathf.Sin(a) * 95f, r = 2.5f, color = new Color(1f, 0.8f, 0f) }); }
                }
            }
            else if (_bossDef.PatternType == "lacera")
            {
                if (pr.Def.Key == "core")
                {
                    shots.Add(new EShot { vx = Mathf.Cos(ang) * 82f, vy = Mathf.Sin(ang) * 82f, r = 3f, color = new Color(1f, 0.27f, 0f) });
                }
                else if (pr.Def.Sweep)
                {
                    int n = Mathf.Min(9, Mathf.CeilToInt(3f * dm));
                    for (int k = 0; k < n; k++) { float a = ang + (k - (n - 1) * 0.5f) * 0.30f; shots.Add(new EShot { vx = Mathf.Cos(a) * 82f, vy = Mathf.Sin(a) * 82f, r = 2.5f, color = new Color(1f, 0.55f, 0f) }); }
                }
            }
            else // voltwyrm
            {
                if (pr.Def.Key == "core")
                {
                    int n = Mathf.Min(8, Mathf.CeilToInt(2.5f * dm));
                    for (int k = 0; k < n; k++) { float a = ang + (k - (n - 1) * 0.5f) * 0.30f; shots.Add(new EShot { vx = Mathf.Cos(a) * 90f, vy = Mathf.Sin(a) * 90f, r = 3f, color = new Color(1f, 0.44f, 0.13f) }); }
                }
                else if (pr.Def.Key == "n1" || pr.Def.Key == "n2" || pr.Def.Key == "n3")
                {
                    int arms = Mathf.Min(5, Mathf.CeilToInt(1.5f * dm));
                    float baseAng = _t * 1.6f;
                    for (int k = 0; k < arms; k++) { float a = baseAng + k * (Mathf.PI * 2f / arms); shots.Add(new EShot { vx = Mathf.Cos(a) * 78f, vy = Mathf.Sin(a) * 78f, r = 2.5f, color = new Color(1f, 0.56f, 0.25f) }); }
                }
                else if ((pr.Def.Key == "sL" || pr.Def.Key == "sR") && _parts.GetArmorState(pr.Id) == ArmorState.Stripped)
                {
                    int n = Mathf.Min(12, Mathf.CeilToInt(6f * dm));
                    for (int k = 0; k < n; k++) { float a = (float)k / n * Mathf.PI * 2f; shots.Add(new EShot { vx = Mathf.Cos(a) * 72f, vy = Mathf.Sin(a) * 72f, r = 2.5f, color = new Color(1f, 0.31f, 0.13f) }); }
                }
            }
            return shots;
        }

        // ═════════════════════════════════════════════════════════════════════
        // Projectile movement + collision
        // ═════════════════════════════════════════════════════════════════════

        private void MovePlayerBullets(float dt)
        {
            for (int i = _pbullets.Count - 1; i >= 0; i--)
            {
                var b = _pbullets[i];
                b.X += b.Vx * dt; b.Y += b.Vy * dt;
                if (b.Y < -14f || b.X < -14f || b.X > IW + 14f) { Destroy(b.Go); _pbullets.RemoveAt(i); continue; }
                b.Go.transform.position = ToWorld(b.X, b.Y);

                bool consumed = false;
                if (_phase == Phase.Boss)
                {
                    foreach (var pr in _partsVis.Values)
                    {
                        if (!_parts.IsPartAlive(pr.Id)) continue;
                        if (b.Pierce && b.HitParts.Contains(pr.Id)) continue;
                        if (Overlaps(b.X, b.Y, pr.Cx, pr.Cy, pr.Def.W, pr.Def.H))
                        {
                            if (b.Pierce) b.HitParts.Add(pr.Id);
                            pr.FlashT = 0.08f;
                            if (pr.Def.Type != PartType.BossCore) _bus.Publish(new LaserHit(pr.Id, KaijuId, b.HeatDelta));
                            SpawnSparks(b.X, pr.Cy, new Color(0.49f, 0.98f, 1f), 2);
                            if (!b.Pierce) { consumed = true; break; }
                        }
                    }
                }
                else
                {
                    foreach (var e in _enemies)
                    {
                        if (!e.Alive) continue;
                        if (b.Pierce && b.HitEnemies.Contains(e)) continue;
                        if (Overlaps(b.X, b.Y, e.X, e.Y, e.W, e.H))
                        {
                            if (b.Pierce) b.HitEnemies.Add(e);
                            e.Hp -= b.Dmg * 2f;
                            e.HitFlash = 0.08f;
                            SpawnSparks(b.X, b.Y, new Color(1f, 0.5f, 0.25f), 3);
                            if (e.Hp <= 0f) KillEnemy(e);
                            if (!b.Pierce) { consumed = true; break; }
                        }
                    }
                }
                if (consumed) { Destroy(b.Go); _pbullets.RemoveAt(i); }
            }
        }

        private void MoveMissiles(float dt)
        {
            for (int i = _missiles.Count - 1; i >= 0; i--)
            {
                var m = _missiles[i];
                m.Life -= dt;
                if (m.Life <= 0f) { Destroy(m.Go); _missiles.RemoveAt(i); continue; }

                if (!m.NoHome)
                {
                    Vector2? target = FindNearestTarget(m.X, m.Y);
                    if (target.HasValue)
                    {
                        float ta = Mathf.Atan2(target.Value.y - m.Y, target.Value.x - m.X);
                        float ca = Mathf.Atan2(m.Vy, m.Vx);
                        float da = Mathf.DeltaAngle(ca * Mathf.Rad2Deg, ta * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                        float na = ca + Mathf.Sign(da) * Mathf.Min(Mathf.Abs(da), 3.6f * dt);
                        float spd = new Vector2(m.Vx, m.Vy).magnitude;
                        m.Vx = Mathf.Cos(na) * spd; m.Vy = Mathf.Sin(na) * spd;
                    }
                }
                m.X += m.Vx * dt; m.Y += m.Vy * dt;
                if (m.Y < -20f || m.Y > IH + 20f) { Destroy(m.Go); _missiles.RemoveAt(i); continue; }
                m.Go.transform.position = ToWorld(m.X, m.Y);

                bool hit = false;
                if (_phase == Phase.Boss)
                {
                    foreach (var pr in _partsVis.Values)
                    {
                        if (!_parts.IsPartAlive(pr.Id)) continue;
                        if (Vector2.Distance(new Vector2(pr.Cx, pr.Cy), new Vector2(m.X, m.Y)) < pr.Def.W * 0.5f + 5f)
                        { MissileDamagePart(pr, m.Dmg, m.Weapon); hit = true; break; }
                    }
                }
                else
                {
                    foreach (var e in _enemies)
                    {
                        if (!e.Alive) continue;
                        if (Vector2.Distance(new Vector2(e.X, e.Y), new Vector2(m.X, m.Y)) < e.W * 0.5f + 5f)
                        { e.Hp -= 90f; e.HitFlash = 0.08f; SpawnSparks(m.X, m.Y, new Color(1f, 0.56f, 0.25f), 4); if (e.Hp <= 0f) KillEnemy(e); hit = true; break; }
                    }
                }
                if (hit) { Destroy(m.Go); _missiles.RemoveAt(i); }
            }
        }

        private void MoveTorpedoes(float dt)
        {
            for (int i = _torpedoes.Count - 1; i >= 0; i--)
            {
                var t = _torpedoes[i];
                t.Life -= dt; t.Y += t.Vy * dt;
                if (t.Life <= 0f || t.Y < -22f) { Destroy(t.Go); _torpedoes.RemoveAt(i); continue; }
                t.Go.transform.position = ToWorld(t.X, t.Y);

                bool hit = false;
                if (_phase == Phase.Boss)
                {
                    foreach (var pr in _partsVis.Values)
                    {
                        if (!_parts.IsPartAlive(pr.Id)) continue;
                        if (Overlaps(t.X, t.Y, pr.Cx, pr.Cy, pr.Def.W + t.R * 2f, pr.Def.H + t.R * 2f))
                        { MissileDamagePart(pr, t.Dmg, WeaponId.M3); ShakeAdd(9f); FlashAdd(0.55f); hit = true; break; }
                    }
                }
                else
                {
                    foreach (var e in _enemies)
                    {
                        if (!e.Alive) continue;
                        if (Overlaps(t.X, t.Y, e.X, e.Y, e.W + t.R * 2f, e.H + t.R * 2f))
                        { e.Hp -= 200f; e.HitFlash = 0.08f; ShakeAdd(5f); if (e.Hp <= 0f) KillEnemy(e); hit = true; break; }
                    }
                }
                if (hit) { SpawnSparks(t.X, t.Y, new Color(1f, 0.75f, 0.25f), 12); Destroy(t.Go); _torpedoes.RemoveAt(i); }
            }
        }

        private void MoveClusters(float dt)
        {
            for (int i = _clusters.Count - 1; i >= 0; i--)
            {
                var c = _clusters[i];
                c.Life -= dt;
                if (c.Life <= 0f) { Destroy(c.Go); _clusters.RemoveAt(i); continue; }
                c.X += c.Vx * dt; c.Vy += c.Grav * dt; c.Y += c.Vy * dt;
                c.Go.transform.position = ToWorld(c.X, c.Y);

                float detY = _phase == Phase.Boss ? 150f : 260f;
                if (c.Y < detY)
                {
                    ShakeAdd(7f); FlashAdd(0.42f);
                    const float R = 62f;
                    if (_phase == Phase.Boss)
                    {
                        foreach (var pr in _partsVis.Values)
                        {
                            if (!_parts.IsPartAlive(pr.Id)) continue;
                            float d = Vector2.Distance(new Vector2(pr.Cx, pr.Cy), new Vector2(c.X, c.Y));
                            if (d < R) MissileDamagePart(pr, c.Dmg * (1f - (d / R) * 0.55f), WeaponId.M4);
                        }
                    }
                    else
                    {
                        foreach (var e in _enemies)
                        {
                            if (!e.Alive) continue;
                            float d = Vector2.Distance(new Vector2(e.X, e.Y), new Vector2(c.X, c.Y));
                            if (d < R) { e.Hp -= 80f; e.HitFlash = 0.08f; if (e.Hp <= 0f) KillEnemy(e); }
                        }
                    }
                    SpawnSparks(c.X, c.Y, new Color(1f, 0.5f, 0.13f), 16);
                    PushFloat(c.X, c.Y - 10, "叢集爆炸！", new Color(1f, 0.5f, 0.13f), true);
                    Destroy(c.Go); _clusters.RemoveAt(i);
                }
            }
        }

        private void MoveEnemyBullets(float dt)
        {
            for (int i = _ebullets.Count - 1; i >= 0; i--)
            {
                var b = _ebullets[i];
                b.X += b.Vx * dt; b.Y += b.Vy * dt;
                if (b.Y > IH + 8f || b.Y < -8f || b.X < -8f || b.X > IW + 8f) { Destroy(b.Go); _ebullets.RemoveAt(i); continue; }
                b.Go.transform.position = ToWorld(b.X, b.Y);
                if (!_over && _pInv <= 0f && Vector2.Distance(new Vector2(b.X, b.Y), new Vector2(_px, _py)) < b.R + 2.2f)
                { TakeDamage(); Destroy(b.Go); _ebullets.RemoveAt(i); }
            }
        }

        private Vector2? FindNearestTarget(float x, float y)
        {
            float bestD = float.MaxValue; Vector2? best = null;
            if (_phase == Phase.Boss)
            {
                foreach (var pr in _partsVis.Values)
                {
                    if (!_parts.IsPartAlive(pr.Id)) continue;
                    float d = (pr.Cx - x) * (pr.Cx - x) + (pr.Cy - y) * (pr.Cy - y);
                    if (d < bestD) { bestD = d; best = new Vector2(pr.Cx, pr.Cy); }
                }
            }
            else
            {
                foreach (var e in _enemies)
                {
                    if (!e.Alive) continue;
                    float d = (e.X - x) * (e.X - x) + (e.Y - y) * (e.Y - y);
                    if (d < bestD) { bestD = d; best = new Vector2(e.X, e.Y); }
                }
            }
            return best;
        }

        private static bool Overlaps(float bx, float by, float cx, float cy, float w, float h)
            => bx > cx - w * 0.5f - 2f && bx < cx + w * 0.5f + 2f && by > cy - h * 0.5f - 2f && by < cy + h * 0.5f + 2f;

        // ── Missile damage → real MissileHit event ─────────────────────────────

        private void MissileDamagePart(PartRuntime pr, float baseDmg, WeaponId weapon)
        {
            if (!_parts.IsPartAlive(pr.Id)) return;
            bool armorIntact = pr.Def.Type == PartType.Armored && _parts.GetArmorState(pr.Id) == ArmorState.Intact;
            if (armorIntact)
            {
                PushFloat(pr.Cx, pr.Cy - 4, "裝甲偏轉", new Color(0.44f, 0.56f, 0.66f), false);
                SpawnSparks(pr.Cx, pr.Cy, new Color(0.25f, 0.38f, 0.5f), 2);
                return;
            }
            _bus.Publish(new MissileHit(pr.Id, KaijuId, baseDmg, weapon));
            bool bonus = _parts.GetHeatState(pr.Id) == HeatState.Softened || pr.StaggerRemaining > 0f;
            pr.FlashT = 0.16f;
            SpawnSparks(pr.Cx, pr.Cy, bonus ? new Color(1f, 0.6f, 0f) : Color.white, bonus ? 7 : 3);
            if (bonus) { ShakeAdd(3f); PushFloat(pr.Cx + (Random.value - 0.5f) * 18f, pr.Cy - 12, "軟化加成！", new Color(1f, 0.53f, 0f), true); }
        }

        // ═════════════════════════════════════════════════════════════════════
        // Part-system event handlers (real dual-track break/soften)
        // ═════════════════════════════════════════════════════════════════════

        private void OnPartStaggered(PartStaggered e)
        {
            if (_partsVis.TryGetValue(e.PartId, out var pr))
            {
                pr.StaggerRemaining = e.Duration;
                if (e.ArmorStripped)
                {
                    ShakeAdd(5f); FlashAdd(0.35f);
                    PushFloat(pr.Cx, pr.Cy - 10, "裝甲破除！", new Color(1f, 0.38f, 0.25f), true);
                }
            }
        }

        private void OnPartStaggerEnd(PartStaggerEnd e)
        {
            if (_partsVis.TryGetValue(e.PartId, out var pr)) pr.StaggerRemaining = 0f;
        }

        private void OnPartBroke(PartBroke e)
        {
            if (e.Type == PartType.BossCore) return; // core has its own victory sequence
            _partsBrokenCount++;
            _freeze = 115f; TriggerSlowMo(0.65f, 0.12f); ShakeAdd(11f + _partsBrokenCount * 0.7f); FlashAdd(0.92f);

            if (_partsVis.TryGetValue(e.PartId, out var pr))
            {
                SpawnDebris(pr.Cx, pr.Cy, pr.Def.W, pr.Def.Hue, 14);
                pr.Body.color = new Color(0.13f, 0.06f, 0.1f, 0.4f);
                if (pr.HeatBar != null) pr.HeatBar.localScale = new Vector3(0, pr.HeatBar.localScale.y, 1);
                if (pr.BreakBar != null) pr.BreakBar.localScale = new Vector3(0, pr.BreakBar.localScale.y, 1);

                int mc = 3 + Random.Range(0, 4);
                for (int i = 0; i < mc; i++) SpawnMatShard(pr.Cx, pr.Cy);
                PushFloat(pr.Cx, pr.Cy - 8, "部位破壞！", new Color(1f, 0.89f, 0.48f), true);
                PushFloat(pr.Cx, pr.Cy + 8, "＋素材 ×" + mc, new Color(0.38f, 0.94f, 0.85f), false);
            }
        }

        private void OnBossCoreBroke(BossCoreBroke e)
        {
            if (_won) return;
            _won = true; _over = true;
            _freeze = 220f; TriggerSlowMo(1.2f, 0.05f); ShakeAdd(24f); FlashAdd(1.0f);

            Vector2 corePos = new Vector2(160, 100);
            foreach (var pr in _partsVis.Values)
            {
                if (pr.Def.Type == PartType.BossCore) { corePos = new Vector2(pr.Cx, pr.Cy); continue; }
                if (_parts.IsPartAlive(pr.Id)) { SpawnDebris(pr.Cx, pr.Cy, pr.Def.W, pr.Def.Hue, 10); pr.Body.color = new Color(0.13f, 0.06f, 0.1f, 0.4f); }
            }
            for (int i = 0; i < 40; i++)
            {
                float a = Random.value * Mathf.PI * 2f, sp = 60f + Random.value * 260f;
                SpawnParticle(corePos.x, corePos.y, Mathf.Cos(a) * sp, Mathf.Sin(a) * sp, 0.8f + Random.value * 1.3f, Random.value < 0.5f ? new Color(1f, 0.82f, 0.25f) : Color.white, Random.value < 0.5f ? 0.18f : 0.12f, 45f);
            }
            _winDelayT = 2.2f;
        }

        private void KillEnemy(Enemy e)
        {
            e.Alive = false; e.Go.SetActive(false);
            _score += e.IsElite ? 400 : 100; _kills++;
            ShakeAdd(e.IsElite ? 8f : 3f); FlashAdd(e.IsElite ? 0.4f : 0.2f);
            int debrisN = e.IsElite ? 22 : 10;
            for (int i = 0; i < debrisN; i++)
            {
                float a = Random.value * Mathf.PI * 2f, sp = 40f + Random.value * 80f;
                SpawnParticle(e.X, e.Y, Mathf.Cos(a) * sp, Mathf.Sin(a) * sp - 20f, 0.3f + Random.value * 0.3f, Random.value < 0.5f ? new Color(0.75f, 0.38f, 0.16f) : new Color(1f, 0.5f, 0.25f), 0.09f, 120f);
            }

            // Fix #2: mob kills drop collectible material shards too, not just boss part breaks —
            // elites drop a much richer haul and can bonus-drop a weapon pod.
            int mc = e.IsElite ? 3 + Random.Range(0, 3) : 1 + Random.Range(0, 2);
            for (int i = 0; i < mc; i++) SpawnMatShard(e.X, e.Y);
            PushFloat(e.X, e.Y - 10, "＋素材 ×" + mc, new Color(0.38f, 0.94f, 0.85f), e.IsElite);

            if (e.IsElite && !_podSpawned && Random.value < 0.6f)
            {
                _podSpawned = true; _podT = 0f;
                SpawnGuaranteedPod(e.X, e.Y);
                PushFloat(e.X, e.Y - 24, "武器莢艙掉落！", new Color(1f, 0.85f, 0.3f), true);
            }
        }

        private void FinishWin()
        {
            _resultBossName = _bossDef.Name;
            _resultPartsBroken = _partsBrokenCount;
            _resultTotalParts = _totalNonCoreParts;
            _resultMatCount = _matCount;
            _resultScore = _score;
            _resultKills = _kills;
            _resultTime = _elapsed;
            _resultFullClear = _partsBrokenCount >= _totalNonCoreParts;
            _resultCoreName = _bossDef.CoreName;
            GoToPhase(Phase.Results);
        }

        // ═════════════════════════════════════════════════════════════════════
        // FX: shake / flash / freeze / particles / mats / floats / bg
        // ═════════════════════════════════════════════════════════════════════

        private void ShakeAdd(float a) => _shake = Mathf.Max(_shake, Mathf.Min(24f, a));
        private void FlashAdd(float a) => _flash = Mathf.Max(_flash, a);
        private void TriggerSlowMo(float dur, float minS) { _slowMo = minS; _slowT = dur; }

        private void TickFx(float fxDt)
        {
            _shake = Mathf.Max(0f, _shake - fxDt * 42f);
            _flash = Mathf.Max(0f, _flash - fxDt * 2.6f);
            Vector3 basePos = new Vector3(0, 0, -10);
            _cam.transform.position = basePos + (_shake > 0.3f ? (Vector3)(Random.insideUnitCircle * _shake * 0.5f * WorldScale) : Vector3.zero);

            for (int i = _floats.Count - 1; i >= 0; i--)
            {
                var f = _floats[i]; f.Y += f.Vy * fxDt; f.Life -= fxDt;
                if (f.Life <= 0f) _floats.RemoveAt(i);
            }
        }

        private void MoveParticles(float dt)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Vy += p.Grav * dt; p.X += p.Vx * dt; p.Y += p.Vy * dt; p.Life -= dt;
                if (p.Life <= 0f) { Destroy(p.Go); _particles.RemoveAt(i); continue; }
                p.Go.transform.position = ToWorld(p.X, p.Y);
                var sr = p.Go.GetComponent<SpriteRenderer>();
                var c = sr.color; c.a = Mathf.Clamp01(p.Life / p.Max); sr.color = c;
            }
        }

        private void MoveMats(float dt)
        {
            for (int i = _mats.Count - 1; i >= 0; i--)
            {
                var m = _mats[i];
                if (m.Bursting)
                {
                    m.Vy += 125f * dt; m.X += m.Vx * dt; m.Y += m.Vy * dt; m.T -= dt;
                    if (m.T <= 0f) m.Bursting = false;
                }
                else
                {
                    float dx = CounterAnchorCanvas.x - m.X, dy = CounterAnchorCanvas.y - m.Y;
                    float d = Mathf.Max(0.001f, Mathf.Sqrt(dx * dx + dy * dy));
                    m.X += dx / d * 350f * dt; m.Y += dy / d * 350f * dt;
                    if (d < 6f) { _matCount++; PushFloat(m.X, m.Y - 4, "＋素材", new Color(0.38f, 0.94f, 0.85f), false); Destroy(m.Go); _mats.RemoveAt(i); continue; }
                }
                m.Go.transform.position = ToWorld(m.X, m.Y);
            }
        }

        private void ScrollBackground(float dt)
        {
            foreach (var t in _stars)
            {
                var p = t.position;
                float speed = (2f + t.localScale.x / WorldScale * 0.6f) * WorldScale;
                p.y -= speed * dt * 20f;
                float hH = IH * 0.5f * WorldScale;
                if (p.y < -hH) p.y = hH;
                t.position = p;
            }
        }

        private void BuildStarfield()
        {
            for (int i = 0; i < 40; i++)
            {
                var go = NewQuad("Star", new Color(0.16f, 0.22f, 0.4f, Random.Range(0.15f, 0.4f)), -5);
                go.transform.SetParent(_worldRoot, false);
                float sz = Random.value < 0.25f ? 0.06f : 0.03f;
                go.transform.localScale = new Vector3(sz, sz, 1f);
                go.transform.position = new Vector3(Random.Range(-IW * 0.5f, IW * 0.5f) * WorldScale, Random.Range(-IH * 0.5f, IH * 0.5f) * WorldScale, 5f);
                _stars.Add(go.transform);
            }
        }

        // ── Spawners ───────────────────────────────────────────────────────────

        private void SpawnPBullet(float x, float y, float vx, float vy, float heat, float dmg, bool pierce)
        {
            var b = new PBullet { X = x, Y = y, Vx = vx, Vy = vy, HeatDelta = heat, Dmg = dmg, Pierce = pierce };
            if (pierce) { b.HitParts = new HashSet<int>(); b.HitEnemies = new HashSet<Enemy>(); }
            b.Go = NewQuad("PBullet", new Color(0.49f, 0.98f, 1f), 3);
            b.Go.transform.SetParent(_worldRoot, false);
            b.Go.transform.localScale = ToWorldSize(3, pierce ? 10 : 6);
            b.Go.transform.position = ToWorld(x, y);
            _pbullets.Add(b);
        }

        private void SpawnEBullet(float x, float y, float vx, float vy, float r, Color color)
        {
            var b = new EBullet { X = x, Y = y, Vx = vx, Vy = vy, R = r, Color = color };
            b.Go = NewQuad("EBullet", color, 3);
            b.Go.transform.SetParent(_worldRoot, false);
            b.Go.transform.localScale = ToWorldSize(r * 2.2f, r * 2.2f);
            b.Go.transform.position = ToWorld(x, y);
            _ebullets.Add(b);
        }

        private void SpawnMissile(float x, float y, float vx, float vy, float dmg, WeaponId weapon, bool noHome)
        {
            var m = new Missile { X = x, Y = y, Vx = vx, Vy = vy, Life = noHome ? 1.9f : 3.2f, Dmg = dmg, NoHome = noHome, Weapon = weapon };
            m.Go = NewQuad("Missile", new Color(1f, 0.56f, 0.25f), 3);
            m.Go.transform.SetParent(_worldRoot, false);
            m.Go.transform.localScale = ToWorldSize(4, 8);
            m.Go.transform.position = ToWorld(x, y);
            _missiles.Add(m);
        }

        private Torpedo NewTorpedo(float x, float y, float vy, float dmg)
        {
            var t = new Torpedo { X = x, Y = y, Vy = vy, Life = 4.2f, Dmg = dmg };
            t.Go = NewQuad("Torpedo", new Color(1f, 0.31f, 0.09f), 3);
            t.Go.transform.SetParent(_worldRoot, false);
            t.Go.transform.localScale = ToWorldSize(t.R * 2f, t.R * 3f);
            t.Go.transform.position = ToWorld(x, y);
            return t;
        }

        private ClusterBomb NewCluster(float x, float y, float vx, float vy, float dmg)
        {
            var c = new ClusterBomb { X = x, Y = y, Vx = vx, Vy = vy, Life = 2.2f, Dmg = dmg };
            c.Go = NewQuad("Cluster", new Color(1f, 0.5f, 0.13f), 3);
            c.Go.transform.SetParent(_worldRoot, false);
            c.Go.transform.localScale = ToWorldSize(c.R * 2f, c.R * 2f);
            c.Go.transform.position = ToWorld(x, y);
            return c;
        }

        private void SpawnSparks(float x, float y, Color color, int n)
        {
            for (int i = 0; i < n; i++)
            {
                float a = Random.value * Mathf.PI * 2f, sp = Random.Range(35f, 70f);
                SpawnParticle(x, y, Mathf.Cos(a) * sp, Mathf.Sin(a) * sp - 10f, 0.15f + Random.value * 0.1f, color, 0.08f, 70f);
            }
        }

        private void SpawnDebris(float x, float y, float w, Color hue, int n)
        {
            for (int i = 0; i < n; i++)
            {
                float a = Random.value * Mathf.PI * 2f, sp = 50f + Random.value * 150f;
                Color c = Random.value < 0.5f ? hue : (Random.value < 0.5f ? new Color(1f, 0.95f, 0.75f) : new Color(1f, 0.54f, 0.29f));
                SpawnParticle(x + (Random.value - 0.5f) * w, y, Mathf.Cos(a) * sp, Mathf.Sin(a) * sp - 30f, 0.5f + Random.value * 0.6f, c, Random.value < 0.4f ? 0.16f : 0.1f, 160f);
            }
        }

        private void SpawnParticle(float x, float y, float vx, float vy, float life, Color color, float size, float grav)
        {
            var p = new Particle { X = x, Y = y, Vx = vx, Vy = vy, Life = life, Max = life, Color = color, Size = size, Grav = grav };
            p.Go = NewQuad("Spark", color, 6);
            p.Go.transform.SetParent(_worldRoot, false);
            p.Go.transform.localScale = new Vector3(size, size, 1f);
            p.Go.transform.position = ToWorld(x, y);
            _particles.Add(p);
        }

        private void SpawnMatShard(float x, float y)
        {
            float a = Random.value * Mathf.PI * 2f, sp = 45f + Random.value * 55f;
            var m = new MatShard { X = x, Y = y, Vx = Mathf.Cos(a) * sp, Vy = Mathf.Sin(a) * sp - 35f, T = 0.28f + Random.value * 0.14f, Bursting = true };
            m.Go = NewQuad("Mat", new Color(0.38f, 0.94f, 0.85f), 4);
            m.Go.transform.SetParent(_worldRoot, false);
            m.Go.transform.localScale = ToWorldSize(4, 4);
            m.Go.transform.position = ToWorld(x, y);
            _mats.Add(m);
        }

        private void PushFloat(float x, float y, string text, Color color, bool bold)
            => _floats.Add(new FloatText { X = x, Y = y - 10, Vy = -20, Life = 1.1f, Max = 1.1f, Text = text, Color = color, Bold = bold });

        // ═════════════════════════════════════════════════════════════════════
        // Part visuals
        // ═════════════════════════════════════════════════════════════════════

        private void BuildPartVisuals()
        {
            foreach (var def in _bossDef.Parts)
            {
                int id = _parts.GetPartId(def.Key);
                if (id < 0) continue;
                _parts.SetWorldPosition(id, new Vector2(def.Bx, def.By));

                var go = new GameObject("Part_" + def.Key);
                go.transform.SetParent(_worldRoot, false);
                go.transform.localScale = ToWorldSize(def.W, def.H);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = _sprite; sr.sortingOrder = 2; sr.color = def.Hue;

                var heatBar = MakeBar(go.transform, new Vector3(0, def.H * 0.5f * WorldScale + 0.1f, 0), new Color(1f, 0.55f, 0.1f));
                var breakBar = MakeBar(go.transform, new Vector3(0, -(def.H * 0.5f * WorldScale) - 0.1f, 0), new Color(1f, 0.3f, 0.3f));

                var pr = new PartRuntime { Id = id, Def = def, Cx = def.Bx, Cy = def.By, Go = go, Body = sr, HeatBar = heatBar, BreakBar = breakBar };
                _partsVis[id] = pr;
            }

            // Fix #1: parts exist (real PartStateSystem is live) from the moment the run starts, but
            // they must stay invisible through the whole STAGE (mob-wave) phase — they are only
            // revealed by the boss-entrance transition in UpdateStagePhase (SetActive(true) there).
            foreach (var pr in _partsVis.Values) pr.Go.SetActive(false);
        }

        private Transform MakeBar(Transform parent, Vector3 lp, Color c)
        {
            var go = new GameObject("Bar"); go.transform.SetParent(parent, false);
            go.transform.localPosition = lp; go.transform.localScale = new Vector3(0, 0.05f, 1f);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = _sprite; sr.color = c; sr.sortingOrder = 5;
            return go.transform;
        }

        private void RefreshPartVisuals()
        {
            foreach (var kv in _parts.Parts)
            {
                if (!_partsVis.TryGetValue(kv.Key, out var pr)) continue;
                var part = kv.Value;
                if (part.BreakState == BreakState.Broken) continue;

                if (pr.HeatBar != null) pr.HeatBar.localScale = new Vector3(Mathf.Clamp01(part.HCurrent / part.HMax) * 1.7f, 0.05f, 1f);
                if (pr.BreakBar != null) pr.BreakBar.localScale = new Vector3(Mathf.Clamp01(part.BCurrent / part.BMax) * 1.7f, 0.05f, 1f);

                Color target;
                bool armored = part.PartType == PartType.Armored;
                bool armorIntact = armored && part.ArmorState == ArmorState.Intact;
                if (part.PartType == PartType.BossCore)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(_t * 6f);
                    target = Color.Lerp(new Color(1f, 0.82f, 0.25f), new Color(1f, 0.96f, 0.78f), pulse * 0.4f);
                }
                else if (armorIntact) target = new Color(0.23f, 0.35f, 0.47f);
                else if (part.HeatState == HeatState.Softened) target = new Color(1f, 0.4f, 0f);
                else if (pr.StaggerRemaining > 0f) target = Color.Lerp(pr.Def.Hue, new Color(0.38f, 0.82f, 1f), 0.5f);
                else target = pr.Def.Hue;

                pr.Body.color = pr.FlashT > 0f ? Color.white : target;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // RESULTS
        // ═════════════════════════════════════════════════════════════════════

        private string _resultBossName, _resultCoreName;
        private int _resultPartsBroken, _resultTotalParts, _resultMatCount, _resultScore, _resultKills;
        private float _resultTime; private bool _resultFullClear;

        private void UpdateResultsInput()
        {
            var kb = Keyboard.current; if (kb == null) return;
            if (kb.rKey.wasPressedThisFrame) StartRun();
            if (kb.mKey.wasPressedThisFrame) GoToLoadout();
        }

        // ═════════════════════════════════════════════════════════════════════
        // Phase transitions / cleanup
        // ═════════════════════════════════════════════════════════════════════

        private void GoToLoadout() => GoToPhase(Phase.Loadout);

        private void GoToPhase(Phase p)
        {
            _phase = p;
            if (p == Phase.Loadout || p == Phase.Results)
            {
                _worldRoot.gameObject.SetActive(false);
            }
        }

        private void ClearAllRuntimeEntities()
        {
            foreach (var e in _enemies) if (e.Go != null) Destroy(e.Go); _enemies.Clear();
            foreach (var b in _pbullets) if (b.Go != null) Destroy(b.Go); _pbullets.Clear();
            foreach (var b in _ebullets) if (b.Go != null) Destroy(b.Go); _ebullets.Clear();
            foreach (var m in _missiles) if (m.Go != null) Destroy(m.Go); _missiles.Clear();
            foreach (var t in _torpedoes) if (t.Go != null) Destroy(t.Go); _torpedoes.Clear();
            foreach (var c in _clusters) if (c.Go != null) Destroy(c.Go); _clusters.Clear();
            foreach (var p in _particles) if (p.Go != null) Destroy(p.Go); _particles.Clear();
            foreach (var m in _mats) if (m.Go != null) Destroy(m.Go); _mats.Clear();
            _floats.Clear();
            if (_pod != null) { Destroy(_pod.Go); _pod = null; }
            foreach (var pr in _partsVis.Values) if (pr.Go != null) Destroy(pr.Go);
            _partsVis.Clear();
        }

        // ═════════════════════════════════════════════════════════════════════
        // Scene / content setup
        // ═════════════════════════════════════════════════════════════════════

        private Vector3 ToWorld(float cx, float cy)
            => new Vector3((cx - IW * 0.5f) * WorldScale, (IH * 0.5f - cy) * WorldScale, 0f);

        private Vector3 ToWorldSize(float w, float h) => new Vector3(w * WorldScale, h * WorldScale, 1f);

        private void SetupCamera()
        {
            _cam = Camera.main;
            if (_cam == null) { var go = new GameObject("Main Camera") { tag = "MainCamera" }; _cam = go.AddComponent<Camera>(); }
            _cam.orthographic = true;
            _cam.transform.position = new Vector3(0, 0, -10);
            _cam.backgroundColor = new Color(0.027f, 0.035f, 0.07f);
            _cam.clearFlags = CameraClearFlags.SolidColor;
            RefreshCameraFraming();
        }

        private void RefreshCameraFraming()
        {
            float halfH = IH * 0.5f * WorldScale;
            float halfW = IW * 0.5f * WorldScale;
            _cam.orthographicSize = Mathf.Max(halfH, halfW / Mathf.Max(0.01f, _cam.aspect));
        }

        private void SpawnPlayer()
        {
            var go = new GameObject("Player");
            go.transform.SetParent(_worldRoot, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = _sprite; sr.color = new Color(0.22f, 0.9f, 1f); sr.sortingOrder = 4;
            go.transform.localScale = ToWorldSize(14, 18);
            _playerT = go.transform; _playerSr = sr;
            _playerT.gameObject.SetActive(false);
        }

        private GameObject NewQuad(string name, Color color, int sortOrder)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            // Bullets / projectiles / sparks / stars read far better as soft round dots than squares.
            bool round = name == "PBullet" || name == "EBullet" || name == "Missile" || name == "Torpedo"
                         || name == "Cluster" || name == "Spark" || name == "Star" || name == "Mat";
            sr.sprite = (round && _circle != null) ? _circle : _sprite;
            sr.color = color; sr.sortingOrder = sortOrder;
            return go;
        }

        // Soft rounded-rect ("squircle") with a feathered edge — makes every entity read as a soft
        // neon shape instead of a hard box. pixelsPerUnit = size so the sprite is 1 world unit.
        private static Sprite MakeSprite()
        {
            const int S = 64; var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            float half = (S - 1) * 0.5f, r = S * 0.30f;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = Mathf.Abs(x - half), dy = Mathf.Abs(y - half);
                    float qx = dx - (half - r), qy = dy - (half - r);
                    float dist = (qx > 0f && qy > 0f) ? Mathf.Sqrt(qx * qx + qy * qy) - r : Mathf.Max(qx, qy) - r;
                    float a = Mathf.Clamp01(-dist / 2.5f + 1f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply(); tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
        }

        // Soft-edged filled circle for bullets/sparks.
        private static Sprite MakeCircle()
        {
            const int S = 48; var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            float c = (S - 1) * 0.5f, rad = S * 0.46f;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01((rad - d) / 2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply(); tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
        }

        // Fix #3: distinct-silhouette shape sprites so enemy tiers read apart at a glance.
        // Same soft-edge / pixelsPerUnit=size approach as MakeSprite/MakeCircle, but the inside
        // test is a signed-distance-to-polygon (Inigo Quilez's sdPolygon) instead of a squircle SDF.
        private static float SdPolygon(Vector2 p, Vector2[] v)
        {
            int n = v.Length;
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = n - 1; i < n; j = i, i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        private static Sprite MakeShapeSprite(int size, Vector2[] pts)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dist = SdPolygon(new Vector2(x, y), pts);
                    float a = Mathf.Clamp01(-dist / 2.5f + 1f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply(); tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // Basic mob tier — small diamond.
        private static Sprite MakeDiamond()
        {
            const int S = 64;
            var pts = new[]
            {
                new Vector2(S * 0.5f, S * 0.04f),
                new Vector2(S * 0.92f, S * 0.5f),
                new Vector2(S * 0.5f, S * 0.96f),
                new Vector2(S * 0.08f, S * 0.5f),
            };
            return MakeShapeSprite(S, pts);
        }

        // Elite mob tier — larger hexagon, distinct from basic diamonds and boss squircles.
        private static Sprite MakeHexagon()
        {
            const int S = 64;
            var pts = new Vector2[6];
            float c = (S - 1) * 0.5f, r = S * 0.46f;
            for (int i = 0; i < 6; i++)
            {
                float a = -Mathf.PI / 2f + i * (Mathf.PI * 2f / 6f);
                pts[i] = new Vector2(c + Mathf.Cos(a) * r, c + Mathf.Sin(a) * r);
            }
            return MakeShapeSprite(S, pts);
        }

        private KaijuDef BuildKaijuDef(BossDef bd)
        {
            var k = ScriptableObject.CreateInstance<KaijuDef>();
            SetPrivate(k, "_kaijuId", bd.Id);
            var defs = new PartDef[bd.Parts.Length];
            for (int i = 0; i < bd.Parts.Length; i++) defs[i] = NewPartDef(bd.Parts[i].Key, bd.Parts[i].Type);
            SetPrivate(k, "_parts", defs);
            return k;
        }

        private static PartDef NewPartDef(string id, PartType type)
        {
            var p = new PartDef();
            SetPrivate(p, "_partId", id); SetPrivate(p, "_partType", type);
            SetPrivate(p, "_dropTableId", "proto_drop"); SetPrivate(p, "_adjacency", new string[0]);
            return p;
        }

        private static void SetPrivate(object o, string f, object v)
        {
            var t = o.GetType(); FieldInfo fi = null;
            while (t != null && fi == null) { fi = t.GetField(f, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public); t = t.BaseType; }
            fi?.SetValue(o, v);
        }

        // ═════════════════════════════════════════════════════════════════════
        // OnGUI — LOADOUT / HUD / RESULTS (IMGUI, no scene wiring)
        // ═════════════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            RefreshCameraFraming();
            switch (_phase)
            {
                case Phase.Loadout: DrawLoadout(); break;
                case Phase.Stage:
                case Phase.Boss: DrawHud(); DrawFloats(); break;
                case Phase.Results: DrawResults(); break;
            }
        }

        private GUIStyle Style(int size, FontStyle style = FontStyle.Normal, TextAnchor anchor = TextAnchor.UpperLeft)
            => new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = style, alignment = anchor, wordWrap = false };

        private void DrawLoadout()
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _guiBgTex);
            float W = Screen.width, H = Screen.height;
            GUI.Label(new Rect(0, H * 0.03f, W, 50), "殲獸戰機  KAIJU BREAKER", Style(30, FontStyle.Bold, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(0, H * 0.03f + 40, W, 30), "Vision Slice — 完整循環展示", Style(14, FontStyle.Normal, TextAnchor.MiddleCenter));

            float colW = W * 0.42f;
            float leftX = W * 0.04f, rightX = W * 0.54f;
            float rowY = H * 0.16f, rowH = 46f;

            GUI.Label(new Rect(leftX, rowY - 26, colW, 24), "主武器 PRIMARY [1-4]", Style(15, FontStyle.Bold));
            for (int i = 0; i < Primaries.Length; i++)
            {
                var r = new Rect(leftX, rowY + i * (rowH + 4), colW, rowH);
                bool sel = _choicePrimary == i;
                GUI.backgroundColor = sel ? new Color(0.2f, 0.55f, 0.75f) : new Color(0.2f, 0.2f, 0.24f);
                if (GUI.Button(r, "")) _choicePrimary = i;
                GUI.Label(new Rect(r.x + 8, r.y + 3, r.width - 16, 20), (sel ? "✓ " : "") + Primaries[i].Name, Style(14, FontStyle.Bold));
                GUI.Label(new Rect(r.x + 8, r.y + 23, r.width - 16, 20), Primaries[i].Niche, Style(11));
            }

            GUI.Label(new Rect(rightX, rowY - 26, colW, 24), "副武器 SECONDARY [5-8]", Style(15, FontStyle.Bold));
            for (int i = 0; i < Secondaries.Length; i++)
            {
                var r = new Rect(rightX, rowY + i * (rowH + 4), colW, rowH);
                bool sel = _choiceSecondary == i;
                GUI.backgroundColor = sel ? new Color(0.75f, 0.45f, 0.2f) : new Color(0.24f, 0.2f, 0.18f);
                if (GUI.Button(r, "")) _choiceSecondary = i;
                GUI.Label(new Rect(r.x + 8, r.y + 3, r.width - 16, 20), (sel ? "✓ " : "") + Secondaries[i].Name, Style(14, FontStyle.Bold));
                GUI.Label(new Rect(r.x + 8, r.y + 23, r.width - 16, 20), Secondaries[i].Niche, Style(11));
            }

            float diffY = rowY + 4 * (rowH + 4) + 20;
            GUI.Label(new Rect(leftX, diffY - 24, W * 0.9f, 22), "難度 DIFFICULTY [Q/E]", Style(15, FontStyle.Bold));
            float diffW = (W * 0.92f) / 4f;
            for (int i = 0; i < Difficulties.Length; i++)
            {
                var r = new Rect(leftX + i * diffW, diffY, diffW - 6, 44);
                bool sel = _choiceDifficulty == i;
                GUI.backgroundColor = sel ? new Color(0.7f, 0.65f, 0.2f) : new Color(0.2f, 0.2f, 0.18f);
                if (GUI.Button(r, "")) _choiceDifficulty = i;
                GUI.Label(new Rect(r.x + 4, r.y + 2, r.width - 8, 20), Difficulties[i].Label, Style(13, FontStyle.Bold, TextAnchor.MiddleCenter));
                GUI.Label(new Rect(r.x + 4, r.y + 22, r.width - 8, 18), Difficulties[i].Desc, Style(10, FontStyle.Normal, TextAnchor.MiddleCenter));
            }

            float bossY = diffY + 60;
            GUI.Label(new Rect(leftX, bossY - 24, W * 0.9f, 22), "目標 TARGET [Z/X/C]", Style(15, FontStyle.Bold));
            string[] bossNames = { "鎧殼獸 CARAPEX", "刃肢獸 LACERA", "熾蛇 VOLTWYRM" };
            string[] bossSub = { "甲殼重裝・L2×M3推薦", "移動四肢・M1追蹤推薦", "縱列蛇身・L4穿透推薦" };
            float bossW = (W * 0.92f) / 3f;
            for (int i = 0; i < BossIds.Length; i++)
            {
                var r = new Rect(leftX + i * bossW, bossY, bossW - 6, 50);
                bool sel = _choiceBoss == BossIds[i];
                GUI.backgroundColor = sel ? new Color(0.65f, 0.28f, 0.2f) : new Color(0.2f, 0.15f, 0.13f);
                if (GUI.Button(r, "")) _choiceBoss = BossIds[i];
                GUI.Label(new Rect(r.x + 4, r.y + 2, r.width - 8, 22), bossNames[i], Style(13, FontStyle.Bold, TextAnchor.MiddleCenter));
                GUI.Label(new Rect(r.x + 4, r.y + 26, r.width - 8, 20), bossSub[i], Style(10, FontStyle.Normal, TextAnchor.MiddleCenter));
            }
            GUI.backgroundColor = Color.white;

            float sumY = bossY + 66;
            var bd = MakeBossDef(_choiceBoss);
            GUI.Label(new Rect(leftX, sumY, W * 0.9f, 24),
                Primaries[_choicePrimary].Name + " ＋ " + Secondaries[_choiceSecondary].Name, Style(14, FontStyle.Bold));
            GUI.Label(new Rect(leftX, sumY + 22, W * 0.9f, 22),
                "vs " + bd.Name + " | " + bd.ShineWeapon + " | 密度 ×" + Difficulties[_choiceDifficulty].Mult.ToString("0.0"), Style(12));

            var startRect = new Rect(W * 0.5f - 90, sumY + 56, 180, 44);
            GUI.backgroundColor = new Color(0.15f, 0.4f, 0.55f);
            if (GUI.Button(startRect, "出發 START")) StartRun();
            GUI.backgroundColor = Color.white;

            GUI.Label(new Rect(0, H - 26, W, 22), "1-4主武 5-8副武 Q/E難度 Z/X/C目標 Enter開始", Style(11, FontStyle.Normal, TextAnchor.MiddleCenter));
        }

        private void DrawHud()
        {
            float W = Screen.width;

            string phaseLabel = _phase == Phase.Boss
                ? ("破壞 " + _partsBrokenCount + "/" + _totalNonCoreParts)
                : (_stageSub == StageSub.Wave1 ? "第一波雜兵" : _stageSub == StageSub.Wave2 ? "第二波雜兵" : "武器莢艙窗口");
            GUI.Label(new Rect(10, 6, 260, 22), phaseLabel, Style(14, FontStyle.Bold));
            GUI.Label(new Rect(10, 26, 260, 20), "分數 " + _score + "  (" + _kills + " 擊殺)", Style(12));
            GUI.Label(new Rect(10, 44, 260, 20), "HP " + Mathf.CeilToInt(Mathf.Max(0, _phpCur)) + "/" + (int)PlayerMaxHp, Style(12));
            var hpRect = new Rect(10, 62, 140, 8);
            GUI.Box(hpRect, "");
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            GUI.Box(new Rect(hpRect.x, hpRect.y, hpRect.width * Mathf.Clamp01(_phpCur / PlayerMaxHp), hpRect.height), "");
            GUI.backgroundColor = Color.white;

            if (_phase == Phase.Boss && _bossDef != null)
            {
                foreach (var kv in _parts.Parts)
                {
                    if (kv.Value.PartType != PartType.BossCore) continue;
                    float frac = 1f - Mathf.Clamp01(kv.Value.BCurrent / kv.Value.BMax);
                    var r = new Rect(W * 0.5f - 130, 8, 260, 10);
                    GUI.Box(r, "");
                    GUI.backgroundColor = new Color(1f, 0.82f, 0.25f);
                    GUI.Box(new Rect(r.x, r.y, r.width * frac, r.height), "");
                    GUI.backgroundColor = Color.white;
                    GUI.Label(new Rect(W * 0.5f - 130, 20, 260, 18), _bossDef.Name + " " + _bossDef.NameEn, Style(11, FontStyle.Bold, TextAnchor.MiddleCenter));
                }
            }

            GUI.Label(new Rect(W - 130, 6, 120, 20), "素材 " + _matCount, Style(14, FontStyle.Bold, TextAnchor.MiddleRight));

            float hudBaseY = Screen.height - 96;
            GUI.Label(new Rect(10, hudBaseY, 220, 20), "P: " + Primaries[_pidx].Name, Style(13, FontStyle.Bold));
            GUI.Label(new Rect(10, hudBaseY + 18, 260, 18), Primaries[_pidx].Niche, Style(10));
            if (_pidx == 2)
            {
                var r = new Rect(10, hudBaseY + 38, 160, 8);
                GUI.Box(r, "");
                if (_l3Charging)
                {
                    float fr = Mathf.Clamp01(_l3Charge / 1.5f);
                    GUI.backgroundColor = fr >= 1f ? Color.white : new Color(0.38f, 0.82f, 1f);
                    GUI.Box(new Rect(r.x, r.y, r.width * fr, r.height), "");
                    GUI.backgroundColor = Color.white;
                    GUI.Label(new Rect(10, hudBaseY + 48, 220, 16), _l3Charge >= 1.5f ? "波動砲 READY — 放開 Z" : "蓄能中...", Style(10));
                }
                else if (_l3Cooldown > 0f)
                {
                    GUI.backgroundColor = new Color(0.15f, 0.3f, 0.4f);
                    GUI.Box(new Rect(r.x, r.y, r.width * (1f - _l3Cooldown / 2.0f), r.height), "");
                    GUI.backgroundColor = Color.white;
                    GUI.Label(new Rect(10, hudBaseY + 48, 220, 16), "冷卻 " + _l3Cooldown.ToString("0.0") + "s", Style(10));
                }
                else GUI.Label(new Rect(10, hudBaseY + 48, 220, 16), "L3 READY [Z hold]", Style(10));
            }

            var swd = Secondaries[_sidx]; var ws = _ws != null ? _ws[_sidx] : null;
            GUI.Label(new Rect(W - 230, hudBaseY, 220, 20), "S: " + swd.Name, Style(13, FontStyle.Bold, TextAnchor.UpperRight));
            if (ws != null)
            {
                var pipR = new Rect(W - 230, hudBaseY + 20, 220, 10);
                if (ws.Reloading)
                {
                    GUI.Box(pipR, "");
                    GUI.backgroundColor = new Color(1f, 0.56f, 0.25f);
                    GUI.Box(new Rect(pipR.x, pipR.y, pipR.width * (1f - ws.ReloadT / swd.Reload), pipR.height), "");
                    GUI.backgroundColor = Color.white;
                    GUI.Label(new Rect(W - 230, hudBaseY + 32, 220, 16), "換彈 " + ws.ReloadT.ToString("0.0") + "s", Style(10, FontStyle.Normal, TextAnchor.UpperRight));
                }
                else
                {
                    GUI.Label(new Rect(W - 230, hudBaseY + 20, 220, 16), ws.Ammo + " / " + swd.Mag, Style(12, FontStyle.Normal, TextAnchor.UpperRight));
                }
            }

            GUI.Label(new Rect(10, Screen.height - 22, 200, 20), Difficulties[_choiceDifficulty].Label, Style(10));
            GUI.Label(new Rect(W - 210, Screen.height - 22, 200, 20), _elapsed.ToString("0.0") + "s", Style(10, FontStyle.Normal, TextAnchor.UpperRight));
            GUI.Label(new Rect(0, Screen.height - 22, W, 20), "R=返回選單  1-4/5-8換武器  空白鍵=副武器  Z蓄L3", Style(10, FontStyle.Normal, TextAnchor.UpperCenter));
        }

        private void DrawFloats()
        {
            foreach (var f in _floats)
            {
                float a = Mathf.Clamp01(f.Life * 1.4f);
                var world = ToWorld(f.X, f.Y);
                var sp = _cam.WorldToScreenPoint(world);
                if (sp.z < 0f) continue;
                var gp = new Vector2(sp.x, Screen.height - sp.y);
                var c = f.Color; c.a = a;
                var style = Style(f.Bold ? 15 : 12, f.Bold ? FontStyle.Bold : FontStyle.Normal, TextAnchor.MiddleCenter);
                style.normal.textColor = c;
                GUI.Label(new Rect(gp.x - 120, gp.y - 12, 240, 24), f.Text, style);
            }
        }

        private void DrawResults()
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _guiBgTex);
            float W = Screen.width, H = Screen.height;
            GUI.Label(new Rect(0, H * 0.06f, W, 50), "狩獵完成", Style(30, FontStyle.Bold, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(0, H * 0.06f + 42, W, 30), _resultBossName + "  DEFEATED", Style(16, FontStyle.Normal, TextAnchor.MiddleCenter));

            var boxR = new Rect(W * 0.15f, H * 0.22f, W * 0.7f, H * 0.42f);
            GUI.Box(boxR, "");
            string[] labels =
            {
                "破壞部位", "素材回收", "擊殺分數", _resultCoreName, "用時", "難度", "全部位破壞"
            };
            string[] values =
            {
                _resultPartsBroken + " / " + _resultTotalParts,
                _resultMatCount + " 個",
                _resultScore + "（" + _resultKills + " 擊殺）",
                "× " + _resultPartsBroken + " 顆",
                _resultTime.ToString("0.0") + " 秒",
                Difficulties[_choiceDifficulty].Label,
                _resultFullClear ? "達成 精魄 +1" : "未達成"
            };
            for (int i = 0; i < labels.Length; i++)
            {
                float y = boxR.y + 10 + i * (boxR.height - 20) / labels.Length;
                GUI.Label(new Rect(boxR.x + 12, y, boxR.width * 0.5f, 24), labels[i], Style(13));
                GUI.Label(new Rect(boxR.x + boxR.width * 0.5f, y, boxR.width * 0.5f - 12, 24), values[i], Style(13, FontStyle.Bold, TextAnchor.UpperRight));
            }

            string msg = _resultFullClear ? "全破壞達成！精魄入袋。你是真正的獵人。"
                : _resultPartsBroken >= Mathf.CeilToInt(_resultTotalParts * 0.5f) ? "你選擇了狩獵，而不只是擊殺。"
                : _resultPartsBroken >= 1 ? "你拆了一個——還想再拆嗎？" : "你直接衝核心過關了。";
            GUI.Label(new Rect(0, boxR.y + boxR.height + 16, W, 26), msg, Style(13, FontStyle.Normal, TextAnchor.MiddleCenter));

            var r1 = new Rect(W * 0.5f - 190, boxR.y + boxR.height + 56, 170, 44);
            var r2 = new Rect(W * 0.5f + 20, boxR.y + boxR.height + 56, 170, 44);
            GUI.backgroundColor = new Color(0.15f, 0.4f, 0.55f);
            if (GUI.Button(r1, "再來一次 (R)")) StartRun();
            GUI.backgroundColor = new Color(0.5f, 0.3f, 0.15f);
            if (GUI.Button(r2, "回選單 (M)")) GoToLoadout();
            GUI.backgroundColor = Color.white;

            GUI.Label(new Rect(0, H - 26, W, 22), "R = 再挑戰　M = 選單", Style(11, FontStyle.Normal, TextAnchor.MiddleCenter));
        }
    }
}
