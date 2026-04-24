using GrayHare.GameEngine.Behaviors;
using SFML.System;

namespace GrayHare.GameEngine.Tests.Behaviors;

public sealed class SteeringForcesTests
{
    // ── WeightedSum ──────────────────────────────────────────────────────────

    [Fact]
    public void WeightedSum_ReturnsZero_WhenNoForces()
    {
        Vector2f result = SteeringForces.WeightedSum(100f);

        Assert.Equal(Constants.Vectors.Zero, result);
    }

    [Fact]
    public void WeightedSum_ScalesForceByWeight()
    {
        Vector2f force = new(10f, 0f);

        Vector2f result = SteeringForces.WeightedSum(1000f, (force, 3f));

        Assert.Equal(new Vector2f(30f, 0f), result);
    }

    [Fact]
    public void WeightedSum_SumsMultipleForces()
    {
        Vector2f a = new(10f, 0f);
        Vector2f b = new(0f, 5f);

        Vector2f result = SteeringForces.WeightedSum(1000f, (a, 1f), (b, 2f));

        Assert.Equal(new Vector2f(10f, 10f), result);
    }

    [Fact]
    public void WeightedSum_TruncatesResultToMaxForce()
    {
        Vector2f force = new(1000f, 0f);

        Vector2f result = SteeringForces.WeightedSum(50f, (force, 5f));

        Assert.True(result.Length <= 50f + float.Epsilon);
    }

    [Fact]
    public void WeightedSum_PreservesDirection_WhenTruncating()
    {
        Vector2f force = new(500f, 0f);

        Vector2f result = SteeringForces.WeightedSum(50f, (force, 1f));

        // Direction should still be along positive X axis.
        Assert.True(result.X > 0f);
        Assert.True(MathF.Abs(result.Y) < float.Epsilon);
    }

    [Fact]
    public void WeightedSum_ReturnsUnchangedForce_WhenBelowMaxForce()
    {
        Vector2f force = new(30f, 0f);

        Vector2f result = SteeringForces.WeightedSum(100f, (force, 1f));

        Assert.Equal(force, result);
    }

    // ── PriorityTruncated ────────────────────────────────────────────────────

    [Fact]
    public void PriorityTruncated_ReturnsZero_WhenNoForces()
    {
        Vector2f result = SteeringForces.PriorityTruncated(100f);

        Assert.Equal(Constants.Vectors.Zero, result);
    }

    [Fact]
    public void PriorityTruncated_ReturnsSingleForce_WhenBudgetSufficient()
    {
        Vector2f force = new(30f, 0f);

        Vector2f result = SteeringForces.PriorityTruncated(100f, force);

        Assert.Equal(force, result);
    }

    [Fact]
    public void PriorityTruncated_TruncatesSingleForce_WhenExceedsBudget()
    {
        Vector2f force = new(200f, 0f);

        Vector2f result = SteeringForces.PriorityTruncated(50f, force);

        Assert.True(result.Length <= 50f + float.Epsilon);
        Assert.True(result.X > 0f);
    }

    [Fact]
    public void PriorityTruncated_HighPriorityForceConsumesEntireBudget_LowPriorityContributesNothing()
    {
        // High-priority force exactly fills the budget.
        Vector2f high = new(100f, 0f);
        Vector2f low = new(50f, 0f);

        Vector2f result = SteeringForces.PriorityTruncated(100f, high, low);

        // Low-priority force must not contribute anything.
        Assert.True(result.Length <= 100f + float.Epsilon);
        Assert.Equal(high, result);
    }

    [Fact]
    public void PriorityTruncated_LowPriorityForceGetsRemainingBudget()
    {
        // High-priority force uses 30 of a 100 budget; low gets the remaining 70.
        Vector2f high = new(30f, 0f);
        Vector2f low = new(0f, 200f);

        Vector2f result = SteeringForces.PriorityTruncated(100f, high, low);

        Assert.True(result.Length <= 100f + float.Epsilon);
        Assert.True(result.X > 0f); // high-priority X contribution
        Assert.True(result.Y > 0f); // low-priority Y contribution
    }

    [Fact]
    public void PriorityTruncated_SkipsZeroForces_WithoutConsumingBudget()
    {
        Vector2f zero = Constants.Vectors.Zero;
        Vector2f actual = new(50f, 0f);

        // Zero force is first but should be skipped; actual force must still be applied in full.
        Vector2f result = SteeringForces.PriorityTruncated(100f, zero, actual);

        Assert.Equal(actual, result);
    }

    [Fact]
    public void PriorityTruncated_ResultMagnitudeNeverExceedsBudget()
    {
        Vector2f f1 = new(80f, 0f);
        Vector2f f2 = new(80f, 0f);
        Vector2f f3 = new(80f, 0f);

        Vector2f result = SteeringForces.PriorityTruncated(100f, f1, f2, f3);

        Assert.True(result.Length <= 100f + float.Epsilon);
    }

    [Fact]
    public void PriorityTruncated_PreservesDirection_WhenPartiallyFilling()
    {
        Vector2f high = new(60f, 0f);  // consumes 60 of 100
        Vector2f low = new(0f, 200f);  // gets 40 remaining, pointing down

        Vector2f result = SteeringForces.PriorityTruncated(100f, high, low);

        Assert.True(result.X > 0f);  // high-priority contribution
        Assert.True(result.Y > 0f);  // low-priority partial contribution
        Assert.True(result.Length <= 100f + float.Epsilon);
    }
}
