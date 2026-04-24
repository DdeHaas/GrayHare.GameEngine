using GrayHare.GameEngine.Application;

namespace GrayHare.GameEngine.Tests.Application;

public sealed class GameTimeTests
{
    // ── Start ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Start_HasZeroTotalsAndFrameNumber()
    {
        GameTime gt = GameTime.Start;

        Assert.Equal(TimeSpan.Zero, gt.Total);
        Assert.Equal(TimeSpan.Zero, gt.Delta);
        Assert.Equal(TimeSpan.Zero, gt.RawTotal);
        Assert.Equal(TimeSpan.Zero, gt.RawDelta);
        Assert.Equal(1f, gt.TimeScale);
        Assert.Equal(0UL, gt.FrameNumber);
    }

    // ── Advance — normal speed ────────────────────────────────────────────────

    [Fact]
    public void Advance_WithDefaultTimeScale_AccumulatesTotal()
    {
        GameTime gt = GameTime.Start;
        TimeSpan frame = TimeSpan.FromSeconds(1.0 / 60);

        gt = gt.Advance(frame);
        gt = gt.Advance(frame);

        Assert.Equal(frame + frame, gt.Total);
        Assert.Equal(frame + frame, gt.RawTotal);
        Assert.Equal(2UL, gt.FrameNumber);
    }

    [Fact]
    public void Advance_DeltaMatchesSuppliedRawDelta_AtScale1()
    {
        GameTime gt = GameTime.Start;
        TimeSpan raw = TimeSpan.FromMilliseconds(16);

        gt = gt.Advance(raw);

        Assert.Equal(raw, gt.Delta);
        Assert.Equal(raw, gt.RawDelta);
    }

    // ── Advance — slow motion ─────────────────────────────────────────────────

    [Fact]
    public void Advance_WithHalfTimeScale_DeltaIsHalfOfRawDelta()
    {
        GameTime gt = GameTime.Start;
        TimeSpan raw = TimeSpan.FromSeconds(1.0);

        gt = gt.Advance(raw, 0.5f);

        Assert.Equal(TimeSpan.FromSeconds(0.5), gt.Delta);
        Assert.Equal(raw, gt.RawDelta);
        Assert.Equal(0.5f, gt.TimeScale);
    }

    [Fact]
    public void Advance_WithHalfTimeScale_TotalIsHalfOfRawTotal_AfterTwoFrames()
    {
        GameTime gt = GameTime.Start;
        TimeSpan raw = TimeSpan.FromSeconds(1.0);

        gt = gt.Advance(raw, 0.5f);
        gt = gt.Advance(raw, 0.5f);

        Assert.Equal(TimeSpan.FromSeconds(1.0), gt.Total);
        Assert.Equal(TimeSpan.FromSeconds(2.0), gt.RawTotal);
    }

    // ── Advance — paused (TimeScale = 0) ─────────────────────────────────────

    [Fact]
    public void Advance_WithZeroTimeScale_DeltaIsZero()
    {
        GameTime gt = GameTime.Start;

        gt = gt.Advance(TimeSpan.FromSeconds(1.0), 0f);

        Assert.Equal(TimeSpan.Zero, gt.Delta);
    }

    [Fact]
    public void Advance_WithZeroTimeScale_TotalDoesNotAdvance()
    {
        GameTime gt = GameTime.Start;

        gt = gt.Advance(TimeSpan.FromSeconds(1.0), 0f);
        gt = gt.Advance(TimeSpan.FromSeconds(1.0), 0f);

        Assert.Equal(TimeSpan.Zero, gt.Total);
    }

    [Fact]
    public void Advance_WithZeroTimeScale_RawTotalStillAdvances()
    {
        GameTime gt = GameTime.Start;
        TimeSpan raw = TimeSpan.FromSeconds(1.0);

        gt = gt.Advance(raw, 0f);
        gt = gt.Advance(raw, 0f);

        Assert.Equal(raw + raw, gt.RawTotal);
    }

    // ── IsPaused ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1f, false)]
    [InlineData(0.5f, false)]
    [InlineData(0f, true)]
    public void IsPaused_ReflectsTimeScale(float timeScale, bool expectedPaused)
    {
        GameTime gt = GameTime.Start.Advance(TimeSpan.FromSeconds(1.0), timeScale);

        Assert.Equal(expectedPaused, gt.IsPaused);
    }

    // ── Advance — negative TimeScale clamped ──────────────────────────────────

    /// <summary>
    /// Negative time-scales must clamp to 0: Delta must be zero and the stored
    /// TimeScale must equal 0.
    /// </summary>
    [Theory]
    [InlineData(-1f)]
    [InlineData(-2f)]
    [InlineData(float.NegativeInfinity)]
    public void Advance_NegativeTimeScale_IsClamped_ToZero(float negativeScale)
    {
        GameTime gt = GameTime.Start.Advance(TimeSpan.FromSeconds(1.0), negativeScale);

        Assert.Equal(TimeSpan.Zero, gt.Delta);
        Assert.Equal(0f, gt.TimeScale);
    }

    // ── FrameNumber ───────────────────────────────────────────────────────────

    [Fact]
    public void Advance_IncrementsFrameNumber_EachCall()
    {
        GameTime gt = GameTime.Start;
        TimeSpan frame = TimeSpan.FromMilliseconds(16);

        gt = gt.Advance(frame);
        gt = gt.Advance(frame);
        gt = gt.Advance(frame);

        Assert.Equal(3UL, gt.FrameNumber);
    }
}
