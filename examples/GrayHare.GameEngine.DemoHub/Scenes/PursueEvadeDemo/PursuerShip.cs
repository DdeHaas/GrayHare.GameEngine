using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.PursueEvadeDemo;

/// <summary>The pursuing agent in the pursue/evade demo.</summary>
internal sealed class PursuerShip : AutonomousAgent
{
    public PursuerShip() : base(new Color(255, 220, 40), maxSpeed: 200f, turnRate: 250f) { }
}
