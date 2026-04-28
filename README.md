# GrayHare.GameEngine

## Overview

GrayHare.GameEngine is a lightweight cross-platform 2D game engine built on [SFML.Net 3.0.0](https://www.sfml-dev.org/) for .NET applications.
It provides a small runtime centered around `GameApplication`, `GameHost`, `GameSceneBase`, and `World`, so you can build scene-driven games with cached assets, audio, input, camera control, animation, steering behaviors, spatial queries, and grid pathfinding without a large framework. It has been tested on Windows and Linux Mint.

## Installation

Install the NuGet package:

```bash
dotnet add package GrayHare.GameEngine
```

## Running the DemoHub

To start the example hub from the repository root, run:

```powershell
dotnet run --project .\examples\GrayHare.GameEngine.DemoHub\GrayHare.GameEngine.DemoHub.csproj
```

The DemoHub creates its demo assets automatically on first run.

## Core capabilities

- **Application runtime** - `GameApplication` creates the SFML window, asset store, audio player, input tracker, ECS world, camera, and scene manager, then exposes them through `GameHost`.
- **Scene stack** - `GameSceneBase`, `SceneManager`, `ChangeScene`, `PushScene`, and `PopScene` support both full scene changes and overlay scenes. Scene operations are applied at the end of the frame.
- **Layered rendering** - Scenes can compose `ISceneLayer` instances around the scene's own `RenderLayer`, making it easy to add HUD, pause, parallax, or overlay layers.
- **ECS** - `World` manages entities, typed components, and allocation-friendly queries over component sets.
- **Input** - `InputTracker` and `InputSnapshot` expose keyboard, mouse, wheel, and joystick state, while `InputActionMap` maps named actions to keys, buttons, mouse buttons, and axes.
- **Assets** - `AssetStore` caches images, textures, fonts, sound buffers, and GLSL shaders. It supports relative asset paths, system-font fallback, and PPM `P3` and `P6` files in addition to formats handled by SFML.
- **Audio** - `AudioPlayer` supports pooled sound playback, streamed music, mute/stateful volume control, and per-category volume settings.
- **Animation and rendering helpers** - `AnimationClip`, `AnimationPlayer`, `Camera2D`, and extension helpers cover sprite-strip animation, smooth camera follow, zoom, rotation, shake, and common rendering utilities.
- **AI and movement** - Steering and movement helpers cover rotation, drifting, steering-force composition, and debug drawing for steering behavior tuning.
- **Navigation and queries** - `PathFinder` supports BFS, DFS, Dijkstra, A*, and flow fields on `PathfindingGrid`, while `SpatialGrid<T>` handles fast radius-based neighbor lookups for large groups of moving objects.
- **Diagnostics** - `EngineLogger` lets a game supply its own log handler for runtime diagnostics.

## Documentation

Additional documentation is available in [`docs\README.md`](docs/README.md). A Dutch version is available in [`docs\README.nl.md`](docs/README.nl.md).

A step-by-step guide for building a basic Tetris game is available in [`docs\how-to-tetris.md`](docs/how-to-tetris.md).

## Requirements

- .NET 10.0 or later
- [SFML.Net](https://www.nuget.org/packages/SFML.Net) 3.0.0

## License

This project is licensed under the zlib License. See the [LICENSE](LICENSE) file for details.
