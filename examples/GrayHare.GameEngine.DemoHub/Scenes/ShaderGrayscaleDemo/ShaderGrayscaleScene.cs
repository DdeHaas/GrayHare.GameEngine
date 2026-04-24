using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.Graphics.Glsl;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.ShaderGrayscaleDemo;

/// <summary>
/// Demo 21 – Grayscale / Tint Shader.
/// Applies a fragment-only GLSL shader to a sprite that desaturates it to
/// grayscale and then tints it with a slowly cycling colour driven by
/// <c>u_tint</c> uniform updates each frame.
/// </summary>
internal sealed class ShaderGrayscaleScene : DemoSceneBase
{
    private Sprite? _sprite;
    private Shader? _shader;
    private Font? _font;
    private Text? _fallbackText;

    public ShaderGrayscaleScene(DemoCatalog catalog, int sceneIndex)
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

        _shader = host.Assets.TryLoadShader(Catalog.Assets.GrayscaleFragPath, out string? failureReason);

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

        Texture texture = host.Assets.LoadTexture(Catalog.Assets.CheckerTexturePath, smooth: true);

        // Scale the sprite to fill most of the viewport so the effect is clearly visible.
        _sprite = new Sprite(texture)
        {
            Origin = new Vector2f(texture.Size.X / 2f, texture.Size.Y / 2f),
            Position = new Vector2f(640f, 360f),
            Scale = new Vector2f(8f, 8f)
        };

        // Bind the sprite's own texture so the sampler reads from it when drawn.
        _shader.SetUniform("u_texture", Shader.CurrentTexture);
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        if (_shader is null)
        {
            return;
        }

        // Cycle through warm hues over time and pass them as the tint colour.
        float t = (float)gameTime.Total.TotalSeconds;
        float r = 0.5f + (0.5f * MathF.Sin(t * 0.7f));
        float g = 0.5f + (0.5f * MathF.Sin((t * 0.5f) + 2.094f));  // 2π/3 offset
        float b = 0.5f + (0.5f * MathF.Sin((t * 0.6f) + 4.189f));  // 4π/3 offset

        _shader.SetUniform("u_tint", new Vec4(r, g, b, 1f));
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
