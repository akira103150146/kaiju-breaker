# Story 005: 循環武器莢艙行為 — 下降、徘徊、循環顯示、拾取

> **Epic**: 關卡系統與 Run 流程
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-07-02
> **Last Updated**: —

## Context

**GDD**: `design/gdd/stage-system.md`
**Requirement**: `TR-stage-002`（莢艙行為確保代理感與保底可達性）
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

**ADR Governing Implementation**: ADR-0003: 資料驅動調校 (primary); ADR-0005: 專案結構與組件邊界 (secondary)
**ADR Decision Summary**: 所有莢艙行為旋鈕（`pod_cycle_interval`、`pod_dwell_time`、`pod_reachable_band_y`、`pod_bob_amplitude`、`pod_descend_speed`、`pod_despawn_after`）存於 `PodDropConfig` SO，不硬編碼。`WeaponPodController`（MonoBehaviour）接收武器池列表與 `PodDropConfig` 引用，通過 DI 注入；拾取後發布 `WeaponPodGrabbed` 事件（Core struct），Weapons 系統與 Meta 系統各自訂閱。

**Engine**: Unity 6.3 | **Risk**: LOW
**Engine Notes**: `OnTriggerEnter2D` 觸發拾取；`transform.localPosition` 驅動 bob；`Coroutine` 或 `Update` 計時循環。Unity 6.3 API 穩定。需確認 2D collider trigger 事件在特定 Physics2D 設定下的幀序（查 Unity 6.3 docs）。

**Control Manifest Rules (Feature — Stage)**:
- Required: `MUST` 莢艙循環武器池；停留 `pod_dwell_time`（12s）確保玩家可等到目標武器
- Required: `MUST NOT` 引用 Weapons / Meta 系統組件——拾取以事件發布，讓各系統自行訂閱
- Required: 代理感 GDD 保證：`pod_dwell_time / pod_cycle_interval ≥ 4`（完整循環次數），不可打破此比例
- Guardrail: 所有 float 旋鈕來自 `PodDropConfig` SO；禁止硬編碼 3.0s / 12s 等數值

---

## Acceptance Criteria

*From GDD `design/gdd/stage-system.md` §F.1, §F.2.1–F.2.3, §L.2:*

- [ ] 收到 `PodSpawnRequested` 事件後，`WeaponPodSpawner` 在事件指定位置實例化 Pod Prefab
- [ ] Pod 在**下降（Descend）**階段以 `pod_descend_speed` 向下移動，直至 Y 座標進入 `pod_reachable_band_y`（`[min_y, max_y]`，`PodDropConfig` SO）
- [ ] Pod 進入可達區域後立即進入**徘徊（Dwell）**階段，停留 `pod_dwell_time`（預設 12.0s）
- [ ] 徘徊中：Bob 上下浮動（振幅 `pod_bob_amplitude`，週期 ~2s，`Mathf.Sin` 驅動）
- [ ] 循環顯示：圖示每 `pod_cycle_interval`（預設 3.0s）切換至下一個武器，過渡 0.3s 淡入淡出
- [ ] 循環索引：`displayIndex = (displayIndex + 1) % weaponPool.Count`（不超出池邊界）
- [ ] MVP 降級（池 < 4 武器）：僅在已啟用武器中循環（例如 S1 Primary Pod：L1 → L2 → L1 → …）
- [ ] 池 = 1 種武器：靜態顯示（無循環動畫），仍可拾取
- [ ] 代理感保證：`pod_dwell_time(12) / pod_cycle_interval(3) = 4 次完整循環`——玩家在停留期間每種武器出現 ≥ 4 次
- [ ] **可達性保證**：Pod 停留時 Y 座標始終在 `pod_reachable_band_y` 範圍內（bob 振幅不得超出邊界）
- [ ] 玩家機體碰觸 Pod → 拾取**當前顯示**的武器；發布 `WeaponPodGrabbed{weaponId, isFirstTime}` 事件；Pod 立即消失
- [ ] `isFirstTime`：查詢 `ISaveService`（注入），若 `weapons_owned` 不含 `weaponId` → `true`
- [ ] 停留結束（`pod_dwell_time` 到期）：Pod 淡出消失（不爆炸）；總壽命 ≤ `pod_despawn_after`（SO）
- [ ] Primary Pod 視覺：冷藍色膠囊 + 雷射符文圖示 + 當前武器色光環；Secondary Pod：橙色膠囊 + 飛彈符文圖示

---

## Implementation Notes

*Derived from ADR-0003 and ADR-0005 Implementation Guidelines:*

- `WeaponPodController` 放 `Assets/_Project/Scripts/Stage/WeaponPodController.cs`（MonoBehaviour，`KaijuBreaker.Stage.asmdef`）。
- 初始化方法：`Init(IEventBus bus, ISaveService save, PodDropConfig config, List<WeaponId> weaponPool, PodType podType)`（由 `WeaponPodSpawner` 在 Instantiate 後呼叫）。
- 內部狀態機（enum `PodPhase`）：`Descending → Dwelling → Despawning`。
- **Descending**：`Update` 每幀移動 `transform.position.y -= config.PodDescendSpeed * Time.deltaTime`；到達 `config.PodReachableBandY.x`（min Y）時轉入 Dwelling。
- **Dwelling**：啟動循環計時器（`private float _cycleTimer`）；bob = `config.PodBobAmplitude * Mathf.Sin(Time.time * (2f * Mathf.PI / 2f))`（週期 2s）；停留計時達 `config.PodDwellTime` → 轉入 Despawning。
- **Despawning**：`alpha` 從 1 線性 → 0（0.5s 淡出）；完成後 `Destroy(gameObject)`。
- 可達性保護：bob 應用後 clamp Y 至 `[config.PodReachableBandY.x, config.PodReachableBandY.y]`，避免振幅超邊界。
- 圖示切換：`SpriteRenderer.color` alpha 插值（0.3s 淡入淡出），在 `OnCycleComplete` 更新 `_currentWeaponIcon` sprite。
- 拾取：`OnTriggerEnter2D(Collider2D other)` → 確認 Tag = "Player" → `bool isFirst = !save.IsWeaponOwned(weaponId)` → 發布 `WeaponPodGrabbed{weaponId, isFirst}` → `Destroy(gameObject)`。
- `WeaponPodSpawner`（純 C# 或 MonoBehaviour）訂閱 `PodSpawnRequested`，從 Prefab pool（物件池或 Instantiate）生成 Pod 並呼叫 `Init`。
- `WeaponPodGrabbed` 事件由 Weapons 系統（換裝）與 Meta 系統（解鎖所有權）各自訂閱；Stage 系統不處理武器換裝邏輯。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 004: `PodSpawnRequested` 事件的發布與保底追蹤（本 Story 消費該事件）
- **武器換裝邏輯**: weapons epic（`WeaponPodGrabbed` 事件後 Weapons 系統換裝）
- **武器所有權永久解鎖**: meta-save epic（`WeaponPodGrabbed{isFirstTime=true}` → Meta 訂閱並存檔）
- Story 007: Stage 1 首次拾取 HUD 提示 tooltip（本 Story 只發布事件，UI 訂閱並顯示）
- 武器池內容定義: content-config epic（`StageDef.primaryWeaponPool` / `secondaryWeaponPool` 列表，`PodDropConfig` SO）

---

## QA Test Cases

*Integration story — PlayMode 自動化測試規格：*

**AC-1**: 循環計時 — 2 池 2 武器
  - Given: Pod 初始化 `weaponPool=[L1, L2]`；`pod_cycle_interval=3.0s`
  - When: 6.1s 後（超過 2 個循環）
  - Then: 顯示武器序列為 L1→L2→L1（cycleCount=2）；`_cycleTimer` 重置
  - Edge cases: 池大小=1 → cycleTimer 不啟動；圖示永遠顯示 pool[0]

**AC-2**: 停留時間保證代理感
  - Given: `pod_dwell_time=12.0s`；`pod_cycle_interval=3.0s`；`weaponPool=[L1,L2]`
  - When: 完整 12s 停留（不被拾取）
  - Then: 每種武器顯示 ≥ 4 次；Pod 在 12.0s ± 0.2s 後開始淡出
  - Edge cases: 停留期間玩家碰觸 Pod 立即消失（不等待停留結束）

**AC-3**: 可達性驗證 — Pod Y 始終在 `pod_reachable_band_y`
  - Given: `pod_reachable_band_y=[100, 400]`；`pod_bob_amplitude=15`
  - When: 整個 Dwelling 階段（每幀採樣 Y 座標）
  - Then: 所有幀 `pod.position.y ∈ [100, 400]`（bob clamp 生效）
  - Edge cases: `pod_bob_amplitude > (max_y - min_y) / 2` 時仍安全 clamp

**AC-4**: 拾取正確武器（當前顯示）
  - Given: Pod cycling L1→L2；玩家在 t=4.5s（顯示 L2）觸碰 Pod
  - When: `OnTriggerEnter2D` 觸發
  - Then: `WeaponPodGrabbed{weaponId=L2}` 發布；Pod 立即消失（不等 despawn）

**AC-5**: `isFirstTime` 旗標正確
  - Given: `ISaveService.IsWeaponOwned(L2)=false`；玩家拾取 L2
  - When: `WeaponPodGrabbed` 發布
  - Then: `event.isFirstTime = true`
  - Edge cases: `IsWeaponOwned(L2)=true` → `isFirstTime=false`

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/_Project/Tests/PlayMode/Stage/pod_cycle_dwell_test.cs` — PlayMode 整合測試，必須全部通過 【BLOCKING】

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 004（`PodSpawnRequested` 事件的發布）
- Depends on: weapons epic（`WeaponPodGrabbed` 事件被 Weapons 系統訂閱換裝——本 Story 僅發布事件，不需 Weapons 系統 DONE 即可實作；但端對端測試需要 Weapons DONE）
- Depends on: meta-save epic（`ISaveService.IsWeaponOwned()` 查詢——可用假介面在整合測試中替代）
- Depends on: content-config epic（`PodDropConfig` SO 含所有旋鈕；`StageDef.primaryWeaponPool` / `secondaryWeaponPool` 列表）
- Unlocks: Story 006（Pre-Boss Lull 的 Pod 由此系統生成）；Story 007（首次拾取事件供 Onboarding tooltip 觸發）
