# Story 006: Missile Family Firing — M1 Homing, M2 Swarm, M4 Cluster

> **Epic**: 武器系統
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: [fill before sprint planning]
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/weapon-system.md`
**Requirement**: `TR-weapon-001`
*(TR-IDs inferred from GDD §H — tr-registry.yaml not yet populated)*

**ADR Governing Implementation**: ADR-0002: 事件架構與系統間通訊 (primary); ADR-0003: 資料驅動調校 (secondary)
**ADR Decision Summary**: 飛彈命中後發 `MissileHit(part_id, kaiju_id, break_delta_base, weapon_id)` 事件；`break_delta_base` 由 `WeaponDef` 旋鈕計算（`BRate × buPerD0`），狀態乘數（B_unsoftened_mult / ×1.5 Stagger）由 KaijuParts 側套用。M1 追蹤飛彈經 `IPartStateQuery.GetWorldPosition(partId)` 讀取目標世界座標。飛彈彈匣狀態機由 `MissileWeaponBase`（Story 003）提供，本 story 實作具體行為。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: M1 飛彈物理追蹤以 `transform.rotation` + `Quaternion.RotateTowards` 或 steering behavior 實作（建議純 C# 可測試，不依賴 Rigidbody2D）。M4 拋物線以解析拋體公式計算落點，不用物理模擬。查驗 Unity 6.3 `Physics2D.OverlapCircleNonAlloc` 用於 M4 AoE 目標採集（`docs/engine-reference/unity/VERSION.md`）。

**Control Manifest Rules (this layer)**:
- Required: MUST 飛彈命中發 `MissileHit(part_id, kaiju_id, break_delta_base, weapon_id)` (§3 Weapons, §4.2)
- Required: MUST M1 追蹤飛彈經 `IPartStateQuery` 讀部位 `world_position` (§3 Weapons)
- Required: MUST `break_delta_base` 計算值從 `WeaponDef.TierKnobs[tier]` 讀取（`m_dmg_mult × D0_reference × buPerD0`）(§1.2)
- Required: MUST 彈匣 / 換彈狀態機繼承自 `MissileWeaponBase`（Story 003），不重複實作 (§3 Weapons)
- Forbidden: MUST NOT 在 Weapons 側套用 B_unsoftened_mult——state_mult 由 KaijuParts 負責 (§4.2)
- Forbidden: MUST NOT 用 Rigidbody2D 模擬飛彈（ADR-0001 敵彈規則延伸；玩家飛彈池歸屬開放，見 §3 BulletSim 備注）

---

## Acceptance Criteria

*From GDD `design/gdd/weapon-system.md` §C.5（M1/M2/M4），scoped to this story:*

- [ ] **M1 追蹤飛彈（Homing Missile）**：每次射擊同時生成 2 枚飛彈（`m1_missiles_per_shot`）；各枚以 ±`m1_tracking_angle_deg`（60°）最大偏轉追蹤 `IPartStateQuery.GetWorldPosition(targetPartId)`；命中後各自發布 `MissileHit(break_delta_base = m1_dmg_per_missile_mult × D0 × buPerD0)`；彈匣 6 枚（= 3 次射擊），換彈 `m1_reload_time`（3s）
- [ ] **M2 蜂群飛彈（Swarm Launcher）**：每次射擊同時發射 8 枚微型飛彈（`m2_micro_count`），扇形擴散覆蓋約 `m2_cone_width_pct`（70%）畫面寬度；各枚命中後各自發布 `MissileHit(break_delta_base = D0/8 × buPerD0)`；彈匣 8 枚（= 1 次齊發），換彈 `m2_reload_time`（5s）
- [ ] **M4 叢集炸彈（Cluster Bomb）**：拋物線飛行，落點 Y 坐標在 `[m4_drop_y_min_pct, m4_drop_y_max_pct]` 螢幕高度範圍內；AoE 半徑 `m4_aoe_radius_pct` × 螢幕高度；AoE 內 N 個部位各自受 `break_delta_base = D0 / N × buPerD0`（上限 `m4_total_output_cap_mult × D0 × buPerD0`）；N=1 時受 `m4_single_target_mult × D0 × buPerD0`（= 2×D₀）；彈匣 4 枚，換彈 3.5s
- [ ] 三種飛彈的所有數值參數（彈匣、換彈時間、傷害倍率、追蹤角度）均從 `WeaponDef.TierKnobs[currentTier]` 讀取
- [ ] M4 AoE 查詢使用 non-alloc overlap（無每發 GC 配置）

---

## Implementation Notes

*Derived from ADR-0002 §2–§3 and GDD §C.5 / §D.3 / §E.5:*

- **`break_delta_base` 換算**：`buPerD0 = WeaponBalanceConfig.BuPerD0`（= 10，以 SO 欄位明確儲存，禁止寫死 `× 10` 在程式碼）。M1 兩枚合計 `D0 × buPerD0 / shot`；M2 單枚 `D0/8 × buPerD0`。
- **M1 追蹤上限**：追蹤角度 ±60°，超出此範圍的目標不追蹤（直線飛行）。每幀以 `Quaternion.RotateTowards` 計算轉向，轉向速度（`m1_tracking_speed`）由 WeaponDef 旋鈕定義。
- **M4 AoE 分配**：`Physics2D.OverlapCircleNonAlloc` 填入 pre-allocated `Collider2D[8]` 陣列，N = 有效命中部位數；`break_delta_base = min(D0 × m4_single_target_mult, D0 / N) × buPerD0`（E.5：N=1 時 2×D₀，N>1 時 D₀/N，總輸出上限 D₀）。
- **彈匣繼承**：三種飛彈直接複用 `MissileWeaponBase` 的 `TryFire()` / `StartReload()` 流程，不重寫狀態機邏輯。
- **M3 排除**：M3 AP Torpedo 因有熱衝擊門檻查詢（`IPartStateQuery.GetHeatState()`），複雜度獨立拆為 Story 007。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 007: M3 AP Torpedo（熱衝擊門檻需 `IPartStateQuery.heat_state` 查詢）
- Story 009: M1/M2/M4 Tier-3 unique mechanics
- KaijuParts: 套用 `B_unsoftened_mult` / `stagger_break_mult` 狀態乘數（KaijuParts Epic）
- GameFeel/UI: 飛彈視覺特效 / M4 落點指示器座標（GameFeel/UI 層）

---

## QA Test Cases

*Integration story — automated test specs.*

**Test: AC-1 — M1 射擊發布 2 次 MissileHit**
- Given: M1 `WeaponDef` fixture（`m1_dmg_per_missile_mult=0.5f`, `m1_missiles_per_shot=2`）；fake bus spy；D0=100f, buPerD0=10f；IPartStateQuery stub（GetWorldPosition 回傳固定座標）
- When: `M1HomingMissile.TryFire()`（彈匣有餘）後模擬兩枚飛彈分別命中 partId=1
- Then: spy 收到 2 次 `MissileHit`，各自 `BreakDeltaBase = 0.5f * 100f * 10f = 500f`
- Edge cases: 追蹤角度超限（180° 後方目標）→ 飛彈直線飛行（不追蹤），仍可命中近距目標

**Test: AC-2 — M2 齊發 8 枚各自 MissileHit**
- Given: M2 fixture（`m2_micro_count=8`）；D0=100f, buPerD0=10f；fake bus spy；8 枚均命中 partId=1
- When: `M2SwarmLauncher.TryFire()`
- Then: spy 收到 8 次 `MissileHit`，各自 `BreakDeltaBase = (100f/8) * 10f = 125f`
- Edge cases: 7 枚命中、1 枚 miss → spy 收到 7 次（miss 不發事件）

**Test: AC-3 — M4 N=2 時各部位受 D₀/2**
- Given: M4 fixture（`m4_single_target_mult=2.0f`, `m4_total_output_cap_mult=1.0f`）；D0=100f；AoE 內 2 個部位（partId=1, 2）
- When: M4 炸彈落點 AoE 覆蓋 2 個部位
- Then: spy 收到 2 次 `MissileHit`，各自 `BreakDeltaBase = (100f/2) * 10f = 500f`；總計 1000f = D₀ × buPerD0（符合 cap）
- Edge cases: N=1（孤立部位）→ `BreakDeltaBase = 2.0f * 100f * 10f = 2000f`（= 2×D₀）

**Test: AC-4 — M1 彈匣耗盡後換彈**
- Given: M1 fixture（`m1_mag_size=6`, `m1_reload_time=3.0f`）；每次 TryFire 消耗 2 枚
- When: TryFire() × 3 次（耗盡 6 枚）；立即 TryFire() 第 4 次
- Then: 第 4 次回傳 false + 觸發 StartReload；3.0s 後 TryFire() 回傳 true
- Edge cases: M2 彈匣 8 枚（= 1 次齊發）→ 首次射擊後立即進入換彈（5.0s）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/Weapons/weapons_missile_family_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (WeaponDef SO 含 M1/M2/M4 旋鈕), Story 003 (MissileWeaponBase + DI 佈線)
- Unlocks: Story 009 (M1/M2/M4 Tier-3 在此基礎上 override)
