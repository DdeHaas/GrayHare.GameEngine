using GrayHare.GameEngine.Abstractions;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes;

/// <summary>
/// Lightweight mutable agent that implements <see cref="IMovableGameObject"/>.
/// Subclasses set visual colour and physics parameters. This is the shared base class
/// for all single-agent steering scene demos.
/// </summary>
internal class AutonomousAgent : IMovableGameObject
{
    public AutonomousAgent(Color color, float maxSpeed = 180f, float turnRate = 300f)
    {
        ShipColor = color;
        MaxSpeed = maxSpeed;
        TurnRate = turnRate;
    }

    public Color ShipColor { get; }
    public Vector2f Position { get; set; }
    public Vector2f HeadingRef = new(1f, 0f);
    public Vector2f Velocity { get; set; }
    public float RotationDegrees;

    float IGameObject.Rotation => RotationDegrees;
    Vector2f IGameObject.Origin => Constants.Vectors.Zero;
    Vector2f IGameObject.Position => Position;
    Vector2f IGameObject.Scale => Constants.Vectors.One;
    int IGameObject.ZOrder => 0;
    FloatRect IGameObject.GlobalBounds =>
        new(Position - new Vector2f(14f, 14f), new Vector2f(28f, 28f));

    float IMovableGameObject.Mass => 1f;
    Vector2f IMovableGameObject.Heading => HeadingRef;
    Vector2f IMovableGameObject.Side => HeadingRef.Perpendicular();
    Vector2f IMovableGameObject.Velocity => Velocity;
    float IMovableGameObject.Speed => Velocity.Length;
    float IMovableGameObject.Acceleration => 0f;
    float IMovableGameObject.Deceleration => 0f;
    float IMovableGameObject.BrakingDeceleration => 0f;
    public float MaxSpeed { get; }
    public float TurnRate { get; }
    float IMovableGameObject.TurnRate => TurnRate;
    float IMovableGameObject.MaxTurnRate => 360f;

    /// <summary>Draws a filled triangle oriented to <see cref="HeadingRef"/> at <see cref="Position"/>.</summary>
    public virtual void Draw(RenderWindow window)
    {
        Vector2f fwd = HeadingRef * 16f;
        Vector2f left = HeadingRef.Perpendicular() * 9f;

        using ConvexShape ship = new(3)
        {
            FillColor = ShipColor,
            OutlineColor = Color.White,
            OutlineThickness = 1f
        };
        ship.SetPoint(0, Position + fwd);
        ship.SetPoint(1, Position - (fwd * 0.6f) + left);
        ship.SetPoint(2, Position - (fwd * 0.6f) - left);
        window.Draw(ship);
    }

    /// <inheritdoc/>
    public void Update(float dt) { }

    /// <inheritdoc/>
    public void Dispose() { }
}
