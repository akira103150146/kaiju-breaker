# Story 001: WeaponDef & WeaponBalanceConfig ScriptableObjects

> **Epic**: 武器系統
> **Status**: Ready
> **Layer**: Core
> **Type**: Config/Data
> **Estimate**: [fill before sprint planning]
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/weapon-system.md`
**Requirement**: `TR-weapon-001`
*(TR-IDs inferred from GDD §H — tr-registry.yaml not yet populated)*

**ADR Governing Implementation**: ADR-0003: 資料驅動調校 — ScriptableObject 為唯一調校資料來源
**ADR Decision Summary**: 所有 GDD 調校旋鈕以 ScriptableObject 資產表達，放 `Assets/_Project/Content/`，執行期唯讀；`WeaponBalanceConfig` + `WeaponDef`×8 承載 D₀/H_rate/B_rate/彈匣/Tier 效果；SO 用 `OnValidate` 對安全範圍做編輯期斷言；測試以假 SO fixture 注入。ADR-0002 (secondary) 定義 `IWeaponTierQuery`，武器執行期經此介面讀 WeaponDef tier 槽。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: ScriptableObject + OnValidate 是 Unity 穩定 API；無 post-cutoff 風險。執行期以 `[CreateAssetMenu]` 建立資產；OnValidate 在 EditMode 及 Play 模式入口呼叫。

**Control Manifest Rules (this layer)**:
- Required: MUST 所有 gameplay/balance 數值來自 SO，放 `Assets/_Project/Content/`，執行期唯讀 (§1.2)
- Required: MUST SO 用 `OnValidate` 對 GDD 安全範圍做編輯期斷言 (§1.2、§3 Content)
- Required: MUST 跨 GDD 共享旋鈕只有一個擁有者 SO（`B_unsoftened_mult`、`stagger_duration` 等由 `WeaponBalanceConfig` 擁有）(§1.2)
- Forbidden: MUST NOT 含執行期行為邏輯（Content 層純資料 + 驗證）(§3 Content)
- Forbidden: MUST NOT 在 gameplay 程式碼寫魔數 / 硬編碼平衡值 (§1.2)
- Forbidden: MUST NOT 把玩家可變資料（Tier 等級）寫入 SO——SO 唯讀，Tier 存 save (§1.2、ADR-0003 §2)

---

## Acceptance Criteria

*From GDD `design/gdd/weapon-system.md` G 章節（調校旋鈕），scoped to this story:*

- [ ] `WeaponBalanceConfig` SO 類別包含 GDD G.1 所有 15 個全域旋鈕欄位：`D0_reference`, `H_max_normal`, `H_max_armored`, `H_max_boss_core`, `H_decay_rate`, `theta_S`, `theta_S_exit`, `B_max_normal`, `B_max_armored`, `B_max_boss_core`, `B_unsoftened_mult`, `required_break_threshold_normal`, `required_break_threshold_armored`, `required_break_threshold_boss_core`, `stagger_duration`, `stagger_break_mult`
- [ ] `WeaponDef` SO 類別包含：`weaponId`（`WeaponId` enum）、`weaponType`（`WeaponType.Laser` / `WeaponType.Missile`）、以及 per-Tier 旋鈕陣列（index 0–3 對應 Tier 0–3），陣列元素為對應的雷射旋鈕 struct（G.2）或飛彈旋鈕 struct（G.3）
- [ ] 建立 8 個 `WeaponDef` 資產（`L1_SpreadLaser`, `L2_FocusBeam`, `L3_WaveCannon`, `L4_PierceBeam`, `M1_HomingMissile`, `M2_SwarmLauncher`, `M3_ApTorpedo`, `M4_ClusterBomb`）及 1 個 `WeaponBalanceConfig` 資產，所有欄位以 GDD G 章節預設值填入
- [ ] `WeaponBalanceConfig.OnValidate` 對所有標有安全範圍的旋鈕做斷言（越界 → Debug.LogWarning 並標記 Inspector 錯誤）；「閘門（Gate）」類型旋鈕須特別警示
- [ ] `WeaponDef.OnValidate` 驗證各 Tier 旋鈕陣列長度為 4、旋鈕值在 GDD G.2/G.3 安全範圍內
- [ ] 所有 SO 定義類別放 `Assets/_Project/Content/`（程式碼）；資產放 `Assets/_Project/Content/Weapons/`

---

## Implementation Notes

*Derived from ADR-0003 Decision §1–§4 and ADR-0002 §2:*

- `WeaponDef` 存各 Tier 的**靜態**旋鈕值；玩家「當前 Tier」由存檔讀取經 `IWeaponTierQuery` 查詢——兩者絕不混寫。`WeaponDef` 不存當前 Tier，只存 `TierKnobs[0..3]` 陣列。
- `WeaponBalanceConfig` 是 `B_unsoftened_mult`、`stagger_duration`、`D0_reference` 等跨系統共享旋鈕的唯一擁有者；`KaijuParts`、`GameFeel` 等系統經介面讀取，不在自身 SO 複製。
- C# 欄位命名用 `lowerCamelCase`（與 GDD 的 `snake_case` 對映：`l2HRate` ← `l2_h_rate`）；Inspector 顯示 `[Tooltip]` 附 GDD 旋鈕原名與單位。
- `WeaponId` 與 `WeaponType` 為 `Core` 層共用列舉（非 Content 層），`WeaponDef` 引用而非重複定義。
- `D0_reference` 預設 100 PU/s；`BU per D0 = 10`（換算錨點見 GDD D.1）——此換算比在 `WeaponBalanceConfig` 以欄位明確記錄，不得在武器程式碼中以魔數 10 硬編碼。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: 以這些 SO fixture 驗證 D₀ 等功率公式（測試）
- Story 003: `WeaponController` 注入 `WeaponDef` 的行為邏輯與 DI 佈線
- Stories 004–009: 各武器具體發射行為與 Tier-3 效果的實作
- Story 010: `LoadoutController` 使用 `WeaponDef` 引用進行插槽驗證

---

## QA Test Cases

*Smoke check — Config/Data story; no automated logic test required.*

- **AC-1**: WeaponBalanceConfig 包含所有 G.1 旋鈕且 OnValidate 有效
  - Setup: 在 Unity Editor 開啟 `Assets/_Project/Content/Weapons/WeaponBalanceConfig.asset`
  - Verify: Inspector 顯示所有 16 個旋鈕欄位（含 stagger_break_mult）；將 `H_max_normal` 改為 200（超過安全上限 150）
  - Pass condition: Inspector 出現警告訊息；恢復 100 後警告消失

- **AC-2**: WeaponDef 8 個資產存在且 weaponType 正確
  - Setup: 在 Project 視窗瀏覽 `Assets/_Project/Content/Weapons/`
  - Verify: 8 個 `.asset` 檔案可見；L1-L4 的 `weaponType = Laser`，M1-M4 的 `weaponType = Missile`
  - Pass condition: 8 個資產全數存在、`weaponType` 無錯配

- **AC-3**: WeaponDef 的 TierKnobs 陣列長度為 4
  - Setup: 開啟任一 `WeaponDef` 資產，展開 Tier Knobs 陣列
  - Verify: 陣列顯示 Element 0–3，各 index 數值可獨立編輯
  - Pass condition: 4 個 tier slot 全部存在且各自持有對應的旋鈕欄位

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**:
- Smoke check pass: `production/qa/smoke-*.md`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None
- Unlocks: Story 002, Story 003, Story 004, Story 005, Story 006, Story 007, Story 008, Story 009, Story 010
