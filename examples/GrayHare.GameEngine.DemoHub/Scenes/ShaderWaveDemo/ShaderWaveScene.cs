using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.ShaderWaveDemo;

/// <summary>
/// Demo 22 – Wave Distortion Shader.
/// Applies a vertex + fragment GLSL shader pair to a full-viewport textured
/// quad. The vertex shader displaces each corner with sine and cosine waves
/// driven by a <c>u_time</c> uniform, creating a rippling warp effect.
/// Inspired by SFML shader examples.
/// </summary>
internal sealed class ShaderWaveScene : DemoSceneBase
{
    private const float ViewportWidth = 1280f;
    private const float ViewportHeight = 720f;

    private Sprite? _sprite;
    private Shader? _shader;
    private Font _font = null!;
    private Text? _fallbackText;
    private Text? _text;

    public ShaderWaveScene(DemoCatalog catalog, int sceneIndex)
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

        _shader = host.Assets.TryLoadShader(
            Catalog.Assets.WaveVertPath,
            Catalog.Assets.WaveFragPath,
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

        _text = new Text(_font)
        {
            DisplayedString = "Praesent suscipit augue in velit pulvinar hendrerit varius purus aliquam.\n" +
                              "Mauris mi odio, bibendum quis fringilla a, laoreet vel orci. Proin vitae vulputate tortor.\n" +
                              "Praesent cursus ultrices justo, ut feugiat ante vehicula quis.\n" +
                              "Donec fringilla scelerisque mauris et viverra.\n" +
                              "Maecenas adipiscing ornare scelerisque. Nullam at libero elit.\n" +
                              "Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas.\n" +
                              "Nullam leo urna, tincidunt id semper eget, ultricies sed mi.\n" +
                              "Morbi mauris massa, commodo id dignissim vel, lobortis et elit.\n" +
                              "Fusce vel libero sed neque scelerisque venenatis.\n" +
                              "Integer mattis tincidunt quam vitae iaculis.\n" +
                              "Vivamus fringilla sem non velit venenatis fermentum.\n" +
                              "Vivamus varius tincidunt nisi id vehicula.\n" +
                              "Integer ullamcorper, enim vitae euismod rutrum, massa nisl semper ipsum,\n" +
                              "vestibulum sodales sem ante in massa.\n" +
                              "Vestibulum in augue non felis convallis viverra.\n" +
                              "Mauris ultricies dolor sed massa convallis sed aliquet augue fringilla.\n" +
                              "Duis erat eros, porta in accumsan in, blandit quis sem.\n" +
                              "In hac habitasse platea dictumst. Etiam fringilla est id odio dapibus sit amet semper dui laoreet.\n",
            CharacterSize = 22,
            Position = new Vector2f(100, 100),
            FillColor = Color.Black,
        };

        // Use a tiled version of the checker texture to make the wave
        // distortion more visually distinct across the full viewport.
        Texture texture = host.Assets.LoadTexture(Catalog.Assets.CheckerTexturePath, smooth: false);
        texture.Repeated = true;

        _sprite = new Sprite(texture)
        {
            TextureRect = new IntRect(new Vector2i(0, 0), new Vector2i((int)ViewportWidth, (int)ViewportHeight)),
            Position = new Vector2f(0f, 0f)
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

        _shader.SetUniform("u_time", (float)gameTime.Total.TotalSeconds);
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        if (_fallbackText is not null)
        {
            window.Draw(_fallbackText);

            return;
        }

        if (_sprite is null || _shader is null || _text is null)
        {
            return;
        }

        window.Draw(_sprite, new RenderStates(_shader));
        window.Draw(_text, new RenderStates(_shader));
    }

    public override void Unload(GameHost host)
    {
        _sprite?.Dispose();
        _fallbackText?.Dispose();
        _text?.Dispose();
    }
}
