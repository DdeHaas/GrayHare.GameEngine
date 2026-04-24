using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.AvoidanceDemo;

/// <summary>Agent used by the wall and obstacle avoidance demo.</summary>
internal sealed class AvoidanceWanderer : AutonomousAgent
{
    public AvoidanceWanderer() : base(new Color(100, 220, 140), maxSpeed: 160f, turnRate: 280f) { }
}
