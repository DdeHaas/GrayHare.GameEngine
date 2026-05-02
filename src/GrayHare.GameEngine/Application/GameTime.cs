namespace GrayHare.GameEngine.Application;

/// <summary>
/// Immutable snapshot of engine time for a single frame.
/// </summary>
/// <param name="Total">Accumulated scaled time elapsed since the first frame.</param>
/// <param name="Delta">Scaled duration of the current frame (affected by <see cref="TimeScale"/>).</param>
/// <param name="RawTotal">
/// Accumulated real time elapsed since the first frame, unaffected by <see cref="TimeScale"/>.
/// </param>
/// <param name="RawDelta">Real-clock duration of the current frame, unaffected by <see cref="TimeScale"/>.</param>
/// <param name="TimeScale">
/// The time multiplier that was active when this snapshot was produced.
/// A value of <c>0</c> means the game is paused; <c>1</c> is normal speed; values less than
/// <c>1</c> produce slow-motion; values greater than <c>1</c> produce fast-forward.
/// </param>
/// <param name="FrameNumber">Monotonically increasing frame counter.</param>
public readonly record struct GameTime(
    TimeSpan Total,
    TimeSpan Delta,
    TimeSpan RawTotal,
    TimeSpan RawDelta,
    float TimeScale,
    ulong FrameNumber)
{
    /// <summary>Zero-valued starting point used before the first frame runs.</summary>
    public static GameTime Start => new(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, 1f, 0);

    /// <summary>Returns <see langword="true"/> when <see cref="TimeScale"/> is <c>0</c>.</summary>
    public bool IsPaused => TimeScale == 0f;

    /// <summary>Returns the scaled delta time for the current frame in seconds as a single-precision float.</summary>
    public float DeltaTotalSeconds => (float)Delta.TotalSeconds;

    /// <summary>Returns the raw delta time for the current frame in seconds as a single-precision float.</summary>
    public float RawDeltaTotalSeconds => (float)RawDelta.TotalSeconds;

    /// <summary>
    /// Returns a new <see cref="GameTime"/> advanced by <paramref name="rawDelta"/>,
    /// scaled by <paramref name="timeScale"/>.
    /// </summary>
    /// <param name="rawDelta">The real wall-clock duration of this frame.</param>
    /// <param name="timeScale">The time multiplier to apply (clamped to ≥ 0).</param>
    public GameTime Advance(TimeSpan rawDelta, float timeScale = 1f)
    {
        float clampedScale = MathF.Max(0f, timeScale);
        TimeSpan scaledDelta = TimeSpan.FromSeconds(rawDelta.TotalSeconds * clampedScale);

        return new(
            Total + scaledDelta,
            scaledDelta,
            RawTotal + rawDelta,
            rawDelta,
            clampedScale,
            FrameNumber + 1);
    }
}
