using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.ClearColorDemo;

internal sealed class ClearColorScene : DemoSceneBase
{
    private readonly CircleShape _pulse = new(80f)
    {
        FillColor = new Color(64, 180, 255)
    };

    public ClearColorScene(DemoCatalog catalog, int sceneIndex)
        : base(catalog, sceneIndex)
    {
        _pulse.Origin = new(80f, 80f);
        _pulse.Position = new(640f, 360f);
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        float scale = 1f + (float)Math.Sin(gameTime.Total.TotalSeconds * 2.5) * 0.2f;
        _pulse.Scale = new(scale, scale);
        _pulse.FillColor = new Color(
            (byte)(96 + 64 * (1 + Math.Sin(gameTime.Total.TotalSeconds)) * 0.5),
            180,
            255);
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        RectangleShape backdrop = new(new Vector2f(1280f, 720f))
        {
            FillColor = new Color(20, 28, 40)
        };

        window.Draw(backdrop);
        window.Draw(_pulse);
    }
}
