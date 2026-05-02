using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.ShaderFlockComboDemo;

/// <summary>
/// Scene layer that renders a wave-distorted checker background using a GLSL vertex and
/// fragment shader pair. The <c>u_time</c> uniform is updated every frame to animate
/// the sine/cosine wave displacement.
/// <para>
/// If shaders are unavailable on the current GPU the layer falls back to a solid
/// colour rectangle so the demo still displays correctly.
/// </para>
/// </summary>
internal sealed class WaveBackgroundLayer : ISceneLayer
{
    private readonly DemoAssetsManifest _assets;
    private Sprite? _sprite;
    private Shader? _shader;
    private RectangleShape? _fallback;
    private float _time;

    public int RenderOrder => -10;

    public WaveBackgroundLayer(DemoAssetsManifest assets)
    {
        ArgumentNullException.ThrowIfNull(assets);

        _assets = assets;
    }

    public void Load(GameHost host)
    {
        if (!Shader.IsAvailable)
        {
            _fallback = new RectangleShape(new Vector2f(host.Window.Size.X, host.Window.Size.Y))
            {
                FillColor = new Color(20, 30, 50)
            };

            return;
        }

        _shader = host.Assets.TryLoadShader(
            _assets.WaveVertPath,
            _assets.WaveFragPath,
            out _);

        if (_shader is null)
        {
            _fallback = new RectangleShape(new Vector2f(host.Window.Size.X, host.Window.Size.Y))
            {
                FillColor = new Color(20, 30, 50)
            };

            return;
        }

        Texture texture = host.Assets.LoadTexture(_assets.CheckerTexturePath, smooth: false);
        texture.Repeated = true;

        float w = host.Window.Size.X;
        float h = host.Window.Size.Y;

        _sprite = new Sprite(texture)
        {
            TextureRect = new IntRect(new Vector2i(0, 0), new Vector2i((int)w, (int)h)),
            Position = new Vector2f(0f, 0f),
            Color = new Color(80, 100, 150, 200)
        };

        _shader.SetUniform("u_texture", Shader.CurrentTexture);
    }

    public void Unload(GameHost host)
    {
        _sprite?.Dispose();
        _fallback?.Dispose();
    }

    public void Update(GameHost host, in GameTime gameTime)
    {
        _time = (float)gameTime.Total.TotalSeconds;

        if (_shader is not null)
        {
            _shader.SetUniform("u_time", _time);
        }
    }

    public void RenderLayer(GameHost host, RenderWindow window)
    {
        if (_fallback is not null)
        {
            window.Draw(_fallback);

            return;
        }

        if (_sprite is null || _shader is null)
        {
            return;
        }

        window.Draw(_sprite, new RenderStates(_shader));
    }
}
