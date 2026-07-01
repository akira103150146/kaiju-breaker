# ADR-0004：存檔系統 — JSON + 原子寫入 + CRC32 (Save System)

- **Title**: 單槽 JSON 存檔於 `Application.persistentDataPath`；暫存改名原子寫入 + 備份；CRC32 完整性；版本遷移；背景非同步寫、事件即時入帳保證「永不丟失」
- **Status**: **Accepted**
- **Date**: 2026-07-01
- **Deciders**: Technical Director
- **相關**: `meta-progression-system.md`（權威規格）、`material-economy.md` F.5、ADR-0002（事件/查詢）、ADR-0003（靜態調校 vs 玩家可變資料的分離）

---

## Context（技術脈絡與問題）

`meta-progression-system.md` 定義**核心承諾：永久養成進度永不丟失 (Permanent Progress is Never Lost)**。玩家破壞部位即時獲得的素材，即使下一秒崩潰或進程被殺，下次啟動須完整存在；武器 Tier 升級不可回滾。

需求要點（權威在該 GDD）：
- **單槽 JSON**，UTF-8，存 `Application.persistentDataPath`；主檔 `save.json` + 備份 `save.bak.json` + 暫存 `save.tmp.json`。
- **原子寫入**：暫存改名 (temp-then-rename)，磁碟任何時刻只有「舊完整」或「新完整」狀態。
- **CRC32 完整性**：偵測**意外磁碟損毀**（非防惡意篡改）；校驗失敗 → 讀備份 → 皆失敗顯示錯誤+重置選項。
- **版本遷移**：`version` 正整數，純函數遷移鏈，最多向前 3 世代；來自未來版本則拒載。
- **非同步背景寫**（佇列深度 1，覆蓋式）；`on_app_suspend/quit` 同步寫作安全網。
- **事件即時入帳**：`on_part_break` 同幀寫記憶體永久庫存 + enqueue autosave。

問題：**在 Unity 6.3 落地此規格，用什麼序列化與 I/O 策略？**

---

## Decision（決策）

`KaijuBreaker.Meta` 組件實作存檔系統，經 `ISaveService`（ADR-0002）供其他系統使用。

### 1. 序列化
- **JSON**，UTF-8。序列化採**確定性 canonical 形式**：key 字母排序、無縮排/空白、浮點固定格式——CRC32 計算與回歸測試依賴此確定性。
- Unity 內建 `JsonUtility` 對巢狀 dictionary（如 `weapons{}`、`materials{}`、`kaiju_records{}`）支援弱 **[需查證 6.3 API]**；預期採**明確的 DTO 類別 + 自訂 canonical 序列化器**（或評估 System.Text.Json / Newtonsoft via package）確保 canonical 輸出與 dictionary 支援。序列化器選型於實作時決定並記錄，**不臆造 `JsonUtility` 能力**。

### 2. 原子寫入序列（對齊 `meta-progression-system.md` C.5.2）
```
1. json_body = canonical_serialize(snapshot 不含 hash)
2. hash = CRC32_hex(json_body)；snapshot.integrity_hash = hash
3. json_final = canonical_serialize(snapshot 含 hash)
4. write_file(save.tmp.json)
5. flush + fsync（確保落盤，非僅緩衝）   ← [需查證 6.3 API] File flush/sync 保證
6. rename(save.tmp.json → save.json)      ← 同檔系統原子改名
7. copy(save.json → save.bak.json)
```

### 3. 完整性 (Integrity)
- `integrity_hash = CRC32_hex(canonical_json(D \ {integrity_hash}))`（IEEE 802.3 CRC-32，8 位 hex 大寫）。
- 載入校驗失敗 → 試 `save.bak.json` → 皆失敗 → 錯誤畫面（重置 / 帶損毀繼續），**不崩潰**。
- **明確非目標**：不做客戶端 DRM/防篡改。防作弊屬未來伺服器側排行榜職責（`meta-progression-system.md` H.9）。

### 4. 版本遷移
- `CURRENT_VERSION = 1`。載入：`version > CURRENT` → 拒載（提示更新）；`version < CURRENT` → 依 `MIGRATIONS[]` 純函數鏈逐級遷移，遷移後 autosave 回寫。
- 最多向前 3 世代（`save_max_migration_generations`）；超過提示「存檔過舊」+ 清除選項。
- 缺欄位以「新遊戲預設值」填充。

### 5. 非同步寫入架構
- 主執行緒：`enqueue_save(deep_copy(state))` 製快照立即返回（深拷貝使主執行緒續改不影響寫入）。
- 背景 **Save Worker 執行緒**：佇列深度 1，新快照覆蓋待處理者（最終寫最新）。
- **例外**：`on_app_suspend`/`on_app_quit` 走**同步寫入**（阻塞至完成）作安全網——捕捉行動裝置「切走 App」最常見場景。
- **[需查證 6.3 API]**：Unity 生命週期回呼 `OnApplicationPause`/`OnApplicationQuit` 在各平台（尤其 Android/iOS）的觸發保證與可用寫入時間窗。

### 6. 事件即時入帳（永不丟失落地）
- `Meta` 訂閱 `on_part_break`：同幀寫記憶體永久庫存（素材由 `Economy` 依 break_quality 計算，見 ADR-0002 資料流）+ `enqueue_save`。
- `on_weapon_upgrade_confirmed`/`on_weapon_pod_pickup(first)`/`on_settings_changed`/`on_loadout_confirmed` 同樣即時 enqueue。
- 允許的邊界損失（by design）：非同步寫完成前的極罕見時間窗；本輪分數/時間；未達全破壞的完成度獎勵（`meta-progression-system.md` C.6.2）。

---

## Alternatives Considered（替代方案）

### A. Unity `PlayerPrefs`
- **否決**：非結構化 key-value、無原子性/完整性保證、平台上限與可靠性差、易被清除；完全不符「永不丟失」承諾。

### B. 二進位序列化 (BinaryFormatter / 自訂 binary)
- **優點**：小、快。
- **缺點**：不可讀、版本遷移脆、`BinaryFormatter` 已棄用且不安全；debug/QA 難檢視。
- **否決**：JSON 可讀性利於 QA/遷移/debug；本遊戲存檔小，效能非瓶頸。

### C. 直接覆寫 `save.json`（無暫存改名）
- **否決**：寫入中途崩潰 → 半寫損毀檔，直接違反原子性與「永不丟失」。暫存改名是必需。

### D. 每次同步寫（不用背景執行緒）
- **缺點**：`on_part_break` 高頻，同步寫會在戰鬥中造成 I/O 卡頓/掉幀。
- **否決**：非同步背景寫 + suspend 同步安全網是正解。

---

## Consequences（後果）

### 正面
- 「永不丟失」承諾以原子寫入 + 即時入帳 + suspend 安全網技術落地。
- CRC32 + 備份 → 意外損毀可偵測可復原，不崩潰。
- JSON 可讀 → QA/debug/遷移友善。
- 版本遷移純函數鏈 → 未來 schema 演進安全。
- 背景非同步寫 → 戰鬥中無 I/O 卡頓。

### 負面 / 成本
- 需自寫 canonical 序列化器保證 CRC32 確定性（`JsonUtility` 未必給確定順序）**[需查證 6.3 API]**。
- 背景執行緒 + 深拷貝快照有實作複雜度（執行緒安全、關閉時 flush）。
- fsync/rename 的原子性保證跨平台（尤其行動/沙盒檔系統）需驗證 **[需查證 6.3 API]**。
- CRC32 不防篡改——明確接受（防作弊屬未來伺服器職責）。

### 效能意涵
- 存檔小（單槽、few KB 級）；序列化+CRC32 成本可忽略。
- 寫入在背景執行緒，主迴圈零阻塞（除 suspend/quit 同步網）。
- 深拷貝快照有短暫配置，但頻率低（事件觸發），非彈幕熱路徑。

---

## Reversibility（可逆性）
**中**：`ISaveService` 抽象隔離消費者。若換序列化器（JsonUtility→Newtonsoft）或加密/雲存，只動 `Meta` 內部；但存檔**格式**一旦有玩家資料在野，變更須走版本遷移鏈（這正是遷移機制存在的理由）。

---

*ADR 版本：1.0.0*
