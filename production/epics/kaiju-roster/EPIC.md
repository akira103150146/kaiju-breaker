# Epic: 巨獸內容（CARAPEX / LACERA / VOLTWYRM）

> **Layer**: Feature
> **GDD**: design/gdd/kaiju/01-carapex.md · design/gdd/kaiju/02-lacera.md · design/gdd/kaiju/03-voltwyrm.md
> **Architecture Module**: `KaijuBreaker.Content`（`KaijuDef` 資產 + `EmitterPatternSO`）
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories kaiju-roster`

## Overview

本 Epic 交付首批 3 隻巨獸的內容實例化——每隻是 `Weapons`+`KaijuParts`+`Economy`+`BulletSim` 的資料組合，以 `KaijuDef`（部位組成/相鄰圖/掉落表/覆寫值）+ `EmitterPatternSO`（各頭目彈幕模式）表達，無需新增程式。CARAPEX（甲殼/教學，MVP 唯一必需）示範加熱→引爆與 ARMORED 護甲閘門；LACERA（肢體/移動）示範動態 `world_position` 追蹤命中；VOLTWYRM（能量/彈幕）示範 L4 縱列穿透與雙護盾。此 Epic 的實現高度依賴 BulletSim（ADR-0001）的模式表達力驗收。

> **tr-registry.yaml 尚未正式化** — 以下 TR-ID 由 kaiju/01–03 §10 驗收標準推導。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0003: 資料驅動調校 | 巨獸以 `KaijuDef` + `EmitterPatternSO` 資產表達，設計師 Inspector 撰寫，無需程式 | LOW（內容資產）；模式表達力實依賴 ADR-0001（Proposed）BulletSim 驗收 |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-kaiju-001 | CARAPEX AC-01 教學循環感知 ≥70%；首次大顎破壞 <3 分；5 分內發現 L3 剝甲 | ❌ 無 ADR（design GDD，playtest） |
| TR-kaiju-002 | AC-02 / VOLTWYRM 10.2 ARMORED 護甲閘門（dorsal_cannon、雙護盾）依部位系統規則正確 | ADR-0003 ✅（資料）+ ADR-0002（事件） |
| TR-kaiju-003 | AC-03 / 10.5 BOSS_CORE 勝利條件：核心破壞觸發勝利、事件順序保證 | ADR-0002 ✅ |
| TR-kaiju-004 | AC-04 / L 10.5 掉落表主題正確：CARAPEX→core_carapace、LACERA→core_limb、VOLTWYRM→core_energy | ADR-0003 ✅ |
| TR-kaiju-005 | AC-05 / 10.4 彈幕可讀性：各巨獸暖色彈幕 + 判定點辨識 ≥80%（D4 SOFTENED/弱點仍可辨） | ❌ 無 ADR（design GDD）；對齊 ADR-0001 護欄 |
| TR-kaiju-006 | AC-06 / LACERA 10.6 L4 垂直對齊窗口存在性（每 Phase1 ≥8 次；關卡評審確認） | ❌ 無 ADR（design GDD + 關卡評審） |
| TR-kaiju-007 | LACERA 10.1 移動部位 `world_position` 每幀更新；M1 追蹤動態位置；破壞位置正確 | ADR-0002 ✅（IPartStateQuery）+ design |
| TR-kaiju-008 | AC-07 / VOLTWYRM 10.5 階段轉換：部位破壞驅動模式切換、單向不可逆 | ADR-0003 ✅ + design |
| TR-kaiju-009 | 三頭目全部彈幕模式可由 `EmitterPatternSO` 撰寫無需新 shape（bullet 11.3 表達力） | ADR-0003 ✅ / ADR-0001 ⚠️（依 BulletSim 驗收） |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/kaiju/01-carapex.md`, `02-lacera.md`, `03-voltwyrm.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs with sign-off in `production/qa/evidence/`

## Next Step

Run `/create-stories kaiju-roster` to break this epic into implementable stories.
