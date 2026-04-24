using GrayHare.GameEngine.Abstractions;
using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.MultipleLayersDemo;

/// <summary>
/// Scene layer that overlays a semi-transparent pause screen when the host is paused.
/// Pressing Space toggles pause/resume via <see cref="GameHost.Pause"/> and
/// <see cref="GameHost.Resume"/>.
/// </summary>
internal sealed class PauseLayer : ISceneLayer
{
    private readonly Font _font;

    public int RenderOrder => int.MaxValue; // Always on top

    public PauseLayer(Font font)
    {
        _font = font;
    }

    public void Load(GameHost host)
    {
    }

    public void Unload(GameHost host)
    {
    }

    public void Update(GameHost host, in GameTime gameTime)
    {
        if (host.Input.WasKeyPressed(Keyboard.Key.Space))
        {
            if (host.IsPaused)
            {
                host.Resume();
            }
            else
            {
                host.Pause();
            }
        }
    }

    public void RenderLayer(GameHost host, RenderWindow window)
    {
        if (!host.IsPaused)
        {
            return;
        }

        using RectangleShape veil = new(new Vector2f(window.Size.X, window.Size.Y))
        {
            FillColor = new Color(0, 0, 0, 160)
        };

        window.Draw(veil);

        window.DrawCenteredText(_font, 64, Color.White, "PAUSED", window.Size.Y / 2f - 110);
        window.DrawCenteredText(_font, 22, new Color(200, 200, 200), "Press Space to resume", window.Size.Y / 2f - 60f);
    }
}
