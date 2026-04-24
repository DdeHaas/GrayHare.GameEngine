using GrayHare.GameEngine.Abstractions;
using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.MultipleLayersDemo;

/// <summary>
/// HUD layer that renders a score display and control hints at the top of the screen.
/// Receives an already-loaded <see cref="Font"/> as a constructor parameter to avoid
/// re-loading assets inside the layer itself.
/// </summary>
internal sealed class HudLayer : ISceneLayer
{
    private readonly Font _font;

    public HudLayer(Font font)
    {
        _font = font;
    }

    public int RenderOrder => 10;
    public uint Score { get; set; }

    public void Load(GameHost host)
    {
    }

    public void Unload(GameHost host)
    {
    }

    public void Update(GameHost host, in GameTime gameTime)
    {
    }

    public void RenderLayer(GameHost host, RenderWindow window)
    {
        using Text hint = new(_font, "Scene layers demo  –  Space = open pause overlay\n" +
            "press S to increase score - 10 = game over\n\n" +
            $"Score: {Score}", 18)
        {
            Position = new Vector2f(20f, 20f),
            FillColor = new Color(200, 200, 200)
        };

        window.Draw(hint);
    }
}
