# Unity 6.3 LTS — Breaking Changes

**Last verified:** 2026-02-13

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

## MEDIUM RISK — Behavioral Changes

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
- [ ] Verify minimum platform versions (Android/iOS)

---

**Sources:**
- https://docs.unity3d.com/6000.0/Documentation/Manual/upgrade-guides.html
- https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/upgrade-guide.html

---

## 6.3 → 6.6 (added 2026-11-09 by /setup-engine)

Sourced 2026-11-09 from Unity's what's-new pages. Items marked *(summary)* came
from a summarized part of a long page, not verbatim text — confirm before relying
on exact names. The 6.6 upgrade guide
(https://docs.unity.com/en-us/engine/6000.6/manual/UpgradeGuideUnity66) returned
**404**, and the 6.4/6.5 upgrade guides were not fetched: this list is
**NOT exhaustive** — NOT SOURCEABLE beyond the pages below.

### 6.4
- PVRTC texture format removed — use ASTC or ETC. https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity64
- URP Compatibility Mode removed for custom render passes (`URP_COMPATIBILITY_MODE` define gone) — use Render Graph. https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity64
- `[SerializeReference]` field whose ancestor lacks `[Serializable]` now warns in the Editor. https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity64

### 6.5
- Particle Shape / Terrain Paint Detail meshes need Read/Write enabled, or the build fails. https://docs.unity.com/en-us/engine/6000.5/manual/whats-new/unity65
- *(summary)* Obsolete `GameObject.rigidbody`, `.camera`, `Component.renderer`, `AddComponent(string)` removed — use `GetComponent<T>` / `AddComponent<T>`. https://docs.unity.com/en-us/engine/6000.5/manual/whats-new/unity65
- *(summary)* Integer-ID APIs (`Object.GetInstanceID`, `Resources.InstanceIDToObject`, `Selection.instanceIDs`) are now compile errors — use `EntityId`. https://docs.unity.com/en-us/engine/6000.5/manual/whats-new/unity65
- *(summary)* Android: x86-64 removed; minimum API level 26; `Screen.fullScreen` has no effect (use AndroidWindowInsets); "Hide Navigation Bar" replaced by the Insets section. https://docs.unity.com/en-us/engine/6000.5/manual/whats-new/unity65
- Editor log is now project-local (`<Project>/Logs/Editor.log`); `-useGlobalLog` restores the old location. https://docs.unity.com/en-us/engine/6000.5/manual/whats-new/unity65

### 6.6
- New projects default Enter Play Mode to *reload scene only*: code is not reloaded and static variables are **not reset**. https://discussions.unity.com/t/unity-6-6-is-now-available/1735357
- Dynamic batching is now obsolete. https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity66
- Particle Shape, Terrain Detail Mesh and **Mesh Collider** meshes need Read/Write enabled, or the build fails. https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity66
- New Hierarchy window is the default (legacy still available). https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity66
- New projects: `Library/Artifacts` renamed `Library/DataStore`. https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity66
- *(summary)* Android: minimum OpenGL ES 3.1 (ES 3.0 unsupported); Legacy and Round icons removed. https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity66
- *(summary)* iOS Swift projects: UnityFramework split into UnityFramework + UnityAPI. https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity66
- NOT SOURCEABLE — PlayerPrefs, ScriptableObject, AssetDatabase, EditorSceneManager and URP API breaks for 6.6: not stated on the pages above.
