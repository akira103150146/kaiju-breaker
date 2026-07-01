# 打擊感與回饋 (Game Feel & Feedback) GDD
## 殲獸戰機 / KAIJU BREAKER

*文件路徑: design/gdd/game-feel.md*
*最後更新: 2026-07-01*
*狀態: Draft — P0，服務導演明確反饋與阻斷風險 #4*
*相依文件: game-concept.md | weapon-system.md（LOCKED）| kaiju-part-system.md*

---

## A. 概覽 (Overview)

本文件定義殲獸戰機所有**打擊感層（Game Feel Layer）**的設計規格——頓幀（Hitstop）、慢動作（Slow-Motion）、螢幕震動（Screen Shake）、視覺閃光（Flash）、粒子效果（VFX Particles）、以及音效提示（SFX Sting）。

本文件的存在有兩個直接驅動因素：

1. **導演明確反饋**（概念原型遊玩後）：「打擊上可以多加一點東西，比如死亡或部位破壞時慢動作，或是大招的螢幕震動。」本文件將此反饋編纂為可執行的具體規格，不留詮釋空間。

2. **阻斷風險 #4 解決方案**：部位進入 SOFTENED 狀態必須在 0.5 秒內被玩家感知（繼承自 kaiju-part-system.md H.2）。本文件提供完整的多感官簽章設計，使 SOFTENED 狀態在任何彈幕密度下都具有不可忽視的視覺/音效辨識度。

**設計優先順序**：Sensation（感官愉悅）是本遊戲第二核心美學（MDA）。所有回饋的選擇都服務「以智取勝、精準拆解」的玩家幻想——尤其是「那一聲爆破」（weapon-system.md B 節）作為全系統最高爽感峰值。

**不可違反的鐵則**：「**彈幕永遠讀得懂**」（game-concept.md 視覺身份錨）。所有打擊感效果必須服從此鐵則——螢幕震動不得使玩家失去敵彈視野，頓幀不得長到吃掉閃避輸入，任何閃光不得遮蔽判定點超過容忍閾值。詳見 C.6 可讀性護欄（Readability Guardrail）。

---

## B. 玩家幻想 (Player Fantasy — Sensation Aesthetic)

殲獸戰機的 Sensation 幻想有三個層次，每個層次對應一套回饋訊號：

| 層次 | 幻想描述 | 主要回饋訊號 |
|------|----------|-------------|
| **精準蓄積（Heat Build）** | 我看著怪物的部位一點一點被加熱，它的色彩開始改變——掌控感 | SOFTENED 橙紅光暈脈動＋音效提示 |
| **剝甲開窗（Armor Strip）** | 震波命中瞬間護甲炸裂，弱點窗口打開，那 2 秒是我的 | 護甲碎片噴射＋震動＋弱點框顯現 |
| **爆裂破壞（Part Break）** | 那一聲爆破，視覺/音效/慢動作同時給——這是我賺到的 | 115ms 頓幀 → 慢動作 → 碎片爆炸 → 素材飛出 |

Boss 死亡是以上三個層次的總和，加上全螢幕的感官爆發，是整場戰鬥的最高潮。

---

## C. 詳細規則 (Detailed Rules)

### C.1 事件與回饋目錄 (Event × Feedback Catalogue)

下表定義每個可觸發打擊感事件的完整回饋訊號組合。所有具體數值見 G 節調校旋鈕。

| 事件 | 觸發來源 | 頓幀 (Hitstop) | 慢動作 (Slow-Mo) | 螢幕震動 (Shake) | 全白閃光 (Flash) | 粒子/VFX | 音效 (SFX) | 介面回饋 |
|------|---------|--------------|----------------|----------------|----------------|---------|----------|---------|
| **雷射命中（Laser Hit）** | 每幀雷射接觸部位 | 無 | 無 | 無 | 無 | 2 道細小火花（冷色） | 射擊嘀嗒聲（sfxTick） | 部位快閃 8ms |
| **部位軟化（SOFTENED）** | `on_part_softened` | 無 | 無 | 震幅 3 | 強度 0.30 | 10 個橙色熱力粒子（spawnHeatFx） | 上升雙音提示 sting（sfxSoften） | 「軟化！SOFTENED」浮動文字＋橙色脈動光暈＋可選圖示（見 C.3） |
| **L3 蓄力震波釋放（Shockwave）** | 玩家 Hold 1.5s 後鬆開 Z | 無 | 無 | 震幅 14 | 強度 0.65 | 全幅擴張藍色震波環＋每個被命中部位 9 顆冷藍火花 | 重低音砲擊聲（sfxShockwave） | 「波動砲！」浮動文字 |
| **護甲剝除（ARMOR_STRIPPED）** | `on_part_staggered + armor_stripped=true` | 無 | 無 | 震幅 5 | 強度 0.35 | 14 顆藍灰裝甲碎片 | 護甲撕裂聲（sfxArmorStrip） | 「裝甲破除！」浮動文字＋弱點框顯現（細白外框） |
| **M3 魚雷命中（Torpedo Impact）** | 魚雷碰撞框接觸部位 | 無 | 無 | 震幅 9 | 強度 0.55 | 22 顆橙紅爆焰粒子（大小混合） | 中低頻衝擊聲（sfxTorpedo） | 無獨立介面；若命中 SOFTENED 觸發下一行 |
| **M3 熱衝擊引爆（Heat-Shock Detonation）** | M3 命中 SOFTENED 部位 | 無 | 無 | 震幅 +8（與魚雷命中震幅取最大值） | 強度 0.50（疊加取最大） | 「熱衝擊引爆！」浮動文字 + 額外 7 顆橙色短命火花 | 無獨立音效（包含在 sfxTorpedo 內） | 紅橙大字浮動文字 |
| **M4 叢集炸彈爆炸（Cluster Detonation）** | 叢集炸彈觸發引爆 | 無 | 無 | 震幅 7 | 強度 0.42 | 26 顆橙黃爆炸粒子 | 中頻爆炸聲（sfxCluster） | 「叢集爆炸！」浮動文字 |
| **部位破壞（Part Break）** | `on_part_break` | **115ms** | 時間軸 0.12，保持 0.65s | 震幅 11＋(已破壞部位數 × 0.7) | 強度 0.92 | 22＋地板(部位寬/42) 個碎片粒子＋5 顆黑色煙塵＋4–7 顆素材軌道球 | 規模化爆破聲（sfxBreak） | 「部位破壞！」大字＋「＋素材 ×N」浮動文字；素材計數跳動彈出 |
| **Boss 死亡（Boss Core Break）** | `on_boss_core_break` | **220ms** | 時間軸 0.05，保持 1.2s | 震幅 24 | 強度 1.0（全白） | 110 顆金白粒子從核心爆射＋所有存活部位同步炸裂 | 四音上行琶音（sfxWin） | 勝利結算序列啟動（由遊戲狀態系統執行） |

> **設計說明**：頓幀（115ms / 220ms）在慢動作**之前**先執行——遊戲時間先凍結給玩家視覺處理，再以慢動作讓視覺奇觀展開。玩家輸入**不受頓幀凍結**（見 C.5）。

---

### C.2 頓幀規格 (Hitstop Specification)

頓幀是遊戲時間的**完全凍結**（`time_scale = 0`），並非慢動作。期間：

- **受凍結**：巨獸動畫、敵彈移動、玩家彈藥移動、粒子世界時間
- **不受凍結**：玩家輸入處理、UI 更新、全白閃光淡出、螢幕震動偏移計算、音效播放
- **原因**：頓幀期間若凍結輸入，玩家閃避輸入會被吃掉，違反「難度是門，不是牆」支柱

| 事件 | 頓幀時間 | 設計意圖 |
|------|---------|---------|
| 部位破壞（Part Break） | `hitstop_part_break_ms = 115ms` | 給視覺足夠幀數顯示爆破第一幀；不超過 150ms（超過開始感覺卡頓） |
| Boss 死亡（Boss Death） | `hitstop_boss_death_ms = 220ms` | 更長頓幀強化最終勝利感；玩家此刻無需閃避，可安全延長 |

---

### C.3 SOFTENED 狀態可讀性簽章 (SOFTENED Readability Signature)

這是阻斷風險 #4 的直接解決方案。SOFTENED 狀態必須在 `on_part_softened` 發出後 **≤ 0.5 秒**被玩家感知。使用三層多感官簽章（缺一不可）：

#### 層次一：顏色偏移（Color Shift）— 持續型
部位基礎色彩疊加橙色色調（`softened_color_hue = #FF6600`），飽和度提升。必須與「冷色＝你，暖色＝威脅」視覺哲學一致——SOFTENED 的橙紅正是「可攻擊弱點」的暖色警示，不是危險，是機會。

#### 層次二：脈動光暈環（Pulsing Glow Ring）— 持續型
以部位邊緣為中心的外發光環，以 `softened_pulse_frequency_hz = 2.0 Hz` 週期閃爍（亮暗交替）。脈動不停止，直至 `on_part_softened_exit` 收到或部位 BROKEN。

視覺規格：
- 光環基色：`#FF6600`（橙），閃爍峰值加入 `#FFCC00`（黃）過渡，模擬熔融感
- 外發光半徑：部位寬度的 25%（像素對齊）
- 閃爍波形：sine 曲線（0 到峰值平滑交替）

#### 層次三：音效提示（SFX Sting）— 瞬發型
`sfxSoften()` 在 `on_part_softened` 發出的同幀播放。兩個三角波振盪器（520 Hz → 880 Hz 上行滑音）在 0.14 秒內完成，音量不壓制背景音，但音色足夠獨特可被有意識辨識。此音效是 SOFTENED 狀態最快的感知通道（音效比視覺更快被注意）。

#### 可選層次四：介面圖示（Optional Icon）
在 SOFTENED 部位上方顯示一個橙色小火焰圖示（1×2 像素符號）。此為視覺輔助強化，對於彈幕遮蔽嚴重的 Nightmare 難度尤其有效。可透過無障礙選單獨立開啟（預設開啟）。

#### SOFTENED_EXIT（退出軟化）
收到 `on_part_softened_exit`：顏色偏移淡出（0.2 秒過渡），光暈環消失，SFX 播放短促下降音（sfxSoften 反向）。

---

### C.4 STAGGERED 與 BROKEN 狀態回饋 (STAGGERED & BROKEN Feedback)

#### STAGGERED（震盪硬直）
收到 `on_part_staggered`：
- **護甲剝除（armor_stripped = true）**：14 顆藍灰碎片噴射 + 護甲撕裂 SFX。部位邊緣出現**細白弱點框**（2 像素外框），維持至 `on_part_stagger_end`。弱點框用冷白色是為了不被橙色 SOFTENED 光暈混淆。
- **普通部位震盪（armor_stripped = false）**：部位短暫白色閃爍（flash = 0.45），3 顆冷藍火花，無碎片。

收到 `on_part_stagger_end + armor_restored = true`：弱點框消失，護甲恢復動畫（碎片慢速歸位，0.3 秒）。

#### BROKEN（部位破壞）
完整爆破序列（按時間軸排列）：

```
同幀：  音效 sfxBreak(規模)  ←  sfxBreak 參數由 part.w / 42 決定
同幀：  全白閃光 flash = 0.92
同幀：  頓幀 freeze = 115ms  ←  遊戲時間凍結
115ms後：慢動作啟動 timescale = 0.12
同幀：  碎片粒子噴射（22 + floor(part.w/42) 個主碎片 + 5 個黑煙）
同幀：  素材軌道球生成（4–7 個），完成頓幀後飛向計數器位置
0.65s後：慢動作保持結束，時間軸線性回升（3.8 倍速/秒）至 1.0
```

**碎片粒子規格**：
- 主碎片（22+）：部位原始色彩（50%）、白黃色 `#fff1c0`（25%）、橙色 `#ff8a4a`（25%）
- 黑煙粒子（5）：`#2a1a22`，較大（4×4 像素），高重力，快速落下
- 噴射速度：50–220 像素/秒，帶向上初始分量（-40 vy），重力 160

**素材軌道球（Material Homing Orbs）**：
- 初始方向：隨機爆射（速度 45–100 像素/秒）
- 約 0.3 秒後切換為追蹤計數器位置（螢幕右上角）的拋物線
- 色彩：青綠色 `#62F0D8`（呼應「素材=收獲」色彩）
- 抵達計數器時：計數器彈跳動畫 + 素材+N 浮動文字

---

### C.5 頓幀期間輸入處理 (Input During Hitstop)

**規則**：頓幀期間所有玩家輸入訊號**照常處理並排隊執行**。頓幀結束後的第一幀起執行已排隊的輸入。

**原因**：若頓幀吃掉閃避輸入，玩家在最爽快的部位破壞瞬間卻被子彈打到，這會嚴重損害打擊感。115ms 的頓幀已是人類反應時間（~200ms）的一半，必須讓閃避在此期間仍可登錄。

**實作備忘**：凍結應透過 `time_scale = 0` 或引擎的固定凍結 timer 實現，而非暫停整個遊戲循環。輸入輪詢必須在凍結 timer 倒計的同幀繼續執行。

---

### C.6 可讀性護欄 (Readability Guardrail)

打擊感設計**必須服從**「彈幕永遠讀得懂」視覺鐵則。以下護欄為硬性限制：

| 護欄 | 限制值 | 原因 |
|------|--------|------|
| 螢幕震動上限 | `shake_magnitude_cap = 24 像素`（Boss 死亡最大值） | 超過此值，螢幕邊緣的敵彈開始逃出可視範圍 |
| 震動方向 | 隨機（非固定方向） | 固定方向震動比隨機更容易導致敵彈預測失誤 |
| 全白閃光持續 | 強度以 2.6 單位/秒線性衰減（flash=1.0 在約 0.38 秒內淡出） | 超過 0.4 秒的不透明白色覆蓋遮蔽判定點 |
| 頓幀上限 | Boss 死亡 220ms；其他場合嚴格不超過 150ms | 超過 150ms 玩家開始感知為「卡頓」而非「爽快頓幀」 |
| SOFTENED 光暈 z-order | 必須渲染於敵彈**之下** | 光暈絕不能遮蔽子彈外框輪廓 |
| 慢動作期間敵彈 | 敵彈跟隨時間軸縮放（不例外） | 若敵彈維持正常速度，慢動作時玩家反而更難閃避 |

---

## D. 公式 (Formulas)

### D.1 螢幕震動模型 (Screen Shake Model)

採用**線性衰減的隨機偏移模型**（Prototype 驗證版本）：

```
on event: 
    current_shake = max(current_shake, event_magnitude)

每幀（FX 時間，不受慢動作縮放）:
    current_shake = max(0,  current_shake − shake_decay_rate × fxDt)

    if current_shake > shake_threshold:
        offset_x = random(−1, 1) × current_shake    （像素，整數化）
        offset_y = random(−1, 1) × current_shake    （像素，整數化）
    else:
        offset_x = 0;  offset_y = 0
```

**變數表**：

| 符號 | 預設值 | 說明 |
|------|--------|------|
| `current_shake` | 0 | 當前震動震幅（像素），clamp 至 [0, shake_magnitude_cap] |
| `event_magnitude` | 依事件（見 G.1） | 觸發事件的震幅，取最大值（不疊加） |
| `shake_decay_rate` | 42 像素/秒 | 線性衰減速率（全域旋鈕） |
| `shake_threshold` | 0.3 | 低於此值視為無震動，偏移歸零 |
| `shake_magnitude_cap` | 24 像素 | 硬性上限（可讀性護欄） |
| `fxDt` | 幀時長（不縮放） | FX 時間基準——震動計算**不受慢動作影響** |

**設計說明**：取最大值而非疊加，確保連續事件（如 M3 魚雷連打）不讓震幅無限累積，是可讀性護欄的執行機制。

---

### D.2 慢動作時間縮放曲線 (Slow-Motion Timescale Curve)

```
on trigger_slow_mo(hold_duration, timescale_min):
    time_scale = timescale_min          ← 瞬間跳至最低值
    slow_hold_timer = hold_duration

每幀（FX 時間）:
    if slow_hold_timer > 0:
        slow_hold_timer -= fxDt
    else:
        time_scale = min(1.0,  time_scale + slow_ramp_rate × fxDt)   ← 線性回升
```

**變數表**：

| 符號 | Part Break 值 | Boss Death 值 | 說明 |
|------|-------------|-------------|------|
| `timescale_min` | 0.12 | 0.05 | 慢動作最低時間縮放比（旋鈕） |
| `hold_duration` | 0.65s | 1.20s | 維持最低時間縮放的保持時長（旋鈕） |
| `slow_ramp_rate` | 3.8 / 秒 | 3.8 / 秒 | 線性回升速率（全域旋鈕） |

**理論回升時間**：
- Part Break：`(1.0 − 0.12) / 3.8 ≈ 0.23 秒`；總效果時長 ≈ 0.88 秒
- Boss Death：`(1.0 − 0.05) / 3.8 ≈ 0.25 秒`；總效果時長 ≈ 1.45 秒

**注意（引擎實作備忘）**：Unity 6.3 LTS 的 `Time.timeScale` 全域縮放會影響 `FixedUpdate`（物理）與所有使用 `Time.deltaTime` 的 `Update`。若要讓玩家輸入、UI、音效時鐘**不受縮放**，改用 `Time.unscaledDeltaTime` / `Time.unscaledTime`（`AudioSource` 播放本身不受 timeScale 影響；UI 動畫走 unscaled 時鐘）。

> **版本備忘**：Unity 6.3 LTS。`Time.timeScale = 0` 會凍結物理與 `FixedUpdate`；hitstop 期間仍須以 unscaled 時鐘輪詢輸入，避免吃掉閃避輸入。實作前請查閱 `docs/engine-reference/unity/VERSION.md`。

---

### D.3 全白閃光衰減 (White Flash Decay)

```
on event:
    flash_intensity = max(flash_intensity,  event_flash_value)   ← 取最大值

每幀（FX 時間）:
    flash_intensity = max(0,  flash_intensity − flash_decay_rate × fxDt)

渲染：
    螢幕最上層疊加一張白色全螢幕矩形，alpha = flash_intensity × flash_max_alpha
```

| 符號 | 預設值 | 說明 |
|------|--------|------|
| `flash_decay_rate` | 2.6 / 秒 | 線性衰減（旋鈕） |
| `flash_max_alpha` | 0.85 | 最大閃光不透明度上限（旋鈕；低於 1.0 確保子彈仍可見） |
| `flash_accessibility_mult` | 1.0 | 無障礙旋鈕；reduce-motion 模式設為 0.0 完全停用閃光 |

---

### D.4 慢動作-頓幀序列排定 (Hitstop-SlowMo Sequencing)

```
on_part_break received:
    1. audio.play(sfxBreak)          ← 同幀，不受時間縮放
    2. flash.trigger(0.92)           ← 同幀
    3. shake.trigger(11 + broken*0.7) ← 同幀
    4. particles.spawn_debris()      ← 同幀
    5. freeze_timer = 115ms          ← 開始頓幀（time_scale = 0）
    6. [115ms 後] freeze 結束
    7. slow_mo.trigger(0.65, 0.12)   ← 慢動作啟動
    8. material_orbs.spawn_homing()  ← 慢動作中素材飛出（讓玩家看清楚）
```

設計意圖：素材軌道球在慢動作期間生成，讓玩家在慢速中看到「素材飛向自己」的視覺，直接強化「破壞即獎勵」的感知。

---

## E. 邊界情況 (Edge Cases)

### E.1 連續破壞多個部位

**情況**：M3 Tier-3 鏈式效果在同一幀觸發兩個 `on_part_break`。

**處理**：第二個 `on_part_break` 收到時，若第一個的頓幀計時器仍在進行，**重置**頓幀計時器至 115ms（不疊加為 230ms）。慢動作的 `hold_duration` 同樣取最大值而非疊加。音效允許多層播放（兩聲爆破同時）。閃光和震動取最大值。

---

### E.2 Boss 死亡事件與部位破壞事件同幀

**情況**：Boss Core 破壞瞬間，kaiju-part-system.md E.6 定義事件順序為 `on_part_break` → `on_boss_core_break`。

**處理**：兩者都在同幀收到，以 `on_boss_core_break` 的參數覆蓋——頓幀 220ms（覆蓋 115ms）、慢動作 timescale 0.05（覆蓋 0.12）、震幅 24（覆蓋 11+）。所有存活部位同步執行 `spawnDebris()`，但不各自觸發額外頓幀（它們是 Boss 死亡視覺的一部分，非獨立事件）。

---

### E.3 SOFTENED 進入時已有其他閃光/震動

**情況**：玩家在部位破壞閃光（flash=0.92）期間命中另一部位使其 SOFTEN。

**處理**：SOFTENED 的 flash=0.30 與現有 0.92 取最大值（= 0.92，無感）。sfxSoften 音效**仍然播放**——音效是 SOFTENED 的最快感知通道，不因其他事件而壓制。脈動光暈同步啟動。

---

### E.4 快速射擊造成的 SOFTENED 音效過於頻繁

**情況**：L2 集束雷射快速蓄熱可能在 0.5 秒內連續觸發多個部位的 SOFTENED。

**處理**：sfxSoften 加入**冷卻**——同一幀內最多允許 `softened_sfx_max_per_frame = 2` 個部位同時播放 SOFTENED 音效；超過時後續壓制，但視覺脈動光暈仍對所有部位獨立啟動。

---

### E.5 無障礙模式下的 reduce-motion

詳見 H 節。所有 reduce-motion 路徑透過旋鈕控制，不修改事件流程本身。

---

## F. 系統相依 (Dependencies)

| 系統 | 相依類型 | 本文件消費的事件/資料 | 本文件向下游提供的效果 |
|------|----------|----------------------|----------------------|
| **kaiju-part-system.md** | 事件來源（必要） | `on_part_softened`、`on_part_softened_exit`、`on_part_staggered`、`on_part_stagger_end`、`on_part_break`、`on_boss_core_break` | 無；本文件是最終消費端 |
| **weapon-system.md（LOCKED）** | 事件來源（必要） | L3 蓄力震波釋放觸發（`releaseShockwave`）；`on_l3_wave_hit` 的 VFX | 無 |
| **material-economy.md（撰寫中）** | 協調（非阻斷） | `on_part_break.world_position` 作為素材軌道球生成點 | 素材軌道球飛向計數器的視覺 |
| **遊戲狀態系統（Game State System）** | 觸發下游 | `on_boss_core_break` 收到後配合勝利序列延遲 | 確保 Boss 死亡特效完整播放後才跳過場畫面 |
| **Unity 6.3 LTS** | 引擎 API | `Time.timeScale`（慢動作/頓幀）、`AudioSource`（SFX，不受 timeScale 影響）、Shader `_Time`（脈動光暈）、`Time.unscaledDeltaTime`（輸入/UI 不縮放） | 見 D.2 的引擎備忘 |
| **art-bible.md（待撰寫）** | 美術規範 | `softened_color_hue (#FF6600)`、碎片色彩規格 | 本文件定義技術規格；實際 sprite/shader 執行依美術聖經 |

---

## G. 調校旋鈕 (Tuning Knobs)

**所有數值存放於外部資料檔，禁止硬編碼。**
路徑：`assets/data/balance/game-feel.yaml`

### G.1 螢幕震動旋鈕

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `shake_mag_soften` | 3 px | 0–6 | 手感 | 部位 SOFTENED 震幅 |
| `shake_mag_armor_strip` | 5 px | 2–8 | 手感 | 護甲剝除震幅 |
| `shake_mag_l3_shockwave` | 14 px | 8–18 | 手感 | L3 蓄力震波釋放震幅 |
| `shake_mag_m3_torpedo_hit` | 9 px | 5–12 | 手感 | M3 魚雷接觸震幅 |
| `shake_mag_m3_heat_shock` | 8 px | 4–12 | 手感 | M3 熱衝擊引爆附加震幅（取最大） |
| `shake_mag_m4_cluster` | 7 px | 4–10 | 手感 | M4 叢集炸彈爆炸震幅 |
| `shake_mag_part_break_base` | 11 px | 8–16 | 閘門 | 部位破壞基礎震幅 |
| `shake_mag_part_break_escalation` | 0.7 px | 0–1.5 | 曲線 | 每個已破壞部位額外增加的震幅（上限由 shake_magnitude_cap 限制） |
| `shake_mag_boss_death` | 24 px | 18–24 | 閘門 | Boss 死亡震幅（不可超過 shake_magnitude_cap）|
| `shake_magnitude_cap` | 24 px | — | 閘門 | 可讀性護欄：震幅硬性上限。**任何事件不得超越此值。** |
| `shake_decay_rate` | 42 px/s | 25–60 | 手感 | 線性衰減速率；值越高震動越短促 |
| `shake_threshold` | 0.3 px | — | 手感 | 低於此值時偏移歸零 |
| `shake_accessibility_mult` | 1.0 | 0.0–1.0 | 閘門 | 無障礙倍率；reduce-motion 模式設為 0.25（保留觸覺感但大幅降低） |

### G.2 慢動作旋鈕

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `slowmo_part_break_timescale` | 0.12 | 0.08–0.25 | 閘門 | 部位破壞慢動作最低時間縮放 |
| `slowmo_part_break_hold_s` | 0.65s | 0.4–0.9 | 手感 | 部位破壞慢動作保持時長 |
| `slowmo_boss_death_timescale` | 0.05 | 0.03–0.12 | 閘門 | Boss 死亡慢動作最低時間縮放 |
| `slowmo_boss_death_hold_s` | 1.20s | 0.8–1.6 | 手感 | Boss 死亡慢動作保持時長 |
| `slowmo_ramp_rate` | 3.8 /s | 2.5–5.5 | 手感 | 線性回升速率（越高越快恢復正常速度） |
| `slowmo_accessibility_mult` | 1.0 | 0.0–1.0 | 閘門 | 無障礙倍率；reduce-motion 模式設為 0.0 完全停用慢動作 |

### G.3 頓幀旋鈕

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `hitstop_part_break_ms` | 115ms | 80–150 | 閘門 | 部位破壞頓幀時長；**超過 150ms 感知為卡頓** |
| `hitstop_boss_death_ms` | 220ms | 160–280 | 手感 | Boss 死亡頓幀時長；可較長因玩家無需閃避 |
| `hitstop_accessibility_mult` | 1.0 | 0.0–1.0 | 閘門 | 無障礙倍率；reduce-motion 模式設為 0.5（縮短頓幀但不移除） |

### G.4 SOFTENED 視覺旋鈕

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `softened_color_hue` | `#FF6600` | — | 美術 | 橙紅偏移目標色（美術確認符合視覺鐵則） |
| `softened_pulse_frequency_hz` | 2.0 Hz | 1.5–3.0 | 手感 | 脈動光暈閃爍頻率 |
| `softened_glow_radius_pct` | 25% | 15–40% | 手感 | 外發光半徑（部位寬度的百分比） |
| `softened_visual_onset_max_s` | 0.5s | — | 閘門 | **阻斷驗收門檻**：SOFTENED 效果出現最大延遲 |
| `softened_sfx_max_per_frame` | 2 | 1–4 | 手感 | 單幀最多同時播放 SOFTENED SFX 數（避免音效擁擠） |
| `softened_icon_enabled` | true | — | 閘門 | 火焰圖示是否預設開啟（可被無障礙選單覆寫） |

### G.5 閃光旋鈕

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|----------|------|------|
| `flash_decay_rate` | 2.6 /s | 1.5–4.0 | 手感 | 全白閃光線性衰減速率 |
| `flash_max_alpha` | 0.85 | 0.6–1.0 | 閘門 | 最大閃光不透明度；低於 1.0 確保子彈可見 |
| `flash_accessibility_mult` | 1.0 | 0.0–1.0 | 閘門 | 無障礙倍率；光敏感模式設為 0.0 完全停用閃光 |

---

## H. 無障礙 (Accessibility)

### H.1 Reduce-Motion 模式（減少動態效果）

可在設定選單獨立開啟。啟用後透過以下旋鈕縮放效果：

| 效果 | 正常模式 | Reduce-Motion 模式 | 影響旋鈕 |
|------|----------|-------------------|---------|
| 螢幕震動 | 全強度 | 25% 強度 | `shake_accessibility_mult = 0.25` |
| 慢動作 | 開啟 | **停用** | `slowmo_accessibility_mult = 0.0` |
| 頓幀 | 全時長 | 50% 時長 | `hitstop_accessibility_mult = 0.5` |
| 全白閃光 | 開啟 | **停用** | `flash_accessibility_mult = 0.0` |

**原因**：
- 螢幕震動保留 25%（非 0）——完全無震動會使大型爆炸感覺「空洞」，低強度震動仍提供打擊感而不引發光敏問題。
- 慢動作在 Reduce-Motion 模式下**完全停用**：慢動作的視覺衝擊對部分玩家（前庭感知敏感者）會造成不適。頓幀保留 50% 確保節拍感不完全消失。
- 全白閃光停用是光敏感者的直接需求，無條件尊重。

### H.2 SOFTENED 可及性（Softened State Accessibility）

SOFTENED 的多感官三層簽章（顏色 + 脈動光暈 + 音效）意味著：
- **色盲玩家**：脈動光暈的形狀與閃爍節律仍可辨識（不依賴橙紅色彩辨別）
- **低音量環境**：顏色偏移和脈動光暈提供純視覺感知通道
- **高彈幕遮蔽（難度 4）**：脈動光暈的閃爍節律（2 Hz）在彈幕空隙中仍可感知；選用圖示作為備援強化

---

## I. 驗收標準 (Acceptance Criteria)

### I.1 SOFTENED 感知速度（體驗性 — UX 阻斷，P0）

- [ ] 部位進入 SOFTENED 後，顏色偏移 + 脈動光暈必須在 **≤ 0.5 秒**內出現（≤ `softened_visual_onset_max_s`）——這是 kaiju-part-system.md H.2 繼承的阻斷條件
- [ ] sfxSoften 音效必須在 `on_part_softened` 發出的**同幀**開始播放，無跨幀延遲
- [ ] 不熟悉遊戲的受測者在含不同彈幕密度的靜態截圖中，能正確識別所有 SOFTENED 部位，**成功率 ≥ 80%**（5 人用戶測試）
- [ ] SOFTENED 視覺效果在最高敵彈密度（難度 4 / Nightmare）下仍可辨識（彈幕遮蔽時間 ≤ 50%）
- [ ] **若未達標，此為 Alpha 里程碑阻斷條件——不得推進**

### I.2 頓幀正確性（功能性 — 阻斷）

- [ ] 部位破壞頓幀精確為 `hitstop_part_break_ms`（±5ms 容差）
- [ ] Boss 死亡頓幀精確為 `hitstop_boss_death_ms`（±5ms 容差）
- [ ] 頓幀期間玩家移動輸入被記錄且在頓幀結束後第一幀執行（閃避輸入不丟失）
- [ ] 頓幀期間敵彈靜止不移動（`time_scale = 0` 正確作用於 Bullet pool）
- [ ] 自動化測試：`tests/unit/game-feel/hitstop_input_test.[ext]`

### I.3 慢動作正確性（功能性 — 阻斷）

- [ ] 部位破壞後：timescale 瞬間降至 `slowmo_part_break_timescale`，維持 `slowmo_part_break_hold_s` 後線性回升
- [ ] Boss 死亡後：timescale 瞬間降至 `slowmo_boss_death_timescale`，維持 `slowmo_boss_death_hold_s` 後線性回升
- [ ] 慢動作期間 SFX 音效**不縮速**（播放速度維持正常）
- [ ] 慢動作期間玩家輸入響應速度**不縮速**（輸入採樣仍以正常頻率進行）
- [ ] 自動化測試：`tests/unit/game-feel/slowmo_timescale_test.[ext]`

### I.4 螢幕震動上限（功能性 — 阻斷）

- [ ] 任何單一事件的震幅不超過 `shake_magnitude_cap`（24 px）
- [ ] 連續事件（M3 連打）的震幅取最大值而非累加，不突破上限
- [ ] Nightmare 難度（最高彈幕密度）下發生 Boss 死亡：敵彈仍在螢幕可視範圍內（QA 目視驗證）
- [ ] 自動化測試：`tests/unit/game-feel/shake_cap_test.[ext]`

### I.5 可讀性護欄（體驗性 — Advisory）

- [ ] 全白閃光在 Boss 死亡事件後 ≤ 0.4 秒淡至低於 20% alpha（QA 目視）
- [ ] SOFTENED 脈動光暈渲染在敵彈層之下（z-order 驗證）
- [ ] 部位破壞閃光（flash=0.92）期間，玩家判定點（1 像素白色）仍可在截圖中辨識

### I.6 導演回饋驗收（體驗性 — Advisory）

- [ ] 設計師遊玩 10 分鐘後確認：部位破壞時慢動作「有感」（主觀評分 ≥ 4/5）
- [ ] 設計師確認：L3 蓄力震波的螢幕震動強化了「大招釋放」的儀式感
- [ ] 設計師確認：Boss 死亡的 1.45 秒總慢動作時長「讓人看清楚了那一刻」而非「太長感覺卡」

### I.7 Reduce-Motion 模式（功能性 — Advisory）

- [ ] Reduce-Motion 開啟後，震動強度降至 25%，閃光完全消失，慢動作停用
- [ ] 開啟/關閉 Reduce-Motion 後所有旋鈕即時生效（無需重啟）
- [ ] 頓幀在 Reduce-Motion 模式下縮短至 50%，但打擊節拍感仍可感知（QA 主觀確認）

### I.8 全值資料驅動（功能性 — 阻斷）

- [ ] `assets/data/balance/game-feel.yaml` 存在且包含 G 節所有旋鈕
- [ ] 修改 yaml 旋鈕值（如 `shake_mag_part_break_base = 0`）後重新進入關卡，效果立即反映（無硬編碼繞過）

---

*文件版本：1.0.0*
*作者：Technical Artist Agent*
*關聯 GDD：game-concept.md | kaiju-part-system.md | weapon-system.md（LOCKED）*
*服務 Prototype：prototypes/weapon-feel-concept/prototype.html（Feel Target — 正式規格以本文件為準）*
