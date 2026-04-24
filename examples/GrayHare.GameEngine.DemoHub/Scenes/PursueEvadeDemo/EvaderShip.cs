using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.PursueEvadeDemo;

/// <summary>The evading agent in the pursue/evade demo.</summary>
internal sealed class EvaderShip : AutonomousAgent
{
    public EvaderShip() : base(new Color(255, 70, 70), maxSpeed: 230f, turnRate: 320f) { }
}
