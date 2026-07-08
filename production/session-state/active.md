# Active Session State — 殲獸戰機 / KAIJU BREAKER

*Last updated: 2026-07-08 (SESSION 9 進行中 — 搖桿靈敏度修正 + EXE/APK 重建 + 8-頭目 roster 設計擴充 5 隻新頭目 + 敵人擴充 + per-part 射擊 schema 規格; schema 實作委派中.)*

## ⚡ SESSION 9 (2026-07-08) — 搖桿修正 + 重建 + 頭目/敵人設計大擴充 + per-part 射擊 schema
- **導演本輪指示**：① 手機搖桿太靈敏→調低。② 先「設計」剩餘頭目 + 更多小怪/菁英（含彈幕發射模式+移動模式）；頭目/菁英參考雷電「不同部位射不同子彈」。③ 用目前狀態重建 EXE+APK。④ 定案：+5 頭目（共 8）、**全部做完整**（設計→schema→實作）。
- **✅ 搖桿修正**（`015fcd0`）：`PlayerInputRouter` 滿速行程 1×→**1.9× 半徑**（`_joyTravelMult` 可調）+ **0.12 中心死區**；捕捉區隨行程放大。編譯綠。
- **✅ 重建 EXE+APK**（含搖桿修正）：APK 45MB、EXE 116.7MB(遊戲資料 18:15 全新)，0 錯。Unity MCP `manage_build`：Android 68s、Win 24s（IL2CPP 快取熱）。
- **✅ 設計大擴充（全提交，未 push）**：
  - `kaiju/00-roster-overview.md`（`c26314a`）：8 頭目骨幹，每隻教一種武器、8 武器剋制全覆蓋。
  - `enemy-roster-expansion.md`（`dd72a3d`）：+6 新小怪(各預告一新頭目) + EnemyTier 正式化 + 菁英=莢艙來源。
  - `kaiju/04-08`（`1d64963`）：5 份完整 10 段 GDD — BROODCORE(蟲群/M4,7部位卵囊)/PRISMSHELL(晶簇/L2,6部位公轉晶面+HitGate)/TIDEMAW(深淵/M2,6部位破甲回填grace5s+6BU/s)/EMBERWING(餘燼/L3,7部位橫跨80%寬+跨部位break_gate)/NULLSPIRE(虛空/綜合capstone,8部位旋轉盾+公轉衛星+終局齊射). 由 5 個 game-designer 平行撰寫.
  - `per-part-firing-schema.md`（`f89dbe1`）：權威 schema 規格 — PartDef 加 Emitters[]/PartMovement/PartGate/ArmorRegen；KaijuDef 加 BodyMovement；enums KaijuTheme+5/MaterialId+5核心/EmitterPatternType+Spiral/MovementType+DiveSwoop·HoverStrafe/EnemyTier. 向後相容.
  - `systems-index` C1 更新為 8 隻 + C2 敵人擴充.
- **⚠️ 待導演定案（非阻擋）**：**經濟 sink** — 5 新核心(core_swarm/crystal/abyss/ember/void)「升級什麼」？現 8 武器升級綁 3 原始核心。選項 A 新養成軸 / B 擴武器成本池 / C 只計圖鑑。實作先讓每部位正確掉新核心不 crash.
- **✅ schema 實作完成 + 驗綠**（`8a81a08`，task #5）：per-part-firing-schema §1–6 全實作。**459 EditMode GREEN**（448→459，+12 新測試，0 fail）。
  - enums：KaijuTheme+5(Swarm/Crystal/Abyss/Ember/Void)、MaterialId+5 core、EmitterPatternType+Spiral(+SpinRate)、MovementType+DiveSwoop/HoverStrafe(+EntryAngle/StrafeHalf)、新 EnemyTier + EnemyDef.Tier。
  - **修好潛在 crash**：unity-specialist 只做了 1/3(3 enum)就停，加了 KaijuTheme 值卻沒補 `EconomyConfig.GetCoreForTheme` case→新主題會拋例外。我補上 5 core 欄位+5 case+其餘全部。
  - `PartFiringSchema.cs`：PartMovement/PartEmitter(+spawner)/PartGate/ArmorRegen/BodyMovement 結構。PartDef 加 Emitters[]/Movement/ArmorRegen/跨部位 Gate；KaijuDef 加 Body。全可選、向後相容。
  - **經濟 sink 定案**：新 5 核心=**機體/utility 養成軸**（與武器殺傷力升級分開，延續 meta utility）。
  - **導演指示**：新頭目實作先用**簡易 placeholder sprite**（memory [[new-bosses-placeholder-sprites]]）。
- **✅ task #6a**（`2049ada`）：`EnemyEmission.Spiral`(相位旋轉環) + `EnemyMovement.DiveSwoop/HoverStrafe` 純邏輯 + 4 測試。**463 EditMode GREEN**。
- **✅ task #6b**（`4b2ca07`）：`BossController` 依 PartDef.Emitters[] 用 `EnemyBulletPool` 真的 per-part 發射（瞄準玩家、依 PartFireGate 閘門、破部位消音）+ 部位移動(`PartMotion`: Orbit/SweepArc/Spin) + `KaijuDef.Body` idle；`GameplaySceneDirector` 把 pool+player 傳進 BeginBossFight。minion-spawner + PartStateSystem 跨部位 gate = 後續。
- **✅ 導演 playtest 6 bug 全修**（`6ae767e` 程式 + `660952b` 場景）：
  1. 炮口方向→CARAPEX 下顎旋轉補回原型值(l=-80/r=+80+flipX)。
  2. 主副武器同色→PlayerProjectile 依主/副+武器型上冷色(雷射青系/飛彈藍白+1.5x)。
  3. 底圖消失→Boss 缺 body-base，補回 CARAPEX `kaiju_carapex_body_base`(sortingOrder -1)；LACERA 有art可後補、VOLTWYRM 無。
  4. 畫面外怪一直射→`GameBootstrap.FitCameraToField` 正交相機自動框住整個直式場地(±4.5×±7)不論 aspect。
  5. 手機應豎版→ProjectSettings 手機鎖 Portrait(關橫向自轉)，PC standalone 維持橫版。
  6. PC 不該有搖桿→PlayerInputRouter 只在 isMobilePlatform 顯示+輪詢觸控。
- **⏳ 重建 EXE+APK 中**（含全部 bug 修復 + schema + 6b）。
- **TaskList**：#1–#11 全 ✅（#5 schema、#6 wiring、#7-11 六個 bug）。
- **✅ 6d 資產化（既有 3 頭目 per-part 發射）**（`7ba9521` 場景 + CARAPEX `<hash>` + `0a2c59e` L/V）：手改 KaijuDef .asset YAML 加各部位 `_emitters`（現有 Emitter SO：AimedShot/TriFan/Wall/Ring）。CARAPEX 核心瞄準/雙下顎三扇/背甲炮牆；LACERA 核心+四肢瞄準；VOLTWYRM 核心瞄準/雙盾牆/雙頸放射。全 AliveOnly（破部位消音）。**execute_code 載入驗證所有 emitter ref 解析成功**。**三頭目現在都不同部位射不同彈**。
- **✅ CARAPEX 視覺修正**：背甲炮 y=4.4(核心上)→1.75(底部朝玩家，解碼原型 ToWorld=(IH/2-By) 確認)；body-base 0.72；場景截圖驗證。
- **⏳ 重建 EXE(116.7MB)+APK(47MB) 完成**（含三頭目 per-part 發射 + CARAPEX 視覺）。
- **下一步(續作)**：① 5 新頭目要建 KaijuDef .asset + 場景 BossPart 階層(placeholder sprite [[new-bosses-placeholder-sprites]]) + BossController roster 註冊；② 部位移動資料(Lacera 四肢 SweepArc/Voltwyrm 頸旋轉—需場景 pivot)；③ 6 新小怪 SO/prefab；④ PartStateSystem ArmorRegen+PartGate(6c)；⑤ minion-spawner(BROODCORE)；⑥ LACERA/VOLTWYRM body-base；⑦ 音樂方向規格。
- **YAML 手改 KaijuDef emitter 的方法**：`_emitters` 陣列加在 part 的 `_dropTableId` 後；每項 `_pattern:{fileID:11400000,guid:<emitterGUID>,type:2}` + `_gate:0`(AliveOnly)。emitter GUID：AimedShot 749e…/TriFan 2693…/Wall 7073…/Ring c713…。refresh assets→execute_code 載入驗證。
- **~~⏳ 下一步 = task #6 執行層接線~~（已完成 6a/6b）**（schema spec §7 其餘）：
  1. `EnemyEmission`/`EnemyMovement` 純函式加 Spiral/DiveSwoop/HoverStrafe 分支(+EditMode 測試)。
  2. `BossController`/`BossPart` 依 PartDef.Emitters[] 用 `EnemyBulletPool` 週期發射暖色彈、依 PartFireGate+break/armor/heat 閘門；SpawnEnemyId 者生小怪；依 PartMovement 每幀更新部位位置；BodyMovement 取代硬編 IdleMotion。
  3. `PartStateSystem` 加 ArmorRegen 回填 + PartGate 破壞/命中閘門。
  4. 資產化：8 隻 KaijuDef .asset 填 per-part emitter/movement(既有 3 補+新 5 建，**placeholder sprite**)；6 新小怪 SO+prefab。
  5. 可玩驗證 + 重建 EXE/APK。

---
*(以下為 session 8 及更早，保留供追溯)*
*session 8 FINAL — 可玩 run A→E + 選單 + 雷電3強化 + 循環莢艙 + meta utility + 三頭目 + 真美術 + 頭目細節. 448 EditMode GREEN. 全 push, HEAD `6d98f31`. EXE+APK 重建含全部.*
*Resume anchor: read THIS + `NEXT-STEPS.md` (same folder) first. Backlog entry point: `production/epics/index.md`.*
*Obsidian mirror: `C:\Users\User\Documents\Note\Kaiju-Breaker\` — full session-8 done/todo in `進度結算-2026-07-08.md`.*

## ✅ 已辦 / ⬜ 待辦 (session 8 收尾快照)
**✅ 已辦（session 8 全部完成、驗證、push、備份、EXE+APK 重建）：**
1. ✅ 首個「正式系統可玩 run」A→E：玩家機體+移動+開火+碰撞+勝負 / 道中成群小怪 / 敵人 5 種移動 + 4 種彈幕(Mono 池化) / Boss 真 PartStateSystem 蓄熱→軟化→擊破→勝利 / 手機虛擬搖桿+副武器鈕 + 勝負結算。
2. ✅ 介面美術頂一下：Ark Pixel 字體(中文OK) + `GameUiSkin` 冷色貼圖(標題/HUD/結算/搖桿)。
3. ✅ 選單流程：**標題 → 選頭目(洛克人X4風 hub) → 強化⚙商店 → 選裝備(L/M/難度) → 出擊 → 結算**。
4. ✅ 雷電3式 in-run 火力強化：P火力/M飛彈/W循環莢艙(顯示當前武器)，每 run 重置、只加殺傷力。
5. ✅ meta utility 升級：花碎片升「開火速度/掉落率」(不重疊殺傷力)，`UtilityUpgrades` +6 測試。
6. ✅ 三頭目 CARAPEX/LACERA/VOLTWYRM 可選可打(BossController roster)。
7. ✅ 美術/輸入修正：藍色 Kenney 船玩家 + 真 Gemini kaiju 頭目美術 + 手機副武器**按住即發**。
8. ✅ 頭目細節：受擊閃白圖 + 裝甲剝除換圖(intact↔stripped) + 軟化暖色 + 待機呼吸擺動。
- **狀態**：448 EditMode GREEN；全 push `origin/main` HEAD `6d98f31`；EXE(116.7MB)+APK(47MB) 重建含全部；Obsidian 已備份。

**⬜ 待辦（下次，非急，已按價值排序）：**
1. ⬜ 武器接**真 WeaponBehaviour**(目前 8 型是 placeholder 每型參數散/集/廣/穿)。
2. ⬜ 每隻頭目**專屬待機動作**(雷龍旋轉/利刃獸肢擺) + **軟化 glow 幾何/粒子** + **擊破碎屑**。
3. ⬜ 新兩頭目(Lacera/Voltwyrm)的 **per-part 開火/移動**(目前只有靜態部件)。
4. ⬜ Boss 進場走**真 PreBossLull/場景載入**(目前道中清空→直接 BeginBossFight)。
5. ⬜ UI 改 **UGUI+TMP**(ADR-0006；目前 IMGUI placeholder)。
6. ⬜ upgrade 等級移進 **SaveData**(目前 ISaveService flag 編碼)。
7. ⬜ **composable emitter**(DiveSwoop/Spiral/Zigzag/多 emitter — 設計已凍結)。
8. ⬜ Game view 設**直向解析度**；材料/UI 圖示 + 頭目頭像。
- **工作習慣**：MCP 進 Play/截圖會搶 Unity 焦點打斷導演 → 預設用 EditMode 測試 + 反射驗證([[verify-without-stealing-focus]])。

## ⚡ RESUME SNAPSHOT (2026-07-07, session 7 final)
- **DONE**: meta-save(7) + stage(7) + game-feel(7) epics 全完成；**App 組合根接線**(`GameComposition`/`GameBootstrap`/`StageDirector`/`BreakPayoffSequencer`/`KaijuThemeRegistry` + Unity adapters)；**placeholder assets** `Assets/_Project/Data/`(ContentRegistry+configs)；**`Bootstrap.unity`** 場景 + `SegmentSequenceRunner`(場景生敵人) + flash 疊層。
- **RUNTIME 驗證**: 真 Play session 發 PartBroke → timeScale=0+flash 0.78+shake 11+Meta partsBroken=1（整條事件鏈跑通）。
- **TESTS**: **442 EditMode + 10 PlayMode GREEN**。**~40 commits 全 push**（origin/main HEAD `3e4d3d3`）。
- **BUILDS**: `Builds/Windows/kaiju-breaker.exe`(118MB) + `Builds/Android/kaiju-breaker.apk`(46.9MB) 含美術+UI修復。
- **✅ session 8 已完成**：首個「正式系統可玩 run」A→E + 介面美術/選單 + **雷電3式強化 + 循環武器莢艙 + meta utility 升級 + 三頭目**。完整體驗＝**標題 → 選頭目(洛克人X4風,三隻可打) → 強化⚙(花碎片升開火速度/掉落率) → 選裝備 → 出擊 → 道中(撿P火力/M飛彈/W莢艙) → Boss(CARAPEX/LACERA/VOLTWYRM 真戰) → 勝負結算**，PC+手機。UI=Ark Pixel+程序化冷色貼圖。**已 push HEAD `65a83e8`**；**EXE+APK 重建中/已含全部**。
- **雷電3式強化（in-run，每run重置，只加殺傷力）**：P綠(火力↑散彈加彈數) / M藍(飛彈↑) / W莢艙(徘徊循環顯示當前武器→切型)；小怪掉P/M、菁英掉莢艙+P。**meta（跨run）只升 utility**（開火速度/掉落率，花碎片，`UtilityUpgrades` 6測試）——兩層不重疊。武器型4味(散/集/廣/穿)。
- **三頭目**：BossController 改 roster(BossEntry[])，選頭目 index 選誰打；Kaiju_Lacera(肢/5部件)+Kaiju_Voltwyrm(能量/核+2裝甲+2頸)；場景 Boss_Lacera/Boss_Voltwyrm，部件綁定驗證 5/5。
- **美術/輸入修正+頭目細節（`af2f55c`,`011694c`）**：玩家=藍色 Kenney 船(ship_0000)；三頭目換上**真 Gemini kaiju 美術**(`Art/Resources/Kaiju/`)；手機副武器改**按住即發**(原本只吃 Began)。頭目細節：**受擊閃白圖 + 裝甲剝除換圖(intact↔stripped) + 軟化暖色 + 待機呼吸擺動**(BossPart 視覺狀態機 + BossController 每幀讀 ArmorState/HeatState)。素材都在 `Art/Resources/`(kaiju+Kenney船)。**EXE+APK 已重建含全部**。
- **★ 工作習慣**：MCP 進 Play/截圖會把 Unity 切前景打斷導演 → 預設用 **EditMode 測試+反射驗證**，視覺確認集中並先告知（[[verify-without-stealing-focus]]）。本輪 B/C/D 全程沒進 Play。**448 EditMode GREEN**。
- **TODO（下次）**: ① 武器選擇/型號真正驅動玩家開火(接真 WeaponBehaviour，目前 placeholder 每型參數) ② 新兩頭目的 per-part 開火/移動 + bespoke kaiju 美術 + 頭目頭像 ③ Boss 進場走真 PreBossLull ④ UI 改 UGUI+TMP(ADR-0006) ⑤ upgrade 等級移進 SaveData(目前 flag 編碼) ⑥ composable emitter ⑦ Game view 直向。
- **關鍵路徑**: `Scripts/App/*` 組合根 · `Scenes/Bootstrap.unity` · `Data/*` assets · MCP `run_tests` EditMode/PlayMode。

## Session 8 (2026-07-07) — 首個「正式系統可玩 run」開工 (道中+Boss並重、正式prefab、彈幕多樣化)
- **導演定案**（見 memory [[playable-run-real-systems-directives]] + [[mobile-controls-joystick-secondary-button]]）：道中與 Boss **並重**（整條迴圈都要）；**正式 prefab+元件**路線；**小怪要更多**且**不同進場/移動邏輯 + 發射子彈邏輯（彈幕多樣化必做）**；PC 輸入 OK，**手機要虛擬搖桿+副武器按鈕**；手機效能「目前看起來沒問題，直接做」→ 敵人子彈先用 **MonoBehaviour 池化 kinematic** 子彈（ADR-0001 hybrid 的 Mono 側，不卡效能 spike）。
- **分階段計畫（TaskList）**：A 道中可玩 ✅ · B 敵人移動多樣化(MovementPatternSO 執行) · C 彈幕多樣化(EmitterPatternSO+Mono池化敵人子彈) · D Boss 戰(真 PartStateSystem) · E 手機控制+勝負結算 UI。
- **★ Phase A 完成並提交（`c435183`）+ 442 EditMode GREEN**：
  - 新腳本 `Scripts/App/Gameplay/`：`IPlayerInput`+`KeyboardMouseInput`（PC 鍵鼠；手機另一 provider 待 Phase E）、`PlayerShip`（移動+HP+i-frames+接觸傷害+0血 Died 事件=真敗北）、`PlayerProjectile`（池化、kinematic、trigger、冷色）、`PlayerWeaponController`（主武器自動開火+池）、`GameplaySceneDirector`（發 LoadoutConfirmed→RunController STAGE+StageDirector 序列→SegmentSequenceRunner 生波；player.Died→敗北）。
  - `Content`：`PlayerShipConfig` SO、`EnemyTierBalanceConfig` SO(T1=30/T2=70 資料驅動 HP)。
  - 改 `EnemyController`(資料驅動 HP+TakeDamage/死亡/hit-flash/出界回收+Rigidbody2D useFullKinematicContacts)、`SegmentSequenceRunner`(死亡/出界=已清→波段能推進)、`GameBootstrap`(+Content getter)。
  - 正式 prefab：`Prefabs/Player.prefab`、`PlayerProjectile.prefab`、升級 `Enemy.prefab`(trigger+kinematic RB+sprite+tierBalance)；生成占位 sprite `Art/Generated/`(冷色玩家/暖色敵人=可讀性規則)；相機改正交直向；波段加密(8/波×2波×3段)。
  - **RUNTIME 驗證（Play + 強制 Physics2D.Simulate，因編輯器失焦會節流 frame，已把 PlayerSettings.runInBackground=true）**：彈→敵 30→18(−12)+彈消失、擊殺 30→0 停用、接觸 100→90(−10)、0血→DEFEAT log。截圖：冷色玩家機底部自動連射、8 暖色敵人橫排。
- **Phase A 已知 follow-up**：① RunController 只有「破核心→RESULTS」勝利路徑，**缺 STAGE/BOSS 敗北→RESULTS 轉換**（Phase E 補，敗北目前只停火+log）② Game view 需設**直向解析度**(目前 landscape，敵人 y=6 生成帶被擠到頂)③ 敵人移動仍是占位下飄(Phase B 換 MovementPatternSO 執行)④ 只有 RamGrub 一種敵人資產(B/C 要補 tri_shot/aimed_gun/ring_burst + 移動/emitter 資產)⑤ 玩家副武器/真武器類接線(Phase D boss 用)⑥ 池化敵人(目前 Instantiate)。
- **frozen 敵人設計已萃取**（給 B/C）：10 隻道中敵(ram_grub/tri_shot/aimed_gun/ring_burst/shield_flier/column_grunt/side_weaver/splitter/kamikaze/fast_strafer；MVP 核心4+菁英)；現行 SO 是**扁平 enum**(MovementType 5種、EmitterPatternType 4種)——先照現行 enum 做執行系統(已是多樣)，composable 版(DiveSwoop/Spiral/Zigzag/多 emitter)為設計已凍結的後續擴充。可讀性硬規則：敵彈暖色/玩家冷色、telegraph≥0.3s、非追蹤、難度只縮數量。
- **★ B→C→D→E 一次全部完成並提交（導演指示「一次完成 BCDE」）+ 全程 442 EditMode GREEN**：
  - **B 敵人移動多樣化**（`7e88a4b`）：`EnemyMovement`(純、可測)執行 5 種 MovementType（StraightRush 俯衝/HorizontalDrift 橫飄/Hover 到位懸停/UTurn 折返上升/Sinusoidal 蛇行），px→world 0.025。驗證：hover 停 2.5、uturn 反轉上升、sine 蛇行 maxX 1.75。
  - **C 彈幕多樣化**（`7e88a4b`）：`EnemyBullet`+`EnemyBulletPool`(Mono 池化 kinematic，ADR-0001 Mono 側)+`EnemyEmission`(純：Aimed 扇/Linear 牆/Radial 環/RingBurst 死亡環)。`EnemyController` 週期發射+0.3s telegraph 閃光+死亡環；暖色彈。context(pool+player)經 SegmentSequenceRunner→WaveSpawner→enemy(選用參數，不動既有測試)。`PlayerShip` 吃彈。名冊：5 movement + 5 emitter SO + tri_shot/aimed_gun/ring_burst/side_weaver 敵人，段落池混編。驗證：ring=8、aimed=3 朝玩家；活體敵人從池發彈→玩家 −10+消失；截圖暖色彈 vs 冷色機。
  - **D Boss 戰**（`f576ea3`）：`BossPart`(場景部件→發真 LaserHit/MissileHit)+`BossController`(從 KaijuDef InitializeParts、綁部件+世界座標、EnterBoss、每幀 Tick、PartBroke 隱藏、BossCoreBroke 勝利、註冊 theme)。玩家副武器(飛彈)開火；`PlayerProjectile` 雷射熱/飛彈破雙軌+boss 分支。內容：`Kaiju_Carapex`(核心+雙下顎+裝甲背甲)+boss-block sprite+場景 Boss 階層。驗證：BeginBossFight→run=Boss；40 雷射軟化核心+4 飛彈擊破→BossCoreBroke→run=Results(won=YES)+log；截圖紅核/灰裝甲/紫下顎。
  - **E 手機控制+勝負結算**（`67627a2`）：`RunController.Defeat()`(STAGE/BOSS→RESULTS 敗北，補上原本只有勝利路徑的缺口)。`GameplaySceneDirector` 敗北→Defeat + IMGUI 結算疊層(VICTORY/DEFEAT+RESTART，DPI 縮放，PC/觸控通用)。`PlayerInputRouter`(取代 KeyboardMouseInput)：一個 IPlayerInput 跨平台=PC 鍵盤/Space + **固定虛擬搖桿(左下移動)+副武器按鈕(右下)** 用螢幕區域 touch/mouse polling(免 Canvas)。驗證：搖桿軸數學(right→(1,0)、half-up→(0,0.5)、遠→clamp 1)、搖桿+按鈕渲染、玩家死→run=Results+DEFEAT 疊層。
- **首個「正式系統可玩 run」= A→E 全綠可玩**。完整迴圈：LOADOUT→道中(多樣移動+彈幕的成群小怪)→Boss(真 PartStateSystem 蓄熱→軟化→擊破→勝利)→勝負結算，PC 鍵鼠+手機搖桿/按鈕。commits `c435183`→`67627a2`（未 push，依 [[commit-often-push-on-request]]）。
- **A→E 已知 follow-up（非阻擋）**：① Boss 進場走真 PreBossLull/場景載入(目前 OnWavesCleared 直接 BeginBossFight)② 結算改 UGUI+像素字(ADR-0006；目前 IMGUI MVP，中文按鈕已改 ASCII 避免亂碼)③ 手機控制 IMGUI→UGUI 圓形 sprite + 裝置 touch-feel 微調(input/story-001 spike)④ 敵人池化(目前 Instantiate)⑤ bespoke kaiju 美術+軟化 glow/裝甲視覺回饋+雷射/飛彈分別視覺⑥ composable emitter(DiveSwoop/Spiral/Zigzag/多 emitter)⑦ Game view 設直向解析度。

## Session 7 (2026-07-06) — stage 001/003 + meta-save 001–004 + 程式流程圖 (方向: meta-save→stage Integration→game-feel 依序做完)
- **導演指示**: 一律中文（[[respond-in-chinese]]）；方向選 1→2→3 全做（meta-save → stage Integration → game-feel）。任務清單見 TaskList。
- **★ 完成並提交（各自 commit，未 push）+ 全綠**：
  - stage/001 Run 狀態機 `RunController`（15）→ 見下方詳述。
  - stage/003 波段重組 `SegmentRecombinator`+`SegmentSequence`（8）→ 見下方。
  - meta/001 SaveData schema + `CanonicalJsonSerializer`(手刻,canonical) + `CRC32Calculator`（11）— `SaveConfig` 加 §G 旋鈕。
  - meta/002 原子寫入 `AtomicSaveWriter`(temp→Flush(true)→File.Replace→copy bak) + `SaveWorker`(depth-1 overwrite queue, deep-copy, SyncWrite, thread)（6）— `SaveData.DeepCopy()`.
  - meta/003 `SaveLoader`(primary→backup→newgame/corrupted, VerifyIntegrity, VersionTooNew, 非崩潰) + `SaveLoadResult` + `NewGameFactory` + `MaterialKeys` + Core `SaveCorrupted` 事件（9）.
  - meta/004 `SaveMigrator`(注入 currentVersion+registry, pure-fn chain, TooOld/NotNeeded/Migrated) + `MigrationResult`（8）.
  - **EditMode suite: 273 → 330 GREEN**（+57）。commits: 19612c3, bba0f19, bbb66ca, a28cdca, e190789, 07e4770。
- **meta-save 關鍵 reconciliation**：`ICanonicalSerializer` 放 Meta（非 Core，因引用 SaveData）；既有 `ISaveService`(economy 版) 表面不動，讀方法/事件訂閱留給 story-006 `MetaSaveService`（additive）；`File.Move(overwrite)` 不存在→用 `File.Replace`；序列化器手刻(Option C)、canonical float 用 "R"、materials/stats 用 long。詳見各 story 檔尾。
- **★ meta-save EPIC 完成（7/7 story，全綠）**：001 schema+serializer+CRC32 · 002 原子寫入+worker · 003 完整性載入+復原 · 004 遷移鏈 · 005 邊界+新遊戲init+loadout fallback · 006 `MetaSaveService` 實作 `ISaveService`+`IWeaponTierQuery`(economy 入帳落地) + PartBroke/HuntEnded stats + FlushSync + `MetaSaveLifecycleBridge` · 007 武器所有權(WeaponPodGrabbed→WeaponUnlocked, monotonic)。commits 至 9b73ba8。**EditMode suite 273→357 GREEN**。
- **meta-save 已知後續（非阻擋，已記於 story 檔）**：(a) App 組合根需 new MetaSaveService 並注入為真正的 ISaveService/IWeaponTierQuery（目前 Economy/Weapons 靠 DI 拿介面，尚未在 App 接線）；(b) per-kaiju/per-difficulty 記錄（parts_ever_broken/full_clear_count/best_time）需 int→string kaiju-id 映射 + HuntEnded 帶 difficulty/time；(c) PlayMode suspend 測試（manual QA doc 已寫 `production/qa/evidence/save-autosave-suspend-evidence.md`）。
- **1→2→3 進度**：✅ 階段1 meta-save（7/7）→ ✅ 階段2 stage（7/7）→ ✅ **階段3 game-feel（完成）**。**整個 1→2→3 計畫達成。**
- **★ game-feel epic（session 7）**：001 config(既存 SO 滿足) · 002 `HitstopSystem`(注入 ITimeScaleControl, Tick 倒數) · 003 `SlowmoSystem`(drop→hold→ramp §D.2) · 004 `ShakeSystem`(trauma max-not-add+衰減+floor偏移+閾值) · 005 `SoftenedSignatureSystem`(軟化登記+SFX限流) · 006 `FlashSystem`(§D.3 max-not-add+衰減+alpha上限) · 007 `ReduceMotionController`+`ReduceMotionSettings`(執行期 a11y 倍率, 存 ISaveService flag)。3 個 time/motion 系統加選用 `ReduceMotionSettings`。commits: 8f7c9c5, d11dcd9, ab2409a, 5d039ba。
- **game-feel follow-up（視覺，非邏輯）**：碎屑/煙/material orb 粒子、glow 幾何/2Hz 脈動、boss 死引爆、hitstop→slowmo 的實際 timescale 接手序（compose 已測系統）、camera/canvas adapter（讀 ShakeSystem.ComputeOffset / FlashSystem.Alpha / ITimeScaleControl→Time.timeScale）。
- **★ 字型裁切修復（session 7）**：像素字吸附「最小16、四捨五入16倍數」把小字放大超出格子→裁切。改成向下吸附(floor 到4、下限8，永不放大)。commit `7ddaea9`。重出 APK 46.9MB。
- **★ stage epic 全完成（session 7）**：001 Run 狀態機 · 002 雜兵生成(WavePlanner+WaveSpawner+prefab, EditMode+PlayMode) · 003 波段重組 · 004 菁英+莢艙保底(PodDropTracker) · 005 循環武器莢(WeaponPodController+prefab, PlayMode) · 006 頭目過渡(PreBossLullController+ISceneLoader) · 007 引導(OnboardingController+ISaveService flags)。commits: 26bbbeb/8463132/1bc96af/61db8d8/d375403/7a9a493。
- **★ 手機 UI 修復（session 7）**：IMGUI 用固定像素 → 高 DPI 手機超小。修法：`OnGUI` 套 DPI 感知 `GUI.matrix` 縮放(clamp(dpi/96,1,4))，桌機 1× 手機 3-4×，世界錨定除以 scale。commit `2fc5291`。**重出 APK 46.9MB(含 UI 修復)**。
- **stage 已知 follow-up（各 story 檔有記，多為 App 接線 + 美術）**：WaveSpawner/WeaponPodSpawner/OnboardingController/PreBossLull 接進 run flow 場景；ISceneLoader 真實 additive 載入(boss 場景屬 kaiju-roster)；敵人子彈發射(ADR-0001)；per-enemy/pod 美術；BossSilhouette。
- **測試總計**：**434 EditMode + 9 PlayMode GREEN**。
- **★ App 組合根接線（session 7 尾）**：`GameComposition`(純 C#，從 `ContentRegistry` 建 bus + Difficulty/Meta/KaijuParts/Economy/GameFeel/RunController, ctor DI) + Unity adapters(`UnityTimeScaleControl`→Time.timeScale、`UnitySceneLoader`→additive SceneManager) + `KaijuThemeRegistry`(IKaijuThemeQuery) + `GameBootstrap`(建 composition、TickGameFeel、shake→camera、pause/quit flush)。整合測試 5/5。commit `2800186`。
- **★ App 接線 1→2→3 完成**：(1) `StageDirector` per-run 組裝（LoadoutConfirmed→recombine+PodTracker+PreBossLull+Onboarding；NotifyLastSegmentEnded→lull→EnterBoss；HuntEnded 清理）`673c6fe`。(2) `BreakPayoffSequencer` Hitstop→Slowmo 乾淨接手（改 subscribeToBus:false 由 sequencer 驅動 + HitstopEnded 事件；boss 覆蓋 §E.2）`4bed0e9`。(3) placeholder assets `Assets/_Project/Data/`（ContentRegistry+10 configs+5 SegmentDef+EnemyDef+StageDef），**驗證真實 assets 能 runtime 組出完整圖**`3bc9e0b`。**測試 442 EditMode + 9 PlayMode**。
- **★ 最後一哩 1→2→3（session 7 尾，正式系統可玩路徑）**：(1) `Bootstrap.unity` 場景（`GameBootstrap`+`ContentRegistry.asset`，加進 build settings；Play 下組圖成功）`7d33f86`。(2) `SegmentSequenceRunner` 把 `StageDirector.CurrentSequence` 各段落依序丟 `WaveSpawner` 生敵人→完成回呼；`WaveSpawner` 生成 SetActive(true)（PlayMode 1/1）`f4ef272`。(3) `GameBootstrap` 全螢幕 flash 疊層（讀 `FlashSystem.Alpha`）；**真實 Play session 端到端驗證**：bus 發 PartBroke → timeScale=0 + flash.Alpha=0.78 + shake=11 + Meta partsBroken=1 `d5e1fdd`。**測試：442 EditMode + 10 PlayMode GREEN**。剩：softened glow / 粒子 / 玩家+碰撞+勝負（美術/gameplay-scene follow-up）。
- **App 接線剩 scene/美術 follow-up**：`ContentRegistry.asset` 指給 Bootstrap 的 `GameBootstrap`；`WaveSpawner` MonoBehaviour 消費 `StageDirector.CurrentSequence` 實際生成敵人；GameFeel 粒子/glow adapter；bossBreakablePartCount 從 KaijuDef 帶入。
- **★ 建置修復（session 7）**：原型 `LoadArt` 用 editor-only `AssetDatabase` → build 裡無圖。修法：美術移到 `Art/Resources/`，`LoadArt` 改 `Resources.Load`（commit `d10e92e`）。重建 **EXE（118MB 含圖）+ APK（46.9MB 含圖）** 成功；`/Builds/` 已 gitignore。Android IL2CPP 建置很慢/不穩（第一次 ~20min、重建卡 ~28min、session 掉線一次），最終都成功。manual QA doc: 建議之後看 gradle 快取。
- **★ 階段2 stage Integration（session 7，真 prefab/場景 + PlayMode）**：
  - 002 ✅ 全完成：`WavePlanner`(池模型+難度縮放)+`SpawnLayoutHelper`+`WaveTimingConfig`(12 EditMode) → `WaveSpawner`+`EnemyController`+`Enemy.prefab`(4 PlayMode 實測 Instantiate+SO 接線)。
  - 004 ✅ 核心：`PodDropTracker`(保底 9 EditMode 含 200 輪)+`EliteKilled`/`PodSpawnRequested`/`EliteShardsDropped` 事件+`PodType`/`PodPoolPreference` 列舉+`SegmentDef.PodPoolPreference`+菁英 HP 縮放。
  - 005 ✅ 核心：`WeaponPodController`(下降/徘徊/循環/可達性 clamp/拾取→`WeaponPodGrabbed`/消失，5 PlayMode)+`WeaponPod.prefab`。
  - **PlayMode 測試基建建立**（`Tests/PlayMode/Stage/`，用執行期 template GO 當 prefab 避開 AssetDatabase）。commits: 26bbbeb, 8463132, 1bc96af, 61db8d8。
  - **剩**：006 頭目過渡（喘息計時 + boss 場景 async 載入，場景重）、007 引導（Stage1 首段覆寫，邏輯）。deferred（各 story 檔有記）：敵人子彈發射(ADR-0001)、`WaveSpawner`/`WeaponPodSpawner` 接進 run flow 場景、per-enemy 美術。
- **測試總計**：**378 EditMode + 9 PlayMode GREEN**。
- **Artifact 程式流程圖**（給導演）：5 視圖，冷色調，掃描實際 Subscribe/Publish 繪製。

## (session 7 起始) stage epic Story 001 (Run 狀態機) + 程式流程圖
- **★ stage/story-001 Run 狀態機 DONE — 15/15 EditMode GREEN → suite now 288/288** (was 273). `RunController` (`Scripts/Stage/RunController.cs`, 純 C#, ctor-DI `IEventBus`+`ISaveService`): LOADOUT→STAGE→BOSS→RESULTS→LOADOUT，`EnterBoss(int totalBreakableParts)` public 排程呼叫、`ConfirmResults()` 回 LOADOUT；訂閱 `LoadoutConfirmed`/`BossCoreBroke`/`PartBroke`/`WeaponPodGrabbed`，發 `RunStateChanged`+`HuntEnded`；full-clear 由 BOSS 期間 distinct PartBroke ids vs total 判定；每次轉換+pod+part-break enqueue autosave。非法轉換擲 `InvalidOperationException`，非 BOSS 的 BossCoreBroke 安全忽略。
- **New Core events** (`Core/Events/RunEvents.cs`): `RunStateChanged{From,To}`、`LoadoutConfirmed`、`WeaponPodGrabbed{Weapon}`（後者 Story 005 擁有/發布，現先宣告供 RunController 訂閱）。
- **Reconciliations** (見 story-001 檔尾): `EnqueueAutosave()`≠story 的 `EnqueueSave()`；`BossCoreBroke`≠`BossCoreBreak`；同幀 PartBroke 存檔合併是 meta-save 職責 → AC-4 用 ≥ 下限斷言。
- **程式流程圖 Artifact**（給 director 看）: 5 視圖（架構分層 / 戰鬥資料流 / Run 狀態機 / 訂閱-發布對照 / 進度），冷色調對齊 art-bible。掃描實際 Subscribe/Publish 接線繪製。
- **★ stage/story-003 波段重組 DONE — 8/8 GREEN → suite 296/296**. `SegmentRecombinator`+`SegmentSequence`（`Scripts/Stage`，純 C#，注入 `System.Random` 決定性）：§D.1 六步驟（pool>N 時排除 no-repeat → 難度過濾 `(int)MinDifficultyTier<=currentTier`，剩<N 還原全池 → Fisher-Yates → 取N → `DifficultyWeight` 升序(OrderBy 穩定) → 組裝 intro+escalating+lull+bossId）。ctor 吃 `DifficultyTier` 列舉（非 story 的 int）。
- **新增 Content 欄位**（content-config 已完成，這是向後相容擴充）：`SegmentDef.DifficultyWeight`(int 1-5 `[Range]`)、`StageDef.IntroSegment`/`PreBossLullSegment`(SegmentDef 引用)。既有 .asset 尚未建立故無痛。
- **Next**: stage 剩 002/004/005/006/007 多為 **Integration（需 Unity prefab/spawn）**；或轉做 **meta-save**（純邏輯偏多，能把 `ISaveService`/`IWeaponTierQuery` 真實實作起來，解 economy/stage 的 autosave 尾巴）。待與導演確認方向。
- **Commits**: story-001 已提交（`19612c3`）。story-003（待提交）：SegmentRecombinator + Content 欄位 + tests + 狀態。未 push（依 [[commit-often-push-on-request]]）。

## Session 6 (2026-07-05) — kaiju art into DEMO + armor-break bug hunt (all pushed)
- **16 boss-kaiju sprites (Gemini Pro web) → green-screened → in-game**: director generated all 16 parts on a green screen; I chroma-keyed (numpy despill + autocrop), named+placed into `Assets/_Project/Art/Kaiju/{Carapex,Lacera,Voltwyrm}/` (16 sprites + 16 `_white.png` hit-flash silhouettes), imported Sprite/Point/Uncompressed, downscaled 55MB→3MB. Source PNGs gitignored (`design/assets/gemini-generation/`). Parts: CARAPEX core/mandible/dorsal(intact+stripped)/body-base · LACERA head/fore+hind limb/tail(intact+stripped)/stub/body-base · VOLTWYRM core/neck/shield(intact+stripped).
- **Wired into prototype boss assembly**: `PartVisDef.Art/ArtStripped/FlipX/ArtScale/Rot` + `BossDef.BodyArt`. BuildPartVisuals loads real sprites (aspect-fit via `bounds`), body-base backdrop, intact↔stripped swap on armor state; RefreshPartVisuals tints heat/core. Procedural-box fallback kept.
- **Demo polish (iterated on director feedback)**: idle motion (breath+drift all bosses; CARAPEX horiz sweep / VOLTWYRM S-slither+spinning core+pulsing neck / LACERA limb hinge); mandibles rotated tip-down (`Rot=-80`); hit-flash = pure-white **silhouette sprite** swap (a white tint MULTIPLIES → invisible on colored art); ship 30→50; per-part **soften + HP-style durability meters** (white tex).
- **★ Armor-break bug — REAL root cause found + fixed** (`db90091`): TWO bugs. (1) prototype never called `_parts.Tick(dt)` in boss phase → heat queued in `_pendingHeatDeltas` never applied → nothing softened (`6a7ee0b`). (2) **`MissileDamagePart` deflect check was `Armored && ArmorState==Intact` — missed the §113 heat-softened bypass**; softening does NOT change ArmorState, so softened armored parts deflected EVERY missile forever ("softened but can't break"). Fixed: also require `HeatState != Softened` to deflect. Verified on the real missile path: softened dc + 3 missiles → Broken. Lesson: diagnose the ACTUAL code path — reflection-publishing MissileHit directly bypassed the buggy line and made logic look fine.
- **Director rule** (memory [[no-balance-tweaks-for-debugging]]): debugging must NOT loosen balance to reproduce a bug. I'd enlarged hit-box ×1.5 + cut drift "to make dc hittable" → reverted (`262b108`).
- **Commits**: difficulty (`48339bb`) · font+prompts+state (`264bc92`) · kaiju sprites (`07d6813`) · wiring (`2f5a075`) · polish (`0f9845a`,`787eccf`) · meters (`7ba6371`,`07a29a0`) · break fixes (`6a7ee0b`,`db90091`) · balance restore (`262b108`).
- **Next**: director Play-tests full CARAPEX break loop (laser-soften→missile-break) now the bug is fixed; then LACERA limb root-pivot (still hinges about center, wants sprite pivot at root), stage epic (code), remaining art (material/UI icons).

## Session 5 (2026-07-05) — art / rough-demo pass (on top of the economy epic below)
- **UI/HUD art pass** (`896b9b1`, pushed): HUD/UI spec `design/assets/specs/hud-ui-assets.md`; prototype IMGUI restyled to art-bible cold palette + a `_pixelFont` hook (one chokepoint). **Font**: default Ark Pixel 16px, then director steered to **more tech-feel** → interactive font-decision **Artifact** published (live HUD mockup + canvas pixel-text + shortlist: Ark Pixel Mono / GNU Unifont / Galmuri), source `design/assets/font-demo.html` (untracked → committing now). Director still to pick + drop a TTF → assign `_pixelFont`.
- **Master asset manifest** (`dd1645a`): `design/assets/MANIFEST.md` — 56 sprite/VFX + font + UI icons batch worklist, all PENDING.
- **AI-art pipeline confirmed + tested**: Unity MCP `generate_image` routes to **fal.ai / OpenRouter** (bring-your-own-key, imports straight to Assets as sprites). Director added a fal key → **auth verified through MCP**, BUT fal account balance is **$0 ("exhausted balance")** → needs a **~$5 top-up** at fal.ai/dashboard/billing before generating. Cost: FLUX **schnell $0.003/MP**, **dev $0.025/MP** → whole MVP ≈ $1–12; validated schnell is the cheap workhorse. (`generate_model` = Tripo/Meshy 3D — unused.)
- **Rough-demo art via FREE CC0 (Kenney Pixel Shmup)** (`7d06e12`, `7d603eb`): pack committed under `Assets/_Project/Art/Kenny/`. Prototype ships wired — **player = blue, mob = red, elite = grey heavy**, enemies rotated 180°; `LoadArt()` editor-loads sprites with **procedural fallback (plan B)** so nothing breaks. Ships enlarged (player ~2×, mob 28×26, elite 48×44; player hitbox is a fixed point so difficulty unchanged). Compiles clean.
- **Still needs bespoke art (no free/off-the-shelf equivalent)**: the 3 Boss kaiju + breakable parts (SOFTENED/BROKEN states) + material/core icons → AI-gen (needs fal $5) or custom. Kenney terrain tiles deliberately NOT used (top-down grass/dirt doesn't fit the space/kaiju vertical shmup → keep the procedural starfield).
- **Open art todo**: (a) optional — Kenney enemy-bullet swap / procedural nebula-parallax bg (both free); (b) fal $5 top-up → schnell-generate boss kaiju + icons from the specs; (c) drop the chosen pixel TTF → assign `_pixelFont`. **Director to Play `Stage01Prototype` and confirm ship size / overall feel.**
- **Code track**: **difficulty epic COMPLETE — 273/273 EditMode GREEN** (was 210; +63 difficulty cases). `DifficultySystem`+`DifficultyScaling` in `Scripts/Difficulty`; `DifficultyConfig.asset` created (§D.3 defaults). Invariance proven by **assembly-scan** (`AssemblyReferenceScanner`): KaijuParts/Weapons/Economy never reference `IDifficultyProvider` (stronger than runtime-clamp); TTB(4×3)+weapon-output(4×8) matrices → `production/qa/evidence/`. Reconciliations in test comments: committed `IDifficultyProvider` = property (current-tier) not method(tier); `EconomyConfig` has NO difficulty knob (stronger than `difficulty_yield_bonus==0`); H.5 accessibility stubs await stage epic. Next unblocked = **stage epic**.
- **Font landed**: Ark Pixel 16px 繁中 TTF (OFL) → `Assets/_Project/Art/Fonts/ArkPixel-16px-zh_tw.ttf` (+hand-written meta: HintedRaster/Dynamic/16). Assigned to both prototypes' `_pixelFont` (via MCP scene edit). **Blur fix**: `Style()` chokepoint snaps every size to nearest 16/32/48 (Ark Pixel crisp only at integer multiples). Trade-off: HUD small text (was 10–13px) → 16px, layout may need micro-tuning — director to Play `Stage01Prototype` + confirm.
- **Gemini boss-kaiju生圖 prompts** → `design/assets/gemini-generation-prompts.md` (+Obsidian mirror): 3 kaiju full-body + parts +補漏的 body base, 去機械化(活體血肉/能量, 對齊art-bible鐵則一), 完整部件/乾淨接合邊, 透明背景. Director生圖用. **Spec 待補**: kaiju-*-assets.md 的 `biomechanical` 用詞 + 2 隻缺 body base asset.

## Session 5 progress (2026-07-05) — WHOLE economy epic + art specs (committed locally, NOT pushed)
- **economy epic COMPLETE — all 5 stories, 210/210 EditMode GREEN** (was 144). `EconomyService` in `Scripts/Economy`:
  - 001 (`f384674`) per-break yield · 002 (`c39960e`) full-clear essence · 004 (`acaf442`) Tier 0→3 `TryUpgrade` · 005 (`7caca3b`) anti-dominant TTB guard · 003 (`b96d5a9`) persistence push-side.
- **New Core surface (review)**: `MaterialId`/`KaijuTheme`/`TierTransition` enums; `IKaijuThemeQuery`; `HuntEnded` event; `ISaveService` +`CreditMaterials`/`GetMaterialCount`/`SpendMaterials`/`SetWeaponTier`. `EconomyConfig` +theme→core map, double-drop, full-clear knobs, upgrade cost table, weapon→core (via theme identity), `MaxTtbImprovementPct`/`Tier0To2CapPct`. `KaijuDef.Theme`.
- **Reconciliations (review)**: (1) theme→core on config + kaijuId→theme via `IKaijuThemeQuery` (runtime int ids can't be config keys). (2) weapon→core = fixed weapon→theme identity → data-driven theme→core (not an editable map, per GDD C.1). (3) tier read via existing `IWeaponTierQuery`, not a dup `ISaveService.GetWeaponTier`. (4) 005 AC-4 "top-3 in all 3 part types" unsatisfiable for the closed-form model → asserted §D.4/§H.3 ≤2.0× + ≤15% caps across all 3 part types; §H.6 viability deferred to QA playtest. (5) 003 file round-trip deferred to Meta (ADR-0004).
- **Follow-ups spawned**: Meta implements full `ISaveService`+`IWeaponTierQuery` over JSON save + publishes `HuntEnded` + save round-trip PlayMode test; QA runs economy §H.5/§H.6 playtests.
- **Art track (background art-director)**: 8 MVP asset specs committed (`856d2c0`, `design/assets/specs/`) — player ship, 3 kaiju (+SOFTENED/BROKEN states), 8 weapons, enemy bullets, material icons, break-VFX; all PENDING (await API key).
- **UI/HUD art pass (`896b9b1`, pushed)**: closed the HUD-spec gap — `design/assets/specs/hud-ui-assets.md` (art-bible §07 → font + palette + component spec). **Font decided: 方舟像素 Ark Pixel 16px** (OFL, 繁體 CJK+Latin, native 16px). Restyled the prototype (MainMenu + Stage IMGUI) to the cold-family palette (`#0A0E1A` bg, white/`#40F8FF`/`#00C0E0` text, blue buttons, warm only for kaiju/threat) + a single `_pixelFont` hook. **DIRECTOR STEP**: download Ark Pixel 16px TTF → `Assets/_Project/Art/Fonts/` → assign to `_pixelFont` on both prototype scripts (+ build a TMP bitmap asset for production). **Remaining art GAPS**: master asset manifest (index/worklist) still not written; UI icon sprites + all sprites PENDING (AI-art key).
- **NEXT (unblocked, pure C#)**: `difficulty` epic (4 stories) → then stage/meta-save/game-feel/input/hud-ui/kaiju-roster (NEXT-STEPS order). Still parked: ADR-0001 perf spike (bullets), art API key.

## Session 4 结算 (2026-07-03) — all committed & PUSHED to origin/main (HEAD 7452d74)
**DONE**: Unity MCP live (144/144 EditMode GREEN). ADR-0001 desktop smoke verified (status still Proposed — phone gate parked, PC-first). **Weapons epic logic COMPLETE**: stories 003–010 + Story 002 balance suite H.1/H.2/H.3/H.7; fixed 100× missile break-unit bug; point-3 mechanics (MidCore, GetHottestSoftenedPartId, data-driven knobs, M2 Chain Hive, L1 beam ladder 2→3→4→5). **7 feedback points**: all design specs authored (`design/gdd/{weapon-tiering-and-equal-power,enemy-tier-system,hit-feel-tiering,bullet-pattern-diversity}.md`, `design/art/scrolling-background-parallax.md`, `design/narrative/story-and-zone-structure.md`, `design/quick-specs/player-firing-direction-vertical.md`) + director decisions (`design/decisions/2026-07-03-director-decisions.md`). **Design fixes**: armor breakable by ANY weapon (heat-soften opens it; code+GDD+tests); no free mid-run weapon swap. **Playable prototype** (`Assets/_Project/Prototype/`, throwaway): full LOADOUT→道中(waves+elite+drops+pod)→BOSS(3 bosses)→RESULTS, uses real PartStateSystem, MCP-verified. Scenes MainMenu + Stage01Prototype.

**TODO (see Obsidian `進度結算-2026-07-03.md` for detail)**:
- Director/Editor: AI-art API key (fal.ai/OpenRouter) for real sprites; Story 001 config `.asset` authoring; ADR-0001 phone perf gate.
- Pure C# (unblocked): economy, difficulty, stage, meta-save, game-feel, input, hud-ui, kaiju-roster (NEXT-STEPS order); WeaponDef SO default→spec sync (cosmetic).
- Blocked/confirm: 7-point IMPLEMENTATIONS mostly gated on ADR-0001 (bullets) or Editor (art/VFX); point-5 story → /brainstorm→/team-narrative (protagonist GENESIS/珍, heavy narrative, biological-alien kaiju); fold new GDDs into systems-index.

## Session 3 progress (2026-07-02, Unity MCP live)
- **User directives this session**: (1) dev/test **PC-first** — cannot freely connect a phone remotely, so phone-gated spikes (ADR-0001 phone FPS, touch-feel) are parked, not blocking pure-logic work. (2) After weapons testing is roughly done, integrate the 7 gameplay-feedback points (`design/feedback/2026-07-02-*`). (3) Back up upcoming artifacts to the Obsidian vault above.
- **ADR-0001 perf spike — DESKTOP smoke PASS** (Unity MCP): compile clean, Play spawns 1000 `BulletVelocity` entities, Burst `IJobEntity` moves them each frame. Recorded in ADR-0001 (status stays **Proposed** — phone gate open). Commit `afacd58`.
- **weapons epic — IN PROGRESS**. Verified GREEN via Unity MCP `run_tests`:
  - Phase 0 shared contracts (commit `a0eec88`): `WeaponBalanceConfig.BuPerD0(10)/HuPerD0(25, inferred)/DefaultPrimary/DefaultSecondary`; `IPartStateQuery.GetHottestAlivePartId()`; `ISaveService.GetInitialLoadout()`; `WeaponEquipped` event.
  - Phase 1 base + Story 003 (`a0eec88`): `WeaponBehaviourBase`/`LaserWeaponBase`/`MissileWeaponBase` (ctor-DI, PartBroke→ClearCollider, magazine SM), stubs `StubPartStateQuery`/`StubWeaponTierQuery`, 9 base tests. **64/64 EditMode GREEN**.
  - Story 010 loadout (`LoadoutController` + tests) implemented, awaiting combined compile.
  - **DONE (2026-07-03): laser + missile families + loadout (004–010) → 121/121 EditMode GREEN** (`139d28a`). Fixed a real **100× missile break-unit bug** (D0Reference misuse; M3 was 3000/6000 instead of 30/60 BU — would instant-break 100-BU parts) — `0605c44`. H.3 M3-gate test at default config green — `49921b6`.
  - **Story 002 equal-power (H.1/H.2) + H.7 FOLDED INTO feedback-point-3 balance pass** (director decision "合併進第3點一次弄"). Balance analysis `design/balance/weapon-d0-equal-power-analysis.md` (`7f62721`) found **6 of 8 weapons outside ±10%** (M2 -80%, L2 +50%, M3 +29%, M1/M4/L3 low) — needs a real retune + new SO fields (EffectiveHitRate, per-weapon ShotInterval, M2DmgPerMissileMult). NOT a test-writing task.
  - **Weapons epic remaining**: Story 001 SO **asset** authoring (needs Editor — director/`.asset` creation); H.1/H.2/H.7 + the equal-power retune (in the point-3 balance pass).
- **NEXT: integrate the 7 gameplay-feedback points** (`design/feedback/2026-07-02-*`). Point 3 (weapon tiering / 散彈 2→3→4→5) absorbs the equal-power retune. Point 5 (story) direction greenlit for expansion. Point 1 (bullet-pattern diversity) gated on ADR-0001 (phone perf — parked). Points 2/4/6/7 → design specs.
- **Key weapons reconciliations** (like the kaiju-parts ones — for review): skip Weapons-side M3-T3 chain (KaijuParts already owns it, avoids double-count); `WaveHit` has no StaggerDuration field; ripple % read from `PartSystemConfig` not `WeaponDef`; M4 AoE uses corrected piecewise formula; tests live in `Tests/EditMode/Weapons/` not `Tests/Weapons/`. **HuPerD0=25 is inferred from G.2 laser defaults — wants an eventual design nod.**

## Where we are
- **Stage**: Pre-Production → **implementing**. Design frozen & consistent; architecture locked; Unity project live; Foundation + kaiju-parts done; weapons in progress.
- **Engine**: Unity 6.3 LTS, C#. Project opens; packages resolved (URP, 2D feature, Input System, Addressables, DOTS Entities/Burst/Collections/Mathematics, Test Framework).
- **Git**: everything committed & **pushed** to `github.com/akira103150146/kaiju-breaker` (origin/main). Working tree clean.
- **Review mode**: lean. **Director authorization standing**: autonomous design/implementation + direct commit (see memory [[user-autonomy-commit]]).

## Done (implemented, compiled, EditMode tests GREEN)
- **core-foundation** (6 stories): `Assets/_Project/Scripts/Core` — `IEventBus`/`TypedEventBus` (sync same-frame, zero-GC, re-entrant, deferred sub/unsub), shared enums (`Types/`), query interfaces (`IPartStateQuery`/`IDifficultyProvider`/`ISaveService`/`IWeaponTierQuery`), Bridge contract (`HitEvent`/`IBulletSimBridge`), `App/GameBootstrap`. Tests: CoreSharedTypes (6) + TypedEventBus (7).
- **content-config** (9 stories): `Assets/_Project/Scripts/Content` — 15 tuning ScriptableObjects (WeaponDef/WeaponBalanceConfig, PartSystemConfig/KaijuDef+PartDef, DifficultyConfig, GameFeelConfig, EmitterPattern/Movement/EnemyDef, Stage/Segment/PodDrop, Economy/Input/Save) + `ContentRegistry` + `ContentTestFactory` (reflection fixtures). Tests: ContentConfig (4).

## Done 2026-07-02 — kaiju-parts stories 001–005 (✅ 56/56 EditMode tests GREEN)
- **kaiju-parts** Logic stories 001–005: `Assets/_Project/Scripts/KaijuParts/` — `BreakablePart` (runtime two-bar model) + `PartStateSystem` (heat SM, armor gate/stagger, break+event emission, adjacency graph, M3 Tier-3 chain; implements `IPartStateQuery`, subscribes Laser/Wave/Missile/PartBroke). EditMode tests: `Assets/_Project/Tests/EditMode/KaijuParts/` (5 files) + helpers `RecordingEventBus`, `PartTestFactory`.
- **Reconciliations vs. story text** (followed committed Core/Content contracts — surfaced for review):
  1. **int IDs**: Core events use `int` part/kaiju/dropTable ids; `PartStateSystem` maps SO string ids → int at load (part id = declaration index; kaiju id passed to `InitializeParts(def, kaijuId)`; drop-table string→int table). Guard still validates the *string* drop id non-empty (throws `InvalidOperationException`).
  2. **config ownership**: heat/break/stagger knobs read from **`WeaponBalanceConfig`** (single source, per the CC-003 dedup), NOT `PartSystemConfig`. Added chain/adjacency knobs to `PartSystemConfig` (M3T3ChainDmgMult, M3T3ChainMaxTargets, M3ChainDamageBase, L2T3AdjacentHeatPct) — story 001 required them; content-config had omitted them.
  3. **event payloads extended**: `PartSoftened`(+CurrentHeat/MaxHeat), `PartStaggered`(+Duration/ArmorStripped), `PartStaggerEnd`(+ArmorRestored) — the story ACs assert these; only KaijuParts constructs them. Added Core `BreakState` enum. Kept committed names `BossCoreBroke`, `PartBroke.Type/Quality/AdjacencyIds`.
- **✅ Verified**: 56/56 EditMode tests pass (headless Unity 6000.3.0f1 runner, `test-results.xml`) — 31 KaijuParts cases covering every story 001–005 QA case + 25 pre-existing. One compile fix applied (test files needed `using KaijuBreaker.Content;` — bare `Content.` shadowed to the sibling test namespace). `.meta` files for the new scripts/folders will be generated on next Editor import (director).
- Story **006** (Softened/Broken readability) left to director: Visual/Feel — VFX onset + 5-tester recognition study (needs the Editor + playtest).

## Design & planning artifacts (all in repo)
- Design: `design/gdd/*.md` (12 systems + 3 kaiju), `design/art-bible.md`, `design/systems-index.md`, `design/registry/entities.yaml`.
- Architecture: `docs/architecture/architecture.md`, `adr/0001-0006`, `control-manifest.md`, `tr-registry.yaml`, `architecture-review-2026-07-02.md`.
- Backlog: `production/epics/` — 13 epics, 97 stories (index.md is the map). `production/sprints/sprint-01.md`.
- CI: `.github/workflows/ci-tests.yml`. Prototypes (throwaway, HTML): `prototypes/weapon-feel-concept`, `prototypes/vision-slice`.

## Key locked decisions
- Weapons: 2 pools (雷射×4 / 飛彈×4), dual-track **蓄熱軟化→衝擊擊破**, D₀ equal-power sidegrades, Tier 0–3.
- Materials: **kaiju-theme core sourcing = every part of a kaiju drops its theme core** (Option A). shard=universal, essence=full-clear.
- Stage drops: **cycling weapon-pod** (pool-typed, ~3s cycle) that **dwells in the reachable band ~12s** so the player waits for the weapon they want; elites are the pod source; mobs are prefabs (Movement+Emitter SO).
- Difficulty: 4 tiers scale bullet density ONLY (pillar 難度是門); TTB/output/materials invariant.
- Architecture: typed struct event bus + DI query interfaces; **hybrid DOTS(BulletSim)+MonoBehaviour** (ADR-0001 **Proposed**, pending perf spike); ScriptableObject config (ADR-0003); UI = SpriteRenderer bars + UGUI (ADR-0006).

## Blocked (need the Unity editor / director's machine)
- **ADR-0001 perf spike** (`bullet-sim/story-001`: 1000 bullets @60fps, 0 GC) → LOCKs ADR-0001 → unblocks 8 bullet-sim + 3 kaiju-encounter stories.
- **Touch-feel spike** (`input/story-001`).

## How to resume implementation
Next unblocked work = **Core systems** (pure C#, EditMode-testable, no DOTS). Recommended order in `NEXT-STEPS.md`. Start with **kaiju-parts** (dual-track state machine + `on_part_break(break_quality)` emitter — the hub the whole combat chain hangs off). Each system: implement in its `Assets/_Project/Scripts/<Module>` + EditMode tests in `Assets/_Project/Tests/EditMode/<Module>`, using `ContentTestFactory` for config fixtures and a fake `IEventBus`/query for isolation.
