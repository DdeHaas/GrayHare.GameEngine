using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.FlockingSpatialGridDemo;

/// <summary>Lightweight autonomous agent used by the spatial-grid demo.</summary>
internal sealed class GridAgent : AutonomousAgent
{
    public GridAgent() : base(new Color(120, 240, 160), maxSpeed: 240f, turnRate: 300f) { }
}
