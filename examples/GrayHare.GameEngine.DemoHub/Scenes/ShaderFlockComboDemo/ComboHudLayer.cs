using GrayHare.GameEngine.Abstractions;
using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.ShaderFlockComboDemo;

/// <summary>
/// Heads-up display layer rendered above all other layers.
/// Shows the active boid count, FPS, shader state, and key hints. Takes a reference
/// to the <see cref="ComboFlockLayer"/> so it can read live simulation statistics.
/// </summary>
internal sealed class ComboHudLayer : ISceneLayer
{
    private readonly Font _font;
    private readonly ComboFlockLayer _flockLayer;

    public int RenderOrder => 10;

    public ComboHudLayer(Font font, ComboFlockLayer flockLayer)
    {
        _font = font;
        _flockLayer = flockLayer;
    }

    public void Load(GameHost host) { }

    public void Unload(GameHost host) { }

    public void Update(GameHost host, in GameTime gameTime) { }

    public void RenderLayer(GameHost host, RenderWindow window)
    {
        string shaderState = Shader.IsAvailable ? "wave shader" : "shader unavailable";
        string debugState = SteeringDebugDrawer.Enabled ? "ON" : "OFF";

        using Text hint = new(
            _font,
            $"Layers + Shader + Steering Flock  ·  {_flockLayer.ActiveBoidCount} boids  ·  {shaderState}\n" +
            $"` debug ({debugState})  ·  ←/→ cycle demos",
            18)
        {
            Position = new Vector2f(20f, 16f),
            FillColor = new Color(240, 240, 240)
        };

        window.Draw(hint);

        SteeringDebugDrawer.DrawStats(window, _font, _flockLayer.Fps, _flockLayer.UpdateMs);
    }
}
