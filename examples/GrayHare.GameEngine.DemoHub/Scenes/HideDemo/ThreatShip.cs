using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.HideDemo;

/// <summary>The chasing threat in the hide demo.</summary>
internal sealed class ThreatShip : AutonomousAgent
{
    public ThreatShip() : base(new Color(255, 130, 30), maxSpeed: 80f, turnRate: 150f) { }
}
