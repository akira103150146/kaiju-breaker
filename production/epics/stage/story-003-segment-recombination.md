# Story 003: 波段池隨機重組（加權 Fisher-Yates + No-Repeat Window）

> **Epic**: 關卡系統與 Run 流程
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/stage-system.md`
**Requirement**: `TR-stage-001`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0005: 專案結構與組件邊界 (primary); ADR-0003: 資料驅動調校 (secondary)
**ADR Decision Summary**: `SegmentRecombinator` 為純 C# 類別（無 `MonoBehaviour`），建構子接收 `StageDef`（SO）、當前難度階（`int`）、`System.Random`（測試注入用），確保演算法可在 EditMode 以固定種子決定性驗證。`StageDef` / `SegmentDef` 攜帶波段池、抽取數、`difficulty_weight`、`min_difficulty_tier`——純資料驅動。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: 純 C# 演算法，不使用 Unity API；`System.Random` 注入（不用 `UnityEngine.Random`，以確保測試決定性）。無 post-cutoff 風險。

**Control Manifest Rules (Feature — Stage)**:
- Required: `MUST` 波段隨機重組；N 個波段依 `difficulty_weight` 升序排列
- Required: `MUST NOT` 引用其他 Feature 系統；`MUST NOT` 硬編碼任何波段池旋鈕
- Guardrail: 所有 public 方法可以假 SO fixture + seeded `Random` 做單元測試（決定性）

---

## Acceptance Criteria

*From GDD `design/gdd/stage-system.md` §D.1, §D.3, §L.1:*

- [ ] 輸出序列：固定引入段 → 抽取 N 個升階波段（`difficulty_weight` 升序）→ 固定前頭目喘息 → 頭目競技場
- [ ] No-repeat window = 1：上一輪最後執行的波段 ID 排除於本輪抽取候選（池大小 ≤ N 時跳過此步）
- [ ] 難度階過濾：移除 `min_difficulty_tier > currentTier` 的波段；若過濾後剩餘 < N，放寬至全池
- [ ] 對剩餘波段執行 Fisher-Yates 隨機洗牌（注入 `System.Random`）；取前 N 個
- [ ] 取出的 N 個波段依 `difficulty_weight` 升序排列（最輕最早）
- [ ] 若池大小 ≤ N，跳過 no-repeat 步驟（確保能抽滿）
- [ ] 自動化測試：100 次隨機種子生成驗證——抽取數量正確、排序正確、no-repeat 生效、難度過濾正確（`L.1` 阻斷標準）
- [ ] `StageDef` SO 提供：`pool[]`（`SegmentDef[]`）、`segmentDrawCount`、`stageId`
- [ ] `SegmentDef` SO 提供：`segmentId`（string）、`difficultyWeight`（int 1–5）、`minDifficultyTier`（int 1–4）

---

## Implementation Notes

*Derived from ADR-0005 and ADR-0003 Implementation Guidelines:*

- `SegmentRecombinator` 放 `Assets/_Project/Scripts/Stage/SegmentRecombinator.cs`（`KaijuBreaker.Stage.asmdef`）。
- 建構子：`SegmentRecombinator(StageDef stageDef, int currentDifficultyTier, System.Random rng)`。
- 主要方法：`SegmentSequence Recombine(IReadOnlyList<string> lastRunPlayedSegmentIds)`。
- 回傳 `SegmentSequence`：`IntroSegment`, `IReadOnlyList<SegmentDef> EscalatingSegments`, `PreBossLullSegment`, `BossArenaRef`（全為不可變值型別 / record）。
- 演算法嚴格按 GDD §D.1 六步驟實作：
  1. 若 `pool.Count > N`：從候選移除 `lastRunPlayedSegmentIds` 匹配的段落
  2. 移除 `minDifficultyTier > currentDifficultyTier` 的段落；若剩餘 < N，還原至全池
  3. Fisher-Yates 洗牌（`for i from n-1 to 1: swap(i, rng.Next(0, i+1))`）
  4. 取前 N
  5. 依 `difficultyWeight` 升序排列（`OrderBy`）
  6. 組裝 `SegmentSequence`
- `StageDef.IntroSegment` 與 `PreBossLullSegment` 為固定欄位（`SegmentDef` 引用，設計師在 Inspector 指定），不參與隨機抽取。
- 測試以固定種子 `new System.Random(seed)` 注入；100 次測試迴圈以不同種子驗證統計覆蓋。
- 所有 public 方法具 doc comment；類別無 Unity 依賴（純 C#）。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: 波段內的波次執行（WaveSpawner 消費 SegmentRecombinator 輸出）
- Story 004: 莢艙保底追蹤（SegmentDef 的 `pod_pool_preference` 由 Story 004 讀取）
- Story 006: 前頭目喘息時序（RunController 從 SegmentSequence 取得 PreBossLull 段落並計時）
- Story 007: Stage 1 第一段落強制 Primary Pod 覆寫（Onboarding 邏輯覆寫重組輸出）

---

## QA Test Cases

*Logic story — EditMode 自動化測試規格：*

**AC-1**: 基本重組 — 抽取數量、排序、no-repeat
  - Given: `StageDef` pool=5 段（weights: 1,2,2,3,3），draw=3，`lastRunPlayedSegmentIds=["s1_02"]`，seed=42
  - When: `Recombine(lastRunPlayedSegmentIds)`
  - Then: 結果含 3 個升階段落；無 "s1_02"；`EscalatingSegments[0].difficultyWeight ≤ [1].difficultyWeight ≤ [2].difficultyWeight`
  - Edge cases: 若隨機結果只剩 weights 相同，允許同 weight 相鄰

**AC-2**: 難度階過濾（D1 只取 minDifficultyTier=1 的段落）
  - Given: pool 含 3 段：minTier=[1,1,3]；currentTier=1；draw=2
  - When: `Recombine([])`
  - Then: 結果只含 minTier=1 的 2 段；minTier=3 的段不出現
  - Edge cases: 若過濾後僅剩 1 段而 draw=2，放寬至全池（minTier=3 段可出現）

**AC-3**: 池大小 ≤ N 時跳過 no-repeat
  - Given: pool=3 段，draw=3，`lastRunPlayedSegmentIds=["s1_03"]`
  - When: `Recombine(lastRunPlayedSegmentIds)`
  - Then: 結果 3 段（包含 "s1_03"，no-repeat 被跳過，確保抽滿）

**AC-4**: 100 次決定性測試
  - Given: 同一 `StageDef` + seed=7
  - When: `Recombine` 執行 100 次（重建相同 `System.Random(7)`）
  - Then: 每次輸出完全相同（決定性）

**AC-5**: 100 次不同種子統計覆蓋
  - Given: `StageDef` pool=5，draw=3；種子 0–99
  - When: 各執行一次 `Recombine([])`
  - Then: 每次 draw count=3；每次 weights 非遞減；pool 中每個段落在 100 次中至少出現 1 次（均勻性粗驗）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/_Project/Tests/Stage/segment_recombination_test.cs` — EditMode 單元測試，必須全部通過 【BLOCKING】

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001（`RunController` 消費 `SegmentSequence`；Story 001 DONE 後才能端對端測試，但單元測試本身不依賴）
- Depends on: content-config epic（`StageDef`、`SegmentDef` SO 類別定義於 `KaijuBreaker.Content`）
- Unlocks: Story 004（SegmentDef 的 `pod_pool_preference` / `elite_wave_index` 供菁英掉落讀取）；Story 006（SegmentSequence 含 PreBossLull 段落）；Story 007（Stage 1 第一段落覆寫邏輯建立在此之上）
