using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.InterposeDemo;

/// <summary>The agent that interposes between the two targets.</summary>
internal sealed class InterposeShip : AutonomousAgent
{
    public InterposeShip() : base(new Color(80, 240, 240), maxSpeed: 220f, turnRate: 320f) { }
}
