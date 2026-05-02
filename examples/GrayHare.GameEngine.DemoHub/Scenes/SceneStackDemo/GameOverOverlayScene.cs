using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Scenes;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.SceneStackDemo;

/// <summary>
/// Semi-transparent red-tinted game-over overlay.
/// Press Enter to pop back to the scene beneath.
/// </summary>
internal sealed class GameOverOverlayScene : GameSceneBase
{
    private Font _font = null!;

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        if (host.Input.WasKeyPressed(Keyboard.Key.Enter))
        {
            host.PopScene();
            return;
        }

        base.Update(host, in gameTime);
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // Semi-transparent red overlay.
        using RectangleShape overlay = new(new Vector2f(host.Window.Size.X, host.Window.Size.Y))
        {
            FillColor = new Color(120, 0, 0, 140)
        };
        window.Draw(overlay);

        float cx = host.Window.Size.X / 2f;
        float cy = host.Window.Size.Y / 2f;

        using Text gameOver = new(_font, "GAME OVER", 60)
        {
            FillColor = new Color(255, 80, 80)
        };
        gameOver.Position = new Vector2f(cx - gameOver.GetLocalBounds().Width / 2f, cy - 60f);
        window.Draw(gameOver);

        using Text hint = new(_font, "Press Enter to dismiss", 22)
        {
            FillColor = new Color(220, 180, 180)
        };
        hint.Position = new Vector2f(cx - hint.GetLocalBounds().Width / 2f, cy + 20f);
        window.Draw(hint);
    }
}
