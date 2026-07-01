# 殲獸戰機 / KAIJU BREAKER
# 美術聖經 (Art Bible)

*文件路徑：design/art-bible.md*
*建立日期：2026-07-01*
*狀態：Draft — 正式版本，閘門所有素材生產*
*種子文件：game-concept.md §視覺錨點*
*相依文件：game-feel.md | bullet-system.md | hud-ui-system.md | kaiju/01-carapex.md | kaiju/02-lacera.md | kaiju/03-voltwyrm.md*

---

## 00. 美術聖經的角色 (Role of This Document)

本文件是殲獸戰機所有視覺素材的**唯一設計規範**。它閘門（gate）所有素材生產——任何不符合本文件的視覺資產必須修改後才能進入版本庫。

美術聖經不是靈感參考板，也不是可選的風格指南。它是**可執行的製作合約**：每個顏色有 Hex 值、每個尺寸有像素預算、每條規則有測試方式。當系統 GDD 與本文件有衝突，以本文件的美術規格為準；當本文件與 game-feel.md 的色彩旋鈕有衝突，以 game-feel.md 的 Hex 值為準（本文件對這些值負責「正式化」，不負責「更改」）。

---

## 01. 兩大美術鐵則 (The Two Art Laws)

這兩條規則是其他所有規則的來源。當任何視覺決策發生衝突，優先服從這兩條。

### 鐵則一：科技對巨獸 (Tech vs. Titan)

**玩家是科技。巨獸是血肉（或能量）。這場對比必須在螢幕上的任何一幀都顯而易見。**

- 玩家戰機：小、乾淨、冷色 (Cold Color)、幾何精準、硬邊緣
- 巨獸：大、複雜、暖色 / 病態色 (Warm / Sick Color)、有機紋理、佔據螢幕
- 任何新素材上線前先問：**「這是科技，還是血肉？(Tech or Flesh?)」** 答案決定色溫與造型語言

*設計測試 (Design Test)*：在「讓戰機也變得花俏複雜」與「保持戰機小巧、讓巨獸更有壓迫感」之間 → 永遠選後者。

### 鐵則二：彈幕永遠讀得懂 (Bullets Always Readable)

**玩家子彈、敵方子彈、玩家判定點 (Hitbox Dot) 三者在任何混亂中必須一眼可辨。**

這是全專案最高優先的視覺鐵則，繼承自 `game-concept.md §視覺錨點`，在 game-feel.md、bullet-system.md、hud-ui-system.md 三個系統中均以硬性護欄 (Readability Guardrail) 實作。

色溫是 0.1 秒以內的生死判斷工具：

> **冷色 (Cold) = 你、安全、科技**
>
> **暖色 (Warm) = 威脅、敵彈、巨獸、危險**

此色彩哲學同時服務〔難度是門，不是牆〕的可及性承諾：玩家不需要高超技巧才能讀懂生死，色溫在 0.1 秒內傳達所有關鍵資訊。

*設計測試 (Design Test)*：在「華麗特效」與「子彈看得清」之間 → 永遠選看得清。

---

## 02. 主調色盤 (Master Palette)

殲獸戰機採用**有限街機調色盤 (Limited Arcade Palette)**，控制總色數以維持像素街機風格的一致性。所有正式素材的顏色必須從本節列出的色盤中選取；未列入者不得出現（全螢幕白閃覆層除外，詳見 §09）。

---

### 2.1 冷色系 (Cold Family) — 玩家 / 科技 / 安全

| 名稱 | 英文名稱 | Hex | 色彩角色 |
|------|---------|-----|---------|
| 判定點白 | Hitbox White | `#FFFFFF` | 玩家 1px 判定點；全遊戲最高優先，任何狀況下恆亮 |
| 雷射核心青 | Laser Core Cyan | `#40F8FF` | 玩家雷射光束內核；玩家子彈代表色 |
| 雷射光暈藍 | Laser Glow Blue | `#80E8FF` | 玩家雷射光暈漸出；玩家飛彈尾焰 |
| 戰機主藍 | Ship Primary Blue | `#2080F0` | 玩家戰機主體 |
| 戰機高光藍 | Ship Rim Highlight | `#60B0FF` | 戰機邊緣 Rim Light（1–2px 邊緣） |
| 艙蓋冰藍 | Cockpit Ice | `#A0D4FF` | 駕駛艙玻璃 / 冷光細節 |
| 戰機底影靛 | Ship Shadow Indigo | `#103880` | 戰機底部 / 後方陰影 |
| 飛彈冷藍 | Missile Cold Blue | `#70C8F0` | 玩家飛彈機身 |
| 科技介面藍 | Tech UI Blue | `#00C0E0` | HUD 冷色調介面元素、系統指示器 |
| 素材軌道青綠 | Material Orb Teal | `#62F0D8` | 素材軌道球 (Material Homing Orbs)；此值由 game-feel.md 定義，**不可更改** |

---

### 2.2 暖色系 (Warm Family) — 敵彈 / 巨獸 / 威脅

#### 2.2.1 敵彈子調色盤 (Enemy Bullet Sub-palette)

所有敵彈必須從以下六色選取。任何敵彈的 `color_id` 不得使用冷色系。

| 名稱 | 英文名稱 | Hex | 對應巨獸 / 模式 |
|------|---------|-----|---------------|
| 主橙彈 | Bullet Primary Orange | `#FF8000` | CARAPEX 大顎交叉彈；標準敵彈基準色 |
| 副黃彈 | Bullet Secondary Yellow | `#FFCC00` | CARAPEX 背甲礫散；VOLTWYRM 能量彈；高威脅大型彈幕 |
| 深紅彈 | Bullet Dark Red | `#CC2200` | CARAPEX 核心光刃；最高威脅 / 核心專屬 |
| 明橙彈 | Bullet Bright Orange | `#FF8C00` | LACERA 刃浪掃射主彈 |
| 橙紅彈 | Bullet Orange-Red | `#FF4500` | LACERA 聚肢爆彈；高濃度聚集彈 |
| 能量白金彈 | Bullet Energy White-Gold | `#FFF0A0` | VOLTWYRM 極高壓縮能量彈（螺旋臂最外彈） |

#### 2.2.2 巨獸血肉 — CARAPEX（甲殼系 / Carapace Theme）

| 名稱 | 英文名稱 | Hex | 色彩角色 |
|------|---------|-----|---------|
| 琥珀甲殼 | Amber Carapace | `#B87020` | 主甲殼；深琥珀基底 |
| 鏽橙爪甲 | Rust Orange Claw | `#A83810` | 大顎端 / 爪甲高光 |
| 病黃次甲 | Sick Yellow Plate | `#C09820` | 次甲殼板塊；表面紋理色 |
| 核心暗紅 | Core Dark Red Pulse | `#800010` | 胸口核心生物脈動光 |
| 甲殼深影 | Carapace Deep Shadow | `#402010` | 底部陰影 / 甲殼縫隙 |

#### 2.2.3 巨獸血肉 — LACERA（肢體系 / Limb Theme）

| 名稱 | 英文名稱 | Hex | 色彩角色 |
|------|---------|-----|---------|
| 病黃綠體 | Sick Yellow-Green Body | `#789010` | 軀幹主體；病態生物感 |
| 橙褐節甲 | Orange-Brown Segment | `#885010` | 體節 / 關節環甲 |
| 刃尖明橙 | Blade Tip Bright Orange | `#D07000` | 刃肢末端高光；危險提示強調色 |
| 關節深影 | Joint Deep Shadow | `#385000` | 關節縫隙 / 最暗區域 |

#### 2.2.4 巨獸能量 — VOLTWYRM（能量系 / Energy Theme）

| 名稱 | 英文名稱 | Hex | 色彩角色 |
|------|---------|-----|---------|
| 核心極白黃 | Core Extreme White-Yellow | `#FFFFF0` | 核心節 (core_node) 極高壓內核；最亮點 |
| 能量弧黃 | Energy Arc Yellow | `#FFE860` | 頸段能量流動脈衝 |
| 外緣橙紅 | Body Edge Orange-Red | `#FF9020` | 蛇身外緣邊界；確保暖色威脅感 |
| 護盾深紫藍 | Shield Deep Purple-Blue | `#503090` | 護盾 ARMOR_INTACT — **冷色例外，見 §2.4** |
| 護盾內光紫 | Shield Inner Purple | `#302080` | 護盾內層發光 |
| 護盾暴露橙 | Shield Exposed Orange | `#FF7020` | 護盾 ARMOR_STRIPPED 裂紋覆層；轉暖表達弱點 |

---

### 2.3 SOFTENED / 熱化子調色盤 (SOFTENED / Heat Sub-palette)

這三個顏色由 `game-feel.md §C.3` 定義為 SOFTENED 狀態的視覺簽章 (Visual Signature)，本文件將其正式化為美術製作合約。

| 名稱 | 英文名稱 | Hex | game-feel.md 旋鈕 | 色彩角色 |
|------|---------|-----|-----------------|---------|
| 軟化橙 | SOFTENED Orange | `#FF6600` | `softened_color_hue` | SOFTENED 狀態基礎色調偏移目標；HEAT 條滿格脈動色 |
| 熔融峰黃 | Molten Peak Yellow | `#FFCC00` | 光暈閃爍峰值過渡色 | SOFTENED 光暈脈動高點，模擬熔融感 |
| 弱點外框白 | Weakness Frame White | `#FFFFFF` | — | ARMOR_STRIPPED 2px 弱點外框；與判定點白共享，傳達緊迫感 |

**與暖色系的協調說明 (Reconciliation Note)**：

`#FF6600` 與 `#FFCC00` 均落在暖色系，與敵彈調色盤有顏色重疊。這是刻意設計：`game-feel.md §C.3` 明確說明「SOFTENED 的橙紅正是『可攻擊弱點』的暖色警示，不是危險，是機會」——暖色信號在此語義是「需要對應行動」，與敵彈的「需要閃避」性質相似但方向相反。

玩家不會混淆 SOFTENED 光暈與敵彈，因為三個層面的差異協同作用：

1. **位置語義**：SOFTENED 光暈固定附著在巨獸部位上（世界物件），敵彈是離散移動小體
2. **脈動節律**：2 Hz 正弦閃爍（Sine Pulse）是 SOFTENED 獨有的行為特徵，敵彈無此行為
3. **形狀尺度**：光暈是部位輪廓向外的擴散環（大型），敵彈是 4–6px 離散圓點（小型）

---

### 2.4 護盾冷色例外說明 (Shield Cold Color Design Exception)

VOLTWYRM 護盾在 ARMOR_INTACT 狀態使用深紫藍（`#503090`），違反「巨獸部位用暖色」的一般規則。此例外有明確設計理由，並嚴格限定適用範圍：

**問題**：護盾是大型靜態物件（非移動子彈）。若使用暖色，在高密度橙黃彈幕中會與敵彈視覺混同，使玩家誤判「護盾是子彈 → 嘗試閃避而非攻擊」。

**解法**：使用紫藍冷色，讓護盾讀成「可互動的大型防禦結構」而非「子彈」。`voltwyrm.md §3` 原文：「外觀有別於敵彈（冷色）」——這是設計意圖，非例外失誤。

**仍服從大原則**：護盾一旦 ARMOR_STRIPPED，立即轉暖色（`#FF7020`）表達弱點已暴露。

**適用範圍**：此例外**僅限**大型靜態 ARMORED 部位。移動中的敵彈、小型敵人、任何動態發射物均不得套用此例外。

---

### 2.5 VFX 特殊色 (VFX Special Colors)

以下顏色為 `game-feel.md §C.4` 定義的粒子規格，本文件正式化為製作合約：

| 名稱 | 英文名稱 | Hex | 用途 |
|------|---------|-----|------|
| 爆破主碎片白黃 | Debris Flash White-Yellow | `#FFF1C0` | 部位破壞主碎片粒子（25% 混合比）|
| 爆破副碎片橙 | Debris Secondary Orange | `#FF8A4A` | 部位破壞碎片（25% 混合比）|
| 黑煙粒子 | Black Smoke Particle | `#2A1A22` | 破壞黑煙（5 顆，4×4px，高重力）|
| 全白閃光覆層 | Full White Flash Overlay | `#FFFFFF` | 全螢幕閃光，`flash_max_alpha = 0.85`（非 1.0）|

---

## 03. 像素規格 (Pixel Standards)

### 3.1 內部解析度與渲染原點

| 項目 | 規格 |
|------|------|
| 內部緩衝區 (Internal Buffer) | **320×480 像素**（縱向 Portrait） |
| 縮放方式 | **整數倍縮放僅（Integer Scaling Only）**：×1 / ×2 / ×3 / ×4 |
| 非整數縮放 | **禁止**——非整數縮放使像素邊緣模糊，破壞「像素街機懷舊」視覺身份 |
| 渲染方向 | 縱向 Portrait；螢幕上方 = 巨獸區，下方 = 玩家區 |
| 色彩模式 | RGBA 32-bit（有透明度 Sprite）；RGB 24-bit（背景）|
| 所有尺寸基準 | 以 320×480 為像素座標基準；所有尺寸以此解析度計 |

### 3.2 Sprite 尺寸預算 (Sprite Size Budgets)

#### 玩家戰機 (Player Ship)

| 素材 | 尺寸 | 備注 |
|------|------|------|
| 戰機本體（idle） | 14×20 px | 以小搏大——必須在巨獸面前顯得渺小 |
| 戰機左 / 右傾斜幀 | 14×20 px | 側移時觸發，視覺偏轉 2–4px |
| 玩家判定點 | 1×1 px | 純白，最高 z-order，不可合批於其他 Sprite |
| 引擎噴焰 | 6×10 px | 附加在戰機底部，獨立圖層，4 幀動畫 |

#### 玩家子彈 (Player Projectiles)

| 素材 | 尺寸 | 形狀語義 |
|------|------|---------|
| L1 散波雷射（各光束） | 2×8 px | 細線；冷藍；扇形 3–4 條 |
| L2 集束雷射（光束） | 3×12 px | 稍寬細線；冷青 |
| L3 震波環（展開幀） | 全幅動畫 6 幀，從中心向外 | 藍色震波圓環 |
| L4 穿透雷射 | 2px 寬 × 垂直滿幅 | 冷白芯 + 冷青邊；一條線刺穿全螢幕 |
| M1 追蹤飛彈 | 5×10 px | 橢圓形；冷藍 |
| M2 蜂群飛彈 | 4×8 px | 小橢圓；冷藍；8 枚扇形 |
| M3 穿甲魚雷 | 6×14 px | 菱形 / 錐形；冷白前端 |
| M4 叢集炸彈 | 8×10 px | 圓形；冷藍灰 |

#### 巨獸部位（模組化 Sprite，各部位獨立圖層）

**CARAPEX（#01，甲殼系）**

| 部位 ID | 類型 | 尺寸 |
|---------|------|------|
| `chest_reactor_core` | BOSS_CORE | 64×64 px |
| `left_mandible` / `right_mandible` | NORMAL | 48×64 px（鏡像對稱可共用源檔）|
| `dorsal_cannon` | ARMORED | 80×48 px（INTACT / STRIPPED 兩狀態各一 Sprite）|

**LACERA（#02，肢體系）**

| 部位 ID | 類型 | 尺寸 |
|---------|------|------|
| `head_core` | BOSS_CORE | 48×48 px |
| `fore_limb_left` / `fore_limb_right` | NORMAL | 16×72 px（掃弧旋轉動畫）|
| `hind_limb_left` / `hind_limb_right` | NORMAL | 14×80 px（掃弧，更大幅度）|
| `tail_carapace` | ARMORED | 32×48 px（INTACT / STRIPPED 兩狀態）|

**VOLTWYRM（#03，能量系）**

| 部位 ID | 類型 | 尺寸 |
|---------|------|------|
| `core_node` | BOSS_CORE | 48×48 px（持續旋轉光環動畫）|
| `shield_left` / `shield_right` | ARMORED | 40×56 px（六角晶格；INTACT / STRIPPED 兩狀態）|
| `neck_seg_1`~`neck_seg_4` | NORMAL | 48×32 px（能量脈衝流動動畫）|

#### UI 元素

| 素材 | 尺寸 | 備注 |
|------|------|------|
| 武器圖示（L1–L4 / M1–M4） | 16×16 px | 所有武器統一尺寸 |
| Tier 徽章（T0–T3） | 8×6 px | 貼附於圖示右上角 |
| 彈頭 Pip（Ammo Pip） | 6×6 px | 實心 / 空心兩狀態 |
| 部位 HEAT 條 / BREAK 條 | [部位寬度] × 3 px | 手機縮放後實體高度 ≥ 6 螢幕像素 |

---

### 3.3 外框規則 (Outline Rules)

所有 Sprite 必須有 1px 純色外框 (1px Solid Outline)，不得以透明邊緣代替。

| 素材類型 | 外框顏色 | 理由 |
|---------|---------|------|
| 敵彈（Enemy Bullets） | `#000000`（純黑）| 最高對比度，任何背景下均可讀 |
| 巨獸部位（Kaiju Parts） | `#1A0800`（深暖黑）| 暖色調深黑，融合暖色系語言 |
| 玩家戰機（Player Ship） | `#102040`（深藍黑）| 冷色調深黑，區分玩家 vs 敵人外框語言 |
| 玩家子彈（Player Bullets） | `#103060`（深冷藍）| 冷色外框，強化「玩家子彈 ≠ 敵彈」 |
| HUD / UI 元素 | `#000000` 或 `#102040` | 依整體調性選擇 |

**禁止**：模糊邊緣（Sub-pixel Rendering）、外框缺口、非整數縮放導致的邊緣漸變。

---

### 3.4 抖色指引 (Dithering Guidance)

| 情境 | 規則 |
|------|------|
| 大型巨獸部位的陰影漸層 | **允許** 2 色棋盤抖色（Checkerboard Dither），限 2×2 或 4×4 尺度 |
| 科技素材（戰機、UI）| 偏好硬邊硬色塊（Hard Ramp）；科技語言 = 乾淨，減少抖色 |
| 小型 Sprite（子彈、< 16px 素材）| **禁止**抖色；尺寸太小抖色不可讀 |
| 爆炸 / 粒子素材 | 不用抖色；使用固定 Hex 值 |

---

### 3.5 動畫幀規範 (Animation Frame Conventions)

| 動畫類型 | 幀數 | 播放速率 | 備注 |
|---------|------|---------|------|
| 戰機閒置浮動 | 4 幀 | 8 fps | 輕微上下位移 1–2px |
| 戰機左 / 右傾斜 | 各 4 幀 | 即時切換（進場 2 幀 / 回正 2 幀）| 側移輸入觸發 |
| 引擎噴焰 | 4 幀 | 12 fps | 強度隨速度變化 |
| 巨獸部位閒置 | 4–8 幀 | 8 fps | 輕微脈動 / 漂浮 |
| SOFTENED 光暈 | Shader 驅動（非幀動畫）| 2 Hz Sine 曲線 | 由 `softened_pulse_frequency_hz = 2.0` 控制 |
| 電報閃光（Telegraph Flash）| 4–6 幀 | 12 fps | 充能由暗到亮；對應各模式 `charge_telegraph_s` |
| 部位破壞爆炸（BROKEN）| 8–12 幀 | 15 fps | 配合頓幀 (Hitstop) 後的慢動作呈現 |
| 護甲剝除（ARMOR_STRIPPED）| 6 幀 | 0.15 秒內完成 | 快速甲殼炸開動畫 |
| 素材軌道球飛行 | Tween 驅動（非幀動畫）| 拋物線飛向計數器 | 慢動作期間播放 |
| 敵彈 Sprite | 1 幀（靜止圖片）| — | 形狀即信號，無動畫需求 |

---

## 04. 彈幕與可讀性規格 (Bullet & Readability Specification)

本節是「彈幕永遠讀得懂」鐵則的美術執行層面具體化。所有規則為**硬性限制 (Hard Constraints)**，不可因視覺效果理由妥協。

### 4.1 三元素辨識系統

螢幕上有三種必須在任何混亂下一眼可辨的元素：

| 元素 | 色溫 | 形狀 | 典型尺寸 |
|------|------|------|---------|
| **玩家判定點**（Hitbox Dot） | 純白 `#FFFFFF` | 1×1 px 方點 | 最小（1px），永不縮放 |
| **玩家子彈**（Player Bullets） | 冷色（青藍系）| 細線（雷射）或橢圓（飛彈）| 小至中型 |
| **敵方子彈**（Enemy Bullets） | 暖色（橙黃紅系）| 圓點或短橢圓 | 小型（4–6px）|

三者任意兩兩配對，在**色溫、形狀、行為**三個維度上均有明確差異。即使色彩完全辨識失敗，形狀差異仍提供備援。

### 4.2 敵彈視覺硬規格 (Enemy Bullet Hard Spec)

| 規格項目 | 要求 |
|---------|------|
| 顏色 | 必須使用 §2.2.1 敵彈子調色盤六色之一，不得使用其他顏色 |
| 外框 | 1px 純黑（`#000000`）外框，不得省略或縮減 |
| 形狀 | 圓點（4–6px）或短橢圓（4×6px）；禁止使用與玩家子彈形狀相似的細線或長橢圓 |
| 內核高亮 | 中心 1–2px 亮點（比外圈更亮的相近暖色），增加粒子立體感 |
| 電報閃光（Pre-fire Telegraph）| 顏色為對應彈色的更亮版本（例：`#FF8000` 基底彈 → `#FFB040` 電報閃光）；持續 `charge_telegraph_s` 秒 |

### 4.3 色盲安全冗餘規格 (Colorblind-Safe Redundancy Spec)

**設計鐵則：任何關鍵視覺信號不得單靠色彩傳達。** 每個關鍵狀態必須同時具備形狀 / 節律 / 行為層面的備援：

| 狀態 | 色彩線索 | 非色彩備援（必須實作）|
|------|---------|---------------------|
| 敵彈 vs 玩家雷射 | 暖橙 vs 冷青 | **形狀**：圓點 vs 細線 |
| 敵彈 vs 玩家飛彈 | 暖橙 vs 冷藍 | **形狀**：圓點 vs 橢圓；**行為**：追蹤 vs 直線 |
| SOFTENED 部位 | `#FF6600` 橙紅光暈 | **2 Hz 脈動閃爍節律** + sfxSoften 音效 |
| ARMOR_STRIPPED 弱點 | `#FFFFFF` 2px 外框 | **倒計時消退動畫**（時間維度線索，無需辨色）|
| 素材軌道球 vs 敵彈 | 青綠 `#62F0D8` vs 暖橙 | **拋物線軌跡飛向計數器**（行為備援）|
| 彈匣低彈警告 | 圖示顏色偏暗 | **1 Hz 閃爍動畫**（節律備援）|
| VOLTWYRM 護盾 vs 敵彈 | 冷紫藍 vs 暖橙 | **尺寸差異**：護盾是大型靜態物件，敵彈是小型移動體 |

---

### 4.4 玩家判定點完整規格 (Player Hitbox Dot Full Specification)

| 規格項目 | 值 |
|---------|-----|
| 尺寸 | 1×1 像素，**不可改變** |
| 顏色 | `#FFFFFF`（純白），任何狀況下不可更改 |
| Z-Order | 渲染堆疊絕對最頂層，高於全白閃光覆層（`flash_max_alpha = 0.85`）|
| 可見性 | 永不消失；不受任何遮罩、閃光、慢動作、螢幕震動影響 |
| 位置 | 戰機 Sprite 幾何中心 |
| 閃光下保障 | 全白閃光上限 0.85 alpha（非 1.0），確保判定點白點在閃光下仍可辨識（此值由 game-feel.md 的 `flash_max_alpha` 旋鈕保證，美術端不可請求提高至 1.0）|

---

## 05. 巨獸輪廓語言 (Kaiju Silhouette Language)

### 5.1 螢幕級壓迫設計原則 (Screen-Scale Oppression)

巨獸的**尺寸壓迫**是〔科技對巨獸〕支柱的視覺執行核心：

- 巨獸進入視野時，必須在戰機進場**之前**已充滿畫面上方
- 玩家螢幕中，巨獸 + 彈幕應佔據 ≥ 50% 畫面面積
- 玩家戰機在同一畫面中必須感覺**極小**；禁止以放大戰機方式「讓玩家感覺強大」

| 巨獸 | 畫面寬佔比 | 畫面高佔比 | 參考尺寸（320×480 基準）|
|------|----------|----------|----------------------|
| CARAPEX | 60–75% | 50–65% | ~192–240px 寬 × ~240–312px 高 |
| LACERA（軀幹）| ~40%（軀幹）；70%（肢體掃及）| — | 軀幹 ~128px；肢掃及 ~224px |
| VOLTWYRM | ~25%（主幹）| 70–80% | ~80px 寬 × ~336–384px 高 |

### 5.2 三主題輪廓差異 (Three Theme Silhouettes)

三隻巨獸在**剪影（Silhouette）**層面必須一眼可辨：

| 巨獸 | 主題 | 輪廓特徵 | 色溫傾向 |
|------|------|---------|---------|
| **CARAPEX** | 甲殼（Carapace）| 厚實橫向梯形；鏡面背殼 + 前端大螯；左右對稱結構 | 琥珀 + 鏽橙 + 病黃 |
| **LACERA** | 肢體（Limb）| 中心軀幹 + 向外延伸的動態刃肢；不對稱、動態掃弧 | 病黃綠 + 橙褐 |
| **VOLTWYRM** | 能量（Energy）| 縱向蛇柱；由上而下節段堆疊；護盾夾護頂端 | 白金能量芯 + 橙紅外緣 |

**剪影測試 (Silhouette Test)**：將三隻巨獸以純黑剪影並排顯示於同一畫面，5 人受測者必須在 1 秒內無誤辨識各自身份。每次美術評審以此作為驗收項目。

### 5.3 部位狀態色彩轉換合約 (Part-State Color Shift Art Contract)

此為美術與 game-feel.md 之間的正式合約。每個可破壞部位在生命週期中經歷三個視覺狀態：

```
INTACT（完整）→ SOFTENED（軟化）→ BROKEN（破壞消失）
```

| 狀態 | 視覺規格 | 執行方式 |
|------|---------|---------|
| **INTACT** | 部位原始色彩，無特殊效果 | 基礎 Sprite 顯示 |
| **SOFTENED** | 部位色彩疊加 `#FF6600` 橙色調偏移（Color Shift），飽和度提升；外緣產生 2 Hz 脈動光暈環（`#FF6600` → 峰值 `#FFCC00` 過渡） | Shader 色調偏移 + 脈動光暈（game-feel.md §C.3 驅動）|
| **BROKEN** | 部位 Sprite 消失；爆炸序列粒子依 §09 規格散射；素材軌道球（`#62F0D8`）飛出 | 爆炸幀動畫 + 粒子系統 |

> **SOFTENED 光暈 Z-Order 規則**（繼承 game-feel.md §C.6）：光暈必須渲染於敵彈層**之下**。光暈絕不得遮蔽任何敵彈的外框輪廓。

**ARMOR_STRIPPED 附加視覺層**：
- 甲殼爆裂動畫（6 幀，0.15 秒內完成）暴露弱點核心
- 弱點核心邊緣：2px 純白（`#FFFFFF`）外框脈動，持續至 `stagger_timer` 歸零
- 倒計時收縮動畫：橙色像素條，同步 `stagger_duration = 2.0s` 線性消退

### 5.4 各巨獸色彩方向速查 (Per-Kaiju Color Direction)

**CARAPEX 色彩方向**

- 主甲三色硬分層：`#B87020`（琥珀）/ `#C09820`（病黃）/ `#A83810`（鏽橙）
- 核心：`#800010` 暗紅生物脈動；BOSS_CORE 標記以 hitbox × 1.2 放大亮紅標誌
- 彈幕：大顎 `#FF8000` / 背甲炮 `#FFCC00` / 核心 `#CC2200`

**LACERA 色彩方向**

- 軀幹主體：`#789010` 病黃綠 + `#885010` 橙褐節環
- 刃肢末端：`#D07000` 明橙高光（威脅強調）；肢體破壞後殘樁以最暗影色顯示
- 彈幕：刃浪 `#FF8C00` / 聚肢 `#FF4500`

**VOLTWYRM 色彩方向**

- 頸段內核：`#FFE860` 能量弧黃，外緣 `#FF9020`；SOFTENED 後整節轉 `#FF6600`
- 核心節：`#FFFFF0` 極亮白黃內核，外包旋轉光環；BOSS_CORE 視覺最亮
- 護盾：INTACT = `#503090` 深紫藍半透明；STRIPPED = `#FF7020` 暖橙裂紋覆層
- 彈幕：螺旋臂 `#FF8000`–`#FFCC00` / 能量牆 `#FFCC00` / 能量彈 `#FFF0A0`

---

## 06. 戰機與科技語言 (Ship & Tech Language)

### 6.1 設計哲學：以小搏大的精準感 (The 以小搏大 Visual Fantasy)

玩家戰機是科技側的視覺代表，體現「精準 vs 蠻力」的視覺對立：

- **小而精準**：外形以幾何形態為主（楔形），硬邊緣，無有機曲線
- **冷色純粹**：嚴格使用 §2.1 冷色系，禁止任何暖色細節出現在戰機本體
- **功能性色彩**：引擎噴焰（冷藍白）、雷射（冷青白）、飛彈（冷藍）各有功能語義
- **最小裝飾**：少色、硬光、幾何即是科技的像素語言；不需要複雜紋理

### 6.2 戰機視覺規格 (Ship Visual Spec)

| 元素 | 規格 |
|------|------|
| 本體形狀 | 縱向楔形（Wedge）；前端尖銳，後端較寬 |
| 主色 | `#2080F0` 戰機主藍 |
| Rim 高光 | `#60B0FF` 1–2px 邊緣高光 |
| 艙蓋 | `#A0D4FF` 冰色，1–2px 小玻璃面積 |
| 底部陰影 | `#103880` |
| 外框 | `#102040` 深藍黑 1px |
| 引擎噴焰 | `#40F8FF`（核心）→ `#2080F0`（漸出）4 幀動畫 |
| 判定點 | 機身幾何中心 1px `#FFFFFF` |

### 6.3 科技素材鑒別測試 (Tech Asset ID Test)

對所有科技類素材（戰機、飛彈、UI、雷射）進行下列檢查，四題全是才通過：

1. 輪廓是否幾何化（非有機曲線）？
2. 主色是否在冷色系？
3. 是否完全無暖色細節（包括高光）？
4. 是否比周圍巨獸素材更簡潔（更少細節、更少色數）？

---

## 07. UI/HUD 視覺風格 (UI / HUD Visual Style)

### 7.1 戰鬥 HUD 首要原則

HUD 的首要職責是**不干擾彈幕可讀性**，服務優先序：

1. 不遮蔽飛行中的敵彈外框
2. 不遮蔽玩家判定點（永遠 P0 最高層）
3. 資訊在需要時才顯示（事件驅動，非全時佔據螢幕）

### 7.2 像素字型規格

| 規格 | 值 |
|------|-----|
| 英數字型 | 像素點陣字型；大寫 / 數字高度 5–7px |
| 中文字型 | 16px 點陣中文（最小可讀尺寸）|
| 縮放 | 整數倍縮放；禁止非整數 |
| 主要色 | 白色（`#FFFFFF`）；重要數字可用冷青（`#40F8FF`）強調 |

### 7.3 部位世界座標血條規格 (World-Space Part Bars)

血條以世界座標（World-Space）形式貼附於各部位 Sprite 框外緣，隨部位移動，不在 HUD 角落設固定面板。

| 血條 | 位置 | 色彩語義 | 規格 |
|------|------|---------|------|
| HEAT 條（熱量槽）| 部位框上方 2px 間距 | 灰 → 橙漸進；SOFTENED 時滿格 `#FF6600` 脈動 | 高度 3px；寬 = 部位 Sprite 寬 |
| BREAK 條（破甲槽）| 部位框下方 2px 間距 | 深藍 → 白漸進（白 = 快要破了）| 同上 |

**狀態與色彩對應**：

| 部位狀態 | HEAT 條 | BREAK 條 |
|---------|---------|---------|
| INTACT | 灰→橙漸進填充 | 藍→白漸進填充 |
| SOFTENED | 滿格 `#FF6600` 脈動（2 Hz，同步 game-feel 光暈）| 藍→白漸進 |
| ARMORED + ARMOR_INTACT | 正常 | 凍結顯示 + 藍灰遮罩（opacity 0.65）|
| ARMORED + ARMOR_STRIPPED | 正常 | 白×1.5 速率填充 + 2px 細白外框（倒計時消退）|
| BROKEN | **不顯示** | **不顯示** |

**SOFTENED HEAT 條同步要求**：`softened_heat_bar_pulse_hz`（HUD 系統）必須等於 `softened_pulse_frequency_hz`（game-feel 光暈）= 2.0 Hz。兩值不一致時血條與光暈失同步。

### 7.4 HUD 螢幕區域色彩方向

| 區域 | 內容 | 色彩方向 |
|------|------|---------|
| 左上（A 區）| 分數、關卡標識、Boss HP 細條 | 白字 / 冷色 UI 框 |
| 右上（B 區）| 素材計數器 | 白字；素材飛入時 `#62F0D8` 閃動彈跳 |
| 左下（C 區）| 主武器槽 + L3 蓄力條 | 冷色 UI；L3 充能從冷 → 暖黃（`#FFE060`）漸變 |
| 右下（D 區）| 副武器槽 + 彈頭 Pip | 冷色 UI；低彈警告 1 Hz 閃爍 |
| 播放區中央 | **無固定 HUD 元素** | — |

### 7.5 Meta Screen 視覺方向 (Meta Screens)

| 元素 | 規格 |
|------|------|
| 背景 | 深藍黑（`#0A0E1A`）；維持街機廳螢幕感 |
| 武器卡片底 | `#1A2030` 深藍灰；選中者 `#40F8FF` 冷青 2px 外框 |
| 主要按鈕 | `#2080F0` 藍底，白字；Hover / 選中加亮外框 |
| 灰化按鈕（Disabled）| `#303040` |
| 素材不足警示 | `#CC2200` 深紅 ✗ 標記（暖色警示，語義一致）|
| 難度卡片 | 四卡等大；選中者輕微放大 ×1.05 + 冷青外框；不設視覺推薦偏向 |
| Tier-3 模糊遮罩 | 像素點陣模糊；遮罩顏色 `#1A2030`（卡片底色），保留輪廓剪影 |

---

## 08. VFX / 粒子色彩語言 (VFX / Particle Color Language)

本節直接對應 game-feel.md §C 的所有 VFX 觸發事件，規定美術端的色彩執行。技術規格以 game-feel.md 為準，本節為製作指引。

| 事件 | 粒子 / 效果色彩規格 | 備注 |
|------|-------------------|------|
| 雷射命中火花（Laser Hit Sparks） | 2 顆細小火花：`#40F8FF` 冷青，2×2px | 玩家武器 = 冷色火花 |
| SOFTENED 熱力粒子 | 10 顆橙色粒子：`#FF6600`；小型上升 | 部位 SOFTENED 觸發瞬間 |
| L3 震波環（Shockwave Ring VFX） | 全幅藍色震波圓環 + 命中部位各 9 顆冷藍火花（`#40F8FF`）| 玩家大招 = 冷色 |
| 護甲剝除碎片（Armor Strip Debris）| 14 顆藍灰碎片：`#8090A0`；中等 4–6px | 護甲材質感 |
| 魚雷爆焰（Torpedo Explosion）| 22 顆橙紅爆焰粒子：`#FF6600` + `#CC4000`；大小混合 | 武器爆炸 = 暖色 |
| 叢集炸彈爆炸（Cluster Detonation）| 26 顆橙黃粒子：`#FF8000` + `#FFCC00` | — |
| 部位破壞主碎片（Part Break Debris）| 依 game-feel.md 合約：50% 部位原色 / 25% `#FFF1C0` / 25% `#FF8A4A` | 主碎片 22+ 顆 |
| 部位破壞黑煙（Break Black Smoke）| 5 顆 `#2A1A22`；4×4px；高重力快速落下 | — |
| 素材軌道球（Material Homing Orbs）| 4–7 顆 `#62F0D8` 青綠球；拋物線飛向計數器 | game-feel.md 合約值，不可更改 |
| Boss 死亡金白爆射 | 110 顆粒子：`#FFFFF0` 白黃內核 + `#FFE860` 外圈；從核心爆射 | 最高視覺等級 |
| 全螢幕白閃（White Flash Overlay）| `#FFFFFF`；`flash_max_alpha = 0.85`（非 1.0）| 0.4 秒內衰減至 < 20% alpha |

**VFX 可讀性約束**（繼承 game-feel.md §C.6）：

- 任何 VFX 不得使螢幕震動超過 `shake_magnitude_cap = 24px`
- 閃光超過 0.4 秒後必須衰減至 < 20% alpha
- 爆炸粒子的 z-order 位於敵彈層之下，不遮蔽飛行中的子彈外框

---

## 09. 素材製作標準 (Asset Production Standards)

### 9.1 命名規範 (Naming Convention)

格式：`[category]_[name]_[variant]_[size].[ext]`

| 分類標籤 | 用途 | 範例 |
|---------|------|------|
| `char` | 玩家戰機及幀 | `char_ship_idle_01.png` |
| `kaiju` | 巨獸部位 | `kaiju_carapex_chest_core_intact.png` |
| `kaiju` | 部位狀態變體 | `kaiju_carapex_dorsal_cannon_stripped.png` |
| `bullet` | 敵彈 Sprite | `bullet_orange_round_small.png` |
| `player` | 玩家彈藥 | `player_laser_l2_beam.png` |
| `vfx` | 特效幀 | `vfx_part_break_frame_01.png` |
| `vfx` | 粒子（靜態）| `vfx_debris_orange_small.png` |
| `ui` | HUD 元素 | `ui_bar_heat_3px.png` |
| `ui` | 武器圖示 | `ui_weapon_l1_icon_16.png` |
| `ui` | 按鈕 | `ui_btn_primary_default.png` |
| `env` | 背景 / 環境 | `env_space_bg_scroll_large.png` |

### 9.2 檔案格式規格

| 格式 | 用途 | 設定 |
|------|------|------|
| `.png` | 所有 Sprite、UI 元素、特效幀 | 無損壓縮；RGBA 32-bit（有透明度）|
| `.png` | 背景（無透明）| RGB 24-bit |
| `.aseprite` | 所有像素素材原始工作檔 | 存入 `assets/src/` 鏡像路徑；不打包進建置 |
| `.atlas` + `.png` | Unity Sprite Atlas 輸出 | 見 §9.4 |

### 9.3 資料夾結構 (Folder Structure under `assets/`)

```
assets/
├── art/
│   ├── characters/
│   │   └── ship/                     # 玩家戰機 Sprite 幀
│   ├── kaiju/
│   │   ├── carapex/                  # CARAPEX 所有部位 + 狀態幀
│   │   ├── lacera/                   # LACERA 所有部位 + 動畫幀
│   │   └── voltwyrm/                 # VOLTWYRM 所有部位 + 狀態幀
│   ├── bullets/
│   │   ├── enemy/                    # 敵彈 Sprite（6 色 × 形狀）
│   │   └── player/                   # 玩家彈藥 Sprite
│   ├── vfx/
│   │   ├── explosions/               # 部位爆炸序列幀
│   │   ├── particles/                # 粒子靜態 Sprite
│   │   └── hit_sparks/               # 命中火花
│   ├── ui/
│   │   ├── hud/                      # 戰鬥 HUD（血條、Pip、圖示等）
│   │   ├── weapons/                  # 武器圖示（L1–L4 / M1–M4）
│   │   └── meta/                     # Loadout / 升級 / 難度畫面 UI
│   └── backgrounds/
│       └── space/                    # 縱向滾動背景
├── src/                              # 原始 .aseprite 工作檔（鏡像 art/ 結構）
├── atlases/                          # Unity Sprite Atlas 輸出
└── data/
    ├── balance/
    │   └── game-feel.yaml
    ├── bullets/
    │   └── bullet_config.yaml
    ├── kaiju/
    │   ├── carapex.yaml
    │   ├── lacera.yaml
    │   └── voltwyrm.yaml
    └── ui/
        └── hud-config.yaml
```

### 9.4 Sprite Atlas 規劃 (Sprite Atlas Guidance)

| Atlas 名稱 | 包含內容 | 理由 |
|-----------|---------|------|
| `atlas_enemy_bullets` | 所有敵彈 Sprite（6 色 × 2 形狀）| 共用單一材質，最大化 GPU Instancing 批次（bullet-system.md §5.4）|
| `atlas_player_weapons` | 玩家子彈 / 雷射 Sprite | 玩家彈獨立池（bullet-system.md §3.2）|
| `atlas_carapex` | CARAPEX 所有部位所有狀態幀 | 單 Boss 單 Atlas |
| `atlas_lacera` | LACERA 同上 | — |
| `atlas_voltwyrm` | VOLTWYRM 同上 | — |
| `atlas_vfx` | 爆炸幀 / 粒子 Sprite / 黑煙 | VFX 獨立 Atlas，避免與 Boss Atlas 同批渲染時混淆 |
| `atlas_ui` | 所有 HUD + Meta Screen UI 元素 | — |

每個 Atlas 建議尺寸 ≤ 2048×2048px，避免低端手機超出 VRAM 預算。

### 9.5 色彩設定 (Color Profile)

| 設定 | 值 |
|------|-----|
| 工作色彩空間 | sRGB |
| 匯出設定 | sRGB；禁止嵌入 ICC Profile（Unity 2D 工作流不需要）|
| 抖色演算法（Aseprite）| Bayer 8×8（如需使用抖色）；僅限大型巨獸部位陰影 |

---

## 10. 驗收標準 (Acceptance Criteria)

### AC-ART-01 彈幕可讀性（UX 阻斷 — Alpha 里程碑）

- [ ] D4 惡夢難度靜態截圖：5 人受測者辨識「敵彈 vs 玩家判定點」成功率 ≥ 80%（繼承 bullet-system.md §11.6）
- [ ] 玩家判定點（1px 白點）在 Boss 死亡全白閃光（`flash = 1.0`）之下仍可在截圖中辨識（z-order 驗證）
- [ ] 所有敵彈均有 1px 純黑外框；截圖取色確認，無外框缺失
- [ ] 玩家雷射（細線）/ 玩家飛彈（橢圓）/ 敵彈（圓點）三種形狀在最高密度截圖中可由形狀單獨辨識（覆蓋色盲場景）

### AC-ART-02 #FF6600 與 game-feel.md 的一致性（功能性 — 阻斷）

- [ ] 所有 SOFTENED 部位色調偏移使用 `softened_color_hue = #FF6600`（設定驗證 + 截圖取色）
- [ ] SOFTENED 光暈脈動峰值過渡到 `#FFCC00`（截圖目視確認）
- [ ] `softened_heat_bar_pulse_hz` = `softened_pulse_frequency_hz` = 2.0 Hz（自動化設定讀取測試，繼承 hud-ui-system.md §M.2）
- [ ] 三隻巨獸的 SOFTENED 外觀在截圖中一致：相同色調偏移量、相同脈動節律

### AC-ART-03 色盲安全冗餘（體驗性 — Advisory）

- [ ] §4.3 表格中所有非色彩備援線索均有對應素材或行為實作（逐項確認）
- [ ] 色盲替代調色盤方案（藍黃對比 / 形狀優先）由美術總監確認並提供替代 Sprite 集
- [ ] 色盲模式啟用後：SOFTENED 狀態可通過 2 Hz 脈動節律辨識，5 人測試 ≥ 70%（繼承 hud-ui-system.md §M.8）

### AC-ART-04 暖色系鐵則（功能性 — 阻斷）

- [ ] 所有敵彈 `color_id` 使用 §2.2.1 列出的六色之一（程式端 enum 驗證 + 截圖取色）
- [ ] 玩家戰機本體不出現任何暖色（橙、紅、黃）像素（截圖取色驗證）
- [ ] VOLTWYRM 護盾 ARMOR_INTACT 以冷紫藍（`#503090`）顯示，5 人受測「護盾 vs 子彈」識別率 ≥ 90%（繼承 voltwyrm.md §10.4）

### AC-ART-05 外框語言一致性（功能性 — Advisory）

- [ ] 所有敵彈外框為 `#000000`；玩家子彈外框為 `#103060`；巨獸部位外框為 `#1A0800`（截圖取色驗證）
- [ ] PC 4K 截圖確認：無任何正式素材出現非整數縮放導致的模糊像素邊緣

### AC-ART-06 剪影辨識度（體驗性 — Advisory）

- [ ] 三隻巨獸純黑剪影同框：5 人受測者在 1 秒內正確辨識成功率 ≥ 80%（每次美術評審執行）
- [ ] 玩家戰機在任何 Boss 戰截圖中，視覺上感覺比巨獸「明顯更小」（主觀評分 ≥ 4/5，5 人測試）

### AC-ART-07 素材命名與結構（功能性 — Advisory）

- [ ] 所有已提交素材檔案名稱符合 §9.1 命名規範（手動審查或 CI 腳本）
- [ ] `assets/` 資料夾結構符合 §9.3；無素材置於指定路徑外
- [ ] 每個 `.png` 正式素材均有對應 `.aseprite` 源檔於 `assets/src/`（製作評審確認）

---

*文件版本：1.0.0*
*美術總監（Art Director Agent）*
*建立日期：2026-07-01*
*閘門狀態：此文件完成後，所有素材生產方可開始。任何視覺素材的合規性以本文件為最終裁定依據。*
*關聯文件：game-concept.md | game-feel.md | bullet-system.md | hud-ui-system.md | kaiju/01-carapex.md | kaiju/02-lacera.md | kaiju/03-voltwyrm.md*
