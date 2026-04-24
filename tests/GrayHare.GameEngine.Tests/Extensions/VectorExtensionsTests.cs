using GrayHare.GameEngine.Extensions;
using SFML.System;

namespace GrayHare.GameEngine.Tests.Extensions;

public sealed class VectorExtensionsTests
{
    // ── Truncate ──────────────────────────────────────────────────────────────

    [Fact]
    public void Truncate_ReturnsOriginal_WhenLengthIsWithinMaximum()
    {
        var v = new Vector2f(3f, 0f);

        Vector2f result = v.Truncate(5f);

        Assert.Equal(v, result);
    }

    [Fact]
    public void Truncate_ClampsLength_WhenVectorExceedsMaximum()
    {
        var v = new Vector2f(10f, 0f);

        Vector2f result = v.Truncate(5f);

        Assert.Equal(5f, result.Length, precision: 4);
    }

    [Fact]
    public void Truncate_PreservesDirection_WhenClamped()
    {
        var v = new Vector2f(0f, 10f);

        Vector2f result = v.Truncate(5f);

        // Direction should still be (0, 1) after truncation.
        Assert.Equal(0f, result.X, precision: 4);
        Assert.True(result.Y > 0f);
    }

    [Fact]
    public void Truncate_ReturnsOriginal_WhenLengthEqualsMaximum()
    {
        var v = new Vector2f(5f, 0f);

        Vector2f result = v.Truncate(5f);

        Assert.Equal(v, result);
    }

    // ── DistanceTo ────────────────────────────────────────────────────────────

    [Fact]
    public void DistanceTo_ReturnsZero_WhenPointsAreCoincident()
    {
        var a = new Vector2f(3f, 4f);

        float dist = a.DistanceTo(a);

        Assert.Equal(0f, dist, precision: 4);
    }

    [Fact]
    public void DistanceTo_ReturnsCorrectDistance_ForHorizontalPair()
    {
        var a = new Vector2f(0f, 0f);
        var b = new Vector2f(5f, 0f);

        Assert.Equal(5f, a.DistanceTo(b), precision: 4);
    }

    [Fact]
    public void DistanceTo_IsSymmetric()
    {
        var a = new Vector2f(1f, 2f);
        var b = new Vector2f(4f, 6f);

        Assert.Equal(a.DistanceTo(b), b.DistanceTo(a), precision: 4);
    }

    [Fact]
    public void DistanceTo_ReturnsCorrectValue_For3_4_5Triangle()
    {
        var a = new Vector2f(0f, 0f);
        var b = new Vector2f(3f, 4f);

        Assert.Equal(5f, a.DistanceTo(b), precision: 4);
    }

    // ── WrapPosition (Vector2f size) ──────────────────────────────────────────

    [Fact]
    public void WrapPosition_ReturnsOriginal_WhenPositionIsInsideBounds()
    {
        var pos = new Vector2f(50f, 100f);
        var size = new Vector2f(200f, 200f);

        Vector2f result = pos.WrapPosition(size);

        Assert.Equal(pos, result);
    }

    [Fact]
    public void WrapPosition_WrapsX_WhenPositionExceedsWidth()
    {
        var pos = new Vector2f(250f, 0f);
        var size = new Vector2f(200f, 200f);

        Vector2f result = pos.WrapPosition(size);

        Assert.Equal(50f, result.X, precision: 4);
        Assert.Equal(0f, result.Y, precision: 4);
    }

    [Fact]
    public void WrapPosition_WrapsNegativeX_IntoPositiveRange()
    {
        var pos = new Vector2f(-50f, 0f);
        var size = new Vector2f(200f, 200f);

        Vector2f result = pos.WrapPosition(size);

        Assert.Equal(150f, result.X, precision: 4);
    }

    [Fact]
    public void WrapPosition_WrapsY_WhenPositionExceedsHeight()
    {
        var pos = new Vector2f(0f, 320f);
        var size = new Vector2f(200f, 300f);

        Vector2f result = pos.WrapPosition(size);

        Assert.Equal(20f, result.Y, precision: 4);
    }

    // ── WrapPosition (Vector2u size) ──────────────────────────────────────────

    [Fact]
    public void WrapPosition_WithVector2uSize_WrapsCorrectly()
    {
        var pos = new Vector2f(1280f, 720f);
        var size = new Vector2u(1280, 720);

        Vector2f result = pos.WrapPosition(size);

        Assert.Equal(0f, result.X, precision: 4);
        Assert.Equal(0f, result.Y, precision: 4);
    }

    [Fact]
    public void WrapPosition_WithVector2uSize_ReturnsOriginal_WhenInsideBounds()
    {
        var pos = new Vector2f(640f, 360f);
        var size = new Vector2u(1280, 720);

        Vector2f result = pos.WrapPosition(size);

        Assert.Equal(pos, result);
    }
}
