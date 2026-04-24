using GrayHare.GameEngine.Abstractions;
using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.MultipleLayersDemo;

/// <summary>
/// Scene layer that renders a scrolling starfield in the background.
/// Stars scroll downward at varying speeds and wrap to the top when they
/// reach the bottom of the window.
/// </summary>
internal sealed class StarfieldLayer : ISceneLayer
{
    public int RenderOrder => -10;

    private readonly Random _random = Random.Shared;

    private const int StarCount = 120;
    private readonly float[] _x = new float[StarCount];
    private readonly float[] _y = new float[StarCount];
    private readonly float[] _speed = new float[StarCount];
    private readonly byte[] _brightness = new byte[StarCount];
    private float _width;
    private float _height;

    public void Load(GameHost host)
    {
        _width = host.Window.Size.X;
        _height = host.Window.Size.Y;

        for (int i = 0; i < StarCount; i++)
        {
            _x[i] = (float)(_random.NextDouble() * _width);
            _y[i] = (float)(_random.NextDouble() * _height);
            _speed[i] = (float)(_random.NextDouble() * 20.0 + 5.0);
            _brightness[i] = (byte)_random.Next(120, 255);
        }
    }

    public void Unload(GameHost host)
    {
    }

    public void Update(GameHost host, in GameTime gameTime)
    {
        float dt = gameTime.DeltaTotalSeconds;

        for (int i = 0; i < StarCount; i++)
        {
            _y[i] += _speed[i] * dt;

            if (_y[i] > _height)
            {
                _y[i] = 0f;
                _x[i] = (float)(_random.NextDouble() * _width);
            }
        }
    }

    public void RenderLayer(GameHost host, RenderWindow window)
    {
        using CircleShape dot = new(1.5f);

        for (int i = 0; i < StarCount; i++)
        {
            dot.Position = new Vector2f(_x[i], _y[i]);
            dot.FillColor = new Color(_brightness[i], _brightness[i], _brightness[i]);
            window.Draw(dot);
        }
    }
}
