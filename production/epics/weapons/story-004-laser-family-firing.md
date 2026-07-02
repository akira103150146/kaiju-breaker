# Story 004: Laser Family Firing — L1 Spread, L2 Focus, L4 Pierce

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
**ADR Decision Summary**: 雷射用 `Physics2D.RaycastAll`（或等效 overlap）對 ≤8 部位在 Mono 側判定，計算 `heat_delta = H_rate × deltaTime`（從 `WeaponDef` tier slot 讀取），以 `IEventBus.Publish<LaserHit>` 同步發布；不直接引用 `KaijuParts`；所有 H_rate / 擴散角 / 射擊間隔均從 `WeaponDef` 讀取，禁止硬編碼。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: `Physics2D.RaycastAll` 回傳 `RaycastHit2D[]`，建議用 non-alloc 版本 `Physics2D.RaycastNonAlloc` 避免每幀 GC——查驗 Unity 6.3 API 是否有參數變化（`docs/engine-reference/unity/VERSION.md`）。L2 hold-fire 需每幀發射，建議搭配固定 layer mask 降低 broadphase 成本。

**Control Manifest Rules (this layer)**:
- Required: MUST 雷射用 raycast/overlap 對 ≤8 部位在 Mono 側判定並發 `LaserHit` (§3 Weapons)
- Required: MUST 經 `IWeaponTierQuery` 讀當前 Tier，從 `WeaponDef.TierKnobs[tier]` 取 H_rate 等旋鈕 (§3 Weapons)
- Required: MUST 穩態 0 B/frame GC alloc（彈幕熱路徑）(§1.7)
- Forbidden: MUST NOT 引用 `KaijuParts` 組件 (§3 Weapons)
- Forbidden: MUST NOT 硬編碼 H_rate / 擴散角 / 射擊間隔（全部從 WeaponDef 讀取）(§1.2)

---

## Acceptance Criteria

*From GDD `design/gdd/weapon-system.md` §C.4（L1/L2/L4），scoped to this story:*

- [ ] **L1 散波雷射（Spread Laser）**：每幀同時發射 3 道 raycast（扇形擴散，角度由 WeaponDef 旋鈕定義）；全中命中時各 beam `heat_delta = l1_h_rate_full × deltaTime / 3`（三束合計 = `l1_h_rate_full × deltaTime`）；僅中央束命中時 `heat_delta = l1_h_rate_center × deltaTime`；每個命中部位各自發布 `LaserHit(partId, kaijuId, heatDelta)`
- [ ] **L2 集束雷射（Focus Beam）**：持續 hold-fire 時每幀發射 1 道 raycast，極窄判定框；命中時 `heat_delta = l2_h_rate × deltaTime`（等效 1.5×D₀ H_rate）；邊緣無衰減（硬截止：命中 = true / false）；發布 `LaserHit`
- [ ] **L4 穿透雷射（Pierce Beam）**：每 `l4_fire_interval` 秒發射一次 pierce raycast（穿透所有碰撞體），路徑上每個部位各自收到 `heat_delta = l4_h_rate × l4_fire_interval`；各自發布 `LaserHit(partId, kaijuId, heatDelta)`；最多 8 個部位
- [ ] 所有三種雷射：`heat_delta` 計算值完全來自 `WeaponDef.TierKnobs[currentTier]`，無行內魔數
- [ ] 無每幀 GC 配置（使用 non-alloc 版本 raycast）

---

## Implementation Notes

*Derived from ADR-0002 §1–§2 and GDD §C.4 / §D.2:*

- **L1 三束 raycast**：方向角度由 `l1_spread_angle`（WeaponDef 旋鈕，單位 degree）計算，兩側束對稱分佈；命中判定以 `layerMask = PartLayer`，不穿透 Boss 本體幾何。
- **L2 邊緣硬截止**：判定框寬度為 `l2_beam_width`（WeaponDef 旋鈕），超出此寬度完全無 heat_delta（非衰減漸變），確保「靜止小弱點最快蓄熱」情境成立。
- **L4 Pierce 非 alloc**：`Physics2D.RaycastNonAlloc(ray, hits, maxDist, layerMask)` 填入 pre-allocated `RaycastHit2D[8]` 陣列；結果按距離排序後逐一發布 `LaserHit`；超過 8 個部位時截斷（control manifest 上限）。
- **L4 fire interval**：使用計時器累積 `Time.deltaTime`（frame-rate independent），非 `Invoke`。
- **Tier 讀取**：每次開火前呼叫 `IWeaponTierQuery.GetTier(weaponId)` 並用 index 取 `WeaponDef.TierKnobs[tier].l_h_rate` 等；若查詢結果超出 array bound 使用 Tier 0（防禦性 fallback）。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 005: L3 Wave Cannon 雙模式（tap/charge）及 `L3WaveHit` 事件
- Story 008: L1/L2/L4 的 Tier-3 unique mechanics（殘熱焰、破點漣漪、熱殘影）
- GameFeel/UI: 雷射視覺特效與音效（Presentation 層）

---

## QA Test Cases

*Integration story — automated test specs.*

**Test: AC-1 — L1 三束全中 heat_delta 正確**
- Given: L1 `WeaponDef` fixture（`l1_h_rate_full=25.0f`）；fake `IEventBus` spy；deltaTime=0.016f；三束 raycast 均命中同一 part（partId=1）
- When: 呼叫 `L1SpreadLaser.FireFrame(deltaTime=0.016f)`
- Then: spy 收到 3 次 `LaserHit`，各自 `HeatDelta ≈ 25.0f * 0.016f / 3 ≈ 0.1333f`；三者相加 ≈ 0.4f
- Edge cases: 僅中央束命中（兩側 miss）→ spy 收到 1 次 `LaserHit`，`HeatDelta = 8.3f * 0.016f ≈ 0.133f`

**Test: AC-2 — L2 僅命中時發 LaserHit（邊緣硬截止）**
- Given: L2 `WeaponDef` fixture（`l2_h_rate=37.5f`）；fake bus；兩個部位：partId=1 在 beam 中心，partId=2 在邊緣外
- When: `L2FocusBeam.FireFrame(deltaTime=0.016f)`（hold=true）
- Then: spy 收到 1 次 `LaserHit`（partId=1，HeatDelta ≈ 0.6f）；partId=2 無事件
- Edge cases: hold=false（未按住）→ spy 收到 0 次事件

**Test: AC-3 — L4 穿透雷射對路徑上 N 個部位各自發 LaserHit**
- Given: L4 fixture（`l4_h_rate=25.0f`, `l4_fire_interval=0.4f`）；路徑上 2 個部位（partId=1, partId=2）；deltaTime 累積模擬 0.4s
- When: 觸發 Pierce 發射（計時器到期）
- Then: spy 收到 2 次 `LaserHit`，各自 `HeatDelta = 25.0f * 0.4f = 10.0f`
- Edge cases: 路徑上 9 個部位（超過 8 上限）→ 只有前 8 個部位收到 LaserHit

**Test: AC-4 — 幀率無關（frame-rate independence）**
- Given: L4 fixture；2 個部位
- When: 以 deltaTime=0.1f 模擬 4 次 `UpdateFrame`（總計 0.4s），最後一次觸發發射
- Then: 最後一次 `UpdateFrame` 發布 LaserHit，`HeatDelta = 25.0f * 0.4f = 10.0f`（與單次大 deltaTime 結果一致）
- Edge cases: 極大 deltaTime（0.5f 超過 fire_interval 0.4f）→ 僅發射一次（無補幀多發）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/Weapons/weapons_laser_family_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (WeaponDef SO 類別), Story 003 (LaserWeaponBase + DI 佈線)
- Unlocks: Story 008 (Laser Tier-3 在此行為基礎上 override)
