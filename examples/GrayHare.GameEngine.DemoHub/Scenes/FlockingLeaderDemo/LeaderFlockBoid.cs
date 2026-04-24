using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.FlockingLeaderDemo;

/// <summary>A single boid that flocks toward and around the leader.</summary>
internal sealed class LeaderFlockBoid : AutonomousAgent
{
    public LeaderFlockBoid(float maxSpeed) : base(new Color(80, 200, 255), maxSpeed, turnRate: 300f) { }
}
