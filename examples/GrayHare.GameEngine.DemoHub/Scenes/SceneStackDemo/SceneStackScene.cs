using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.SceneStackDemo;

/// <summary>
/// Demonstrates <see cref="GameHost.PushScene"/> and <see cref="GameHost.PopScene"/>
/// by allowing the user to push pause and game-over overlay scenes onto the stack.
/// </summary>
internal sealed class SceneStackScene : DemoSceneBase
{
    private Font _font = null!;
    private float _ballX;
    private float _ballY;
    private float _velX = 180f;
    private float _velY = 140f;
    private int _score;
    private float _scoreTimer;

    public SceneStackScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);

        _font = host.Assets.LoadFont();
        _ballX = host.Window.Size.X / 2f;
        _ballY = host.Window.Size.Y / 2f;
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        if (host.Input.WasKeyPressed(Keyboard.Key.P))
        {
            host.PushScene(new PauseOverlayScene());
            return;
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.G))
        {
            host.PushScene(new GameOverOverlayScene());
            return;
        }

        base.Update(host, in gameTime);

        float deltaTime = gameTime.DeltaTotalSeconds;

        // Increment score every second.
        _scoreTimer += deltaTime;
        if (_scoreTimer >= 1f)
        {
            _scoreTimer -= 1f;
            _score++;
        }

        // Bounce the ball.
        const float radius = 20f;
        float maxX = host.Window.Size.X - radius;
        float maxY = host.Window.Size.Y - radius;

        _ballX += _velX * deltaTime;
        _ballY += _velY * deltaTime;

        if (_ballX < radius || _ballX > maxX)
        {
            _velX = -_velX;
            _ballX = Math.Clamp(_ballX, radius, maxX);
        }

        if (_ballY < radius || _ballY > maxY)
        {
            _velY = -_velY;
            _ballY = Math.Clamp(_ballY, radius, maxY);
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // Bouncing ball.
        using CircleShape ball = new(20f)
        {
            Origin = new Vector2f(20f, 20f),
            Position = new Vector2f(_ballX, _ballY),
            FillColor = new Color(80, 200, 255)
        };
        window.Draw(ball);

        using Text scoreText = new(_font, $"Score: {_score}", 26)
        {
            Position = new Vector2f(20f, 20f),
            FillColor = new Color(240, 240, 240)
        };
        window.Draw(scoreText);

        using Text depthText = new(_font, $"Stack Depth: {host.SceneStackDepth}", 20)
        {
            Position = new Vector2f(20f, 56f),
            FillColor = new Color(180, 180, 180)
        };
        window.Draw(depthText);
    }
}
