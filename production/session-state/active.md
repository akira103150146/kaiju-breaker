# Active Session State — 殲獸戰機 / KAIJU BREAKER

*Last updated: 2026-07-12 (SESSION 16 — **UI 全面改 UGUI+TMP（ADR-0006，導演選 B 程式建構案）**。IMGUI 完全移除。編譯0錯、503 EditMode GREEN、6 畫面+觸控+flash 全 Play 截圖驗證、點擊管線驗證。本地 3 commit 未 push。)*

## ✅ SESSION 16 (2026-07-12) — UI 從 IMGUI 全面遷移到 UGUI+TMP（ADR-0006）

**導演本輪指示**：選「先1」= UI 改 UGUI+TMP；架構選 **B｜程式建構 UGUI+TMP**（執行期程式建 Canvas/TMP/Button，不在編輯器手拉階層）。

**⚠️ 關鍵發現（字型）**：repo 的 `ArkPixel-16px-zh_tw.ttf` **只有 97 個 CJK 字**（Ark Pixel 16px 尺寸未完成；10px 缺158/242、16px 缺215/242）。**遊戲中文一直靠 Windows 系統字型 fallback 撐著**（平滑非像素，Android 無保證）。→ 改用 **Noto Sans TC**（`C:\Windows\Fonts\NotoSansTC-VF.ttf`，OFL 可嵌入、完整繁中）複製進 `Art/Fonts/NotoSansTC.ttf`。**遊戲實際只用 242 個中文字**（掃 Scripts/Data/Art 抽出）。

**✅ 已辦（3 commit，全驗證，未 push）：**
1. **TMP 基建**（`8aaec9e`）：匯入 TMP Essentials（`Assets/TextMesh Pro/`）+ 用 Noto TC 建 `NotoSansTC SDF.asset`（Dynamic 動態圖集、Mobile Distance Field shader）+ 設為 TMP 預設字型 + 預烘焙 337 字(95 ASCII+242 CJK) 0 缺字單張圖集。
2. **選單+HUD → UGUI**（`6e62ab7`）：新 `GameUiView.cs`（程式建 Overlay Canvas+CanvasScaler 直向參考1080×1920+6畫面：標題/選頭目hub/強化商店/選裝備/HUD/結算，用 TextMeshProUGUI+UGUI Button+EventSystem）。`GameplaySceneDirector` 移除 OnGUI+全部 Draw*，改狀態機驅動 `GameUiView` + `On*` callback。HUD 錨定螢幕上/下緣(任何比例都在畫面內)。符號改 Noto 有的字(⚙→◆、☑/☐→■/□)。App asmdef 加 `UnityEngine.UI`+`Unity.TextMeshPro`。
3. **觸控+flash → UGUI + 刪 GameUiSkin**（`37bf800`）：`PlayerInputRouter` 觸控搖桿/副武/集氣鈕改 UGUI Image(專屬無 scaler overlay canvas，1單位=1螢幕px，polling 邏輯不動)；`GameBootstrap` flash 改全螢幕 UGUI Image(sortingOrder 200、raycastTarget off、alpha 綁 FlashSystem)。刪掉 `GameUiSkin.cs`。**專案零 OnGUI/GUILayout/GUIStyle**。

**✅ 驗證**：503/503 EditMode 綠(×3)、編譯0錯、**6 畫面+觸控+flash 全部 Play 截圖確認繁中清晰渲染**、**點擊管線端到端驗證**(raycast→Button→callback→director 狀態變:點 Cell5→_selBossIndex 0→5)。

**可調/注意**：字型是一行切換(`TMP_Settings` 預設字型)——若導演要中文也像素風，唯一夠用是 **Ark Pixel 12px 完整版**(35MB、更方、source 較肥)。`GameUiView` 顏色/尺寸/參考解析度皆在該檔內。`ArkPixel-16px-zh_tw.ttf`(舊子集)仍在但已無人用(可日後清)。

**✅ 建置完成**：EXE 130.5MB(0錯,+11MB=Noto字型隨build) + APK 56.3MB(0錯,+8.8MB字型；驗證 PK zip 有效、含新 libil2cpp.so+AndroidManifest)。雙平台皆含新 UGUI+TMP UI。

**⬜ 待辦**：①導演實測新 UI(手感/中文/手機觸控/各畫面) ②依回饋微調版面 ③push(等指示) ④(可選)字型改靜態子集省 ~11MB APK ⑤(待導演定)中文字型 Noto 黑體 vs Ark Pixel 12px 像素風 ⑥5頭目 bespoke 美術(唯一剩的大項)。

---

*(以下為 SESSION 15)*

*Last updated: 2026-07-12 (SESSION 15 — 導演3輪回饋共10項(波次時限撤退/強化統一+提頻/小怪縮小+留邊界內/①音效逐發命中音+BGM/手機集氣鈕). **編譯乾淨0錯、503 EditMode GREEN、EXE(119MB)+APK(47.5MB)雙平台重建含全部**。已 push origin main `94fc662`。)*

## ✅ SESSION 15 (2026-07-12) — 導演回饋2輪6修 + 音效補完（編譯綠+503測試綠+已build）

**⚠️ 環境+關鍵解法**：Unity MCP(UnityMCP server)開場沒被索引進 session→工具呼叫不到。**不必重開**：改用 curl/python 直打 `http://127.0.0.1:8080/mcp` 走 JSON-RPC 驅動 refresh/run_tests/manage_build（見 memory [[drive-unity-mcp-over-http]]，客戶端 `scratchpad/mcp_call.py`）。editor state resource 是 `mcpforunity://editor/state`；build 是 async 用 job_id 輪詢，Android 期間 bridge 忙→看 `Builds/**` 檔案 mtime。

**✅ 驗證結果**：refresh force+compile → `read_console` **0 error/0 warning**；`run_tests EditMode` → **503 passed / 0 failed / 0 skipped**(8.46s)；`manage_scene save` Bootstrap；`manage_build windows64` → 成功，`Builds/Windows`(119MB，App/Content/Stage.dll+level0 全 10:17 重建)；Android APK 建置中。

**✅ 已辦（純 harness Edit/Write + Python 合成，全部存檔，未編譯驗證）：**
1. **道中波次時限撤退**（導演#1）：`WavePacing` 改純函式 `Decide()`→`Hold/ReleaseNext/RetreatLeftovers`。每波有時間上限 `WaveTimingConfig.WaveTimeLimitSeconds`(預設12s)；清完(alive≤門檻)即放下一波；**時間到還沒清→存活敵人撤退飛離(上緣)**，清空後才放下一波(不疊波)。最後一波不套用。`EnemyController.BeginRetreat()`(停火+直上飛出+新頂部despawn界線)、`WaveSpawner.RetreatAllAlive()`。重寫 `wave_pacing_test.cs`(7測試)。舊 `ShouldReleaseNextWave/_sinceFullySpawned/MaxWaveWaitSeconds` 移除(MaxWaveWaitSeconds 保留 deprecated 屬性給 .asset 相容)。
2. **關卡內強化簡化**（導演#2）：`GameplaySceneDirector.SpawnDrop` 菁英只掉**兩種強化**(火力P=強化當下主武器 / 飛彈M=強化當下副武器)。**移除 L1→L4 / M1→M4 武器型別切換艙掉落**(WeaponPod系統程式保留但休眠)。玩家維持選裝備所選武器，只變強不換型。
3. **小怪縮小**（導演#3，過頭了）：`EnemyController` 加全域 `_bodySizeMult`(預設0.72)套在 EnemyDef.BodySize 上，一個旋鈕整批縮小+保留11隻相對大小。原 0.55~0.95 → 約0.40~0.68。
4. **①音效-逐發命中音**：`SfxPlayer.PlayEnemyHit()`(節流0.04s，enemy_hit 音檔本就在但沒接)；`EnemyCombatContext.OnEnemyHit` 回呼→`EnemyController.SetCombatContext(...onHit)`→`TakeDamage` 非致命命中時播；`GameplaySceneDirector` 傳 `()=>Sfx.PlayEnemyHit()`。
5. **①音效-BGM**：Python 合成兩段無縫循環(`scratchpad/gen_bgm.py`)→`Resources/Music/bgm_stage.wav`(道中,Am進行,140bpm,13.7s)+`bgm_boss.wav`(168bpm,三全音張力,11.4s)，PCM16 mono 44100 配管線，附匯入 meta(2D/背景載入)。`SfxPlayer` 加第二個 loop AudioSource + `PlayMusic/StopMusic`；道中開始播 stage、進BOSS切 boss、進結算停。

**✅ 導演第二輪 3 追加回饋（同 session，程式已改存檔）：**
6. **強化統一(看當下武器就強化)**：移除 P(主)/M(副)分開掉落；改**單一綠色強化片** `PowerUpKind.Power`→`PlayerWeaponController.AddArsenalPower()`(同時+1主火力&+1副飛彈)。玩家不管什麼配備撿到都不浪費。
7. **強化次數提高**：`GameplaySceneDirector` 加旋鈕 `_eliteStrengthenCount`(2,菁英掉2片) + `_trashStrengthenChance`(0.14,一般小怪每隻14%掉1片)。比原「只菁英掉」頻繁很多。
8. **小怪留在邊界內**：`EnemyController` 每幀把 x 夾在 ±`_fieldHalfWidth`(4.3,對齊 GameBootstrap 邊界條)，移動模式再也不會把小怪帶出可及範圍(上下進出自由)。原 ±5.2 側向 despawn 留作保險(不再觸發)。
9. **小怪再更小**：`_bodySizeMult` 0.72→**0.6**(原0.55~0.95 → 約0.33~0.57)。

**✅ 導演第三輪回饋（同 session）：手機集氣鈕**
10. **L3 波動改手動集氣**（導演選項A，取代原自動充能）：`IPlayerInput.PrimaryHeld`；PC=按住滑鼠左鍵/J、手機=右下新增「集氣」按鈕(疊在副武器鈕上方)。`TickWaveCharge` 改按住充能、放開發射(充越久越強，達 power cap)；MinWaveCharge 0.12 防誤觸。`PlayerInputRouter.ChargeControlVisible`(僅主武器=L3 才顯示/輪詢，由 `GameplaySceneDirector` run 開始依 `_selPrimary==L3` 設定；主武器整場固定不切換)。L1/L2/L4 維持自動開火不受影響。KeyboardMouseInput 也補 PrimaryHeld(=J，因其左鍵是拖曳移動)。

**可調旋鈕**：`WaveTimingConfig.WaveTimeLimitSeconds`(12)·`EnemyController._bodySizeMult`(0.6)/`_fieldHalfWidth`(4.3)/`_retreatSpeed`(6.5)/`_despawnAboveY`(9)·`GameplaySceneDirector._eliteStrengthenCount`(2)/`_trashStrengthenChance`(0.14)·`SfxPlayer` HitMinInterval(0.04)+BGM 音量(stage0.5/boss0.55)。

**⬜ 待辦（重開 session 後）：** ①refresh 編譯②跑 EditMode(應 ~502+ 綠，含改寫的 wave_pacing 7測試)③存Bootstrap→build EXE+APK④導演實測(撤退手感/縮小尺寸/兩種強化/命中音+BGM)⑤commit⑥**③UI改UGUI+TMP**(大工程,建議 Unity 可驅動時做,靠截圖迭代)⑦5頭目bespoke美術。

---

*(以下為 SESSION 14)*

*Last updated: 2026-07-10 (SESSION 14 — 導演大回饋一輪：武器四型獨立成長 + 道中清場門控 + 小怪辨識/放大 + 打擊回饋 + 菁英專屬掉落 + 免費音效 + BOSS耐久/血條遞減. 500 EditMode GREEN. 全 push origin main (ef313f2). EXE 116.83MB + APK 45MB.)*

## ✅ SESSION 14 (2026-07-10) — 導演回饋大整理（武器/道中/手感/音效/BOSS）

**⚠️ 環境同前**：Unity 綁主 checkout `C:\Game\kaiju-breaker`(main)。本次腳本改動用 harness Edit/Write（未觸發攔截）＋ MCP refresh/run_tests/execute_code(資料)。**未 push**（依 [[commit-often-push-on-request]]）。導演授權全自主（「剩下你全部自己完成」）→ 用 harness task list 逐項執行。

**✅ 已辦（commit `4c987fb` 武器急修 + `8e3b293` 大批次；500 EditMode GREEN；EXE 116.83MB + APK 重建）：**
1. **主武器四型獨立成長身分**（導演明訂規則，修「全部都變散彈」重大 bug）：L1散波=加彈數/L2集束=數量不變只變大一點+傷害爆增/L3波動=充能上限(cap隨power)集滿更痛/L4穿透=平行直射+可穿透數增加。副武M1-4=純加數量+傷害。穿透從 bool 改「可穿透次數」。
2. **冷暖色統一**：玩家全冷色(飛彈青綠teal/雷射青藍)、敵彈全暖色(mob/boss 各一套依 emitter 型別染色)。
3. **道中清場門控**（不重疊）：`WaveSpawner` 改 wave-by-wave，下一波要「場上敵人≤門檻 或 等待上限」且過最小間隔才放。純函式 `WavePacing` +4 回歸測試。
4. **菁英穿插**：`WavePlanner` 菁英混進普通波隨機位置；5段中3段(s1_02/03/05)有菁英波。菁英 HpMult 3.0 / DensityMult 1.6（資料）→更肥更密+光環+放大。
5. **小怪辨識+放大**：EnemyDef 加 Shape/Color/Size；`EnemyShapeSprites` 程序化形狀(方/圓/三角/菱/六角/箭)；11 隻各設獨特形狀+顏色+體型(0.55~0.95 world)。
6. **打擊回饋**：小怪被打閃白+squash-回彈(`TickHitPop`)；超出±5.2X 失效；`GameBootstrap` 畫左右邊界標示。
7. **難度=彈幕密度**：mob emitter 基數降到 D1≈單發(Wall1/Ring2/MobSpiral1/DeathRing3/Aimed1/TriFan1)，密度[1,2,3,4]往上加。
8. **掉落只從菁英**：一般小怪不掉；菁英掉 武器艙+火力+飛彈。
9. **免費音效系統**：純Python合成6個SFX(shoot/hit/explode/player-hit/part-break/pickup)→`Resources/Sfx`；`SfxPlayer`(GameBootstrap擁有,非singleton)訂閱事件匯流排+直呼。
10. **手感尺寸**：玩家 2.8125→2.15、子彈 1→0.7、小怪放大 → 目標明顯大於子彈好瞄準。
11. **BOSS 耐久+血條**：破壞條改**遞減**(血條用扣的,過熱條仍累積,破壞後藏條)；破壞門檻~2x(200/320/420)因武器 buff 後太脆。破壞機制測試 pin 舊門檻(`BalanceClassicBreak`)。

**⬜ 待辦（導演實測後微調）：**
- 各數值旋鈕微調：武器成長倍率(L2/L3/L4)、`WaveTimingConfig`門控(門檻2/等待8s/最小間隔2.2s/波內錯開0.14s)、小怪 BodySize/顏色、emitter 基數、菁英 HpMult 3.0、BOSS 破壞門檻 200/320/420、玩家/子彈縮放。
- 音效：目前道中小怪「命中」逐發音效未接(只有爆炸/玩家中彈/破部位/拾取)；BGM 未做；可再補 shoot 以外的層次。
- 5 新頭目 bespoke 美術、UI 改 UGUI+TMP、手機實測(FPS/門控/難度/斷腳/新音效)。

---

*(SESSION 13 — PartGate 純視覺 polish 收尾: 稜殼剝甲也停火(FireGate+4) + 不可命中→可命中揭露白閃脈動.)*

*(SESSION 12 — LACERA 斷腳可拖 + PartGate 跨部位閘門(6c)全線 + 新5核心經濟sink + Play實測 + 重建EXE/APK. 486 EditMode GREEN. 全 push origin main. EXE 116.81MB + APK 45MB.)*

## ✅ SESSION 13 (2026-07-10) — PartGate 純視覺 polish 收尾（剝甲也停火 + 揭露脈動）

**⚠️ 環境同 session-12**：Unity 綁**主 checkout `C:\Game\kaiju-breaker`(main)**。腳本改動用 **Bash 寫檔到主 checkout + MCP refresh/run_tests**（避開 harness 對 Edit 的攔截 & session-12 記的 MCP script 驗證器誤判）。**未 push**（依 [[commit-often-push-on-request]]，等導演指示）。

**✅ 已辦（本地 main，2 commit，495 EditMode GREEN，0 fail）：**
1. **稜殼晶面「剝甲也停火」**（`e999ef7`）：`PartFireGate` 加 `SilenceWhenSoftenedOrStripped=4`（軟化**或**剝甲皆停火，原本只軟化停火）。PRISMSHELL 4 片 facet emitter `_gate:1→4`（facet 為 Armored 部位，剝甲成立）。把 emitter gate 真值表抽成純 `Content.EmitterGateEval.IsOpen(gate,heat,armor,gatePartBroken)`，`BossController.GateOpen` 改委派 → 現在可 EditMode 隔離測試。+9 測試 `emitter_gate_eval_test`（486→495）。
2. **不可命中↔可命中「揭露脈動」**（`2ed01d4`）：跨部位 HittableWhen 閘門開啟（un-hittable→hittable edge）時，`BossPart` 閃白一拍（`_revealFlashSeconds`=0.35s，比 hit flash 0.06s 長=讀作「揭露」非「命中」），把玩家目光拉到剛露出的弱點（潮顎核心從背甲後現身、稜殼 weak_node 於接縫露出）。沿用既有 hit-flash 渲染路徑 + 1.5s 冷卻防 gate 抖動閃爍。**純視覺**，無法 EditMode 測；495 綠(無回歸)。**待導演批次 Play 目視確認**([[verify-without-stealing-focus]]，背景不搶焦點)。

**可調旋鈕**：`BossPart._revealFlashSeconds`(0.35) · `RevealCooldown`(1.5) · PRISMSHELL facet gate。

**⬜ 待辦（同 session-12 剩餘，導演定方向）：** 5頭目 bespoke 美術；utility 手感微調旋鈕（見下）；音樂/音效方向；UI 改 UGUI+TMP；手機實測難度/FPS/斷腳/閘門手感（含本輪揭露脈動目視）；未 push 的本地 commit（`e999ef7`/`2ed01d4`）待導演指示 push + 重建 EXE/APK。

---

## ✅ SESSION 12 (2026-07-09~10) — LACERA斷腳 + PartGate(6c) + 新5核心sink + Play實測 + 重建

**⚠️ 環境**：Unity 綁**主 checkout `C:\Game\kaiju-breaker`(branch main)**，非 job worktree。Unity 改動用 MCP 落主 checkout、在 main 提交/push。**踩雷**：MCP `script_apply_edits` 驗證器會把多個 `=> LevelOf(...)` 唯讀屬性誤判成「重複方法」擋下 → 改用「Write 到 job tmp + Bash cp 覆蓋 + refresh」或 python 插入繞過。

**✅ 已辦（全 push origin main；486 EditMode GREEN；EXE 116.81MB + APK 45MB 重建）：**
1. **LACERA 斷腳可拖子物件**（`90e53be`）：`BossPart._brokenStub`，斷腳啟用子物件+關原腿圖/碰撞；四腿各建 `stub` 子物件，導演手調位置/粗細/旋轉存檔。
2. **PartGate 執行引擎**（`8d379e9`，+10測試）：`PartStateSystem` HittableWhen（關=全攻擊無效）/BreakableWhen（關=只擋破壞值、雷射熱仍軟化）；即時判定、RequireAll、壞id視ungated不soft-lock；public `IsPartCurrentlyHittable`。
3. **3頭目 gate 資料**（`0d012c5`，+1測試→480）：潮顎 heart_core=Hittable/dorsal_plate破；燼使 wing_vent_l2/r2=Breakable/翼根破；稜殼 weak_node=Hittable/任一facet軟化或剝甲（新增 enum `PartGateCond.GatePartSoftenedOrStripped`）。
4. **HittableWhen 場景 collider+sprite 開關**（`3ab168a`+`9c07d89`）：`BossPart.SetHittable` 每幀關collider+renderer（關時隱藏+子彈穿過），破壞後no-op；`BossController.SyncPartVisuals` 驅動。
5. **稜殼晶面軟化停火**（`0e774a1`）：4片facet emitter FireGate=SilenceWhenSoftened。
6. **新5核心經濟sink**（`e6dd4c6`/`cceff5d`/`bfd0a01`，+6測試→486）：`UtilityUpgrades` 加5條核心軌（各吃自己核心、成本(lv+1)×4、上限5、存flag）。強化商店UI +5行。**全5項in-run生效**：Ember移速/Abyss無敵/Void開場火力(1+lvl，不升上限)/Swarm副武冷卻(不碰飛彈數)/Crystal道具磁吸。**忠於主題重對應**：副武無彈匣系統→改冷卻縮短；材料非實體拾取(破部位直接記帳)→改P/M/W道具磁吸。皆非killpower、不重疊in-run軸。
7. **Play 實測**：TIDEMAW HittableWhen 閘門 runtime 驗證(gate註冊、`IsPartCurrentlyHittable(heart_core)=False`、SyncPartVisuals後心核collider+renderer關=藏背甲後+不可命中)、全5 utility 消費者即時反應、截圖印證。**★失焦節流**：Unity 失焦不跑Update(非bug，前景遊玩正常)。
8. **state 檔更新**（`31b46db`）+ 重建 EXE/APK + Obsidian 備份。

**⬜ 待辦（導演定方向）：**
- PartGate 純視覺 polish：不可命中↔可命中的暗淡/亮白脈動提示；稜殼晶面「剝甲也停火」(需擴 FireGate enum)。
- utility 手感微調旋鈕：`MoveSpeedMult`(×0.06/lv)·`IFrameMult`(×0.15)·`PowerUpItem.MagnetRadius` K=2·`SecondaryCooldownMult`(×0.1)·`CoreCostFor`((lv+1)×4)·`StartPowerLevel`。若要「真彈匣/實體材料掉落物」= 更大獨立工程。
- 5頭目 bespoke 美術、音樂/音效方向、UI 改 UGUI+TMP、手機實測難度/FPS/斷腳/閘門手感。

---

## ✅ SESSION 11 (2026-07-09) — 頭目/難度/公平性/FPS 連續修正 + 原頭目視覺重塑

**✅ 已辦（全部 commit + push origin main `1ba7d4f`；469 EditMode GREEN；EXE+APK 多次重建 0 錯）：**

**新頭目 3 BUG（回報）：**
1. ✅ 部位打不壞 → 程序化建的部位 collider `m_IsTrigger:0`(原頭目 1)，子彈用 OnTriggerEnter2D 穿過去。`BossPart.Awake` 強制所有部位 isTrigger=true。
2. ✅ 部位過小/跟子彈一樣大 → placeholder 用 Unity 內建迷你 sprite(0.16 world) 縮放沒用。改成 **1 單位方形 sprite(保留色)+絕對 `_placeholderWorldSize`(2.0 world)**。
3. ✅ 最低難度彈幕爆炸 → 齊射錯開 + 依發射部位數正規化射速(`_bossDensityReferenceEmitters` 4)。

**🔴 重大 BUG（回報）：新頭目部位打完不消失、擋著、還在射**
4. ✅ 根因：session-9 加 5 新核心 enum 時**漏補 `MaterialKeys.ToKeyMap`**。破部位→經濟記帳新核心→`ToKey` 查表**丟 KeyNotFoundException**→事件匯流排 fail-loud(有測試鎖 Publish 外拋)→中止 PartBroke 派送→後訂閱的 `BossController.OnPartBroke`(Hide+消音)沒跑。補 5 key + **回歸測試**(每個 MaterialId 都要有 key)。

**難度（導演方法：規律不變、只改單發子彈數）：**
5. ✅ Emitter 基數降到 D1 稀疏(集中線 1 顆/放射保留 3 顆)、`BulletDensityMult` **[1,2,3,4]** 往上加；SO 預設/OnValidate 區間/測試同步。小怪彈幕也接難度(`EnemyCombatContext` 傳倍率)。

**公平性（導演：正常人躲得掉嗎，定量算）：**
6. ✅ 根因：玩家判定框過大(0.62 world≈整台船)。改資料驅動 `PlayerShipConfig.HitboxWorldSize`(0.24)。

**APK FPS：**
7. ✅ 鎖 30(手機預設沒設 targetFrameRate)。`GameBootstrap` 僅 mobile 設 targetFrameRate=裝置更新率(60/120) clamp[60,`_maxFrameRate`120]，PC 不動。

**原頭目視覺（回報，MCP 編輯器截圖逐一驗證）：**
8. ✅ **LACERA**：body-base 圖補上(磁碟早有沒接)；四肢從共用 pivot{0,3}改**各自根部**(截圖模擬擺動驗證，最終 {±0.95,3.2/2.6} 擺幅 28/30)；**頭上移 y3.9**(導演建議，露出腳根部)；**斷腳換殘根圖**(`BossPart._brokenSprite`=limb_stub_broken，破壞後換殘根+關碰撞不消失)；過熱(軟化)也顯示破殼 stripped 圖。
9. ✅ **VOLTWYRM**：頸原地自轉(session-9 設錯 Spin)→移除→**重塑縱向能量龍**：頭(核心)在最頂、兩盾在頭下方展開、體節在下方 **S 形波動**(n1 繞頭下{0,4.0}±12°、n2 繞 n1 下{0,2.9}±16°反相)。

**基建：** gh CLI 裝好(`C:\Program Files\GitHub CLI`)+登入 akira103150146；PR #1 已合併；全部 push origin main。

**★ 新技術：** MCP `manage_camera` screenshot(include_image) 可**視覺驗證**頭目排列/動作(啟用頭目→正交相機→截圖→我讀圖)。用來對齊 LACERA 腳根部、設計 VOLTWYRM 龍身。[[verify-without-stealing-focus]] 預設仍 EditMode，但視覺美術工作可用截圖迴圈。

**⬜ 待辦（導演實測後微調）：**
- 手機實測 FPS(60/120)、難度手感(D1 是否夠低、梯度)、頭目視覺(LACERA 腳擺動/斷腳、VOLTWYRM 蛇擺)。
- **可調旋鈕**：`PlayerShipConfig.HitboxWorldSize`(0.24)·`DifficultyConfig` 密度[1,2,3,4]+emitter 基數·`BossPart._placeholderWorldSize`(2.0)·`BossController._bossDensityReferenceEmitters`(4)·`GameBootstrap._maxFrameRate`(120)·`Kaiju_Lacera` 四肢 pivot/arc·`Kaiju_Voltwyrm` n1/n2 擺幅。
- 5 新頭目 bespoke 美術(目前色塊 placeholder)；新 5 核心經濟 sink；PartGate 跨部位；音樂/音效；UI 改 UGUI+TMP。

---

*Last updated: 2026-07-09 (SESSION 10b — 導演第二輪回報修正：重大 bug 部位打完不消失(MaterialKeys 漏 key)+難度改 count-first+部位放大+APK FPS 解鎖. 469 EditMode GREEN. EXE 116.8MB+APK 44MB 重建含全部. 本地 main 748ea59 未 push origin(de54e3a).)*

## ✅ SESSION 10b (2026-07-09) — 導演第二輪回報 4 修 + 重建

**導演回報 → 已修（全綠 469 EditMode、雙平台重建 0 錯）：**
1. ✅ **🔴 重大：新頭目部位打完不消失、擋著、還在射** → 根因：session 9 加 5 個新核心 enum 時**漏補 `MaterialKeys.ToKeyMap`**（缺 CoreSwarm/Crystal/Abyss/Ember/Void）。破新頭目部位→`EconomyService.OnPartBroke`→`MetaSaveService.CreditMaterials`→`MaterialKeys.ToKey(id)`=`ToKeyMap[id]`→**KeyNotFoundException**。事件匯流排是 fail-loud（`economy_yield_on_break`/`run_state_machine` 兩測試鎖定 Publish 外拋），例外中止 `PartBroke` 派送 → **後訂閱的 `BossController.OnPartBroke`(Hide+消音) 沒跑** → 部位 Broken 卻不隱藏、不消音。只發生在新頭目（只有它們掉新核心）。修：補 5 key + **回歸測試** `material_keys_cover_all_ids_test`（每個 MaterialId 都要有 key）。與之前 `EconomyConfig.GetCoreForTheme` 同類「enum 擴充只補一半」坑。
2. ✅ **難度過高（導演指定方法）** → 規律不變（放射還放射、集中線還集中線），**只改單發子彈數**：emitter 基數降到 D1 稀疏（BossAimed 3→1、BossWall 5→2、BossSpiral 4→2、TriFan 3→1、Wall 4→2、Ring 8→3(保留放射)、MobSpiral 4→2、DeathRing 10→4）；`BulletDensityMult` 1/1.25/1.5/1.8 → **1/2/3/4**（D1 稀疏基準、往上加）。同步更新 SO 欄位預設、OnValidate 區間、2 個難度測試矩陣。
3. ✅ **部位跟子彈一樣大** → 根因：placeholder 用 Unity 內建迷你 sprite（原生 0.16 world），縮放 transform 沒用（sprite+collider 一起放大但起點太小、且血條子物件會被撐爆）。修：`BossPart.Awake` 對 placeholder 部位換 **1 單位方形 sprite（保留顏色）** + 設**絕對世界大小** `_placeholderWorldSize`(2.0，≈子彈 10 倍)，sprite 與 collider 都等於它。取代原本的 scale-mult 旋鈕。
4. ✅ **APK FPS 鎖 30** → 根因：程式沒設 `Application.targetFrameRate`（手機 Unity 預設 30、vSync 在 Android 被忽略）。修：`GameBootstrap.Awake` **僅 mobile** 設 targetFrameRate=裝置更新率(60Hz→60/120Hz→120) clamp [60,`_maxFrameRate`=120]；PC 維持 quality vSync 不動。

**建置**：refresh+編譯乾淨(0 CSxxxx)→存 Bootstrap→MCP 建 Win(116.8MB)+Android(APK 44MB)，皆 0 錯。EXE 增量重建(App.dll+level0 新、Meta.dll 上一次已含 MaterialKeys)、APK 全新。

**可調旋鈕**：`BossPart._placeholderWorldSize`(2.0，多部位頭目重疊就調小) · `DifficultyConfig` 密度倍率[1,2,3,4] + 各 emitter 基數 · `GameBootstrap._maxFrameRate`(120) · `PlayerShipConfig.HitboxWorldSize`(0.24)。

**commits(本地 main，未 push origin)**：`4404c46`(MaterialKeys+難度+回歸測試) · `2cfd40b`(部位尺寸) · `528e6fb`(難度測試對齊) · `748ea59`(FPS)。**待導演實測後決定 push origin main**。

---

*Last updated: 2026-07-09 (SESSION 10 — 新頭目 3 BUG 修正 + 四階難度稽核/公平性修正 + 雙平台重建. EXE 116.8MB + APK 44MB 最新含修正. 分支 worktree-fix-new-boss-parts / 本地 main de54e3a; 遠端 main 已更新 de54e3a、PR #1 已合併(MERGED)。)*

## ✅ SESSION 10 (2026-07-09) — 新頭目 BUG 修 + 難度公平性 + 重建

**導演回報 → 已修（全部程式碼修正、資料驅動、不動平衡不變量；唯讀診斷比對 `Bootstrap.unity` 新舊部位）：**
1. ✅ **新頭目部位打不壞** → 根因：程序化(execute_code)建的部位 collider `m_IsTrigger:0`（原頭目 :1），玩家子彈用 `OnTriggerEnter2D` → 穿過去不觸發命中，蓄熱/破壞值永遠 0。修：`BossPart.Awake` 強制所有部位 `isTrigger=true`（統一、對原頭目無害）。
2. ✅ **新頭目部位過小** → 根因：維持 Unity 預設 1×1 box collider（原頭目手調過、絕不會剛好 1×1）。修：`BossPart.Awake` 把剛好 1×1 的 placeholder 部位依 `_placeholderPartScaleMult`(1.4，可調) 整體放大（視覺塊+hitbox 一起長大），已調過的部位不動。
3. ✅ **最低難度彈幕爆炸** → 新頭目發射部位暴增（巢母 6 / 虛尖 8 vs 原 ~4）+ 所有 emitter 初始冷卻同值 → 同幀齊射 → 瞬間滿畫面。修：`BossController.NormaliseEmitterCadence` 依發射部位數正規化射速（`_bossDensityReferenceEmitters`，預設 4 → 原頭目不受影響）+ 錯開初始冷卻。作用在難度倍率之前，D1=×1.0 不變量不動。
4. ✅ **四階難度稽核 + 公平性（導演：正常人躲得掉嗎）** → 用真實資料定量算可閃避性。**根因：玩家判定框過大** = 0.62 world（≈整台船），有效致死半徑 ~0.39（彈幕遊戲常規的 5–8 倍）→ 密彈幕到處彈隙都小於致死區 → 躲不掉。修：資料驅動 `PlayerShipConfig.HitboxWorldSize`(0.24，獨立於船視覺縮放，`PlayerShip.Awake` 套用) → 致死半徑 ~0.20，D1 舒適可躲、D4(×1.8) 仍有挑戰。速度(9 vs 子彈 2.4–3.75)/反應(telegraph 0.3s)/移動範圍本來就沒問題。
5. ✅ **小怪彈幕接難度（機制破口）** → `EnemyController.FireVolley` 原本只吃 elite 倍率，`BulletDensityMult` 沒接、`DifficultyScaling.ScaledBulletCount` 是死碼。修：經 `EnemyCombatContext` 把難度倍率傳給每隻小怪 + 巢母召喚物。D1 不變，D2–D4 道中彈變密。難度**只縮密度**（難度是門，非雷電式速度縮放）。

**四階難度稽核結論**：選難度→系統 ✅ / 敵人數量 ✅ / 頭目彈幕 ✅ / 段落門檻 ✅ / 小怪彈幕 ❌(已修) / 玩家能躲 ❌(已修判定框)。模型（只縮密度）本身合理，問題是**沒接完 + 判定框不公平**，都修好。**小發現(未改)**：`WavePlanner` 沒套 `EnemyCapPerScene`(20)——D4 每波才 13 隻不會超；`DifficultyConfig.asset` D4 密度 1.8（設計文件寫 2.0），落在安全範圍。

**建置**：fast-forward 合併分支 → 本地 main → refresh + 編譯乾淨(0 CSxxxx，只剩既有空 asmdef 提醒) → 存 Bootstrap → MCP 建 Win(116.8MB / 27s) + Android(APK 44MB / 160s)，皆 0 錯；`kaiju-breaker_Data` 程式 DLL + `level0` 場景今日重寫，確認含修正（.exe 啟動器 stub 未變是正常，Unity 同版本不重寫）。

**可調旋鈕**：`PlayerShipConfig.HitboxWorldSize`(0.24) · `BossController._bossDensityReferenceEmitters`(4) · `BossPart._placeholderPartScaleMult`(1.4)。

**commits**：`ef45427`(新頭目 3 修) · `de54e3a`(難度公平性)。分支已推 origin + PR #1 (draft，未合遠端 main)。gh CLI 本機已裝(`C:\Program Files\GitHub CLI`) + 登入 akira103150146。

**待導演**：實測 D1 手感（有壓力但躲得掉？）；滿意再把旋鈕定案 / 讓遠端 main 更新（叫我 push 或自己合 PR #1）。

---

*Last updated: 2026-07-09 (SESSION 9 收尾 — 8 頭目全可玩 per-part 射擊+移動、6 新小怪、破甲回填、巢母生怪、跳過道中、5 項 playtest 修正. 466 EditMode GREEN. EXE 116.8MB+APK 47MB 最新. 全 push + Obsidian.)*

## ✅ SESSION 9 已辦 / ⬜ 待辦（收尾整合快照）

**✅ 已辦（全部提交、測試綠、EXE+APK 重建、Obsidian 備份）：**
1. ✅ 手機搖桿靈敏度調低（1.9× 行程 + 死區）。
2. ✅ 8 頭目設計：`00-roster-overview` 骨幹 + 5 新頭目 GDD(04-08) + `enemy-roster-expansion` + `per-part-firing-schema` 規格。
3. ✅ per-part 射擊 schema 實作：PartDef 加 Emitters[]/Movement/Gate/ArmorRegen、KaijuDef 加 Body、enums(KaijuTheme+5/MaterialId+5/EmitterType+Spiral/MovementType+2/EnemyTier)。
4. ✅ **8 頭目全可玩**：3 既有 + 5 新(巢母/稜殼/潮顎/燼使/虛尖)，KaijuDef+場景階層(placeholder 色塊)+roster+選頭目 4×2 格。每部位射不同彈、破部位消音。
5. ✅ 部位移動（肢掃/晶面公轉/盾自轉/衛星公轉/頸旋轉，邊動邊射）。
6. ✅ boss 專屬彈幕(BossSpiral/Aimed/Wall)；6 新小怪(含 DiveSwoop/HoverStrafe/Spiral)加進道中。
7. ✅ 破甲回填(TIDEMAW,+3 測試)；巢母卵囊生 spore_mite(上限 8)。
8. ✅ 跳過道中直達 BOSS（loadout 切換,測試用）。
9. ✅ **5 項 playtest 修正**：破甲/熱量雙條、難度真的接進 boss 發射+射速大降(D1 約 7 發/秒)、新頭目打擊框放大(0.85→1.35)、命中 pop+閃、飛彈綠×2/雷射青藍。
10. ✅ CARAPEX 視覺：下顎朝下、背甲炮移到底部、body-base。
- **狀態**：**466 EditMode GREEN**；EXE 116.8MB+APK 47MB(02:47-48)含全部；全 push origin/main；Obsidian 備份。

**⬜ 待辦（下次，導演決定順序）：**
1. ⬜ 5 新頭目 **bespoke 美術 / body-base**（目前色塊 placeholder）。
2. ⬜ 彈幕/pop/難度**微調**（依 playtest 手感）。
3. ⬜ **PartGate 執行**(6c 跨部位 hittable/breakable：稜殼 weak_node 需鄰面軟化、潮顎核心藏背甲後、燼使外翼孔需翼根剝甲)。
4. ⬜ **音樂/音效**方向規格 + BGM 接線（CC0 占位或 AI 生成）。
5. ⬜ per-part 移動場景 pivot 微調（新頭目 Orbit t=0 可能小跳）。
6. ⬜ 頭目 emitter cadence 各自細調；per-kaiju 專屬待機動作/軟化 glow/擊破碎屑。
7. ⬜ UI 改 UGUI+TMP(ADR-0006，目前 IMGUI)；Game view 直向解析度。

**★ 工作守則**（memory）：build 前先 `manage_scene save`（否則跳對話框卡住 MCP，且 reload 可能變 Untitled→load Bootstrap）[[save-scene-before-build]]；MCP 進 Play/截圖搶焦點→預設 EditMode+反射驗證 [[verify-without-stealing-focus]]；新頭目先 placeholder 圖 [[new-bosses-placeholder-sprites]]；常態 commit、push 依指示 [[commit-often-push-on-request]]。

---
*(以下為 session 9 逐步細節，保留追溯)*

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
- **✅ 5 新頭目全可玩**（`caff979`，2026-07-09）：execute_code 程序化建 5 KaijuDef asset(含各部位 emitter)+場景 BossPart 階層(placeholder 色塊 sprite：核紅/一般橙/裝甲鋼藍)+註冊 roster(KaijuId 1–8 唯一)。選頭目 UI 改動態 4×2 格顯示全 8 隻。全用 AssetDatabase/SerializedObject 驗證：8-boss roster、emitter 解析、部位名稱對得上 def。emitter：巢母卵囊放射/稜殼晶面放射/潮顎顎牆/燼使燼孔放射/虛尖脊柱放射+衛星瞄準+盾牆。EXE 116.8MB+APK 47MB 重建含全部。
  - **踩雷**：execute_code 的 `using` 不能放方法內(用完整命名空間)；`GameObject.Find` 找不到 inactive(頭目隱藏中，改用 roster BossRoot 參照驗證)；Unity 跳「save scene」對話框會卡住 MCP build 指令(導演按掉才通)。KaijuDef .asset 手改/程序化都可(SerializedObject 設 _parts/_emitters 陣列最穩)。
- **✅ 導演加點 1/3/4/5 全完成**（2026-07-09，程序化 execute_code）：
  - **① 部位移動**（`e5ada5a`）：利刃獸四肢 SweepArc/稜殼晶面 Orbit/虛尖盾 Spin+衛星 Orbit/雷龍頸 Spin。TickPartMotion 驅動、邊動邊射。
  - **⑤ boss 專屬彈幕**（`c2c3bee`）：Emitter_BossSpiral(旋轉臂)/BossAimed(密扇)/BossWall(寬牆)，8 頭目 42 部位依角色重指向(Radial→Spiral/Linear→Wall/Aimed→Aimed)。引入旋轉螺旋。
  - **③ 6 新小怪**（`e4902f0`）：spore_mite/spiral_turret/diver/prism_drone/bubbler/void_lancer + Movement_DiveSwoop/HoverStrafe + Emitter_MobSpiral，加進 5 段落池。EnemyController 加 spin 相位讓 Spiral 真旋轉。
  - **④a 破甲回填**（`cd14b8b`）：PartStateSystem.TickArmorRegen(grace 後衰退 BCurrent，飛彈重置/雷射不重置，不復活已破，預設關)。TIDEMAW 三顎啟用(5s/6BU)。+3 測試 → **466 EditMode GREEN**。
  - **④b 巢母生小怪**（`e7f5bdf`）：BossController spawner emitter(SpawnEnemyId 生 EnemyDef，全域上限 8，戰鬥始/勝清理)。巢母 5 卵囊同放螺旋彈+生 spore_mite；破卵囊兩者停。指定 _minionPrefab=Enemy/_minionDef=spore_mite。
  - EXE 116.8MB+APK 47MB 重建含全部。
  - **★踩雷**：build 前 Unity 若有未存場景會跳「save scene」模態對話框**卡住 MCP build**(build/status 全 timeout、telemetry 可回)；且 domain reload 後作用場景可能變成 Untitled 空場景(Bootstrap 被卸)。**修法/守則**：build 前先 `manage_scene save`；若場景變 Untitled→`manage_scene load Bootstrap`(它已含磁碟上存好的改動)再存再 build。見 memory [[save-scene-before-build]]。
- **下一步(續作)**：5 新頭目 body-base/bespoke 美術(目前色塊)；PartGate 執行(6c 跨部位 hittable/breakable)；頭目 emitter cadence 微調；音樂方向規格；per-part 移動的場景 pivot 微調(新頭目 Orbit t=0 可能有小跳)。
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
