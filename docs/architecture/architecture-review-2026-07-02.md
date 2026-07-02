# 架構複審報告 (Architecture Review Report) — 殲獸戰機 / KAIJU BREAKER

*文件路徑：docs/architecture/architecture-review-2026-07-02.md*
*日期：2026-07-02*
*引擎：Unity 6.3 LTS（C#）*
*作者：Technical Director Agent（`/architecture-review full` + `rtm` reconciliation）*
*輸入：11 GDD（+ game-concept + 3 kaiju）｜6 ADR｜architecture.md｜control-manifest.md｜13 EPIC.md｜97 story 檔*

---

## 裁決 (Verdict)：**CONCERNS**

**無阻斷問題 (No blocking issues)。** 所有 Foundation / Core 層需求皆由 **Accepted** ADR 覆蓋；無新的跨-ADR 衝突；引擎版本一致（Unity 6.3）。CONCERNS 來自可修復的**文件陳舊**（architecture.md 與 control-manifest.md 未隨 ADR-0006 更新）與兩個先前未登記的 TR-ID（本次已形式化）。這些不阻擋 Pre-Production 推進，但應在下一輪 doc-sync 修正。

---

## 追溯性摘要 (Traceability Summary)

| 指標 | 數值 |
|------|------|
| 總需求數 (registry 條目) | **95** |
| ✅ 由 Accepted ADR 覆蓋 | 66 |
| ⚠️ 由 ADR-0001（Proposed）覆蓋 | 6（TR-bullet-001~006；kaiju-009 部分）|
| 🟦 design-only（合法無需 ADR：playtest/主觀/內容驗收） | 23 |
| ❌ 需 ADR 卻無（真缺口） | **0** |
| 每個 ADR 至少覆蓋 1 需求（無孤兒 ADR） | ✅ 6/6 |
| 每個 GDD 系統有對應 epic | ✅ S1–S10 + C1 全覆蓋 |
| 每個 epic 對應到程式模組 | ✅ 13/13 |

**registry ↔ epics/stories 對帳：** 97 story 檔引用的每個 TR-ID 都能對應到 registry 條目；反之 EPIC.md 表格的每個 TR-ID 都已登記。無「story 引用但無需求」的錯配（唯二例外 TR-ui-009/010 於下方處理）。

---

## 1. 需求形式化與對帳結果 (TR Registry Formalization)

- `docs/architecture/tr-registry.yaml` 由空白模板 → **95 條 active 需求**，version 1 → 2。
- ID 來源：13 份 EPIC.md 的 GDD-Requirements 表（本身由各 GDD 的 §H/§I/§L/§M/§10/§11 驗收標準推導）。
- **完全沿用 epics/stories 既有 ID**，零重新編號。系統 slug、序號與現有 story 引用一致。

### 已解決的兩個 placeholder（非錯配，屬待形式化）
`hud-ui` epic 的 story-006、story-007 明文寫「add TR-ui-009 / TR-ui-010 when registry is formalized」——它們是 UIScreenManager 基礎設施與 Loadout 畫面，屬 ADR-0006 §3 涵蓋但無獨立 M.x AC 的架構基礎組件。**本次已登記為 TR-ui-009 / TR-ui-010（active）**，並在 registry 註記其來源。這兩個 story 檔的 `TR-ui-???` placeholder 現可回填正式 ID（建議由 `/create-stories` 或手動 doc-sync 處理，非阻斷）。

---

## 2. 覆蓋缺口 (Coverage Gaps)

**需 ADR 卻無 ADR 的真缺口：0。** 所有 ❌ 標記經逐一核實，皆為以下兩類合法情形：

**(a) design-only（playtest / 主觀 / 內容 / 關卡評審驗收）— 無需架構決策：**
TR-economy-005｜TR-difficulty-006/007｜TR-stage-004/005/006｜TR-weapon-006/008｜TR-kaiju-001/005/006｜TR-input-002/003/004/006（跨方案可玩性 playtest）等 23 項。這些的**基礎設施**（Input 模組結構、EconomyConfig、可讀性護欄）已分別由 ADR-0005/0003/0001 承載；未達標的是「達成率/手感/曲線」等只能由 playtest 判定的目標，登記為 design-only 是正確的。

**(b) 原型驗證閘門 (prototype gate) — 已知、刻意：**
- TR-bullet-001/002（1,000@60fps、0 GC）→ ADR-0001 Proposed，待效能原型 LOCK。
- **TR-input-001（觸控手感）→ 與 bullet-001 同性質的阻斷級原型閘門**，但目前僅標 design-only。architecture.md §9 已將「觸控彈幕手感未驗證」列為 HIGH 風險並指定原型閘門緩解。**觀察（非阻斷）：** 觸控閘門的技術地位與 ADR-0001 對等，但沒有等價的 ADR 或風險條目 ID 綁定驗收數字。建議在 vertical-slice 前，比照 bullet-sim 為觸控原型 spike（input story-001 已存在）明確標記為「gate」而非普通 design-only，以免其阻斷性在排程中被淡化。

---

## 3. 跨-ADR 衝突偵測 (Cross-ADR Conflict Detection)

**無新增衝突。** 資料所有權清晰且三處文件一致：
- `break_quality` 由 **KaijuParts** 於破壞幀計算；`shard/core yield` 由 **Economy** 自算（payload 不含產量）；**Meta** 讀結果入帳。此鏈在 ADR-0002、architecture.md §5.2/§10.5、control-manifest §3/§4.2 完全對齊，無所有權雙claim。
- 難度乘數唯一擁有者 = `DifficultyConfig`（ADR-0003 / TR-content-004 / TR-difficulty-*），其他系統經 `IDifficultyProvider` 唯讀，無複製衝突。

control-manifest §6 已記錄 5 條既有來源分歧（事件前綴、測試路徑、常數命名、ADR-0001 狀態、draw call 數字），皆附裁決，屬**已管理**非未解衝突。

### ADR 依賴排序 (Dependency Order) — 無環
```
Foundation（無阻斷前置）：ADR-0005（結構）｜ADR-0003（SO 資料）｜ADR-0002（事件）
依賴 Foundation：       ADR-0004（存檔；需 0002/0003）
Feature/旗艦：          ADR-0001（彈幕；需 0002/0003/0005）— Status: Proposed（效能閘門）
Presentation：          ADR-0006（UI；需 0001/0002/0003/0005）— Accepted
```
**注意（非阻斷）：** ADR-0006 的 `相關` 引用 ADR-0001（Proposed），但其依賴僅為方向性約束（「Canvas 不進彈幕播放區」——實為對 ADR-0001 渲染路徑的**緩解**，以 SpriteRenderer 部位血條規避）。ADR-0006 的 Accepted 不受 ADR-0001 閘門阻擋，無依賴環。

---

## 4. 引擎相容性 (Engine Compatibility) — Unity 6.3

- **版本一致：** 6/6 ADR 一致以 Unity 6.3 LTS / DOTS Entities 1.3+ 為目標，無陳舊版本引用。
- **`[需查證 6.3 API]` 旗標：** 已知且分散於 ADR-0001（Entities 時間注入/Blob 烘焙/World 生命週期）、ADR-0004（`JsonUtility` dictionary/canonical、fsync/rename、`OnApplicationPause/Quit` 平台保證）、ADR-0006（`Canvas.pixelPerfect`+PixelPerfectCamera、URP 2D sorting、TMP 像素字型）、game-feel（ECS timeScale 凍結）。全部有明確「實作前查證、不臆造簽名」紀律，並要求記錄於 `docs/architecture/tech-spikes/`。
- **無棄用 API 引用；無跨-ADR 對同一 post-cutoff API 的矛盾假設。**
- **一致性觀察（非阻斷）：** `time_scale=0` 凍結敵彈的 ECS 時間注入 [需查證] 同時被 ADR-0001（BulletSim）與 game-feel epic（TR-gamefeel-002）依賴——屬**單一** spike 需同時滿足兩方，建議該 spike 明確列為兩者共用驗收，避免各自假設分歧。

### 沒有 Engine Compatibility 章節的 ADR
ADR 採用敘事式 `Consequences/效能意涵` 而非固定「Engine Compatibility」小節，但每份都以 `[需查證 6.3 API]` 內嵌標記引擎盲點，實質等價。無盲區。

---

## 5. GDD 修訂旗標 (GDD Revision Flags)

**無。** 未發現任何 GDD 假設與已驗證引擎行為或 Accepted ADR 相牴觸。所有 `[需查證]` 屬「待查證」而非「已知牴觸」，故不觸發 GDD 回饋修訂。

---

## 6. 架構文件覆蓋 (Architecture Document Coverage) — 陳舊發現（CONCERNS 來源）

ADR-0006（UI 框架，Accepted 2026-07-02）寫成後，**上游總藍圖與控制清單未同步**：

| 文件 | 陳舊處 | 建議修正 |
|------|--------|---------|
| `architecture.md` §1 技術棧 | 「UI 框架 UGUI — 決議見 §6.1 待辦」 | 改為「由 ADR-0006 定案（三層分治）」 |
| `architecture.md` §6.1 | 「待辦決議（非 MVP 阻斷）：HUD UGUI vs UI Toolkit…需補一份 UI ADR」 | 移除待辦，指向 ADR-0006 |
| `architecture.md` §8 追溯矩陣 S7 列 | 治理 ADR 標「ADR-0002、UI ADR（待補）」 | 改為「ADR-0002、ADR-0006」 |
| `architecture.md` §10 開放問題 #2 | 「HUD 框架：UGUI vs UI Toolkit？暫定 UGUI」 | 標為 RESOLVED（ADR-0006）|
| `architecture.md` 頁尾 `關聯 ADR` | 列 0001–0005，**漏 0006** | 補上 0006 |
| `control-manifest.md` §0 | Accepted 清單列 0002/0003/0004/0005，**漏 0006** | 補 ADR-0006 = Accepted |
| `control-manifest.md` §3 `UI` 段 | 「框架暫定 UGUI（UI ADR 待補）」 | 改為「依 ADR-0006」 |

這些是**文件落後於決策**，非架構缺陷——ADR-0006 本身完整且 hud-ui epic 已正確引用它。屬非阻斷 doc-sync 工作。

---

## 阻斷問題 (Blocking Issues)
**無。** 可推進 Pre-Production 閘門。

## 非阻斷問題 (Non-Blocking — 建議在 doc-sync / 下一輪處理)
1. **doc-sync：** architecture.md + control-manifest.md 補齊 ADR-0006 引用（§6 表）。
2. **回填 TR-ID：** hud-ui story-006/007 的 `TR-ui-???` placeholder 回填為 TR-ui-009/010。
3. **觸控閘門地位：** TR-input-001 明確標為 prototype gate（比照 TR-bullet-001），避免阻斷性被淡化。
4. **共用 spike：** ECS `timeScale=0` 凍結敵彈的 [需查證] spike 明列為 ADR-0001 與 TR-gamefeel-002 共用驗收。

## 需建立的 ADR (Required ADRs)
**無新增。** 現有 6 份 ADR 覆蓋 MVP 全部需要架構決策的需求。ADR-0001 仍需由效能原型閘門推進 Proposed → Accepted（非新 ADR，屬狀態轉換）。

---

## 交接 (Handoff)

- **即時行動 Top 3：** (1) 效能原型 spike（bullet-sim story-001）驅動 ADR-0001 LOCK；(2) 觸控手感 spike（input story-001）；(3) doc-sync 補 ADR-0006 引用。
- **Pre-gate 檢查：** 進 `/gate-check pre-production` 前確認 `tests/` 骨架、CI workflow、UX/accessibility 文件狀態（本報告未逐一 Glob 驗證，交由 gate-check）。
- **重跑觸發：** ADR-0001 轉 Accepted 後、或新增任何 ADR 後，重跑 `/architecture-review` 驗證覆蓋。

---

*報告版本：1.0.0 — registry v2 已寫入 `docs/architecture/tr-registry.yaml`（95 條 active）*
