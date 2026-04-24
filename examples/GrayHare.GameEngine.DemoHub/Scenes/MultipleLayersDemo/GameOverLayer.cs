using GrayHare.GameEngine.Abstractions;
using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.MultipleLayersDemo;

/// <summary>
/// Scene layer that renders a game-over overlay and automatically fires
/// <see cref="GameOverHandled"/> after a short countdown so the owning scene
/// can transition to the next state.
/// </summary>
internal sealed class GameOverLayer : ISceneLayer
{
    private readonly Font _font;
    private string _waitTimerText = string.Empty;
    private float _returnTimer;
    private bool _isGameOver;
    private bool _isEnabled;

    public int RenderOrder => int.MaxValue; // Always on top

    public bool IsGameOver
    {
        get => _isGameOver;
        set
        {
            _isGameOver = value;
            if (value)
            {
                _isEnabled = true;
            }
        }
    }

    public event Action? GameOverHandled;

    public GameOverLayer(Font font)
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
        if (!_isEnabled)
        {
            return;
        }

        float dt = gameTime.DeltaTotalSeconds;

        const int waitDuration = 6; // seconds to wait before handling game over

        _returnTimer += dt;
        _waitTimerText = new string('.', Math.Max(0, waitDuration - (int)_returnTimer)); // animate dots while waiting
        if (_returnTimer >= waitDuration)
        {
            GameOverHandled?.Invoke();
        }
    }

    public void RenderLayer(GameHost host, RenderWindow window)
    {
        if (!_isEnabled)
        {
            return;
        }

        window.DrawCenteredText(_font, 64, Color.Red, "GAME OVER", window.Size.Y / 2f);
        window.DrawCenteredText(_font, 40, Color.White, _waitTimerText, window.Size.Y / 2f + 40f);
    }
}
