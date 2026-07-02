# Story 007: M3 AP Torpedo — Heat-Shock Gate & Softened Query

> **Epic**: 武器系統
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: [fill before sprint planning]
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/weapon-system.md`
**Requirement**: `TR-weapon-003`
*(TR-IDs inferred from GDD §H — tr-registry.yaml not yet populated)*

**ADR Governing Implementation**: ADR-0002: 事件架構與系統間通訊 (primary); ADR-0003: 資料驅動調校 (secondary)
**ADR Decision Summary**: Weapons 需讀部位 `heat_state`（M3 熱衝擊引爆觸發）——經 `IPartStateQuery.GetHeatState(partId)` 查詢（ADR-0002 §2 明確列出）；M3 在 SOFTENED 時以 `m3_heat_shock_fill_mult` 放大 `break_delta_base` 後再 `Publish<MissileHit>`；KaijuParts 仍套用 `M_state_mult`（SOFTENED → ×1.0）完成最終破甲計算；所有乘數從 `WeaponDef` 讀取。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: M3 直線飛行魚雷，無追蹤，中速；以 `transform.Translate` + `speed` 旋鈕（WeaponDef）移動，穿透碰撞層（GDD：穿透護盾/護甲層直達部位本體）。穿透判定需 `Physics2D.RaycastAll` 但只取第一個有效部位——查驗 Unity 6.3 layer override 行為。

**Control Manifest Rules (this layer)**:
- Required: MUST M3 熱衝擊引爆經 `IPartStateQuery.GetHeatState(partId)` 查詢，以 `heat_state == SOFTENED` 為觸發條件 (§3 Weapons)
- Required: MUST `m3_heat_shock_fill_mult`（= 2.0）從 `WeaponDef.TierKnobs[tier]` 讀取，禁止硬編碼 (§1.2)
- Required: MUST 門檻鎖：`required_break_threshold_*` 強制玩家需填滿完整破甲槽，熱衝擊引爆只加速填充速率（GDD G.1 備注）——此驗收由 Story 002 H.3 測試覆蓋 (§1.2)
- Forbidden: MUST NOT 在 Weapons 層套用 `B_unsoftened_mult`（那是 KaijuParts 的職責）(§4.2)
- Forbidden: MUST NOT 用 Rigidbody2D 模擬魚雷移動 (§5)

---

## Acceptance Criteria

*From GDD `design/gdd/weapon-system.md` §C.5 M3 / §E.1 / §D.3，scoped to this story:*

- [ ] M3 發射時查詢 `IPartStateQuery.GetHeatState(targetPartId)` — 若 `SOFTENED`：`break_delta_base = m3_dmg_unsoftened_mult × m3_heat_shock_fill_mult × D0_reference × buPerD0`（= 3.0 × 2.0 × 100 × 10 = 6000 BU equivalent）；若非 SOFTENED：`break_delta_base = m3_dmg_unsoftened_mult × D0_reference × buPerD0`（= 3000 BU equivalent，由 KaijuParts 套用 0.35 → 最終 ≈ 1050 BU）
- [ ] `MissileHit` 事件的 `weapon_id = WeaponId.M3`（供 KaijuParts 識別為魚雷命中）
- [ ] 彈匣 3 枚，換彈 `m3_reload_time`（4s）；彈匣耗盡時輸出歸零 4s（GDD E.7）
- [ ] 魚雷以直線中速飛行，無追蹤；穿透護盾 / 護甲層直達部位本體（layerMask 設定只偵測部位本體 layer）
- [ ] 所有乘數（`m3_dmg_unsoftened_mult`, `m3_heat_shock_fill_mult`）從 `WeaponDef.TierKnobs[currentTier]` 讀取

---

## Implementation Notes

*Derived from ADR-0002 §2 and GDD §C.5 M3 / §E.1 / §D.1:*

- **熱衝擊時機**：M3 命中部位時同幀查詢 `IPartStateQuery.GetHeatState(partId)`，不快取（確保使用最新狀態）。查詢結果 `== PartHeatState.Softened` 時啟動熱衝擊乘數。
- **換算說明**：GDD D.1 確立 `1 D₀ = 10 BU/s（對已軟化部位）`；但 `break_delta_base` 是「瞬間 BU 填充量」而非速率。M3 SOFTENED: `6×D₀` 等效 `6 × 100 × 10 = 6000` BU 即時注入量（KaijuParts B_max=100 BU，單發填滿 × 60）。`buPerD0` 從 `WeaponBalanceConfig` 讀取。
- **`required_break_threshold` 強制**：熱衝擊引爆放大了每發 BU 填充速率，但 KaijuParts 仍需累積至 `required_break_threshold`（= B_max）才觸發破壞——此行為在 KaijuParts 側實作，Weapons 只需確保 `break_delta_base` 計算正確。
- **穿透設定**：魚雷 prefab 使用特定 Physics Layer（不含 Boss 護盾 layer），以 trigger 偵測第一個部位本體。穿透護盾的設定為 layer mask 組態，不需特殊 API。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 006: M1/M2/M4 基礎飛彈行為（MissileWeaponBase 繼承基礎）
- Story 009: M3 Tier-3「穿甲爆破鏈」（接收 `on_part_break` 並發 MissileHit 至相鄰部位）
- KaijuParts: 套用 `B_unsoftened_mult`（= 0.35 for non-SOFTENED）及 `required_break_threshold` 閘門——屬 KaijuParts Epic
- Story 002: H.3 驗證測試（跳過蓄熱的 TTB ≥ 正常路徑 1.5×）

---

## QA Test Cases

*Integration story — automated test specs.*

**Test: AC-1 — SOFTENED 部位觸發熱衝擊引爆（高 break_delta_base）**
- Given: M3 `WeaponDef` fixture（`m3_dmg_unsoftened_mult=3.0f`, `m3_heat_shock_fill_mult=2.0f`）；D0=100f, buPerD0=10f；IPartStateQuery stub（GetHeatState 回傳 `SOFTENED`）；fake bus spy
- When: M3 魚雷命中 partId=1
- Then: spy 收到 `MissileHit(BreakDeltaBase = 3.0f * 2.0f * 100f * 10f = 6000f, WeaponId = M3)`
- Edge cases: 命中瞬間部位狀態從 SOFTENED 變 NORMAL（race condition）——以同幀查詢結果為準（不重查）

**Test: AC-2 — NORMAL 部位僅基礎 break_delta_base（跳過蓄熱路徑效率懲罰）**
- Given: 同上 fixture；IPartStateQuery stub（GetHeatState 回傳 `NORMAL`）
- When: M3 魚雷命中 partId=1
- Then: spy 收到 `MissileHit(BreakDeltaBase = 3.0f * 1.0f * 100f * 10f = 3000f)`（KaijuParts 側套用 0.35 → 最終 1050 BU，本測試不驗證 KaijuParts 行為）
- Edge cases: STAGGERED（非 SOFTENED）→ 同 NORMAL 路徑（無熱衝擊）；KaijuParts 側套用 stagger_mult=1.5

**Test: AC-3 — 彈匣 3 枚耗盡後換彈 4s**
- Given: M3 fixture（`m3_mag_size=3`, `m3_reload_time=4.0f`）；fake bus spy
- When: `TryFire()` × 3（全彈匣）；第 4 次 `TryFire()`
- Then: 第 4 次回傳 false；4.0s 後 `TryFire()` 可再發射
- Edge cases: 換彈期間命中不可能（魚雷已在飛行中，彈匣是計數器非 reload 禁射）

**Test: AC-4 — IPartStateQuery 在每次命中時重新查詢（非快取）**
- Given: IPartStateQuery stub，首次 call 回傳 NORMAL，第二次 call 回傳 SOFTENED
- When: M3 發射兩枚（分兩次 TryFire）
- Then: 第 1 枚 `BreakDeltaBase = 3000f`（NORMAL）；第 2 枚 `BreakDeltaBase = 6000f`（SOFTENED）
- Edge cases: 驗證沒有狀態快取（上次查詢結果不影響下次）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/Weapons/weapons_m3_heat_shock_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (WeaponDef SO 含 M3 旋鈕), Story 003 (MissileWeaponBase), Story 006 (飛彈 DI 佈線模式確立)
- Unlocks: Story 009 (M3 Tier-3「穿甲爆破鏈」在此基礎上 override)
