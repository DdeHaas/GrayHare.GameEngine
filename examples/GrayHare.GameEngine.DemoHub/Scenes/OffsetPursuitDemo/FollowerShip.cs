using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.OffsetPursuitDemo;

/// <summary>A follower ship in the offset pursuit formation demo.</summary>
internal sealed class FollowerShip : AutonomousAgent
{
    public FollowerShip(Color color) : base(color, maxSpeed: 220f, turnRate: 280f) { }
}
