# Unity 6.3 LTS — Breaking Changes

**Last verified:** 2026-07-01

This document tracks breaking API changes and behavioral differences between Unity 2022 LTS
(likely in model training) and Unity 6.3 LTS (current version). Organized by risk level.

## HIGH RISK — Will Break Existing Code

### Entities/DOTS API Complete Overhaul
**Versions:** Entities 1.0+ (Unity 6.0+)

```csharp
// ❌ OLD (pre-Unity 6, GameObjectEntity pattern)
public class HealthComponent : ComponentData {
    public float Value;
}

// ✅ NEW (Unity 6+, IComponentData)
public struct HealthComponent : IComponentData {
    public float Value;
}

// ❌ OLD: ComponentSystem
public class DamageSystem : ComponentSystem { }

// ✅ NEW: ISystem (unmanaged, Burst-compatible)
public partial struct DamageSystem : ISystem {
    public void OnCreate(ref SystemState state) { }
    public void OnUpdate(ref SystemState state) { }
}
```

**Migration:** Follow Unity's ECS migration guide. Major architectural changes required.

---

### Input System — Legacy Input Deprecated
**Versions:** Unity 6.0+

```csharp
// ❌ OLD: Input class (deprecated)
if (Input.GetKeyDown(KeyCode.Space)) { }

// ✅ NEW: Input System package
using UnityEngine.InputSystem;
if (Keyboard.current.spaceKey.wasPressedThisFrame) { }
```

**Migration:** Install Input System package, replace all `Input.*` calls with new API.

---

### URP/HDRP Renderer Feature API Changes
**Versions:** Unity 6.0+

```csharp
// ❌ OLD: ScriptableRenderPass.Execute signature
public override void Execute(ScriptableRenderContext context, ref RenderingData data)

// ✅ NEW: Uses RenderGraph API
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
```

**Migration:** Update custom render passes to use RenderGraph API.

---

### URP Compatibility Mode REMOVED
**Versions:** Removed in 6.3 (was deprecated in 6.0)

URP **Compatibility Mode (the non-Render Graph path) is gone in 6.3.**
`RenderGraphSettings.enableRenderCompatibilityMode` is now **read-only and always returns `false`**.
The `UPM_COMPATIBILITY_MODE` scripting define is only a temporary stopgap and **will stop working in 6.4+**.

```csharp
// ❌ DEAD in 6.3 — Compatibility Mode can no longer be enabled (property is read-only)
// ...enableRenderCompatibilityMode = true;  // no-op

// ✅ Author every ScriptableRendererFeature / ScriptableRenderPass for Render Graph
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) { }
```

**Migration:** Convert every custom render pass to the Render Graph API. Pre-6.0
`Execute(ScriptableRenderContext, ref RenderingData)` passes are dead code in 6.3.
Source: https://docs.unity3d.com/6000.3/Documentation/Manual/UpgradeGuideUnity63.html

---

### `[SerializeField]` Now Field-Only
**Versions:** Unity 6.3

`[SerializeField]` can now **only be applied to fields** — putting it on a property
or other member is a **compile error**.

```csharp
// ❌ Compile error in 6.3
[SerializeField] public int Score { get; private set; }

// ✅ Use the field: target on the backing field
[field: SerializeField] public int Score { get; private set; }
```

**Migration:** Remove `[SerializeField]` from non-fields; use `[field: SerializeField]`
for auto-properties that must be serialized.
Source: https://docs.unity3d.com/6000.3/Documentation/Manual/UpgradeGuideUnity63.html

---

## MEDIUM RISK — Behavioral Changes

### UI Toolkit USS Parser Now Stricter
**Versions:** Unity 6.3

Previously-tolerated invalid USS is now reported as **errors**. Stylesheets that loaded
on 2022 LTS may fail to load in 6.3.

**Migration:** Fix the USS syntax flagged in the Inspector/Console after upgrading.
Source: https://docs.unity3d.com/6000.3/Documentation/Manual/UpgradeGuideUnity63.html

---

### Build Settings Replaced by Build Profiles
**Versions:** Unity 6.0+

The single **Build Settings** window is gone. Builds are now configured through
**Build Profiles** — per-platform configurations with per-profile setting overrides
(`File > Build Profiles`).

**Migration:** Recreate build configurations as Build Profiles.
Source: https://docs.unity3d.com/6000.0/Documentation/Manual/build-profiles.html

---

### Addressables — Asset Loading Returns
**Versions:** Unity 6.2+

Asset loading failures now throw exceptions by default instead of returning null.
Add proper exception handling or use `TryLoad` variants.

```csharp
// ❌ OLD: Silent null on failure
var handle = Addressables.LoadAssetAsync<Sprite>("key");
var sprite = handle.Result; // null if failed

// ✅ NEW: Throws on failure, use try/catch or TryLoad
try {
    var handle = Addressables.LoadAssetAsync<Sprite>("key");
    var sprite = await handle.Task;
} catch (Exception e) {
    Debug.LogError($"Failed to load: {e}");
}
```

---

### Physics — Default Solver Iterations Changed
**Versions:** Unity 6.0+

Default solver iterations increased for better stability.
Check `Physics.defaultSolverIterations` if you rely on old behavior.

---

## LOW RISK — Deprecations (Still Functional)

### UGUI (Legacy UI)
**Status:** Deprecated but supported
**Replacement:** UI Toolkit

UGUI still works but UI Toolkit is recommended for new projects.

---

### Legacy Particle System
**Status:** Deprecated
**Replacement:** Visual Effect Graph (VFX Graph)

---

### Old Animation System
**Status:** Deprecated
**Replacement:** Animator Controller (Mecanim)

---

## Platform-Specific Breaking Changes

### WebGL
- **Unity 6.0+**: WebGPU is now the default (WebGL 2.0 fallback available)
- Update shaders for WebGPU compatibility

### Android
- **Unity 6.0+**: Minimum API level raised to 24 (Android 7.0)
- **Unity 6.3**: Minimum API level raised again to **25 (Android 7.1+)**
- **Unity 6.3**: `PlayerSettings.Android.androidIsGame` is obsolete → use the new **App Category** Player setting
  - Source: https://docs.unity3d.com/6000.3/Documentation/Manual/UpgradeGuideUnity63.html

### iOS
- **Unity 6.0+**: Minimum deployment target raised to iOS 13

---

## Migration Checklist

When upgrading from 2022 LTS to Unity 6.3 LTS:

- [ ] Audit all DOTS/ECS code (complete rewrite likely needed)
- [ ] Replace `Input` class with Input System package
- [ ] Update custom render passes to RenderGraph API
- [ ] Add exception handling to Addressables calls
- [ ] Test physics behavior (solver iterations changed)
- [ ] Consider migrating UGUI to UI Toolkit for new UI
- [ ] Update WebGL shaders for WebGPU
- [ ] Verify minimum platform versions (Android min API now 25 in 6.3 / iOS)
- [ ] Convert all custom render passes to Render Graph (Compatibility Mode removed in 6.3)
- [ ] Fix `[SerializeField]` on non-fields → `[field: SerializeField]`
- [ ] Recreate build configs as Build Profiles
- [ ] Fix any USS flagged by the stricter 6.3 parser

---

**Sources:**
- https://docs.unity3d.com/6000.3/Documentation/Manual/UpgradeGuideUnity63.html
- https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html
- https://docs.unity3d.com/6000.0/Documentation/Manual/upgrade-guides.html
- https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/upgrade-guide.html
