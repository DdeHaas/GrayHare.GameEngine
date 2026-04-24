using GrayHare.GameEngine.Abstractions;
using GrayHare.GameEngine.Behaviors;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub;

/// <summary>
/// A lightweight ship drawable/movable that is used by the rotation and movement demo scenes.
/// </summary>
internal sealed class ShipAgent : IMovableGameObject
{
    // ── Physics state exposed as mutable fields (intentional for demo simplicity) ──
    public Vector2f Position { get; set; }
    public float RotationDegrees;
    public Vector2f HeadingRef;
    public Vector2f Velocity { get; set; }

    // IGameObject
    float IGameObject.Rotation => RotationDegrees;
    Vector2f IGameObject.Origin => Constants.Vectors.Zero;
    Vector2f IGameObject.Position => Position;
    Vector2f IGameObject.Scale => Constants.Vectors.One;
    int IGameObject.ZOrder => 0;
    FloatRect IGameObject.GlobalBounds => new(Position - new Vector2f(12f, 12f), new Vector2f(24f, 24f));

    // IMovableGameObject
    public float Mass => 1f;
    Vector2f IMovableGameObject.Heading => HeadingRef;
    Vector2f IMovableGameObject.Side => HeadingRef.Perpendicular();
    Vector2f IMovableGameObject.Velocity => Velocity;
    float IMovableGameObject.Speed => Velocity.Length;
    public float Acceleration => 240f;
    public float Deceleration => 80f;
    public float BrakingDeceleration => 320f;
    public float MaxSpeed => 300f;
    public float TurnRate => 150f;
    public float MaxTurnRate => 360f;

    // Behaviors (created once, reused each frame)
    public RotationBehavior Rotation { get; }
    public MovementWithRotationBehavior Movement { get; }
    public MovementWithDriftingBehavior Drifting { get; }

    public ShipAgent()
    {
        Rotation = new RotationBehavior(this);
        Movement = new MovementWithRotationBehavior(this);
        Drifting = new MovementWithDriftingBehavior(this);
    }

    // Heading convenience
    public Vector2f Heading
    {
        get => HeadingRef;
        set => HeadingRef = value;
    }

    public void Draw(RenderWindow window)
    {
        // Simple triangle pointing in heading direction.
        Vector2f fwd = HeadingRef * 18f;
        Vector2f left = HeadingRef.Perpendicular() * 10f;

        Vector2f tip = Position + fwd;
        Vector2f baseLeft = Position - (fwd * 0.6f) + left;
        Vector2f baseRight = Position - (fwd * 0.6f) - left;

        using ConvexShape ship = new(3)
        {
            FillColor = new Color(100, 200, 255),
            OutlineColor = Color.White,
            OutlineThickness = 1f
        };
        ship.SetPoint(0, tip);
        ship.SetPoint(1, baseLeft);
        ship.SetPoint(2, baseRight);
        window.Draw(ship);
    }

    public void Update(float deltaTime)
    {
    }

    public void Dispose()
    {
    }
}
