# ADR-0006：UI 框架選擇 (UI Framework Selection — UGUI vs UI Toolkit)

- **Title**: 三層 UI 採不同渲染策略：世界座標部位血條以 SpriteRenderer 實作（非 UI 系統）；戰鬥中 HUD 與 Meta 畫面均採 UGUI；不使用 UI Toolkit（避免在小規模 Indie 專案中承擔 Unity 6.3 執行期尚待驗證的風險）
- **Status**: **Accepted**
- **Date**: 2026-07-02
- **Deciders**: UI Specialist
- **相關**: `hud-ui-system.md`、`technical-preferences.md`、ADR-0001（BulletSim — 彈幕在播放區渲染，禁止 Canvas 介入）、ADR-0002（事件匯流排 — UI 透過 `IPartStateQuery` 讀狀態、訂閱匯流排事件）、ADR-0003（ScriptableObject 設定 — UI config 存放於此）、ADR-0005（`UI` assembly 隔離，僅依賴 `Core`）

---

## Context（技術脈絡與問題）

殲獸戰機的 UI 橫跨三種本質不同的使用情境，每種對渲染策略的要求截然不同：

**（a）世界座標部位血條（World-Space Part Bars）**
HEAT 條與 BREAK 條以 3 px 高細條貼附在 Boss 各部位 Sprite 上下，隨部位在世界座標移動。後期 Boss 最多 8 個部位同時顯示 16 條（`hud-ui-system.md` E.2/J.1）。這些元素存在於彈幕播放區——此區已由 DOTS/Burst SpriteRenderer 批次渲染主導（ADR-0001）；把 Canvas 引入此區會建立額外的 Canvas dirty 路徑，並與 BulletSim 渲染路徑產生交互作用。

**（b）戰鬥中 HUD（In-Combat Screen-Space HUD）**
元素少（主武器槽 C 區、副武器彈匣 D 區、素材計數 B 區、關卡進度 A 區、判定點 P0），但更新頻繁——L3 蓄力條與副武器換彈條每幀更新。像素藝術風格要求整數倍縮放（×1/×2/×3/×4），非整數縮放模糊像素是不可接受的視覺破壞（`hud-ui-system.md` H.3）。

**（c）戰鬥外 Meta 畫面（Meta Screens）**
Loadout Hub、永久升級畫面、難度選擇畫面。複雜版面（武器卡片網格、升級費用清單、難度卡片）、資料驅動（武器 Tier、素材庫存即時同步）、需支援完整手把導航、三段文字縮放（100/125/150%）、三種色盲模式、Reduce-Motion 廣播、手機 Safe Area 與拇指安全區（`hud-ui-system.md` F/H/I）。

**技術約束（來自 `technical-preferences.md` 與現有 ADR）：**
- 目標平台：PC (Steam) + Mobile (iOS/Android)，60 FPS，≤200 draw call
- URP 2D Renderer + Pixel Perfect Camera（整數倍縮放必須保持）
- 輸入：鍵盤/滑鼠、Touch（Full）、Gamepad（Partial），三者同時支援
- UI assembly（`KaijuBreaker.UI`）只依賴 `Core`（ADR-0005）；UI 透過 `IPartStateQuery` 讀狀態、訂閱匯流排事件更新（ADR-0002）
- 零 GC 穩態目標（ADR-0001/0002）；UI 不得在每幀分配 managed 記憶體

**框架比較（Unity 6.3 現實）：**

| | **UGUI（Canvas）** | **UI Toolkit（UXML/USS）** |
|---|---|---|
| 世界座標 | World Space Canvas（成熟）| RenderTexture 繞路（工作流摩擦大）|
| 畫面空間 | 成熟、所有功能齊備 | Unity 6 持續演進 |
| 手把導航 | EventSystem + Selectable（完整成熟）| [需查證 Unity 6.3 完整度] |
| Mobile Touch | 成熟 | [需查證 Unity 6.3 完整度] |
| 像素整數縮放 | `Canvas.pixelPerfect` + PixelPerfectCamera ref | 需 USS 自行處理，無內建等效 |
| 資料綁定 | 程式碼手動；無內建 runtime binding | Runtime binding（`INotifyBindablePropertyChanged`）成熟 |
| CSS 主題（色盲模式）| 需程式碼切換 | USS 變數替換（更優雅）|
| 點陣字型支援 | TMP — 已驗證 | Font Asset — [需查證 Unity 6.3 像素字型品質] |
| GC 壓力 | 開發者完全可控 | Binding runtime GC 特性 [需查證] |

---

## Decision（決策）

### 策略總覽

採**三層分治策略**，根據各層的本質選擇最適工具：

```
(a) World-Space Part Bars  →  SpriteRenderer（非 UI 系統，無 Canvas）
(b) In-Combat HUD          →  UGUI（Screen Space - Camera，雙 Canvas 分層）
(c) Meta Screens           →  UGUI（Screen Space - Overlay，screen stack 管理）
                               不使用 UI Toolkit
```

---

### 1. （a）世界座標部位血條：SpriteRenderer（非 UI 系統）

**決定**：HEAT 條與 BREAK 條使用 **SpriteRenderer + MaterialPropertyBlock**，掛載於各部位 Prefab 的子物件，完全不引入 Canvas。

**理由：**
- 彈幕播放區由 DOTS/Burst SpriteRenderer 批次主導（ADR-0001）；World Space Canvas 即使很小，也會製造額外的 Canvas dirty 路徑，打破 URP 2D Sprite Batcher 的批次邏輯。
- 3 px 高的像素條完全不需要 UI 系統的佈局引擎、觸控事件或 EventSystem——只需填充比例（`_FillAmount` shader property）與可見性控制。
- SpriteRenderer 在 URP 2D Sprite Batcher 下與彈幕 Sprite 共享 atlas，draw call 增量極小；後期 Boss 16 個 SpriteRenderer 遠比 8 個 World Space Canvas（各自建立 Canvas batch）更輕量。
- ARMOR 遮罩（`armor_mask_opacity = 0.65`）、弱點細白外框、STAGGERED 倒計時淡出，均以獨立 SpriteRenderer 圖層 + MaterialPropertyBlock 實作，無需 UI。

**實作細節：**
- 每個部位 Prefab 包含以下子 SpriteRenderer 組合（由 `PartBarController` MonoBehaviour 管理）：
  - `HeatBar`：1 px 高基礎 Sprite + `BarFill.shader`，`_FillAmount` 對應 `H_current/H_max`
  - `BreakBar`：同上，`_FillAmount` 對應 `B_current/B_max`
  - `ArmorMask`：半透明藍灰 Sprite，`alpha = armor_mask_opacity`；`on_armor_stripped` 後停用
  - `WeakFrame`：空心外框 Sprite，`alpha` 隨 `stagger_timer` 線性衰減
- 血條寬度等於部位 Sprite 寬度：`PartBarController.ResizeBars()` 讀取父 SpriteRenderer 的 `bounds.size.x` 並設定 localScale.x。
- Sorting Layer：`PartsUI`（置於 `Bullets` layer 之上、`GameFeel` layer 之下）；具體層級定義由美術總監確認並寫入 Unity Project Settings。
- 事件來源：`PartBarController` 訂閱 `IEventBus`（`PartSoftened`、`PartStaggered`、`PartBroke`、`ArmorStripped`）並呼叫 `IPartStateQuery` 讀取 `H_current`/`B_current`，符合 ADR-0002 事件架構。

---

### 2. （b）戰鬥中 HUD：UGUI（Screen Space - Camera Canvas）

**決定**：戰鬥中 HUD 使用 UGUI，分為三個 Canvas，共用同一 SpriteAtlas（`HUD_Atlas`）：

| Canvas | 內容 | 更新頻率 | `sortingOrder` |
|--------|------|---------|---------------|
| `HUD_Static` | 關卡標識框、區域框架裝飾 | 場景載入後靜止 | 10 |
| `HUD_Dynamic` | L3 蓄力條、換彈條、彈頭 pip、素材計數、Boss HP 條、分數 | 每幀或每事件 | 11 |
| `Hitbox_Overlay` | 判定點（1 px 白點，P0） | 靜止，但 z-order 絕對最高 | 99 |

**理由：**
- 兩 Canvas 分離（Static/Dynamic）確保每幀 dirty 只觸發 `HUD_Dynamic` rebuild，`HUD_Static` 不受影響——UGUI Canvas 粒度優化的標準模式。
- `Hitbox_Overlay` 獨立 Canvas 設 `sortingOrder = 99` 確保判定點在全白閃光覆蓋層（`flash_max_alpha = 0.85`）之上，滿足 `hud-ui-system.md` D.5 與 M.1 的硬性約束。
- UGUI `Canvas.pixelPerfect = true`（配合 `PixelPerfectCamera` Camera 引用）是 Pixel Perfect Camera 的已知整合路徑，確保整數倍縮放。[需查證 — 見 Consequences 版本注意事項 #1]
- Pip 狀態（`Image.enabled`）、換彈條（`Image.fillAmount`）、蓄力條（`Image.fillAmount`）：無每幀 string 分配，維持零 GC。
- 素材計數彈跳與「+N」浮動文字由匯流排事件驅動（ADR-0002），非每幀輪詢；浮動文字 GameObject 池化（object pool in `KaijuBreaker.UI`），避免 GC。

**手機 Safe Area：**
`SafeAreaFitter` MonoBehaviour 套用 `Screen.safeArea` 到 `HUD_Static` 與 `HUD_Dynamic` 的 root RectTransform，確保劉海/圓角不遮蔽 A/B/C/D 區 HUD 資訊（`hud-ui-system.md` H.2）。

---

### 3. （c）Meta 畫面：UGUI（Screen Space - Overlay）

**決定**：Loadout Hub、永久升級畫面、難度選擇畫面（含結算畫面）均以 UGUI 實作，各畫面一個獨立 Canvas Prefab，由 `UIScreenManager`（screen stack）管理 push/pop。不使用 UI Toolkit。

**UIScreenManager（screen stack）：**
獨立 MonoBehaviour（`KaijuBreaker.UI` assembly），提供：
- `Push(IScreen screen)` — 開啟新畫面於頂層
- `Pop()` — 返回前一畫面
- `Replace(IScreen screen)` — 換掉當前畫面
- `ClearTo(IScreen screen)` — 清空 stack 並顯示目標畫面

各 Canvas Prefab 實作 `IScreen`（含 `OnShow()` / `OnHide()` / `OnFocus()` 生命週期）。Back/B button/Escape 永遠呼叫 `Pop()`。開啟新畫面時，`EventSystem.SetSelectedGameObject()` 設定至最邏輯的首個 Selectable。

**理由：**
- **手把導航成熟度**：Meta 畫面需要複雜方向鍵導航（武器卡片網格 4×2、費用清單、難度卡片排）。UGUI `Selectable.navigation`（`Navigation.mode = Explicit` 自訂路由）+ `EventSystem` 在 Unity 6.3 完全成熟，文件完整。UI Toolkit 執行期手把導航在 Unity 6.x 持續演進，此規模 Indie 專案不應在此承擔未知風險。[需查證 — 見 Consequences 版本注意事項 #2]
- **觸控支援**：UGUI 觸控在 iOS/Android 完全成熟；武器卡片 tap 選擇、升級按鈕、手機底部抽屜 Modal 佈局，UGUI RectTransform + `PointerClickHandler` 是已知路徑。[需查證 UI Toolkit 對等性 — 見注意事項 #3]
- **像素字型**：Meta 畫面使用像素點陣字型（美術總監確認）；TextMeshPro + UGUI 是已驗證組合，PixelPerfectCamera 與 TMP 整合有充分社群資源。UI Toolkit Font Asset 對像素字型的支援品質尚待驗證。[需查證 — 見注意事項 #4]
- **小規模專案一致性**：Meta 畫面僅 3–4 個，沒有足夠規模讓 UI Toolkit 的學習成本回收；全 UGUI 讓 `KaijuBreaker.UI` assembly 開發者只需精通一套系統。
- **色盲模式與文字縮放**：UI Toolkit USS 變數替換更優雅，但此規模下 UGUI 程式碼方案（ScriptableObject 儲存三套 Color/Sprite 設定 + `reduce_motion_changed` 廣播模式的色盲等效事件）可接受。

---

## Alternatives Considered（替代方案）

### A. UI Toolkit（全部使用，含 HUD 與 Meta 畫面）

**優點**：USS 變數讓三種色盲主題一行切換；runtime binding 讓升級費用/庫存同步清晰；UXML 版面宣告式、維護性高；CSS 文字縮放乾淨。

**缺點**：
- 世界座標 part bars：仍需 RenderTexture 繞路，16 條獨立 RenderTexture 開銷不可接受。
- 手把導航 [需查證] + Mobile Touch [需查證]：兩個核心需求在 Unity 6.3 的成熟度需要 spike 驗證；未驗證前不應採用為 Accepted 生產方案。
- 像素整數縮放：需 USS 自行重建 `pixelPerfect` 等效邏輯，有落實風險。
- 點陣字型導入流程與品質 [需查證]。
- 全案採 UI Toolkit 引入不必要學習曲線；3 個 Meta 畫面的複雜度不足以回收成本。

**否決**：核心輸入需求（手把/Touch）的 Unity 6.3 成熟度不確定，且小規模專案規模效益不足。若 Unity 7+ 大幅成熟、專案規模擴大，Meta 畫面是最自然的遷移入口點（見 Reversibility）。

### B. UGUI World Space Canvas 用於部位血條（取代 SpriteRenderer）

**優點**：與 HUD 同一工具組；`Image.fillAmount` 語義清晰；Canvas 作為部位子物件自動跟隨位移。

**缺點**：
- 每個部位一個 World Space Canvas = 8 個 Canvas 在彈幕播放區 = 8 條額外的 Canvas batch 路徑，干擾 URP 2D Sprite Batcher。
- `Image.fillAmount` 每幀更新觸發 Canvas dirty；後期 Boss 16 個 bar 每幀各觸發 = 16 Canvas rebuild，在彈幕最密集（GPU 最需要餘量）的時段增加 CPU 開銷。
- 增加 draw call 壓力，侵蝕 ≤200 預算。
- SpriteRenderer 在相同需求下完全夠用，且更輕量——引入 Canvas 無額外收益。

**否決**：效能確定性低於 SpriteRenderer；draw call 開銷超出收益。

### C. 混合：UI Toolkit Meta 畫面 + UGUI HUD

**優點**：Meta 畫面使用 USS 主題切換（色盲模式優雅）+ runtime binding（升級費用同步）；HUD 保持 UGUI 像素縮放確定性。

**缺點**：
- 工具組分裂：`KaijuBreaker.UI` assembly 開發者需精通兩套系統、兩種事件模型（UIElements vs EventSystem/UnityEvent）、兩種文字渲染（Font Asset vs TMP）。
- UGUI HUD → UI Toolkit Meta 畫面的焦點轉移 [需查證]：`EventSystem` 與 UI Toolkit `PanelSettings` 的焦點協調在 Unity 6.3 是否已明確定義，需要 spike。
- 3 個 Meta 畫面的規模效益不足以承受分裂工具組的複雜度。

**否決**：收益/成本比不符合 Indie 小規模專案；複雜度超過 USS 主題切換的優雅性收益。

### D. 純 SpriteRenderer 實作 HUD（不使用任何 UI 框架）

**優點**：整個遊戲零 Canvas 開銷，draw call 最低。

**缺點**：文字（分數、素材數量、武器名稱）需要 TMP 獨立使用或 SpriteRenderer 字元 atlas，版面邏輯全部手寫；HUD 元素少，但開發成本遠超 UGUI。

**否決**：過度工程化；HUD 元素數量有限，UGUI 足以輕量承載，無需放棄 Canvas 帶來的版面基礎設施。

---

## Consequences（後果）

### 正面

- **確定性高**：三層均採 Unity 6.3 中成熟穩定的技術路徑（SpriteRenderer / UGUI），無需事前 spike 驗證輸入系統或渲染管線的未知問題。
- **彈幕區零 Canvas 開銷**：SpriteRenderer part bars 徹底隔離 UI 系統與 BulletSim 渲染路徑，保護 ≤200 draw call 預算；在彈幕最密集時段不增加額外 Canvas rebuild 壓力。
- **像素整數縮放保證**：UGUI `Canvas.pixelPerfect` 是 Pixel Perfect Camera 的已知整合點；免去在 USS 重建等效邏輯的風險。
- **手把導航無疑問**：UGUI `Selectable` + `EventSystem` 對 Partial Gamepad Support 的需求完整覆蓋，自訂 `Navigation.mode = Explicit` 路由允許精確控制武器卡片網格的方向鍵流。
- **UI assembly 單一工具組**：`KaijuBreaker.UI` 只需精通 UGUI + TMP，降低維護成本與新人上手門檻。
- **ADR-0002 相容**：UGUI MonoBehaviour 在 `OnEnable` 訂閱匯流排事件、`OnDisable` 取消訂閱；透過 `IPartStateQuery` 注入讀取狀態；無需每幀輪詢，零 GC 穩態可達。
- **ADR-0005 相容**：`PartBarController`（部位血條）與所有 UGUI Screen MonoBehaviour 均屬 `KaijuBreaker.UI` assembly，只依賴 `Core`；UIScreenManager 作為 screen stack 集中管理，符合 DI over singletons 原則。

### 負面 / 成本

- **色盲主題切換需程式碼**：UGUI 無 USS 變數切換；三種色盲模式（預設/藍黃/形狀優先）需在 `UIConfig` ScriptableObject（ADR-0003）儲存三套 `Color` 與 `Sprite` 設定，並在色盲模式變更事件時全數套用。實作成本高於 USS，但在此規模可接受。
- **文字縮放需程式碼**：三段縮放（100/125/150%）需在 Meta 畫面 `OnShow()` 時套用 TMP fontSize 乘數；新增畫面必須接入縮放系統，需維護紀律。
- **Canvas 管理紀律**：HUD 雙 Canvas 分層（Static/Dynamic）需嚴格維護，防止開發過程中不慎將動態元素移入靜態 Canvas（觸發不必要 rebuild）。建議在 `KaijuBreaker.Tools` assembly 加入 Editor 驗證腳本，CI 執行。
- **PartBarController 耦合部位 Prefab**：SpriteRenderer part bars 掛載在部位 Prefab 上，比 Canvas overlay 更緊密地與 art pipeline 整合；部位 Sprite 寬度改動需呼叫 `PartBarController.ResizeBars()`——應在 Boss 設計評審清單加入此項。
- **未來 UI Toolkit 遷移成本**：若 Unity 7+ UI Toolkit 大幅成熟、色盲主題與手把導航完全對等，Meta 畫面遷移需重寫 UXML/USS，但 HUD 與 part bars 不受影響（見 Reversibility）。

### Unity 6.3 版本注意事項（[需查證] 項目清單）

以下項目在 ADR 撰寫時未能確認，**UI Sprint 開始前必須由 UI 開發者在實際 Unity 6.3 編輯器中驗證，並將結果記錄於 `docs/architecture/tech-spikes/` 下對應的 spike 文件：**

1. **[需查證] `Canvas.pixelPerfect` + `PixelPerfectCamera` 在 URP `Screen Space - Camera` 模式下的行為**：確認在 URP 2D Renderer 中，Camera 引用 `PixelPerfectCamera` 的 Screen Space Camera Canvas 是否正確執行整數倍縮放，且行為與 URP 2022 LTS 一致（已知 URP 在 Unity 6.0 進行過 2D Renderer 重構）。
2. **[需查證] UI Toolkit runtime 手把 / D-Pad 導航完整度（供未來 Meta 畫面評估）**：Unity 6.3 中 `FocusController` 對方向鍵導航、Explicit 路由、Modal focus trap、Back button hook 的支援完整度，以及與 `InputSystem` 的整合是否達到 UGUI `Selectable` 的功能對等。（此為未來評估資料點，非當前決策阻斷項。）
3. **[需查證] UI Toolkit runtime Touch 在 iOS/Android 的 Panel input 路由**：Unity 6.3 UI Toolkit runtime Panel 在行動平台的 touch tap / long-press / drag 事件是否與 UGUI `PointerEventData` 等效，尤其在 Safe Area 邊界附近的行為。（同上，未來評估用。）
4. **[需查證] UI Toolkit Unity 6.3 Font Asset 對像素點陣字型的支援品質**：導入自訂點陣字型至 Font Asset、確保整數縮放下無子像素 hinting 的流程，與 TMP + UGUI 現有路徑的品質對比。（未來評估用。）
5. **[需查證] UGUI `Screen Space - Camera` Canvas 與 URP 2D Renderer Renderer Feature 的 sorting order 互動**：確認 `sortingOrder` 值在 URP 2D 的有效範圍，以及 `Hitbox_Overlay`（sortingOrder = 99）是否正確渲染於 game-feel.md 全白閃光覆蓋層（SpriteRenderer-based）之上。

### 效能意涵

- **SpriteRenderer part bars**：無 Canvas 開銷；`MaterialPropertyBlock` 更新為 GPU instancing 安全（不打破 Sprite Batcher batching）；16 個 SpriteRenderer 共用 atlas 時在 URP 2D Sprite Batcher 下批次——draw call 增量目標 ≤ 2（依 atlas 配置）。
- **UGUI HUD**：雙 Canvas 分離確保動態更新只 dirty `HUD_Dynamic`；全 Sprite Atlas batching；`Hitbox_Overlay` 單 Image 無額外開銷。HUD 目標額外 draw call ≤ 5（兩 Canvas 各一批 + hitbox + 浮動文字池）。
- **UGUI Meta 畫面**：非戰鬥期間無彈幕，draw call 預算充裕；武器卡片與費用清單版面靜態，更新由匯流排事件驅動而非每幀輪詢；文字使用 `TMP_Text.SetText(string, args)` 或 `SetCharArray`，避免 string 分配。
- **整體 draw call 估算（戰鬥最重情境）**：BulletSim SpriteRenderer batches（≤150） + part bars（≤2） + HUD（≤5） + GameFeel（≤20）= 估算 ≤177，留有 ≥23 餘量至 200 上限。（具體值需 profiling 驗證。）

---

## Reversibility（可逆性）

**中高**。

- **Part bars（SpriteRenderer → UGUI World Space Canvas）**：可逆，`PartBarController` 重構即可；`KaijuBreaker.UI` assembly 邊界不變；不影響其他系統。
- **HUD（UGUI → UI Toolkit）**：可逆但需重寫 UXML/USS；建議在 Meta 畫面 post-MVP 評估 UI Toolkit 成熟度後一併考量。
- **Meta 畫面（UGUI → UI Toolkit）**：可逐畫面遷移，`IScreen` 介面讓 `UIScreenManager` 對具體實作無感；此為未來遷移最低風險的入口點。
- **不可逆原則**：「Canvas 不進彈幕播放區」（part bars 用 SpriteRenderer）是效能安全邊界，以及「像素整數縮放必須明確支援」——即使未來遷移工具組，這兩條原則應保留。

---

*ADR 版本：1.0.0*
