using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.HideDemo;

/// <summary>Agent that hides from the threat ship.</summary>
internal sealed class HiderShip : AutonomousAgent
{
    public HiderShip(Color color) : base(color, maxSpeed: 200f, turnRate: 300f) { }
}
