using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.FlockingLeaderDemo;

/// <summary>Larger, gold-coloured leader ship drawn at 1.4× the standard boid scale.</summary>
internal sealed class FlockLeader : AutonomousAgent
{
    public FlockLeader() : base(new Color(255, 210, 40), maxSpeed: 220f, turnRate: 200f) { }

    /// <inheritdoc/>
    public override void Draw(RenderWindow window)
    {
        Vector2f fwd = HeadingRef * 22f;
        Vector2f left = HeadingRef.Perpendicular() * 13f;

        using ConvexShape ship = new(3)
        {
            FillColor = ShipColor,
            OutlineColor = Color.White,
            OutlineThickness = 2f
        };
        ship.SetPoint(0, Position + fwd);
        ship.SetPoint(1, Position - (fwd * 0.6f) + left);
        ship.SetPoint(2, Position - (fwd * 0.6f) - left);
        window.Draw(ship);
    }
}
