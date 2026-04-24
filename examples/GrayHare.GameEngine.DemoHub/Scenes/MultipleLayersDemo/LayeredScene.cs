using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.MultipleLayersDemo;

/// <summary>
/// Demonstrates the scene layer system.
/// <list type="bullet">
///   <item>A <see cref="StarfieldLayer"/> (RenderOrder = -10) draws a scrolling starfield behind the scene.</item>
///   <item>The scene itself renders a bouncing ball.</item>
///   <item>A <see cref="HudLayer"/> (RenderOrder = 10) draws a semi-transparent HUD on top.</item>
///   <item>A <see cref="PauseLayer"/> (RenderOrder = <see cref="int.MaxValue"/>) handles Space to pause/resume.</item>
///   <item>A <see cref="GameOverLayer"/> fires <see cref="GameOverLayer.GameOverHandled"/> after the countdown,
///         upon which the scene resets itself via <see cref="GameHost.ChangeScene"/>.</item>
/// </list>
/// </summary>
internal sealed class LayeredScene : DemoSceneBase
{
    private Font? _font;
    private float _ballX;
    private float _ballY;
    private float _velX = 220f;
    private float _velY = 180f;

    private HudLayer? _hudLayer;
    private GameOverLayer? _gameOverLayer;
    private bool _isGameOver;

    public LayeredScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex)
    {
        AddLayer(new StarfieldLayer());
    }

    public override void Load(GameHost host)
    {
        _font = host.Assets.LoadFont();
        _ballX = host.Window.Size.X / 2f;
        _ballY = host.Window.Size.Y / 2f;

        // Create the HUD & Pause layers here so we can pass the loaded font as a constructor parameter.
        _hudLayer = new HudLayer(_font);
        AddLayer(_hudLayer);
        AddLayer(new PauseLayer(_font));

        _gameOverLayer = new GameOverLayer(_font);
        _gameOverLayer.GameOverHandled += () =>
        {
            host.ChangeScene(new LayeredScene(Catalog, SceneIndex));
        };

        AddLayer(_gameOverLayer);

        base.Load(host); // Call base.Load after adding layers so they get loaded before the scene's Update/Render.
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        if (_isGameOver)
        {
            _gameOverLayer!.IsGameOver = true;
            return;
        }

        float dt = (float)gameTime.DeltaTotalSeconds;
        float w = host.Window.Size.X;
        float h = host.Window.Size.Y;

        _ballX += _velX * dt;
        _ballY += _velY * dt;

        if (_ballX < BallRadius || _ballX > w - BallRadius)
        {
            _velX = -_velX;
            _ballX = Math.Clamp(_ballX, BallRadius, w - BallRadius);
        }

        if (_ballY < BallRadius || _ballY > h - BallRadius)
        {
            _velY = -_velY;
            _ballY = Math.Clamp(_ballY, BallRadius, h - BallRadius);
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.S))
        {
            _hudLayer!.Score++;
        }

        _isGameOver = _hudLayer!.Score >= 10;
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        using CircleShape ball = new(BallRadius)
        {
            Origin = new Vector2f(BallRadius, BallRadius),
            Position = new Vector2f(_ballX, _ballY),
            FillColor = new Color(80, 200, 120)
        };

        window.Draw(ball);
    }

    private const float BallRadius = 24f;
}
