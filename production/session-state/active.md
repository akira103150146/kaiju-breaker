# Active Session State

*Last updated: 2026-07-01*

## Current Task
**自主推進設計管線**（導演授權:自主設計＋可直接 commit）。已完成 Phase A–D 一大批。

## Project Snapshot
- **Game**: 殲獸戰機 / KAIJU BREAKER — 科幻縱向彈幕射擊 ＋ 破壞部位狩獵養成
- **Stage**: **Pre-Production** (`production/stage.txt`) — Concept 閘門 PASS,已進架構期
- **Review mode**: lean (`production/review-mode.txt`)
- **Engine**: Unity 6.3 LTS — configured;`Assets/` 尚空,Unity 專案未建(架構期才建)
- **Platform**: 多平台（PC + 手機）
- **Visual direction**: 像素街機懷舊 (Retro Pixel Arcade)

## 已完成（本輪大批產出）
- [x] 概念原型 `break-part-feel` 試玩 → 導演判定 OK(概念閘門實質通過)
- [x] **武器系統 GDD** `gdd/weapon-system.md`(8 sidegrade、雙軌、D₀ 等功率、調校旋鈕、驗收)+ 2 阻斷缺口修補
- [x] **可破壞部位系統 GDD** `gdd/kaiju-part-system.md`(雙槽狀態機、部位類型、相鄰圖、事件契約)
- [x] **素材經濟 GDD** `gdd/material-economy.md`(5 素材、巨獸主題綁定核心、淺養成曲線)
- [x] **武器手感原型** `prototypes/weapon-feel-concept/prototype.html`(8 武器切換、雙軌、hitstop/慢動作/螢幕震動)
- [x] **3 隻巨獸** `gdd/kaiju/01-carapex · 02-lacera · 03-voltwyrm`(部位配置、彈幕模式、剋制 loadout、掉落)
- [x] **關卡系統 GDD** `gdd/stage-system.md`(手作波段池隨機重組、6 雜兵、武器莢艙掉落、3 關、四難度密度)
- [x] **系統索引/依賴圖** `design/systems-index.md`(全系統地圖 + 剩餘工作優先序)
- [x] 跨文件對齊:核心素材改「巨獸主題綁定」;registry 註冊 5 素材;部位數上限放寬到 8
- [x] **難度系統 GDD** `gdd/difficulty-system.md`(四階密度縮放、跨難度 TTB/輸出不變驗收)
- [x] **遊戲手感 GDD** `gdd/game-feel.md`(hitstop/慢動作/螢幕震動 juice — 導演指定;#FF6600 軟化簽章解阻斷 #4;reduce-motion 開關;引擎 API 對齊 Unity)
- [x] **HUD/UI GDD** `gdd/hud-ui-system.md`(world-space 部位血條、三元介面畫面、手機安全區)
- [x] **輸入系統 GDD** `gdd/input-system.md`(Sky Force 拖曳偏移觸控、鍵鼠、手柄;觸控手感待專屬原型)
- [x] **彈幕系統 GDD** `gdd/bullet-system.md`(物件池、彈幕 DSL、DOTS/Burst、單點判定、可讀性護欄)
- [x] **展示原型** `prototypes/vision-slice/prototype.html`(完整循環:loadout→雜兵→Boss 破部位→結算;3 Boss 可選;全 juice)— 導演確認「很棒」;修好 DPI/Boss進場/時序/滑入 bug
- [x] **美術聖經** `design/art-bible.md`(兩大鐵律、~35 色調色盤冷/暖家族、像素規格、可讀性、3 巨獸剪影)
- [x] **存檔/元進度系統 GDD** `gdd/meta-progression-system.md`(永久 vs 每輪、武器所有權=拾取永久解鎖、JSON schema、永不丟失)
- [x] **全域一致性複審**(進 Pre-Production 閘門)→ 初判 CONCERNS,揪出經濟系統阻斷叢集(B1/B2/B3);已全部修正
- [x] **經濟一致性修正**:核心掉落統一為 Option A(巨獸所有部位皆掉主題核心);MVP 改 L1+M2(解 CARAPEX 單獸升級死結);break_quality 命名統一。技術系統/難度/覆蓋複審全部 PASS
- [x] **Concept→Pre-Production 閘門 PASS** → 階段推進至 Pre-Production
- [x] **技術架構 + 5 ADR** `docs/architecture/`(主藍圖 + ADR-0001 彈幕後端 hybrid DOTS/Mono〔Proposed,待效能原型〕/ 0002 事件匯流排 / 0003 SO 資料驅動 / 0004 存檔 / 0005 asmdef 模組)。technical-preferences ADR log/套件/禁用模式已登錄
- [~] **Unity 專案骨架建置中**(背景;Assets/_Project 結構 + asmdef + manifest,依 ADR-0005)

## Pre-Production 下一步
- Unity 骨架完成 → commit
- **效能原型驗證 ADR-0001**(手機 1000 彈@60fps、0 GC)→ LOCK 彈幕後端
- `/test-setup` 測試框架 + CI;`/create-control-manifest` 程式規則sheet
- `/create-epics` → `/create-stories`(把 GDD 拆成可實作 story)
- `/vertical-slice` 用真引擎驗證完整循環

## Key Decisions
- 5 pillars locked;武器雙軌骨架(雷射蓄熱軟化→飛彈破甲擊破)
- **核心素材 = 巨獸主題綁定**(甲殼→core_carapace / 肢體→core_limb / 能量→core_energy),shard 通用、essence 為 full-clear → MH 式「農特定巨獸」黏著
- 3 隻巨獸各偏好不同 loadout(CARAPEX→L2×M3 / LACERA→M1 追蹤 / VOLTWYRM→L4 穿透＋L3×M2),用內容驗證 sidegrade
- 難度四階只縮放彈幕密度/雜兵數,絕不鎖內容

## 下一步優先序（見 systems-index.md §3）
- **P0** VFX/SFX + HUD 的「軟化提示可讀性」(阻斷 #4;也是導演要的 juice)
- **P0** 難度系統 GDD(被全面引用但缺彙整)
- **P1** 輸入系統 GDD(觸控彈幕手感 = 概念未解風險)、HUD/UI 完整 GDD
- 之後:`/review-all-gdds` 全域複審 → `/gate-check` 或 `/vertical-slice`

## Open Follow-ups / 原型待驗
- M3 6× 是否秒殺(TTB 矩陣)、軟化狀態 0.5s 可辨性 → 用武器手感原型測
- LACERA 農刷率是否過高(Vertical Slice 監測)
- Stage 1 是否納入 L3(教學武器池開放問題)

## Biggest Risk
武器平衡(等價而異性格)— D₀ 等功率 + 8×8 無主導 loadout 驗收鎖住,3 隻巨獸各偏好不同 loadout 提供內容驗證,仍需原型/實測數據校準。
