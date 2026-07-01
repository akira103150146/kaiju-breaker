# Technical Preferences

<!-- Populated by /setup-engine. Updated as the user makes decisions throughout development. -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: Unity 6.3 LTS
- **Language**: C#
- **Rendering**: Universal Render Pipeline (URP) — 2D Renderer + Pixel Perfect Camera
- **Physics**: Unity 2D Physics (Box2D); bullets and most projectiles use custom kinematic movement + trigger overlap, not rigidbody simulation

## Input & Platform

<!-- Written by /setup-engine. Read by /ux-design, /ux-review, /test-setup, /team-ui, and /dev-story -->
<!-- to scope interaction specs, test helpers, and implementation to the correct input methods. -->

- **Target Platforms**: PC (Steam), Mobile (iOS / Android)
- **Input Methods**: Keyboard/Mouse, Touch, Gamepad
- **Primary Input**: Equal priority — PC keyboard/arrows and mobile touch-drag are both first-class
- **Gamepad Support**: Partial
- **Touch Support**: Full
- **Platform Notes**: Bullet patterns must stay readable on the smallest phone screen; touch uses one-finger drag-to-move; all UI must support both mouse-click and touch-tap — no hover-only interactions. Maintain input parity so neither platform feels like a port.

## Naming Conventions

- **Classes**: PascalCase (e.g., `PlayerShip`, `BulletPattern`)
- **Variables**: Public fields/properties PascalCase (e.g., `MoveSpeed`); private fields `_camelCase` (e.g., `_moveSpeed`)
- **Signals/Events**: C# events PascalCase with `On` prefix (e.g., `OnPartDestroyed`); UnityEvents PascalCase
- **Files**: PascalCase matching the class (e.g., `PlayerShip.cs`)
- **Scenes/Prefabs**: PascalCase (e.g., `BossArena.unity`, `PlayerShip.prefab`)
- **Constants**: PascalCase or UPPER_SNAKE_CASE (e.g., `MaxHealth` or `MAX_HEALTH`)

## Performance Budgets

- **Target Framerate**: 60 FPS (both PC and mobile)
- **Frame Budget**: 16.6 ms/frame
- **Draw Calls**: ≤ 200 (URP 2D; rely on SpriteAtlas batching for dense bullet patterns)
- **Memory Ceiling**: [TO BE CONFIGURED — set when minimum-spec mobile target is chosen]

## Testing

- **Framework**: Unity Test Framework (NUnit) — EditMode tests for pure logic, PlayMode tests for integration
- **Minimum Coverage**: [TO BE CONFIGURED]
- **Required Tests**: Balance formulas, gameplay systems, networking (if applicable)

## Forbidden Patterns

<!-- Add patterns that should never appear in this project's codebase -->
- [None configured yet — add as architectural decisions are made]

## Allowed Libraries / Addons

<!-- Add approved third-party dependencies here -->
- [None configured yet — add as dependencies are approved]

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- [No ADRs yet — use /architecture-decision to create one]

## Engine Specialists

<!-- Written by /setup-engine when engine is configured. -->
<!-- Read by /code-review, /architecture-decision, /architecture-review, and team skills -->
<!-- to know which specialist to spawn for engine-specific validation. -->

- **Primary**: unity-specialist
- **Language/Code Specialist**: unity-specialist (C# review — primary covers it)
- **Shader Specialist**: unity-shader-specialist (Shader Graph, HLSL, URP/HDRP materials)
- **UI Specialist**: unity-ui-specialist (UI Toolkit UXML/USS, UGUI Canvas, runtime UI)
- **Additional Specialists**: unity-dots-specialist (ECS, Jobs system, Burst compiler), unity-addressables-specialist (asset loading, memory management, content catalogs)
- **Routing Notes**: Invoke primary for architecture and general C# code review. Invoke DOTS specialist for any ECS/Jobs/Burst code. Invoke shader specialist for rendering and visual effects. Invoke UI specialist for all interface implementation. Invoke Addressables specialist for asset management systems.

### File Extension Routing

<!-- Skills use this table to select the right specialist per file type. -->
<!-- If a row says [TO BE CONFIGURED], fall back to Primary for that file type. -->

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Game code (.cs files) | unity-specialist |
| Shader / material files (.shader, .shadergraph, .mat) | unity-shader-specialist |
| UI / screen files (.uxml, .uss, Canvas prefabs) | unity-ui-specialist |
| Scene / prefab / level files (.unity, .prefab) | unity-specialist |
| Native extension / plugin files (.dll, native plugins) | unity-specialist |
| General architecture review | unity-specialist |
