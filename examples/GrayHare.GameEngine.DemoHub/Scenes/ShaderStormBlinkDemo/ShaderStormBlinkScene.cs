using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.ShaderStormBlinkDemo;

/// <summary>
/// Demo - Storm + Blink Shader.
/// Scene that displays a large number of randomly colored points with a dynamic shader effect,
/// including a fallback message if shaders are unavailable.
/// Inspired by SFML shader examples.
/// </summary>
internal sealed class ShaderStormBlinkScene : DemoSceneBase
{
    private Shader? _shader;
    private Font? _font;
    private Text? _fallbackText;
    private VertexArray? _points;

    public ShaderStormBlinkScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex)
    {
    }

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

        _shader = host.Assets.TryLoadShader(
            Catalog.Assets.StormVertPath,
            Catalog.Assets.BlinkFragPath,
            out string? failureReason);

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

        Random random = Random.Shared;
        Vector2u size = host.Window.Size;

        // Create the points
        _points = new VertexArray(PrimitiveType.Points);
        for (int i = 0; i < 40000; ++i)
        {
            float x = random.Next(0, (int)size.X);
            float y = random.Next(0, (int)size.Y);
            byte r = (byte)random.Next(0, 255);
            byte g = (byte)random.Next(0, 255);
            byte b = (byte)random.Next(0, 255);
            _points.Append(new Vertex(new Vector2f(x, y), new Color(r, g, b)));
        }
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, gameTime);

        if (_shader is null)
        {
            return;
        }

        Vector2i mousePos = Mouse.GetPosition(host.Window);
        Vector2f position = new Vector2f(mousePos.X, mousePos.Y);
        _shader.SetUniform("storm_position", position);

        float time = (float)gameTime.Total.TotalSeconds;
        float radius = 200f + (MathF.Cos(time) * 150f);
        _shader.SetUniform("storm_inner_radius", radius / 3);
        _shader.SetUniform("storm_total_radius", radius);
        _shader.SetUniform("blink_alpha", 0.5f + (MathF.Cos(time * 3) * 0.25F));
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

        RenderStates states = new(_shader);
        window.Draw(_points, states);
    }

    public override void Unload(GameHost host)
    {
        _fallbackText?.Dispose();
        _points?.Clear();
        _points?.Dispose();
    }
}
