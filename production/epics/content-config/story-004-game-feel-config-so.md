# Story 004: GameFeelConfig ScriptableObject

> **Epic**: Content 調校資料框架（ScriptableObject）
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Config/Data
> **Estimate**: M (3h)
> **Manifest Version**: 2026-07-02
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/game-feel.md`
**Requirement**: `TR-content-002`, `TR-content-001`, `TR-content-004`, `TR-content-005`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: [ADR-0003: 資料驅動調校（ScriptableObject）]
**ADR Decision Summary**: game-feel.md G.1–G.5 全部旋鈕集中於 `GameFeelConfig` SO；視覺時序旋鈕（`softened_visual_onset_max_s`、`stagger_visual_onset_max_s`）由此 SO 擁有，為 kaiju-part-system.md G.3 的單一來源。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: `Color` 型別在 Unity 序列化穩定；hitstop 以 `float` ms 存值，`Time.timeScale` 操作留給 GameFeel 系統。無 post-cutoff 風險。

**Control Manifest Rules (this layer)**:
- Required: `KaijuBreaker.Content.asmdef`；`OnValidate` 涵蓋 G.1–G.5 全部欄位安全範圍；特別驗證 `ShakeMagnitudeCap` 視為護欄上限（值不應低於最大 shake 旋鈕）
- Forbidden: 在其他 SO 複製 `softened_visual_onset_max_s`、`stagger_visual_onset_max_s` 等視覺時序旋鈕；運行期 `Time.timeScale` 呼叫（純資料容器）
- Guardrail: 視覺時序旋鈕由此 SO 統一擁有，kaiju-part-system.md G.3 為引用關係

---

## Acceptance Criteria

*From `game-feel.md` G.1–G.5, governed by ADR-0003:*

- [ ] `GameFeelConfig` C# class 位於 `Assets/_Project/Scripts/Content/`；`[CreateAssetMenu(menuName = "KaijuBreaker/GameFeelConfig")]`
- [ ] 持有 **G.1 震動旋鈕**（13 欄位）：`ShakeMagSoften`、`ShakeMagArmorStrip`、`ShakeMagL3Shockwave`、`ShakeMagM3TorpedoHit`、`ShakeMagM3HeatShock`、`ShakeMagM4Cluster`、`ShakeMagPartBreakBase`、`ShakeMagPartBreakEscalation`、`ShakeMagBossDeath`、`ShakeMagnitudeCap`（護欄，預設 24px）、`ShakeDecayRate`、`ShakeThreshold`、`ShakeAccessibilityMult`
- [ ] 持有 **G.2 慢動作旋鈕**（6 欄位）：`SlowmoPartBreakTimescale`、`SlowmoPartBreakHoldSeconds`、`SlowmoBossDeathTimescale`、`SlowmoBossDeathHoldSeconds`、`SlowmoRampRate`、`SlowmoAccessibilityMult`
- [ ] 持有 **G.3 Hitstop 旋鈕**（3 欄位）：`HitstopPartBreakMs`（預設 115）、`HitstopBossDeathMs`（預設 220）、`HitstopAccessibilityMult`
- [ ] 持有 **G.4 SOFTENED 視覺旋鈕**（6 欄位）：`SoftenedColorHue`（`Color`，#FF6600）、`SoftenedPulseFrequencyHz`、`SoftenedGlowRadiusPct`、`SoftenedVisualOnsetMaxSeconds`（**TR-content-004 單一來源**，kaiju-part-system.md G.3 參照點）、`SoftenedSfxMaxPerFrame`、`SoftenedIconEnabled`
- [ ] 持有 **G.5 閃光旋鈕**（3 欄位）：`FlashDecayRate`、`FlashMaxAlpha`、`FlashAccessibilityMult`
- [ ] 持有 **視覺時序旋鈕**（TR-content-004）：`StaggerVisualOnsetMaxSeconds`（kaiju-part-system.md G.3 擁有者）
- [ ] `GameFeelConfig.asset` 位於 `Assets/_Project/Content/GameFeel/`；全部 32 欄位以 GDD G.1–G.5 預設值填充
- [ ] `OnValidate()` 斷言：`ShakeMagnitudeCap` ≥ 所有 `ShakeMag*` 欄位中最大值；`SlowmoPartBreakTimescale` ∈ (0.0, 1.0]；`HitstopPartBreakMs` ∈ [50, 300]；`SoftenedVisualOnsetMaxSeconds` ∈ (0.0, 1.0]；`FlashMaxAlpha` ∈ [0.0, 1.0]

---

## Implementation Notes

*Derived from ADR-0003 and game-feel.md §C:*

**TR-content-004 執行**：`SoftenedVisualOnsetMaxSeconds`（0.5s）與 `StaggerVisualOnsetMaxSeconds`（0.3s）這兩個時序上限旋鈕唯一存於 `GameFeelConfig`。`kaiju-part-system.md` G.3 提及這些值作為 AC 門檻（Alpha 里程碑驗收用途），部位系統在執行期讀取此 SO 而非自持值。

`SoftenedColorHue` 建議用 `Color` 而非 `string`（`Color.HSVToRGB` 或直接 Inspector 色板更易調整），預設 RGB 等效 #FF6600（Orange）。

`ShakeMagnitudeCap` 的 `OnValidate` 需遍歷所有 `ShakeMag*` 欄位找最大值，若 cap 低於最大 shake 值即 `LogError`（GDD 明定 24px 為護欄上限，設計師不應能繞過此限制）。

`SlowmoAccessibilityMult`、`HitstopAccessibilityMult`、`ShakeAccessibilityMult`、`FlashAccessibilityMult` 這四個 a11y 旋鈕預設 1.0（完全啟用），設計師可降低以減弱效果；上限 1.0，下限 0.0（OnValidate 驗證）。

---

## Out of Scope

- [Story 001]: `WeaponBalanceConfig` 仍擁有 `stagger_duration`（STAGGER 時長），與 `StaggerVisualOnsetMaxSeconds`（視覺顯示時序上限）不同欄位
- [Story 008]: ContentRegistry 提供 `GetGameFeelConfig()` 查詢
- [Story 009]: `GameFeelConfig` fixture 工廠
- GameFeel 系統邏輯（震動、慢動作、Hitstop 的實際 `Time.timeScale` 呼叫留給 `KaijuBreaker.GameFeel` 系統）

---

## QA Test Cases

*Config/Data — manual smoke check steps:*

- **AC-1**: 全部 32 欄位以 GDD 預設值填充
  - Setup: 選取 `GameFeelConfig.asset`；逐區塊（G.1–G.5）核對
  - Verify: `HitstopPartBreakMs = 115`、`HitstopBossDeathMs = 220`、`ShakeMagnitudeCap = 24`、`SoftenedColorHue = #FF6600`、`SoftenedVisualOnsetMaxSeconds = 0.5`、`StaggerVisualOnsetMaxSeconds = 0.3`；全部 a11y 乘數 = 1.0
  - Pass condition: 所有欄位與 game-feel.md G.1–G.5 預設完全一致

- **AC-2**: ShakeMagnitudeCap 護欄 OnValidate 偵測
  - Setup: 將 `ShakeMagBossDeath` 設為 `30`（超過 cap=24）
  - Verify: Console `LogError` 提及 `ShakeMagnitudeCap` 低於 `ShakeMagBossDeath`
  - Pass condition: 修正 `ShakeMagBossDeath = 20` 或提升 `ShakeMagnitudeCap = 30` 後無錯誤

- **AC-3**: SlowmoTimescale 越界偵測
  - Setup: 將 `SlowmoPartBreakTimescale` 改為 `1.5`（超過 1.0 上限）
  - Verify: Console `LogError` 含 `SlowmoPartBreakTimescale`
  - Pass condition: 還原 ≤ 1.0 後無錯誤

- **AC-4**: SoftenedVisualOnsetMaxSeconds 確認單一來源
  - Setup: 在 IDE 搜尋 `PartSystemConfig.cs` 是否含 `SoftenedVisualOnset` 或 `StaggerVisualOnset` 欄位名稱
  - Verify: 搜尋結果為空（兩個時序旋鈕僅存於 `GameFeelConfig`）
  - Pass condition: 零重複欄位

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**: smoke check pass — `production/qa/smoke-content-config.md`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002（需確認 `PartSystemConfig` 不持有視覺時序旋鈕，以確保單一來源）
- Unlocks: Story 008, Story 009
