using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.FlockingDemo;

/// <summary>A single boid agent used by the flocking demo.</summary>
internal sealed class FlockBoid : AutonomousAgent
{
    public FlockBoid() : base(new Color(80, 200, 255), maxSpeed: 280f, turnRate: 320f) { }
}
