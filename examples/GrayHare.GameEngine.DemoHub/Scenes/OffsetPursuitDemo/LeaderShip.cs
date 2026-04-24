using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.OffsetPursuitDemo;

/// <summary>The leader ship in the offset pursuit formation demo.</summary>
internal sealed class LeaderShip : AutonomousAgent
{
    public LeaderShip() : base(new Color(240, 240, 240), maxSpeed: 120f, turnRate: 160f) { }
}
