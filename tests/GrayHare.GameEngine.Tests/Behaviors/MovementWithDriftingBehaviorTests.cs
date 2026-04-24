using GrayHare.GameEngine.Behaviors;
using SFML.System;

namespace GrayHare.GameEngine.Tests.Behaviors;

public sealed class MovementWithDriftingBehaviorTests
{
    [Fact]
    public void IsTurningLeft_UpdatesRotation()
    {
        var obj = new FakeMovable { Rotation = 90f, TurnRate = 180f, MaxTurnRate = 360f, Mass = 1f };
        var behavior = new MovementWithDriftingBehavior(obj) { IsTurningLeft = true };
        float rotation = 90f;
        Vector2f heading = new(0f, 1f);

        behavior.Update(1f, ref rotation, ref heading);

        Assert.True(rotation < 90f);
    }

    [Fact]
    public void IsTurningRight_UpdatesRotation()
    {
        var obj = new FakeMovable { Rotation = 90f, TurnRate = 180f, MaxTurnRate = 360f, Mass = 1f };
        var behavior = new MovementWithDriftingBehavior(obj) { IsTurningRight = true };
        float rotation = 90f;
        Vector2f heading = new(0f, 1f);

        behavior.Update(1f, ref rotation, ref heading);

        Assert.True(rotation > 90f);
    }

    [Fact]
    public void IsMovingForwards_DelegatesInput_ReturnsTrue()
    {
        var obj = new FakeMovable();
        var behavior = new MovementWithDriftingBehavior(obj);

        behavior.IsMovingForwards = true;

        Assert.True(behavior.IsMovingForwards);
    }

    [Fact]
    public void IsMovingForwards_ProducesNonZeroVelocity()
    {
        var obj = new FakeMovable { Heading = new Vector2f(1f, 0f), Acceleration = 100f, MaxSpeed = 200f, Mass = 1f };
        var behavior = new MovementWithDriftingBehavior(obj) { IsMovingForwards = true };
        float rotation = 0f;
        Vector2f heading = obj.Heading;

        Vector2f velocity = behavior.Update(1f, ref rotation, ref heading);

        Assert.True(velocity.Length > 0f);
    }

    [Fact]
    public void TurningAlone_DoesNotChangeVelocityWhenAtRest()
    {
        // With no forward input and zero starting velocity, turning should not produce velocity.
        var obj = new FakeMovable { Velocity = new Vector2f(0f, 0f), Rotation = 0f, TurnRate = 90f, MaxTurnRate = 360f, Mass = 1f };
        var behavior = new MovementWithDriftingBehavior(obj) { IsTurningRight = true };
        float rotation = 0f;
        Vector2f heading = new(1f, 0f);

        Vector2f velocity = behavior.Update(1f, ref rotation, ref heading);

        // Velocity stays zero because no forward input was given.
        Assert.Equal(0f, velocity.Length, precision: 4);
    }

    [Fact]
    public void IsBraking_DelegatesInput_ReturnsTrue()
    {
        var obj = new FakeMovable();
        var behavior = new MovementWithDriftingBehavior(obj);

        behavior.IsBraking = true;

        Assert.True(behavior.IsBraking);
    }

    [Fact]
    public void IsBraking_DelegatesInput_ReturnsFalse()
    {
        var obj = new FakeMovable();
        var behavior = new MovementWithDriftingBehavior(obj);

        behavior.IsBraking = false;

        Assert.False(behavior.IsBraking);
    }
}
