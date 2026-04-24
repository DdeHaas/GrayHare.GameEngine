using GrayHare.GameEngine.Abstractions;
using GrayHare.GameEngine.Behaviors;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.Tests.Behaviors;

public sealed class RotationBehaviorTests
{
    [Fact]
    public void UpdateRotation_DecreasesAngle_WhenTurningLeft()
    {
        var obj = new FakeMovable { Rotation = 90f, TurnRate = 180f, MaxTurnRate = 360f, Mass = 1f };
        var behavior = new RotationBehavior(obj) { IsTurningLeft = true };
        Vector2f heading = obj.Heading;

        float newRotation = behavior.UpdateRotation(1f, ref heading);

        Assert.True(newRotation < 90f);
    }

    [Fact]
    public void UpdateRotation_IncreasesAngle_WhenTurningRight()
    {
        var obj = new FakeMovable { Rotation = 90f, TurnRate = 180f, MaxTurnRate = 360f, Mass = 1f };
        var behavior = new RotationBehavior(obj) { IsTurningRight = true };
        Vector2f heading = obj.Heading;

        float newRotation = behavior.UpdateRotation(1f, ref heading);

        Assert.True(newRotation > 90f);
    }

    [Fact]
    public void UpdateRotation_DoesNotChangeAngle_WhenNoInput()
    {
        var obj = new FakeMovable { Rotation = 45f, TurnRate = 180f, MaxTurnRate = 360f, Mass = 1f };
        var behavior = new RotationBehavior(obj);
        Vector2f heading = obj.Heading;

        float newRotation = behavior.UpdateRotation(1f, ref heading);

        Assert.Equal(45f, newRotation);
    }

    [Fact]
    public void UpdateRotation_UpdatesHeading_WhenTurning()
    {
        var obj = new FakeMovable { Rotation = 0f, TurnRate = 90f, MaxTurnRate = 360f, Mass = 1f };
        var behavior = new RotationBehavior(obj) { IsTurningRight = true };
        Vector2f heading = obj.Heading;
        Vector2f originalHeading = heading;

        behavior.UpdateRotation(1f, ref heading);

        Assert.NotEqual(originalHeading, heading);
    }
}

public sealed class MovementBehaviorTests
{
    [Fact]
    public void UpdateMovement_IncreasesSpeed_WhenMovingForwards()
    {
        var obj = new FakeMovable
        {
            Heading = new Vector2f(1f, 0f),
            Acceleration = 100f,
            MaxSpeed = 200f,
            Mass = 1f
        };
        var behavior = new MovementBehavior(obj) { IsMovingForwards = true };

        Vector2f velocity = behavior.UpdateMovement(1f, Constants.Vectors.Zero, obj.Position);

        Assert.True(velocity.Length > 0f);
    }

    [Fact]
    public void UpdateMovement_NeverExceedsMaxSpeed()
    {
        var obj = new FakeMovable
        {
            Heading = new Vector2f(1f, 0f),
            Acceleration = 10_000f,
            MaxSpeed = 100f,
            Mass = 1f
        };
        var behavior = new MovementBehavior(obj) { IsMovingForwards = true };

        Vector2f velocity = behavior.UpdateMovement(10f, Constants.Vectors.Zero, obj.Position);

        Assert.True(velocity.Length <= 100f + float.Epsilon);
    }

    [Fact]
    public void UpdateMovement_Decelerates_WhenNoInput()
    {
        var obj = new FakeMovable
        {
            Heading = new Vector2f(1f, 0f),
            Deceleration = 50f,
            MaxSpeed = 200f,
            Mass = 1f
        };
        var behavior = new MovementBehavior(obj);

        Vector2f start = new(100f, 0f);
        Vector2f velocity = behavior.UpdateMovement(1f, start, obj.Position);

        Assert.True(velocity.Length < start.Length);
    }
}

/// <summary>Minimal <see cref="IGameObject"/> stub for obstacle/hiding-spot tests.</summary>
internal sealed class FakeGameObject : IGameObject
{
    public float Rotation { get; set; }
    public Vector2f Origin { get; set; }
    public Vector2f Position { get; set; }
    public Vector2f Scale { get; set; } = Constants.Vectors.One;
    public int ZOrder { get; set; }
    public FloatRect GlobalBounds { get; set; } = new(new Vector2f(-10f, -10f), new Vector2f(20f, 20f));

    public void Draw(RenderWindow window) { }
    public void Update(float deltaTime) { }
    public void Dispose() { }
}

/// <summary>Minimal <see cref="IMovableGameObject"/> stub for unit tests.</summary>
internal sealed class FakeMovable : IMovableGameObject
{
    public float Rotation { get; set; }
    public Vector2f Origin { get; set; }
    public Vector2f Position { get; set; }
    public Vector2f Scale { get; set; } = Constants.Vectors.One;
    public int ZOrder { get; set; }
    public FloatRect GlobalBounds { get; set; } = new(new Vector2f(0f, 0f), new Vector2f(20f, 20f));
    public float Mass { get; set; } = 1f;
    public Vector2f Heading { get; set; } = new(1f, 0f);
    public Vector2f Side => Heading.Perpendicular();
    public Vector2f Velocity { get; set; }
    public float Speed => Velocity.Length;
    public float Acceleration { get; set; } = 100f;
    public float Deceleration { get; set; } = 50f;
    public float BrakingDeceleration { get; set; } = 150f;
    public float MaxSpeed { get; set; } = 200f;
    public float TurnRate { get; set; } = 90f;
    public float MaxTurnRate { get; set; } = 360f;

    public void Draw(RenderWindow window) { }
    public void Update(float deltaTime) { }
    public void Dispose() { }
}
