# Active Session State

*Last updated: 2026-07-01*

## Current Task
**武器系統設計 → GDD → 原型實作**（導演核可全程）。
- 概念原型 `break-part-feel` 已試玩 → 導演判定 OK（等於通過概念閘門）。
- 回饋：打擊 juice 要更強 → **部位破壞/死亡時慢動作 + 大招螢幕震動**（已納入武器原型與未來引擎實作清單）。
- 武器方向拍板（3 項）：① 雙軌「蓄熱軟化→衝擊擊破」骨架保留 ② M3 魚雷 6× 爆發先保留、靠實測平衡再調 ③ GDD 正式化並實作。
- **武器 GDD 已寫** → `design/gdd/weapon-system.md`（8 段齊全；自我 design-review + balance-check 通過；已補 2 個阻斷缺口：`1 D₀=10 BU` 換算錨點、`B_max_boss_core` + `required_break_threshold_*` 護甲門檻旋鈕）。
- **武器手感原型建置中**（背景 agent）→ `prototypes/weapon-feel-concept/prototype.html`（8 武器可切換、雙軌、juice）。

## Project Snapshot
- **Game**: 殲獸戰機 / KAIJU BREAKER — 科幻縱向彈幕射擊 ＋ 破壞部位狩獵養成
- **Stage**: Concept (`production/stage.txt`) — 概念閘門已實質通過，準備進 map-systems
- **Review mode**: lean (`production/review-mode.txt`)
- **Engine**: Unity 6.3 LTS — configured（C#, URP 2D + Pixel Perfect, Box2D）；`Assets/` 尚空，Unity 專案未建
- **Platform**: 多平台（PC + 手機）
- **Visual direction**: 像素街機懷舊 (Retro Pixel Arcade)

## Progress Checklist
- [x] /start onboarding (stage=Concept, review=lean)
- [x] /brainstorm → 概念文件 `design/gdd/game-concept.md`
- [x] /setup-engine (Unity 6.3 LTS)
- [x] /prototype `break-part-feel`（HTML）→ 導演試玩 OK
- [x] 武器系統 GDD `design/gdd/weapon-system.md`（8 武器 sidegrade、雙軌、公式、調校旋鈕、驗收）
- [~] 武器手感原型 `prototypes/weapon-feel-concept/prototype.html`（背景建置中）
- [ ] `/art-bible`（種子 = 概念文件視覺錨點）
- [ ] `/map-systems`

## Key Decisions
- 5 pillars locked：科技對巨獸 / 頭目是靈魂 / 橫向選擇 / 破壞即獎勵 / 難度是門不是牆
- 武器：2 池（主=雷射系×4 蓄熱軟化，副=飛彈系×4 衝擊破甲），場地掉落取得，等 D₀ 功率預算 sidegrade
- 雙軌骨架：雷射蓄熱→部位軟化→飛彈破甲擊破（把兩池綁進破部位循環）
- 每武器 Tier-3 唯一機制「深化性格、不加數值」
- M3 魚雷 6× 爆發保留，靠 `required_break_threshold_*` 護甲門檻防止秒殺（H.3 阻斷驗收）

## Open Follow-ups（GDD 審查標出，實作前需處理）
- **缺 GDD `design/gdd/kaiju-part-system.md`**（可破壞部位系統）— 武器發出 on_laser_hit / on_missile_hit / on_part_break 事件，接收端契約未定，含部位「相鄰圖」（L2/M3 Tier-3 連鎖依賴）。**下一個該寫的 GDD。**
- **缺 GDD `design/gdd/material-economy.md`**（素材→永久升級經濟）。
- 掉落頻率指引（Drop System）尚未定。
- 待原型驗證：#1 M3 6× 是否秒殺（TTB 矩陣）、#4 軟化狀態 0.5s 內是否可辨。

## Biggest Risk
武器平衡（等價而異性格）— 已建 D₀ 等功率模型 + 8×8 無主導 loadout 驗收（H.2）鎖住，但需原型/實測數據校準。
