# Story 009: Missile Tier-3 Unique Mechanics (M1/M2/M3/M4)

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
**ADR Decision Summary**: M1 Tier-3 查詢 `IPartStateQuery` 取熱量最高部位；M3 Tier-3 訂閱 `PartBroke` 並發 `MissileHit` 至相鄰部位（鏈式破壞），以 `is_chain_break` 旗標防遞迴（由 KaijuParts 管理，Weapons 僅判斷第一次觸發）；M2/M4 Tier-3 修改彈匣 / 分裂行為，數值全從 `WeaponDef.TierKnobs[3]` 讀取。ADR-0002 §3：`PartBroke.is_chain_break` 旗標由 KaijuParts 設定，Weapons 在 M3 T3 handler 中需檢查此旗標。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: M1 T3 需遍歷所有可破壞部位查 `H_current`——建議 `IPartStateQuery.GetHottestPart(WeaponId)` 在介面上公開，或遍歷 `IPartStateQuery.GetAllPartIds()` + `GetHeatCurrent(partId)`（具體介面方法於實作時確定並記錄）。M4 T3 子彈分裂以解析公式計算 6 個方向向量（45° 間隔）。

**Control Manifest Rules (this layer)**:
- Required: MUST M1 T3 熱源引導經 `IPartStateQuery` 查熱量最高部位 (§3 Weapons)
- Required: MUST M3 T3 接收 `PartBroke` 後發 `MissileHit` 至 `adjacency_list`（上限 `m3_t3_chain_max_targets`）(§3 Weapons, §4.2)
- Required: MUST M3 T3 handler 檢查 `PartBroke.is_chain_break == false` 才觸發鏈式——防遞迴 (§4.2 ADR-0002 §4 負面)
- Required: MUST 所有 Tier-3 旋鈕從 `WeaponDef.TierKnobs[3]` 讀取 (§1.2)
- Forbidden: MUST NOT 在接收 `PartBroke` 後發 `on_part_break`（只發 `MissileHit`，由 KaijuParts 決定是否破壞）(§3 Weapons)
- Forbidden: MUST NOT 硬編碼 3 枚飛彈 / 12 枚彈匣 / 6 子彈 / 45° 等 Tier-3 常數 (§1.2)

---

## Acceptance Criteria

*From GDD `design/gdd/weapon-system.md` §C.5 Tier-3 欄位 / §H.7，scoped to this story:*

- [ ] **M1 T3 熱源引導**：`IWeaponTierQuery.GetTier(M1) == 3` 時每次射擊發射 `m1_t3_missiles_per_shot`（= 3）枚；第 3 枚自動鎖定當前 H_current 最高的部位（經 `IPartStateQuery` 查詢）；前 2 枚追蹤行為不變
- [ ] **M2 T3 飽和齊射**：Tier-3 時彈匣容量為 `m2_t3_mag_count`（= 12）；可選拆為 2 次 6 枚連發，兩次連發之間間隔 `m2_t3_burst_micro_cd`（1s）；換彈時間與 Tier-0 相同（5s）
- [ ] **M3 T3 穿甲爆破鏈**：接收 `PartBroke(is_chain_break == false)` 事件後（且 `IWeaponTierQuery.GetTier(M3) == 3`），對 payload `adjacency_list` 中最多 `m3_t3_chain_max_targets`（= 2）個部位各發 `MissileHit(break_delta_base = m3_t3_chain_dmg_mult × D0 × buPerD0)`（= 1.5 × 100 × 10 = 1500 BU）；`is_chain_break == true` 的 `PartBroke` 事件不觸發鏈式（防遞迴）
- [ ] **M4 T3 子母炸彈**：`IWeaponTierQuery.GetTier(M4) == 3` 時，母彈落地後分裂為 `m4_t3_child_count`（= 6）枚子彈，以 45° 間隔星形散佈；各子彈命中後發 `MissileHit(break_delta_base = m4_t3_child_dmg_pct × D0 × buPerD0)`（= 0.20 × 100 × 10 = 200 BU）
- [ ] `IWeaponTierQuery.GetTier() < 3` 時 M1/M2/M4 保持原有行為；M3 不觸發鏈式
- [ ] H.7 自動化測試（Story 002）在本 story 實作後應轉為綠燈

---

## Implementation Notes

*Derived from ADR-0002 §1–§3 and GDD §C.5 / §E.7:*

- **M1 T3 熱源查詢**：呼叫 `IPartStateQuery.GetAllPartIds()` 後遍歷取 `GetHeatCurrent(partId)` 最大值；如介面尚未公開此方法，於實作時在 `IPartStateQuery` 介面上新增（需同步更新 `Core` 定義並標記 story dependency）。
- **M2 T3 連發狀態機**：在 `MissileWeaponBase` 的彈匣狀態機上增加「burst phase」狀態：`BURST_A`（6 枚） → `BURST_COOLDOWN`（1s） → `BURST_B`（6 枚） → `RELOADING`。換彈計時器與 Tier-0 相同。
- **M3 T3 鏈式防遞迴**：`PartBroke` payload 的 `is_chain_break` 欄位由 KaijuParts 設定為 `true` 在鏈式破壞時。Weapons M3 T3 handler：`if (!evt.IsChainBreak && IWeaponTierQuery.GetTier(M3) == 3) { /* emit chain MissileHit */ }`。
- **M4 T3 子彈方向**：6 個方向 = `Quaternion.Euler(0, 0, i * 45f) * Vector2.up`（i = 0..5），覆蓋 360°（與 GDD「45° 間隔星形」一致）。子彈速度從 WeaponDef `m4_t3_child_speed` 旋鈕讀取。
- **M3 T3 `break_delta_base` 無狀態查詢**：鏈式 MissileHit 不需查詢目標 heat_state（鏈式傷害固定為 `m3_t3_chain_dmg_mult × D0 × buPerD0`）；KaijuParts 仍套用 M_state_mult。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 006: M1/M2/M4 Tier-0 到 Tier-2 基礎行為
- Story 007: M3 Tier-0 到 Tier-2 熱衝擊門檻（本 story 只加 Tier-3 鏈式路徑）
- Story 008: L1/L2/L3/L4 Tier-3
- KaijuParts: 設定 `is_chain_break = true`、套用狀態乘數——屬 KaijuParts Epic

---

## QA Test Cases

*Integration story — automated test specs.*

**Test: AC-1 — M1 T3 第 3 枚鎖定熱量最高部位**
- Given: M1 fixture（`m1_t3_missiles_per_shot=3`）；IWeaponTierQuery stub（GetTier=3）；IPartStateQuery stub（partId=1 H_current=80, partId=2 H_current=40, partId=3 H_current=90）；fake bus spy
- When: `M1HomingMissile.TryFire()`
- Then: spy 收到 3 次 `MissileHit`；前 2 枚追蹤一般目標；第 3 枚 `PartId == 3`（H_current 最高）
- Edge cases: 熱量並列時取 index 最低者（需定義決勝規則）；所有部位 H_current=0 → 第 3 枚行為同一般追蹤

**Test: AC-2 — M2 T3 兩次連發各 6 枚，中間 1s 冷卻**
- Given: M2 fixture（`m2_t3_mag_count=12`, `m2_t3_burst_micro_cd=1.0f`）；IWeaponTierQuery stub（GetTier=3）；fake bus spy；所有飛彈命中 partId=1
- When: `M2SwarmLauncher.TryFire()`
- Then: spy 立即收到 6 次 MissileHit；1.0s 冷卻後自動發射第 2 批，spy 再收到 6 次；共 12 次 MissileHit
- Edge cases: 在 burst_cooldown 期間呼叫 TryFire() → 被忽略（不打斷連發）

**Test: AC-3 — M3 T3 鏈式破壞（is_chain_break=false）**
- Given: M3 fixture（`m3_t3_chain_dmg_mult=1.5f`, `m3_t3_chain_max_targets=2`）；D0=100f, buPerD0=10f；IWeaponTierQuery stub（GetTier=3）；fake bus spy；`PartBroke(PartId=1, IsChainBreak=false, adjacency_list=[2,3,4])`
- When: 透過 fake bus 發布 PartBroke 事件
- Then: spy 收到 2 次 `MissileHit`（PartId=2, BreakDeltaBase=1500f；PartId=3, BreakDeltaBase=1500f）；adjacency_list 第 3 個（partId=4）不觸發（超過 max_targets=2）
- Edge cases: `is_chain_break=true` → spy 收到 0 次 MissileHit（防遞迴保護生效）

**Test: AC-4 — M4 T3 子母炸彈發 6 次 MissileHit**
- Given: M4 fixture（`m4_t3_child_count=6`, `m4_t3_child_dmg_pct=0.20f`）；D0=100f, buPerD0=10f；IWeaponTierQuery stub（GetTier=3）；fake bus spy；6 子彈各命中 partId=1..6
- When: M4 母彈落點觸發分裂
- Then: spy 收到 6 次 `MissileHit`，各自 `BreakDeltaBase = 0.20f * 100f * 10f = 200f`
- Edge cases: Tier 0/1/2 → 不分裂，母彈行為同 Story 006

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/Weapons/weapons_missile_tier3_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (WeaponDef Tier-3 旋鈕), Story 003 (IWeaponTierQuery + PartBroke 訂閱), Story 006 (M1/M2/M4 基礎行為), Story 007 (M3 基礎行為)
- Unlocks: Story 002 (H.7 tier3_identity_depth_test 可轉為綠燈)
