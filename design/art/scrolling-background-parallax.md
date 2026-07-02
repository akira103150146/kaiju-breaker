# 背景可捲動系統（前進假象）／ Scrolling Background — Parallax System
## 殲獸戰機 / KAIJU BREAKER

*文件路徑：design/art/scrolling-background-parallax.md*
*建立日期：2026-07-03*
*狀態：Draft — 待總監確認後轉正式（見 J 節開放問題）*
*來源意見：`design/feedback/2026-07-02-改進意見與劇情草案.md` §A.4（背景可捲動）＋ §B（五區劇情草案，未確認）*
*相依文件：art-bible.md | stage-system.md | docs/architecture/adr/0006-ui-framework.md | docs/architecture/adr/0003-data-driven-config-scriptableobjects.md*

---

## A. 概覽 (Overview)

本文件定義殲獸戰機的**縱向捲動背景（Scrolling Background）**視覺與資料系統：以多層視差（Multi-Layer Parallax）製造「玩家正在某地區持續前進」的錯覺，直接服務 `design/feedback/2026-07-02-改進意見與劇情草案.md` §A.4 的意見，並與 §B 劇情草案「一區一區推進」的敘事骨架呼應。系統核心原則是**每關卡皆可獨立設定**（捲動方向／速度／主題／必要時的鏡頭語意覆寫），透過 `StageBackgroundConfig` ScriptableObject 驅動（落實 ADR-0003），杜絕把單一捲動方向或速度寫死在程式中。背景在美術鐵則上永遠**視覺從屬**於彈幕與判定點——本文件的可讀性護欄與美術聖經（`art-bible.md`）的暖色/冷色鐵則一體適用。

本文件為**規格文件（Design/Art Spec）**，不含實作程式碼；Shader、`BackgroundScrollController`、相機語意的實際實作由 `technical-artist` 依本文件之資料合約負責。

---

## B. 玩家幻想 (Player Fantasy)

玩家的核心感受是**持續推進的孤身降下感**：畫面看似固定在同一片播放區，但背景的連續流動讓玩家在潛意識層面確信「我正在往魔物巢穴深處前進」，而非停留在單一靜態舞台上打波次。這服務兩個目的：

1. **敘事支撐**：呼應 §B 劇情草案「單機降下地表，一區一區清剿魔物」的骨幹——即使關卡結構是手作波段池隨機重組（`stage-system.md` §D，非程序生成關卡地形），背景捲動仍能在**不增加關卡設計負擔**的前提下給玩家「地區正在展開」的空間敘事感。
2. **深度與氛圍，但不搶戲**：多層視差提供的景深感是免費的氛圍分數（暖色巨獸 vs 冷色玩家的舞台感更立體），但背景資訊密度必須遠低於彈幕——玩家的眼睛在 0.1 秒內判讀生死的能力（美術聖經鐵則二）不得因背景而打折。

**設計測試（Design Test）**：在「背景越豐富越有電影感」與「背景越安靜、彈幕越突出」之間 → 永遠選後者。背景的職責是**烘托**節奏，不是**參與**節奏。

---

## C. 詳細規則 (Detailed Rules)

### C.1 多層視差模型 (Multi-Layer Parallax Model)

系統採**三層可選、二層基準**的視差模型。層數本身是每關卡可調的資料欄位（`layers[]` 陣列長度 1–3），非寫死三層：

| 層級 | 角色 | 深度比 (`depth_ratio`) 建議值 | 內容密度 | 是否必要 |
|------|------|------------------------|---------|---------|
| **Far（遠景）** | 天空／深處基調；建立地區色彩身分 | 0.10–0.30（預設 0.20） | 極低——大色塊漸層 + 稀疏遠景剪影 | **必要**（每關至少此層） |
| **Mid（中景）** | 可辨識的地區地標剪影（廢棄建築、珊瑚礁塔、鏽蝕結構等） | 0.35–0.65（預設 0.50） | 中——最主要的「正在通過某地標」讀出訊號 | **必要**（每關至少此層） |
| **Near（近景）** | 前景飄浮碎片／薄霧，加強視差深度 | 0.70–1.20（預設 0.90，可超過 1.0 做誇張前景掠過感） | 極稀疏、低不透明度 | **選用**——手機低階裝置可停用（見 C.7 效能護欄） |

**深度比（depth_ratio）語意**：某層的實際捲動速度 = 全關卡基準速度 × 該層深度比（見 D 節公式）。深度比越接近 1.0，該層視覺移動越快、越貼近前景；越接近 0，該層越像遙遠背景。

**視覺密度守則**：層級越靠近前景（near），畫面資訊量必須越低（更稀疏、更低不透明度）——這是視差深度感與彈幕可讀性之間的直接取捨；近景層若過於密集，會在高密度彈幕時段製造視覺雜訊。

---

### C.2 每關卡背景設定（`StageBackgroundConfig` ScriptableObject）

每個關卡對應一個 `StageBackgroundConfig` 資產（落實 ADR-0003：所有調校旋鈕以 SO 表達，不硬編碼）。以下為資料合約範例（非正式 C# 定義，欄位命名待 `technical-artist` 定案）：

```yaml
# 範例 1 — 對齊現有 stage-system.md §G.1 Stage 1「礁岩前哨站 REEF OUTPOST ALPHA」
stage_id: "stage_01"
zone_theme_id: "reef_outpost"
zone_theme_name_zh: "礁岩前哨站"
scroll_direction: [0, -1]           # 標準縱向捲動；背景往下捲 = 玩家向上前進的錯覺（見 C.3）
base_scroll_speed_px_s: 40          # 320×480 內部緩衝區基準；安全範圍見 G 節
layers:
  - layer_id: "far"
    texture: "env_reefoutpost_bg_far_tile"
    depth_ratio: 0.20
    tint_hex: "#0A1830"             # 深海冷藍黑，對齊 stage-system.md §G.1「深海藍黑底」既有描述
    alpha: 1.0
    tile_height_px: 480
  - layer_id: "mid"
    texture: "env_reefoutpost_bg_mid_tile"
    depth_ratio: 0.50
    tint_hex: "#123048"             # 冷藍珊瑚礁剪影
    alpha: 1.0
    tile_height_px: 480
  - layer_id: "near"                # 選用層
    texture: "env_reefoutpost_bg_near_debris"
    depth_ratio: 0.90
    tint_hex: "#1A4058"
    alpha: 0.35
    tile_height_px: 240
    enabled_on_mobile: false        # 手機效能旋鈕（見 C.7）
boss_arena_speed_ratio: 0.10        # 進入 BOSS 狀態後捲動趨緩至基準速度的 10%（見 C.7）
camera_framing_override: null       # 本關無特殊鏡頭語意
```

```yaml
# 範例 2 — 特殊關卡（如「掉到地底」下墜關卡）啟用 camera_framing_override
stage_id: "stage_underground_example"
zone_theme_id: "orbital_elevator_ruins"   # 暫對應劇情草案 §B 第 4 區「軌道電梯遺構」；未確認，見 I 節
scroll_direction: [0, -1]                 # 螢幕語意不變（玩家仍在畫面下方向上射擊，見 C.3）；捲動方向仍向下
base_scroll_speed_px_s: 65                # 較快速度營造「墜落」急迫感
layers:
  - layer_id: "far"
    texture: "env_elevator_bg_far_tile"
    depth_ratio: 0.20
    tint_hex: "#100818"
    alpha: 1.0
    tile_height_px: 480
  - layer_id: "mid"
    texture: "env_elevator_bg_mid_tile"
    depth_ratio: 0.55
    tint_hex: "#1A0F28"
    alpha: 1.0
    tile_height_px: 480
boss_arena_speed_ratio: 0.10
camera_framing_override:
  vignette_tint_hex: "#1A0800"            # 收緊視野暗角，強化「深井」壓迫感
  fov_zoom_mult: 1.08                     # 內部緩衝區裁切輕微放大；不得破壞整數縮放（見 C.4／美術聖經 §3.1）
  near_layer_depth_ratio_override: 1.15   # 覆寫近景層深度比，強化下墜速度感
```

**欄位說明**：

| 欄位 | 類型 | 說明 |
|------|------|------|
| `stage_id` | string | 對應 `stage-system.md` 關卡 ID |
| `zone_theme_id` / `zone_theme_name_zh` | string | 地區主題識別，供美術資產命名與未來 UI 顯示引用 |
| `scroll_direction` | Vector2（正規化） | 捲動方向；預設 `[0, -1]`（見 C.3） |
| `base_scroll_speed_px_s` | float | 全關卡基準捲動速度，320×480 內部緩衝區像素／秒 |
| `layers[]` | array | 1–3 筆層級資料；每筆含 `layer_id` / `texture` / `depth_ratio` / `tint_hex` / `alpha` / `tile_height_px` / 選用 `enabled_on_mobile` |
| `boss_arena_speed_ratio` | float 0–1 | `RunState = BOSS` 時的速度乘數（見 C.7） |
| `camera_framing_override` | 物件或 null | 選用；供特殊關卡（如地底下墜）覆寫暗角色調 / 鏡頭裁切 / 層級深度比。實際鏡頭實作屬 `technical-artist`／programmer 範疇，本文件僅定義美術端需要控制的資料欄位 |

---

### C.3 捲動方向與鏡頭語意 (Scroll Direction & Camera Framing)

**基準約定**：殲獸戰機的螢幕語意固定——螢幕上方 = 巨獸區、下方 = 玩家區（美術聖經 §3.1）。玩家戰機在畫面中的相對位置不因關卡而改變。因此**幾乎所有關卡的 `scroll_direction` 預設為 `[0, -1]`（背景向下捲動）**，藉由背景往下流動，製造「玩家戰機正往上／往前推進」的經典縱向 STG 錯覺——這是本文件的預設行為，也是任務指示中確認的方向。

**方向仍須為資料欄位而非寫死**的原因：

1. 導演已確認個別關卡可能採用不同**取景敘事**（例如「掉到地底」的下墜關卡）——即使螢幕語意不變，**捲動速度、色調、鏡頭語意**仍需要能夠獨立於其他關卡調整（見範例 2）。
2. 保留 `scroll_direction` 為真正可調欄位（而非程式碼寫死向下）符合 Tunable-First 原則，也為未來可能的特殊敘事片段（例如撤退／被推回的短暫逆向捲動橋段）保留可能性——**但這類反向捲動需要美術資產專門設計以確保視覺連續，非本文件現階段建議的預設用法**（見 J 節開放問題）。

**「掉到地底」關卡的實際做法建議**：由於螢幕語意不因關卡改變，下墜感**不透過反轉捲動方向達成**，而是透過以下組合表達（即 `camera_framing_override` 欄位存在的理由）：
- 提高 `base_scroll_speed_px_s`（下墜速度感）
- 暗角色調（`vignette_tint_hex`）與更暗的整體 tint，強化「深井」封閉感
- 提高近景層深度比（`near_layer_depth_ratio_override`），讓碎片掠過感更強烈
- 地區主題色調從冷藍轉為更深的紫黑／暖黑混合（仍需服從 C.4 可讀性護欄）

---

### C.4 可讀性護欄 (Readability Guardrails)

繼承美術聖經（`art-bible.md`）鐵則二「彈幕永遠讀得懂」與其色彩哲學（冷色＝安全／暖色＝威脅）。背景**不得**參與這套色溫語言，必須維持中性、低對比、退居幕後：

| 規則 | 硬性限制值 | 理由 |
|------|-----------|------|
| 彩度上限（HSV Saturation） | Far／Mid 層 ≤ 35%；Near 層 ≤ 20% | 避免背景色彩鮮豔度接近敵彈（敵彈彩度普遍 ≥ 70%），確保圖／地分離 |
| 亮度範圍（HSV Value） | 15%–55% | 避免背景過亮而與判定點白（`#FFFFFF`）或弱點外框白搶視覺焦點；避免過暗導致敵彈黑色外框（`#000000`）隱形 |
| 禁用色 | 不得使用 §2.2.1 敵彈六色任一 Hex 值；不得使用 `#FF6600`（SOFTENED 保留色調）；不得使用純黑 `#000000`（敵彈外框保留色）或純白 `#FFFFFF`（判定點／弱點外框保留色）作為背景主色 | 保留色域是全案的功能性視覺信號，背景誤用會製造誤判風險 |
| 禁用節律 | 背景元素不得以 2 Hz 正弦脈動呈現（`softened_pulse_frequency_hz` 保留給 SOFTENED 光暈） | 避免玩家誤讀「弱點裸露」信號 |
| 冷暖傾向 | 遠景層建議維持冷色基底（藍／藍紫／深青灰），即使該區主題偏暖（如熔岩、鏽蝕），暖色只能以**低飽和點綴**出現於中景／近景，不作為遠景基底 | 對齊既有 `stage-system.md` §G.2「暖色但低飽和，不干擾彈幕可讀性」的既定作法（Stage 2 深淵峽谷已示範此原則） |

**設計測試（Design Test）**：在任一背景草稿定稿前，將該畫面與 D4 難度最高密度彈幕合成截圖比對——若無法在 1 秒內分辨「這是背景還是危險」，重新降低該層彩度／亮度直到通過。

---

### C.5 渲染層級與繪製呼叫 (Sorting Order & Draw Calls)

依 ADR-0006 既定原則——**彈幕播放區不引入 Canvas**，世界座標元素一律以 SpriteRenderer 實作。背景比照辦理：

- 新增 `Background` Sorting Layer，置於現有渲染堆疊**最底層**（低於 BulletSim 的彈幕批次、低於 `PartsUI`、低於 `GameFeel`）。三個子層（far/mid/near）不建議各開獨立 Sorting Layer，改以同一 `Background` Layer 內的 `order in layer` 區分深度（例如 far = -30、mid = -20、near = -10），降低 Unity 2D 設定複雜度。
- **[需與 technical-artist 確認]**：`Background` 與既有 `Bullets` / `PartsUI` / `GameFeel` 三層的完整排序堆疊目前**尚無單一權威文件**（目前散落於 ADR-0006 與 `game-feel.md` §C.6 的個別敘述）。建議 technical-artist 在實作本系統時，順手整理一份完整 Sorting Layer 堆疊表（可置於 `docs/architecture/` 或 `bullet-system.md`），本文件不越權代為決定既有層級細節。

---

### C.6 命名與資料夾規範 (Naming & Folder Convention)

延伸美術聖經 §9.1／§9.3 既有的 `env_` 分類標籤與 `assets/art/backgrounds/` 資料夾，從單一 `space/` 子資料夾擴充為每區獨立子資料夾：

**命名格式**：`env_[zone_slug]_bg_[layer]_[variant].[ext]`

範例：
- `env_reefoutpost_bg_far_tile.png`
- `env_reefoutpost_bg_mid_tile.png`
- `env_reefoutpost_bg_near_debris_01.png`

**資料夾結構（建議擴充 `assets/art/backgrounds/`）**：

```
assets/art/backgrounds/
├── reef_outpost/          # Stage 1（既有 stage-system.md §G.1）
│   ├── env_reefoutpost_bg_far_tile.png
│   ├── env_reefoutpost_bg_mid_tile.png
│   └── env_reefoutpost_bg_near_debris_01.png
├── abyssal_rift/           # Stage 2（既有 §G.2）
├── voltage_spire/          # Stage 3（既有 §G.3）
└── [future_zone_slug]/     # 五區劇情草案確認後依序建立（見 I 節）
```

> **[待辦，非本次任務範圍]**：本文件建議 `art-bible.md` §9.1／§9.3 與 `stage-system.md` §J 相依表加入本文件之相互引用（見 F 節、J 節）。本次任務僅新增本檔案，尚未編輯上述既有檔案，需另行請示。

---

### C.7 與 Run 狀態機的掛鉤 (Stage Run-State Integration)

背景捲動由 `RunController` 的 `RunStateChanged{from, to}` 事件（`production/epics/stage/story-001-run-state-machine.md`）驅動速度切換，透過 `IEventBus` 訂閱（比照 ADR-0006 `PartBarController` 的既有訂閱模式）：

| `RunState` | 背景行為 |
|------------|---------|
| `LOADOUT` | Meta 畫面情境，背景系統不啟用（Meta 背景另由 `art-bible.md` §7.5 深藍黑 `#0A0E1A` 底規範） |
| `STAGE` | 依 `StageBackgroundConfig.base_scroll_speed_px_s` 全速捲動；貫穿引入段、隨機波段、前頭目喘息 |
| `BOSS` | 捲動速度趨緩至 `base_scroll_speed_px_s × boss_arena_speed_ratio`（預設 10%）——傳達「已抵達巨獸巢穴，不再前進」的敘事收束，呼應 `stage-system.md` §G.1.3「巨大陰影從螢幕上方緩緩覆蓋」的既有前頭目喘息敘事節點 |
| `RESULTS` | 捲動凍結於當前偏移；結算畫面 Overlay 疊加於凍結背景之上 |
| 暫停（`Time.timeScale = 0`） | 捲動必須以 scaled `Time.deltaTime` 驅動，暫停時自然凍結，恢復時不得跳幀或產生接縫感 |

**下一輪重啟**：每次新 Run 從 `LOADOUT → STAGE` 重新進入時，捲動偏移歸零重算（不延續上一輪的貼圖偏移狀態），避免累積浮點誤差。

---

## D. 公式 (Formulas)

### D.1 單層實際捲動速度

```
layer_scroll_speed_px_s = base_scroll_speed_px_s × depth_ratio
```

- `base_scroll_speed_px_s`：關卡基準速度（px/s，320×480 內部緩衝區座標）
- `depth_ratio`：該層深度比（0.10–1.20，見 C.1）

**範例計算**（Stage 1 範例設定，`base_scroll_speed_px_s = 40`）：

| 層 | depth_ratio | 實際速度 | 480px 貼圖完整捲動一輪耗時 |
|----|------------|---------|------------------------|
| Far | 0.20 | 8 px/s | 60s |
| Mid | 0.50 | 20 px/s | 24s |
| Near | 0.90 | 36 px/s | 13.3s |

三層速度差距明顯（60s / 24s / 13.3s），確保視差分層感可辨；同時任一層不會過快到產生頻閃或過慢到看不出移動。

### D.2 BOSS 狀態捲動速度

```
boss_state_layer_speed_px_s = layer_scroll_speed_px_s × boss_arena_speed_ratio
```

範例：Mid 層原速 20 px/s，`boss_arena_speed_ratio = 0.10` → BOSS 狀態下降至 2 px/s（近乎靜止的緩慢漂移，非完全停止，避免死板）。

### D.3 UV 循環偏移（供 technical-artist 實作參考）

```
uv_offset_y(t) = (layer_scroll_speed_px_s × t / tile_height_px) mod 1.0
```

- `t`：累積遊玩時間（秒，scaled `Time.deltaTime` 累加）
- `tile_height_px`：該層貼圖可無縫循環的高度（像素）
- 結果為 0.0–1.0 之間的 UV Y 偏移量，交由 shader 或素材位移控制

### D.4 背景系統 Draw Call 估算

```
total_bg_draw_calls = active_layer_count × 1   （假設每層為單一材質的 tiling shader，1 層 = 1 draw call）
```

- `active_layer_count` 建議上限 3（far + mid 必要 + near 選用）
- 目標：`total_bg_draw_calls ≤ 4`（含 1 個安全餘量，供近景層以小型池化 Sprite 實作時的額外 1 次批次）
- **此估算需與 ADR-0006 §效能意涵的既有 draw call 估算表合併重算**（該表目前為 BulletSim ≤150 + part bars ≤2 + HUD ≤5 + GameFeel ≤20 = ≤177，餘量 ≥23；背景系統應消耗此餘量中的 ≤4，仍留 ≥19 餘量至 200 上限）——**此為本文件對 ADR-0006 的補充提醒，非本次任務範圍內的實際編輯**（見 J 節）

---

## E. 邊界情況 (Edge Cases)

| 情況 | 處理方式 |
|------|---------|
| 波段隨機重組（同關卡內波段順序每輪不同，`stage-system.md` §D） | 背景設定綁定於**關卡（Stage）**而非個別波段（Segment），波段重組不觸發背景切換，捲動全程連續不中斷 |
| 關卡切換（Stage N → Stage N+1，未來多關卡串接） | 新關卡載入時套用新 `StageBackgroundConfig`；捲動偏移歸零，過渡建議採 0.3–0.5s 淡入淡出避免硬切接縫（實作細節由 technical-artist 決定） |
| 「掉到地底」等反向敘事關卡 | 捲動方向維持 `[0,-1]`（見 C.3）；下墜感由速度／色調／`camera_framing_override` 達成，非反轉方向 |
| 幀率波動 / 低階裝置掉幀 | 捲動必須以 `Time.deltaTime` 驅動而非固定每幀位移，確保 30fps 與 60fps 裝置的視覺捲動速度一致 |
| 手機裝置畫面比例差異（非 320×480 比例） | Pixel Perfect Camera 整數縮放下應以邊界裁切（pillarbox/letterbox）處理而非拉伸背景；貼圖需比內部緩衝區略寬留安全邊界。**[需與 technical-artist 確認實際安全邊界像素值]** |
| 暫停選單開啟 | 捲動凍結（見 C.7）；恢復時不得有可見跳幀或接縫爆發 |
| BOSS 死亡全螢幕白閃（`flash_max_alpha = 0.85`） | 背景在此時序上位於全部元素最底層，閃光覆蓋層天然遮蔽背景，無需額外處理 |
| 貼圖循環接縫 | 每層貼圖上下邊緣像素須無縫銜接（seamless tile）；美術資產產出時列入 QA 截圖比對項目（見 H 節 AC-BG-05） |
| Near 層在低階手機停用 | `enabled_on_mobile: false` 時，Mid 層需獨立承擔足夠的視差深度感（不依賴 Near 層才能成立） |
| 同一關卡多次遊玩（刷關重玩性） | 背景視覺每輪相同（背景與波段隨機重組無關，見上）；若後續希望降低重複感，可在波段邊界切換備選中景貼圖變體（選用增強項，見 J 節開放問題，非本版必要需求） |

---

## F. 相依 (Dependencies)

| 相依文件 | 方向 | 說明 |
|---------|------|------|
| `docs/architecture/adr/0006-ui-framework.md` | 本文件遵循 | 世界座標元素以 SpriteRenderer 實作、不引入 Canvas 的既定原則；本文件的背景系統比照辦理 |
| `docs/architecture/adr/0003-data-driven-config-scriptableobjects.md` | 本文件遵循 | `StageBackgroundConfig` 必須 SO 化，數值不得硬編碼 |
| `design/art-bible.md` §2／§3／§9 | 本文件遵循 + 建議擴充 | 調色盤保留色域（C.4 引用）、320×480 內部解析度與整數縮放（美術聖經 §3.1，Edge Case 引用）、素材命名與資料夾規範（C.6 延伸） |
| `design/gdd/stage-system.md` §C／§G／§J | 本文件遵循 + 建議加入相依 | 既有 Stage 1–3 的背景視覺描述（C.2 範例 1 的來源）、Run 節奏結構；建議 `stage-system.md` §J 加入本文件為相依（本次任務範圍未實際編輯該檔） |
| `production/epics/stage/story-001-run-state-machine.md` | 本文件遵循 | `RunState`／`RunStateChanged` 事件掛鉤點（C.7） |
| `design/feedback/2026-07-02-改進意見與劇情草案.md` §A.4／§B | 本文件源起 | 本文件回應的原始意見；§B 五區劇情草案為未確認參考（見 I 節） |
| `technical-artist`（delegate） | 下游實作 | Shader、`BackgroundScrollController`、camera framing 實際實作；Sorting Layer 堆疊整理（C.5） |

> **雙向性提醒**：依 `.claude/rules/design-docs.md`「相依須雙向」原則，本文件相依的 `stage-system.md`、`art-bible.md`、ADR-0006 目前**尚未**反向引用本文件（因其建立時間早於本文件）。已在各節標註「建議加入相依」，實際編輯需另行請示（見 J 節）。

---

## G. 調校旋鈕 (Tuning Knobs)

**所有數值建議存放於 `assets/data/stages/background_config.yaml`（或個別 `StageBackgroundConfig` SO 資產），禁止硬編碼。**

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|----------|--------|---------|------|------|
| `bg_base_scroll_speed_px_s` | 40 | 20–70 | 手感 | 關卡基準捲動速度（320×480 內部緩衝區） |
| `bg_layer_count_max` | 3 | 1–3 | 閘門 | 單關卡最大啟用層數（效能上限） |
| `bg_far_depth_ratio` | 0.20 | 0.10–0.30 | 曲線 | 遠景層深度比 |
| `bg_mid_depth_ratio` | 0.50 | 0.35–0.65 | 曲線 | 中景層深度比 |
| `bg_near_depth_ratio` | 0.90 | 0.70–1.20 | 曲線 | 近景層深度比（選用層） |
| `bg_near_layer_alpha` | 0.35 | 0.15–0.55 | 手感 | 近景層不透明度上限，避免干擾彈幕 |
| `bg_max_saturation_far_mid` | 35% | 20–45% | 閘門 | 遠／中景層 HSV 彩度上限（可讀性護欄） |
| `bg_max_saturation_near` | 20% | 10–30% | 閘門 | 近景層 HSV 彩度上限 |
| `bg_boss_arena_speed_ratio` | 0.10 | 0.0–0.30 | 手感 | 進入 BOSS 狀態後的捲動速度乘數 |
| `bg_draw_call_ceiling` | 4 | 2–6（超過 4 需升級 technical-artist 覆核） | 閘門 | 背景系統總 draw call 上限 |
| `bg_tile_height_px` | 480 | 240–960 | 閘門 | 貼圖無縫循環高度；建議為 480（內部緩衝區高度）之整數倍或因數，避免非整數縮放感 |
| `bg_segment_variant_swap` | false | on / off | 手感（選用增強項）| 是否於波段邊界切換備選中景貼圖以降低重複感；非必要需求（見 J 節） |

---

## H. 驗收標準 (Acceptance Criteria)

### AC-BG-01 可讀性（UX 阻斷）

- [ ] D4 惡夢難度最高彈幕密度靜態截圖：5 人受測者「背景元素 vs 敵彈」不產生混淆，成功率 ≥ 90%
- [ ] 背景層取色檢查：無使用美術聖經 §2.2.1 敵彈六色、`#FF6600`（SOFTENED 保留）、純黑 `#000000`（敵彈外框保留）、純白 `#FFFFFF`（判定點／弱點外框保留）
- [ ] 背景無 2 Hz 節律動畫（與 SOFTENED 脈動衝突風險排除）
- [ ] 遠／中景層 HSV 彩度 ≤ 35%；近景層 ≤ 20%（截圖取色驗證）

### AC-BG-02 效能（功能性 — 阻斷）

- [ ] 背景系統總 draw call ≤ `bg_draw_call_ceiling`（Profiler 於最低規格手機驗證）
- [ ] 併入背景後，戰鬥最重情境總 draw call 重新估算仍 ≤ 200（對照 ADR-0006 §效能意涵既有估算，見 D.4）
- [ ] 30fps 與 60fps 裝置的背景視覺捲動速度一致（±5% 容差），確認以 `Time.deltaTime` 驅動而非固定幀位移

### AC-BG-03 資料驅動（功能性）

- [ ] 每個關卡的捲動方向／速度／層數／主題／`camera_framing_override` 皆由 `StageBackgroundConfig` 讀取，程式中無硬編碼捲動值
- [ ] 更換 `StageBackgroundConfig` 資產即可切換關卡背景，設計師可於 Inspector 調參，無需改動程式碼
- [ ] `camera_framing_override` 為選用欄位；未填寫時套用預設鏡頭語意，不報錯

### AC-BG-04 Run 狀態整合（功能性）

- [ ] `RunStateChanged` 事件正確驅動速度切換：`STAGE` 全速 → `BOSS` 依 `boss_arena_speed_ratio` 趨緩 → `RESULTS` 凍結
- [ ] `Time.timeScale = 0` 暫停時背景捲動確實凍結；恢復後無跳幀或接縫感（人工驗證）

### AC-BG-05 無縫循環（體驗性 — Advisory）

- [ ] 各層貼圖上下邊緣像素連續；QA 截圖比對上下邊界無可見接縫
- [ ] 關卡全程（7–15 分鐘）連續遊玩下，playtest 回報「背景重複感過強」比例 ≤ 30%（advisory，非阻斷；若超標，評估啟用 `bg_segment_variant_swap`）

---

## I. 參考：五區主題對應（劇情草案 §B，未確認）

> ⚠️ 以下對應**僅供背景主題方向參考**，尚未成為正式規格。`design/feedback/2026-07-02-改進意見與劇情草案.md` §B 的五區劇情草案本身標註「待你確認」，且其頭目名單（甲殼中型魔物／高速掠食型／孵化母體／機械寄生型／巨型母體 Kaiju）與現有 `stage-system.md` 已定義的三頭目（CARAPEX／LACERA／VOLTWYRM）**尚未正式對齊**——兩套關卡命名體系目前並存（見 J 節開放問題）。若總監確認採用五區草案，需要一輪正式的美術主題定案（含實際 Hex 值），本節僅提供**方向性**建議，不含最終色值。

| 區 | 地區主題（草案） | 背景主題方向建議 | 特殊備註 |
|----|----------------|-----------------|---------|
| 1 | 墜落點・廢棄港灣 | 冷灰藍港灣剪影；教學段背景資訊密度應最低，呼應 `stage-system.md` §H 引導設計原則 | 首關，避免視覺負擔干擾操作教學 |
| 2 | 鏽蝕市街 | 冷灰底 + 低飽和鏽橙點綴（比照既有 Stage 2「暖色但低飽和」原則） | 中景層可放廢棄建築剪影，強化「市街」空間感 |
| 3 | 污染濕地・生體巢穴 | 冷色基底為主，**避免**使用病黃綠色調（`#789010` 家族）作為背景主色 | ⚠️ 若此區頭目美術方向沿用 LACERA 病黃綠家族色（`art-bible.md` §2.2.3），背景必須明確避開同色系，否則會與巨獸剪影混淆——此為美術總監需要在正式定案時特別把關的風險點 |
| 4 | 軌道電梯遺構 | 縱向長場景；建議捲動速度略高於平均，強化「電梯井」垂直感 | C.2 範例 2（地底下墜關卡）可套用或調整於此區 |
| 5 | 魔物母巢・核心 | 最深、最暗、最低彩度基底，為終局頭目戰鋪墊「已達核心」的視覺收束 | BOSS 狀態捲動趨緩（C.7）在此區的敘事意義最強——「已無路可退，只能面對核心」 |

---

## J. 待總監確認的問題 (Open Questions for Director Review)

1. **五區草案與既有三頭目（CARAPEX/LACERA/VOLTWYRM）如何對齊？** 劇情草案 §B 的五區頭目命名（甲殼中型魔物等）與 `stage-system.md` 現有三關卡頭目是否為同一批頭目的重新編排，或是全新頭目名單？此問題會直接決定 I 節五區背景主題最終能否直接沿用既有 `art-bible.md` 巨獸色彩方向（§5.4），或需要全新美術主題定案。
2. **反向捲動（逆向敘事片段）是否需要支援？** 本文件 C.3 建議「掉到地底」透過速度／色調表達而非反轉方向；若總監希望保留真正的反向捲動可能性（例如未來撤退橋段），需要額外一輪美術資產設計（反向捲動貼圖需雙向無縫），目前僅預留資料欄位、未規劃美術產出。
3. **近景層（Near）是否列為 MVP 必要項，或全面列為選用強化項？** 本文件目前建議 Near 層全面選用（尤其手機關閉），若總監希望以視差深度感作為賣點特別強化，可能需要重新評估效能餘量分配（見 D.4）。
4. **波段邊界貼圖變體切換（`bg_segment_variant_swap`）是否要排入本版需求？** 目前列為選用增強項（G 節），若總監認為重複感風險高，可提前排入正式驗收範圍。
5. **是否核准後續編輯 `art-bible.md` §9.1／§9.3 與 `stage-system.md` §J，加入本文件的相互引用？** 本次任務僅新增本檔案，尚未觸碰既有文件（見 F 節雙向性提醒），需另行請示。
6. **Sorting Layer 完整堆疊表由誰、於何處整理？** C.5 已標註目前無單一權威文件；建議由 `technical-artist` 主責，但確切文件歸屬（`docs/architecture/` 或 `bullet-system.md`）待決定。

---

*文件版本：0.1.0（Draft）*
*美術總監（Art Director Agent）*
*建立日期：2026-07-03*
*狀態：待總監確認 J 節問題後轉正式；轉正式後建議跑 `/design-review design/art/scrolling-background-parallax.md`*
