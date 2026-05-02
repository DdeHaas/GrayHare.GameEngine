using GrayHare.GameEngine.Behaviors;
using SFML.System;

namespace GrayHare.GameEngine.Tests.Core;

public sealed class WallTests
{
    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsStartAndEnd()
    {
        var start = new Vector2f(0f, 0f);
        var end = new Vector2f(100f, 0f);

        var wall = new Wall(start, end);

        Assert.Equal(start, wall.Start);
        Assert.Equal(end, wall.End);
    }

    [Fact]
    public void Constructor_ComputesUnitNormal_ForHorizontalWall()
    {
        // A wall pointing right (+X): Perpendicular() = (-Y, X) = (0, 1) — pointing down in screen space.
        var wall = new Wall(new Vector2f(0f, 0f), new Vector2f(1f, 0f));

        Assert.Equal(0f, wall.Normal.X, precision: 4);
        Assert.Equal(1f, wall.Normal.Y, precision: 4);
    }

    [Fact]
    public void Constructor_ComputesUnitNormal_ForVerticalWall()
    {
        // A wall pointing down (+Y): Perpendicular() = (-Y, X) = (-1, 0) — pointing left.
        var wall = new Wall(new Vector2f(0f, 0f), new Vector2f(0f, 1f));

        Assert.Equal(-1f, wall.Normal.X, precision: 4);
        Assert.Equal(0f, wall.Normal.Y, precision: 4);
    }

    [Fact]
    public void Constructor_NormalIsUnitLength()
    {
        var wall = new Wall(new Vector2f(0f, 0f), new Vector2f(3f, 4f));

        Assert.Equal(1f, wall.Normal.Length, precision: 4);
    }

    [Fact]
    public void Constructor_ZeroLengthSegment_UsesFallbackNormal()
    {
        var point = new Vector2f(50f, 50f);

        var wall = new Wall(point, point);

        // Fallback normal for a zero-length segment is (0, -1).
        Assert.Equal(new Vector2f(0f, -1f), wall.Normal);
    }

    // ── TryGetIntersection ────────────────────────────────────────────────────

    [Fact]
    public void TryGetIntersection_ReturnsTrue_WhenFeelerCrossesWall()
    {
        // Horizontal wall from (0, 10) to (20, 10).
        var wall = new Wall(new Vector2f(0f, 10f), new Vector2f(20f, 10f));

        // Feeler from above to below the wall, crossing the midpoint.
        bool hit = wall.TryGetIntersection(new Vector2f(10f, 0f), new Vector2f(10f, 20f), out float t);

        Assert.True(hit);
        Assert.Equal(0.5f, t, precision: 4);
    }

    [Fact]
    public void TryGetIntersection_ReturnsFalse_WhenFeelerDoesNotReachWall()
    {
        var wall = new Wall(new Vector2f(0f, 10f), new Vector2f(20f, 10f));

        // Feeler stops short of the wall.
        bool hit = wall.TryGetIntersection(new Vector2f(10f, 0f), new Vector2f(10f, 5f), out _);

        Assert.False(hit);
    }

    [Fact]
    public void TryGetIntersection_ReturnsFalse_WhenParallel()
    {
        // Feeler and wall both run horizontally — never intersect.
        var wall = new Wall(new Vector2f(0f, 10f), new Vector2f(20f, 10f));

        bool hit = wall.TryGetIntersection(new Vector2f(0f, 5f), new Vector2f(20f, 5f), out _);

        Assert.False(hit);
    }

    [Fact]
    public void TryGetIntersection_ReturnsFalse_WhenFeelerMissesWallHorizontally()
    {
        // Wall is from (0,10)→(10,10). Feeler passes through (15,0)→(15,20), which is
        // to the right of the wall — the crossing point is outside the wall segment.
        var wall = new Wall(new Vector2f(0f, 10f), new Vector2f(10f, 10f));

        bool hit = wall.TryGetIntersection(new Vector2f(15f, 0f), new Vector2f(15f, 20f), out _);

        Assert.False(hit);
    }

    [Fact]
    public void TryGetIntersection_TValue_IsZero_WhenFeelerStartsOnWall()
    {
        var wall = new Wall(new Vector2f(0f, 10f), new Vector2f(20f, 10f));

        bool hit = wall.TryGetIntersection(new Vector2f(10f, 10f), new Vector2f(10f, 20f), out float t);

        Assert.True(hit);
        Assert.Equal(0f, t, precision: 4);
    }

    [Fact]
    public void TryGetIntersection_TValue_IsOne_WhenFeelerEndsOnWall()
    {
        var wall = new Wall(new Vector2f(0f, 10f), new Vector2f(20f, 10f));

        bool hit = wall.TryGetIntersection(new Vector2f(10f, 0f), new Vector2f(10f, 10f), out float t);

        Assert.True(hit);
        Assert.Equal(1f, t, precision: 4);
    }
}
