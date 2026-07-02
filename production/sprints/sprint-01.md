# Sprint 1 — 2026-07-02 to 2026-07-22 (3 週 / 15 工作日)

> 殲獸戰機 / KAIJU BREAKER — Pre-Production 首個實作衝刺
> 引擎：Unity 6.3 LTS (C#)｜Review mode: `lean`（PR-SPRINT gate 依 skill 略過）
> **DRAFT** — 由 producer 依 director 預先授權自主產出，待 director 精修。

## Sprint Goal

站起引擎無關的 **Foundation 骨幹**（Core 事件匯流排 + DI + 唯讀查詢介面、Content ScriptableObject 框架），使所有 Core 系統可對真實基礎設施開發；同時以兩個 spike 解決兩個最高風險閘門（ADR-0001 彈幕效能、觸控手感），解鎖下游大量被阻擋的工作。

## Capacity

> **無 velocity 歷史 —— 以下為明確假設，需 director 確認。未虛構團隊。**

- **假設**：1 名 C# 開發者（引擎無關工作）＋ director 可在其 Unity 機器上兼職執行兩個 spike（需編輯器/裝置，開發者環境尚在裝套件）。
- **Sprint 長度**：3 週 = 15 工作日（刻意超過 2 週；理由見下）。
- **C# 開發容量**：15 日 − 20% buffer (3 日) = **12 可用 dev-日**。
- **Spike 容量（director）**：兩個 timeboxed spike，各 ≤3 日，於 sprint 前 1/3 平行執行，**不佔用 C# 開發者的 12 日**。
- **為何 3 週而非 2 週**：完整 Foundation（core-foundation 6 + content-config 9）≈ 12 dev-日，正好填滿 2 週的無-buffer 容量；加上 20% buffer 需 3 週。若 director 確認為純單人（開發者＝spike 執行者），則 spike 與 C# 工作競爭同一人力，**2 週內無法同時完成兩者** → 需砍 content-config 後段或延長。**此為第一號待確認項。**

## Tasks

### Must Have (Critical Path)

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|--------------|---------------------|
| CF-001 | Core `.asmdef` 建立與共用型別定義 (WeaponId/PartType/BreakQuality/DifficultyTier/Run 狀態列舉) | unity-cs-dev | 0.5 | — | `.asmdef` 編譯；共用型別集中於 Core；EditMode 測試通過 (TR-core-004/006) |
| CF-002 | `IEventBus` 介面 + GDD 事件 readonly struct 定義 | unity-cs-dev | 0.5 | CF-001 | 全 GDD 事件契約以 readonly struct 表達；`Publish<T>(in T)`/`Subscribe<T>` 簽名定義 (TR-core-001/002) |
| CF-003 | 唯讀查詢介面定義 (IPartStateQuery/IDifficultyProvider/ISaveService/IWeaponTierQuery) | unity-cs-dev | 0.5 | CF-001 | 介面於 Core；可注入假實作供測試 (TR-core-003) |
| CF-004 | EventBus 具體實作（同步同幀分發 + 穩態零 GC） | unity-cs-dev | 1.5 | CF-002 | 單元測試證同步分發順序；穩態 0 GC alloc 斷言通過 (TR-core-001/002) |
| CF-005 | DOTS↔Mono Bridge 值型 struct 合約 | unity-cs-dev | 0.5 | CF-002 | Bridge HitEvent 值型合約定義；不含引擎依賴 (TR-core-002) |
| CF-006 | App 組合根 DI 佈線合約 | unity-cs-dev | 1.0 | CF-003, CF-004 | App 為唯一組合根；無持狀態 static 單例；整合測試佈線通過 (TR-core-005) |
| SPK-BULLET | **Spike:** DOTS/ECS 彈幕效能原型（手機基準機 1,000@60fps + 零 GC） | director (Unity 機) | 3.0 (timebox) | Unity 編輯器 + 基準裝置 | 於基準機測得結果並記錄至 `docs/architecture/tech-spikes/`；**LOCKs ADR-0001 Proposed→Accepted**（達標）或觸發後端重評（未達標） |
| SPK-INPUT | **Spike:** 觸控手感原型（遮蔽解決、彈幕閃避可行、L3 觸控可行） | director (Unity 機) | 2.5 (timebox) | Unity 編輯器 + 觸控裝置 | 觸控手感結論記錄至 `docs/architecture/tech-spikes/`；判定 pre-MVP 觸控閘門通過/需迭代 (TR-input-001) |

### Should Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|--------------|---------------------|
| CC-001 | WeaponBalanceConfig + WeaponDef SO | unity-cs-dev | 1.0 | CF-001 | SO 定義 + OnValidate 範圍斷言；工廠/fixture 可注入 (TR-content-001/002/005) |
| CC-003 | DifficultyConfig SO | unity-cs-dev | 0.5 | CF-001 | 難度乘數唯一權威來源 SO；執行期唯讀 (TR-content-004) |
| CC-008 | ContentRegistry 服務（載入/索引 SO） | unity-cs-dev | 1.0 | CC-001, CC-003 | 執行期唯讀載入；整合測試通過 (TR-content-001/003) |
| CC-009 | SO 測試 fixture 支援 | unity-cs-dev | 0.5 | CC-008 | 工廠/`.asset` fixture；無行內魔數 (TR-content-005) |
| CC-002 | PartSystemConfig + KaijuDef SO | unity-cs-dev | 1.0 | CF-001 | SO 定義 + OnValidate (TR-content-002) |
| CC-004 | GameFeelConfig SO | unity-cs-dev | 0.5 | CF-001 | SO 定義 (TR-content-002) |
| CC-005 | EmitterPatternSO + MovementPatternSO + EnemyDef | unity-cs-dev | 1.0 | CF-001 | 撰寫層 SO 定義；為 bullet-sim 烘焙預備 (TR-content-002) |
| CC-006 | StageDef + SegmentDef + PodDropConfig SO | unity-cs-dev | 1.0 | CF-001 | SO 定義 (TR-content-002) |
| CC-007 | EconomyConfig + InputSettings + SaveConfig SO | unity-cs-dev | 1.0 | CF-001 | SO 定義 (TR-content-002) |

### Nice to Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|--------------|---------------------|
| DF-001 | DifficultyConfig SO + Core 基礎型別與介面 | unity-cs-dev | 0.5 | CF-003, CC-003 | ⚠️ 疑與 CC-003 重疊 —— 見 Notes/待確認項 |
| DF-002 | DifficultySystem 實作 + 運行期乘數套用 | unity-cs-dev | 1.0 | DF-001, CF-004 | IDifficultyProvider 供 Stage/BulletSim 唯讀；整合測試 (TR-difficulty-001) |

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|--------------|
| — | 首個 sprint，無 carryover | — |

## Excluded (刻意不排入 Sprint 1)

- **bullet-sim story 002–009（8 個 Blocked）** —— 依 ADR-0001 LOCK；解鎖條件 = SPK-BULLET DONE。
- **kaiju-roster 3 個 Blocked story** —— 依 ADR-0001 LOCK。
- **Core/Feature/Presentation 玩法 story**（weapons/kaiju-parts/economy/stage/hud-ui/game-feel/meta 實作）—— 排入後續 sprint，依賴本 sprint 的 Foundation。

## Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| ADR-0001 未於 SPK-BULLET LOCK（效能未達 1,000@60fps） | Medium | High — 阻擋 8 bullet + 3 kaiju story，衝擊整條旗艦路徑 | Spike 排在 sprint 最前；timebox 3 日；未達標即觸發 director + technical-director 後端重評，勿讓其滑到 sprint 末 |
| 觸控手感 spike 結論為「不可行/需重大迭代」 | Medium | High — 觸控為主要平台，pre-MVP 阻斷 | 早排；若未過，記錄具體阻礙供 game-designer/creative-director 決策，不阻擋 C# Foundation 進度 |
| 單人容量假設錯誤（開發者＝spike 執行者） | Medium | Medium — 2 週內無法同時完成 spike + 全 Foundation | 待 director 確認人力模型；若單人則砍 CC-002/004/005/006/007 至 Sprint 2 |
| Unity 6.3 環境未就緒（director 仍在裝套件） | High | Medium — 阻擋兩個 spike 起跑 | C# Foundation (CF/CC) 引擎無關可先行；spike 待環境就緒即啟動；追蹤為外部依賴 |
| `[需查證 6.3 API]` 盲點（Entities 1.3 時間注入/Blob、Input System API） | Medium | Medium — spike 中觸雷 | spike 紀律：實作前查證 `docs/engine-reference/unity/`，不臆造簽名；結果記 tech-spikes/ |

## Dependencies on External Factors

- **Unity 6.3 編輯器 + 套件安裝完成**（director 進行中）—— SPK-BULLET / SPK-INPUT 的硬前置。
- **手機基準裝置** —— SPK-BULLET 效能量測與 SPK-INPUT 觸控量測需真機（非 headless）。
- **CI gate**（`.github/workflows/ci-tests.yml`）—— 所有 Logic/Integration story（CF-*、CC-008/009、DF-*）須帶通過的單元/整合測試方可合併，依 coding-standards「測試為阻斷閘門」。Config/Data story（CC-001~007）走 smoke check（ADVISORY）。

## Definition of Done for this Sprint

- [ ] 所有 Must Have 任務完成（CF-001~006 + 兩個 spike 結論已記錄）
- [ ] SPK-BULLET 已在基準機量測，ADR-0001 完成 Proposed→Accepted 或觸發重評決策
- [ ] SPK-INPUT 觸控手感結論已記錄並交 creative/design 決策
- [ ] 所有任務通過驗收標準
- [ ] QA plan 存在（`production/qa/qa-plan-sprint-1.md`）
- [ ] 所有 Logic/Integration story 有通過的單元/整合測試（CI 綠燈）
- [ ] Smoke check 通過（`/smoke-check sprint`）
- [ ] QA 簽核：APPROVED 或 APPROVED WITH CONDITIONS
- [ ] 無 S1/S2 bug
- [ ] 設計文件依任何偏差更新
- [ ] Code reviewed 並合併

## Notes / 待 director 確認項

1. **人力模型**（最高優先）：單人（開發者亦執行 spike）還是「開發者 + director 兼職跑 spike」？決定 spike 是否與 C# 工作競爭同一人力，進而決定 2 週 vs 3 週、以及 content-config 後段是否需延後。
2. **DF-001 vs CC-003 重疊**：difficulty story-001「DifficultyConfig SO + Core 基礎型別」與 content-config story-003「DifficultyConfig SO」疑似重複載體。建議二選一為權威（傾向 CC-003 於 Content 模組），DF-001 收斂為僅 Core 型別/介面或直接併入 DF-002。請 director 裁決以免雙 claim。
3. **Sprint 長度**：草案採 3 週（含 buffer）；若偏好 2 週節奏，建議將 CC-002/004/005/006/007（非解鎖關鍵路徑的 SO）移至 Sprint 2。
4. **Owner 佔位**：`unity-cs-dev` 為引擎 C# 開發角色佔位，`director` 為 spike 執行者佔位；請以實際 agent/人員替換。

> **Scope check:** 本 sprint 未新增 epic 範圍外 story（全數取自既有 backlog）。如後續加入額外 story，執行 `/scope-check [epic]`。
