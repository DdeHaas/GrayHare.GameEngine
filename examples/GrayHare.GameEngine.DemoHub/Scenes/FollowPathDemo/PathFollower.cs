using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.FollowPathDemo;

/// <summary>Agent that follows the demo path.</summary>
internal sealed class PathFollower : AutonomousAgent
{
    public PathFollower() : base(new Color(60, 220, 100), maxSpeed: 180f, turnRate: 280f) { }
}
