using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.ShaderHighlanderDemo;

/// <summary>
/// Demonstrates a fragment shader that uses GLSL 4.60 subgroup operations.
/// Each GPU subgroup elects one "leader" pixel that renders red; all others
/// render dark blue, producing a pattern that exposes GPU workgroup topology.
/// Falls back to an error message when the hardware or driver does not support
/// the required GLSL version.
/// </summary>
internal sealed class ShaderHighlanderScene : DemoSceneBase
{
    private RectangleShape? _canvas;
    private Shader? _shader;
    private Font _font = null!;
    private Text? _fallbackText;

    public ShaderHighlanderScene(DemoCatalog catalog, int sceneIndex)
        : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);

        _font = host.Assets.LoadFont();

        if (!Shader.IsAvailable)
        {
            _fallbackText = new Text(
                _font,
                "Shaders are not available on this GPU.",
                30)
            {
                Position = new Vector2f(200f, 300f),
                FillColor = new Color(255, 120, 80)
            };

            return;
        }

        _shader = host.Assets.TryLoadShader(Catalog.Assets.TheHighlanderFragPath, out string? failureReason);

        if (_shader is null)
        {
            _fallbackText = new Text(
                _font,
                failureReason ?? "Shader failed to load.",
                30)
            {
                Position = new Vector2f(200f, 300f),
                FillColor = new Color(255, 120, 80)
            };

            return;
        }

        _canvas = new RectangleShape((host.Window.Size.X, host.Window.Size.Y));
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        if (_fallbackText is not null)
        {
            window.Draw(_fallbackText);

            return;
        }

        if (_shader is null)
        {
            return;
        }

        window.Draw(_canvas, new RenderStates(_shader));
    }

    public override void Unload(GameHost host)
    {
        _canvas?.Dispose();
        _fallbackText?.Dispose();
    }
}
