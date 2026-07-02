# Story 008: Laser Tier-3 Unique Mechanics (L1/L2/L3/L4)

> **Epic**: 武器系統
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: [fill before sprint planning]
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/weapon-system.md`
**Requirement**: `TR-weapon-007`
*(TR-IDs inferred from GDD §H — tr-registry.yaml not yet populated)*

**ADR Governing Implementation**: ADR-0002: 事件架構與系統間通訊 (primary); ADR-0003: 資料驅動調校 (secondary)
**ADR Decision Summary**: 所有 Tier-3 觸發均經 `IWeaponTierQuery.GetTier(weaponId) == 3` 門檻後啟動；L2 Tier-3 接收 `on_part_break`（訂閱 `PartBroke`）並發 `LaserHit` 至相鄰部位；L1/L4 殘熱 / 熱殘影以獨立計時器 per-part 管理（每幀持續發 `LaserHit`）；L3 Tier-3 共鳴擴散在蓄力釋放同幀額外注入熱量；所有倍率旋鈕從 `WeaponDef.TierKnobs[3]` 讀取。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: L1/L4 殘熱計時器需 per-part 實例（`Dictionary<PartId, float>` 或等效結構），避免 heap alloc 建議用固定大小陣列或 pool。`PartBroke` 事件 payload 含 `adjacency_list`（GDD F.1 / ADR-0002 §3），L2 Tier-3 直接讀取，無需回查 `KaijuParts`。

**Control Manifest Rules (this layer)**:
- Required: MUST 所有 Tier-3 效果以 `IWeaponTierQuery.GetTier(weaponId) == 3` 門檻啟動 (§3 Weapons)
- Required: MUST L2 Tier-3 接收 `PartBroke` 後僅發 `LaserHit` 至相鄰部位，不改部位狀態 (§3 Weapons)
- Required: MUST 殘熱 / 熱殘影計時器以 `Time.deltaTime` 累積，per-part 獨立計時 (§1.7)
- Required: MUST 所有 Tier-3 旋鈕（倍率 / 持續時間 / 百分比）從 `WeaponDef.TierKnobs[3]` 讀取 (§1.2)
- Forbidden: MUST NOT 硬編碼 1.5s / 40% / 30% / 50% 等 Tier-3 旋鈕值 (§1.2)
- Forbidden: MUST NOT 在接收 `PartBroke` 後修改部位狀態（純輸出層行為）(§3 Weapons)
- Guardrail: E.6 多重殘熱疊加規則——L1 殘熱焰 + L4 熱殘影同時作用於同一部位時，取最高 H_rate，不相加 (§1.2 → GDD E.6)

---

## Acceptance Criteria

*From GDD `design/gdd/weapon-system.md` §C.4 Tier-3 欄位 / §E.6 / §H.7，scoped to this story:*

- [ ] **L1 T3 全幅掃蕩**：`IWeaponTierQuery.GetTier(L1) == 3` 時波束數從 3 擴充為 4（`l1_t3_beam_count`）；任一束命中部位後啟動 `l1_t3_residual_duration`（1.5s）殘熱焰計時器，計時中持續每幀發 `LaserHit(heat_delta = l1_t3_residual_rate_mult × l1_h_rate_full × deltaTime)`，即使玩家移開仍持續
- [ ] **L2 T3 破點漣漪**：`IPartStateQuery.GetHeatPct(partId) >= l2_t3_autotrack_heat_pct`（80%）時啟動 ±`l2_t3_autotrack_range_px` 微追蹤；接收 `PartBroke` 事件後，對 `adjacency_list` 中每個相鄰 partId 發布 `LaserHit(heat_delta = l2_t3_adjacent_heat_pct × H_max_normal)`（30% 熱量脈衝）
- [ ] **L3 T3 共鳴擴散**：蓄力震波命中（charge 釋放事件）同幀額外發布一次 `LaserHit(heat_delta = l3_t3_heat_inject_pct × H_max_normal)`（50% 熱量即時注入），與原本的蓄力 `LaserHit` 分開發布
- [ ] **L4 T4 熱殘影**：pierce 命中部位後啟動 `l4_t3_afterimage_duration`（2s）熱殘影計時器，每幀發 `LaserHit(heat_delta = l4_t3_afterimage_rate_mult × l4_h_rate × deltaTime)`（40%）；per-part 獨立計時
- [ ] **E.6 疊加規則**：L1 殘熱焰與 L4 熱殘影同時作用於同一部位時，每幀只發一次 `LaserHit`，`heat_delta = max(l1_residual_rate, l4_afterimage_rate) × deltaTime`（取最高，不相加）
- [ ] `IWeaponTierQuery.GetTier() < 3` 時所有 Tier-3 路徑不啟動（Tier-0 到 2 行為與 Story 004/005 一致）

---

## Implementation Notes

*Derived from ADR-0002 §1–§2 and GDD §C.4 / §E.6:*

- **per-part 殘熱計時器**：建議以固定大小 `float[]`（大小 = 最大部位數，從 `KaijuDef.partCount` 或固定常數）避免 Dictionary 的 GC 壓力。計時器為負數或 0 代表未啟動；每幀 `timer[partIndex] -= deltaTime`，> 0 時持續發 `LaserHit`。
- **L2 Tier-3 `PartBroke` 訂閱**：在 `L2FocusBeam.OnEnable` 訂閱 `PartBroke`，在 `OnDisable` 取消。handler 需 check `IWeaponTierQuery.GetTier(WeaponId.L2) == 3` 才執行漣漪邏輯（基底 handler 只清碰撞體）。
- **L2 微追蹤**：每幀 `GetHeatPct(currentTargetPartId)` 呼叫；>`l2_t3_autotrack_heat_pct` 時 aim point 以 clamp 方式偏移 ±`l2_t3_autotrack_range_px` 像素，跟隨目標中心。仍以硬截止判定（不是擴大判定框）。
- **L3 T3 注入時機**：共鳴擴散與蓄力 `LaserHit` 在同一幀發布（先發原本的蓄力 heat，再發額外的即時注入 heat），確保同幀語義（ADR-0002 §1）。
- **H.7 自動化測試**：Story 002 的 `weapons_tier3_identity_depth_test.cs` 覆蓋 TTB ≤ 15% 縮短的量測。本 story 只需確保行為正確；H.7 數值驗證在 Story 002 完成後自動通過。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 004: L1/L2/L4 Tier-0 到 Tier-2 基礎發射行為
- Story 005: L3 Tier-0 到 Tier-2 雙模式（本 story 只加 Tier-3 路徑）
- Story 009: M1/M2/M3/M4 Tier-3
- GameFeel/UI: 殘熱焰 / 熱殘影視覺特效

---

## QA Test Cases

*Integration story — automated test specs.*

**Test: AC-1 — L1 T3 殘熱焰在 1.5s 內持續發 LaserHit**
- Given: L1 fixture（`l1_t3_residual_rate_mult=0.40f`, `l1_h_rate_full=25.0f`, `l1_t3_residual_duration=1.5f`）；IWeaponTierQuery stub（GetTier = 3）；fake bus spy
- When: L1 波束命中 partId=1；隨後 30 次 `UpdateFrame(deltaTime=0.05f)`（= 1.5s）
- Then: spy 在 1.5s 內每幀收到 `LaserHit(PartId=1, HeatDelta ≈ 0.40f * 25f * 0.05f = 0.5f)`；1.5s 後無新事件（計時器歸零）
- Edge cases: 計時器未歸零時再次命中同部位（計時器重置到 1.5s，不累加）

**Test: AC-2 — L2 T3 漣漪在 PartBroke 後發 LaserHit 至相鄰部位**
- Given: L2 fixture（`l2_t3_adjacent_heat_pct=0.30f`）；WeaponBalanceConfig（H_max_normal=100f）；IWeaponTierQuery stub（GetTier = 3）；fake bus spy；`PartBroke` 事件 payload（adjacency_list=[partId=2, partId=3]）
- When: 透過 fake bus 發布 `PartBroke(PartId=1, adjacency_list=[2,3])`
- Then: spy 收到 2 次 `LaserHit`（PartId=2, HeatDelta=30f；PartId=3, HeatDelta=30f）
- Edge cases: Tier < 3 時接收 PartBroke → spy 無 LaserHit（只有基底 clear collider）

**Test: AC-3 — L3 T3 共鳴擴散在蓄力釋放同幀發兩次 LaserHit**
- Given: L3 fixture（`l3_t3_heat_inject_pct=0.50f`）；WeaponBalanceConfig（H_max_normal=100f）；IWeaponTierQuery stub（GetTier = 3）；fake bus spy；hold 1.5s 後釋放
- When: L3 蓄力釋放
- Then: spy 收到 2 次 `LaserHit` 針對同 partId：第 1 次為蓄力 heat（l3_charge_h_rate × charge_time），第 2 次為 `HeatDelta=50f`（即時注入）；兩者在同一 Update 呼叫內
- Edge cases: Tier 0/1/2 → 僅 1 次 LaserHit（無注入）

**Test: AC-4 — E.6 L1 殘熱 + L4 熱殘影同時取最大，不疊加**
- Given: L1 fixture（residual_rate = 0.40f × 25 = 10 HU/s）；L4 fixture（afterimage_rate = 0.40f × 25 = 10 HU/s）；兩者均同時作用於 partId=1；deltaTime=0.016f
- When: `UpdateFrame(deltaTime=0.016f)`（L1 殘熱計時器和 L4 殘影計時器均 > 0）
- Then: spy 對 partId=1 收到恰好 1 次 `LaserHit`，`HeatDelta = max(10,10) * 0.016f = 0.16f`（不是 0.32f）
- Edge cases: L1 rate ≠ L4 rate → 取較大者

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/Weapons/weapons_laser_tier3_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (WeaponDef Tier-3 旋鈕欄位), Story 003 (IWeaponTierQuery 佈線), Story 004 (L1/L2/L4 基礎行為), Story 005 (L3 基礎行為)
- Unlocks: Story 002 (H.7 tier3_identity_depth_test 可轉為綠燈)
