using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.InterposeDemo;

/// <summary>One of the two wandering targets in the interpose demo.</summary>
internal sealed class TargetShip : AutonomousAgent
{
    public TargetShip(Color color) : base(color, maxSpeed: 100f, turnRate: 200f) { }
}
