# Story 005: L3 Wave Cannon — Dual-Mode Tap/Charge + L3WaveHit Event

> **Epic**: 武器系統
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: [fill before sprint planning]
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/weapon-system.md`
**Requirement**: `TR-weapon-004`
*(TR-IDs inferred from GDD §H — tr-registry.yaml not yet populated)*

**ADR Governing Implementation**: ADR-0002: 事件架構與系統間通訊 (primary); ADR-0003: 資料驅動調校 (secondary)
**ADR Decision Summary**: L3 波動砲蓄力震波命中後發 `L3WaveHit(part_id, kaiju_id)` 事件（ADR-0002 §1 明確列出此第三條武器輸出通道）；KaijuParts 訂閱此事件觸發震盪硬直 / 剝甲；所有時間參數（`l3_charge_time`, `l3_stagger_window`, `l3_charge_cooldown`, `l3_tap_output_mult`, `l3_charge_output_mult`）均從 `WeaponDef` tier slot 讀取。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: 長按計時器使用 `Time.deltaTime` 累積（非 `WaitForSeconds`），確保 `timeScale` 凍結時蓄力暫停（符合頓幀設計）。L3 蓄力模式需「長按計時事件」(GDD F.6)，實作層需與 `Input` 系統協作——本 story 假設輸入介面已抽象（bool `isHeld` 作為注入參數）。

**Control Manifest Rules (this layer)**:
- Required: MUST 飛彈命中發 `L3WaveHit(part_id, kaiju_id)`（ADR-0002 §1 明確定義此通道）(§3 Weapons, §4.2)
- Required: MUST 所有時間旋鈕（charge_time, cooldown, stagger_window）從 `WeaponDef` 讀取 (§1.2)
- Required: MUST 計時器以 `Time.deltaTime` 累積（幀率無關）(§1.7)
- Forbidden: MUST NOT 在 Weapons 層觸發震盪硬直——只發 `L3WaveHit`，由 `KaijuParts` 消費 (§3 Weapons, §4.2)
- Forbidden: MUST NOT 硬編碼 1.5s / 2.5×D₀ / 2.0s cooldown 等數值 (§1.2)

---

## Acceptance Criteria

*From GDD `design/gdd/weapon-system.md` §C.4 L3 和 §E.4，scoped to this story:*

- [ ] **短脈衝模式（Tap）**：按下主武器鍵後 < `l3_charge_time` 秒釋放 → 對目標發布 `LaserHit(heat_delta = l3_tap_output_mult × D0_reference × heatConversionFactor)`；無冷卻，可連發
- [ ] **蓄力模式（Hold/Charge）**：長按 ≥ `l3_charge_time` 秒後釋放 → 發布 `LaserHit(heat_delta = l3_charge_output_mult × D0_reference × heatConversionFactor)`，同幀發布 `L3WaveHit(partId, kaijuId)`，隨後進入 `l3_charge_cooldown` 秒冷卻；冷卻中無法觸發蓄力
- [ ] `l3_stagger_window` 旋鈕值（預設 2.0s）存於 `WeaponDef`——由 KaijuParts 消費，Weapons 層不直接使用（作為欄位存在，供 KaijuParts 透過 `IWeaponTierQuery` 或事件 payload 取得）
- [ ] 蓄力進度以計時器（`Time.deltaTime` 累積）驅動，`timeScale = 0` 時蓄力暫停
- [ ] 蓄力冷卻中再次嘗試長按不觸發蓄力（僅允許短脈衝或阻擋輸入，由武器狀態機管理）
- [ ] 所有輸出倍率 / 時間從 `WeaponDef.TierKnobs[currentTier]` 讀取，無硬編碼

---

## Implementation Notes

*Derived from ADR-0002 §1 and GDD §C.4 L3 / §E.4:*

- **`L3WaveHit` struct（Core）**：`{ PartId PartId; KaijuId KaijuId; }` — Weapons 發布，KaijuParts 訂閱後在其領域執行 2s 硬直。Weapons **不持有**硬直計時器，它屬於 KaijuParts 職責。
- **狀態機**：`IDLE` → `CHARGING`（hold 計時累積）→ 若 < charge_time 釋放回 `IDLE`（發 tap）→ 若 ≥ charge_time 釋放到 `COOLDOWN`（發 charge + L3WaveHit）→ cooldown 計時後回 `IDLE`。
- **熱量換算**：`heat_delta = l3_tap_output_mult × WeaponBalanceConfig.D0Reference × buPerD0 / H_maxNormal`？——實際上 GDD 中 L3 tap = 0.6×D₀ 是「輸出倍率」，對應的 H_rate 見 D.2 表格（15 HU/s for tap）。實作時以 `WeaponDef.TierKnobs[tier].l3TapHRate`（直接儲存 HU/s）計算 `heat_delta = l3TapHRate × deltaTime` 更直接——與 GDD D.2 表格一致。
- **`l3_stagger_window` 傳遞方式**：`L3WaveHit` payload 可攜帶 `float StaggerDuration`（從 WeaponDef 讀取後放入 payload），讓 KaijuParts 不需另外查詢，符合 ADR-0002 「payload 一次攜齊」原則。
- **觸控風險（GDD F.6 / I）**：長按判定可能與移動滑動衝突——本 story 以 bool 介面接收「hold intent」，觸控邊界情況由 `Input` 系統 Story 6 處理，此處不阻斷。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 004: L1/L2/L4 基礎雷射發射（LaserWeaponBase 繼承基礎）
- Story 008: L3 Tier-3「共鳴擴散」（蓄力震波同時注入 50% 熱量）
- KaijuParts: 接收 `L3WaveHit` 後的震盪硬直 / 護甲剝離邏輯（屬 KaijuParts Epic）
- GameFeel/UI: 蓄力進度視覺指示器（Presentation 層）

---

## QA Test Cases

*Integration story — automated test specs.*

**Test: AC-1 — Tap 模式發 LaserHit（短按）**
- Given: L3 `WeaponDef` fixture（`l3_charge_time=1.5f`, `l3_tap_h_rate=15.0f`）；fake bus spy；deltaTime 累積 0.3f（< 1.5s）後釋放
- When: `L3WaveCannon.UpdateFrame(deltaTime=0.3f, isHeld=true)` × N 幀後 `UpdateFrame(deltaTime=0.016f, isHeld=false)`
- Then: spy 收到 1 次 `LaserHit`（HeatDelta ≈ 15.0f * 0.3f = 4.5f）；0 次 `L3WaveHit`
- Edge cases: 累積恰好等於 `l3_charge_time`（boundary：1.5f）應視為 charge（≥）

**Test: AC-2 — Charge 模式發 LaserHit + L3WaveHit（長按）**
- Given: 同上 fixture；hold 累積 ≥ 1.5s 後釋放
- When: 模擬 hold 1.5s 後釋放
- Then: spy 收到 1 次 `LaserHit`（HeatDelta 對應 l3_charge_h_rate 乘以 charge_time）；spy 收到 1 次 `L3WaveHit(partId, kaijuId)`；兩事件同幀（測試中連續 publish）
- Edge cases: payload 中 `StaggerDuration` 欄位 ≈ 2.0f（來自 WeaponDef fixture）

**Test: AC-3 — 蓄力冷卻期間不觸發第二次蓄力**
- Given: 同上 fixture（`l3_charge_cooldown=2.0f`）；first charge 已觸發
- When: 立即再次 hold ≥ 1.5s
- Then: 冷卻期內 spy 無新的 `L3WaveHit`；2.0s 冷卻後再次 hold ≥ 1.5s 可觸發
- Edge cases: tap 在冷卻期間仍可使用（`LaserHit` 可發布）

**Test: AC-4 — timeScale=0 時蓄力暫停**
- Given: 同上 fixture；使用 `Time.timeScale` mock（或 unscaledDeltaTime vs deltaTime 對比測試）
- When: `timeScale = 0`，Update 呼叫 50 次
- Then: 蓄力計時器值不增加（計時暫停）；狀態仍為 `CHARGING`
- Edge cases: 此測試需 PlayMode 或計時器抽象（純 C# 計時器以注入 `Func<float> getDeltaTime` 解耦）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/Weapons/weapons_l3_wavecannon_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (WeaponDef 含 L3 旋鈕), Story 003 (LaserWeaponBase + IEventBus 佈線)
- Unlocks: Story 008 (L3 Tier-3「共鳴擴散」在此基礎上 override)
