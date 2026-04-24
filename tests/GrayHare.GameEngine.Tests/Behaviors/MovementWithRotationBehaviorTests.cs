using GrayHare.GameEngine.Behaviors;
using SFML.System;

namespace GrayHare.GameEngine.Tests.Behaviors;

public sealed class MovementWithRotationBehaviorTests
{
    [Fact]
    public void IsThrottling_IncreasesSpeed()
    {
        var obj = new FakeMovable { Heading = new Vector2f(1f, 0f), Acceleration = 100f, MaxSpeed = 200f, Mass = 1f };
        var behavior = new MovementWithRotationBehavior(obj) { IsMovingForwards = true };
        float rotation = 0f;
        Vector2f heading = obj.Heading;

        Vector2f velocity = behavior.Update(1f, ref rotation, ref heading);

        Assert.True(velocity.Length > 0f);
    }

    [Fact]
    public void IsThrottling_NeverExceedsMaxSpeed()
    {
        var obj = new FakeMovable { Heading = new Vector2f(1f, 0f), Acceleration = 10_000f, MaxSpeed = 100f, Mass = 1f };
        var behavior = new MovementWithRotationBehavior(obj) { IsMovingForwards = true };
        float rotation = 0f;
        Vector2f heading = obj.Heading;

        Vector2f velocity = behavior.Update(10f, ref rotation, ref heading);

        Assert.True(velocity.Length <= 100f + float.Epsilon);
    }

    [Fact]
    public void IsBraking_DecreasesSpeed()
    {
        var obj = new FakeMovable { Heading = new Vector2f(1f, 0f), Acceleration = 100f, MaxSpeed = 200f, BrakingDeceleration = 150f, Mass = 1f };
        var behavior = new MovementWithRotationBehavior(obj) { IsMovingForwards = true };
        float rotation = 0f;
        Vector2f heading = obj.Heading;

        // Build up some speed first.
        behavior.Update(1f, ref rotation, ref heading);
        behavior.IsMovingForwards = false;
        behavior.IsBraking = true;
        Vector2f afterBrake = behavior.Update(1f, ref rotation, ref heading);

        Assert.True(afterBrake.Length < 100f);
    }

    [Fact]
    public void PassiveDeceleration_ReducesSpeed_WhenNoInput()
    {
        var obj = new FakeMovable { Heading = new Vector2f(1f, 0f), Acceleration = 100f, MaxSpeed = 200f, Deceleration = 50f, Mass = 1f };
        var behavior = new MovementWithRotationBehavior(obj) { IsMovingForwards = true };
        float rotation = 0f;
        Vector2f heading = obj.Heading;

        // Build up speed.
        Vector2f withThrottle = behavior.Update(1f, ref rotation, ref heading);
        behavior.IsMovingForwards = false;

        // Let passive deceleration act.
        Vector2f afterDecel = behavior.Update(1f, ref rotation, ref heading);

        Assert.True(afterDecel.Length < withThrottle.Length);
    }

    [Fact]
    public void IsTurningLeft_DecreaseRotation()
    {
        var obj = new FakeMovable { Rotation = 90f, TurnRate = 180f, MaxTurnRate = 360f, Mass = 1f };
        var behavior = new MovementWithRotationBehavior(obj) { IsTurningLeft = true };
        float rotation = 90f;
        Vector2f heading = new(0f, 1f);

        behavior.Update(1f, ref rotation, ref heading);

        Assert.True(rotation < 90f);
    }

    [Fact]
    public void IsTurningRight_IncreasesRotation()
    {
        var obj = new FakeMovable { Rotation = 90f, TurnRate = 180f, MaxTurnRate = 360f, Mass = 1f };
        var behavior = new MovementWithRotationBehavior(obj) { IsTurningRight = true };
        float rotation = 90f;
        Vector2f heading = new(0f, 1f);

        behavior.Update(1f, ref rotation, ref heading);

        Assert.True(rotation > 90f);
    }

    [Fact]
    public void VelocityAlignsWithHeading_WhenThrottling()
    {
        var obj = new FakeMovable { Heading = new Vector2f(1f, 0f), Acceleration = 100f, MaxSpeed = 200f, Mass = 1f };
        var behavior = new MovementWithRotationBehavior(obj) { IsMovingForwards = true };
        float rotation = 0f;
        Vector2f heading = obj.Heading;

        Vector2f velocity = behavior.Update(1f, ref rotation, ref heading);

        // Velocity must be parallel to heading — no drift.
        Assert.True(velocity.X > 0f);
        Assert.Equal(0f, velocity.Y, precision: 4);
    }
}
