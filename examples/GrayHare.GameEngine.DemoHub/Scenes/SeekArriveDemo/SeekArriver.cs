using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.SeekArriveDemo;

/// <summary>Agent used by the Seek/Arrive demo.</summary>
internal sealed class SeekArriver : AutonomousAgent
{
    public SeekArriver() : base(new Color(100, 200, 255), maxSpeed: 220f) { }
}
