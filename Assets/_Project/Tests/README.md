# 測試架構（Test Framework）— KAIJU BREAKER / 殲獸戰機

**Engine**: Unity 6.3 LTS  
**Test Framework**: Unity Test Framework (NUnit)  
**CI/CD**: `.github/workflows/ci-tests.yml`  
**Setup Date**: 2026-07-02

---

## 目錄配置（Directory Structure）

```
Assets/_Project/Tests/
  ├── EditMode/                    # 單元測試（無需進遊戲、純邏輯）
  │   ├── EditModeTests.asmdef     # Assembly definition
  │   ├── Weapons/
  │   │   └── *_test.cs            # 武器公式、傷害計算、Tier 行為
  │   ├── KaijuParts/
  │   │   └── *_test.cs            # 部位狀態機、熱累積、護甲邏輯
  │   ├── Economy/
  │   │   └── *_test.cs            # 素材產量公式（27 情境）
  │   ├── Meta/
  │   │   └── *_test.cs            # 存檔序列化、遷移鏈、CRC32 驗證
  │   ├── Difficulty/
  │   │   └── *_test.cs            # 難度乘數、查詢介面
  │   ├── Helpers/
  │   │   ├── FakeEventBus.cs      # 測試用假事件匯流排
  │   │   └── ScriptableObjectFixture.cs  # SO fixture 工廠
  │   └── README.md                # EditMode 用途及 DI 注入示例
  │
  ├── PlayMode/                    # 整合測試（需進遊戲、ECS/物理/時間）
  │   ├── PlayModeTests.asmdef     # Assembly definition
  │   ├── BulletSim/
  │   │   └── *_test.cs            # 敵彈模擬（ECS 場景、1000 彈 @60fps）
  │   ├── Stage/
  │   │   └── *_test.cs            # Run 狀態機、波段轉換、 autosave 觸發
  │   ├── GameFeel/
  │   │   └── *_test.cs            # 頓幀 / 慢動作 / 震動 / 閃光 timing
  │   ├── Input/
  │   │   └── *_test.cs            # 觸控 / 鍵鼠 / 手柄輸入判讀
  │   └── README.md                # PlayMode 用途及場景 setup
  │
  ├── Fixtures/                    # 共用測試資料（SO、場景）
  │   ├── WeaponDefMock.asset      # 武器配置 mock
  │   ├── PartDefMock.asset        # 部位配置 mock
  │   └── TestScene.unity          # 最小可玩場景（PlayMode 用）
  │
  └── README.md                    # 本文件
```

---

## 啟用測試框架（Enabling Unity Test Framework）

Unity Test Framework 預裝於 Unity 2019+，無需額外安裝。

1. **開啟 Test Runner**
   - 菜單 → `Window → General → Test Runner`
   - 或快捷鍵 `Ctrl+Alt+T` (Windows) / `Cmd+Shift+T` (Mac)

2. **檢查組件定義**
   - 確認 `Assets/_Project/Tests/EditMode/EditModeTests.asmdef` 存在
   - 確認 `Assets/_Project/Tests/PlayMode/PlayModeTests.asmdef` 存在
   - 每個 asmdef 應列出依賴：`Core`, `Content`, 對應系統組件, `NUnit`

3. **執行測試**
   - **EditMode**: Test Runner 左窗格選 "EditMode" 標籤 → 點 "Run All"
   - **PlayMode**: 選 "PlayMode" 標籤 → 點 "Run All"
   - CLI: `Unity -projectPath . -runTests -testPlatform editmode`

---

## 測試命名規則（Test Naming Conventions）

### 檔案名稱
- **格式**: `[系統]_[功能]_test.cs`
- **範例**:
  - `weapons_laser_damage_test.cs`
  - `kaiju_parts_heat_accumulation_test.cs`
  - `economy_shard_yield_test.cs`
  - `meta_save_migration_test.cs`

### 方法名稱
- **格式**: `Test_[情境]_[預期結果]()` 或 `Test[ScenarioExpected]()`
- **範例**:
  ```csharp
  [Test]
  public void Test_LaserBaseDamageAtTier1_ReturnsExpectedValue()
  {
      // Arrange, Act, Assert
  }

  [Test]
  public void Test_PartSoftenedThenHit_BreaksWithSoftenedBonus()
  {
  }

  [Test]
  public void Test_SaveCorruptedWithInvalidCRC32_LoadsFallbackWithoutCrash()
  {
  }
  ```

### 測試類別
- **格式**: `[功能]Tests` 或 `[系統][功能]Tests`
- **範例**:
  ```csharp
  public class WeaponsLaserDamageTests { }
  public class KaijuPartsHeatAccumulationTests { }
  public class EconomyShardYieldTests { }
  ```

---

## Story 類型 → 測試證據（Test Evidence by Story Type）

依 `coding-standards.md` §Testing Standards，故事必須在完成前提供測試證據。

| Story 類型 | 必需證據 | 位置 | 門檻 |
|---|---|---|---|
| **Logic** (公式、AI、狀態機) | 自動化單元測試通過 | `Assets/_Project/Tests/EditMode/[System]/` | **BLOCKING** |
| **Integration** (多系統) | 整合測試 OR 玩測紀錄 | `Assets/_Project/Tests/PlayMode/[System]/` | **BLOCKING** |
| **Visual/Feel** (動畫、VFX、手感) | 截圖 + lead 簽核 | `production/qa/evidence/` | ADVISORY |
| **UI** (菜單、HUD、畫面) | 手動走查文件 OR 互動測試 | `production/qa/evidence/` | ADVISORY |
| **Config/Data** (平衡調整) | Smoke check 通過 | `production/qa/smoke-[日期].md` | ADVISORY |

**重點**：
- **BLOCKING 級別故事若無測試不可標 Done**。
- CI 會在 main 和 PR 上強制執行：**測試失敗 = 禁止 merge**。

---

## 測試規則（Automated Test Rules）

### 決定性（Determinism）
- **絕不使用隨機種子** — 測試結果必須每次都相同。
- **絕不依賴時間** — 若需時間，用 `mock Time` 或 `MonoBehaviour.StartCoroutine` 注入。
- **反例**:
  ```csharp
  // ❌ 壞：時間依賴
  [Test]
  public void Test_SomeLogic()
  {
      var now = DateTime.Now;  // 時間會變，測試飄忽
  }

  // ✓ 好：決定性輸入
  [Test]
  public void Test_SomeLogic()
  {
      float deltaTime = 0.016f;  // 硬編定值，決定性
  }
  ```

### 隔離（Isolation）
- **每個測試自建 / 自拆狀態**，測試不得依賴彼此的執行順序。
- **EditMode**: 用 fake SO fixture 或建構注入。
- **PlayMode**: 建立隔離的測試場景或清理 GameObject。
- 反例:
  ```csharp
  // ❌ 壞：狀態洩漏（test-1 設定的值影響 test-2）
  private static int GlobalCounter = 0;
  
  [Test]
  public void Test_First() { GlobalCounter = 10; }
  
  [Test]
  public void Test_Second() { Assert.AreEqual(10, GlobalCounter); }  // 靠 test-1 順序

  // ✓ 好：隔離狀態
  [Test]
  public void Test_First() 
  { 
      var sys = new EconomySystem();
      Assert.AreEqual(100, sys.CalculateShardYield(...));
  }
  ```

### 獨立性（Independence）
- **單元測試不得呼叫外部 API、資料庫、檔案 I/O**。
- **用依賴注入 + fake 實作**代替真實系統。
- **EditMode 範例**:
  ```csharp
  [Test]
  public void Test_WeaponDamageScaling()
  {
      // 不引用真實 Weapons 組件，注入 fake tier query
      var fakeQuery = new FakeTierQuery(tier: 2);
      var weapon = new WeaponCalculator(fakeQuery);
      
      var damage = weapon.CalculateDamage(weaponId: 1);
      Assert.AreEqual(expectedDamage, damage);
  }
  ```

### 無硬編碼（No Hardcoded Data）
- **用 fixture 工廠或常數檔案**，而非散落的魔術數字。
- 例外：邊界值測試，其中精確數字正是測試重點。
- **反例**:
  ```csharp
  // ❌ 壞：多處散布同數值
  [Test] public void Test_Part1() { Assert.AreEqual(42, Calculate()); }
  [Test] public void Test_Part2() { Assert.AreEqual(42, Calculate()); }  // 重複

  // ✓ 好：集中常數
  private const float ExpectedDamage = 42f;
  [Test] public void Test_LaserBaseDamage() { Assert.AreEqual(ExpectedDamage, ...); }
  [Test] public void Test_LaserWithBonus() { Assert.AreEqual(ExpectedDamage * 1.5f, ...); }
  ```

---

## 勿自動化的案例（What NOT to Automate）

- **視覺保真度** — 著色器輸出、VFX 外觀、動畫曲線 → 截圖 + 人工檢視
- **手感品質** — 輸入反應力、重量感、timing 體感 → 人工試玩驗證
- **平台特定渲染** — 在目標硬體測試，不在 headless CI
- **完整遊戲會話** — 全遊戲流程玩透透 → 玩測覆蓋，非自動化

---

## EditMode 測試範例（EditMode Test Example）

```csharp
// File: Assets/_Project/Tests/EditMode/Weapons/weapons_laser_damage_test.cs
using NUnit.Framework;
using UnityEngine;
using KaijuBreaker.Core;
using KaijuBreaker.Content;
using KaijuBreaker.Weapons;

namespace KaijuBreaker.Tests.EditMode.Weapons
{
    public class WeaponsLaserDamageTests
    {
        private WeaponCalculator _calculator;
        private FakeTierQuery _fakeQuery;
        private WeaponDef _laserDef;

        [SetUp]
        public void Setup()
        {
            _fakeQuery = new FakeTierQuery(tier: 1);
            _calculator = new WeaponCalculator(_fakeQuery);
            _laserDef = ScriptableObject.CreateInstance<WeaponDef>();
            _laserDef.BaseDamage = 10f;
        }

        [TearDown]
        public void Teardown()
        {
            if (_laserDef != null) Object.DestroyImmediate(_laserDef);
        }

        [Test]
        public void Test_LaserBaseDamageAtTier1_ReturnsExpectedValue()
        {
            float damage = _calculator.CalculateDamage(_laserDef, heatBonus: false);
            Assert.AreEqual(10f, damage);
        }

        [Test]
        public void Test_LaserWithSoftenedBonus_DamageIncreases()
        {
            float damageBase = _calculator.CalculateDamage(_laserDef, heatBonus: false);
            float damageSoftened = _calculator.CalculateDamage(_laserDef, heatBonus: true);
            Assert.Greater(damageSoftened, damageBase);
        }
    }
}
```

---

## PlayMode 測試範例（PlayMode Test Example）

```csharp
// File: Assets/_Project/Tests/PlayMode/BulletSim/bullet_sim_density_test.cs
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using KaijuBreaker.BulletSim;
using KaijuBreaker.Core;

namespace KaijuBreaker.Tests.PlayMode.BulletSim
{
    public class BulletSimDensityTests
    {
        private BulletSimSystem _bulletSim;
        private GameObject _testScene;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // 建立隔離場景
            _testScene = new GameObject("TestBulletScene");
            _bulletSim = _testScene.AddComponent<BulletSimSystem>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_testScene != null) Object.Destroy(_testScene);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_1000BulletsAt60FPS_NoGCAlloc()
        {
            _bulletSim.SpawnBulletBurst(count: 1000);
            long gcBefore = System.GC.GetTotalMemory(false);

            for (int i = 0; i < 60; i++)
            {
                _bulletSim.Update(Time.deltaTime);
                yield return null;
            }

            long gcAfter = System.GC.GetTotalMemory(false);
            Assert.AreEqual(gcBefore, gcAfter, "GC alloc detected in bullet hot path");
        }
    }
}
```

---

## 依賴注入 + Fake 實作（DI + Fakes for Testing）

### FakeEventBus 示例
```csharp
// Assets/_Project/Tests/EditMode/Helpers/FakeEventBus.cs
using System;
using System.Collections.Generic;
using KaijuBreaker.Core;

namespace KaijuBreaker.Tests.EditMode.Helpers
{
    public class FakeEventBus : IEventBus
    {
        private Dictionary<Type, List<Delegate>> _subscriptions = new();
        public List<object> PublishedEvents { get; } = new();

        public void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            var type = typeof(T);
            if (!_subscriptions.ContainsKey(type))
                _subscriptions[type] = new List<Delegate>();
            _subscriptions[type].Add(handler);
        }

        public void Publish<T>(in T evt) where T : IGameEvent
        {
            PublishedEvents.Add(evt);
            var type = typeof(T);
            if (_subscriptions.TryGetValue(type, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    ((Action<T>)handler)?.Invoke(evt);
                }
            }
        }
    }
}
```

### 在測試中使用
```csharp
[Test]
public void Test_PartBreakPublishesEvent()
{
    var eventBus = new FakeEventBus();
    var partSystem = new KaijuPartSystem(eventBus);
    
    partSystem.TakeDamage(partId: 1, damage: 100f);
    
    Assert.AreEqual(1, eventBus.PublishedEvents.Count);
    Assert.IsInstanceOf<PartBroke>(eventBus.PublishedEvents[0]);
}
```

---

## CI/CD 執行（CI/CD Execution）

GitHub Actions 工作流 (`.github/workflows/ci-tests.yml`) 於以下時機自動執行：
- **Push to main**
- **Pull Request to main**

工作流執行：
1. EditMode 測試（編譯器模式，無需 Play）
2. PlayMode 測試（執行時間較長）
3. 上傳測試報告作為 artifact

**若任一測試失敗，merge 被禁止。**

詳見 `.github/workflows/ci-tests.yml`。

---

## 常見問題（FAQ）

**Q: 如何在本機執行測試？**  
A: 開啟 Test Runner (`Window → General → Test Runner`)，選 EditMode 或 PlayMode，點 "Run All"。或用 CLI: `Unity -projectPath . -runTests -testPlatform editmode`。

**Q: PlayMode 測試很慢，能跳過嗎？**  
A: 不能。BLOCKING 級別故事必需整合測試。但可在本機開發時先跑 EditMode，待提交 PR 時讓 CI 跑完整組。

**Q: 如何 mock 事件匯流排？**  
A: 用 `FakeEventBus`（見 Helpers/）。注入系統建構子：`new MySystem(eventBus: fakeEventBus)`。

**Q: ScriptableObject fixture 如何建立？**  
A: 在 `[SetUp]` 用 `ScriptableObject.CreateInstance<T>()`，在 `[TearDown]` 用 `Object.DestroyImmediate()`。見上方 WeaponDef 範例。

**Q: 測試失敗但本機通過？**  
A: 通常是決定性或隔離問題。檢查是否依賴時間、亂數、其他測試狀態。必要時加 `[Order]` 明確排序（不推薦）。

---

*版本：1.0.0 — 2026-07-02*  
*參考：coding-standards.md, ADR-0005, control-manifest.md*
