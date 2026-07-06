# Story 004: 菁英怪生成 + 莢艙保底掉落追蹤

> **Epic**: 關卡系統與 Run 流程
> **Status**: ✅ Core complete (2026-07-06 — PodDropTracker 9/9 EditMode GREEN incl. 200-run guarantee; elite HP scaling in EnemyController). Elite-death event emission + elite density deferred (see reconciliations).
> **Layer**: Core
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-07-02
> **Last Updated**: 2026-07-06

## Context

**GDD**: `design/gdd/stage-system.md`
**Requirement**: `TR-stage-002`（莢艙保底）; `TR-stage-001`（菁英波次生成）
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0003: 資料驅動調校 (primary); ADR-0005: 專案結構與組件邊界 (secondary)
**ADR Decision Summary**: 菁英規格（HP 倍率、密度倍率、光環色、shard bonus）全存於 `EnemyConfig` SO；莢艙保底邏輯（`primary_spawned` / `secondary_spawned` 追蹤）由可注入的 `PodDropTracker` 純 C# 類別實作；`PodDropConfig` SO 提供保底數量旋鈕。系統間通訊經 `IEventBus`（`EliteKilled`、`PodSpawnRequested` 事件）。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: 菁英視覺標記（光環）為 SpriteRenderer / particle 層，不影響邏輯。`elite_hp_mult` 於 Instantiate 後注入 `EnemyController`。Unity 6.3 API 穩定。

**Control Manifest Rules (Feature — Stage)**:
- Required: `MUST` 莢艙保底掉落；Pre-Boss Lull 非同步預載 `kaiju_<id>` 期間保底仍生效
- Required: `MUST NOT` 引用其他 Feature 系統；`MUST NOT` 硬編碼 elite_hp_mult 等數值
- Guardrail: `PodDropTracker` 每 Run 重置；不在 SO 上儲存可變狀態（ADR-0003 唯讀原則）

---

## Acceptance Criteria

*From GDD `design/gdd/stage-system.md` §E.3, §F.3, §F.2.4, §L.2:*

- [ ] 菁英 Prefab（`EnemyDef.is_elite=true`）生成時：HP = 基礎 HP × `elite_hp_mult`（預設 2.5，來自 `EnemyConfig` SO）
- [ ] 菁英視覺標記：外圈暖琥珀像素光環（`elite_aura_color: #FFAA33`）、體型縮放 ×1.1（`elite_scale_mult`，`EnemyConfig` SO）
- [ ] 菁英死亡：發布 `EliteKilled{segmentId, podPoolPreference}` 事件（`podPoolPreference` 來自 `SegmentDef`）
- [ ] `PodDropTracker`（injectable，per-run）：追蹤 `PrimarySpawned`、`SecondarySpawned` bool；EliteKilled 觸發對應 bool 設 true
- [ ] `pod_pool_preference = "auto"`：`PodDropTracker` 選擇尚未 Spawn 的池類型（Primary 優先）
- [ ] 最後升階波段結束後，若 `PrimarySpawned=false` 或 `SecondarySpawned=false`：強制發布 `PodSpawnRequested{type=缺口類型, isGuaranteed=true}`
- [ ] 若最後升階波段無菁英（D1 早期波段）：段落結束時同樣觸發非菁英保底 Pod Spawn
- [ ] 前頭目喘息：永遠發布 `PodSpawnRequested`（補充缺口池；兩池均 Spawned → random）；計數 = `pre_boss_lull_pod_count`（SO）
- [ ] 菁英死亡額外給予 `elite_shard_bonus`（預設 +3）碎片：發布 `EliteShardsDropped{amount}` 供 Economy 訂閱
- [ ] 自動化測試：200 輪（各種隨機種子）——進入喘息前 `PrimarySpawned AND SecondarySpawned` 恆成立（`L.2` 阻斷標準）

---

## Implementation Notes

*Derived from ADR-0003 and ADR-0005 Implementation Guidelines:*

- `PodDropTracker` 放 `Assets/_Project/Scripts/Stage/PodDropTracker.cs`（純 C# 類別）。
- 建構子：`PodDropTracker(IEventBus bus, PodDropConfig config)`；每 Run 開始由 `RunController` / `App` 重建（確保狀態重置）。
- 訂閱：`bus.Subscribe<EliteKilled>(OnEliteKilled)` → 更新 `PrimarySpawned` / `SecondarySpawned`；決定 pool type；發布 `PodSpawnRequested{podType, spawnPosition, segmentId}`。
- `PodDropConfig` SO 欄位（`KaijuBreaker.Content`）：`guaranteed_primary_per_stage`、`guaranteed_secondary_per_stage`、`pre_boss_lull_pod_count`。
- 保底強制邏輯在 `RunController.OnLastSegmentEnded()` 觸發：呼叫 `podDropTracker.FlushGuaranteed()`——若缺口存在則發布強制 `PodSpawnRequested`。
- `PodSpawnRequested` 事件由 Story 005 的 `WeaponPodSpawner` 訂閱，實際 Spawn 物件。
- 菁英生成在 `WaveSpawner`（Story 002）處理；Story 004 只補充菁英死亡後的追蹤與保底邏輯。
- 視覺標記（光環）：菁英 Prefab 自帶 `EliteAuraController` 組件，從 `EnemyConfig.elite_aura_color` 設置；不在此 Story 客製化光環動畫（placeholder 可接受）。
- 難度菁英出場規則（§E.3.3）由 SegmentDef 波次設計控制（D1 早期段落 `elite_wave_index=-1`），不在執行期動態插入——運行邏輯不需知道難度閘門，由設計師在 SO 配置。

> **⚠️ BLOCKED（部分）**: 菁英 `EmitterPatternSO` 的 `elite_density_mult`（子彈密度加乘）依賴 BulletSim 執行期（ADR-0001 Proposed）。本 Story 僅覆蓋：菁英生成視覺 + HP + shard bonus + 莢艙保底追蹤。菁英密集彈幕整合列入 **depends-on-blocked**，待 ADR-0001 Accepted 後補完。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: 菁英 Prefab 實際 Instantiate（WaveSpawner 負責；本 Story 消費死亡事件）
- Story 005: Pod 物件的下降 / 循環 / 徘徊 / 拾取行為（本 Story 只發布 `PodSpawnRequested`）
- Story 006: 前頭目喘息計時（RunController 觸發喘息後，本 Story 的追蹤器補充保底 Pod）
- **菁英子彈密度（`elite_density_mult` 執行期）**: depends-on-blocked: ADR-0001

---

## QA Test Cases

*Integration story — PlayMode 自動化測試規格：*

**AC-1**: 菁英死亡觸發 Pod 追蹤 + 事件
  - Given: `PodDropTracker` 初始化（Primary=false, Secondary=false）；`SegmentDef.pod_pool_preference="primary"`
  - When: 發布 `EliteKilled{podPoolPreference=primary}`
  - Then: `PodDropTracker.PrimarySpawned=true`；`PodSpawnRequested{podType=Primary}` 被發布 1 次
  - Edge cases: 同類型第二個 `EliteKilled` 不再重複發布同類型 Pod（已 Spawned）

**AC-2**: `auto` 偏好選擇缺口池
  - Given: `PrimarySpawned=true, SecondarySpawned=false`
  - When: `EliteKilled{podPoolPreference=auto}`
  - Then: `PodSpawnRequested{podType=Secondary}` 被發布（補缺口）
  - Edge cases: 兩池均 Spawned + auto → `PodSpawnRequested{podType=Random}`

**AC-3**: 最後段落結束保底強制
  - Given: 最後升階段落結束；`SecondarySpawned=false`
  - When: `podDropTracker.FlushGuaranteed()` 呼叫
  - Then: `PodSpawnRequested{podType=Secondary, isGuaranteed=true}` 發布
  - Edge cases: 兩池均已 Spawned → FlushGuaranteed 不發布額外事件

**AC-4**: 200 輪保底統計覆蓋
  - Given: Stage 1 配置（pool=5，draw=3）；PodDropTracker per-run 重置；種子 0–199
  - When: 完整模擬 N 段落 + 段落結束強制邏輯
  - Then: 全 200 輪在 FlushGuaranteed 後 `PrimarySpawned AND SecondarySpawned == true`

**AC-5**: 前頭目喘息保底 Pod
  - Given: 兩池均已 Spawned（PrimarySpawned=true, SecondarySpawned=true）
  - When: 喘息開始，`PodDropTracker.SpawnPreBossLullPods()` 呼叫
  - Then: 發布 1 個 `PodSpawnRequested{podType=Random}`（`pre_boss_lull_pod_count=1`）
  - Edge cases: `pre_boss_lull_pod_count=2` 時發布 2 個事件

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/PlayMode/Stage/weapon_pod_guarantee_test.cs` — PlayMode 整合測試，必須全部通過 【BLOCKING】

**Status**: [x] ✅ `Assets/_Project/Tests/EditMode/Stage/pod_drop_tracker_test.cs` 9/9 GREEN (Unity MCP, 2026-07-06). Covers AC-1 (elite→primary pod + dedupe), AC-2 (auto fills gap / random when both covered), AC-3 (FlushGuaranteed forces missing pool, no-op when covered), AC-4 (200-seed guarantee holds after flush), AC-5 (pre-boss lull random / count=2 / gap-first).

**Reconciliations vs story text** (surfaced for review):
1. **`PodDropTracker` is pure event-driven C#** → EditMode Logic test (story labelled PlayMode, but nothing touches the scene). New Core events `EliteKilled` / `PodSpawnRequested` / `EliteShardsDropped` + `PodType`/`PodPoolPreference` enums; new `SegmentDef.PodPoolPreference` field (the SO lacked it). `PodDropConfig` already had the guarantee/lull count knobs.
2. **Elite specs live on `EnemyDef`** (EliteHpMult/EliteAuraColor/EliteShardBonus), not a separate `EnemyConfig` — `EnemyController.Init` now scales elite HP by `EliteHpMult` (`ceil`).
3. **Deferred (documented):** (a) emitting `EliteKilled`/`EliteShardsDropped` on *actual* elite death needs a combat/damage system — enemies don't take damage yet (bullets blocked by ADR-0001); the tracker is driven by the event directly, exactly as the ACs specify. (b) `elite_scale_mult` (×1.1 body scale) has no committed config field → skipped (no hardcode). (c) `elite_density_mult` bullet density → ADR-0001. (d) `Random` pod-type resolution at spawn is Story 005's.

---

## Dependencies

- Depends on: Story 003（`SegmentDef` 的 `pod_pool_preference` / `elite_wave_index` 由 SegmentRecombinator 組裝輸出）
- Depends on: Story 002（`WaveSpawner` 生成菁英實例，菁英死亡後發布 `EliteKilled`）
- Depends on: content-config epic（`PodDropConfig` SO、`EnemyConfig` SO 含 `elite_hp_mult`、`elite_aura_color`、`elite_shard_bonus`）
- **depends-on-blocked**: ADR-0001（BulletSim）— 菁英 `elite_density_mult` 執行期整合
- Unlocks: Story 005（`PodSpawnRequested` 事件供 WeaponPodSpawner 消費）；Story 006（喘息保底 Pod 在 RunController 控制下觸發）
- Note: economy epic（`EliteShardsDropped` 事件 → Economy 計算 shard 入帳）
