using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.Application;

/// <summary>Configuration applied when creating a <see cref="GameApplication"/>.</summary>
public sealed class GameApplicationOptions
{
    /// <summary>Window title bar text.</summary>
    public string Title { get; init; } = "GrayHare.GameEngine";

    /// <summary>Initial window size in pixels.</summary>
    public Vector2u WindowSize { get; init; } = new(1280, 720);

    /// <summary>Color used to clear the window each frame before rendering.</summary>
    public Color ClearColor { get; init; } = new(18, 24, 32);

    /// <summary>Frame-rate cap (0 = uncapped). Ignored when <see cref="VerticalSyncEnabled"/> is true.</summary>
    public uint FrameRateLimit { get; init; } = 60;

    /// <summary>Enables vertical synchronization.</summary>
    public bool VerticalSyncEnabled { get; init; } = true;

    /// <summary>
    /// Root directory used when resolving relative asset paths.
    /// Defaults to the application base directory.
    /// </summary>
    public string ContentRootPath { get; init; } = AppContext.BaseDirectory;

    /// <summary>
    /// Optional log handler for engine diagnostics. When set, the engine calls this
    /// delegate for scene transitions, asset loads, and other diagnostic events.
    /// Defaults to <see langword="null"/> (uses <see cref="System.Diagnostics.Debug.WriteLine(string)"/>).
    /// </summary>
    public Action<string>? LogHandler { get; init; }
}
