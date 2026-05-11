# GrayHare.GameEngine

A lightweight 2D game engine built on [SFML.Net 3.0.0](https://www.sfml-dev.org/download/sfml.net/) for .NET applications.
GrayHare.GameEngine provides an Entity-Component-System world, a service-locator host, scene management with a stack,
input polling, asset caching, audio playback, sprite animation, a 2D camera, and a rich library of steering behaviors —
all composed with minimal boilerplate. The engine's sole external dependency is **SFML.Net 3.0.0**.

---

## Table of Contents

- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Application](#application)
  - [GameApplicationOptions](#gameapplicationoptions)
  - [GameApplication](#gameapplication)
  - [GameHost](#gamehost)
  - [GameTime](#gametime)
  - [Scene Stack](#scene-stack)
- [Camera2D](#camera2d)
- [Abstractions](#abstractions)
  - [IGameObject](#igameobject)
  - [IMovableGameObject](#imovablegameobject)
  - [IRenderLayer](#irenderlayer)
  - [ISceneLayer](#iscenelayer)
- [Scenes](#scenes)
- [ECS](#ecs)
  - [Entity](#entity)
  - [World](#world)
- [Input](#input)
  - [InputSnapshot](#inputsnapshot)
  - [InputActionMap](#inputactionmap)
- [Assets](#assets)
- [Audio](#audio)
- [Animation](#animation)
- [Behaviors](#behaviors)
  - [MovementBehavior](#movementbehavior)
  - [MovementWithRotationBehavior](#movementwithrotationbehavior)
  - [MovementWithDriftingBehavior](#movementwithdriftingbehavior)
  - [RotationBehavior](#rotationbehavior)
  - [SteeringBehavior](#steeringbehavior)
  - [SteeringDebugDrawer](#steeringdebugdrawer)
  - [SteeringForces](#steeringforces)
- [Extensions](#extensions)
- [Shaders](#shaders)
- [Wall](#wall)
- [Spatial](#spatial)
- [Pathfinding](#pathfinding)
- [Constants](#constants)
- [Diagnostics](#diagnostics)
- [Design Patterns](#design-patterns)

---

## Architecture

```text
┌─────────────────────────────────────────────────────────────┐
│                      GameApplication                        │
│       window creation · main loop · frame timing            │
└──────────────────────────────┬──────────────────────────────┘
                               │ owns
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                        GameHost                             │
│          service locator — passed to every scene            │
│  Window │ Input │ Assets │ Audio │ World │ Camera │ Options │
└──┬───────┬────────┬─────────┬───────┬────────┬──────────────┘
   │       │        │         │       │        │
   ▼       ▼        ▼         ▼       ▼        ▼
Render  Input    Asset    Audio    ECS    Camera2D
Window  Tracker  Store    Player   World  (shake/zoom/follow)
           │                        │
           ▼                        ▼
      InputSnapshot          Entity / Components
    (per-frame snapshot)     (sparse-set stores)

┌─────────────────────────────────────────────────────────────┐
│                      SceneManager                           │
│      Load → Update/Render loop → Unload → next scene        │
└──────────────────────────────┬──────────────────────────────┘
                               │ manages
                               ▼
                        GameSceneBase
                     (virtual lifecycle hooks)
             Load · Update · RenderLayer · Unload
                               │ owns zero or more
                               ▼
                          ISceneLayer
                  (compositing units with render order)
              background (order < 0) · foreground (order ≥ 0)
```

| Layer | Responsibility |
|-------|----------------|
| **GameApplication** | Creates the SFML window, drives the main loop, measures frame time |
| **GameHost** | Service locator; the single object passed to every scene |
| **SceneManager** | Transitions between scenes at safe frame boundaries; supports a scene stack |
| **GameSceneBase** | User-defined game screens; override virtual methods and implement `RenderLayer` to draw; owns `ISceneLayer` objects |
| **ISceneLayer** | Lightweight compositing units attached to a scene; drawn in render-order before (background) or after (foreground) the scene's own content |
| **World / Entity** | Lightweight ECS; entities are integer IDs, components live in typed dictionaries |
| **AssetStore** | Loads and caches textures, fonts, sounds, and shaders |
| **InputTracker / InputSnapshot** | Builds a frame-scoped snapshot of keyboard, mouse, and joystick state |
| **AudioPlayer** | Manages active `Sound` instances and a streamed `Music` track; master + category volume control |
| **Camera2D** | 2D view with smooth follow, zoom, rotation, and screen-shake |
| **Behaviors** | Composable movement, rotation, and steering-force strategies |
| **SpatialGrid** | Grid-based spatial hash for fast radius-based neighbor queries |

---

## Getting Started

The minimal bootstrap creates a `GameApplicationOptions` record, instantiates `GameApplication`,
and calls `Run` with your first scene. Assets are resolved relative to `ContentRootPath`.

```csharp
using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Scenes;
using SFML.Graphics;
using SFML.System;

// Define a scene
sealed class TitleScene : GameSceneBase
{
    private Font? _font;

    public override void Load(GameHost host)
    {
        _font = host.Assets.LoadFont();
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        if (_font is null)
        {
            return;
        }

        window.DrawCenteredText(_font, 48, Color.White, "Hello, World!", 300f);
    }
}

// Configure and run
string contentRoot = Path.Combine(AppContext.BaseDirectory, "Assets");

GameApplicationOptions options = new()
{
    Title           = "My Game",
    WindowSize      = new Vector2u(1280, 720),
    ContentRootPath = contentRoot,
    FrameRateLimit  = 60,
    VerticalSyncEnabled = true,
};

GameApplication app = new(options);
app.Run(new TitleScene());
```

---

## Application

### Overview

The `Application` namespace contains the top-level bootstrap types used to configure and run the engine.
`GameApplication` owns the window and drives the main loop. `GameHost` is the service locator passed to
every scene and layer throughout the frame.

> **See demos:** [`OverviewScene`](../examples/GrayHare.GameEngine.DemoHub/Scenes/OverviewScene),
> [`ClearColorDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/ClearColorDemo)

---

### GameApplicationOptions

`GameApplicationOptions` is an init-only record used to configure the window and engine before startup.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Title` | `string` | `"GrayHare.GameEngine"` | Window title bar text |
| `WindowSize` | `Vector2u` | `(1280, 720)` | Initial window size in pixels |
| `ClearColor` | `Color` | `(18, 24, 32)` | Background colour cleared each frame |
| `FrameRateLimit` | `uint` | `60` | Frame-rate cap (0 = uncapped); ignored when `VerticalSyncEnabled` is `true` |
| `VerticalSyncEnabled` | `bool` | `true` | Enable vertical synchronization |
| `ContentRootPath` | `string` | `AppContext.BaseDirectory` | Root directory for resolving relative asset paths |
| `LogHandler` | `Action<string>?` | `null` | Optional delegate for engine diagnostics; `null` falls back to `Debug.WriteLine` |
| `State` | `SFML.Window.State` | `State.Windowed` | Window state (Windowed | Fullscreen) |
| `Style` | `SFML.Window.Styles` | `Stiles.Default` | Window style (None | Titlebar | Resize | Close | Default) |

---

### GameApplication

| Member | Parameters | Description |
|--------|-----------|-------------|
| `GameApplication(options?)` | `GameApplicationOptions?` | Creates the application; uses defaults when `options` is `null` |
| `Run(initialScene)` | `GameSceneBase` | Opens the window, loads the initial scene, and runs the main loop until the window is closed or `Exit()` is called |

---

### GameHost

`GameHost` is the service locator passed to every scene and layer. All engine subsystems are
accessible through its properties. Access is restricted to the main thread.

| Member | Type | Description |
|--------|------|-------------|
| `Window` | `RenderWindow` | The SFML render window |
| `Input` | `InputSnapshot` | Per-frame input state snapshot |
| `InputActions` | `InputActionMap?` | Optional named action map; assign in `Load` |
| `Assets` | `AssetStore` | Asset cache for textures, fonts, and sounds |
| `Audio` | `AudioPlayer` | Audio playback manager |
| `World` | `World` | ECS world for the current scene; cleared on scene change |
| `Camera` | `Camera2D` | 2D camera controlling the active view |
| `Options` | `GameApplicationOptions` | Options used to create the application |
| `TimeScale` | `float` | Current time-scale multiplier (0 = paused, 1 = normal) |
| `IsPaused` | `bool` | `true` when `TimeScale` is 0 |
| `ExitRequested` | `bool` | `true` after `Exit()` has been called |
| `SceneStackDepth` | `int` | Number of scenes currently on the stack |
| `Pause()` | — | Sets `TimeScale` to 0 |
| `Resume()` | — | Restores `TimeScale` to 1 |
| `SetTimeScale(timeScale)` | `float` | Sets `TimeScale` (clamped to ≥ 0) |
| `ChangeScene(scene)` | `GameSceneBase` | Replaces the entire stack at end of frame; ECS world is cleared |
| `PushScene(overlay)` | `GameSceneBase` | Pushes an overlay scene on top; current scene receives `OnDeactivated` |
| `PopScene()` | — | Removes the top scene at end of frame; scene below receives `OnActivated` |
| `Exit()` | — | Signals the main loop to close and stop |

> **See demo:** [`TimeScaleDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/TimeScaleDemo)

---

### GameTime

`GameTime` is an immutable per-frame snapshot of engine timing. Use `gameTime.DeltaTotalSeconds`
for movement and timers. Use `RawDeltaTotalSeconds` only when behaviour must ignore time scaling.

| Member | Type | Description |
|--------|------|-------------|
| `Total` | `TimeSpan` | Accumulated scaled time since the first frame |
| `Delta` | `TimeSpan` | Scaled duration of the current frame |
| `RawTotal` | `TimeSpan` | Accumulated real (unscaled) time since the first frame |
| `RawDelta` | `TimeSpan` | Real-clock duration of the current frame |
| `TimeScale` | `float` | Time multiplier active when this snapshot was produced |
| `FrameNumber` | `ulong` | Monotonically increasing frame counter |
| `IsPaused` | `bool` | `true` when `TimeScale` is 0 |
| `DeltaTotalSeconds` | `float` | Scaled frame delta in seconds (use for movement and timers) |
| `RawDeltaTotalSeconds` | `float` | Raw frame delta in seconds (use for shake, UI, and pause-aware timers) |
| `Start` (static) | `GameTime` | Zero-valued sentinel used before the first frame |
| `Advance(rawDelta, timeScale)` | `TimeSpan`, `float` | Returns the next `GameTime` advanced by the given delta |

---

### Scene Stack

The engine supports a stack of scenes in addition to the standard single-scene flow.

| Host method | When to use |
|-------------|-------------|
| `ChangeScene(scene)` | Full transition — replace the entire stack; ECS world is cleared |
| `PushScene(overlay)` | Push a pause menu, game-over screen, or any overlay on top; the scene beneath receives `OnDeactivated` |
| `PopScene()` | Remove the top scene; the scene beneath receives `OnActivated` |

Scene transitions are deferred to the end of the frame. The ECS world is cleared only on `ChangeScene`;
a pushed overlay shares the same world as the scene below it.

> **See demos:** [`SceneStackDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/SceneStackDemo),
> [`HideDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/HideDemo),
> [`MultipleLayersDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/MultipleLayersDemo)

---

## Camera2D

### Overview

`Camera2D` wraps an SFML `View` and is attached to `GameHost.Camera`. It provides smooth
following, zoom, rotation, and a decaying screen-shake effect. The engine applies the camera view
automatically each frame before rendering. Access is restricted to the main thread.

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `Camera2D(viewportSize)` | `Vector2u` | — | Create a camera whose viewport matches the window size; centered on the viewport |
| `Position` | — | `Vector2f` | World-space center of the camera; get/set |
| `Zoom` | — | `float` | Zoom level: 1 = default, > 1 = zoom in, < 1 = zoom out; clamped to ≥ 0.01 |
| `Rotation` | — | `float` | Camera rotation in degrees (clockwise); get/set |
| `ViewportSize` | — | `Vector2f` | Viewport dimensions; set from window size at construction |
| `Follow(target, lerpSpeed, deltaTime)` | `Vector2f`, `float`, `float` | `void` | Smoothly lerp toward `target`; `lerpSpeed` controls tracking speed |
| `Shake(intensity, duration)` | `float`, `float` | `void` | Start a screen-shake that decays linearly over `duration` seconds |
| `UpdateShake(deltaTime)` | `float` | `void` | Advance the shake timer; called automatically by the engine each frame with raw delta time |
| `GetView()` | — | `View` | Produce an SFML `View` reflecting position, zoom, rotation, and current shake offset |
| `ScreenToWorld(screenPos)` | `Vector2i` | `Vector2f` | Convert a screen-space pixel position to world space |
| `WorldToScreen(worldPos)` | `Vector2f` | `Vector2i` | Convert a world-space position to a screen-space pixel |
| `Reset()` | — | `void` | Restore default state: centered, zoom 1, no rotation, no shake |

> **See demos:** [`CameraDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/CameraDemo),
> [`CameraExtrasDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/CameraExtrasDemo)

---

## Abstractions

### Overview

These interfaces define the contracts that game objects and rendering components must satisfy.
Implement `IGameObject` for anything that can be placed and drawn in the world. Implement
`IMovableGameObject` for objects that participate in the behavior and steering systems.

---

### IGameObject

| Member | Type | Description |
|--------|------|-------------|
| `Position` | `Vector2f` | World-space position |
| `Rotation` | `float` | Rotation in degrees |
| `Origin` | `Vector2f` | Local origin (pivot point) |
| `Scale` | `Vector2f` | Scale applied to the object |
| `ZOrder` | `int` | Draw-ordering hint; lower values are drawn first |
| `GlobalBounds` | `FloatRect` | Axis-aligned bounding box in world space |
| `Draw(window)` | `RenderWindow` | Draw the object to `window` |
| `Update(deltaTime)` | `float` | Advance the object's state by `deltaTime` seconds |
| `Dispose()` | — | Release SFML resources held by the object |

---

### IMovableGameObject

Extends `IGameObject` with the physics properties required by the behavior and steering systems.

| Member | Type | Description |
|--------|------|-------------|
| `Mass` | `float` | Object mass; heavier objects turn and accelerate more slowly |
| `Heading` | `Vector2f` | Unit direction vector derived from `Rotation` |
| `Side` | `Vector2f` | Right-perpendicular of `Heading` |
| `Velocity` | `Vector2f` | Current velocity vector |
| `Speed` | `float` | Scalar speed; equivalent to `Velocity.Length` |
| `Acceleration` | `float` | Acceleration force applied per second when moving forward |
| `Deceleration` | `float` | Passive deceleration applied per second when no input is active |
| `BrakingDeceleration` | `float` | Stronger deceleration applied per second when braking |
| `MaxSpeed` | `float` | Maximum speed the object may reach |
| `TurnRate` | `float` | Turn rate in degrees per second (before mass scaling) |
| `MaxTurnRate` | `float` | Hard cap on the turn rate after mass scaling |

---

### IRenderLayer

`IRenderLayer` defines the contract for any object that can draw content to the render window.
It is implemented by `GameSceneBase` and `ISceneLayer`.

| Member | Parameters | Description |
|--------|-----------|-------------|
| `RenderLayer(host, window)` | `GameHost`, `RenderWindow` | Draw the layer's content to `window` |

---

### ISceneLayer

`ISceneLayer` extends `IRenderLayer` with load/unload and per-frame update. Layers are registered
on a `GameSceneBase` via `AddLayer`. Their `RenderOrder` determines rendering position relative
to the scene's own content: negative orders render first (background), non-negative orders render last
(foreground/overlay).

| Member | Parameters | Description |
|--------|-----------|-------------|
| `RenderOrder` | — | Rendering position: < 0 renders before the scene, ≥ 0 renders after |
| `Load(host)` | `GameHost` | Initialize and load resources for this layer |
| `Unload(host)` | `GameHost` | Release resources held by this layer |
| `Update(host, gameTime)` | `GameHost`, `in GameTime` | Advance game logic for one frame |
| `RenderLayer(host, window)` | `GameHost`, `RenderWindow` | Draw the layer's content |
| `OnActivated(host)` | `GameHost` | Called when the owning scene becomes the top of the stack; default no-op |
| `OnDeactivated(host)` | `GameHost` | Called when another scene is pushed on top; default no-op |

---

## Scenes

### Overview

All game screens derive from `GameSceneBase`. The class manages the render stack of `ISceneLayer`
objects and implements the lifecycle that `SceneManager` drives.

| Method | Description |
|--------|-------------|
| `Load(host)` | Called once when the scene becomes active; load assets and register layers here |
| `Update(host, gameTime)` | Called once per frame; forwards to all registered layers |
| `RenderLayer(host, window)` | **Abstract** — draw the scene's own content; called between layers with negative and non-negative render orders |
| `Render(host, window)` | Called by the engine; not for override — renders layers then `RenderLayer` |
| `Unload(host)` | Called when the scene is replaced; unloads layers in reverse registration order |
| `OnActivated(host)` | Called when this scene becomes the top of the stack (initial activation or pop above it) |
| `OnDeactivated(host)` | Called when another scene is pushed on top |
| `AddLayer(layer)` | Register a layer; inserted at the correct position by `RenderOrder` (stable ascending sort) |
| `RemoveLayer(layer)` | Remove a previously registered layer; returns `true` if found |
| `Name` | Virtual property; defaults to the concrete type name |
| `Dispose()` | Override `Dispose(bool disposing)` to release scene-specific resources |

> **See demos:** [`MultipleLayersDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/MultipleLayersDemo),
> [`SceneStackDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/SceneStackDemo),
> [`HideDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/HideDemo)

---

## ECS

### Overview

The ECS module provides a lightweight Entity-Component-System. Entities are integer IDs wrapped in a
generational handle (`Entity`). Components are plain C# types stored in per-type dictionaries inside
a `World` instance. The `World` is scoped to the active scene and cleared automatically on
`host.ChangeScene`.

---

### Entity

`Entity` is a `readonly record struct` with two fields:

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `int` | Unique entity identifier within the world |
| `Generation` | `int` | Incremented each time the ID is recycled; stale handles compare unequal to live entities |

---

### World

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `EntityCount` | — | `int` | Number of live entities |
| `ComponentTypeCount` | — | `int` | Number of distinct component types registered |
| `CreateEntity()` | — | `Entity` | Create a new entity; recycles IDs from destroyed entities |
| `Exists(entity)` | `Entity` | `bool` | `true` if the entity is alive in this world |
| `DestroyEntity(entity)` | `Entity` | `void` | Destroy the entity and remove all its components; no-op on stale handles |
| `Clear()` | — | `void` | Remove all entities and components; resets the ID counter |
| `AddComponent<T>(entity, component)` | `Entity`, `T` | `void` | Attach or replace a component on an entity; throws when entity does not exist |
| `RemoveComponent<T>(entity)` | `Entity` | `bool` | Remove a component; returns `true` if it was present |
| `HasComponent<T>(entity)` | `Entity` | `bool` | `true` if the entity has the given component type |
| `TryGetComponent<T>(entity, out T)` | `Entity` | `bool` | Try to retrieve a component; returns `false` when absent |
| `GetComponent<T>(entity)` | `Entity` | `T` | Retrieve a component; throws `KeyNotFoundException` when absent |
| `ComponentCount<T>()` | — | `int` | Number of entities with the given component type |
| `Query<T>()` | — | `IEnumerable<Entity>` | Enumerate all entities that have `T` |
| `Query<TA, TB>()` | — | `IEnumerable<Entity>` | Enumerate all entities that have both `TA` and `TB` |
| `Query<TA, TB, TC>()` | — | `IEnumerable<Entity>` | Enumerate all entities that have `TA`, `TB`, and `TC`; iterates the smallest store first |
| `ForEach<T>(action)` | `Action<Entity, T>` | `void` | Invoke `action` for every entity with `T`; snapshot-safe — action may modify components |
| `ForEach<TA, TB>(action)` | `Action<Entity, TA, TB>` | `void` | Two-component variant; iterates smallest store first |
| `ForEach<TA, TB, TC>(action)` | `Action<Entity, TA, TB, TC>` | `void` | Three-component variant; iterates smallest store first |

> **See demos:** [`EcsDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/EcsDemo),
> [`Ecs3Demo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/Ecs3Demo),
> [`EcsRecyclingDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/EcsRecyclingDemo),
> [`EcsComponentOpsDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/EcsComponentOpsDemo)

---

## Input

### Overview

Input is captured once per frame by `InputTracker` and exposed as an immutable `InputSnapshot` on
`host.Input`. An optional `InputActionMap` on `host.InputActions` maps named actions to physical
keys, mouse buttons, joystick buttons, and axes — allowing binding remapping without changing gameplay code.

---

### InputSnapshot

`InputSnapshot` is the per-frame input state. Do not store references across frames; access through
`host.Input` each frame.

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `Empty` (static) | — | `InputSnapshot` | An empty snapshot with no active input; used as a safe null-object default |
| `CurrentKeys` | — | `IReadOnlySet<Keyboard.Key>` | All keyboard keys currently held down |
| `PreviousKeys` | — | `IReadOnlySet<Keyboard.Key>` | Keys that were held last frame |
| `CurrentButtons` | — | `IReadOnlySet<Mouse.Button>` | Mouse buttons currently held |
| `PreviousButtons` | — | `IReadOnlySet<Mouse.Button>` | Mouse buttons held last frame |
| `MousePosition` | — | `Vector2i` | Current mouse position in window coordinates |
| `MouseWheelDelta` | — | `float` | Accumulated mouse-wheel scroll delta this frame |
| `ConnectedJoysticks` | — | `IReadOnlySet<uint>` | IDs of currently connected joysticks |
| `IsKeyDown(key)` | `Keyboard.Key` | `bool` | `true` while `key` is held |
| `WasKeyPressed(key)` | `Keyboard.Key` | `bool` | `true` on the first frame `key` was pressed |
| `WasAnyKeyPressed()` | — | `bool` | `true` on the first frame any key was pressed |
| `WasKeyReleased(key)` | `Keyboard.Key` | `bool` | `true` on the first frame `key` was released |
| `IsMouseButtonDown(button)` | `Mouse.Button` | `bool` | `true` while `button` is held |
| `WasMouseButtonPressed(button)` | `Mouse.Button` | `bool` | `true` on the first frame `button` was pressed |
| `WasMouseButtonReleased(button)` | `Mouse.Button` | `bool` | `true` on the first frame `button` was released |
| `IsJoystickConnected(joystickId)` | `uint` | `bool` | `true` when the joystick is connected |
| `IsJoystickButtonDown(joystickId, button)` | `uint`, `uint` | `bool` | `true` while the joystick button is held |
| `WasJoystickButtonPressed(joystickId, button)` | `uint`, `uint` | `bool` | `true` on the first frame the joystick button was pressed |
| `WasJoystickButtonReleased(joystickId, button)` | `uint`, `uint` | `bool` | `true` on the first frame the joystick button was released |
| `GetJoystickAxis(joystickId, axis)` | `uint`, `Joystick.Axis` | `float` | Current axis value; returns 0 when the joystick or axis is not found |

---

### InputActionMap

`InputActionMap` maps named actions to one or more physical bindings. Assign to
`host.InputActions` in `Load` to make it available throughout the scene.
Action names are case-insensitive. Multiple bindings per action are supported.

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `MapKey(action, key)` | `string`, `Keyboard.Key` | `void` | Bind a keyboard key to a named action |
| `MapButton(action, joystickId, button)` | `string`, `uint`, `uint` | `void` | Bind a joystick button to a named action |
| `MapMouseButton(action, button)` | `string`, `Mouse.Button` | `void` | Bind a mouse button to a named action |
| `MapAxis(action, joystickId, axis, deadZone?)` | `string`, `uint`, `Joystick.Axis`, `float` | `void` | Bind a joystick axis; default dead zone is 10 |
| `IsActionDown(action, input)` | `string`, `InputSnapshot` | `bool` | `true` while any binding for the action is held (keys, mouse, joystick) |
| `WasActionPressed(action, input)` | `string`, `InputSnapshot` | `bool` | `true` on the first frame any binding was pressed |
| `WasActionReleased(action, input)` | `string`, `InputSnapshot` | `bool` | `true` on the first frame any binding was released |
| `GetAxisValue(action, input)` | `string`, `InputSnapshot` | `float` | Returns the first axis value outside the dead zone; 0 if none |
| `ClearAction(action)` | `string` | `void` | Remove all bindings for a named action |
| `ClearAll()` | — | `void` | Remove all bindings |

> **See demos:** [`InputDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/InputDemo),
> [`InputActionDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/InputActionDemo),
> [`GameControllerDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/GameControllerDemo)

---

## Assets

### Overview

`AssetStore` loads and caches all external resources — textures, images, fonts, sound buffers, and
shaders — keyed by resolved path. All SFML formats are supported for images and textures; additionally,
PPM P3 (ASCII) and PPM P6 (binary) files are decoded internally. Paths are resolved against
`ContentRootPath` using `ResolvePath`. Calling any `Load*` method with the same path returns the
cached instance. Access is restricted to the main thread.

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `AssetStore(contentRootPath)` | `string` | — | Initialize with the given content root directory |
| `ContentRootPath` | — | `string` | Absolute path to the root directory |
| `ResolvePath(assetPath)` | `string` | `string` | Resolve a relative path against `ContentRootPath` |
| `LoadImage(assetPath)` | `string` | `Image` | Load an image; PPM P3/P6 supported; throws `AssetNotFoundException` when missing |
| `LoadTexture(assetPath, smooth?)` | `string`, `bool` | `Texture` | Load a texture; returns a fallback 8×8 magenta texture when the file is missing |
| `LoadFont(assetPath?)` | `string?` | `Font` | Load a font; falls back to a system font when `null` or when the file is missing |
| `LoadSoundBuffer(assetPath)` | `string` | `SoundBuffer` | Load a sound buffer; throws `AssetNotFoundException` when missing |
| `LoadShader(fragPath)` | `string` | `Shader` | Load a fragment-only shader; throws `ShaderCompilationException` on failure |
| `LoadShader(vertPath, fragPath)` | `string`, `string` | `Shader` | Load a vertex + fragment shader pair; throws on failure |
| `TryLoadShader(fragPath, out reason)` | `string`, `out string?` | `Shader?` | Load a fragment shader; returns `null` with a message on failure |
| `TryLoadShader(vertPath, fragPath, out reason)` | `string`, `string`, `out string?` | `Shader?` | Load a shader pair; returns `null` with a message on failure |
| `Unload(assetPath)` | `string` | `void` | Remove and dispose a single cached asset; no-op when not cached |
| `UnloadAll()` | — | `void` | Remove and dispose all cached assets; the store remains open for new loads |
| `Dispose()` | — | `void` | Dispose the store and all cached assets |

> **See demos:** [`SpriteDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/SpriteDemo),
> [`AssetFallbackDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/AssetFallbackDemo),
> [`TextDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/TextDemo),
> [`ShapeTextureDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/ShapeTextureDemo)

---

## Audio

### Overview

`AudioPlayer` manages a pool of active `Sound` instances and a single streamed `Music` track.
It provides master, SFX, and music volume controls plus a mute toggle. Sounds are played from
`SoundBuffer` assets loaded through `AssetStore`. Access is restricted to the main thread.

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `AudioPlayer(assets)` | `AssetStore` | — | Initialize with the asset store used for buffer loading |
| `MasterVolume` | — | `float` | Master volume (0–100); applied on top of both categories |
| `SfxVolume` | — | `float` | Sound effect volume (0–100) |
| `MusicVolume` | — | `float` | Music volume (0–100) |
| `IsMuted` | — | `bool` | `true` when all output is silenced; stored volumes are preserved |
| `IsMusicPlaying` | — | `bool` | `true` when a music track is currently playing |
| `MaxActiveSounds` | — | `int` | Pool size cap; `PlaySound` returns `null` when the limit is reached (default 32) |
| `ActiveSoundCount` | — | `int` | Number of `Sound` instances currently playing or paused |
| `PreloadSound(assetPath)` | `string` | `void` | Pre-allocate a pooled source to avoid hitches on first play |
| `PlaySound(assetPath, volume?, pitch?)` | `string`, `float`, `float` | `Sound?` | Play a sound effect; returns the `Sound` instance or `null` when the pool is full |
| `StopAllSounds()` | — | `void` | Stop all active sound effects |
| `PlayMusic(assetPath, loop?)` | `string`, `bool` | `void` | Start or restart streamed music |
| `PauseMusic()` | — | `void` | Pause the current music track |
| `ResumeMusic()` | — | `void` | Resume a paused music track |
| `StopMusic()` | — | `void` | Stop and unload the current music track |
| `SetMasterVolume(volume)` | `float` | `void` | Set master volume; reapplies to all active sounds and music |
| `SetSfxVolume(volume)` | `float` | `void` | Set SFX category volume; reapplies to all active sounds |
| `SetMusicVolume(volume)` | `float` | `void` | Set music category volume; reapplies to the current music track |
| `Mute()` | — | `void` | Silence all output without losing stored volumes |
| `Unmute()` | — | `void` | Restore output using stored volumes |
| `Update()` | — | `void` | Called automatically by the engine each frame to remove finished sounds from the pool |
| `Dispose()` | — | `void` | Stop all audio and release SFML resources |

> **See demos:** [`AudioDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/AudioDemo),
> [`MusicDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/MusicDemo)

---

## Animation

### Overview

The animation system drives frame-based sprite animation. An `AnimationClip` holds the ordered list
of `AnimationFrame` records. `AnimationPlayer` advances playback and renders the current frame.

---

### AnimationFrame

A `readonly record struct` representing a single frame.

| Member | Type | Description |
|--------|------|-------------|
| `Texture` | `Texture` | The SFML texture to display for this frame |
| `Duration` | `TimeSpan` | How long this frame is displayed before advancing |

---

### AnimationClip

Holds an ordered list of `AnimationFrame` records.

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `AnimationClip(frames)` | `IReadOnlyList<AnimationFrame>` | — | Create a clip from the given frames; throws when the list is empty |
| `Frames` | — | `IReadOnlyList<AnimationFrame>` | The frames in playback order |

---

### AnimationPlayer

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `AnimationPlayer(clip, isLooping, autoPlay?)` | `AnimationClip`, `bool`, `bool` | — | Create a player; `autoPlay` defaults to `true` |
| `IsFinished` | — | `bool` | `true` when a non-looping clip has displayed its last frame |
| `IsLooping` | — | `bool` | `true` when the clip loops after the last frame |
| `IsPaused` | — | `bool` | `true` when playback is paused |
| `Position` | — | `Vector2f` | World-space render position; get/set |
| `Scale` | — | `Vector2f` | Scale factor; get/set |
| `Rotation` | — | `float` | Rotation in degrees; get/set |
| `FrameIndex` | — | `int` | Current frame index; negative values clamp to 0; values beyond last frame wrap |
| `Play()` | — | `void` | Start or resume playback from the current frame |
| `Pause()` | — | `void` | Freeze playback at the current frame |
| `Resume()` | — | `void` | Resume after `Pause` |
| `Reset()` | — | `void` | Rewind to frame 0 and clear the finished flag |
| `Update(delta)` | `TimeSpan` | `void` | Advance playback; call once per frame |
| `Render(window)` | `RenderWindow` | `void` | Draw the current frame; call after `Update` |
| `Dispose()` | — | `void` | Release SFML sprite resources |

> **See demos:** [`AnimationDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/AnimationDemo),
> [`ExplosionAnimationDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/ExplosionAnimationDemo),
> [`AnimationOneShotDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/AnimationOneShotDemo)

---

## Behaviors

### Overview

The `Behaviors` namespace provides composable movement strategies for `IMovableGameObject` implementations.
All behaviors are stateless relative to the game object — they compute and return a new velocity or a
force vector each frame, leaving the caller in full control of state.

---

### MovementBehavior

Applies force-based acceleration, braking, and passive deceleration. Rotation and velocity are
independent — suitable for free-drifting movement (top-down tanks, space shooters).

| Member | Description |
|--------|-------------|
| `MovementBehavior(gameObject)` | Initialize for `gameObject` |
| `IsMovingForwards` | Accelerate along the current heading |
| `IsMovingBackwards` | Decelerate or reverse along the heading |
| `IsBraking` | Apply braking deceleration; clamped to zero (no reverse); ignored when `IsMovingForwards` is set |
| `IsStrafingLeft` | Strafe perpendicular-left relative to heading |
| `IsStrafingRight` | Strafe perpendicular-right relative to heading |
| `UpdateMovement(deltaTime, currentVelocity)` | Compute and return the new velocity for this frame |

> **See demos:** [`MovementDemos`](../examples/GrayHare.GameEngine.DemoHub/Scenes/MovementDemos),

---

### MovementWithRotationBehavior

Couples heading rotation to movement direction. Heading, position, and velocity are all updated together.

| Member | Description |
|--------|-------------|
| `MovementWithRotationBehavior(gameObject)` | Initialize for `gameObject` |
| `IsMovingForwards` | Accelerate in the heading direction |
| `IsMovingBackwards` | Decelerate or reverse |
| `IsBraking` | Apply braking deceleration; never reverses |
| `IsTurningLeft` | Rotate the heading counter-clockwise |
| `IsTurningRight` | Rotate the heading clockwise |
| `UpdateMovement(deltaTime, currentVelocity, currentRotation)` | Returns the new `(velocity, rotation)` tuple for this frame |

> **See demo:** [`MovementDemos`](../examples/GrayHare.GameEngine.DemoHub/Scenes/MovementDemos)

---

### MovementWithDriftingBehavior

Decouples steering input from velocity so that momentum carries the object after turning — producing
a drifting/slide feel.

| Member | Description |
|--------|-------------|
| `MovementWithDriftingBehavior(gameObject)` | Initialize for `gameObject` |
| `IsMovingForwards` | Accelerate in the heading direction |
| `IsMovingBackwards` | Decelerate or reverse |
| `IsBraking` | Apply braking deceleration |
| `IsTurningLeft` | Rotate heading counter-clockwise |
| `IsTurningRight` | Rotate heading clockwise |
| `UpdateMovement(deltaTime, currentVelocity, currentRotation)` | Returns the new `(velocity, rotation)` tuple for this frame |

> **See demo:** [`MovementDemos`](../examples/GrayHare.GameEngine.DemoHub/Scenes/MovementDemos)

---

### RotationBehavior

Rotates an object toward a target angle or target position, with configurable turn rate and direction.

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `RotationBehavior(gameObject)` | `IMovableGameObject` | — | Initialize for `gameObject` |
| `RotateToward(targetAngle, deltaTime)` | `float`, `float` | `float` | Rotate toward `targetAngle` in degrees; returns the new rotation |
| `RotateTowardPosition(targetPosition, deltaTime)` | `Vector2f`, `float` | `float` | Rotate toward a world-space position; returns the new rotation |
| `RotateBy(degrees, deltaTime, direction)` | `float`, `float`, `RotationDirection` | `float` | Rotate by a fixed amount in the given direction; returns the new rotation |

`RotationDirection` is an enum with values `Clockwise` and `CounterClockwise`.

> **See demo:** [`MovementDemos`](../examples/GrayHare.GameEngine.DemoHub/Scenes/MovementDemos)

---

### SteeringBehavior

Computes autonomous steering forces for a single `IMovableGameObject`. All methods return a
`Vector2f` force to be summed and applied as:

```text
acceleration = totalForce / mass
velocity    += acceleration * deltaTime
position    += velocity * deltaTime
```

| Method | Parameters | Description |
|--------|-----------|-------------|
| `SteeringBehavior(gameObject)` | `IMovableGameObject` | Initialize for `gameObject` |
| `Seek(targetPosition)` | `Vector2f` | Steer directly toward the target |
| `Flee(targetPosition)` | `Vector2f` | Steer directly away from the target |
| `Arrive(targetPosition, slowingRadius)` | `Vector2f`, `float` | Seek but taper speed within `slowingRadius` |
| `Pursue(target)` | `IMovableGameObject` | Seek the predicted future position of a moving target |
| `Evade(target)` | `IMovableGameObject` | Flee the predicted future position of a moving target |
| `Wander(ref wanderAngle, wanderRadius, wanderDistance)` | `ref float`, `float`, `float` | Smooth random wandering; `wanderAngle` is updated in place |
| `ObstacleAvoidance(obstacles, detectionLength, agentRadius)` | `IReadOnlyList<IGameObject>`, `float`, `float` | Lateral + braking force to dodge the closest obstacle in the detection box |
| `WallAvoidance(walls, feelerLength, feelerAngle)` | `IReadOnlyList<Wall>`, `float`, `float` | Three-feeler force pushing away from the closest wall intersection |
| `StayWithinBounds(boundary, margin)` | `FloatRect`, `float` | Restoring force toward the inside of `boundary` |
| `OffsetPursuit(leader, offset)` | `IMovableGameObject`, `Vector2f` | Arrive at a fixed offset in the leader's local coordinate frame |
| `Interpose(agentA, agentB)` | `IMovableGameObject`, `IMovableGameObject` | Arrive at the midpoint between two moving agents |
| `Separation(neighbors, separationRadius)` | `IReadOnlyList<IMovableGameObject>`, `float` | Push away from neighbors within `separationRadius` |
| `Alignment(neighbors)` | `IReadOnlyList<IMovableGameObject>` | Match the average heading of neighbors |
| `Cohesion(neighbors)` | `IReadOnlyList<IMovableGameObject>` | Seek the average position of neighbors |

> **See demos:** [`MovementDemos`](../examples/GrayHare.GameEngine.DemoHub/Scenes/MovementDemos),
> [`SeekArriveDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/SeekArriveDemo),
> [`PursueEvadeDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/PursueEvadeDemo),
> [`InterposeDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/InterposeDemo),
> [`OffsetPursuitDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/OffsetPursuitDemo),
> [`FollowPathDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/FollowPathDemo),
> [`AvoidanceDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/AvoidanceDemo),
> [`FlockingDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/FlockingDemo),
> [`FlockingLeaderDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/FlockingLeaderDemo),
> [`FlockingSpatialGridDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/FlockingSpatialGridDemo)

---

### SteeringDebugDrawer

Static debug-rendering helpers for visualizing steering and spatial state.

| Method | Parameters | Description |
|--------|-----------|-------------|
| `Enabled` (property) | — | Toggle all debug drawing; default `true` |
| `DrawVelocity(window, agent, color?)` | `RenderWindow`, `IMovableGameObject`, `Color?` | Draw the velocity vector |
| `DrawHeading(window, agent, color?)` | `RenderWindow`, `IMovableGameObject`, `Color?` | Draw the heading vector |
| `DrawDetectionBox(window, agent, detectionLength, color?)` | `RenderWindow`, `IMovableGameObject`, `float`, `Color?` | Draw the obstacle-avoidance detection box |
| `DrawWallFeelers(window, agent, feelerLength, feelerAngle, color?)` | `RenderWindow`, `IMovableGameObject`, `float`, `float`, `Color?` | Draw the three wall-avoidance feelers |
| `DrawNeighborhoodRadius(window, agent, radius, color?)` | `RenderWindow`, `IMovableGameObject`, `float`, `Color?` | Draw the flocking neighborhood circle |
| `DrawSpatialGrid(window, grid, font?)` | `RenderWindow`, `SpatialGrid<IMovableGameObject>`, `Font?` | Draw occupied cells and optional cell-count labels |

---

### SteeringForces

`SteeringForces` is an immutable record used to pass named pre-computed forces to the standard
truncated-sum integration pipeline.

| Member | Type | Description |
|--------|------|-------------|
| `Seek` | `Vector2f` | Seek force (default `Zero`) |
| `Flee` | `Vector2f` | Flee force |
| `Arrive` | `Vector2f` | Arrive force |
| `Pursue` | `Vector2f` | Pursue force |
| `Evade` | `Vector2f` | Evade force |
| `Wander` | `Vector2f` | Wander force |
| `ObstacleAvoidance` | `Vector2f` | Obstacle avoidance force |
| `WallAvoidance` | `Vector2f` | Wall avoidance force |
| `Separation` | `Vector2f` | Flocking separation force |
| `Alignment` | `Vector2f` | Flocking alignment force |
| `Cohesion` | `Vector2f` | Flocking cohesion force |
| `StayWithinBounds` | `Vector2f` | Bounds-restoring force |

---

## Extensions

### Overview

The `Extensions` namespace provides extension methods for SFML primitives, reducing boilerplate
in scene and behavior code.

---

### FloatExtensions

Extension methods on `float`.

| Method | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `ToVector2f(degrees)` | — | `Vector2f` | Convert a heading angle in degrees to a unit direction vector |

---

### VectorExtensions

Extension methods on `Vector2f`.

| Method | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `DistanceTo(vector, other)` | `Vector2f` | `float` | Euclidean distance between two positions |
| `Truncate(vector, maxLength)` | `float` | `Vector2f` | Clamp the vector's length to `maxLength` |
| `WrapPosition(vector, worldSize)` | `Vector2f` | `Vector2f` | Wrap a position for toroidal (screen-edge) movement |
| `WrapPosition(vector, windowSize)` | `Vector2u` | `Vector2f` | `Vector2u` overload |

---

### ShapeExtensions

Extension methods on SFML shapes.

| Method | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `ToTexture(shape, padding?)` | `int` | `Texture` | Rasterize any `Shape` to a reusable `Texture`; `padding` adds extra pixels around the shape |

> **See demo:** [`ShapeTextureDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/ShapeTextureDemo)
> [`MovementDemos`](../examples/GrayHare.GameEngine.DemoHub/Scenes/MovementDemos)

---

### WindowExtensions

Extension methods on `RenderWindow`.

| Method | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `DrawCenteredText(window, font, size, color, text, y, style?)` | `Font`, `uint`, `Color`, `string`, `float`, `Text.Styles?` | `void` | Draw text horizontally centered at the given Y coordinate |
| `GetCenter(window)` | | `Vector2f` | Returns the center coordinates |

> **See demos:** [`SpriteDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/SpriteDemo),
> [`TextDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/TextDemo),

---

## Shaders

### Overview

GLSL shader support is provided through `AssetStore`. Shaders are loaded, compiled, and cached per
path. `GlslVersionParser` is a static utility used internally by `TryLoadShader` to include the
detected GLSL version number in failure messages, aiding cross-platform diagnosis of
version-mismatch errors.

---

### GlslVersionParser (static)

| Method | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `Parse(shaderSource)` | `string` | `int?` | Extract the GLSL version number from a `#version NNN` directive; returns `null` if absent |

---

### Loading Shaders via AssetStore

| Method | Description |
|--------|-------------|
| `assets.LoadShader(fragPath)` | Load a fragment-only shader; throws `ShaderCompilationException` on failure |
| `assets.LoadShader(vertPath, fragPath)` | Load a vertex + fragment shader pair; throws on failure |
| `assets.TryLoadShader(fragPath, out reason)` | Load a fragment shader; returns `null` with a descriptive message on failure |
| `assets.TryLoadShader(vertPath, fragPath, out reason)` | Load a shader pair; returns `null` with a descriptive message on failure |

> **Platform note:** GLSL version support varies by GPU driver and OS. Use `TryLoadShader` in
> production builds to degrade gracefully when a shader is not supported on the target platform.

> **See demos:** [`ShaderBlurDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/ShaderBlurDemo),
> [`ShaderGrayscaleDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/ShaderGrayscaleDemo),
> [`ShaderHighlanderDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/ShaderHighlanderDemo),
> [`ShaderPixelateDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/ShaderPixelateDemo),
> [`ShaderWaveDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/ShaderWaveDemo),
> [`ShaderStormBlinkDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/ShaderStormBlinkDemo),
> [`ShaderFlockComboDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/ShaderFlockComboDemo)

---

## Wall

### Overview

`Wall` is an immutable `readonly record struct` representing a directed line segment used primarily
by `SteeringBehavior.WallAvoidance()`. The wall's inward-facing normal is computed automatically
from the start-to-end direction at construction time. Swapping `Start` and `End` reverses the
normal, making the wall face the opposite side.

| Member | Type | Description |
|--------|------|-------------|
| `Wall(start, end)` | Constructor | Construct the wall and compute its unit normal |
| `Start` | `Vector2f` | The start point of the segment |
| `End` | `Vector2f` | The end point of the segment |
| `Normal` | `Vector2f` | Unit normal vector (left perpendicular of `Start → End`); the side the wall faces |
| `TryGetIntersection(from, to, out test)` | `Vector2f`, `Vector2f`, `out float` → `bool` | Parametric line-segment intersection; `test` ∈ [0, 1] is the normalized position along the `from → to` ray |

> **See demos:** [`AvoidanceDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/AvoidanceDemo),
> [`SteeringDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/SteeringDemo),
> [`PathfindingAgentDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/PathfindingAgentDemo)

---

## Spatial

### Overview

The `Spatial` module provides `SpatialGrid<T>`, a generic grid-based spatial hash that
partitions 2D space into fixed-size cells for fast radius-based neighbor queries. It is
designed for a per-frame rebuild workflow and integrates directly with the flocking methods
on `SteeringBehavior` (Separation, Alignment, Cohesion) by returning results in a
`List<T>` that implements `IReadOnlyList<T>`.

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `SpatialGrid(cellSize)` | `float` | — | Create a grid with the given cell width/height; a good default equals the largest query radius |
| `CellSize` | — | `float` | The width and height of each grid cell |
| `Count` | — | `int` | The number of items currently stored in the grid |
| `Clear()` | — | `void` | Remove all items; cell lists are pooled internally so subsequent `Add` calls reuse them |
| `Add(item, position)` | `T`, `Vector2f` | `void` | Insert an item at the given world-space position |
| `FindNeighbors(position, radius, results, exclude?)` | `Vector2f`, `float`, `List<T>`, `T?` | `int` | Clear `results`, fill with items within `radius`, and return the count; optionally skip `exclude` |
| `EnumerateCells()` | — | `IEnumerable<(Vector2f, int)>` | Yield each occupied cell's world-space origin and item count (for debug visualization) |

> **Performance notes:**
> - Cell lists are pooled so that `Clear` → `Add` cycles produce zero allocations after warm-up.
> - `FindNeighbors` uses squared-distance checks (no `MathF.Sqrt`) in the inner loop.
> - Choose `cellSize` close to the largest query radius; smaller cells reduce per-query work
>   but increase the number of cells visited.

> **See demo:** [`FlockingSpatialGridDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/FlockingSpatialGridDemo)

---

## Pathfinding

### Overview

The `Pathfinding` namespace provides five graph-search algorithms for grid-based navigation —
BFS, DFS, Dijkstra, A\*, and Flow Field — plus a `PathfindingDebugDrawer` for visualizing search
results. All algorithms use 4-direction (orthogonal) movement and return a `PathfindingResult` that
captures both the solved path and the full explored region, making it straightforward to compare
exploration patterns across algorithms.

---

### GridCell

A `readonly record struct` that identifies a single grid position.

| Member | Type | Description |
|--------|------|-------------|
| `GridCell(Row, Column)` | Constructor | Create a cell at the given zero-based row and column |
| `Row` | `int` | Zero-based row index |
| `Column` | `int` | Zero-based column index |

---

### PathfindingAlgorithm

| Value | Description |
|-------|-------------|
| `BFS` | Breadth-first search — guarantees shortest path on unweighted grids |
| `DFS` | Depth-first search — finds a valid path but does not guarantee shortest |
| `Dijkstra` | Dijkstra's algorithm — guarantees shortest path |
| `AStar` | A\* with Manhattan heuristic — guarantees shortest path, explores fewer cells |
| `FlowField` | Flow field — BFS from goal outward; path extracted by following per-cell vectors |

---

### PathfindingGrid

A rectangular grid of walkable and blocked cells. All cells are walkable by default.
Neighbor queries use 4-direction (orthogonal) movement only.

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `PathfindingGrid(rows, columns)` | `int`, `int` | — | Create a grid; throws `ArgumentOutOfRangeException` when either dimension ≤ 0 |
| `Rows` | — | `int` | Number of rows |
| `Columns` | — | `int` | Number of columns |
| `IsInBounds(cell)` | `GridCell` | `bool` | Whether the cell is within grid bounds |
| `IsWalkable(cell)` | `GridCell` | `bool` | Whether the cell is in-bounds and not blocked |
| `IsBlocked(cell)` | `GridCell` | `bool` | Whether the cell is in-bounds and blocked |
| `SetBlocked(cell, blocked)` | `GridCell`, `bool` | `void` | Mark a cell blocked or walkable; throws `ArgumentOutOfRangeException` when out of bounds |
| `Clear()` | — | `void` | Reset all cells to walkable |
| `GetWalkableNeighbors(cell, results)` | `GridCell`, `List<GridCell>` | `void` | Fill `results` with walkable orthogonal neighbors; reuse the list across calls |

---

### PathfindingResult

| Member | Type | Description |
|--------|------|-------------|
| `Start` | `GridCell` | The starting cell of the search |
| `End` | `GridCell` | The target cell of the search |
| `Path` | `IReadOnlyList<GridCell>` | Ordered cells from start to end (inclusive); empty when no path was found |
| `Visited` | `IReadOnlySet<GridCell>` | All cells explored during the search |
| `Found` | `bool` | `true` when `Path.Count > 0` |

---

### PathFinder (static)

| Method | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `FindPath(grid, start, end, algorithm)` | `PathfindingGrid`, `GridCell`, `GridCell`, `PathfindingAlgorithm` | `PathfindingResult` | Dispatch to the specified algorithm |
| `BreadthFirstSearch(grid, start, end)` | `PathfindingGrid`, `GridCell`, `GridCell` | `PathfindingResult` | BFS — guarantees shortest path |
| `DepthFirstSearch(grid, start, end)` | `PathfindingGrid`, `GridCell`, `GridCell` | `PathfindingResult` | DFS — valid path, not guaranteed shortest |
| `Dijkstra(grid, start, end)` | `PathfindingGrid`, `GridCell`, `GridCell` | `PathfindingResult` | Dijkstra — guarantees shortest path |
| `AStar(grid, start, end)` | `PathfindingGrid`, `GridCell`, `GridCell` | `PathfindingResult` | A\* — guarantees shortest path, uses Manhattan heuristic |
| `BuildFlowField(grid, goal)` | `PathfindingGrid`, `GridCell` | `FlowFieldResult` | BFS from `goal` outward; builds a per-cell direction map |

> **Edge-case behaviour:**
> - If `start` is blocked, `Found` is `false` and `Visited` is empty.
> - If `end` is blocked, the search explores normally but `Found` is `false`.
> - If `start == end`, the result contains a single-cell path `[start]`.

---

### FlowFieldResult

Returned by `PathFinder.BuildFlowField`. Stores a per-cell direction map built by BFS outward from
the goal. Each reachable cell records its next step, enabling O(1) path queries per agent per frame.

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `Goal` | — | `GridCell` | The goal cell the field was built toward |
| `GetNextCell(cell)` | `GridCell` | `GridCell?` | Next cell toward the goal; `null` when `cell` is the goal or unreachable |
| `IsReachable(cell)` | `GridCell` | `bool` | `true` if the cell can reach the goal |
| `ReachableCells` | — | `IEnumerable<GridCell>` | All cells with a recorded next step (excludes the goal) |

> **Usage pattern:** Call `BuildFlowField` once when the maze changes, then call `GetNextCell`
> every frame per agent. The flow field is static; rebuild it whenever the grid or goal changes.

---

### PathfindingDebugDrawer (static)

| Member | Parameters | Description |
|--------|-----------|-------------|
| `Enabled` | — | Toggle all debug drawing; default `true` |
| `DrawGrid(window, grid, cellSize, origin)` | `RenderWindow`, `PathfindingGrid`, `float`, `Vector2f` | Draw blocked (wall) cells and grid lines |
| `DrawResult(window, result, cellSize, origin, showVisited)` | `RenderWindow`, `PathfindingResult`, `float`, `Vector2f`, `bool` | Draw visited cells (optional), solved path, and start/end markers |
| `DrawFlowField(window, field, cellSize, origin)` | `RenderWindow`, `FlowFieldResult`, `float`, `Vector2f` | Draw per-cell direction arrows for the full flow field |

**Colour legend:**

| Element | Colour |
|---------|--------|
| Wall cells | Dark grey `(60, 65, 80)` |
| Grid lines | Darker grey `(50, 55, 70)` |
| Visited cells | Translucent blue `(40, 80, 140, 80)` |
| Path cells | Bright green `(80, 220, 120, 180)` |
| Start marker | Green `(50, 200, 50)` |
| End marker | Red `(220, 50, 50)` |

---

### PathfindingGridExtensions (static)

Bridges `PathfindingGrid` with `Wall` geometry so that steering walls can block pathfinding cells
without manual conversion.

| Method | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `grid.ApplyWalls(walls, cellSize, origin)` | `IEnumerable<Wall>`, `float`, `Vector2f` | `void` | Mark every cell intersected by a `Wall` segment as blocked. Tests all four cell edges and both wall endpoints. Does **not** clear the grid first. |

> **See demos:** [`PathfindingDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/PathfindingDemo),
> [`PathfindingAgentDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/PathfindingAgentDemo)

---

## Constants

### Overview

The `Constants` class exposes named `Vector2f` values to replace magic-number literals in game and behavior code.

### Constants.Vectors

| Constant | Value | Description |
|----------|-------|-------------|
| `Constants.Vectors.Zero` | `Vector2f(0, 0)` | Zero vector; use for resetting position, velocity, or accumulated forces |
| `Constants.Vectors.One` | `Vector2f(1, 1)` | Unit-scale vector; use as a default scale or uniform direction |

---

## Diagnostics

### Overview

`EngineLogger` is the engine's built-in lightweight logging sink. It is used internally for scene
transitions, asset loads, and shader compilation failures. Redirect output to any `Action<string>`
handler by setting `GameApplicationOptions.LogHandler` at startup or by calling
`EngineLogger.SetHandler` at any time.

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `SetHandler(handler)` | `Action<string>?` | `void` | Replace the active log handler; pass `null` to silence all output |
| `Log(message)` | `string` | `void` | Write a diagnostic message through the active handler |

> **See demo:** [`ShapeTextureDemo`](../examples/GrayHare.GameEngine.DemoHub/Scenes/ShapeTextureDemo)

---

## Design Patterns

The following design patterns shape the architecture of GrayHare.GameEngine:

| Pattern | Where Applied | Purpose |
|---------|--------------|---------|
| **Service Locator** | `GameHost` | Provides all subsystems to scenes through a single typed accessor, avoiding deep constructor-injection chains |
| **Template Method** | `GameSceneBase` | Defines the scene lifecycle skeleton (`Load → Update → RenderLayer → Unload`); subclasses override virtual methods and implement the abstract `RenderLayer` |
| **Strategy** | `MovementBehavior`, `MovementWithDriftingBehavior`, `MovementWithRotationBehavior`, `SteeringBehavior` | Composable movement algorithms selected and combined at runtime without modifying game-object classes |
| **Entity-Component-System** | `World` + `Entity` | Separates data (components) from identity (entities) and logic (systems), enabling flexible object composition without deep inheritance |
| **Flyweight / Asset Cache** | `AssetStore` | Ensures each asset is loaded once and shared; path-keyed dictionaries serve as the flyweight factory |
| **Frame-Scoped Snapshot** | `InputSnapshot`, `GameTime` | Per-frame state is captured once and treated as read-only within a frame, safe to read from anywhere during a frame without mutation risk |
| **Null Object** | `InputSnapshot.Empty`, `GameTime.Start` | Safe default instances that require no null checks, simplifying code paths that run before real state is available |
| **Observer (via SFML events)** | `InputTracker` | SFML window events are dispatched to the input tracker each frame; scenes access state through `InputSnapshot` |
| **Spatial Hashing** | `SpatialGrid<T>` | Partitions 2D space into fixed-size cells for O(1) cell lookup and radius-based neighbor queries, replacing brute-force O(n²) scans |
| **Graph Search (BFS / DFS / Dijkstra / A\* / Flow Field)** | `PathFinder` | Interchangeable search strategies encapsulated behind a unified `FindPath` dispatch, enabling runtime algorithm switching without changing call sites |
