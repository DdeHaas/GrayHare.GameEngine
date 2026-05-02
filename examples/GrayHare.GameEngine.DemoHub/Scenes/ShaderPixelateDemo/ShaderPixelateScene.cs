using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.ShaderPixelateDemo;

/// <summary>
/// Demo – Pixelate Shader.
/// Applies a fragment shader that quantizes texture coordinates to create
/// a pixelation effect. The pixel size is controlled by mouse position,
/// creating a dynamic retro/mosaic visual.
/// Inspired by SFML shader examples.
/// </summary>
internal sealed class ShaderPixelateScene : DemoSceneBase
{
    private Sprite? _sprite;
    private Shader? _shader;
    private Font _font = null!;
    private Text? _fallbackText;

    public ShaderPixelateScene(DemoCatalog catalog, int sceneIndex)
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

        _shader = host.Assets.TryLoadShader(Catalog.Assets.PixelateFragPath, out string? failureReason);

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

        Texture texture = host.Assets.LoadTexture(Catalog.Assets.BackgroundImagePath, smooth: true);

        float x = host.Window.Size.X / 2f - texture.Size.X / 2f;
        float y = host.Window.Size.Y / 2f - texture.Size.Y / 2f;

        _sprite = new Sprite(texture)
        {
            Position = new Vector2f(x, y),
        };

        _shader.SetUniform("u_texture", Shader.CurrentTexture);
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        if (_shader is null)
        {
            return;
        }

        Vector2i mousePos = Mouse.GetPosition(host.Window);
        float normalizedX = mousePos.X / 1280f;
        float normalizedY = mousePos.Y / 720f;

        // Control pixel size based on mouse position (0.001 to 0.1)
        float pixelThreshold = (normalizedX + normalizedY) / 30f;
        _shader.SetUniform("u_pixel_threshold", pixelThreshold);
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        if (_fallbackText is not null)
        {
            window.Draw(_fallbackText);

            return;
        }

        if (_sprite is null || _shader is null)
        {
            return;
        }

        window.Draw(_sprite, new RenderStates(_shader));
    }

    public override void Unload(GameHost host)
    {
        _sprite?.Dispose();
        _fallbackText?.Dispose();
    }
}
