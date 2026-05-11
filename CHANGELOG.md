# Changelog

All notable changes to this project will be documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [0.8.0] - 2026-05-11

### Added

- RenderWindow extension GetCenter
- Option to set Window style & state for the GameApplication

### Fixed

- Cleanup unused files that caused build failure


## [0.7.0] - 2026-05-02

### Added

- extra examples to support the README

### Changed

- Interfaces moved namespace GrayHare.GameEngine.Abstractions to GrayHare.GameEngine.
- Examples refactored and cleaned
- README rewriten

## [0.6.2] - 2026-04-26

### Added

- [A step-by-step guide for building a basic Tetris game](docs/how-to-tetris.md).

### Fixed

- `AudioPlayer.PlaySound` now reuses stopped `Sound` instances from an internal pool instead of allocating a new OpenAL source on every call. This eliminates the per-call `alGenSources` hitch that caused small pauses in the game loop when sound effects fired during gameplay.

### Changed

- README

## [0.6.1] — First public release

### Added

- Initial public NuGet package for .NET 10 built on SFML.Net 3.0.0.
- Core application runtime with `GameApplication`, `GameHost`, `GameTime`, `GameSceneBase`, and `SceneManager`.
- Layered scene composition through `ISceneLayer` and `IRenderLayer`.
- ECS runtime with `World`, `Entity`, typed components, and allocation-friendly queries.
- Input system covering keyboard, mouse, mouse wheel, and joystick state via `InputTracker` and `InputSnapshot`.
- Named input binding with `InputActionMap` for keys, mouse buttons, joystick buttons, and joystick axes.
- Cached asset loading for images, textures, fonts, sound buffers, and GLSL shaders via `AssetStore`.
- Asset helpers including relative path resolution, system-font fallback, and PPM `P3` / `P6` image support.
- Audio playback with pooled sound effects, streamed music, mute support, and master/SFX/music volume control.
- 2D rendering helpers including `Camera2D`, `AnimationClip`, `AnimationPlayer`, and window/math extension helpers.
- Steering and movement helpers for rotation, drifting, steering-force composition, and debug drawing.
- Grid-based spatial queries with `SpatialGrid<T>`.
- Grid pathfinding with BFS, DFS, Dijkstra, A*, and flow-field support.

### Notes

- Version `0.6.1` is the first public release of `GrayHare.GameEngine`.
- Earlier `0.x` versions were private development builds and are not treated as public release history.

---

## Prior development builds

Private development builds existed before `0.6.1`, but they were not published as supported public releases.
