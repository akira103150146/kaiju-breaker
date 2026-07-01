# 系統索引與依賴圖 (Systems Index & Dependency Map)
## 殲獸戰機 / KAIJU BREAKER

*文件路徑: design/systems-index.md*
*最後更新: 2026-07-01*
*狀態: Living document — 每寫一份新 GDD 就更新*
*用途: 全專案系統地圖。追蹤每個系統的設計狀態、彼此依賴、與剩餘工作優先序。*

---

## 1. 系統清單與狀態 (System Inventory)

| # | 系統 (System) | GDD | 狀態 | 摘要 |
|---|---------------|-----|------|------|
| S1 | **武器系統 (Weapon System)** | `gdd/weapon-system.md` | ✅ LOCKED | 8 武器 sidegrade、雙軌蓄熱→破甲、D₀ 等功率、Tier 0–3 |
| S2 | **可破壞部位系統 (Breakable Part System)** | `gdd/kaiju-part-system.md` | ✅ 草稿完整 | 熱量/破甲雙槽、狀態機、部位類型、相鄰圖、事件契約 |
| S3 | **素材經濟與永久升級 (Material Economy)** | `gdd/material-economy.md` | ✅ 草稿完整 | 5 素材、巨獸主題綁定核心、淺養成曲線、跨輪永久 |
| S4 | **關卡系統 (Stage System)** | `gdd/stage-system.md` | ✅ 草稿完整 | 手作波段池隨機重組、6 種雜兵、武器莢艙掉落、3 關、四難度密度 |
| C1 | **巨獸內容 (Kaiju Roster)** | `gdd/kaiju/01–03` | ✅ 首批 3 隻 | CARAPEX(甲殼/教學) · LACERA(肢體/移動) · VOLTWYRM(能量/彈幕) |
| S5 | **難度系統 (Difficulty System)** | `gdd/difficulty-system.md` | ✅ 草稿完整 | 四階(D1 普通/D2 困難/D3 極限/D4 惡夢)只縮放彈幕密度/雜兵數;TTB 與武器輸出跨難度不變(阻斷驗收) |
| S6 | **輸入系統 (Input System)** | — | ⬜ 缺 GDD | 雙輸入(觸控 + 鍵鼠/手柄);觸控彈幕手感 = 概念未解風險 |
| S7 | **HUD / UI 系統** | — | ⬜ 缺 GDD | 武器 HUD、熱量/破甲槽、素材計數、loadout 選擇、軟化提示可讀性(阻斷 #4 一部分在此) |
| S8 | **VFX / SFX / 打擊感 (Game Feel)** | `gdd/game-feel.md` | ✅ 草稿完整 | juice:hitstop 115ms、慢動作、螢幕震動(≤24px 護欄)、軟化簽章(#FF6600 ≤0.5s 可辨,解阻斷 #4)、破部位爆發;含 reduce-motion 開關 |
| S9 | **子彈/彈幕引擎 (Bullet System)** | — | ⬜ 缺 GDD | 彈幕生成/物件池/可讀性;巨獸攻擊模式已定義,底層技術待架構期 |
| S10 | **存檔/loadout 元系統 (Meta & Save)** | 部分於 S3 | ◻ 部分 | 跨輪永久進度、loadout 選擇、存檔;部分規則已在 material-economy |

圖例:✅ 草稿完整 / ◻ 部分涵蓋 / ⬜ 缺 GDD

---

## 2. 依賴圖 (Dependency Graph)

```
                         ┌─────────────────┐
                         │  S6 輸入系統     │──── 控制 ───┐
                         └─────────────────┘             │
                                                          ▼
   S1 武器系統 ──(on_laser_hit / on_missile_hit / on_l3_wave_hit)──▶ S2 部位系統
       ▲                                                        │
       │ Tier 升級讀取旋鈕                                       │ on_part_break
       │                                                        ▼
   S3 素材經濟 ◀────────(破部位→掉素材, break_quality)──────────┘
       │                                                        │
       │ 永久升級(sink)                                         │ 狀態讀出
       ▼                                                        ▼
   S10 元系統/存檔                                        S7 HUD/UI ── 軟化提示可讀性
                                                                ▲
   C1 巨獸內容 = S1+S2+S3 的實例化(部位配置/攻擊模式/掉落)      │
       ▲                                                        │
       │ 關卡高潮 = 巨獸                                         │
   S4 關卡系統 ──(武器莢艙掉落 → S1 場地取得)                    │
       │  ├─ 雜兵波段(S9 彈幕)                                  │
       │  └─ 由 S5 難度決定密度 ──────────────────────────────┘
       ▼
   S8 VFX/SFX ── 破部位 juice / 軟化提示 / 螢幕震動(跨 S2/S7)
```

**關鍵資料流**:
- S1→S2:武器發命中事件,部位系統維護熱量/破甲狀態(權威事件定義在 S2)。
- S2→S3:`on_part_break(break_quality, drop_table_id)` 觸發素材掉落。
- S3→S1:素材升級武器 Tier(Tier-3 解鎖獨特機制)。
- C1 綁定 S1/S2/S3:每隻巨獸的主題決定其核心素材(甲殼→core_carapace / 肢體→core_limb / 能量→core_energy)。
- S4→S1:武器靠關卡雜兵掉的莢艙取得(雙池:主=雷射 / 副=飛彈)。
- S5 橫切:只縮放彈幕密度/雜兵數,絕不鎖內容(〔難度是門〕)。

---

## 3. 剩餘設計工作優先序 (Design Backlog Priority)

| 優先 | 系統/工作 | 為何現在 | 阻斷什麼 |
|------|-----------|----------|----------|
| ~~P0~~ ✅ | ~~S8 遊戲手感 + 軟化提示可讀性~~ → `gdd/game-feel.md` **完成**(hitstop/慢動作/螢幕震動 juice + #FF6600 軟化簽章解阻斷 #4) | — | — |
| ~~P0~~ ✅ | ~~S5 難度系統 GDD~~ → `gdd/difficulty-system.md` **完成**(四階密度縮放 + 跨難度不變驗收) | — | — |
| **P0(新)** | **S7 HUD/UI 完整 GDD** | game-feel 已定義部位上的軟化簽章,但武器 HUD、雙槽顯示、loadout 選擇、素材計數仍缺 | 玩家決策可讀性 |
| **P1** | **S6 輸入系統 GDD** | 概念未解風險:觸控彈幕手感是否成立;影響手機主平台 | 手機可玩性驗證、原型觸控路徑 |
| **P1** | **S7 HUD/UI 完整 GDD** | 武器 HUD、雙槽顯示、loadout 選擇、素材計數 | 玩家決策可讀性、養成回饋 |
| **P2** | **S9 彈幕引擎 GDD** | 攻擊模式已在巨獸文件定義,底層物件池/效能屬架構期 | 手機彈幕效能(概念最高技術風險) |
| **P2** | **S10 存檔/loadout 元系統 GDD** | 跨輪永久進度、存檔格式;部分已在 S3 | 進度保存、防竄改(安全) |
| **P3** | 更多巨獸(4–5 隻達 Full Vision) | 內容擴充,MVP 只需 CARAPEX | Alpha/Full Vision 內容量 |

---

## 4. 跨系統未解問題 (Open Cross-System Questions)

- **原型待驗**:M3 魚雷 6× 引爆是否秒殺部位(TTB 矩陣)?軟化狀態 0.5s 內可辨?(武器手感原型 `prototypes/weapon-feel-concept/` 已可測)
- **農刷平衡**:LACERA 為 `core_limb` 唯一來源且每場高產,是否使玩家只農單一巨獸?(Vertical Slice 監測選擇分布)
- **教學武器池**:Stage 1 是否納入 L3 波動砲,還是留到 Stage 2 首見?(關卡系統開放問題)
- **觸控手感**:雙輸入下彈幕精準度(Sky Force 式手指拖曳偏移)未驗證 → S6 + 原型觸控路徑。

---

## 5. 對映階段閘門 (Stage Gate Alignment)

- **Concept → Pre-Production 閘門**:需 MVP 系統 GDD 齊全(S1–S4 ✅)＋ 概念核可(lean)。目前 P0 的 S5/S7/S8 建議在進 Pre-Production 前補上,以支撐 vertical slice。
- **Pre-Production**:以 `/vertical-slice` 驗證完整遊戲循環(需 S5–S8 到位)。
- **下一步建議**:補 P0 三項(S8 juice/可讀性、S5 難度)→ `/review-all-gdds` 全域一致性複審 → `/map-systems` 或 `/gate-check` 決定是否進 Pre-Production。
