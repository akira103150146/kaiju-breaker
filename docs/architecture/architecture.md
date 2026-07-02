# 技術架構總藍圖 (Master Technical Architecture)
## 殲獸戰機 / KAIJU BREAKER

*文件路徑：docs/architecture/architecture.md*
*最後更新：2026-07-01*
*狀態：Proposed — Pre-Production 啟動；效能相關承諾待原型 LOCK*
*作者：Technical Director Agent*
*引擎：Unity 6.3 LTS（C#）*

---

## 0. 目的與範圍 (Purpose & Scope)

本文件是殲獸戰機所有程式系統的**最高層架構藍圖 (high-level architecture blueprint)**。它把 11 份 GDD 設計系統映射到具體的程式模組、組件定義 (assembly definition, `.asmdef`)、Unity 專案佈局、場景結構、執行期資料流與效能預算，並以**可追溯性矩陣 (traceability matrix)** 把每個設計系統連到其治理的架構決策記錄 (ADR)。

**設計驅動的三條硬約束（來自 `.claude/docs/coding-standards.md`，貫穿全架構）：**

1. **資料驅動、零硬編碼 (Data-driven, no hardcoding)**：所有可調數值以 ScriptableObject 資產表達（見 §7、ADR-0003）。
2. **DI 優於單例 (DI over singletons)**：系統間以介面注入與事件匯流排溝通，可獨立單元測試（見 §5、ADR-0002、ADR-0005）。
3. **每個系統有 ADR**：本文件 + 5 份核心 ADR 覆蓋 MVP 全部系統；後續系統以 `/architecture-decision` 補充。

**專案第一技術風險（來自 `game-concept.md`）：手機彈幕效能——同畫面上千子彈 × 觸控 × 手機。** 整個架構的旗艦決策（ADR-0001）與效能策略（§8）都圍繞它組織。

---

## 1. 技術棧 (Technology Stack)

| 層面 | 選型 | 理由 / 備註 |
|------|------|------------|
| **引擎 (Engine)** | Unity 6.3 LTS | 手機生態成熟、2D 工具完整、與 Sky Force 類標竿對齊；為近年首個含**生產就緒 DOTS/Entities 1.3+** 的 LTS（見 `docs/engine-reference/unity/VERSION.md`）|
| **語言 (Language)** | C#（C# 9，Unity 6 預設）| — |
| **渲染 (Rendering)** | URP 2D (Universal Render Pipeline) + 2D Renderer + Pixel Perfect Camera | 像素街機風；單 sprite atlas 批次；暖/冷色彈幕可讀性 |
| **彈幕模擬 (Bullet Sim)** | **DOTS/ECS + Burst + Jobs**（Entities 1.3+）| 敵彈：大量同質、每幀相同數學的 DOP 負載（見 ADR-0001）|
| **遊戲/UI 物件** | MonoBehaviour + 物件池 (object pooling) | 玩家、部位、武器、UI、少量特例（見 ADR-0001 混合策略）|
| **輸入 (Input)** | Input System package（新版，Unity 6 預設）| 三方案（觸控 / 鍵鼠 / 手柄）抽象動作映射（見 §4、`input-system.md`）|
| **資產載入 (Asset Loading)** | Addressables | 場景、巨獸、關卡內容以群組管理與非同步載入（見 §3.4）|
| **UI 框架** | **ADR-0006**:世界座標血條 = `SpriteRenderer`；in-combat HUD + meta 畫面 = UGUI | UI Toolkit 為 post-MVP 再評估項 |
| **靜態調校資料** | ScriptableObject（唯讀，設計師撰寫）| 取代 GDD 中提及的 `assets/data/**/*.yaml` 佔位路徑（見 ADR-0003）|
| **存檔資料** | JSON + 原子寫入 + CRC32（`Application.persistentDataPath`）| 玩家可變狀態；與靜態調校資料分離（見 ADR-0004）|
| **測試 (Testing)** | Unity Test Framework (UTF / NUnit)，EditMode + PlayMode | CI：`game-ci/unity-test-runner@v4`（見 `coding-standards.md`）|
| **版本控制** | Git（trunk-based）；LFS 管理 sprite/audio 二進位資產 | — |

> **版本不確定性標記 (Version Uncertainty Flag)**：LLM 知識截止約 Unity 2022.3；Unity 6.0–6.3 對 **Entities/DOTS API、Input System、URP、Addressables** 均有重大變更。本文件標示 **[需查證 6.3 API]** 之處，實作前務必查 `docs/engine-reference/unity/VERSION.md` 與官方文件，**不得臆造 API 簽名**。具體已知風險點見 §9。

---

## 2. 模組地圖與組件定義 (Module Map & Assembly Definitions)

每個 GDD 系統映射到一個程式模組，每個模組是一個 `.asmdef`。組件邊界即**測試邊界**與**依賴邊界**——系統之間**不得直接引用彼此**，僅透過 `Core` 定義的事件匯流排與查詢介面溝通（落實 DI、可測試性；完整規則見 ADR-0005）。

### 2.1 組件清單

| 組件 (`.asmdef`) | 對應 GDD 系統 | 職責 | 依賴 |
|------------------|--------------|------|------|
| **`KaijuBreaker.Core`** | 橫切 | 事件匯流排介面、DI 容器/服務定位、遊戲/Run 狀態機、共用型別 (`WeaponId`, `PartType`, `BreakQuality`, `DifficultyTier`…)、查詢介面、數學工具 | 無（僅 UnityEngine）|
| **`KaijuBreaker.Content`** | 全系統的調校資料 | 所有 ScriptableObject 定義：`WeaponDef`、`PartDef`、`KaijuDef`、`DifficultyConfig`、`GameFeelConfig`、`EmitterPatternSO`、`StageDef`、`EconomyConfig`、`SaveConfig`、`InputSettings` | Core |
| **`KaijuBreaker.BulletSim`** | S9 彈幕引擎 | **DOTS/ECS**：敵彈生成/模擬/碰撞、空間網格廣相、Emitter Blob 烘焙、DOTS↔Mono 事件橋 | Core, Content, Unity.Entities, Burst, Collections, Mathematics |
| **`KaijuBreaker.Weapons`** | S1 武器系統 | 雷射 raycast/overlap、飛彈池、蓄熱/破甲輸出、Tier 效果、發 `on_laser_hit`/`on_missile_hit`/`on_l3_wave_hit` | Core, Content |
| **`KaijuBreaker.KaijuParts`** | S2 可破壞部位 | 部位狀態機、相鄰圖、STAGGERED/護甲閘門、發 `on_part_softened…`/`on_part_break`/`on_boss_core_break`；實作 `IPartStateQuery` | Core, Content |
| **`KaijuBreaker.Economy`** | S3 素材經濟 | 素材產量公式、升級成本、Tier 授予；消費 `on_part_break`/`on_hunt_end` | Core, Content |
| **`KaijuBreaker.Meta`** | S10 元進度/存檔 | JSON 序列化、原子寫入、CRC32、版本遷移、背景存檔 worker、武器所有權 | Core, Content |
| **`KaijuBreaker.Stage`** | S4 關卡/波段 + Run 流程 | 波段隨機重組、莢艙保底掉落、**Run 狀態機 (LOADOUT→STAGE→BOSS→RESULTS)** 驅動、雜兵生成 | Core, Content, Difficulty |
| **`KaijuBreaker.Difficulty`** | S5 難度系統 | 提供 `enemy_count_mult`/`bullet_density_mult`；唯讀密度來源 (single source of truth) | Core, Content |
| **`KaijuBreaker.Input`** | S6 輸入系統 | 三方案抽象動作、觸控相對偏移拖曳、L3 蓄力事件、重映射 | Core, Content |
| **`KaijuBreaker.GameFeel`** | S8 打擊感 | 頓幀/慢動作/螢幕震動/閃光、SOFTENED 簽章、素材軌道球；消費部位事件 | Core, Content |
| **`KaijuBreaker.UI`** | S7 HUD/UI | 世界座標部位血條、武器 HUD、素材計數、Loadout/升級/難度三畫面；訂閱事件、查詢介面 | Core, Content |
| **`KaijuBreaker.App`** (Bootstrap) | 組合根 (composition root) | DI 佈線、場景載入、把各系統接到事件匯流排；**唯一**引用全部系統的組件 | 全部 |
| **`KaijuBreaker.*.Tests`** | 每系統一個 | EditMode/PlayMode 測試；注入假資料 (fixture SO) | 對應系統 + Core + Content + NUnit |

### 2.2 依賴圖 (Dependency Graph)（鏡射 `systems-index.md` §2，但以事件解耦）

```
                         KaijuBreaker.App  (composition root — 引用全部)
                                  │  佈線 DI + 事件匯流排 + 場景載入
   ┌──────────┬──────────┬────────┼─────────┬──────────┬──────────┐
   ▼          ▼          ▼        ▼         ▼          ▼          ▼
 Input     Stage ───► Difficulty  Weapons  KaijuParts  Economy   Meta   BulletSim  GameFeel  UI
   │          │   讀密度乘數        │   事件    │  事件      │事件    │事件      │           │
   │          │                    └──►(on_*_hit)──►KaijuParts──►(on_part_break)──►Economy/Meta
   │          │                                       │
   └──────────┴───────────────── 全部僅依賴 ──────────┴──────────────────────────┐
                                    ▼                                            ▼
                          KaijuBreaker.Content  (ScriptableObject 定義) ───► KaijuBreaker.Core
```

**關鍵解耦原則**：`Weapons` **不引用** `KaijuParts`；命中透過 `on_laser_hit`/`on_missile_hit` 事件（payload 契約權威在 GDD）跨越。反向查詢（部位 `heat_state`、`world_position`）走 `Core` 定義的 `IPartStateQuery` 介面注入。此設計讓每個系統可用假事件/假查詢單元測試（`coding-standards.md`：DI over singletons）。

---

## 3. Unity 專案佈局 (Project Layout)

### 3.1 資產資料夾結構

```
Assets/
└── _Project/                        # 全部第一方資產（底線前綴使其排在頂部）
    ├── Scripts/
    │   ├── Core/                     # KaijuBreaker.Core.asmdef
    │   ├── Content/                  # KaijuBreaker.Content.asmdef（SO 定義類別）
    │   ├── BulletSim/                # KaijuBreaker.BulletSim.asmdef（ECS systems/components/jobs）
    │   ├── Weapons/                  # KaijuBreaker.Weapons.asmdef
    │   ├── KaijuParts/               # KaijuBreaker.KaijuParts.asmdef
    │   ├── Economy/                  # KaijuBreaker.Economy.asmdef
    │   ├── Meta/                     # KaijuBreaker.Meta.asmdef
    │   ├── Stage/                    # KaijuBreaker.Stage.asmdef（含 RunController 狀態機）
    │   ├── Difficulty/              # KaijuBreaker.Difficulty.asmdef
    │   ├── Input/                    # KaijuBreaker.Input.asmdef
    │   ├── GameFeel/                 # KaijuBreaker.GameFeel.asmdef
    │   ├── UI/                       # KaijuBreaker.UI.asmdef
    │   └── App/                      # KaijuBreaker.App.asmdef（Bootstrap/組合根）
    ├── Content/                      # ★ 所有 ScriptableObject 資產（取代 GDD 的 assets/data/*.yaml）
    │   ├── Weapons/                  # L1..L4, M1..M4 的 WeaponDef.asset + 全域 WeaponBalanceConfig
    │   ├── Parts/                    # PartSystemConfig（H_max, B_max, theta_S…）
    │   ├── Kaiju/                    # carapex/lacera/voltwyrm 的 KaijuDef + 部位 + EmitterPatternSO
    │   ├── Difficulty/              # DifficultyConfig（乘數表，唯一來源）
    │   ├── Economy/                  # EconomyConfig（產量/成本旋鈕）
    │   ├── Stages/                   # StageDef + SegmentDef + PodDropConfig
    │   ├── GameFeel/                 # GameFeelConfig（震動/慢動作/頓幀/SOFTENED）
    │   ├── Input/                    # InputSettings 預設
    │   └── Meta/                     # SaveConfig
    ├── Scenes/                       # Bootstrap / MetaHub / Run / StageArenas / BossArenas
    ├── Art/                          # sprites, atlases（單一敵彈 atlas 見 §8.4）, animations
    ├── Audio/                        # BGM, SFX
    ├── Prefabs/                      # 玩家、雜兵、莢艙、UI、部位 prefab
    ├── VFX/                          # 粒子、shader（脈動光暈、SOFTENED 色偏）
    └── Settings/                     # URP asset, Input Actions asset, Addressables 設定, Quality
```

### 3.2 組件定義放置

每個 `Scripts/<Module>/` 根目錄放一個 `.asmdef`。測試組件放 `Assets/_Project/Tests/<Module>/`（EditMode）與 `Assets/_Project/Tests/PlayMode/`（需要 ECS World / 場景者）。ECS 相關組件必須引用 Unity.Entities/Burst/Collections/Mathematics（僅 `BulletSim` 這麼做，隔離 DOTS 學習成本與編譯依賴）。

### 3.3 命名慣例（寫入 `.claude/docs/technical-preferences.md` 供各專員遵循）

| 種類 | 慣例 | 範例 |
|------|------|------|
| 類別 (Class) | PascalCase | `PartStateMachine`, `BulletSimulationSystem` |
| 方法/屬性 | PascalCase | `TryUpgrade`, `CurrentHeat` |
| 私有欄位 | `_camelCase` | `_activeBullets` |
| 常數 | PascalCase | `MaxConcurrentBulletsMobile` |
| ScriptableObject 資產 | `PascalCase.asset` | `L2_FocusBeam.asset` |
| 事件/訊號 | GDD 契約名（snake 對映 C# PascalCase 事件） | `on_part_break` → `PartBroke` |
| ECS Component | PascalCase struct + `IComponentData` | `BulletVelocity`, `BulletLifetime` |

### 3.4 Addressables 群組

| 群組 | 內容 | 載入時機 |
|------|------|---------|
| `core_boot` | Bootstrap 場景 + 全域 config SO | 啟動時（可內建） |
| `meta` | MetaHub 場景 + UI 資產 | 進入元介面 |
| `stage_common` | Run 場景骨架 + 玩家 prefab + 敵彈 atlas + 雜兵 | 進入 Run |
| `kaiju_<id>` | 各巨獸 prefab/部位/Emitter Blob 來源/動畫 | 該關卡 Boss Arena 前預載 |
| `stage_<id>` | 各關卡環境/波段資產 | 選定關卡時 |

巨獸與關卡以獨立群組，支援「內容按需載入」與未來 DLC/擴充巨獸；`kaiju_<id>` 在前頭目喘息 (Pre-Boss Lull) 開始時非同步預載，避免 Boss 入場卡頓。

---

## 4. 場景架構 (Scene Architecture)

### 4.1 場景清單與載入模式

| 場景 | 載入模式 | 生命週期 | 內容 |
|------|---------|---------|------|
| **Bootstrap** | Single（第一個載入）| **常駐 (persistent)** | DI 容器、服務、事件匯流排、存檔載入、Addressables 初始化；不含遊戲畫面 |
| **MetaHub** | Additive（Bootstrap 之上） | 進元介面時 | Loadout / 永久升級 / 難度選擇三畫面（UI）|
| **Run** | Additive | 一輪遊玩 | Run 骨架 + 玩家 + HUD + BulletSim ECS World host |
| **StageArena_\<id\>** | Additive（Run 之上）| 波段階段 | 關卡環境、雜兵生成點 |
| **BossArena_\<kaiju\>** | Additive（Run 之上）| 頭目階段 | 巨獸 prefab、部位、Emitter |

**策略**：Bootstrap 常駐持有跨場景服務（存檔、事件匯流排、Run 狀態機、DI）。MetaHub 與 Run **互斥切換**（卸一載一）。Run 內部以**附加子場景**串接 StageArena → BossArena，讓喘息期能非同步預載 Boss 而不中斷。全部場景切換走 Addressables 非同步 API。

### 4.2 Run 狀態機 (Run State Machine)

`RunController`（位於 `Stage`，狀態列舉於 `Core`）驅動一輪的生命週期：

```
        [MetaHub 產出 RunConfig(loadout, difficulty, stageId)]
                              │
            ┌─────────────────▼─────────────────┐
            │  LOADOUT  → 讀 last_loadout/難度，   │  (元介面內完成；on_loadout_confirmed → autosave)
            │            玩家確認出發              │
            └─────────────────┬─────────────────┘
                              ▼  載入 Run + StageArena
            ┌─────────────────────────────────────┐
            │  STAGE   → 波段隨機重組、雜兵波、      │  BulletSim 中密度；莢艙保底掉落
            │            莢艙拾取(→所有權 autosave)  │
            └─────────────────┬─────────────────┘
                              ▼  Pre-Boss Lull（預載 kaiju_<id>）
            ┌─────────────────────────────────────┐
            │  BOSS    → KaijuParts 接管命中事件、   │  BulletSim 高密度（第一風險熱點）
            │            部位破壞→素材(autosave)     │
            └────────┬──────────────────┬─────────┘
             勝利(on_boss_core_break)   失敗/放棄
                     ▼                  ▼
            ┌─────────────────────────────────────┐
            │  RESULTS → 結算素材、完成度精魄、      │  on_hunt_end → autosave；可進升級
            │            更新 kaiju_records          │
            └─────────────────┬─────────────────┘
                              ▼  返回 MetaHub 開始下一輪
```

狀態轉換與存檔觸發點與 `meta-progression-system.md` C.8 跨輪流程、`stage-system.md` C 關卡解剖完全對齊。狀態機本身是純 C# 類別，可用假事件驅動做 PlayMode 測試。

---

## 5. 執行期架構 (Runtime Architecture)

### 5.1 遊戲迴圈與時間 (Game Loop & Time)

| 時鐘 | 用途 | 受 `Time.timeScale` 影響？ |
|------|------|--------------------------|
| `Time.deltaTime`（scaled） | 巨獸動畫、敵彈模擬、玩家彈、粒子世界時間 | 是（頓幀/慢動作作用於此）|
| `Time.unscaledDeltaTime` | 玩家輸入輪詢、UI、螢幕震動計算、閃光淡出 | 否（`game-feel.md` C.5：頓幀不得吃掉閃避輸入）|
| `AudioSource` 時鐘 | SFX 播放 | 否（Unity 內建，播放不隨 timeScale 縮速）|
| ECS `SystemState` 時間 | 彈幕模擬 | 見 §5.4：BulletSim 讀取 scaled deltaTime 以隨頓幀凍結敵彈 |

**頓幀/慢動作實作**：`GameFeel` 以 `Time.timeScale = 0`（頓幀）/ `= 0.12`（慢動作）驅動 scaled 世界；輸入與 UI 走 unscaled。ECS 彈幕系統把 `Time.timeScale` 併入其 `deltaTime` 來源，使 `time_scale=0` 時敵彈靜止（`game-feel.md` I.2 阻斷驗收）。**[需查證 6.3 API]** Entities 1.3 時間注入方式。

### 5.2 雙軌 soften→break 事件流 (The Dual-Track Event Flow)

系統核心互動——武器↔部位↔素材↔存檔——完全以事件串接，這是全架構最關鍵的資料流：

```
[玩家自動開火/副武器輸入]
        │
        ▼
  Weapons（雷射 raycast / 飛彈池命中部位）
        │  發事件（payload 契約權威見 weapon-system.md F.1 / kaiju-part-system.md C.5）
        ├── on_laser_hit(part_id, kaiju_id, heat_delta) ─────────┐
        ├── on_missile_hit(part_id, kaiju_id, break_delta_base, weapon_id) ─┐
        └── on_l3_wave_hit(part_id, kaiju_id) ───────────────────┤        │
                                                                 ▼        ▼
                                                        KaijuParts（狀態機）
                                             蓄熱 H → SOFTENED；破甲 B →（護甲閘門/STAGGERED）
                                                                 │
                          ┌──────────────────────────────────────┼───────────────────────────┐
                          ▼（狀態變更事件）                        ▼（破壞終態）                 ▼（查詢介面）
                 on_part_softened / _exit              on_part_break(                  IPartStateQuery:
                 on_part_staggered / _end               part_id, part_type,            heat_state, world_position
                          │                              break_quality, world_pos,      (Weapons 追蹤/Tier-3、
                          ▼                              drop_table_id, adjacency,        UI 血條讀取)
                   GameFeel / UI                         is_chain_break)
                 (SOFTENED 簽章、頓幀…)                        │
                                                ┌──────────────┼───────────────┬───────────────┐
                                                ▼              ▼               ▼               ▼
                                            Economy        Meta(Save)       GameFeel         Weapons
                                        (依 break_quality  (即時入帳素材     (115ms 頓幀→     (清碰撞體、
                                         算 shard/core     +enqueue autosave  慢動作→碎片→     L2 Tier-3 漣漪、
                                         yield)             見 ADR-0004)      素材軌道球)      M3 Tier-3 鏈)
                                                                                              │
                                          BOSS_CORE 破壞時額外：on_boss_core_break ──► RunController(→RESULTS)
```

**`break_quality` 是雙軌技術表現→獎勵的關鍵載體**：於破壞成立那一幀由 `KaijuParts` 依 `heat_state`/`stagger_timer` 計算（`NORMAL`/`SOFTENED`/`SOFTENED_STAGGERED`），`Economy` 據此決定素材倍率。事件契約與所有權見 ADR-0002。

### 5.3 DOTS 彈幕 ↔ MonoBehaviour 遊戲 邊界 (The Bridge)

這是混合架構（ADR-0001）最關鍵的整合點。敵彈活在 ECS World，遊戲/部位/玩家活在 MonoBehaviour 世界。兩側每幀以**明確的同步點 (sync point)** 交換資料，避免隱性耦合：

```
每幀資料流：

 MonoBehaviour 側 ──寫入─►  ECS 單例/Blob（每幀更新）
   • 玩家判定點 world position          → PlayerPointSingleton (IComponentData)
   • 存活部位 AABB + part_id 表          → PartColliderBuffer (DynamicBuffer/Blob)
   • 難度密度乘數 bullet_density_mult    → DifficultySingleton
   • Time.timeScale                      → SimTimeSingleton（頓幀凍結敵彈）

 ECS 側（Burst Jobs，平行）
   • Emitter 系統：依 EmitterPattern Blob + 密度 hook 生成敵彈（寫連續陣列）
   • Simulation 系統：位置積分、lifetime、離屏剔除、重建空間網格
   • Collision 系統：敵彈 vs 玩家點（網格查詢）；玩家飛彈 vs 部位 AABB
   • 命中結果 → NativeQueue<HitEvent>（struct，Burst 友善）

 ECS 側 ──主執行緒同步點──► MonoBehaviour 側
   • Bridge 系統（ECS main-thread system 或 host MonoBehaviour）
     排空 NativeQueue<HitEvent>，翻譯為 managed 事件：
       - 敵彈命中玩家點  → PlayerHit 事件（Player/GameFeel 消費）
       - 玩家飛彈命中部位 → on_missile_hit（republish 到 Core 事件匯流排）
```

**設計要點**：
- 跨界資料是**值型 struct（POD）**，不傳 managed 引用進 ECS（Burst 相容、零 GC）。
- **玩家雷射不進 ECS**：雷射是連續判定（raycast/overlap），直接在 `Weapons`（Mono）對少量部位（≤8）判定並發事件，不佔敵彈通道（`bullet-system.md` §8.1）。
- **玩家飛彈可進 ECS 獨立池**，或走 Mono 池——原型量測後定（ADR-0001 開放項）；追蹤飛彈需部位 `world_position`，經 `PartColliderBuffer` 提供。
- Bridge 是唯一的 DOTS↔Mono 事件翻譯層，**單點可測、可替換**——若 ADR-0001 退回純 Mono 後端，只換 Bridge 與模擬後端，事件契約與 Emitter 資產不動。

### 5.4 服務組合與 DI (Composition & DI)

`KaijuBreaker.App` 是唯一組合根：啟動時建構事件匯流排（`IEventBus`）、註冊各系統為服務（實作查詢介面如 `IPartStateQuery`、`IDifficultyProvider`、`ISaveService`），並以建構子/方法注入接線。**禁止 static 單例持有遊戲狀態**（`coding-standards.md`）；唯一允許的 static 是無狀態工具與 `Core` 的事件匯流排存取點（本身可被測試替換）。詳見 ADR-0002、ADR-0005。

---

## 6. 資料架構 (Data Architecture)

### 6.1 靜態調校資料 = ScriptableObject（唯讀）

**所有 GDD「調校旋鈕 (Tuning Knobs)」章節的數值以 ScriptableObject 表達**，放 `Assets/_Project/Content/`，執行期唯讀載入，落實「零硬編碼」（ADR-0003）。這**取代** GDD 各文件中提及的 `assets/data/**/*.yaml`、`input_settings.json`（調校用途者）等佔位路徑——那些路徑是引擎無關 GDD 的佔位，Unity 實作統一為 SO 資產。

| GDD 旋鈕群 | ScriptableObject | 對應 GDD |
|-----------|------------------|---------|
| 武器全域 + 各武器 | `WeaponBalanceConfig` + `WeaponDef`×8 | `weapon-system.md` G |
| 部位系統 | `PartSystemConfig` | `kaiju-part-system.md` G |
| 巨獸定義 + 部位 + 相鄰 + 掉落表 | `KaijuDef`（含 `PartDef[]`, adjacency, drop_table_id）| `kaiju-part-system.md` C.6 |
| 彈幕模式 | `EmitterPatternSO`（→ 載入時烘焙為 Burst Blob）| `bullet-system.md` §4 |
| 難度乘數 | `DifficultyConfig`（唯一來源）| `difficulty-system.md` G |
| 素材/升級 | `EconomyConfig` | `material-economy.md` G |
| 關卡/波段/莢艙 | `StageDef` + `SegmentDef` + `PodDropConfig` | `stage-system.md` K |
| 打擊感 | `GameFeelConfig` | `game-feel.md` G |
| 輸入 | `InputSettings`（預設；玩家覆寫存 save）| `input-system.md` K |

**雙層彈幕模型**：`EmitterPatternSO`（設計師 Inspector 撰寫）→ 載入時烘焙為不可變 Burst 友善 Blob（執行期唯讀），使設計師無需碰程式即可撰寫三頭目全部模式，且後端可換（`bullet-system.md` §4；ADR-0001/0003）。

**UI 框架決議（ADR-0006，Accepted）**：三層拆分——世界座標部位血條用 `SpriteRenderer + MaterialPropertyBlock`(不進 Canvas，避免彈幕區開銷)、in-combat HUD 用 UGUI 多 Canvas(判定點 overlay sort 99)、meta 畫面用 UGUI `UIScreenManager` 堆疊。UI Toolkit 因 Unity 6.3 手柄導航/手機成熟度不確定,列為 post-MVP 再評估。

### 6.2 玩家可變資料 = JSON 存檔（讀寫）

玩家進度（武器 Tier/所有權、素材、巨獸紀錄、設定、統計）以**單一 JSON 檔** + **暫存改名原子寫入** + **CRC32 完整性**持久化於 `Application.persistentDataPath`，背景執行緒非同步寫入，`on_part_break` 即時入帳保證「永不丟失」。完整規格見 ADR-0004 與 `meta-progression-system.md`。

**靜態 vs 可變的邊界**：SO = 設計師撰寫、玩家不可改、隨版本走；JSON save = 玩家產生、跨版本遷移。兩者不混——這是可維護性的關鍵切分。

---

## 7. 效能策略 (Performance Strategy) — 第一風險

| 平台 | 目標 fps | 彈幕+碰撞份額 [需引擎階段驗證] | 同屏敵彈預算 [需引擎階段驗證] |
|------|---------|------------------------------|------------------------------|
| **PC** | 60（120 加分）| ≤ 2.0 ms | 1,500–2,000 |
| **手機（中階基準機）** | 60 穩定，最低不掉 30 | ≤ 3.5 ms | **800–1,200（承諾點 1,000）** |

**手機 sustain 1,000 敵彈 @60fps 是本專案第一技術風險的核心承諾數字，標記 [需引擎階段驗證]，須由效能原型在基準機實測後才 LOCK**（`bullet-system.md` 11.1；ADR-0001 驗證閘門）。

**策略支柱**：
1. **DOTS/ECS + Burst + Jobs**（敵彈）：struct-only 資料、SIMD 向量化、跨核平行、零 per-bullet GC（ADR-0001）。
2. **物件池，執行期零配置**：敵彈池與玩家飛彈池分區預配置（PC 2560/256，手機 1536/128，×1.3 餘裕）；無 `Instantiate`/`Destroy`/boxing（`bullet-system.md` §3.2）。
3. **繪製批次**：所有敵彈共用單一暖色 sprite atlas + 單一材質 → GPU instancing / SRP Batcher，draw call 壓到個位數（§8.4）。
4. **廣相碰撞**：玩家單點判定 + 均勻空間網格 (spatial hash grid)，每幀由模擬 Job 重建；玩家飛彈 vs ≤8 部位 AABB 粗篩。
5. **離屏剔除**：出界即 despawn（省模擬+碰撞），剔除邊界 +8% 視口。
6. **無逐彈 Update()**：模擬集中於 Job，杜絕上千 MonoBehaviour.Update 開銷。
7. **可讀性 > 密度 > 數量**：硬性同屏上限截斷密度乘數，永不為數量犧牲可讀性（`readability_cap_priority` 不可關）。

**處置順序（若手機達不到 1,000）**：(1) 降 D4 密度乘數（安全範圍內）→ (2) 收緊同屏上限 → (3) 最後才視覺犧牲；**絕不犧牲可讀性換數量**。

### 7.1 記憶體與載入（初步預算，待原型細化）

- 敵彈 atlas 單張、暖色少色 → 小；池為預配置 NativeArray，記憶體恆定。
- Addressables 按巨獸/關卡群組載入，避免一次載入全內容。
- 目標載入時間、記憶體天花板於 `technical-preferences.md` 效能預算段落補齊（原型後）。

---

## 8. 可追溯性矩陣 (Traceability Matrix)

| # | GDD 系統 | GDD 檔案 | 程式模組 (`.asmdef`) | 治理 ADR |
|---|---------|---------|---------------------|---------|
| S1 | 武器系統 | `weapon-system.md` | `KaijuBreaker.Weapons` | ADR-0002（事件）、ADR-0003（資料）|
| S2 | 可破壞部位 | `kaiju-part-system.md` | `KaijuBreaker.KaijuParts` | ADR-0002、ADR-0003 |
| S3 | 素材經濟 | `material-economy.md` | `KaijuBreaker.Economy` | ADR-0002、ADR-0003 |
| S4 | 關卡/波段 + Run 流程 | `stage-system.md` | `KaijuBreaker.Stage` | ADR-0005（狀態機/邊界）、ADR-0003 |
| S5 | 難度系統 | `difficulty-system.md` | `KaijuBreaker.Difficulty` | ADR-0003（唯一密度來源）|
| S6 | 輸入系統 | `input-system.md` | `KaijuBreaker.Input` | ADR-0003（設定資料）、ADR-0005 |
| S7 | HUD/UI | `hud-ui-system.md` | `KaijuBreaker.UI` | ADR-0002（訂閱事件）、ADR-0006（UI 框架）|
| S8 | 打擊感 | `game-feel.md` | `KaijuBreaker.GameFeel` | ADR-0002、ADR-0003 |
| S9 | 彈幕引擎 | `bullet-system.md` | `KaijuBreaker.BulletSim` | **ADR-0001（旗艦）**、ADR-0003 |
| S10 | 元進度/存檔 | `meta-progression-system.md` | `KaijuBreaker.Meta` | **ADR-0004**、ADR-0002 |
| C1 | 巨獸內容 | `kaiju/01–03` | `Content`（KaijuDef 資產）| ADR-0003 |
| — | 橫切基礎設施 | 本文件 | `KaijuBreaker.Core` / `App` | ADR-0002、ADR-0005 |

---

## 9. 技術風險 (Technical Risks)

| 風險 | 等級 | 緩解 | 治理 |
|------|------|------|------|
| **手機彈幕效能未達 1,000@60fps** | **最高** | DOTS/Burst + 池 + 單 atlas + 空間網格；效能原型驗證閘門；退回純 Mono 池備案（撰寫層解耦） | ADR-0001 |
| **DOTS/Entities 1.3 整合成本 / 團隊學習曲線** | 高 | DOTS 隔離於單一組件 `BulletSim`；Bridge 單點；撰寫層 SO 不知後端；可逆 | ADR-0001 |
| **Unity 6.3 API 知識落差**（LLM 截止 2022.3）| 高 | 標記 [需查證 6.3 API]；實作前查 `engine-reference/unity/VERSION.md` + 官方；不臆造簽名 | 本文件 §1 |
| **觸控彈幕手感未驗證** | 高 | 專用觸控原型 + playtest（阻斷 pre-MVP）；輸入數值不隨難度縮放 | `input-system.md` L.1 |
| **跨系統事件契約漂移** | 中 | 契約集中於 `Core` 型別 + ADR-0002；payload 欄位對齊 TR-registry；整合測試 | ADR-0002 |
| **存檔損毀/中途終止丟素材** | 中 | 原子寫入 + CRC32 + 備份 + on_app_suspend 同步寫；on_part_break 即時入帳 | ADR-0004 |
| **timeScale=0 頓幀吃掉輸入** | 中 | 輸入/UI 走 unscaled 時鐘；ECS 併入 timeScale；自動化測試 | `game-feel.md` I.2 |
| **武器平衡（等功率橫向選擇）** | 高（設計）| 8×8 TTB 矩陣自動化測試；數值全在 SO 可調 | ADR-0003；`weapon-system.md` H |

技術風險以穩定 ID 追蹤於 `docs/architecture/tr-registry.yaml`（由 `/architecture-review` 填充）。

---

## 10. 開放問題 (Open Questions)

1. **玩家飛彈池：ECS 獨立池 vs Mono 池？** 追蹤飛彈需部位 `world_position`；量測後定（ADR-0001 開放項）。
2. **HUD 框架** — 已由 ADR-0006 定案(SpriteRenderer 血條 + UGUI HUD/meta)。~~UGUI vs UI Toolkit~~ 已解決。
3. **ECS 時間注入方式（頓幀凍結敵彈）** 在 Entities 1.3 的正確 API [需查證 6.3 API]。
4. **手機基準機型號**：於效能原型階段確定並記錄（`bullet-system.md` §5.1）。
5. **on_part_break payload 中 shard/core yield 由誰算**：kaiju-part / economy / save 三方——本架構採 `Economy` 依 `break_quality`+`kaiju_id` 獨立計算（對齊 `material-economy.md` F.1），`Meta` 讀結果入帳；需三方最終確認（ADR-0002）。
6. **Emitter Blob 烘焙時機**：關卡載入 vs 建置期預烘焙——原型後定。

---

*文件版本：1.0.0*
*狀態：Proposed — 待 `/gate-check` Pre-Production 閘門與彈幕效能原型驗證後推進 LOCK*
*關聯 ADR：0001（彈幕後端）| 0002（事件架構）| 0003（資料驅動 SO）| 0004（存檔）| 0005（專案結構/組件）*
