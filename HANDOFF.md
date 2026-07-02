# Kaiju-Breaker — 工作進度存檔 / Handoff

**最後更新:2026-07-02**
**用途:** 跨 session / 存檔用。新 Claude Code session 一開,說「讀 HANDOFF.md 照做」即可接手。

---

## 一句話狀態

kaiju-parts 001–005 的**純邏輯層 EditMode 測試全綠**;下一個關卡是 **ADR-0001 子彈效能 spike**(過了解鎖最多後續 story)。spike 的程式碼已備好,Unity MCP 也已接上,就差**在中階手機上實測 FPS/GC**。

---

## 一、到目前為止做了什麼

1. **kaiju-parts 001–005 — EditMode 測試全綠** ✅
   純函式邏輯層已驗證正確(熱量、軟化、擊破、掉材料等的計算)。
   *注意:EditMode 是隔離的純邏輯測試,涵蓋不到效能、手感、視覺、跨系統串接。*

2. **ADR-0001 子彈效能 spike — 程式碼已建立** ✅(尚未量測)
   位置:`Assets/_Project/Scripts/BulletSim/PerfSpike/`(獨立 asmdef,量完可整包刪)
   - `BulletVelocity.cs` — IComponentData(2D 速度)
   - `BulletPerfSpikeConfig.cs` — 旋鈕(`Count` 預設 1000、`Speed`、`Bound`、`Enabled`)
   - `BulletSpawnSystem.cs` — 進 Play 自動生 1000 個 entity(純程式,無 prefab / SubScene / Baker)
   - `BulletMoveSystem.cs` — Burst + IJobEntity 平行移動,**每幀 0 GC**
   - `README.md` — 跑法 + 量測步驟 + 渲染版升級路徑
   設計:**純模擬版**(不渲染),用現有套件就能編譯執行,對準 ADR-0001 最關鍵的「0 GC + job 吞吐」。

3. **Unity MCP(MCP for Unity)— 已安裝並驗證 bridge 正常** ✅
   - 套件:`https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.0.0`
   - Bridge 已驗證在 **`http://127.0.0.1:8080/mcp`** 服務,`mcp-for-unity-server v3.4.2`,MCP 握手成功。
   - Claude Code 設定已寫入 `~/.claude.json`(scope 到 kaiju-breaker 專案,http transport)。
   - **未完成的一步:** 目前這個 session 沒載到 Unity 工具(見第三節)。

---

## 二、接下來要做的事(優先序)

| # | 任務 | 為何重要 | 誰能做 |
|---|------|---------|--------|
| 1 | **ADR-0001 效能 spike 實測** | 過了才能把 ADR-0001 Proposed→Accepted,**解鎖 8 個 bullet-sim + 3 個 kaiju 遭遇戰 story**。目標:~1000 子彈 @60fps、**每幀 0 GC**,量在**中階手機**上。 | 桌面驗證可交給 Unity MCP;**手機實測必須你來** |
| 2 | **觸控手感 spike**(input/story-001) | Sky-Force 式相對拖曳 + `touch_follow_lerp`,憑手感調,真機。 | 你(手感是人判斷) |
| 3 | **Story 006 — 辨識度視覺** | SOFTENED 橘紅發光要在 `PartSoftened` 後 ≤0.5s 出現、5 位素人 ≥80% 辨識率、D4 高彈幕不被遮蓋 >50%。純 Visual/Feel。 | 你(玩測 + 截圖/錄影) |
| 4 | **跨系統 PlayMode 串接** | 「雷射→熱量→軟化→飛彈擊破→掉材料」的整條串接,要等 Weapons/Stage/BulletSim 做出來。 | 之後實作 |

**建議先做 #1** —— 一過解鎖最多工作。

> **另有設計 backlog:** 使用者 2026-07-02 提了 7 點遊戲性改進意見(彈幕多元化、集氣改直向、子彈/武器分級、背景捲動、打擊感分級、怪物數值分級)+ 一版劇情草案(可愛科幻機器人妹子打魔物,參考《劍星》)。整理在 `design/feedback/2026-07-02-改進意見與劇情草案.md`(Obsidian 同步)。**劇情草案待使用者確認**;這些是 backlog,不影響上面效能 spike 的優先序。

### ADR-0001 spike 實測步驟(摘要,詳見 PerfSpike/README.md)
1. 跑 spike 前:給 `C:\Game\kaiju-breaker` + Unity 加 Windows Defender 例外、刪 `Library/BurstCache`(避開 Burst 4551 誤報)。
2. `Jobs → Burst → Enable Compilation` 打勾(量測時必開)。
3. 開空場景按 Play → `Window → Entities → Hierarchy` 應見 ~1000 entity 在移動。
4. Profiler:**GC Alloc 欄 = 0 B**、`BulletMoveJob` 落在 worker thread。
5. **Build 到中階手機**(Development Build + Autoconnect Profiler),看**持續 FPS ≥ 60、GC/frame = 0**;把 `Count` 往 2000/4000 推,記下崩潰的天花板。
6. 把數字(機型、發數、FPS、GC/frame、job 主執行緒成本)寫進 ADR-0001,達標就改 Accepted。

---

## 三、Unity MCP:如何讓新 session 真的用到

Bridge 已 OK,但 MCP 工具**只在 session 啟動時載入一次**,且那筆設定 **scope 在 kaiju-breaker 專案**。要讓 Claude 能直接操作 Unity:

1. 保持 **Unity 開著**(bridge 別關,8080 要一直在)。
2. **在 kaiju-breaker 底下開新 session:**
   ```
   cd C:\Game\kaiju-breaker
   claude
   ```
3. 新 session 跑 `claude mcp list`,應見 **UnityMCP ✔ Connected**。
4. 之後 Claude 的工具清單會多出 `manage_editor`(play mode 控制)、`read_console`、`manage_scene`、`manage_asset` 等,能直接驅動桌面 Editor。

### 貼給新 session 的接手指令
> 讀 `Assets/_Project/Scripts/BulletSim/PerfSpike/README.md`。這是 ADR-0001 的子彈效能 spike。用 Unity MCP 工具幫我:
> 1. `read_console` 確認目前**沒有編譯紅字**(有的話先回報)。
> 2. 開或新建一個空場景(含 Camera + Directional Light),進 **Play mode**。
> 3. 打開 Entities Hierarchy 確認有 **~1000 個 entity** 在跑、會移動。
> 4. 截一張 Game view + 回報 Console 有無錯誤。
> 目標:驗證純模擬層能跑起來、Console 乾淨;手機實機的 FPS/GC 我自己另外用真機量。

---

## 四、界線 —— Claude(桌面 Editor)做不到、必須你來的部分

- **中階手機的 FPS/GC 實測** — MCP 只能驅動桌面 Editor,真機要你 build。(0 GC 的 *bug* 在 Editor Profiler 就抓得到;真機主要確認絕對幀率。)
- **觸控手感** — 本質是人手判斷。
- **Story 006 辨識度** — 要 5 位素人肉眼測 ≥80%。
- **記憶不跨 session** — 兩個 session 的 auto-memory 是不同資料夾;接手靠「讀這份 HANDOFF + PerfSpike/README」,不是靠記得。

---

## 五、環境備忘

- Unity **6000.3.0f1(Unity 6.3)**、DOTS:entities 1.4.7 / burst 1.8.29 / mathematics 1.3.3(manifest 已有);**尚未裝** `com.unity.entities.graphics`(要看到子彈才需要,見 PerfSpike/README)。
- Git:目前處於 **detached HEAD**,commit 前先 `git switch -c <branch>`,並連新腳本的 `.meta` 一起 commit(否則他人拉下來 GUID 對不上)。
- PerfSpike 的 `.meta` 檔:Unity 首次匯入時自動產生。
