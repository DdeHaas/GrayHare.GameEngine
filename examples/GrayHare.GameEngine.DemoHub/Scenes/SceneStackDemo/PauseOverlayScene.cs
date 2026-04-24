using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Scenes;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.SceneStackDemo;

/// <summary>
/// Semi-transparent pause overlay pushed on top of the main scene.
/// Press Escape to pop back to the scene beneath.
/// </summary>
internal sealed class PauseOverlayScene : GameSceneBase
{
    private Font? _font;

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        if (host.Input.WasKeyPressed(Keyboard.Key.Escape))
        {
            host.PopScene();
            return;
        }

        base.Update(host, in gameTime);
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // Semi-transparent dark overlay.
        using RectangleShape overlay = new(new Vector2f(host.Window.Size.X, host.Window.Size.Y))
        {
            FillColor = new Color(0, 0, 0, 160)
        };
        window.Draw(overlay);

        if (_font is null)
        {
            return;
        }

        float cx = host.Window.Size.X / 2f;
        float cy = host.Window.Size.Y / 2f;

        using Text paused = new(_font, "PAUSED", 60)
        {
            FillColor = Color.White
        };
        paused.Position = new Vector2f(cx - paused.GetLocalBounds().Width / 2f, cy - 60f);
        window.Draw(paused);

        using Text hint = new(_font, "Press Esc to resume", 22)
        {
            FillColor = new Color(200, 200, 200)
        };
        hint.Position = new Vector2f(cx - hint.GetLocalBounds().Width / 2f, cy + 20f);
        window.Draw(hint);
    }
}
