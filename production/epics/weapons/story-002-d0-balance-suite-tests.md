# Story 002: D₀ Balance Suite — Automated Formula Tests

> **Epic**: 武器系統
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: [fill before sprint planning]
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/weapon-system.md`
**Requirement**: `TR-weapon-001`, `TR-weapon-002`, `TR-weapon-003`, `TR-weapon-007`
*(TR-IDs inferred from GDD §H — tr-registry.yaml not yet populated)*

**ADR Governing Implementation**: ADR-0003: 資料驅動調校 — ScriptableObject 為唯一調校資料來源
**ADR Decision Summary**: 平衡公式測試把 SO 值作為輸入參數，改 SO 即重跑；測試以假 SO fixture 注入（工廠函式或測試專用 `.asset`），不用行內魔數；系統以建構子接收 config，測試時注入固定 fixture → 決定性、隔離。ADR-0002 (secondary)：查詢介面 (`IPartStateQuery`, `IWeaponTierQuery`) 在此測試層以假實作注入，確認公式計算正確性而非網路行為。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: 測試以 Unity Test Framework EditMode 執行（NUnit），無需 PlayMode 或 MonoBehaviour。ScriptableObject 可由 `ScriptableObject.CreateInstance<T>()` 在測試中建立，無需資產檔案。

**Control Manifest Rules (this layer)**:
- Required: MUST 測試決定性（無亂數種子 / 無時間相依）、隔離（自建自拆狀態）、獨立（不依執行順序）、無 I/O（DI 注入假依賴）(§1.6)
- Required: MUST 測試以假 SO fixture 注入，不用行內魔數（ADR-0003 §4）
- Required: MUST 測試檔命名 `[system]_[feature]_test`；函式 `test_[scenario]_[expected]` (§1.6)
- Required: MUST（驗證先行）Logic 故事先寫測試，再實作讓測試通過 (§1.6)
- Forbidden: MUST NOT 為過 CI 停用 / skip 失敗測試——修根因 (§1.6)
- Guardrail: 此 story 僅寫測試；測試在 Story 003–009 實作完成後轉為綠燈（先紅後綠是預期狀態）

---

## Acceptance Criteria

*From GDD `design/gdd/weapon-system.md` §H（H.1 / H.2 / H.3 / H.7），scoped to this story:*

- [ ] **H.1**: `weapons_dps_equivalence_test.cs` — 8 把武器各自 `Sustained_Output = Total_Output_per_Mag / (Mag_Duration + Reload_Time)` 結果均落在 `D0_reference × [0.9, 1.1]` 之內；測試使用 GDD G 章節預設值的假 SO fixture
- [ ] **H.2**: `weapons_loadout_matrix_test.cs` — 對「標準 Boss（Normal Part）」場景計算全 64 組 loadout 的 TTB，驗證 `max(TTB) / min(TTB) ≤ 2.0`；且無任何 loadout 在普通 / 強化 / Boss 核心三種部位類型上同時排名前三
- [ ] **H.3**: `weapons_m3_gate_validation_test.cs` — 在任意合法護甲設定（`B_unsoftened_mult` ∈ [0.20, 0.50]）下，M3 跳過蓄熱直接破甲的 TTB ≥ 正常蓄熱路徑 TTB × 1.5；遍歷 `B_unsoftened_mult` 安全範圍若干取樣點驗證
- [ ] **H.7**: `weapons_tier3_identity_depth_test.cs` — 對 8 把武器比較 Tier-1 vs Tier-3 的普通部位 TTB，每把武器 `TTB_tier3 ≥ TTB_tier1 × 0.85`（縮短不超過 15%）

---

## Implementation Notes

*Derived from ADR-0003 §4 and GDD D 章節（公式）:*

**D.1 Sustained_Output 公式**（用於 H.1 / H.2）：
```
Sustained_Output(w) = Total_Output_per_Mag(w) / (Mag_Duration(w) + Reload_Time(w))
```
- L2 無彈匣：以「連續 30 秒最優命中」為等效 Mag cycle（`Mag_Duration` 視為無限，用 30s 視窗計算）
- L3：tap 模式與 charge 模式分別計算後取加權平均（按使用比例），或採 GDD 指定的最優命中情境

**D.2 T_soften 公式**（TTB 前半）：
```
T_soften = theta_S / max(H_rate_weapon - H_decay_rate, epsilon)
```

**D.3 T_break 公式**（TTB 後半，軟化後）：
```
T_break = B_max / B_rate_softened          (連續飛彈)
T_break = ceil(B_max / B_fill_per_shot) × shot_interval   (M3 離散)
```

**TTB = T_soften + T_break**（GDD D.4）

所有測試以 `ScriptableObject.CreateInstance<WeaponDef>()` 建立 fixture，填入 GDD 預設值——禁止在測試程式碼中用魔數直接算結果。

`IPartStateQuery` 在 H.2/H.3 測試中注入 stub，回傳固定的 `PartState = NORMAL` / `SOFTENED`。

**H.3 Guard**：`required_break_threshold_normal`（預設 = `B_max_normal` = 100 BU）確保熱衝擊引爆只加速填充速率，玩家仍須填滿整條破甲槽。TTB 公式需包含此閾值強制。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: WeaponDef + WeaponBalanceConfig SO 類別（測試所需的 class 定義在此）
- Stories 003–009: 讓這些測試轉為綠燈的實際武器行為實作
- H.4 / H.5 / H.6 / H.8：非自動化測試（體驗性 / 關卡設計 / 主觀驗收），不在本 story 涵蓋

---

## QA Test Cases

*Logic story — automated test specs. 開發者實作這些測試後，測試即是自身的 QA 證據。*

**Test: H.1 — 等功率等價**
- Given: 8 個假 WeaponDef fixture（GDD G.2/G.3 預設值）+ WeaponBalanceConfig fixture（D0_reference=100）
- When: 對每把武器執行 `Sustained_Output` 公式
- Then: 全 8 個結果落在 [90.0f, 110.0f] PU/s；各自 Assert.That(result, Is.InRange(90f, 110f))
- Edge cases: L2（無彈匣）採 30s 視窗；L3 short-pulse vs charge — 各自分開驗證再求加權平均；M3 持續輸出約 1.29×D₀（GDD 明許容差內）

**Test: H.2 — 無主導 loadout（TTB 矩陣）**
- Given: 同上 fixture；Normal Part（H_max=100, B_max=100, theta_S=100, H_decay_rate=3, B_unsoftened_mult=0.35）
- When: 計算 4×4=16 Primary×Secondary 組合的 TTB（標準 Boss, Normal Part）
- Then: `Assert.That(maxTTB / minTTB, Is.LessThanOrEqualTo(2.0f))`；遍歷 Normal / Armored / Core 三種部位，取各 loadout 排名，驗證無 loadout 在三種部位均排名前三
- Edge cases: TTB 並列時以 index 排序決定名次；boundary ratio 恰好 2.0 應通過（≤，非 <）

**Test: H.3 — M3 熱衝擊引爆護甲門檻**
- Given: M3 WeaponDef fixture（m3_dmg_unsoftened_mult=3.0, m3_heat_shock_fill_mult=2.0, m3_mag_size=3, m3_reload_time=4.0s）；WeaponBalanceConfig 取 B_unsoftened_mult ∈ {0.20, 0.35, 0.50}（安全範圍端點與中值）；Normal Part（B_max=100）
- When: 計算 (a) 跳過蓄熱純 M3 路徑 TTB：T_soften=0，T_break = B_max / (3×D₀×10×B_unsoftened_mult)；(b) 正常路徑 TTB（L1 蓄熱 + M3 熱衝擊）
- Then: 對每個 B_unsoftened_mult 取樣點，`Assert.That(ttbUnsoftened, Is.GreaterThanOrEqualTo(ttbNormal * 1.5f))`
- Edge cases: B_unsoftened_mult=0.50（上限）下比值是否仍 ≥1.5×（平衡閘門最鬆弛情境）

**Test: H.7 — Tier-3 身份深化不放大數值**
- Given: 每把武器的 Tier-1 和 Tier-3 WeaponDef fixture；Normal Part
- When: 分別計算 TTB_tier1 和 TTB_tier3（Tier-3 旋鈕含 unique mechanic 的 delta）
- Then: 對每把武器 `Assert.That(ttbTier3, Is.GreaterThanOrEqualTo(ttbTier1 * 0.85f))`（TTB 縮短不超過 15%）
- Edge cases: L3 T3 共鳴擴散即時注入 50% 熱量 → 等效縮短 T_soften；計算需包含此效果但結果仍需 ≥ 0.85×

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/Weapons/weapons_dps_equivalence_test.cs` — must exist and pass
- `Assets/_Project/Tests/Weapons/weapons_loadout_matrix_test.cs` — must exist and pass
- `Assets/_Project/Tests/Weapons/weapons_m3_gate_validation_test.cs` — must exist and pass
- `Assets/_Project/Tests/Weapons/weapons_tier3_identity_depth_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (WeaponDef + WeaponBalanceConfig SO 類別須存在，以建立 fixture)
- Unlocks: 提供迴歸保護供 Stories 003–009 實作時驗證平衡不漂移
