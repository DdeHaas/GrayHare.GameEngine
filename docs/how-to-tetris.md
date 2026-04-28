# How-to: Build a basic Tetris game with GrayHare.GameEngine

This guide walks through building a minimal but complete Tetris game from scratch using
GrayHare.GameEngine. It covers the core engine patterns — scenes, layers, the tick loop, and
input — using a Tetris game as the context.

---

## Table of Contents

- [Project setup](#project-setup)
- [Project structure](#project-structure)
- [Game constants](#game-constants)
- [Tetromino definitions](#tetromino-definitions)
- [Gameplay scene](#gameplay-scene)
- [HUD layer](#hud-layer)
- [Welcome scene](#welcome-scene)
- [Entry point](#entry-point)
- [Running the game](#running-the-game)
- [Next steps](#next-steps)

---

## Project setup

Create a new console project and add the engine package:

```bash
dotnet new console -n GrayHare.Tetris
cd GrayHare.Tetris
dotnet add package GrayHare.GameEngine
```

## Project structure

```
GrayHare.Tetris/
├── GrayHare.Tetris.csproj
├── Program.cs
├── GameConstants.cs
├── Tetromino.cs
├── Layers/
│   └── HudLayer.cs
└── Scenes/
    ├── WelcomeScene.cs
    └── GameplayScene.cs
```

```bash
cd GrayHare.Tetris
mkdir Layers
mkdir Scenes
```

Use your favorite code editor to create the .cs files.

---

## Game constants

`GameConstants.cs` centralises all magic numbers. The standard Tetris board is 10 columns × 20
rows.

```csharp
using SFML.Graphics;

namespace GrayHare.Tetris;

internal static class GameConstants
{
    // Window
    public const uint WindowWidth = 400;
    public const uint WindowHeight = 700;

    // Grid
    public const int BoardCols = 10;
    public const int BoardRows = 20;
    public const int CellSize = 28;
    public const int BoardOffsetX = (int)(WindowWidth / 2) - (BoardCols * CellSize / 2);
    public const int BoardOffsetY = 60;

    // Tick speed — seconds per automatic drop step
    public const float TickInterval = 0.5f;

    // Colors
    public static readonly Color GridLineColor = new(40, 40, 40);
    public static readonly Color HudColor = Color.White;
}
```
---

## Tetromino definitions

Each of the 7 standard pieces is defined as four rotation states. Every rotation state is a list
of four `(col, row)` offsets relative to the piece's origin. The origin is the top-left corner of
the smallest bounding box that contains the piece.

`TetrominoShapes.cs`

```csharp
using SFML.Graphics;

namespace GrayHare.Tetris;

/// <summary>Shape data for all 7 standard tetrominoes.</summary>
internal static class TetrominoShapes
{
    /// <summary>
    /// Rotation states for each piece type.
    /// Index 0 = I, 1 = O, 2 = T, 3 = S, 4 = Z, 5 = J, 6 = L.
    /// Each rotation contains four (col, row) offsets from the piece origin.
    /// </summary>
    public static readonly (int Col, int Row)[][][] Rotations =
    [
        // I — cyan (4-wide bounding box)
        [
            [(0, 1), (1, 1), (2, 1), (3, 1)],
            [(2, 0), (2, 1), (2, 2), (2, 3)],
            [(0, 2), (1, 2), (2, 2), (3, 2)],
            [(1, 0), (1, 1), (1, 2), (1, 3)],
        ],
        // O — yellow (2-wide bounding box)
        [
            [(0, 0), (1, 0), (0, 1), (1, 1)],
            [(0, 0), (1, 0), (0, 1), (1, 1)],
            [(0, 0), (1, 0), (0, 1), (1, 1)],
            [(0, 0), (1, 0), (0, 1), (1, 1)],
        ],
        // T — purple (3-wide bounding box)
        [
            [(0, 0), (1, 0), (2, 0), (1, 1)],
            [(0, 0), (0, 1), (1, 1), (0, 2)],
            [(1, 0), (0, 1), (1, 1), (2, 1)],
            [(1, 0), (0, 1), (1, 1), (1, 2)],
        ],
        // S — green (3-wide bounding box)
        [
            [(1, 0), (2, 0), (0, 1), (1, 1)],
            [(0, 0), (0, 1), (1, 1), (1, 2)],
            [(1, 0), (2, 0), (0, 1), (1, 1)],
            [(0, 0), (0, 1), (1, 1), (1, 2)],
        ],
        // Z — red (3-wide bounding box)
        [
            [(0, 0), (1, 0), (1, 1), (2, 1)],
            [(1, 0), (0, 1), (1, 1), (0, 2)],
            [(0, 0), (1, 0), (1, 1), (2, 1)],
            [(1, 0), (0, 1), (1, 1), (0, 2)],
        ],
        // J — blue (3-wide bounding box)
        [
            [(0, 0), (0, 1), (1, 1), (2, 1)],
            [(0, 0), (1, 0), (0, 1), (0, 2)],
            [(0, 0), (1, 0), (2, 0), (2, 1)],
            [(1, 0), (1, 1), (0, 2), (1, 2)],
        ],
        // L — orange (3-wide bounding box)
        [
            [(2, 0), (0, 1), (1, 1), (2, 1)],
            [(0, 0), (0, 1), (0, 2), (1, 2)],
            [(0, 0), (1, 0), (2, 0), (0, 1)],
            [(0, 0), (1, 0), (1, 1), (1, 2)],
        ],
    ];

    public static readonly Color[] Colors =
    [
        new(0, 240, 240),   // I — cyan
        new(240, 240, 0),   // O — yellow
        new(160, 0, 240),   // T — purple
        new(0, 240, 0),     // S — green
        new(240, 0, 0),     // Z — red
        new(0, 0, 240),     // J — blue
        new(240, 160, 0),   // L — orange
    ];

    // Spawn column for each piece type on a 10-wide board (centers the bounding box).
    // I uses a 4-wide box → col 3; O uses a 2-wide box → col 4; all others use 3-wide → col 3.
    public static readonly int[] SpawnCols = [3, 4, 3, 3, 3, 3, 3];
}
```
`Tetromino.cs`

```csharp
using SFML.Graphics;

namespace GrayHare.Tetris;

/// <summary>Represents the active falling piece.</summary>
internal sealed class Tetromino
{
    private static readonly Random _rng = Random.Shared;

    /// <summary>Index into <see cref="TetrominoShapes.Rotations"/> and <see cref="TetrominoShapes.Colors"/>.</summary>
    public int TypeIndex { get; }

    /// <summary>Current rotation state (0–3).</summary>
    public int Rotation { get; private set; }

    /// <summary>Column of the piece origin.</summary>
    public int Col { get; set; }

    /// <summary>Row of the piece origin.</summary>
    public int Row { get; set; }

    /// <summary>The fill color of this piece.</summary>
    public Color Color => TetrominoShapes.Colors[TypeIndex];

    /// <summary>Creates a new random tetromino spawned at the top-center of the board.</summary>
    public Tetromino()
    {
        TypeIndex = _rng.Next(TetrominoShapes.Rotations.Length);
        Rotation = 0;
        Col = TetrominoShapes.SpawnCols[TypeIndex];
        Row = 0;
    }

    /// <summary>Returns all four cell positions for the current rotation state.</summary>
    public IEnumerable<(int Col, int Row)> Cells()
    {
        foreach ((int dc, int dr) in TetrominoShapes.Rotations[TypeIndex][Rotation])
        {
            yield return (Col + dc, Row + dr);
        }
    }

    /// <summary>Rotates the piece clockwise.</summary>
    public void RotateCW() => Rotation = (Rotation + 1) % 4;
}
```

---

## Gameplay scene

`GameplayScene` owns the board state, the active piece, the tick loop, line clearing, and scoring.
It uses the standard scene lifecycle: `Load` initialises resources and layers; `Update` drives
logic; `RenderLayer` draws the board and active piece; `Unload` releases SFML resources.

`Scenes/GameplayScene.cs`

```csharp
using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Scenes;
using GrayHare.Tetris.Layers;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.Tetris.Scenes;

/// <summary>Main Tetris gameplay scene.</summary>
internal sealed class GameplayScene : GameSceneBase
{
    private readonly Font _font;

    // Null-colour means the cell is empty.
    private readonly Color?[,] _board = new Color?[GameConstants.BoardCols, GameConstants.BoardRows];
    private readonly RectangleShape _cellRect = new();
    private readonly VertexArray _gridLines = new(PrimitiveType.Lines);

    private Tetromino _active = new();
    private float _tickAccumulator;
    private bool _gameOver;
    private int _score;

    /// <summary>Creates a new gameplay scene.</summary>
    /// <param name="font">Font used for game-over text.</param>
    public GameplayScene(Font font)
    {
        _font = font ?? throw new ArgumentNullException(nameof(font));
    }

    /// <summary>Called once when the scene becomes active.</summary>
    public override void Load(GameHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        // Attach the HUD layer. A RenderOrder of 10 means it draws after RenderLayer.
        AddLayer(new HudLayer(_font, () => _score));

        // Pre-build grid-line geometry once rather than every frame.
        for (int r = 0; r <= GameConstants.BoardRows; r++)
        {
            float y = GameConstants.BoardOffsetY + (r * GameConstants.CellSize);
            float x0 = GameConstants.BoardOffsetX;
            float x1 = x0 + (GameConstants.BoardCols * GameConstants.CellSize);
            _gridLines.Append(new Vertex(new Vector2f(x0, y), GameConstants.GridLineColor));
            _gridLines.Append(new Vertex(new Vector2f(x1, y), GameConstants.GridLineColor));
        }

        for (int c = 0; c <= GameConstants.BoardCols; c++)
        {
            float x = GameConstants.BoardOffsetX + (c * GameConstants.CellSize);
            float y0 = GameConstants.BoardOffsetY;
            float y1 = y0 + (GameConstants.BoardRows * GameConstants.CellSize);
            _gridLines.Append(new Vertex(new Vector2f(x, y0), GameConstants.GridLineColor));
            _gridLines.Append(new Vertex(new Vector2f(x, y1), GameConstants.GridLineColor));
        }

        ResetBoard();

        // base.Load must be called last so AddLayer calls complete before layer Load hooks run.
        base.Load(host);
    }

    /// <summary>Called once when the scene is replaced by another scene.</summary>
    public override void Unload(GameHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        // base.Unload first so layers are unloaded before we release shared objects.
        base.Unload(host);

        _cellRect.Dispose();
        _gridLines.Dispose();
        // _font is owned by AssetStore — do not dispose it here.
    }

    /// <summary>Called once per frame for game-logic updates.</summary>
    public override void Update(GameHost host, in GameTime gameTime)
    {
        ArgumentNullException.ThrowIfNull(host);

        // base.Update drives the attached layers (including the HUD layer).
        base.Update(host, in gameTime);

        if (_gameOver)
        {
            if (host.Input.WasKeyPressed(Keyboard.Key.Enter) ||
                host.Input.WasKeyPressed(Keyboard.Key.Space))
            {
                host.ChangeScene(new GameplayScene(_font));
            }

            if (host.Input.WasKeyPressed(Keyboard.Key.Escape))
            {
                host.Exit();
            }

            return;
        }

        HandleInput(host);

        // Automatic drop tick. The while loop (not if) handles frames that run
        // longer than one tick interval without skipping steps.
        _tickAccumulator += gameTime.DeltaTotalSeconds;
        while (_tickAccumulator >= GameConstants.TickInterval)
        {
            _tickAccumulator -= GameConstants.TickInterval;
            TickDrop();
        }
    }

    /// <summary>
    /// Renders the scene's own content (called between layers with negative and non-negative RenderOrder).
    /// </summary>
    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(window);

        window.Draw(_gridLines);
        DrawLockedCells(window);
        DrawActivePiece(window);

        if (_gameOver)
        {
            DrawGameOver(window);
        }
    }

    private void HandleInput(GameHost host)
    {
        if (host.Input.WasKeyPressed(Keyboard.Key.Left))
        {
            TryMove(-1, 0);
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Right))
        {
            TryMove(1, 0);
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Down))
        {
            // Soft drop: move one step down and reset the tick so the piece
            // doesn't immediately drop again on the next automatic tick.
            if (TryMove(0, 1))
            {
                _tickAccumulator = 0f;
            }
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Up))
        {
            TryRotate();
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Space))
        {
            HardDrop();
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Escape))
        {
            host.Exit();
        }
    }

    private void TickDrop()
    {
        if (!TryMove(0, 1))
        {
            LockPiece();
        }
    }

    private bool TryMove(int dc, int dr)
    {
        _active.Col += dc;
        _active.Row += dr;

        if (Collides(_active))
        {
            _active.Col -= dc;
            _active.Row -= dr;
            return false;
        }

        return true;
    }

    private void TryRotate()
    {
        int savedRotation = _active.Rotation;
        _active.RotateCW();

        if (!Collides(_active))
        {
            return;
        }

        // Simple wall-kick: try nudging one cell left then one cell right.
        _active.Col--;
        if (!Collides(_active))
        {
            return;
        }

        _active.Col += 2;
        if (!Collides(_active))
        {
            return;
        }

        // Rotation failed — restore original state.
        _active.Col--;
        _active.Rotation = savedRotation;
    }

    private void HardDrop()
    {
        while (TryMove(0, 1))
        {
            // keep dropping until blocked
        }

        LockPiece();
    }

    private void LockPiece()
    {
        foreach ((int c, int r) in _active.Cells())
        {
            if (r >= 0 && r < GameConstants.BoardRows && c >= 0 && c < GameConstants.BoardCols)
            {
                _board[c, r] = _active.Color;
            }
        }

        int cleared = ClearFullLines();
        _score += cleared switch
        {
            1 => 100,
            2 => 300,
            3 => 500,
            4 => 800,
            _ => 0,
        };

        _active = new Tetromino();

        if (Collides(_active))
        {
            _gameOver = true;
        }
    }

    private int ClearFullLines()
    {
        int cleared = 0;

        for (int r = GameConstants.BoardRows - 1; r >= 0; r--)
        {
            if (!IsRowFull(r))
            {
                continue;
            }

            // Shift every row above this one down by one.
            for (int shift = r; shift > 0; shift--)
            {
                for (int c = 0; c < GameConstants.BoardCols; c++)
                {
                    _board[c, shift] = _board[c, shift - 1];
                }
            }

            for (int c = 0; c < GameConstants.BoardCols; c++)
            {
                _board[c, 0] = null;
            }

            cleared++;
            r++; // re-examine this row index — it now holds what was above
        }

        return cleared;
    }

    private bool IsRowFull(int row)
    {
        for (int c = 0; c < GameConstants.BoardCols; c++)
        {
            if (_board[c, row] is null)
            {
                return false;
            }
        }

        return true;
    }

    private bool Collides(Tetromino piece)
    {
        foreach ((int c, int r) in piece.Cells())
        {
            if (c < 0 || c >= GameConstants.BoardCols)
            {
                return true;
            }

            if (r >= GameConstants.BoardRows)
            {
                return true;
            }

            // Cells above the board (r < 0) are allowed — they are not yet visible.
            if (r >= 0 && _board[c, r] is not null)
            {
                return true;
            }
        }

        return false;
    }

    private void ResetBoard()
    {
        for (int c = 0; c < GameConstants.BoardCols; c++)
        {
            for (int r = 0; r < GameConstants.BoardRows; r++)
            {
                _board[c, r] = null;
            }
        }

        _active = new Tetromino();
        _tickAccumulator = 0f;
        _gameOver = false;
        _score = 0;
    }

    private void DrawLockedCells(RenderWindow window)
    {
        for (int c = 0; c < GameConstants.BoardCols; c++)
        {
            for (int r = 0; r < GameConstants.BoardRows; r++)
            {
                if (_board[c, r] is Color color)
                {
                    DrawCell(window, c, r, color);
                }
            }
        }
    }

    private void DrawActivePiece(RenderWindow window)
    {
        foreach ((int c, int r) in _active.Cells())
        {
            if (r >= 0 && r < GameConstants.BoardRows)
            {
                DrawCell(window, c, r, _active.Color);
            }
        }
    }

    private void DrawCell(RenderWindow window, int col, int row, Color color)
    {
        float x = GameConstants.BoardOffsetX + (col * GameConstants.CellSize) + 1f;
        float y = GameConstants.BoardOffsetY + (row * GameConstants.CellSize) + 1f;
        float size = GameConstants.CellSize - 2f;

        _cellRect.Position = new Vector2f(x, y);
        _cellRect.Size = new Vector2f(size, size);
        _cellRect.FillColor = color;
        window.Draw(_cellRect);
    }

    private void DrawGameOver(RenderWindow window)
    {
        float cx = GameConstants.WindowWidth / 2f;
        float cy = GameConstants.WindowHeight / 2f;

        using Text over = new(_font, "GAME OVER", 42);
        over.FillColor = Color.Black;
        FloatRect ob = over.GetLocalBounds();
        over.Origin = new Vector2f(ob.Position.X + ob.Size.X / 2f, ob.Position.Y + ob.Size.Y / 2f);
        over.Position = new Vector2f(cx, cy);

        using Text hint = new(_font, "ENTER — restart    ESC — quit", 18);
        hint.FillColor = Color.Black;
        FloatRect hb = hint.GetLocalBounds();
        hint.Origin = new Vector2f(hb.Position.X + hb.Size.X / 2f, hb.Position.Y);
        hint.Position = new Vector2f(cx, cy + 52f);

        using RectangleShape panel = new()
        {
            Size = new Vector2f(hb.Size.X + 20, 120f),
            FillColor = new Color(200, 200, 200)
        };
        panel.Origin = new Vector2f(panel.Size.X / 2f, panel.Size.Y / 2f);
        panel.Position = new Vector2f(cx - 4f, cy + 20f);

        window.Draw(panel);
        window.Draw(over);
        window.Draw(hint);
    }
}
```

### Key engine patterns used above

| Pattern | Where | Notes |
|---------|-------|-------|
| `AddLayer(layer)` | `Load` | Attaches an `ISceneLayer`; layers with `RenderOrder >= 0` render after `RenderLayer`. |
| `base.Load(host)` | Last line of `Load` | Ensures all `AddLayer` calls complete before layer `Load` hooks run. |
| `base.Update(host, gameTime)` | First line of `Update` | Forwards the frame to attached layers so they update. |
| `base.Unload(host)` | First line of `Unload` | Unloads layers before the scene releases its own resources. |
| Tick accumulator | `Update` | `while` (not `if`) keeps logic deterministic across variable-length frames. |
| `host.ChangeScene(...)` | `Update` | Requests a scene swap; the engine applies it at the end of the frame. |
| `gameTime.DeltaTotalSeconds` | `Update` | Use for all movement and timers. |

---

## HUD layer

`ISceneLayer` lets you draw independently of the scene's own `RenderLayer`. A `RenderOrder` of
`10` places the HUD on top.

`Layers/HudLayer.cs`

```csharp
using GrayHare.GameEngine.Abstractions;
using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.Tetris.Layers;

/// <summary>Heads-up display showing the current score.</summary>
internal sealed class HudLayer : ISceneLayer
{
    private readonly Font _font;
    private readonly Func<int> _getScore;
    private int _lastScore = -1;
    private string _scoreText = string.Empty;

    public int RenderOrder => 10;

    /// <summary>Creates the HUD layer.</summary>
    /// <param name="font">Font used to render text.</param>
    /// <param name="getScore">Callback that returns the current score.</param>
    public HudLayer(Font font, Func<int> getScore)
    {
        _font = font ?? throw new ArgumentNullException(nameof(font));
        _getScore = getScore ?? throw new ArgumentNullException(nameof(getScore));
    }

    /// <summary>Called once when the scene becomes active.</summary>
    public void Load(GameHost host) { }

    /// <summary>Called once when the scene is replaced by another scene.</summary>
    public void Unload(GameHost host) { }

    /// <summary>Called once per frame for game-logic updates.</summary>
    public void Update(GameHost host, in GameTime gameTime)
    {
        ArgumentNullException.ThrowIfNull(host);

        int score = _getScore();
        if (score != _lastScore)
        {
            _scoreText = $"SCORE: {score:D6}";
            _lastScore = score;
        }
    }

    /// <summary>
    /// Renders the content (with negative RenderOrder, before scene and non-negative RenderOrder after scene).
    /// </summary>
    public void RenderLayer(GameHost host, RenderWindow window)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(window);

        using Text text = new(_font, _scoreText, 22);
        text.FillColor = GameConstants.HudColor;
        text.Position = new Vector2f(GameConstants.BoardOffsetX, 18f);
        window.Draw(text);
    }
}
```

> **Lazy string formatting** — rebuild the formatted score string only when the value changes.
> Creating a new `string` every frame inside `RenderLayer` generates allocation pressure;
> caching it in `Update` is the conventional approach used across all example games.

---

## Welcome scene

A simple title screen. The font is loaded here and passed to `GameplayScene` so that it is loaded
from disk only once and cached by `AssetStore` for the lifetime of the application.

`Scenes/WelcomeScene.cs`

```csharp
using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Extensions;
using GrayHare.GameEngine.Scenes;
using SFML.Graphics;
using SFML.Window;

namespace GrayHare.Tetris.Scenes;

/// <summary>Title screen — press any key to start.</summary>
internal sealed class WelcomeScene : GameSceneBase
{
    private Font? _font;
    private float _blinkTimer;
    private bool _showPrompt = true;

    private const float BlinkInterval = 0.55f;

    /// <summary>Called once when the scene becomes active.</summary>
    public override void Load(GameHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        _font = host.Assets.LoadFont(); // User system font

        base.Load(host);
    }

    /// <summary>Called once when the scene is replaced by another scene.</summary>
    public override void Unload(GameHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        base.Unload(host);

        // The font is owned by AssetStore — setting the reference to null is correct;
        // calling Dispose() here would be wrong.
        _font = null;
    }

    /// <summary>Called once per frame for game-logic updates.</summary>
    public override void Update(GameHost host, in GameTime gameTime)
    {
        ArgumentNullException.ThrowIfNull(host);

        base.Update(host, in gameTime);

        if (host.Input.WasKeyPressed(Keyboard.Key.Escape))
        {
            host.Exit();
            return;
        }

        if (host.Input.WasAnyKeyPressed())
        {
            host.ChangeScene(new GameplayScene(_font!));
            return;
        }

        _blinkTimer += gameTime.DeltaTotalSeconds;
        if (_blinkTimer >= BlinkInterval)
        {
            _blinkTimer -= BlinkInterval;
            _showPrompt = !_showPrompt;
        }
    }

    /// <summary>
    /// Renders the content (with negative RenderOrder, before scene and non-negative RenderOrder after scene).
    /// </summary>
    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(window);

        if (_font is null)
        {
            return;
        }

        window.DrawCenteredText(_font, 64, new Color(200, 60, 60), "TETRIS", 140f);
        window.DrawCenteredText(_font, 18, new Color(0, 160, 200), new string('─', 32), 218f);

        if (_showPrompt)
        {
            window.DrawCenteredText(_font, 24, Color.Yellow, "PRESS ANY KEY TO START", 320f);
        }

        window.DrawCenteredText(_font, 14, new Color(100, 100, 100), "Powered by GrayHare GameEngine", 680f);
    }
}
```

> **`DrawCenteredText`** is an extension method on `RenderWindow` from
> `GrayHare.GameEngine.Extensions`. It draws text horizontally centred at the given `y` position.

---

## Entry point

`Program.cs` creates the window options and starts the application on the welcome scene.

```csharp
using GrayHare.GameEngine.Application;
using GrayHare.Tetris.Scenes;
using SFML.Graphics;
using SFML.System;

string contentRoot = Path.Combine(AppContext.BaseDirectory, "Assets");

GameApplicationOptions options = new()
{
    Title = "Tetris",
    WindowSize = new Vector2u(GameConstants.WindowWidth, GameConstants.WindowHeight),
    ClearColor = new Color(20, 20, 20),
    FrameRateLimit = 60,
    VerticalSyncEnabled = true,
    ContentRootPath = contentRoot,
    LogHandler = Console.WriteLine,
};

new GameApplication(options).Run(new WelcomeScene());
```

> `contentRoot` is the root that `AssetStore` uses when resolving relative asset names.

---

## Running the game

```bash
dotnet run
```

The window opens at 400 × 700 pixels.

| Key | Action |
|-----|--------|
| ← / → | Move left / right |
| ↑ | Rotate clockwise |
| ↓ | Soft drop (one step, resets tick) |
| Space | Hard drop |
| Enter | Restart after game over |
| Esc | Quit |
