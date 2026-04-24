using GrayHare.GameEngine.Application;
using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.ShaderFlockComboDemo;

/// <summary>
/// Demo 30 – Layers + Shader + Steering Flock.
/// Combines three scene layers to show layer composition in action:
/// <list type="bullet">
///   <item>
///     <see cref="WaveBackgroundLayer"/> (RenderOrder = -10) renders a wave-distorted
///     checker background using a GLSL vertex + fragment shader pair.
///   </item>
///   <item>
///     <see cref="ComboFlockLayer"/> (RenderOrder = 0) runs a flocking simulation with
///     separation, alignment, cohesion, and wander steering behaviors over 80 boids.
///   </item>
///   <item>
///     <see cref="ComboHudLayer"/> (RenderOrder = 10) draws a semi-transparent stats
///     overlay on top of all other layers.
///   </item>
/// </list>
/// Press <c>`</c> (Grave) to toggle the steering debug overlay on the flock layer.
/// </summary>
internal sealed class ShaderFlockComboScene : DemoSceneBase
{
    private ComboFlockLayer? _flockLayer;

    public ShaderFlockComboScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex)
    {
        // Wave background is registered early so it is the bottom-most layer.
        AddLayer(new WaveBackgroundLayer(catalog.Assets));
    }

    public override void Load(GameHost host)
    {
        Font font = host.Assets.LoadFont();

        // Flocking and HUD layers depend on a loaded font and on each other,
        // so they are created here before base.Load propagates Load to all layers.
        _flockLayer = new ComboFlockLayer();
        AddLayer(_flockLayer);
        AddLayer(new ComboHudLayer(font, _flockLayer));

        base.Load(host);
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // All rendering is handled by layers; nothing to draw here.
    }
}
