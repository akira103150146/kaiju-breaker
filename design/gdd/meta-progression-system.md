# 元進度與存檔系統 (Meta-Progression & Save System) GDD
## 殲獸戰機 / KAIJU BREAKER

*文件路徑: design/gdd/meta-progression-system.md*
*最後更新: 2026-07-01*
*狀態: Draft*
*相依文件: game-concept.md | material-economy.md | weapon-system.md（LOCKED）| hud-ui-system.md | difficulty-system.md*

---

## A. 概覽 (Overview)

元進度與存檔系統是殲獸戰機所有跨輪持久化資料的唯一負責系統。它定義三件事：

1. **什麼資料跨輪永久保留**（武器 Tier 等級、所有權、素材庫存、巨獸狩獵紀錄、設定）與**什麼資料每輪重置**（場地拾取武器、本輪分數 / 時間、傷害進度）。
2. **武器所有權模型（Weapon Ownership Model）**：武器如何從「鎖定（Locked）」變為「可在 Loadout 畫面選擇（Owned）」。
3. **存檔的讀寫生命週期**：何時寫入磁碟、如何確保原子性（Atomicity）防止損毀、以及版本遷移（Version Migration）策略。

本系統**不**計算素材產量（`material-economy.md` 負責）、不定義武器數值（`weapon-system.md` 負責）、不控制難度縮放（`difficulty-system.md` 負責）——它只**持久化這些系統的狀態**，並在遊戲啟動時將其還原。

**核心承諾（Core Promise）：永久養成進度永不丟失（Permanent Progress is Never Lost）。**

玩家在戰鬥中成功破壞部位時即時獲得的素材，即使下一秒遊戲崩潰或進程被強制終止，已入帳的素材保證在下次啟動時完整存在。武器 Tier 升級同樣不可回滾。

---

## B. 玩家幻想 (Player Fantasy)

**目標 MDA 美學**：能力感（Competence）＋ 投資感（Investment）＋ 成就感（Achievement）

**「我的進度屬於我」** — 玩家進入 Loadout 畫面，看到武器的 Tier 徽章（Tier Badge）、庫存裡的核心數量，不需要想「這是不是有存到？」玩家的每一個決策（投入素材升級、選擇狩獵哪隻巨獸）是真實的長期投資，而不是需要擔心存檔問題的假性資產。

**「失敗不懲罰，成長不欺騙」** — 在 Boss 戰第 45 秒死亡，但前 30 秒已破壞兩個部位並入袋素材——玩家回到主選單，那些素材在庫存裡。存檔系統是「公平的目擊者（Fair Witness）」：如實記錄玩家得到的，不多不少，也不因意外而消失。

**「武器傳記」** — 每把武器的 Tier 徽章不只是數字，它是一份玩家技術與狩獵成果的縮影：多少場狩獵、多少次全破壞、選擇了哪條升級路徑。

---

## C. 詳細規則 (Detailed Rules)

### C.1 永久狀態 vs 每輪狀態 (Persistent vs Per-Run State)

#### C.1.1 永久狀態（Persistent State）——跨輪永久保留

| 資料類別 | 欄位 / 說明 | 寫入時機 |
|---------|-----------|---------|
| **武器所有權（Weapon Ownership）** | `weapons[id].owned: bool` — 是否可在 Loadout 畫面選擇 | 首次拾取該武器莢艙（Weapon Pod）時 |
| **武器 Tier（Weapon Tier）** | `weapons[id].tier: int 0–3` — 永久升級等級 | 升級確認時 |
| **素材庫存（Material Inventory）** | `materials[id]: int ≥ 0` — 5 種素材各自數量 | 每次部位破壞（即時）；狩獵成功結算時（完成度獎勵） |
| **巨獸狩獵紀錄（Kaiju Records）** | 每隻巨獸的曾破壞部位集合、全破壞次數、各難度最佳時間、各難度狩獵次數 | 每次狩獵結束（成功或失敗）時 |
| **上次選擇難度（Last Difficulty）** | `meta.last_selected_difficulty: D1–D4` | 每次確認出發前（`on_loadout_confirmed`）|
| **上次 Loadout（Last Loadout）** | `meta.last_loadout.primary / secondary: WeaponID` | 每次確認 Loadout 出發前 |
| **設定（Settings）** | 減少動態（Reduce-Motion）、色盲模式、文字縮放、BGM / SFX 音量 | 設定變更即時 |
| **統計（Stats）** | 總輪次、總部位破壞數、總全破壞次數、累計遊玩時間（秒） | 每次狩獵結束時 |

#### C.1.2 每輪狀態（Per-Run State）——輪結束後重置

| 資料類別 | 說明 | 重置時機 |
|---------|------|---------|
| **當輪武器裝備（Current Loadout In-Run）** | 啟動時從 `meta.last_loadout` 讀取；場地拾取後替換為當輪臨時武器 | 每輪開始時（從 `last_loadout` 重新載入）|
| **本輪難度（Run Difficulty Tier）** | 開局選定後鎖定至本輪結束 | 每輪開始時（從 `last_selected_difficulty` 讀取）|
| **本輪分數 / 時間（Run Score / Time）** | 用於結算與巨獸最佳時間比對；僅超越最佳時間時更新永久紀錄 | 每輪開始時重置 |
| **場地拾取武器（Field-Dropped Weapon）** | 場地拾取的當輪臨時換裝；若為首次拾取，同步解鎖所有權（見 C.2.2）| 每輪開始時重置 |
| **部位蓄熱 / 破甲進度（H / B Progress）** | `kaiju-part-system.md` 管理的運行時狀態；部位未破壞則無素材入帳 | 每輪開始時重置 |
| **進行中素材動畫（In-Flight Material Animation）** | 視覺軌道球飛行狀態——純 UI 效果；實際入帳已在 `on_part_break` 瞬間完成 | 每輪結束時清除 |

> **設計說明**：素材軌道球飛行動畫是純視覺效果。實際材料在 `on_part_break` 事件觸發瞬間即已計入 `materials` 永久庫存並排入存檔佇列。UI 動畫讓玩家感知這個入帳過程，但「入帳」本身不依賴動畫完成。

---

### C.2 武器所有權模型 (Weapon Ownership Model)

#### C.2.1 兩種武器狀態

| 狀態 | 英文 | 可在 Loadout 畫面選擇？ | 可在升級畫面升級？ | 可在場地拾取並當輪使用？ |
|------|------|----------------------|-----------------|----------------------|
| **鎖定（Locked）** | Locked | 否（顯示鎖定圖示）| 否（升級按鈕灰化）| **是**（任何難度均可從場地掉落並臨時使用）|
| **擁有（Owned）** | Owned | 是 | 是 | 是 |

**核心設計意圖**：鎖定不等於「無法使用」——鎖定武器仍可在任何輪次中從場地拾取並立即使用。「鎖定」只影響 Loadout 畫面的**預選資格**（能否作為本輪起始武器）。

此模型實現兩件事：（1）讓玩家在決定「這把值得農核心來擁有嗎？」之前有機會在場地試用；（2）製造「尚未解鎖武器」的好奇心保留勾（Curiosity Retention Hook），對應 `game-concept.md` 留存勾設計。

#### C.2.2 首次解鎖觸發（First-Pickup Permanent Unlock）

```
on_weapon_pod_pickup(weapon_id):
  if save.weapons[weapon_id].owned == false:
    save.weapons[weapon_id].owned = true
    enqueue_autosave()              // 立即排入非同步寫入佇列（見 C.5.3）
    notify_ui(WEAPON_UNLOCKED, weapon_id)
  // 無論是否首次，均按 weapon-system.md C.3 標準拾取規則換裝
  equip_weapon_for_run(weapon_id)
```

**觸發條件**：玩家機體接觸武器莢艙，無論在哪個難度階或關卡，不需要任何前置條件。`owned` 從 `false` 改為 `true` 並立即持久化。

**分工**：武器莢艙的場地掉落規則（哪個關卡掉落哪些武器池）由 `stage-system.md` 負責。本系統不控制掉落池，只監聽拾取事件並更新所有權。

#### C.2.3 起始所有權（New Game Default Ownership）

| 武器 | 起始狀態 | 理由 |
|------|---------|------|
| **L1 散波雷射（Spread Laser）** | Owned（預設 Loadout 主武器）| 新手友善廣覆蓋；MVP 核心武器 |
| **M1 追蹤飛彈（Homing Missile）** | Owned（預設 Loadout 副武器）| 新手友善自動追蹤；MVP 核心武器 |
| L2、L3、L4 | Locked | 場地發現首次解鎖 |
| M2、M3、M4 | Locked | 場地發現首次解鎖 |

> **對應 `material-economy.md` G.4 MVP 配置**：`active_weapon_pool: {L1, M1}` 直接對應此起始狀態。MVP 場地只掉落 L1 / M1 莢艙；其餘 6 格在 Loadout 畫面顯示「更多武器開發中」（`hud-ui-system.md` J.6）。

#### C.2.4 所有權對 Loadout 的影響

- Loadout 畫面從 `weapons[*].owned` 讀取所有 8 把武器的狀態：`owned = true` 渲染為可選卡片；`owned = false` 渲染為鎖定圖示（帶 T0 標籤）。
- **升級限制**：`owned = false` 的武器在升級畫面的升級按鈕維持灰化——避免玩家對從未見過的武器進行盲目投資，保持「先試後投」的發現樂趣。
- **Tier 升級不賦予所有權**：武器 Tier 的提升不觸發 `owned = true`；所有權僅由場地首次拾取觸發。

---

### C.3 存檔資料模式 (Save Data Schema)

**格式**：單一 JSON 文件，UTF-8 編碼，存於平台標準持久化路徑（Unity：`Application.persistentDataPath`）。

**路徑**：

| 文件 | 路徑 | 用途 |
|-----|------|------|
| 主存檔 | `{persistentDataPath}/save.json` | 當前存檔（讀寫） |
| 備份影本 | `{persistentDataPath}/save.bak.json` | 每次成功寫入後同步更新，損毀時備援 |
| 暫存文件 | `{persistentDataPath}/save.tmp.json` | 原子寫入中間文件（見 C.5.2） |

**單一存檔槽（Single Save Slot）**：本遊戲為單人遊戲，僅維護一個存檔槽。多存檔槽帶來的覆蓋風險與 UI 複雜度均超過其必要性。

```json
{
  "version": 1,
  "integrity_hash": "A4B3C2D1",

  "weapons": {
    "L1": { "tier": 2, "owned": true  },
    "L2": { "tier": 0, "owned": false },
    "L3": { "tier": 0, "owned": false },
    "L4": { "tier": 0, "owned": false },
    "M1": { "tier": 1, "owned": true  },
    "M2": { "tier": 0, "owned": false },
    "M3": { "tier": 0, "owned": false },
    "M4": { "tier": 0, "owned": false }
  },

  "materials": {
    "shard_common":   72,
    "core_carapace":   5,
    "core_limb":       3,
    "core_energy":     0,
    "essence_kaiju":   2
  },

  "kaiju_records": {
    "CARAPEX": {
      "parts_ever_broken":         ["armored_dorsal_cannon", "normal_claw_left", "boss_core"],
      "full_clear_count":           2,
      "hunt_count_per_difficulty":  { "D1": 5, "D2": 1, "D3": 0, "D4": 0 },
      "best_time_per_difficulty":   { "D1": 185.3, "D2": null, "D3": null, "D4": null }
    },
    "LACERA": {
      "parts_ever_broken":         [],
      "full_clear_count":           0,
      "hunt_count_per_difficulty":  { "D1": 0, "D2": 0, "D3": 0, "D4": 0 },
      "best_time_per_difficulty":   { "D1": null, "D2": null, "D3": null, "D4": null }
    },
    "VOLTWYRM": {
      "parts_ever_broken":         [],
      "full_clear_count":           0,
      "hunt_count_per_difficulty":  { "D1": 0, "D2": 0, "D3": 0, "D4": 0 },
      "best_time_per_difficulty":   { "D1": null, "D2": null, "D3": null, "D4": null }
    }
  },

  "meta": {
    "last_selected_difficulty": "D1",
    "last_loadout": {
      "primary":   "L1",
      "secondary": "M1"
    },
    "first_launch_complete": true
  },

  "settings": {
    "reduce_motion":   false,
    "colorblind_mode": "default",
    "text_scale":      1.0,
    "bgm_volume":      1.0,
    "sfx_volume":      1.0
  },

  "stats": {
    "total_runs_started":      12,
    "total_runs_completed":     8,
    "total_parts_broken":      87,
    "total_full_clears":       14,
    "total_play_time_seconds": 43200
  }
}
```

**欄位規格表**：

| 欄位路徑 | 型別 | 有效值 / 範圍 | 說明 |
|---------|------|-------------|------|
| `version` | int | [1, ∞) | 存檔版本；本規範定義 v1 |
| `integrity_hash` | string | 8 位元 hex | CRC32 校驗碼（見 D.2）；校驗範圍為其他所有欄位 |
| `weapons[id].tier` | int | {0, 1, 2, 3} | 永久 Tier 等級；單向只升，不可回滾 |
| `weapons[id].owned` | bool | {true, false} | 是否可在 Loadout 畫面選擇；首次拾取後不可逆為 true |
| `materials[id]` | int | [0, ∞) | 各素材庫存；無上限正整數；只因武器升級消耗而減少 |
| `kaiju_records[id].parts_ever_broken` | string[] | part_id 集合 | 跨所有輪次曾破壞過的部位 ID 集合（集合語義，重複不重記）|
| `kaiju_records[id].full_clear_count` | int | [0, ∞) | 該巨獸的全破壞成功次數 |
| `kaiju_records[id].hunt_count_per_difficulty` | map{D1..D4: int} | [0, ∞) 每階 | 各難度下成功完成狩獵次數 |
| `kaiju_records[id].best_time_per_difficulty` | map{D1..D4: float\|null} | (0, ∞)\|null | 各難度下最短勝利時間（秒）；null = 從未完成 |
| `meta.last_selected_difficulty` | string | {D1, D2, D3, D4} | 對應 `difficulty-system.md` G.2 `remember_last_difficulty` |
| `meta.last_loadout.primary` | string | WeaponID（owned=true 的主武器）| 下次開局預設 Loadout 主武器 |
| `meta.last_loadout.secondary` | string | WeaponID（owned=true 的副武器）| 下次開局預設 Loadout 副武器 |
| `meta.first_launch_complete` | bool | — | 首次引導是否完成；用於 D1 首發預設邏輯 |
| `settings.reduce_motion` | bool | — | 對應 `hud-ui-system.md` I.3 |
| `settings.colorblind_mode` | string | {"default","blue_yellow","shape_priority"} | 對應 `hud-ui-system.md` I.2 |
| `settings.text_scale` | float | {1.0, 1.25, 1.5} | 對應 `hud-ui-system.md` I.1 |
| `settings.bgm_volume` | float | [0.0, 1.0] | 背景音樂音量 |
| `settings.sfx_volume` | float | [0.0, 1.0] | 音效音量 |
| `stats.*` | int | [0, ∞) | 純統計數據；不影響遊戲邏輯，供成就系統未來擴充 |

---

### C.4 存檔版本控制與遷移 (Save Versioning & Migration)

**版本規則**：`version` 為正整數，本規範定義 `CURRENT_VERSION = 1`。每當存檔結構變更（新增、刪除欄位或語義改變），須遞增 `CURRENT_VERSION` 並在此節登記遷移函數（Migration Function）。

**載入時版本處理流程**：

```
on_load(raw_json):
  if raw_json.version > CURRENT_VERSION:
    // 存檔來自未來版本的程式 → 拒絕載入，提示玩家更新遊戲
    show_error(SAVE_TOO_NEW)
    abort()

  while raw_json.version < CURRENT_VERSION:
    raw_json = MIGRATIONS[raw_json.version + 1](raw_json)
    raw_json.version += 1

  // 此時 raw_json.version == CURRENT_VERSION
  validate_integrity(raw_json)      // 見 D.2 校驗公式
  load_into_memory(raw_json)

  if raw_json was migrated:
    autosave()    // 將遷移後版本寫回磁碟，避免每次啟動重複遷移
```

**遷移函數登記表**：

| 目標版本 | 遷移內容 | 登記日期 |
|--------|---------|---------|
| v1 | 初始版本，無需遷移函數 | 2026-07-01 |
| （未來版本由開發者在此補充）| — | — |

**遷移設計原則**：
- 遷移函數必須是**純函數（Pure Function）**——給定相同輸入產生相同輸出，無副作用。
- 舊版本缺少的新欄位以 C.7 **新遊戲預設值**填充。
- 最多支援向前遷移 **3 個版本差距**（`v_current - v_save ≤ save_max_migration_generations`，見 G.2）；超過此範圍的超舊存檔顯示「存檔過舊，無法自動遷移」並提供清除選項。

---

### C.5 存檔生命週期與原子性 (Save Lifecycle & Atomicity)

#### C.5.1 存檔觸發事件（Save Trigger Events）

| 觸發事件 | 描述 | 寫入方式 |
|---------|------|---------|
| `on_part_break(...)` | 部位破壞 → 即時入帳素材（D.1）| **非同步寫入（Async Write）** |
| `on_hunt_end(all_broken=true)` | 全破壞狩獵成功 → 入帳完成度獎勵（精魄 + 碎片）| 非同步寫入 |
| `on_hunt_end(all_broken=false)` | 狩獵結束（非全破壞或失敗）→ 更新 kaiju_records + stats | 非同步寫入 |
| `on_weapon_pod_pickup(first_time=true)` | 首次拾取 → 解鎖所有權 | 非同步寫入 |
| `on_weapon_upgrade_confirmed(...)` | 升級確認 → 扣素材 + 升 Tier（原子操作）| 非同步寫入 |
| `on_loadout_confirmed` | 確認出發 → 記錄 last_loadout + last_difficulty | 非同步寫入 |
| `on_settings_changed` | 設定變更 → 寫入 settings | 非同步寫入 |
| `on_app_suspend / on_app_quit` | 應用暫停 / 退出 | **同步寫入（Sync Write，阻塞至完成）** |

#### C.5.2 原子性寫入序列（Atomic Write via Temp-Then-Rename）

採用暫存文件改名策略，確保磁碟上任何時刻只存在「舊完整狀態」或「新完整狀態」，不存在中間損毀狀態：

```
atomic_write(save_snapshot):
  json_body = canonical_json_serialize(save_snapshot)    // key 字母排序，無縮排
  hash_val  = CRC32_hex(json_body)                       // 見 D.2
  save_snapshot.integrity_hash = hash_val
  json_final = canonical_json_serialize(save_snapshot)   // 含 hash 的最終序列化

  write_file(TEMP_PATH, json_final)     // 寫入暫存路徑
  flush_and_sync(TEMP_PATH)             // 確保資料到達磁碟（非僅緩衝區）
  rename(TEMP_PATH, SAVE_PATH)          // 原子改名（同一文件系統保證）
  copy_file(SAVE_PATH, BACKUP_PATH)     // 更新備份影本
```

#### C.5.3 非同步寫入架構（Async Write Architecture）

除 `on_app_suspend / quit` 外，所有寫入在**背景執行緒（Background Thread）**上進行，不阻塞主遊戲迴圈：

```
// 主執行緒：
enqueue_save(deep_copy(current_save_state))   // 製作快照，立即返回
                                               // 深拷貝確保主執行緒繼續修改不影響寫入

// 背景執行緒（Save Worker）：
while running:
  if pending_snapshot exists:
    atomic_write(pending_snapshot)
    pending_snapshot = null
  else:
    sleep(save_worker_idle_ms)
```

**佇列深度 = 1（覆蓋式）**：若上一次寫入尚未完成，新快照覆蓋待處理快照。最終寫入的永遠是最新狀態。

---

### C.6 永不丟失保證 (Never-Lost Guarantee)

**「失敗是學習，不是懲罰。永久養成進度永不丟失。」** — `game-concept.md` 核心承諾的技術落地。

#### C.6.1 保證的內容（What is Guaranteed）

| 資料 | 保證 | 技術實現 |
|-----|------|---------|
| **已破壞部位的素材** | 進程強制終止後，已入帳素材在下次啟動時完整存在 | `on_part_break` 即時觸發非同步寫入；`on_app_suspend` 觸發同步寫入作為安全網 |
| **武器 Tier 等級** | 升級確認後不可回滾；崩潰不影響已升 Tier | 升級操作為原子寫入（C.5.2），磁碟永遠是升級前或升級後的完整狀態 |
| **武器所有權** | 首次拾取後即持久化 | `on_weapon_pod_pickup(first_time=true)` 即時排入寫入佇列 |
| **設定** | 變更後即時持久化 | `on_settings_changed` 即時排入寫入佇列 |

#### C.6.2 允許丟失的內容（Acceptable Losses — By Design）

| 資料 | 允許丟失的理由 |
|-----|-------------|
| **本輪分數 / 時間**（非最佳成績）| 每輪狀態；符合街機設計——重開即重試 |
| **狩獵完成度獎勵**（精魄 + 完成度碎片）若崩潰在結算前 | 這些獎勵需要「完成任務」才給，崩潰等同任務中斷；非「失敗懲罰」，是「未達完成條件」|
| **場地拾取的當輪臨時武器**（非首次解鎖）| 每輪狀態；下輪從 last_loadout 重載 |
| **進行中部位的 H / B 傷害進度** | 部位未破壞故無素材入帳；不違反承諾 |

#### C.6.3 素材入帳的精確時間點

```
// on_part_break 事件被觸發的同一幀內執行：
materials["shard_common"]       += compute_shard_yield(break_state)    // D.1 公式
materials[core_type(kaiju_id)]  += compute_core_yield(part_type, break_state)
kaiju_records[kaiju_id].parts_ever_broken.add(part_id)
enqueue_save(deep_copy(current_save_state))    // 非同步排入寫入佇列
```

素材在**事件觸發同幀**寫入記憶體中的永久庫存，並排入非同步寫入佇列。玩家看到的軌道球飛行動畫是純視覺反饋，不影響入帳時間點。

---

### C.7 新遊戲初始化 (New Game Initialization)

當不存在存檔文件（首次啟動）或玩家選擇清除存檔時，初始化以下預設狀態並立即寫入磁碟：

```json
{
  "version": 1,
  "integrity_hash": "（自動計算）",

  "weapons": {
    "L1": { "tier": 0, "owned": true  },
    "L2": { "tier": 0, "owned": false },
    "L3": { "tier": 0, "owned": false },
    "L4": { "tier": 0, "owned": false },
    "M1": { "tier": 0, "owned": true  },
    "M2": { "tier": 0, "owned": false },
    "M3": { "tier": 0, "owned": false },
    "M4": { "tier": 0, "owned": false }
  },

  "materials": {
    "shard_common": 0, "core_carapace": 0,
    "core_limb": 0, "core_energy": 0, "essence_kaiju": 0
  },

  "kaiju_records": {
    "CARAPEX":  { "parts_ever_broken": [], "full_clear_count": 0,
                  "hunt_count_per_difficulty": {"D1":0,"D2":0,"D3":0,"D4":0},
                  "best_time_per_difficulty":  {"D1":null,"D2":null,"D3":null,"D4":null} },
    "LACERA":   { "parts_ever_broken": [], "full_clear_count": 0,
                  "hunt_count_per_difficulty": {"D1":0,"D2":0,"D3":0,"D4":0},
                  "best_time_per_difficulty":  {"D1":null,"D2":null,"D3":null,"D4":null} },
    "VOLTWYRM": { "parts_ever_broken": [], "full_clear_count": 0,
                  "hunt_count_per_difficulty": {"D1":0,"D2":0,"D3":0,"D4":0},
                  "best_time_per_difficulty":  {"D1":null,"D2":null,"D3":null,"D4":null} }
  },

  "meta": {
    "last_selected_difficulty": "D1",
    "last_loadout": { "primary": "L1", "secondary": "M1" },
    "first_launch_complete": false
  },

  "settings": {
    "reduce_motion": false, "colorblind_mode": "default",
    "text_scale": 1.0, "bgm_volume": 1.0, "sfx_volume": 1.0
  },

  "stats": {
    "total_runs_started": 0, "total_runs_completed": 0,
    "total_parts_broken": 0, "total_full_clears": 0,
    "total_play_time_seconds": 0
  }
}
```

---

### C.8 跨輪流程 (Cross-Run Flow)

存檔系統的讀寫事件，對應完整跨輪流程如下。此流程與 `hud-ui-system.md` G 章節畫面流程完全對齊。

```
【啟動（App Launch）】
  → 讀取 save.json → 驗證 integrity_hash（D.2）→ 版本遷移（若需要）→ 分發至各子系統
  ↓
【主選單 → Loadout Hub】
  讀取：weapons[*].owned（渲染可選武器）| weapons[*].tier（Tier 徽章）
        materials（庫存與升級費用）| last_loadout（預選武器）
        kaiju_records（完成度顯示）
  ↓ [可選：進入永久升級畫面]
    玩家消耗素材升 Tier → on_weapon_upgrade_confirmed → autosave
  ↓
【難度選擇畫面（Difficulty Select）】
  讀取：meta.last_selected_difficulty（預填難度）
  ↓
【確認出發（Loadout + Difficulty Confirmed）】
  寫入：meta.last_loadout、meta.last_selected_difficulty
  → on_loadout_confirmed → autosave
  ↓
【戰鬥（Combat）】
  讀取：weapons[*].tier（套用武器調校旋鈕）
  事件監聽：
    on_part_break          → 入帳素材 + 更新 parts_ever_broken + autosave（C.6.3）
    on_weapon_pod_pickup   → 若首次：owned=true + autosave
  ↓ 勝利（Boss Core Break）              ↓ 失敗 / 放棄（Player Death / Abandon）
  on_hunt_end(all_broken=?)              on_hunt_failed
  → 入帳完成度獎勵（若全破壞）           → 更新 stats.total_runs_started
  → 更新 kaiju_records（full_clear,      → autosave
    hunt_count, best_time）
  → 更新 stats → autosave
  ↓
【結算畫面（Results Screen）】
  顯示本輪入帳明細（素材 + 是否全破壞）
  [可選：進入升級畫面] → 升級消耗 → autosave
  ↓
【返回 Loadout Hub 開始下一輪】
```

---

## D. 公式 (Formulas)

### D.1 材料入帳公式 (Material Credit Formula)

**命名表達式**：

```
permanent_inventory[m]  ←  permanent_inventory[m]  +  yield(m, event, ctx)

yield(m, event, ctx) =

  // 情況 1：部位破壞即時入帳（material-economy.md D.1 計算結果）
  floor(shard_base × quality_mult[ctx.break_state])
      if event = on_part_break  ∧  m = shard_common

  core_yield(ctx.part_type, ctx.break_state)
      if event = on_part_break  ∧  m = core_type(ctx.kaiju_id)
      // core_type 映射：CARAPEX→core_carapace，LACERA→core_limb，VOLTWYRM→core_energy
      // 僅簽名部位（ARMORED / BOSS_CORE）產核心；NORMAL 部位核心產量 = 0

  0   otherwise（on_part_break 事件中 m 非目標素材）

  // 情況 2：全破壞結算入帳（on_hunt_end 觸發）
  shard_completeness_bonus
      if event = on_hunt_end  ∧  ctx.all_broken = true  ∧  m = shard_common

  essence_per_full_clear
      if event = on_hunt_end  ∧  ctx.all_broken = true  ∧  m = essence_kaiju

  0   if event = on_hunt_end  ∧  ctx.all_broken = false
```

**變數表**：

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `permanent_inventory[m]` | int | [0, ∞) | 玩家對素材 m 的永久持有量（存檔 `materials[id]`）|
| `m` | enum | {shard_common, core_carapace, core_limb, core_energy, essence_kaiju} | 素材類型（定義於 `material-economy.md` C.1）|
| `event` | enum | {on_part_break, on_hunt_end} | 觸發入帳的遊戲事件 |
| `ctx.break_state` | enum | {NORMAL, SOFTENED, SOFTENED_STAGGERED} | 部位被破壞時的狀態；影響 shard 倍率 |
| `ctx.part_type` | enum | {NORMAL_PART, ARMORED_PART, BOSS_CORE_PART} | 決定是否產核心 |
| `ctx.kaiju_id` | enum | {CARAPEX, LACERA, VOLTWYRM, ...} | 決定核心種類 |
| `ctx.all_broken` | bool | {true, false} | 本場狩獵結束時是否全部位均已破壞 |
| `shard_base` | int | [1, 4] | 基礎碎片量（`material-economy.md` G.1，預設 2）|
| `quality_mult` | float | {1.0, 1.5, 2.0} | 品質倍率：NORMAL=1.0 / SOFTENED=1.5 / SOFTENED_STAGGERED=2.0 |
| `shard_completeness_bonus` | int | [3, 10] | 全破壞結算碎片獎勵（預設 5）|
| `essence_per_full_clear` | int | {1, 2} | 全破壞結算精魄數（預設 1）|
| `core_yield(part_type, break_state)` | int | {0, 1, 2} | 核心產量：非簽名部位=0；Standard/Precision=1；Perfect=2（`core_perfect_double_drop=true` 時）|

**輸出範圍**：`permanent_inventory[m]` 無上界（unbounded above）；下界 0。素材只因武器升級消耗而減少，永不為負（升級前置條件驗證）。

**運算範例**（破壞 CARAPEX 的強化背甲砲，部位狀態：SOFTENED = Precision 品質）：
```
yield(shard_common)  = floor(2 × 1.5)  = 3   // SOFTENED 倍率 1.5
yield(core_carapace) = 1                      // ARMORED 簽名部位，Precision 品質（非 Perfect，不雙倍）

// 入帳前：shard_common=72, core_carapace=5
// 入帳後：shard_common=75, core_carapace=6
// → 同幀排入 autosave
```

---

### D.2 存檔完整性公式 (Save Integrity Hash Formula)

**命名表達式**：

```
S  =  CRC32_hex( canonical_json( D  \  { S } ) )
```

**變數表**：

| 符號 | 型別 | 範圍 | 說明 |
|------|------|------|------|
| `D` | JSON object | — | 完整存檔資料物件 |
| `S` | string | 8 位元 hex [00000000–FFFFFFFF] | `D.integrity_hash` 欄位的值 |
| `D \ { S }` | JSON object | — | D 去掉 `integrity_hash` 欄位後的子集 |
| `canonical_json(X)` | function → string | UTF-8 | 確定性 JSON 序列化：所有 key 按字母排序、無縮排 / 空白、浮點格式固定 |
| `CRC32_hex(str)` | function → string | 8 位元 hex | IEEE 802.3 CRC-32 多項式；輸出大寫 hex 字串 |

**輸出範圍**：固定 8 位元 hex 字串，碰撞機率 2⁻³² ≈ 2.3×10⁻¹⁰——足以偵測意外磁碟損毀，不需防惡意篡改（見 H.9）。

**驗證程序**：
```
on_load:
  stored_hash    = D.integrity_hash
  recomputed     = CRC32_hex(canonical_json(D \ {"integrity_hash"}))
  if stored_hash ≠ recomputed:
    try BACKUP_PATH → repeat validation
    if BACKUP_PATH also fails → show_error(SAVE_CORRUPTED) → offer reset
```

**運算範例**（簡化示意）：
```
// canonical_json 輸出（部分）：
'{"materials":{"core_carapace":5,"core_energy":0,...},"version":1,...}'

// 計算 CRC32：
CRC32_hex('{"materials":...}')  →  "A4B3C2D1"   // 示例值

// 寫入 D.integrity_hash = "A4B3C2D1"，再次序列化整個物件後落盤
```

---

## E. 邊界情況 (Edge Cases)

### E.1 進程在兩次部位破壞之間被強制終止 (Process Kill Mid-Run)

**情況**：部位 A 破壞後、部位 B 破壞前強制終止進程。

**規則**：
- 部位 A 的素材已在 `on_part_break` 觸發時排入非同步佇列 → **大概率已落盤**（正常情況）。
- 若終止發生在非同步寫入完成之前（極罕見的特定時間窗口）→ 部位 A 的素材丟失為**設計上允許的邊界損失**，素材量小，機率極低。
- `on_app_suspend`（行動裝置切換 App）觸發**同步寫入**，捕捉最常見的「使用者切走」場景，大幅降低此邊界的發生頻率。
- 部位 B 的 H / B 傷害進度為每輪狀態 → 下次啟動重置（不違反承諾）。

### E.2 存檔文件損毀（校驗失敗）(Save File Corruption)

**情況**：CRC32 校驗失敗（D.2 驗證程序）。

**規則**：
1. 自動嘗試讀取備份影本 `save.bak.json` 並重新校驗。
2. 若備份亦失敗 → 顯示錯誤畫面「存檔損毀（Save Corrupted）」，提供兩個選項：
   - **重置全部進度**（New Game，清除損毀存檔）
   - **帶損毀存檔繼續**（強制讀取，記錄 `save_integrity_warning = true`，風險自負）

### E.3 首次啟動（無存檔文件）(First Launch)

**情況**：找不到 `save.json`。

**規則**：以 C.7 預設值初始化新存檔 → 立即同步寫入磁碟 → 正常進入 Loadout 畫面。

### E.4 存檔版本高於當前應用版本（降級情境）(Newer Save, Older App)

**情況**：玩家在新版本上存了檔，降級回舊版程式。

**規則**：拒絕載入，顯示「存檔版本較新，請更新遊戲（Save Version Too New）」，不修改存檔文件。

### E.5 last_loadout 指向的武器不在 Owned 集合內

**情況**：遊戲更新後（或存檔遷移後），`last_loadout.primary` 指向 `owned = false` 的武器。

**規則**：載入時驗證並 fallback：
```
if NOT weapons[last_loadout.primary].owned:
    last_loadout.primary = first_weapon_where(pool=LASER, owned=true)
    // L1 永遠為 Owned（新遊戲預設），fallback 必然存在

if NOT weapons[last_loadout.secondary].owned:
    last_loadout.secondary = first_weapon_where(pool=MISSILE, owned=true)
    // M1 永遠為 Owned，同理
```

### E.6 升級確認後立即崩潰（素材扣除 + Tier 提升的原子性）

**情況**：`on_weapon_upgrade_confirmed` 執行後、非同步寫入完成前進程崩潰。

**規則**：原子寫入（C.5.2）確保磁碟永遠是「升級前完整狀態」或「升級後完整狀態」，絕不出現「素材扣了但 Tier 沒升」的中間狀態。若快照未落盤，效果等同升級操作未完成（素材回到升級前數量），玩家需再次確認。這是允許的：升級是 UI 操作，不是戰鬥中的即時事件，再次點擊升級即可。

### E.7 非全破壞狩獵結束（部分部位破壞後勝利或失敗）

**情況**：玩家僅破壞部分部位後，Boss 戰以勝利（只打核心策略）或失敗收場。

**規則**：
- 各部位破壞時的素材在 `on_part_break` 入帳（永久保留）。
- `kaiju_records[id].parts_ever_broken` 更新已破壞的部位 ID 集合（永久記錄）。
- 完成度獎勵（精魄 + 完成度碎片）不發放——未達全破壞條件，不是懲罰，是條件未滿足。
- 勝利但非全破壞時，`hunt_count_per_difficulty` 仍計數（勝利次數）；`full_clear_count` 不計數。

---

## F. 系統相依 (Dependencies)

| 相依系統 | 方向 | 本系統消費 | 本系統提供 |
|---------|------|-----------|---------|
| **material-economy.md** | 雙向 | `on_part_break` 事件的入帳計算參數（D.1 公式的 yield 值由 material-economy 計算後傳入）；`on_hunt_end` 完成度獎勵值 | `materials[*]` 庫存（升級費用比對）；`on_weapon_upgrade_confirmed` 素材扣除後的新庫存快照 |
| **weapon-system.md（LOCKED）** | 單向（讀取）| 武器 ID 列表（L1–L4, M1–M4）；Tier 效果調校旋鈕定義 | `weapons[*].tier`（武器系統讀取，決定運行時效果）；`weapons[*].owned`（Loadout 畫面讀取）|
| **difficulty-system.md** | 雙向 | 對應 `remember_last_difficulty` 設計（讀取 last_selected_difficulty 預填 UI）| `meta.last_selected_difficulty`（玩家每輪確認後寫入）|
| **hud-ui-system.md** | 服務（提供資料）| 升級確認事件；Loadout 確認事件；難度確認事件；設定變更事件 | `weapons[*]`（Tier 徽章、鎖定狀態）；`materials[*]`（庫存顯示）；`kaiju_records[*]`（完成度顯示）；`settings[*]`（設定還原）|
| **stage-system.md** | 單向（訂閱事件）| `on_weapon_pod_pickup(weapon_id, is_first_time)` 事件 | `weapons[*].owned`（場地系統查詢以決定 is_first_time 旗標）|
| **kaiju-part-system.md** | 單向（訂閱事件）| `on_part_break(part_id, part_type, break_state, kaiju_id, ...)` 事件（帶入 yield 參數）| `kaiju_records[*].parts_ever_broken`（供巨獸 UI 讀取完成度）|

**本系統訂閱的關鍵事件簽名（待跨系統確認）**：

```
// kaiju-part-system 發出；material-economy 計算 yield 後填入 payload：
on_part_break(
    part_id:       string,
    part_type:     PartType,         // NORMAL_PART | ARMORED_PART | BOSS_CORE_PART
    break_state:   BreakState,       // NORMAL | SOFTENED | SOFTENED_STAGGERED
    kaiju_id:      string,
    shard_yield:   int,              // material-economy.md D.1 計算結果
    core_yield:    int               // 同上
)

// 狩獵結束事件（material-economy 計算完成度獎勵後填入）：
on_hunt_end(
    kaiju_id:              string,
    is_all_parts_broken:   bool,
    completion_time_s:     float,
    difficulty:            DifficultyTier,
    shard_bonus:           int,      // 完成度碎片獎勵（0 if not all_broken）
    essence_yield:         int       // 精魄數量（0 if not all_broken）
)

// 武器拾取事件（stage-system 或 player 物件發出）：
on_weapon_pod_pickup(weapon_id: WeaponID, is_first_time: bool)

// 升級確認事件（upgrade UI 發出）：
on_weapon_upgrade_confirmed(
    weapon_id:     WeaponID,
    new_tier:      int,
    cost_shard:    int,
    cost_core:     int,
    cost_essence:  int
)
```

---

## G. 調校旋鈕 (Tuning Knobs)

**所有旋鈕存放於 `assets/data/meta/save-config.yaml`，禁止硬編碼。**

### G.1 武器解鎖旋鈕

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|---------|--------|----------|------|------|
| `starting_owned_weapons` | ["L1","M1"] | — | 閘門 | 新遊戲起始擁有的武器 ID 列表；變更需確認 Loadout Hub fallback 邏輯 |
| `unlock_trigger` | "first_pickup" | — | 閘門 | 武器解鎖觸發方式；目前唯一合法值為 "first_pickup"（首次拾取莢艙）|

### G.2 存檔行為旋鈕

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|---------|--------|----------|------|------|
| `save_async_queue_depth` | 1 | 1–3 | 閘門 | 非同步寫入佇列深度；1 = 最新快照覆蓋式 |
| `save_backup_enabled` | true | — | 閘門 | 是否維護備份影本 `save.bak.json` |
| `save_max_migration_generations` | 3 | 2–5 | 閘門 | 向下相容最大版本差距；超過則拒絕遷移並提示玩家 |
| `save_worker_idle_ms` | 100 | 50–500 | 手感 | 存檔背景執行緒空閒輪詢間隔（毫秒）|

### G.3 完整性旋鈕

| 旋鈕名稱 | 預設值 | 安全範圍 | 類型 | 說明 |
|---------|--------|----------|------|------|
| `integrity_algorithm` | "CRC32" | {"CRC32","SHA1"} | 閘門 | 校驗演算法；SHA1 可作未來升級選項 |
| `integrity_fail_action` | "try_backup" | {"try_backup","warn_and_continue","force_reset"} | 閘門 | 校驗失敗時的行動策略 |

### G.4 MVP 子集旋鈕

| 旋鈕名稱 | MVP 值 | 全版本值 | 說明 |
|---------|--------|---------|------|
| `active_weapon_ids` | ["L1","M1"] | 全 8 把 | 存檔中追蹤的武器 ID 集合 |
| `active_kaiju_ids` | ["CARAPEX"] | ["CARAPEX","LACERA","VOLTWYRM",...] | kaiju_records 中初始化的巨獸 ID |

---

## H. 驗收標準 (Acceptance Criteria)

### H.1 進程殺除後已得素材完整（功能性 — **阻斷**）

- [ ] **手動 QA 測試**：在任意 `on_part_break` 觸發後 100 ms 內，以 Task Manager（Windows）/ `kill -9`（行動裝置）強制終止進程；重啟應用程式，驗證該部位素材產量（按 D.1 公式預期值）在庫存中完整存在，誤差為 0。
- [ ] 每個里程碑（Prototype / Vertical Slice / Full Vision）各執行 10 次，通過率須達 100%。
- [ ] **特殊情境**：`on_app_suspend` 後立即強制終止 → 存檔完整（同步寫入應已完成）。

### H.2 材料入帳公式正確性（功能性 — 阻斷）

- [ ] 自動化測試：`tests/unit/save/material_credit_formula_test.[ext]`，覆蓋：
  - 3 break_state × 3 part_type × 3 kaiju = 27 種部位破壞基本情境
  - `on_hunt_end(all_broken=true)` → 精魄 + 碎片獎勵數值正確
  - `on_hunt_end(all_broken=false)` → 精魄 = 0，完成度碎片 = 0
  - 連續 4 次部位破壞 → `materials[*]` 累加值正確，無截斷 / 溢位

### H.3 武器所有權狀態機（功能性 — 阻斷）

- [ ] 新遊戲：L1.owned=true, M1.owned=true；L2/L3/L4/M2/M3/M4.owned=false。
- [ ] 首次拾取 L2 莢艙 → L2.owned=true；應用程式重啟後仍為 true。
- [ ] 第二次拾取 L2 莢艙 → owned 維持 true（不觸發重複解鎖通知）。
- [ ] 自動化測試：`tests/unit/save/weapon_ownership_state_test.[ext]`，覆蓋所有 8 把武器的 owned 狀態轉換（new game → first pickup → restart → verify → second pickup → verify）。

### H.4 原子性寫入——無中間損毀狀態（功能性 — 阻斷）

- [ ] 在 `atomic_write` 的 `write_file` 步驟後、`rename` 步驟前模擬進程終止：磁碟上 `save.json` 為兩種完整狀態之一（舊或新），不存在損毀的 JSON（不完整文件 / 無效 JSON）。
- [ ] 自動化測試：`tests/unit/save/atomic_write_test.[ext]`，在 `write_file` 完成後注入 exception，確認 save.json 仍為有效 JSON 且與舊快照完全匹配。

### H.5 完整性校驗（功能性 — 阻斷）

- [ ] 手動修改 `save.json` 任意數值後重啟 → CRC32 校驗失敗，系統嘗試讀取備份影本。
- [ ] 若 save.json 和 save.bak.json 均損毀 → 顯示錯誤畫面，提供重置選項，應用程式不崩潰。
- [ ] 自動化測試：`tests/unit/save/integrity_hash_test.[ext]`，驗證 `CRC32_hex(canonical_json(D))` 計算結果與預先計算的參考值一致（回歸測試，防止 canonical_json 實作漂移）。

### H.6 版本遷移（功能性）

- [ ] v1 存檔在當前應用版本（v1）下正確讀取，無任何遷移行為。
- [ ] 日後推出 v2 時：v1 存檔成功遷移至 v2，缺少的新欄位以新遊戲預設值填充，不丟失既有資料。
- [ ] `save.version > CURRENT_VERSION` → 拒絕載入，顯示「請更新遊戲」，不修改存檔。

### H.7 難度與 Loadout 預填（功能性）

- [ ] 首次啟動：難度預選 D1；Loadout 預選 L1 + M1（對應 `difficulty-system.md` G.2 `default_difficulty_on_first_launch = D1`）。
- [ ] 完成一輪後返回主選單：難度預填為上輪選擇；Loadout 預填上次選擇的武器。
- [ ] 自動化測試：`tests/unit/save/last_selection_persistence_test.[ext]`，覆蓋首次啟動 / 一輪後 / 兩輪切換難度後三種情境。

### H.8 last_loadout Fallback 正確性（功能性）

- [ ] 當 `last_loadout.primary` 指向 `owned = false` 的武器時，fallback 為第一把 `owned = true` 的主武器（保證為 L1）。
- [ ] 自動化測試：`tests/unit/save/loadout_fallback_test.[ext]`。

### H.9 夢想層排行榜欺騙防護（設計備忘 — 非阻斷）

> 本系統刻意**不實作**客戶端 DRM 或防篡改加密。CRC32 的目的是偵測意外磁碟損毀，不是阻止惡意篡改。未來若啟動「夢想層（Dream Layer）」排行榜（`game-concept.md` Retention Hooks），成績驗證應由**伺服器側（Server-Side Validation）**處理，不在客戶端存檔層解決。**存檔安全的職責：防損毀。防作弊的職責：伺服器**。此為正式設計備忘，待排行榜 GDD 立案時引用。

---

## 開放問題 (Open Questions)

| 優先級 | 問題 | 阻斷里程碑 | 解答方式 |
|--------|------|------------|---------|
| **高** | `on_part_break` 事件 payload 中的 `shard_yield` / `core_yield` 由誰計算後傳入？當前建議由 `material-economy.md` 計算後填入 event payload，本系統直接讀取。需三方（kaiju-part-system / material-economy / save-system）確認事件所有人與資料流向。 | Prototype | 三方 GDD 作者協調確認事件簽名 |
| **中** | 升級畫面是否允許升級「尚未擁有（owned=false）」的武器（鎖定武器預投資）？本 GDD 當前規則：不允許（見 C.2.4）。若設計師希望開放，需修改 C.2.4 並更新 `hud-ui-system.md` F.2 升級按鈕邏輯。 | Vertical Slice | 設計師試玩後決定 |
| **低** | `stats.*` 欄位是否需要增細顆粒度以支援未來成就系統（如分難度的全破壞次數、特定武器使用場數）？ | Full Vision | 待成就系統 GDD 提出需求時擴充 stats schema（版本遷移）|

---

*文件版本：1.0.0*
*作者：Systems Designer Agent*
*最後更新：2026-07-01*
*狀態：Draft*
*關聯 GDD：game-concept.md | material-economy.md | weapon-system.md（LOCKED）| hud-ui-system.md | difficulty-system.md*
