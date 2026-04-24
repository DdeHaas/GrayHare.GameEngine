# GrayHare.GameEngine

A lightweight 2D game engine built on [SFML.Net 3.0.0](https://www.sfml-dev.org/) for .NET applications.
GrayHare.GameEngine provides an Entity-Component-System world, a service-locator host, scene management,
input polling, asset caching, audio playback, sprite animation, and a rich library of steering behaviors —
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
  - [Camera2D](#camera2d)
  - [Scene Stack (Push / Pop)](#scene-stack-push--pop)
- [Abstractions](#abstractions)
  - [`IGameObject`](#igameobject)
  - [`IMovableGameObject : IGameObject`](#imovablegameobject--igameobject)
- [Scenes](#scenes)
  - [GameSceneBase](#gamescenebase)
  - [ISceneLayer](#iscenelayer)
- [ECS](#ecs)
- [Input](#input)
  - [InputSnapshot](#inputsnapshot)
  - [InputActionMap](#inputactionmap)
- [Assets](#assets)
- [Audio](#audio)
- [Animation](#animation)
- [Behaviors](#behaviors)
- [SteeringForces](#steeringforces)
- [Extensions](#extensions)
- [Shaders](#shaders)
- [Wall](#wall)
- [Spatial](#spatial)
- [Pathfinding](#pathfinding)
- [Constants](#constants)
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
│  Window │ Input │ Assets │ Audio │ World │ Options          │
└──┬───────┬────────┬─────────┬───────┬────────┬──────────────┘
   │       │        │         │       │        │
   ▼       ▼        ▼         ▼       ▼        ▼
Render  Input    Asset    Audio    ECS     Game
Window  Tracker  Store    Player   World   Options
           │                         │
           ▼                         ▼
      InputSnapshot            Entity / Components
    (per-frame snapshot)       (sparse-set stores)

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
| **SceneManager** | Transitions between scenes at safe frame boundaries |
| **GameSceneBase** | User-defined game screens; override virtual methods and implement `RenderLayer` to draw; owns `ISceneLayer` objects |
| **ISceneLayer** | Lightweight compositing units attached to a scene; drawn in render-order before (background) or after (foreground) the scene's own content |
| **World / Entity** | Lightweight ECS; entities are integer IDs, components live in typed dictionaries |
| **AssetStore** | Loads and caches textures, fonts, sounds, and shaders |
| **InputTracker / InputSnapshot** | Builds a frame-scoped snapshot of keyboard and mouse state |
| **AudioPlayer** | Manages active `Sound` instances and removes finished ones each frame |
| **Behaviors** | Composable movement, rotation, and steering-force strategies |
| **SpatialGrid** | Grid-based spatial hash for fast radius-based neighbor queries |

---

## Getting Started

Add **SFML.Net 3.0.0** to your project, reference `GrayHare.GameEngine`, then create an entry point
and a first scene:

```csharp
using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Scenes;

using SFML.Graphics;
using SFML.System;
using SFML.Window;

// Configure and launch the engine
GameApplicationOptions options = new()
{
    Title          = "My First Game",
    WindowSize     = new Vector2u(1280, 720),
    FrameRateLimit = 60
};

new GameApplication(options).Run(new MainMenuScene());

// -------------------------------------------------------------------

internal sealed class MainMenuScene : GameSceneBase
{
    private Font? _font;

    public override void Load(GameHost host)
    {
        _font = host.Assets.LoadFont();
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        if (host.Input.WasKeyPressed(Keyboard.Key.Escape))
        {
            host.Exit();
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        window.DrawCenteredText(_font!, 48, Color.White, "Press ESC to quit", 340f);
    }
}
```

---

## Application

### Overview

The `Application` namespace is the engine's bootstrap layer. `GameApplication` creates the SFML window
and runs the main game loop. `GameHost` acts as the service locator that exposes all subsystems to
scenes. `SceneManager` handles scene transitions at safe frame boundaries.

### `GameApplicationOptions`

All properties use `init`-only setters and are safe to share across threads after construction.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Title` | `string` | `"GrayHare.GameEngine"` | Window title bar text |
| `WindowSize` | `Vector2u` | `(1280, 720)` | Initial window resolution in pixels |
| `ClearColor` | `Color` | `RGB(18, 24, 32)` | Background color applied each frame before rendering |
| `FrameRateLimit` | `uint` | `60` | Maximum frames per second (`0` = unlimited) |
| `VerticalSyncEnabled` | `bool` | `true` | Enable or disable vertical synchronization |
| `ContentRootPath` | `string` | `AppContext.BaseDirectory` | Root directory used to resolve relative asset paths |

### `GameApplication`

| Method | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `GameApplication(options)` | `GameApplicationOptions` | — | Construct the application with the provided options |
| `Run(initialScene)` | `GameSceneBase` | `void` | Create the SFML window, wire all subsystems, and start the blocking main loop |

### `GameHost`

| Member | Type | Description |
|--------|------|-------------|
| `Window` | `RenderWindow` | The active SFML render window |
| `Input` | `InputSnapshot` | Immutable snapshot of this frame's keyboard and mouse state |
| `Assets` | `AssetStore` | Asset loader and cache |
| `Audio` | `AudioPlayer` | Sound playback manager |
| `World` | `World` | The ECS world for this session |
| `Camera` | `Camera2D` | The 2D camera for the current application |
| `InputActions` | `InputActionMap?` | Optional action map; assign from a scene or `Program.cs` |
| `Options` | `GameApplicationOptions` | Read-only engine configuration |
| `ExitRequested` | `bool` | `true` after `Exit()` has been called |
| `TimeScale` | `float` | Current time-scale multiplier (`0` = paused, `1` = normal, `<1` = slow-motion). Defaults to `1`. |
| `IsPaused` | `bool` | `true` when `TimeScale` is `0` |
| `ChangeScene(scene)` | `GameSceneBase` → `void` | Queue a scene transition; clears the ECS world at the next frame boundary |
| `Pause()` | — → `void` | Set `TimeScale` to `0`, freezing `GameTime.Delta` and `GameTime.Total` |
| `Resume()` | — → `void` | Restore `TimeScale` to `1` |
| `SetTimeScale(value)` | `float` → `void` | Set `TimeScale` to `value` (clamped to ≥ 0) |
| `Exit()` | — → `void` | Signal the main loop to terminate after the current frame |

### Usage Example

```csharp
GameApplicationOptions options = new()
{
    Title               = "Space Shooter",
    WindowSize          = new Vector2u(1920, 1080),
    FrameRateLimit      = 120,
    VerticalSyncEnabled = false,
    ClearColor          = new Color(5, 5, 20)
};

new GameApplication(options).Run(new TitleScene());
```

### `GameTime`

`GameTime` is an immutable `record struct` that carries all timing information for the current frame.
It is constructed and advanced by `GameApplication` and passed to `GameSceneBase.Update()` on every frame.

| Member | Type | Description |
|--------|------|-------------|
| `Total` | `TimeSpan` | Total scaled elapsed time since the application started (frozen when paused) |
| `Delta` | `TimeSpan` | Scaled elapsed time for the current frame (zero when paused) |
| `RawTotal` | `TimeSpan` | Unscaled total elapsed time; always advances regardless of `TimeScale` |
| `RawDelta` | `TimeSpan` | Unscaled elapsed time for the current frame; always advances regardless of `TimeScale` |
| `DeltaTotalSeconds` | `float` | Scaled elapsed time for the current frame as seconds; convenient for physics calculations (equivalent to `(float)Delta.TotalSeconds`) |
| `RawDeltaTotalSeconds` | `float` | Unscaled elapsed time for the current frame as seconds (equivalent to `(float)RawDelta.TotalSeconds`) |
| `TimeScale` | `float` | The time-scale multiplier that was applied this frame |
| `IsPaused` | `bool` | `true` when `TimeScale` is `0` |
| `FrameNumber` | `ulong` | Zero-based frame counter |
| `Start` | `static GameTime` | Zero-initialized sentinel value representing the state before the first frame |
| `Advance(rawDelta, timeScale)` | `TimeSpan`, `float = 1f` → `GameTime` | Return a new `GameTime` advanced by `rawDelta` scaled by `timeScale` |

### `GameTime` Usage Example

```csharp
public override void Update(GameHost host, in GameTime gameTime)
{
    // DeltaTotalSeconds is equivalent to (float)gameTime.Delta.TotalSeconds.
    float dt = gameTime.DeltaTotalSeconds;

    // Animate position using total elapsed time as a sine-wave input
    float wave = MathF.Sin((float)gameTime.Total.TotalSeconds * 2f);
    _sprite.Position = new Vector2f(400f + wave * 100f, 300f);

    // One-time initialization logic on the very first frame
    if (gameTime.FrameNumber == 0)
    {
        Console.WriteLine("First frame rendered.");
    }
}
```
### `Camera2D`

`Camera2D` wraps an SFML `View` and provides smooth following, zoom, rotation, and screen-shake effects.
It is created and owned by `GameHost` and is available via `host.Camera`. In a standard
`GameApplication` loop, the engine already advances `UpdateShake(gameTime.RawDeltaTotalSeconds)`
and applies `GetView()` before the current scene renders. Call those methods manually only when
you manage a standalone `Camera2D` instance or temporarily override the active view yourself.

| Member | Type | Description |
|--------|------|-------------|
| `Position` | `Vector2f` | World-space center position of the camera |
| `Zoom` | `float` | Zoom level where `1` = default, `>1` = zoom in, `<1` = zoom out; clamped to minimum `0.01` |
| `Rotation` | `float` | Camera rotation in degrees (clockwise) |
| `ViewportSize` | `Vector2f` | Viewport dimensions (set at construction from window size) |
| `Follow(target, lerpSpeed, deltaTime)` | `Vector2f`, `float`, `float` → `void` | Smoothly move the camera toward `target` using linear interpolation scaled by `deltaTime` |
| `Shake(intensity, duration)` | `float`, `float` → `void` | Initiate a screen-shake effect that decays linearly over `duration` seconds |
| `UpdateShake(deltaTime)` | `float` → `void` | Advance the shake timer and recalculate shake offset; `GameApplication` already does this for `host.Camera` using raw/unscaled time |
| `GetView()` | — → `View` | Return an SFML `View` reflecting current camera state; use when you need to apply or override a view manually |
| `Reset()` | — → `void` | Reset camera to default state: centered, no zoom, no rotation, no shake |

### Usage Example

```csharp
public class GameplayScene : GameSceneBase
{
    private Vector2f _playerPos = new(400, 300);

    public override void Update(GameHost host, in GameTime gameTime)
    {
        // Follow the player with smooth interpolation
        host.Camera.Follow(_playerPos, lerpSpeed: 5f, deltaTime: gameTime.DeltaTotalSeconds);

        // Trigger a shake on collision
        if (DetectedCollision())
        {
            host.Camera.Shake(intensity: 8f, duration: 0.2f);
        }

        // Zoom control with mouse wheel
        host.Camera.Zoom += host.Input.MouseWheelDelta * 0.05f;
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // Draw scene content here.
        // GameApplication already updated shake and applied host.Camera for this frame.
    }
}
```

### Scene Stack (Push / Pop)

`GameHost` provides `ChangeScene`, `PushScene`, and `PopScene` for flexible scene management.

**ChangeScene:** Replaces the entire scene stack with a new scene. The ECS world is cleared. Use for transitions between distinct game states (title → gameplay → pause).

**PushScene / PopScene:** Manages a stack of scenes. Useful for overlay UI like pause menus, dialogs, and HUDs. The underlying scene persists and is unloaded only when popped or changed. When pushing an overlay:
- The current top scene receives `OnDeactivated()`
- The overlay receives `Load()` and `OnActivated()`
- When popping:
- The overlay receives `Unload()` and is disposed
- The scene beneath it receives `OnActivated()`

| Method | Description |
|--------|-------------|
| `ChangeScene(scene)` | Queue a complete scene transition; clears the entire stack and ECS world |
| `PushScene(overlay)` | Queue a scene to be pushed on top of the current stack (e.g. pause menu, dialog) |
| `PopScene()` | Queue removal of the top scene; throws `InvalidOperationException` if only one scene exists |
| `SceneStackDepth` | Number of scenes currently on the stack |

### Usage Example

```csharp
// Pause menu overlay
internal sealed class PauseMenuScene : GameSceneBase
{
    public override void Load(GameHost host)
    {
        // Pause the game when the menu opens
        host.Pause();
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        if (host.Input.WasKeyPressed(Keyboard.Key.Escape))
        {
            // Resume and close the pause menu
            host.Resume();
            host.PopScene();
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Q))
        {
            // Close pause menu and go back to title
            host.Resume();
            host.PopScene();
            host.ChangeScene(new TitleScene());
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // Draw semi-transparent overlay
        RectangleShape overlay = new(window.Size.ToSF())
        {
            FillColor = new Color(0, 0, 0, 128)
        };
        window.Draw(overlay);

        // Draw pause menu UI
        window.DrawCenteredText(_font, 48, Color.White, "PAUSED", 300f);
        window.DrawCenteredText(_font, 20, Color.White, "Press ESC to resume", 360f);
    }

    public override void Unload(GameHost host)
    {
        // Resume automatically when the overlay closes
        host.Resume();
    }
}

// In gameplay scene
if (host.Input.WasKeyPressed(Keyboard.Key.Escape))
{
    host.PushScene(new PauseMenuScene());
}
```

---

## Abstractions

### Overview

`IGameObject` and `IMovableGameObject` define the minimal contracts that all engine-managed objects
implement. These interfaces decouple the rendering and update pipeline from concrete game-object types.

### `IGameObject`

| Member | Type | Description |
|--------|------|-------------|
| `Rotation` | `float` | Current rotation in degrees |
| `Origin` | `Vector2f` | Local origin point (pivot for rotation and scale) |
| `Position` | `Vector2f` | World-space position |
| `Scale` | `Vector2f` | Scale factor applied to the object |
| `ZOrder` | `int` | Draw-order priority; lower values are drawn first |
| `GlobalBounds` | `FloatRect` | World-space axis-aligned bounding box |
| `Draw(window)` | `RenderWindow` → `void` | Render the object to the provided window |
| `Update(deltaTime)` | `float` → `void` | Advance per-frame logic by `deltaTime` seconds |

### `IMovableGameObject : IGameObject`

Extends `IGameObject` with the physics properties required by the `Behaviors` layer.

| Member | Type | Description |
|--------|------|-------------|
| `Mass` | `float` | Object mass used to scale applied forces|
| `Heading` | `Vector2f` | Unit vector representing the current facing direction |
| `Side` | `Vector2f` | Left perpendicular of `Heading` |
| `Velocity` | `Vector2f` | Current velocity vector |
| `Speed` | `float` | Scalar speed (magnitude of `Velocity`) |
| `Acceleration` | `float` | Acceleration magnitude |
| `Deceleration` | `float` | Passive deceleration rate (e.g. friction) |
| `BrakingDeceleration` | `float` | Active braking deceleration rate |
| `MaxSpeed` | `float` | Speed cap |
| `TurnRate` | `float` | Current turn rate |
| `MaxTurnRate` | `float` | Maximum turn rate per frame |

### Usage Example

```csharp
public sealed class Ship : IMovableGameObject
{
    // IGameObject
    public float    Rotation  { get; private set; }
    public Vector2f Origin    { get; } = new(16, 16);
    public Vector2f Position  { get; set; }
    public Vector2f Scale     { get; } = new(1, 1);
    public int      ZOrder    => 0;
    public FloatRect GlobalBounds => _sprite.GetGlobalBounds();

    // IMovableGameObject
    public float    Mass                { get; } = 1.0f;
    public Vector2f Heading             { get; private set; } = new(0, -1);
    public Vector2f Side                => new(-Heading.Y, Heading.X);
    public Vector2f Velocity            { get; set; }
    public float    Speed               => MathF.Sqrt(Velocity.X * Velocity.X + Velocity.Y * Velocity.Y);
    public float    Acceleration        { get; } = 200f;
    public float    Deceleration        { get; } = 60f;
    public float    BrakingDeceleration { get; } = 400f;
    public float    MaxSpeed            { get; } = 300f;
    public float    TurnRate            { get; set; }
    public float    MaxTurnRate         { get; } = 3.5f;

    public void Draw(RenderWindow window)
    {
        window.Draw(_sprite);
    }

    public void Update(float deltaTime)
    {
        /* apply velocity to position */
    }

    private readonly Sprite _sprite;

    public Ship(Texture texture)
    {
        _sprite = new Sprite(texture);
    }
}
```

---

## Scenes

### Overview

`GameSceneBase` is the abstract base class for all game screens. Override its virtual lifecycle
methods and the abstract `RenderLayer` method to load assets, update logic, and draw. Scene
transitions are requested through `GameHost.ChangeScene()` and take effect safely at the next
frame boundary.

Each scene can own one or more `ISceneLayer` objects that are composited around the scene's own
content in a defined render order. Use layers to attach background visuals (scrolling skies,
parallax tiles) or foreground visuals (HUD, vignette effects) without coupling that logic to the
scene itself.

### `GameSceneBase`

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `Name` | — | `string` | Scene identifier; defaults to the runtime class name |
| `Load(host)` | `GameHost` | `void` | Called once when the scene becomes active; load assets here |
| `Unload(host)` | `GameHost` | `void` | Called once when the scene is deactivated; dispose per-scene resources here |
| `Update(host, gameTime)` | `GameHost`, `in GameTime` | `void` | Called every frame before `RenderLayer`; advance game logic here |
| `RenderLayer(host, window)` *(abstract)* | `GameHost`, `RenderWindow` | `void` | Implement this method to issue the scene's own draw calls |
| `Render(host, window)` | `GameHost`, `RenderWindow` | `void` | Renders all layers and the scene's own content. Calls negative-RenderOrder layers, then RenderLayer, then non-negative layers |
| `AddLayer(layer)` *(protected)* | `ISceneLayer` | `void` | Register a layer with this scene; call from the constructor or `Load` |

> **Transition safety:** When `host.ChangeScene(new NextScene())` is called, the current scene's
> `Unload` runs (layers are unloaded first), the ECS `World` is cleared, and the new scene's
> `Load` runs — all at the start of the following frame so the current frame always completes cleanly.

### `ISceneLayer`

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `RenderOrder` | — | `int` | Render position relative to the scene; negative = background, zero/positive = foreground |
| `Load(host)` | `GameHost` | `void` | Called once when the owning scene loads |
| `Unload(host)` | `GameHost` | `void` | Called once when the owning scene unloads |
| `Update(host, gameTime)` | `GameHost`, `in GameTime` | `void` | Called every frame (same frame as the owning scene) |
| `RenderLayer(host, window)` | `GameHost`, `RenderWindow` | `void` | Called every frame at the position determined by `RenderOrder` |
| `OnActivated(host)` | `GameHost` | `void` | Called when the owning scene becomes the active top of the stack (default: no-op) |
| `OnDeactivated(host)` | `GameHost` | `void` | Called when another scene is pushed on top of the owning scene (default: no-op) |

> **Lifecycle forwarding:** `OnActivated` and `OnDeactivated` have default empty implementations.
> The engine forwards both events automatically to every registered layer via the scene's internal
> `ActivateInternal` / `DeactivateInternal` methods — even if the owning scene overrides the
> virtual `GameSceneBase.OnActivated` / `OnDeactivated` without calling `base`.
> Override these in a layer when it needs to react to the scene being pushed or popped
> (for example, stopping a timer in a pause overlay).

> **Render order:** Layers with `RenderOrder < 0` are *background* layers and are drawn before the
> scene's own `Render` call.  Layers with `RenderOrder >= 0` are *foreground* layers and are drawn
> after it.  Multiple layers are sorted ascending by `RenderOrder`; layers with the same value
> preserve the order in which they were registered.
>
> **Parameters:** Pass data to layers through their constructors.  The owning scene creates the
> layer objects and injects dependencies at construction time.

### Usage Example

```csharp
// Background layer — scrolling starfield, drawn behind the scene
internal sealed class StarfieldLayer : ISceneLayer
{
    public int RenderOrder => -10; // background

    public void Load(GameHost host) { }
    public void Unload(GameHost host) { }
    public void Update(GameHost host, in GameTime gameTime) { }

    public void RenderLayer(GameHost host, RenderWindow window)
    {
        // draw scrolling stars ...
    }
}

// Foreground layer -- HUD, drawn on top of the scene
internal sealed class HudLayer : ISceneLayer
{
    private readonly Font _font;

    // Parameters are passed by the owning scene via the constructor
    public HudLayer(Font font) => _font = font;

    public int RenderOrder => 10; // foreground

    public void Load(GameHost host) { }
    public void Unload(GameHost host) { }

    public void Update(GameHost host, in GameTime gameTime)
    {
        // ... HUD per-frame logic ...
    }

    public void RenderLayer(GameHost host, RenderWindow window)
    {
        // draw HUD ...
    }
}

// Scene that owns both layers
internal sealed class GameplayScene : GameSceneBase
{
    private Font? _font;

    public GameplayScene()
    {
        AddLayer(new StarfieldLayer()); // register background before Load
    }

    public override void Load(GameHost host)
    {
        _font = host.Assets.LoadFont();
        AddLayer(new HudLayer(_font)); // register foreground after font is loaded
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        // ... game logic ...
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // scene's own content -- drawn between StarfieldLayer and HudLayer
    }

    public override void Unload(GameHost host)
    {
        _font?.Dispose();
    }
}
```

---

## ECS

### Overview

The ECS (Entity-Component-System) module provides a lightweight, dictionary-backed world where
entities are plain integer IDs and components are stored in per-type sparse sets. It supports
single- and dual-component queries and is reset automatically on each scene transition.

### `Entity`

`Entity` is a `record struct` wrapping a single `int Id`.

| Member | Description |
|--------|-------------|
| `Id` | Unique integer identifier for this entity |
| `ToString()` | Returns `"Entity(n)"` |

### `World`

| Method | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `CreateEntity()` | — | `Entity` | Allocate a new entity with a unique ID |
| `Exists(entity)` | `Entity` | `bool` | Return `true` if the entity has not been destroyed |
| `DestroyEntity(entity)` | `Entity` | `void` | Remove the entity and all of its attached components |
| `Clear()` | — | `void` | Destroy all entities and components in the world |
| `AddComponent<T>(entity, component)` | `Entity`, `T` | `void` | Attach a component of type `T` to the entity |
| `RemoveComponent<T>(entity)` | `Entity` | `bool` | Detach the component; returns `false` if the entity has none |
| `TryGetComponent<T>(entity, out component)` | `Entity`, `out T` | `bool` | Safely retrieve a component; returns `false` if absent |
| `GetComponent<T>(entity)` | `Entity` | `T` | Retrieve a component; throws if the entity has none |
| `Query<T>()` | — | `IEnumerable<Entity>` | Enumerate all entities that have component type `T` |
| `Query<TA, TB>()` | — | `IEnumerable<Entity>` | Enumerate all entities that have both `TA` and `TB` |
| `Query<TA, TB, TC>()` | — | `IEnumerable<Entity>` | Enumerate all entities that have `TA`, `TB`, and `TC` |

> **Implementation note:** Component stores are created lazily on the first `AddComponent<T>` call
> and are backed by `Dictionary<int, TComponent>` sparse sets.

### Usage Example

```csharp
// Define components as plain record structs
record struct Position(float X, float Y);
record struct Velocity(float X, float Y);
record struct Health(int Current, int Max);

World world = host.World;

// Create entities and attach components
Entity player = world.CreateEntity();
world.AddComponent(player, new Position(100, 200));
world.AddComponent(player, new Velocity(0, 0));
world.AddComponent(player, new Health(100, 100));

Entity enemy = world.CreateEntity();
world.AddComponent(enemy, new Position(400, 300));
world.AddComponent(enemy, new Health(50, 50));

// Per-frame movement: update all entities that have both Position and Velocity
float dt = (float)gameTime.Delta.TotalSeconds;

foreach (Entity e in world.Query<Position, Velocity>())
{
    Position pos = world.GetComponent<Position>(e);
    Velocity vel = world.GetComponent<Velocity>(e);
    world.AddComponent(e, pos with { X = pos.X + vel.X * dt, Y = pos.Y + vel.Y * dt });
}

// Safe retrieval with null-check pattern
if (world.TryGetComponent<Health>(player, out Health hp) && hp.Current <= 0)
{
    world.DestroyEntity(player);
}
```

---

## Input

### Overview

`InputSnapshot` is a frame-scoped snapshot of all keyboard and mouse input captured during a single frame.
It is updated in-place by the engine each frame to avoid allocations, but scenes should treat it as
read-only within a single frame. The internal `InputTracker` is wired automatically by `GameApplication`.
Scenes always access the current snapshot through `host.Input`.

### `InputSnapshot`

| Member | Type | Description |
|--------|------|-------------|
| `Empty` | `static InputSnapshot` | A snapshot with no keys or buttons pressed; useful for testing and default state |
| `CurrentKeys` | `IReadOnlySet<Keyboard.Key>` | Keys held down this frame |
| `PreviousKeys` | `IReadOnlySet<Keyboard.Key>` | Keys held down last frame |
| `CurrentButtons` | `IReadOnlySet<Mouse.Button>` | Mouse buttons held down this frame |
| `PreviousButtons` | `IReadOnlySet<Mouse.Button>` | Mouse buttons held down last frame |
| `MousePosition` | `Vector2i` | Cursor position in window coordinates |
| `MouseWheelDelta` | `float` | Scroll wheel delta accumulated this frame |
| `IsKeyDown(key)` | `Keyboard.Key` → `bool` | `true` while the key is held |
| `WasKeyPressed(key)` | `Keyboard.Key` → `bool` | `true` on the exact frame the key was first pressed |
| `WasAnyKeyPressed()` | — → `bool` | `true` if any key transitioned to pressed this frame |
| `WasKeyReleased(key)` | `Keyboard.Key` → `bool` | `true` on the exact frame the key was released |
| `IsMouseButtonDown(button)` | `Mouse.Button` → `bool` | `true` while the button is held |
| `WasMouseButtonPressed(button)` | `Mouse.Button` → `bool` | `true` on the exact frame the button was first pressed |
| `WasMouseButtonReleased(button)` | `Mouse.Button` → `bool` | `true` on the exact frame the button was released |

### Usage Example

```csharp
public override void Update(GameHost host, in GameTime gameTime)
{
    InputSnapshot input = host.Input;

    // Use the DeltaTotalSeconds property as short for: (float)gameTime.Delta.TotalSeconds
    float dt = gameTime.DeltaTotalSeconds;

    // Continuous movement while keys are held
    if (input.IsKeyDown(Keyboard.Key.W))
    {
        _position.Y -= _speed * dt;
    }

    if (input.IsKeyDown(Keyboard.Key.S))
    {
        _position.Y += _speed * dt;
    }

    if (input.IsKeyDown(Keyboard.Key.A))
    {
        _position.X -= _speed * dt;
    }

    if (input.IsKeyDown(Keyboard.Key.D))
    {
        _position.X += _speed * dt;
    }

    // Single-frame actions
    if (input.WasKeyPressed(Keyboard.Key.Space))
    {
        FireBullet();
    }

    if (input.WasMouseButtonPressed(Mouse.Button.Left))
    {
        SpawnParticle(input.MousePosition);
    }

    // Scroll wheel zoom
    _zoom += input.MouseWheelDelta * 0.1f;
}
```

### `InputActionMap`

`InputActionMap` is the recommended abstraction for managing game controls. It lets you define logical actions
(move, jump, fire) and bind them to multiple physical inputs (keys, buttons, axes) without game logic depending
on specific devices. This pattern makes it easy to support keyboard, gamepad, and custom controls simultaneously.

| Method | Description |
|--------|-------------|
| `MapKey(actionName, key)` | Bind a keyboard key to an action |
| `MapButton(actionName, joystickId, button)` | Bind a joystick button to an action |
| `MapAxis(actionName, joystickId, axis, deadZone = 10f)` | Bind a joystick axis to an action; values whose absolute magnitude stays below `deadZone` are treated as zero |
| `WasActionPressed(actionName, input)` | Returns `true` if the action was triggered this frame (one-time event) |
| `IsActionDown(actionName, input)` | Returns `true` if the action is currently held down (continuous) |
| `GetAxisValue(actionName, input)` | Returns the current axis value `[-100, 100]` for mapped axes outside the dead zone, or `0` otherwise |

### Usage Example

```csharp
public class GameplayScene : GameSceneBase
{
    private const uint PrimaryJoystickId = 0;
    private InputActionMap _actions = new();

    public override void Load(GameHost host)
    {
        // Bind movement actions
        _actions.MapKey("moveLeft", Keyboard.Key.A);
        _actions.MapKey("moveLeft", Keyboard.Key.Left);
        _actions.MapButton("moveLeft", PrimaryJoystickId, (uint)Joystick.Button.DPadLeft);

        _actions.MapKey("moveRight", Keyboard.Key.D);
        _actions.MapKey("moveRight", Keyboard.Key.Right);
        _actions.MapButton("moveRight", PrimaryJoystickId, (uint)Joystick.Button.DPadRight);

        _actions.MapKey("moveUp", Keyboard.Key.W);
        _actions.MapKey("moveUp", Keyboard.Key.Up);
        _actions.MapButton("moveUp", PrimaryJoystickId, (uint)Joystick.Button.DPadUp);

        _actions.MapKey("moveDown", Keyboard.Key.S);
        _actions.MapKey("moveDown", Keyboard.Key.Down);
        _actions.MapButton("moveDown", PrimaryJoystickId, (uint)Joystick.Button.DPadDown);

        // Bind action buttons
        _actions.MapKey("fire", Keyboard.Key.Space);
        _actions.MapButton("fire", PrimaryJoystickId, (uint)Joystick.Button.A);

        _actions.MapKey("jump", Keyboard.Key.W);
        _actions.MapButton("jump", PrimaryJoystickId, (uint)Joystick.Button.X);
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        float dt = gameTime.DeltaTotalSeconds;

        // Check for one-time actions
        if (_actions.WasActionPressed("fire", host.Input))
        {
            FireBullet();
        }

        if (_actions.WasActionPressed("jump", host.Input))
        {
            Jump();
        }

        // Check for continuous movement
        Vector2f movement = Vector2f.Zero;

        if (_actions.IsActionDown("moveLeft", host.Input))
            movement.X -= 1f;
        if (_actions.IsActionDown("moveRight", host.Input))
            movement.X += 1f;
        if (_actions.IsActionDown("moveUp", host.Input))
            movement.Y -= 1f;
        if (_actions.IsActionDown("moveDown", host.Input))
            movement.Y += 1f;

        // Apply normalized movement
        if (movement.LengthSquared > 0)
        {
            movement = movement.Normalized();
            _position += movement * _speed * dt;
        }
    }
}
```

---

## Assets

### Overview

`AssetStore` is the central cache for all engine resources: images, textures, fonts, sound buffers,
and GLSL shaders. All assets are keyed by their relative path and loaded lazily on first access.
Shader-load failures are also cached so that broken shaders do not trigger repeated compilation
attempts on every frame.

### `AssetStore`

| Method / Property | Parameters | Return Type | Description |
|-------------------|-----------|-------------|-------------|
| `ContentRootPath` | — | `string` | Absolute root directory used to resolve relative asset paths |
| `ResolvePath(assetPath)` | `string` | `string` | Convert a relative asset path to an absolute file path |
| `LoadImage(assetPath)` | `string` | `Image` | Load and cache an SFML `Image`; supports PPM P3/P6 and all SFML-native formats |
| `LoadTexture(assetPath, smooth)` | `string`, `bool = false` | `Texture` | Load and cache an SFML `Texture`; `smooth: true` enables bilinear filtering |
| `LoadFont(assetPath)` | `string? = null` | `Font` | Load and cache a font; falls back to a system font when `null` |
| `LoadSoundBuffer(assetPath)` | `string` | `SoundBuffer` | Load and cache an SFML `SoundBuffer` |
| `LoadShader(fragAssetPath)` | `string` | `Shader` | Load a fragment-only GLSL shader; throws on failure |
| `LoadShader(vertAssetPath, fragAssetPath)` | `string`, `string` | `Shader` | Load a vertex + fragment shader pair; throws on failure |
| `TryLoadShader(fragAssetPath, out failureReason)` | `string`, `out string?` | `Shader?` | Load a fragment shader; returns `null` with a descriptive message on failure |
| `TryLoadShader(vertAssetPath, fragAssetPath, out failureReason)` | `string`, `string`, `out string?` | `Shader?` | Load a shader pair; returns `null` with a descriptive message on failure |
| `Unload(assetPath)` | `string` | `void` | Dispose and remove a single cached asset; no-op if the path was never loaded |
| `UnloadAll()` | — | `void` | Dispose and clear all cached assets (textures, fonts, sounds, shaders) |
| `Dispose()` | — | `void` | Dispose all cached SFML resources |

> **PPM support:** ASCII (P3) and binary (P6) PPM images are natively decoded, including 16-bit
> channel depth. This allows custom procedural textures to be loaded without a third-party image
> library.

### `AssetPathResolver` (static)

| Method | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `NormalizeContentRoot(contentRoot)` | `string` | `string` | Normalize the content root to an absolute path |
| `ResolvePath(contentRoot, assetPath)` | `string`, `string` | `string` | Combine a root directory and a relative path into an absolute path |

### `SystemFont` (static)

| Method | Return Type | Description |
|--------|-------------|-------------|
| `FindSystemFont()` | `string` | Locate a suitable system font file. Searches for Segoe UI / Arial on Windows, Arial / Helvetica on macOS, and DejaVu Sans / Liberation Sans on Linux. Throws `FileNotFoundException` if no font is found. |

### Usage Example

```csharp
public override void Load(GameHost host)
{
    // Load a texture (subsequent calls with the same path return the cached instance)
    _texture = host.Assets.LoadTexture("sprites/hero.png", smooth: true);

    // Load the default system font
    _font = host.Assets.LoadFont();

    // Load a custom font from the content root
    _titleFont = host.Assets.LoadFont("fonts/PressStart2P.ttf");

    // Load a sound effect
    _jumpBuffer = host.Assets.LoadSoundBuffer("sounds/jump.wav");

    // Attempt to load a fragment shader with graceful fallback
    if (host.Assets.TryLoadShader("shaders/bloom.frag", out string? reason) is { } shader)
    {
        _bloomShader = shader;
    }
    else
    {
        Console.Error.WriteLine($"Shader unavailable: {reason}");
    }
}
```

---

## Audio

### Overview

`AudioPlayer` manages a collection of active `Sound` instances. It plays sounds on demand, tracks
them internally, and removes finished sounds each frame to prevent resource leaks.

### `AudioPlayer`

| Method | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `PlaySound(assetPath, volume, loop)` | `string`, `float = 100f`, `bool = false` | `Sound` | Load the sound via `AssetStore` and immediately play it; returns the live `Sound` instance |
| `StopAll()` | — | `void` | Immediately stop and dispose all active sounds |
| `Dispose()` | — | `void` | Release all managed sound resources |

### Usage Example

```csharp
public override void Update(GameHost host, in GameTime gameTime)
{
    if (host.Input.WasKeyPressed(Keyboard.Key.Space))
    {
        // Play a one-shot sound effect at 80 % volume
        host.Audio.PlaySound("sounds/laser.wav", volume: 80f);
    }

    if (host.Input.WasKeyPressed(Keyboard.Key.M))
    {
        // Start looping background music and keep a reference to stop it later
        _music = host.Audio.PlaySound("music/theme.ogg", volume: 50f, loop: true);
    }
}
```

---

## Animation

### Overview

The animation system supports frame-by-frame sprite animation driven by elapsed time. An
`AnimationClip` holds a sequence of `AnimationFrame` records, built either from a horizontal
sprite sheet or from a list of individual textures. `AnimationPlayer` drives playback and renders
the current frame to the window each frame.

### `AnimationFrame`

`AnimationFrame` is a `record struct` with two fields:

| Field | Type | Description |
|-------|------|-------------|
| `Duration` | `TimeSpan` | How long this frame is displayed before advancing |
| `Texture` | `Texture` | The SFML texture rendered for this frame |

### `AnimationClip`

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `Name` | — | `string` | Identifier for the clip |
| `Frames` | — | `IReadOnlyList<AnimationFrame>` | Ordered list of animation frames |
| `CreateFromImage(name, image, frameWidth, frameHeight, frameCount, frameDuration)` | `string`, `Image`, `uint`, `uint`, `uint`, `TimeSpan` | `AnimationClip` | Slice a horizontal sprite sheet into evenly spaced frames starting at (0, 0) |
| `CreateFromImage(name, image, frameWidth, frameHeight, frameCount, frameDuration, startX, startY)` | …, `uint`, `uint` | `AnimationClip` | Same as above but reads frames from a pixel offset — use `startY = rowIndex * frameHeight` to target any row in a multi-row sprite sheet |
| `CreateFromTextures(name, textures, frameDuration)` | `string`, `IReadOnlyList<Texture>`, `TimeSpan` | `AnimationClip` | Build a clip from an explicit list of pre-loaded textures |
| `Dispose()` | — | `void` | Release all frame textures |

### `AnimationPlayer`

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `IsFinished` | — | `bool` | `true` when a non-looping clip has displayed its last frame |
| `IsLooping` | — | `bool` | `true` when the player is set to loop the clip |
| `IsPaused` | — | `bool` | `true` when playback is frozen mid-sequence |
| `Position` | — | `Vector2f` | World position where the animation is rendered |
| `Scale` | — | `Vector2f` | Scale factor applied to each frame |
| `Rotation` | — | `float` | Rotation in degrees applied to each frame |
| `FrameIndex` | — | `int` | Zero-based index of the currently displayed frame |
| `Play()` | — | `void` | Start or resume playback; also clears `IsPaused` |
| `Pause()` | — | `void` | Freeze at the current frame without resetting it |
| `Resume()` | — | `void` | Continue playback after a `Pause()` call |
| `Reset()` | — | `void` | Rewind playback to frame 0 |
| `Update(delta)` | `TimeSpan` | `void` | Advance playback by `delta`; moves to the next frame when the current frame's duration elapses |
| `Render(window)` | `RenderWindow` | `void` | Draw the current frame to the window |
| `Dispose()` | — | `void` | Release player resources |

### Usage Example

```csharp
private AnimationClip?   _runClip;
private AnimationPlayer? _player;

public override void Load(GameHost host)
{
    // Build a clip from a 4-frame horizontal sprite sheet (64 × 64 px per frame)
    Image sheet = host.Assets.LoadImage("sprites/run_sheet.png");
    _runClip = AnimationClip.CreateFromImage(
        name:          "run",
        image:         sheet,
        frameWidth:    64,
        frameHeight:   64,
        frameCount:    4,
        frameDuration: TimeSpan.FromMilliseconds(100)
    );

    _player = new AnimationPlayer(_runClip, isLooping: true, autoPlay: true)
    {
        Position  = new Vector2f(200, 300)
    };
}

public override void Update(GameHost host, in GameTime gameTime)
{
    _player!.Update(gameTime.Delta);
}

public override void Render(GameHost host, RenderWindow window)
{
    _player!.Render(window);
}
```

---

## Behaviors

### Overview

The `Behaviors` namespace provides composable, strategy-based movement components for game agents.
`MovementBehavior` and `RotationBehavior` handle player-driven locomotion. `SteeringBehavior`
implements a full suite of autonomous steering forces for AI agents. `SteeringForces` provides static
helpers for combining multiple steering forces safely. All behaviors are constructed with an
`IMovableGameObject` and operate on its properties each frame.

### `RotationDirection` (enum)

| Value | Integer | Description |
|-------|---------|-------------|
| `Default` | `0` | Take the shortest angular path to the target |
| `Clockwise` | `1` | Clockwise rotation |
| `Counterclockwise` | `-1` | Counterclockwise rotation |

### `RotationBehavior`

Construct with an `IMovableGameObject`. Set the turning flags before calling `UpdateRotation` each frame.

| Member | Description |
|--------|-------------|
| `IsTurningLeft` | Set to `true` to apply a counterclockwise turn this frame |
| `IsTurningRight` | Set to `true` to apply a clockwise turn this frame |
| `UpdateRotation(deltaTime, ref heading)` | Apply mass-scaled rotation clamped to `MaxTurnRate`; updates `heading` in place; returns the new rotation angle in degrees |

### `MovementBehavior`

Construct with an `IMovableGameObject`. Set the movement flags before calling `UpdateMovement` each frame.

| Member | Description |
|--------|-------------|
| `IsMovingForwards` | Accelerate in the current heading direction |
| `IsMovingBackwards` | Accelerate opposite to the current heading |
| `IsBraking` | Apply braking deceleration along the current velocity without reversing |
| `IsStrafingLeft` | Accelerate along the left side vector, independent of heading |
| `IsStrafingRight` | Accelerate along the right side vector, independent of heading |
| `UpdateMovement(deltaTime, currentVelocity, currentPosition)` | Apply acceleration, passive deceleration, and braking; returns the new velocity `Vector2f` |

### `MovementWithDriftingBehavior`

Composes `RotationBehavior` and `MovementBehavior`. Turning does not instantly redirect momentum —
the object drifts like a spacecraft.

| Member | Description |
|--------|-------------|
| `IsMovingForwards` | Apply forward thrust |
| `IsBraking` | Apply braking deceleration without redirecting current momentum |
| `IsTurningLeft` | Turn left without redirecting velocity |
| `IsTurningRight` | Turn right without redirecting velocity |
| `Update(deltaTime, ref rotation, ref heading)` | Advance movement and rotation; returns new velocity `Vector2f` |

### `MovementWithRotationBehavior`

Velocity always aligns with the heading; no drift. The agent cannot reverse — braking only
decelerates to a stop.

| Member | Description |
|--------|-------------|
| `IsMovingForwards` | Apply forward acceleration |
| `IsBraking` | Apply braking deceleration |
| `IsTurningLeft` | Turn left; velocity direction updates immediately |
| `IsTurningRight` | Turn right; velocity direction updates immediately |
| `Update(deltaTime, ref rotation, ref heading)` | Advance movement and rotation; returns new velocity `Vector2f` |

### `SteeringBehavior`

Construct with an `IMovableGameObject`. Every method returns a `Vector2f` steering force.

#### Integration Pattern

Steering forces are applied using semi-implicit Euler integration. Apply this pattern every frame:

```csharp
// 1. Compute individual forces
Vector2f wander    = _steering.Wander(ref _wanderAngle, 80f, 200f);
Vector2f wallForce = _steering.WallAvoidance(_walls, 120f, 45f);
Vector2f bounds    = _steering.StayWithinBounds(_boundary, 50f);

// 2. Combine into one force (see SteeringForces below)
Vector2f totalForce = SteeringForces.WeightedSum(
    _agent.MaxSpeed,
    (wander, 1f), (bounds, 2f), (wallForce, 4f));

// 3. Integrate: force → acceleration → velocity → position
_velocity += (totalForce / _agent.Mass) * deltaTime;
_velocity  = _velocity.Truncate(_agent.MaxSpeed);
_position += _velocity * deltaTime;

// 4. Keep heading aligned with velocity
_steering.UpdateHeadingWhileMoving(deltaTime, ref _rotation);
```

#### Basic Forces

| Method | Parameters | Description |
|--------|-----------|-------------|
| `Seek(targetPosition)` | `Vector2f` | Steer towards the target at maximum speed |
| `Flee(targetPosition)` | `Vector2f` | Steer away from the target at maximum speed |
| `Arrive(targetPosition, slowingRadius)` | `Vector2f`, `float` | Seek with a deceleration zone; the agent slows as it enters the slowing radius |

#### Prediction Forces

| Method | Parameters | Description |
|--------|-----------|-------------|
| `Pursue(target)` | `IMovableGameObject` | Lead the target by predicting its future position from current velocity |
| `Evade(target)` | `IMovableGameObject` | Flee from the target's predicted future position |

#### Pattern Forces

| Method | Parameters | Description |
|--------|-----------|-------------|
| `Wander(ref wanderAngle, wanderRadius, wanderDistance)` | `ref float`, `float`, `float` | Produce smooth pseudo-random wandering by jittering an angle on a projected circle ahead of the agent |

**Wander parameter guide:**

| Parameter | Typical value | Effect |
|-----------|--------------|--------|
| `wanderAngle` | `ref float` per agent | Keeps wander state between frames; initialize to `0f` |
| `wanderRadius` | `50–100` | Radius of the projected wander circle; larger = wider turns |
| `wanderDistance` | `100–250` | Distance of the wander circle ahead of the agent; larger = smoother |

#### Obstacle and Wall Avoidance

| Method | Parameters | Description |
|--------|-----------|-------------|
| `ObstacleAvoidance(obstacles, detectionLength, agentRadius)` | `IReadOnlyList<IGameObject>`, `float`, `float` | Project a detection box ahead and steer away from the nearest circular obstacle |
| `WallAvoidance(walls, feelerLength, feelerAngle)` | `IReadOnlyList<Wall>`, `float`, `float` | Cast three feelers and steer away from the closest wall intersection |
| `StayWithinBounds(boundary, margin)` | `FloatRect`, `float` | Return a centering force when the agent approaches the boundary edges |

**Wall setup guide:**

A `Wall` is a directed line segment. Its normal is the *left perpendicular* of the Start→End
direction — meaning it is **one-sided**: the repulsion force only activates when the agent
approaches the wall from the front face (the side the normal points toward).

For **border walls** this is straightforward — direct each wall so its normal points inward:

```csharp
float w = 1280f, h = 720f;

// Normal points right  (inward for left edge)
Wall left   = new(new Vector2f(0, 0), new Vector2f(0, h));
// Normal points left   (inward for right edge)
Wall right  = new(new Vector2f(w, h), new Vector2f(w, 0));
// Normal points down   (inward for top edge)
Wall top    = new(new Vector2f(w, 0), new Vector2f(0, 0));
// Normal points up     (inward for bottom edge)
Wall bottom = new(new Vector2f(0, h), new Vector2f(w, h));
```

For **interior walls** that agents can approach from either side, register the wall **twice**
with Start↔End swapped so both faces have an active normal:

```csharp
// Diagonal interior wall — visible from both sides
Wall diag1 = new(new Vector2f(400f, 200f), new Vector2f(600f, 400f));
Wall diag2 = new(new Vector2f(600f, 400f), new Vector2f(400f, 200f)); // reversed

List<Wall> walls = [left, right, top, bottom, diag1, diag2];
```

#### Multi-Agent Forces

| Method | Parameters | Description |
|--------|-----------|-------------|
| `OffsetPursuit(leader, offset, slowingRadius)` | `IMovableGameObject`, `Vector2f`, `float` | Follow a leader while maintaining a fixed offset in the leader's local space |
| `Interpose(object1, object2)` | `IMovableGameObject`, `IMovableGameObject` | Position the agent at the midpoint between two moving objects |
| `Hide(target, obstacles, distanceFromBoundary, threatDistance)` | `IMovableGameObject`, `IReadOnlyList<IGameObject>`, `float`, `float` | Move behind the nearest obstacle to hide from the threat |
| `FollowPath(ref pathIndex, pathToFollow, slowingRadius)` | `ref int`, `IReadOnlyList<Vector2f>`, `float` | Arrive at successive waypoints; automatically advance the path index |

#### Flocking

| Method | Parameters | Description |
|--------|-----------|-------------|
| `Separation(neighbors, separationRadius)` | `IReadOnlyList<IMovableGameObject>`, `float` | Push the agent away from nearby neighbors |
| `Alignment(neighbors)` | `IReadOnlyList<IMovableGameObject>` | Steer to match the average heading of the neighborhood |
| `Cohesion(neighbors)` | `IReadOnlyList<IMovableGameObject>` | Seek the centroid of all neighbors |

#### Utility

| Method | Parameters | Description |
|--------|-----------|-------------|
| `UpdateHeadingWhileMoving(deltaTime, ref rotation)` | `float`, `ref float` | Keep `Heading` aligned with the current velocity direction when the agent is moving |

### `SteeringForces`

The `SteeringForces` static class provides two strategies for combining multiple steering force
vectors into a single vector that is guaranteed not to exceed `maxForce`.

| Method | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `WeightedSum(maxForce, params entries)` | `float`, `ReadOnlySpan<(Vector2f Force, float Weight)>` | `Vector2f` | Scale each force by its weight, sum all contributions, then truncate to `maxForce` |
| `PriorityTruncated(budget, params forces)` | `float`, `ReadOnlySpan<Vector2f>` | `Vector2f` | Fill the budget with forces in listed order; stop when the budget is exhausted |

#### Choosing a strategy

| Scenario | Recommended strategy | Reason |
|----------|---------------------|--------|
| Avoidance + wander (forces can oppose each other) | `WeightedSum` | Higher weights on safety forces ensure they dominate even when wander pulls the wrong way |
| Pure flocking (separation + alignment + cohesion) | `WeightedSum` | Predictable blending regardless of individual force magnitudes |
| Strictly independent, budget-filling forces | `PriorityTruncated` | Guarantees high-priority forces are satisfied first when they are either fully active or zero |

> **Note:** `PriorityTruncated` is unsafe when a lower-priority force can oppose a
> higher-priority force with the remaining budget. For example, if `wallForce = 25` away
> and `wanderForce = 55` toward the wall, the net result is 30 toward the wall. Use
> `WeightedSum` with elevated weights for all safety-critical forces instead.

#### `WeightedSum` example

```csharp
Vector2f wander    = _steering.Wander(ref _wanderAngle, 80f, 200f);
Vector2f obstacles = _steering.ObstacleAvoidance(_obstacles, 100f, 16f);
Vector2f walls     = _steering.WallAvoidance(_walls, 120f, 45f);
Vector2f bounds    = _steering.StayWithinBounds(_boundary, 50f);

// Avoidance forces have higher weights so they dominate wander
Vector2f force = SteeringForces.WeightedSum(
    _agent.MaxSpeed,
    (wander,    1f),
    (obstacles, 2f),
    (bounds,    3f),
    (walls,     4f));
```

#### `PriorityTruncated` example

```csharp
Vector2f separation = _steering.Separation(_neighbors, 60f);
Vector2f alignment  = _steering.Alignment(_neighbors);
Vector2f cohesion   = _steering.Cohesion(_neighbors);

// Flocking forces are added in priority order up to budget
Vector2f force = SteeringForces.PriorityTruncated(
    _agent.MaxSpeed,
    separation,
    alignment,
    cohesion);
```

### `SteeringDebugDrawer`

`SteeringDebugDrawer` is a per-agent component that visualizes the internal state of every
`SteeringBehavior` method during development. It implements `IDisposable` — dispose when the
agent is destroyed to release SFML shape resources.

#### Enabling and disabling

```csharp
// Toggle the debug overlay — typically bound to the backtick key
SteeringDebugDrawer.Enabled = host.Input.WasKeyPressed(Keyboard.Key.Grave);
```

`static Enabled` is a global flag. Setting it to `false` makes every `Draw*` call a no-op, so
you can safely leave all debug calls in your update/render loop without a runtime cost.

#### API reference

| Method | Signature | Color | Description |
|--------|----------|-------|-------------|
| `DrawVelocityAndHeading` | `(window)` | White / Green | Velocity arrow (white) and heading arrow (green) from the agent's position |
| `DrawWander` | `(window, wanderAngle, wanderRadius, wanderDistance)` | Cyan / Yellow | Wander circle (cyan) with current target dot (yellow) |
| `DrawSeek` | `(window, targetPosition)` | Green | Line from agent to seek target |
| `DrawFlee` | `(window, targetPosition)` | Orange | Line from agent to flee point |
| `DrawArrive` | `(window, targetPosition, slowingRadius)` | Green | Seek line and slowing-radius circle |
| `DrawPursue` | `(window, target)` | Magenta | Line to predicted future position |
| `DrawEvade` | `(window, target)` | Red | Line to predicted future position |
| `DrawAutoPilot` | `(window, targetPosition, arrivalRadius)` | Light green / Yellow | Target dot, arrival radius circle, and required-heading arrow |
| `DrawBoundary` | `(window, boundary, margin)` | Blue | Outer boundary rect and inner margin rect |
| `DrawObstacleAvoidance` | `(window, obstacles, detectionLength, agentRadius)` | Orange box / Red hit | Detection box; nearest obstacle highlighted |
| `DrawWallAvoidance` | `(window, walls, feelerLength, feelerAngle)` | Yellow feelers / Red hit | Three feelers; active wall intersection shown in red |
| `DrawOffsetPursuit` | `(window, leader, offset, slowingRadius)` | Cyan / Magenta | Formation slot, predicted target, and steering lines |
| `DrawInterpose` | `(window, object1, object2)` | Orange / Green | Predicted positions of both objects and midpoint target |
| `DrawHide` | `(window, target, obstacles, distanceFromBoundary, threatDistance)` | Orange / Green | Hiding spots behind obstacles relative to the threat |
| `DrawFollowPath` | `(window, pathToFollow)` | Grey / Yellow | Waypoint dots and connecting path lines |
| `DrawNeighborhood` | `(window, neighbors, neighborhoodRadius)` | Blue | Neighbor detection radius circle and lines to neighbors |
| `DrawSeparation` | `(window, neighbors, separationRadius)` | Red | Separation radius circle and repulsion arrows |
| `DrawAlignment` | `(window, neighbors)` | Cyan | Arrow in the direction of the average heading |
| `DrawCohesion` | `(window, neighbors)` | Green | Arrow toward centroid of the neighborhood |
| `static DrawStats` | `(window, font, fps, updateMs)` | Light grey text | FPS and last update time in the bottom-left corner |
| `static DrawSpatialGrid<T>` | `(window, grid, font)` | Blue cells / White count | Occupied grid cells as translucent rectangles with item-count labels |
| `Dispose()` | — | — | Release all SFML shape resources |

#### Integration pattern

Create the debug drawer alongside the behavior, pair each `SteeringBehavior` call with its
matching `Draw*` call inside `Render`, and toggle with a key binding:

```csharp
// Construction
_steering = new SteeringBehavior(this);
_debug    = new SteeringDebugDrawer(this);

// In scene Update — toggle with backtick
SteeringDebugDrawer.Enabled = host.Input.WasKeyPressed(Keyboard.Key.Grave);

// In agent Update — compute forces normally
public void Update(float dt)
{
    Vector2f wander    = _steering.Wander(ref _wanderAngle, 80f, 200f);
    Vector2f wallForce = _steering.WallAvoidance(_walls, 120f, 45f);

    Vector2f force = SteeringForces.WeightedSum(
        MaxSpeed,
        (wander, 1f), (wallForce, 4f));

    _velocity += (force / Mass) * dt;
    _velocity  = _velocity.Truncate(MaxSpeed);
    _position += _velocity * dt;
    _steering.UpdateHeadingWhileMoving(dt, ref _rotation);
}

// In agent Render — mirror each behavior call with its debug counterpart
public void Render(RenderWindow window, Font font, double fps, double updateMs)
{
    // ... draw agent sprite ...

    _debug.DrawVelocityAndHeading(window);
    _debug.DrawWander(window, _wanderAngle, 80f, 200f);
    _debug.DrawWallAvoidance(window, _walls, 120f, 45f);

    // DrawStats is static — call once per frame, not once per agent
    SteeringDebugDrawer.DrawStats(window, font, fps, updateMs);
}
```

### Usage Example

```csharp
using GrayHare.GameEngine.Behaviors;
using SFML.Graphics;
using SFML.System;

internal sealed class AutonomousAgent : IMovableGameObject, IDisposable
{
    private readonly SteeringBehavior    _steering;
    private readonly SteeringDebugDrawer _debug;
    private readonly IList<Wall>         _walls;
    private readonly FloatRect           _boundary;
    private float                        _wanderAngle;
    private float                        _rotation;

    public Vector2f Position { get; set; }
    public Vector2f Velocity { get; set; }
    public Vector2f Heading  { get; private set; } = new(0f, -1f);
    public float    Mass     { get; } = 1f;
    public float    MaxSpeed { get; } = 160f;
    public float    Speed    => Velocity.Length;

    public AutonomousAgent(IList<Wall> walls, FloatRect boundary)
    {
        _walls    = walls;
        _boundary = boundary;
        _steering = new SteeringBehavior(this);
        _debug    = new SteeringDebugDrawer(this);
    }

    public void Update(float dt)
    {
        Vector2f wander      = _steering.Wander(ref _wanderAngle, 80f, 200f);
        Vector2f wallForce   = _steering.WallAvoidance(_walls, 120f, 45f);
        Vector2f boundsForce = _steering.StayWithinBounds(_boundary, 50f);

        // Safety forces have elevated weights to dominate wander
        Vector2f force = SteeringForces.WeightedSum(
            MaxSpeed,
            (wander,      1f),
            (boundsForce, 2f),
            (wallForce,   4f));

        Velocity += (force / Mass) * dt;
        Velocity  = Velocity.Truncate(MaxSpeed);
        Position += Velocity * dt;
        _steering.UpdateHeadingWhileMoving(dt, ref _rotation);
    }

    public void Render(RenderWindow window, Font font, double fps, double updateMs)
    {
        // ... draw agent sprite ...

        _debug.DrawVelocityAndHeading(window);
        _debug.DrawWander(window, _wanderAngle, 80f, 200f);
        _debug.DrawWallAvoidance(window, _walls, 120f, 45f);
        SteeringDebugDrawer.DrawStats(window, font, fps, updateMs);
    }

    public void Dispose()
    {
        _debug.Dispose();
    }
}
```

---

## Extensions

### Overview

The `Extensions` namespace provides convenience extension methods for common SFML and .NET types,
reducing boilerplate in scene and behavior code.

### Extension Methods

| Extension Class | Method | Parameters | Return Type | Description |
|----------------|--------|-----------|-------------|-------------|
| `FloatExtensions` | `float.ToVector2f()` | — | `Vector2f` | Convert a rotation angle in degrees to a unit `Vector2f`; 0° points right, angles increase clockwise |
| `ShapeExtensions` | `Shape.ToTexture(padding)` | `uint = 0` | `Texture` | Render the shape off-screen to a `Texture`; `padding` adds transparent pixels around the shape |
| `VectorExtensions` | `Vector2f.Truncate(maximum)` | `float` | `Vector2f` | Clamp the vector's length to `maximum`, preserving direction |
| `VectorExtensions` | `Vector2f.DistanceTo(to)` | `Vector2f` | `float` | Calculate the Euclidean distance between two points |
| `VectorExtensions` | `Vector2f.WrapPosition(size)` | `Vector2f` | `Vector2f` | Wrap each component into the range `[0, size)`, producing toroidal / screen-wrap movement |
| `VectorExtensions` | `Vector2f.WrapPosition(size)` | `Vector2u` | `Vector2f` | `Vector2u` overload of `WrapPosition`; converts `size` to `Vector2f` before wrapping |
| `WindowExtensions` | `RenderWindow.DrawCenteredText(font, fontSize, color, text, y)` | `Font`, `uint`, `Color`, `string`, `float` | `void` | Draw a text string horizontally centered in the window at the specified Y coordinate |

### Usage Example

```csharp
// Convert a heading angle to a direction vector
float rotationDeg = 45f;
Vector2f direction = rotationDeg.ToVector2f(); // ≈ (0.707, 0.707)

// Clamp velocity to the agent's maximum speed
Vector2f velocity = new(500f, 300f);
Vector2f clamped  = velocity.Truncate(250f);

// Euclidean distance check
float dist = agentPosition.DistanceTo(targetPosition);
if (dist < 50f)
{
    Console.WriteLine("Target is close.");
}

// Rasterize a circle shape to a reusable texture
CircleShape circle = new(16f) { FillColor = Color.Red };
Texture tex = circle.ToTexture(padding: 2);

// Wrap an agent's position for toroidal (screen-wrap) movement
Vector2f screenSize = new(1280f, 720f);
position = position.WrapPosition(screenSize);

// Same wrap using a Vector2u window size
position = position.WrapPosition(window.Size);

// Draw centered title text
window.DrawCenteredText(_font, 48, Color.White, "GAME OVER", 340f);
```

---

## Shaders

### Overview

GLSL shader support is provided through `AssetStore`. Shaders are loaded, compiled, and cached per
path. `GlslVersionParser` is a static utility used internally by `TryLoadShader` to include the
detected GLSL version number in failure messages, aiding cross-platform diagnosis of
version-mismatch errors.

### `GlslVersionParser` (static)

| Method | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `Parse(shaderSource)` | `string` | `int?` | Extract the GLSL version number from a `#version NNN` directive. Returns `null` if no directive is present. |

### Loading Shaders via `AssetStore`

| Method | Description |
|--------|-------------|
| `assets.LoadShader(fragPath)` | Load a fragment-only shader. Throws on compilation failure. |
| `assets.LoadShader(vertPath, fragPath)` | Load a vertex + fragment shader pair. Throws on failure. |
| `assets.TryLoadShader(fragPath, out reason)` | Load a fragment shader; returns `null` with a descriptive message on failure. |
| `assets.TryLoadShader(vertPath, fragPath, out reason)` | Load a shader pair; returns `null` with a descriptive message on failure. |

> **Platform note:** GLSL version support varies by GPU driver and OS. Use `TryLoadShader` in
> production builds to degrade gracefully when a shader is not supported on the target platform.

### Usage Example

```csharp
private Shader? _waveShader;
private Clock   _clock = new();

public override void Load(GameHost host)
{
    if (host.Assets.TryLoadShader("shaders/wave.frag", out string? reason) is { } shader)
    {
        _waveShader = shader;
    }
    else
    {
        Console.Error.WriteLine($"Wave shader failed to load: {reason}");
    }
}

public override void Render(GameHost host, RenderWindow window)
{
    if (_waveShader is not null)
    {
        _waveShader.SetUniform("time", _clock.ElapsedTime.AsSeconds());
        window.Draw(_sprite, new RenderStates(_waveShader));
    }
    else
    {
        // Fallback: render without post-process effect
        window.Draw(_sprite);
    }
}
```

---

## Wall

### Overview

`Wall` is an immutable `readonly record struct` representing a directed line segment used primarily
by `SteeringBehavior.WallAvoidance()`. The wall's inward-facing normal is computed automatically
from the start-to-end direction at construction time. Swapping `Start` and `End` reverses the
normal, making the wall face the opposite side.

### `Wall`

| Member | Type | Description |
|--------|------|-------------|
| `Start` | `Vector2f` | The start point of the segment |
| `End` | `Vector2f` | The end point of the segment |
| `Normal` | `Vector2f` | Unit normal vector (left perpendicular of `Start → End`) — the side the wall faces |
| `Wall(start, end)` | Constructor | Compute and store the unit normal automatically |
| `TryGetIntersection(from, to, out float test)` | `Vector2f`, `Vector2f`, `out float` → `bool` | Parametric line-segment intersection; `test` ∈ [0, 1] is the normalized position along the `from → to` ray |

### Usage Example

```csharp
// Define four inward-facing walls that form a closed room
List<Wall> walls =
[
    new(new Vector2f(  0,   0), new Vector2f(800,   0)),  // top
    new(new Vector2f(800,   0), new Vector2f(800, 600)),  // right
    new(new Vector2f(800, 600), new Vector2f(  0, 600)),  // bottom
    new(new Vector2f(  0, 600), new Vector2f(  0,   0)),  // left
];

// Use wall avoidance in the agent update
SteeringBehavior steering = new(agent);

public void Update(float dt)
{
    Vector2f avoidForce = steering.WallAvoidance(walls, feelerLength: 100f, feelerAngle: 45f);
    _velocity += (avoidForce / agent.Mass) * dt;
    _velocity  = _velocity.Truncate(agent.MaxSpeed);
    _position += _velocity * dt;
}

// Manual intersection test — e.g. for projectile collision
Vector2f bulletStart = new(100, 100);
Vector2f bulletEnd   = new(900, 100);

foreach (Wall wall in walls)
{
    if (wall.TryGetIntersection(bulletStart, bulletEnd, out float t))
    {
        Console.WriteLine($"Bullet hits wall at t = {t:F2}");
    }
}
```

---

## Spatial

### Overview

The `Spatial` module provides `SpatialGrid<T>`, a generic grid-based spatial hash that
partitions 2D space into fixed-size cells for fast radius-based neighbor queries. It is
designed for a per-frame rebuild workflow and integrates directly with the flocking methods
on `SteeringBehavior` (Separation, Alignment, Cohesion) by returning results in a
`List<T>` that implements `IReadOnlyList<T>`.

### `SpatialGrid<T>`

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `SpatialGrid(cellSize)` | `float` | — | Create a grid with the given cell width/height; a good default equals the largest query radius |
| `CellSize` | — | `float` | The width and height of each grid cell |
| `Count` | — | `int` | The number of items currently stored in the grid |
| `Clear()` | — | `void` | Remove all items; cell lists are pooled internally so subsequent `Add` calls reuse them |
| `Add(item, position)` | `T`, `Vector2f` | `void` | Insert an item at the given world-space position |
| `FindNeighbors(position, radius, results, exclude?)` | `Vector2f`, `float`, `List<T>`, `T?` | `int` | Clear `results`, fill it with items within `radius`, and return the count; optionally skip `exclude` |
| `EnumerateCells()` | — | `IEnumerable<(Vector2f, int)>` | Yield each occupied cell's world-space origin and item count (for debug visualization) |

> **Performance notes:**
> - Cell lists are pooled so that `Clear` → `Add` cycles produce zero allocations after warm-up.
> - `FindNeighbors` uses squared-distance checks (no `MathF.Sqrt`) in the inner loop.
> - Choose `cellSize` close to the largest query radius; smaller cells reduce per-query work
>   but increase the number of cells visited.

### Usage Example

```csharp
// ── Setup ────────────────────────────────────────────────────────────────
var grid      = new SpatialGrid<IMovableGameObject>(cellSize: 120f);
var neighbors = new List<IMovableGameObject>();

// ── Each frame in Update ─────────────────────────────────────────────────
grid.Clear();
foreach (IMovableGameObject agent in allAgents)
{
    grid.Add(agent, agent.Position);
}

foreach (IMovableGameObject agent in allAgents)
{
    grid.FindNeighbors(agent.Position, neighborhoodRadius, neighbors, exclude: agent);

    // neighbors is a List<T> which implements IReadOnlyList<T> —
    // pass directly to any SteeringBehavior flocking method:
    Vector2f sep = steering.Separation(neighbors, separationRadius);
    Vector2f ali = steering.Alignment(neighbors);
    Vector2f coh = steering.Cohesion(neighbors);
}

// ── Debug visualization in Render ────────────────────────────────────────
SteeringDebugDrawer.DrawSpatialGrid(window, grid, font);
```

---

## Pathfinding

### Overview

The `Pathfinding` namespace provides five graph-search algorithms for grid-based navigation —
BFS, DFS, Dijkstra, A*, and Flow Field — plus a `PathfindingDebugDrawer` for visualizing search results. All
algorithms use 4-direction (orthogonal) movement and return a `PathfindingResult` that captures
both the solved path and the full explored region, making it straightforward to compare
exploration patterns across algorithms.

### `GridCell`

A `readonly record struct` that identifies a single grid position.

| Member | Type | Description |
|--------|------|-------------|
| `GridCell(Row, Column)` | constructor | Create a cell at the given zero-based row and column |
| `Row` | `int` | Zero-based row index |
| `Column` | `int` | Zero-based column index |

### `PathfindingAlgorithm`

| Value | Description |
|-------|-------------|
| `BFS` | Breadth-first search — guarantees shortest path on unweighted grids |
| `DFS` | Depth-first search — finds a valid path but does not guarantee shortest |
| `Dijkstra` | Dijkstra's algorithm — guarantees shortest path |
| `AStar` | A\* with Manhattan heuristic — guarantees shortest path, explores fewer cells |
| `FlowField` | Flow field — BFS from goal outward; path extracted by following per-cell vectors |

### `PathfindingGrid`

A rectangular grid of walkable and blocked cells used for pathfinding queries. All cells are
walkable by default. Neighbor queries use 4-direction (orthogonal) movement only.

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `PathfindingGrid(rows, columns)` | `int`, `int` | — | Create a grid; throws `ArgumentOutOfRangeException` if either dimension ≤ 0 |
| `Rows` | — | `int` | Number of rows |
| `Columns` | — | `int` | Number of columns |
| `IsInBounds(cell)` | `GridCell` | `bool` | Whether the cell is within the grid bounds |
| `IsWalkable(cell)` | `GridCell` | `bool` | Whether the cell is in-bounds and not blocked |
| `IsBlocked(cell)` | `GridCell` | `bool` | Whether the cell is in-bounds and blocked |
| `SetBlocked(cell, blocked)` | `GridCell`, `bool` | `void` | Mark a cell as blocked or walkable; throws `ArgumentOutOfRangeException` if out of bounds |
| `Clear()` | — | `void` | Reset all cells to walkable |
| `GetWalkableNeighbors(cell, results)` | `GridCell`, `List<GridCell>` | `void` | Clear and fill the caller-owned list with walkable orthogonal neighbors; reuse the list across calls to avoid allocations |

### `PathfindingResult`

| Member | Type | Description |
|--------|------|-------------|
| `Start` | `GridCell` | The starting cell of the search |
| `End` | `GridCell` | The target cell of the search |
| `Path` | `IReadOnlyList<GridCell>` | Ordered cells from start to end (inclusive); empty when no path was found |
| `Visited` | `IReadOnlySet<GridCell>` | All cells explored during the search |
| `Found` | `bool` | `true` when `Path.Count > 0` |

### `PathFinder` (static)

| Method | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `FindPath(grid, start, end, algorithm)` | `PathfindingGrid`, `GridCell`, `GridCell`, `PathfindingAlgorithm` | `PathfindingResult` | Dispatch to the specified algorithm |
| `BreadthFirstSearch(grid, start, end)` | `PathfindingGrid`, `GridCell`, `GridCell` | `PathfindingResult` | BFS — guarantees shortest path |
| `DepthFirstSearch(grid, start, end)` | `PathfindingGrid`, `GridCell`, `GridCell` | `PathfindingResult` | DFS — valid path, not guaranteed shortest |
| `Dijkstra(grid, start, end)` | `PathfindingGrid`, `GridCell`, `GridCell` | `PathfindingResult` | Dijkstra — guarantees shortest path |
| `AStar(grid, start, end)` | `PathfindingGrid`, `GridCell`, `GridCell` | `PathfindingResult` | A\* — guarantees shortest path, uses Manhattan heuristic |
| `BuildFlowField(grid, goal)` | `PathfindingGrid`, `GridCell` | `FlowFieldResult` | BFS from `goal` outward; builds a per-cell direction map for the entire grid |

> **Edge-case behaviour:**
> - If `start` is blocked, `Found` is `false` and `Visited` is empty.
> - If `end` is blocked, the search explores normally and `Visited` reflects the explored region, but `Found` is `false` because no walkable path leads to a blocked cell.
> - If `start == end`, the result contains a single-cell path `[start]`.

### `FlowFieldResult`

Returned by `PathFinder.BuildFlowField`. Stores a per-cell direction map built by BFS outward
from the goal. Each reachable cell records its next step toward the goal, enabling O(1) path
queries per agent per frame — useful for navigating many agents to the same destination.

| Member | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `Goal` | — | `GridCell` | The goal cell the field was built toward |
| `GetNextCell(cell)` | `GridCell` | `GridCell?` | The next cell to move to from `cell`; `null` if `cell` is the goal or unreachable |
| `IsReachable(cell)` | `GridCell` | `bool` | `true` if the cell can reach the goal (includes the goal itself) |
| `ReachableCells` | — | `IEnumerable<GridCell>` | All cells with a recorded next step (excludes the goal) |

> **Usage pattern:** Call `BuildFlowField` once when the maze changes, then call `GetNextCell`
> every frame per agent. The flow field is static; rebuild it whenever the grid or goal changes.

### `PathfindingDebugDrawer` (static)

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

### `PathfindingGridExtensions` (static)

Bridges `PathfindingGrid` with the engine's `Wall` geometry so that steering walls can
block pathfinding cells without manual conversion.

| Method | Parameters | Return Type | Description |
|--------|-----------|-------------|-------------|
| `grid.ApplyWalls(walls, cellSize, origin)` | `IEnumerable<Wall>`, `float`, `Vector2f` | `void` | Mark every cell intersected by a `Wall` segment as blocked. Tests all four cell edges and both wall endpoints. Does **not** clear the grid first. |

> **How it works:** For each wall, only cells within the wall's axis-aligned bounding box
> are tested, so performance scales with wall length rather than grid size. A cell is
> blocked when any of its four edges intersects the wall segment, or when either wall
> endpoint falls inside the cell (handling very short walls entirely inside one cell).

### Usage Example

```csharp
// ── Setup ────────────────────────────────────────────────────────────────
Vector2f origin   = new(20f, 60f);
float    cellSize = 28f;

var grid = new PathfindingGrid(rows: 22, columns: 42);

// Option A: stamp Wall segments from level geometry onto the grid
IEnumerable<Wall> levelWalls = scene.GetWalls();
grid.ApplyWalls(levelWalls, cellSize, origin);

// Option B: block individual cells directly
grid.SetBlocked(new GridCell(5, 10), true);

// ── Search ───────────────────────────────────────────────────────────────
GridCell          start  = new(0, 0);
GridCell          end    = new(21, 41);
PathfindingResult result = PathFinder.FindPath(grid, start, end, PathfindingAlgorithm.BFS);

if (result.Found)
{
    Console.WriteLine($"Path: {result.Path.Count} cells");
}

// ── Debug visualization in Render ────────────────────────────────────────
PathfindingDebugDrawer.DrawGrid(window, grid, cellSize, origin);
PathfindingDebugDrawer.DrawResult(window, result, cellSize, origin, showVisited: true);

// Flow field — build once, navigate many agents
FlowFieldResult field = PathFinder.BuildFlowField(grid, goal);
PathfindingDebugDrawer.DrawFlowField(window, field, cellSize, origin);
GridCell? next = field.GetNextCell(agentCell); // O(1) per agent per frame
```

---

## Constants

### Overview

The `Constants` class exposes globally accessible named `Vector2f` values to replace magic number
literals in game and behavior code.

### `Constants.Vectors`

| Constant | Value | Description |
|----------|-------|-------------|
| `Constants.Vectors.Zero` | `Vector2f(0, 0)` | Zero vector; useful for resetting position, velocity, or accumulated forces |
| `Constants.Vectors.One` | `Vector2f(1, 1)` | Unit-scale vector; useful as a default scale or uniform direction |

### Usage Example

```csharp
// Reset an agent's velocity to a standstill
agent.Velocity = Constants.Vectors.Zero;

// Apply a neutral (identity) scale to a sprite
_sprite.Scale = Constants.Vectors.One;

// Guard: skip steering if the agent has not yet moved
if (agent.Velocity == Constants.Vectors.Zero)
{
    return;
}
```

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

---

## New in V1

### Scene Stack (Push / Pop)

The engine supports overlay scenes via a scene stack. Use `PushScene` to add an overlay
(e.g. a pause menu) on top of the current scene, and `PopScene` to remove it.

| Member | Description |
|--------|-------------|
| `GameHost.PushScene(GameSceneBase overlay)` | Queues an overlay scene to be pushed at end of frame. |
| `GameHost.PopScene()` | Queues the top scene to be popped at end of frame. |
| `GameHost.SceneStackDepth` | Number of scenes currently on the stack. |
| `GameSceneBase.OnActivated(GameHost)` | Called when the scene becomes the top of the stack. |
| `GameSceneBase.OnDeactivated(GameHost)` | Called when another scene is pushed on top. |

Only the top scene receives `Update` calls. All scenes on the stack are rendered bottom-to-top.

### RemoveLayer

| Member | Description |
|--------|-------------|
| `RemoveLayer(ISceneLayer)` | Removes a previously added layer. Returns true if found. |

### IDisposable Support

`GameSceneBase` now implements `IDisposable`. Override `Dispose(bool disposing)` for custom cleanup.
The engine disposes scenes automatically when they are removed from the stack.

### Camera2D

A 2D camera wrapping SFML's `View` with smooth follow, zoom, and screen-shake.

| Member | Description |
|--------|-------------|
| `Camera2D.Position` | World-space center position. |
| `Camera2D.Zoom` | Zoom level (1 = normal, >1 = zoom in, <1 = zoom out). |
| `Camera2D.Rotation` | Rotation in degrees. |
| `Camera2D.ViewportSize` | Viewport dimensions captured from the window size at construction. |
| `Camera2D.Follow(target, lerpSpeed, deltaTime)` | Smoothly tracks a target position. |
| `Camera2D.Shake(intensity, duration)` | Starts a decaying screen-shake. |
| `Camera2D.UpdateShake(deltaTime)` | Advances the shake timer using raw/unscaled frame time. |
| `Camera2D.GetView()` | Returns the SFML View when you need to apply or override a view manually. |
| `Camera2D.ScreenToWorld(screenPos)` | Converts a screen pixel coordinate to a world-space position, accounting for camera position, zoom, rotation, and shake. |
| `Camera2D.WorldToScreen(worldPos)` | Converts a world-space position to a screen pixel coordinate. Useful for placing UI elements above world objects. |
| `Camera2D.Reset()` | Restores defaults. |
| `GameHost.Camera` | The camera instance for the current application. |

In the normal `GameApplication` render loop, the engine already updates shake and applies
`GameHost.Camera` before the current scene renders.

### Music Streaming

`AudioPlayer` now supports streaming music from disk via SFML's `Music` class.

| Member | Description |
|--------|-------------|
| `PlayMusic(path, volume, loop)` | Streams a music file. Stops previous music first. |
| `StopMusic()` | Stops and disposes the current track. |
| `PauseMusic()` / `ResumeMusic()` | Pause/resume the current track. |
| `IsMusicPlaying` | Whether music is currently playing. |

### Volume Control

| Member | Description |
|--------|-------------|
| `MasterVolume` / `SfxVolume` / `MusicVolume` | Volume levels (0–100). |
| `SetMasterVolume(float)` / `SetSfxVolume(float)` / `SetMusicVolume(float)` | Setters (clamp to 0–100). |
| `Mute()` / `Unmute()` | Silence without losing stored levels. |
| `IsMuted` | Whether audio is muted. |
| `MaxActiveSounds` | Concurrent sound limit (default 32). |
| `ActiveSoundCount` | Number of active sounds. |

### ECS Enhancements

| Member | Description |
|--------|-------------|
| `HasComponent<T>(entity)` | Check without out-parameter. |
| `EntityCount` | Number of live entities. |
| `ComponentTypeCount` | Number of registered component types. |
| `ComponentCount<T>()` | Number of entities with component T. |
| `ForEach<T>(action)` | Iterate entities with one component. |
| `ForEach<TA, TB>(action)` | Iterate entities with two components. |
| `ForEach<TA, TB, TC>(action)` | Iterate entities with three components. |

Entity IDs are now recycled. `Entity` includes a `Generation` field to detect stale handles.

### Gamepad Support

| Member | Description |
|--------|-------------|
| `InputSnapshot.IsJoystickConnected(id)` | Whether a joystick is connected. |
| `InputSnapshot.IsJoystickButtonDown(id, button)` | Joystick button held. |
| `InputSnapshot.WasJoystickButtonPressed(id, button)` | First frame pressed. |
| `InputSnapshot.GetJoystickAxis(id, axis)` | Axis value (−100 to 100). |
| `InputSnapshot.ConnectedJoysticks` | Set of connected joystick IDs. |

### Input Action Mapping

`InputActionMap` lets you define named actions bound to keys and joystick buttons/axes.

| Member | Description |
|--------|-------------|
| `MapKey(action, key)` | Bind a keyboard key. |
| `MapButton(action, joystickId, button)` | Bind a joystick button. |
| `MapMouseButton(action, mouseButton)` | Bind a mouse button (`Mouse.Button`). |
| `MapAxis(action, joystickId, axis, deadZone)` | Bind a joystick axis. |
| `IsActionDown(action, input)` | Any binding held. |
| `WasActionPressed(action, input)` | First frame any binding pressed. |
| `WasActionReleased(action, input)` | First frame any binding released. |
| `GetAxisValue(action, input)` | Axis value outside dead zone. |
| `GameHost.InputActions` | Optional action map instance. |

### Asset Fallback

When `AssetStore.LoadTexture` cannot find a file, it returns a 16×16 magenta/black checkerboard
fallback texture instead of throwing. A diagnostic message is logged via `EngineLogger`.

### Engine Logging

`EngineLogger` provides minimal diagnostic logging (scene transitions, asset loads, shader failures).

| Member | Description |
|--------|-------------|
| `EngineLogger.SetHandler(Action<string>?)` | Replace or disable the log handler. |
| `EngineLogger.Log(string)` | Write a message. |
| `GameApplicationOptions.LogHandler` | Set a custom handler at startup. |

Default handler writes to `System.Diagnostics.Debug.WriteLine`.
