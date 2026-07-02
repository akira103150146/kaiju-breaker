# 內容回饋 → 變更提案 (Content Feedback → Change Proposals)

> **來源**: 導演口頭回饋 2026-07-02。**狀態**: 提案，等待核准後才動既有（frozen）GDD。
> **原則**: 每項標明【影響的 GDD】【快 win / 需傳播】【是否牴觸既有 pillar】。核准後我用 `/propagate-design-change` 逐一改文件並補測試。

圖例：🟢 快 win（低風險、不動 pillar）｜🟡 中（需改多份 GDD/平衡）｜🔴 需討論（可能動核心 pillar 或 scope）

---

## P2. 集氣射擊改直向 🟢（其實已是設計，屬實作/原型問題）
- **回饋**：目前集氣（L3 波動砲）是橫向，橫向打不到敵人，應改直向。
- **現況**：`weapon-system.md` L3 = **全幅擴張震波環（vertical/full-width）**，不是橫向光束；遊戲確認為縱向（玩家在下、往上打）。
- **結論**：設計無需改。你看到的橫向是 **HTML 原型 (`prototypes/weapon-feel-concept`) 的表現**。做 Weapons 系統時直接照 GDD 實作成「向上全幅震波」即可。
- **動作**：無需改 GDD；記一條實作備註到 weapons epic story，避免重蹈原型。

## P3a. 玩家散彈隨強化 2→3→4→5 條 🟢
- **回饋**：散彈原本兩條，升級變三、四，滿等五條。
- **現況**：L1 散波雷射 base=3 條、Tier-3=4 條。
- **提案**：改為 **每 Tier 條數 = [2, 3, 4, 5]**（Tier 0/1/2/3）。**受 D₀ 等功率約束**：條數↑則每條威力↓（總輸出守恆），符合「等功率側徵」pillar——升級＝覆蓋變廣、非變強。乾淨且好實作。
- **影響**：`weapon-system.md`（L1 tier 表 + `l1_t3_beam_count`→改為 per-tier 陣列）、`WeaponDef`/`WeaponBalanceConfig` 旋鈕。🟡 需同步平衡表。

## P3b. 玩家子彈依強化有不同大小 🟢
- **提案**：Tier 越高，投射物視覺/命中略放大（或數量放大擇一，避免又大又多破壞平衡）。建議**主要靠數量（P3a），大小僅作視覺回饋**微調，維持等功率。
- **影響**：`art-bible.md`（sprite 尺寸表）、`weapon-system.md`。🟢

## P3c. 敵人子彈分大小顆 🟢
- **回饋**：敵彈也可以有大顆小顆之分。
- **現況**：敵彈固定 4–6px，**無 size/radius 參數**。
- **提案**：`BulletDef` 增 **size 分級（small 4px / medium 7px / large 10px）**，同步命中半徑與視覺。大顆＝更慢更好躲、傷害/威嚇更高；小顆＝快而密。符合「彈幕永遠讀得懂」——大小本身就是讀取線索。
- **影響**：`bullet-system.md`（新增 size 欄位 + 命中）、`EmitterPatternSO`/子彈資料、`art-bible.md`。🟡

## P1a. 敵人彈幕更多元（同心圓放射 / Z字 / 彎曲）🟡
- **回饋**：現在只放一排；想要多重同心圓放射、Z字型、彎曲射擊等，寫進各敵人 prefab。
- **現況**：已有 5 種 **發射形狀** enum：`AIMED_FAN / RING / SPIRAL / WALL / CROSS`。RING＝放射環（多層＝同心圓）、SPIRAL 已在。**但缺「子彈運動」層**——目前子彈多為直線。
- **提案**：在「發射形狀」之上新增 **子彈運動修飾 (motion modifier)**：`STRAIGHT / SINE(Z字/蛇行) / CURVE(彎曲/迴旋) / ACCEL(加速) / HOMING(弱追蹤)`。形狀×運動＝組合爆炸，且**逐一寫進各 `EnemyDef`/prefab** 讓每種敵人有辨識度。
- **影響**：`bullet-system.md`（新增 motion enum + 每種的參數）、`EmitterPatternSO`、各 `EnemyDef`。🟡 需補 EditMode 測試（軌跡確定性）。

## P1b. 敵人移動方式/大小更多樣 🟢
- **現況**：已有 10 種移動 pattern（ram/tri_shot/aimed_gun/ring_burst/shield_flier/side_weaver/column_grunt/splitter/kamikaze/fast_strafer）——其實已不少。
- **提案**：(1) `EnemyDef` 增 **scale/size 分級**（small/normal/large mob），大體型＝慢、血多、威嚇；(2) 若要更多移動花樣，補 2–3 種（如 `figure8` 8字、`teleport_blink` 短瞬移、`orbit` 繞行）。
- **影響**：`stage-system.md`（mob roster）、`MovementPatternSO`、`EnemyDef`。🟢

## P4. 背景可捲動製造「前進」假象 🟢
- **回饋**：背景要移動，做出正在某地區前進的假象。
- **現況**：`art-bible.md` 只有 `backgrounds/space/` 資料夾佔位，**無 parallax 規格**。
- **提案**：新增 **視差捲動規格**：2–3 層深度（遠景慢、中景中、近景快 + 前景粒子），縱向捲動速率隨關卡節奏微調（boss 前減速停下＝到達感）。純表現層、不影響玩法平衡。
- **影響**：新增 `art-bible.md` §背景視差 或獨立 `design/gdd/background-system.md`；`game-feel.md`（捲動速率隨段落）。🟢

## P6. 打擊感依敵人分級差異化 🟡
- **回饋**：全部一樣會膩；基礎小怪＝小爆炸、菁英＝中爆炸+震動、依此類推。
- **現況**：`game-feel.md` 只依**部位型別**分（part break 115ms / boss core 220ms），**未依敵人分級**；菁英目前只是 1.5×彈幕 + 2.5×血，無打擊感差異。
- **提案**：新增**擊殺回饋分級**表：
  | 敵人級別 | Hitstop | 震動 | 爆炸/閃光 |
  |---|---|---|---|
  | 雜兵 mob | ~40ms | 無/極輕 | 小爆 |
  | 菁英 elite | ~80ms | 輕震 6px | 中爆 |
  | 精英王 miniboss | ~130ms | 中震 12px | 大爆 |
  | Boss core（既有） | 220ms | 24px | 全白 |
- **影響**：`game-feel.md`（新增 event rows + 旋鈕）、`EnemyDef`（tier 欄位驅動）。🟡

## P7. 不同等級的怪要有血量/護甲/機制（不只 Boss 有機制）🔴/🟡
- **回饋**：有機制的不該只有 BOSS；不同等級怪要設計不同血量/護甲之類。
- **現況**：雜兵單純、菁英只是 2.5×血；破部位/護甲/軟化機制**專屬 Boss**。
- **提案（分階段，避免 scope 爆炸）**：
  - **階段一 🟢**：`EnemyDef` 正式化 **敵人分級 (mob / elite / miniboss)**，各有不同血量帶與**子彈 size/pattern**（接 P1、P3c、P6），已能製造明顯層次。
  - **階段二 🟡**：給 **elite** 一個**簡化版機制**——單一「護甲弱點」：正面有護甲（正常彈減傷），需繞側/打特定點才破。是 Boss 護甲閘門的縮小版，複用既有 armor 概念。
  - **階段三 🔴（需你拍板）**：給 **miniboss** 一個**迷你破部位**（1–2 個可破部位 → 掉小獎勵）。這會把 `KaijuParts` 系統從「只有 Boss」擴到雜兵層——**已實作的 KaijuParts 剛好能複用**（它不綁定 Boss），但會增加關卡與美術產能。建議 MVP 後再上。
- **影響**：`stage-system.md`、`difficulty-system.md`、`EnemyDef`、可能 `kaiju-part-system.md`（放寬「部位只屬 Boss」的敘述）。🔴 pillar 檢查：難度 pillar 是「難度只縮放彈幕密度、血量/獎勵不變」——**分級怪的血量差異是「不同敵人本來就不同」，不是難度縮放**，不牴觸；但要在 GDD 寫清楚以免和「難度不改血量」混淆。

---

## 建議執行順序（核准後）

1. **P2**：記實作備註（0 成本）。
2. **P3a + P3b**：改 L1 tier 條數/大小 → 平衡表 + 測試。🟢
3. **P1b + P4**：敵人 size 分級 + 背景視差（純表現/資料）。🟢
4. **P3c + P1a**：子彈 size + 運動修飾（bullet-system 較大改，含測試）。🟡
5. **P6 + P7 階段一/二**：敵人分級 + 打擊感分級 + elite 護甲弱點。🟡
6. **P7 階段三**：miniboss 迷你破部位——**MVP 後再議**。🔴

> 你在下方勾選要走哪些、以什麼順序，我就用 `/propagate-design-change` 逐項落到 GDD 並補測試。P2/P3a/P3b/P1b/P4 我可以直接開始（低風險）；P7 階段三想先聽你決定。
