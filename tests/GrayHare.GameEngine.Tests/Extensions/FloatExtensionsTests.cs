using GrayHare.GameEngine.Extensions;
using SFML.System;

namespace GrayHare.GameEngine.Tests.Extensions;

public sealed class FloatExtensionsTests
{
    // ── ToVector2f ────────────────────────────────────────────────────────────

    [Fact]
    public void ToVector2f_Zero_PointsRight()
    {
        Vector2f v = 0f.ToVector2f();

        Assert.Equal(1f, v.X, precision: 4);
        Assert.Equal(0f, v.Y, precision: 4);
    }

    [Fact]
    public void ToVector2f_90Degrees_PointsDown()
    {
        // In SFML screen-space, 90° clockwise = +Y direction.
        Vector2f v = 90f.ToVector2f();

        Assert.Equal(0f, v.X, precision: 4);
        Assert.Equal(1f, v.Y, precision: 4);
    }

    [Fact]
    public void ToVector2f_180Degrees_PointsLeft()
    {
        Vector2f v = 180f.ToVector2f();

        Assert.Equal(-1f, v.X, precision: 4);
        Assert.Equal(0f, v.Y, precision: 4);
    }

    [Fact]
    public void ToVector2f_270Degrees_PointsUp()
    {
        Vector2f v = 270f.ToVector2f();

        Assert.Equal(0f, v.X, precision: 4);
        Assert.Equal(-1f, v.Y, precision: 4);
    }

    [Fact]
    public void ToVector2f_ProducesUnitVector()
    {
        Vector2f v = 45f.ToVector2f();

        Assert.Equal(1f, v.Length, precision: 4);
    }
}
