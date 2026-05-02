using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.SeekArriveDemo;

/// <summary>
/// Demonstrates <see cref="SteeringBehavior.Seek"/> and <see cref="SteeringBehavior.Arrive"/>.
/// An agent follows the mouse cursor; toggle S to switch between Seek (constant speed)
/// and Arrive (slows down near target).
/// </summary>
internal sealed class SeekArriveScene : DemoSceneBase
{
    private readonly SeekArriver _agent = new();
    private readonly SteeringBehavior _steering;
    private readonly SteeringDebugDrawer _debug;
    private Font _font = null!;
    private bool _useArrive = true;
    private double _fps;
    private double _updateMs;
    private const float SlowingRadius = 120f;

    public SeekArriveScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex)
    {
        _steering = new SteeringBehavior(_agent);
        _debug = new SteeringDebugDrawer(_agent);
    }

    public override void Load(GameHost host)
    {
        base.Load(host);

        _agent.Position = new Vector2f(640f, 360f);
        _agent.HeadingRef = new Vector2f(1f, 0f);
        _agent.RotationDegrees = 0f;

        _font = host.Assets.LoadFont();
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        if (host.Input.WasKeyPressed(Keyboard.Key.Grave))
        {
            SteeringDebugDrawer.Enabled = !SteeringDebugDrawer.Enabled;
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Space))
        {
            _useArrive = !_useArrive;
        }

        float deltaTime = gameTime.DeltaTotalSeconds;
        Vector2f target = new(host.Input.MousePosition.X, host.Input.MousePosition.Y);

        Vector2f force = _useArrive
            ? _steering.Arrive(target, SlowingRadius)
            : _steering.Seek(target);

        _agent.Velocity = (_agent.Velocity + (force * deltaTime)).Truncate(_agent.MaxSpeed);
        _agent.HeadingRef = _steering.UpdateHeadingWhileMoving(deltaTime, ref _agent.RotationDegrees);
        _agent.Position += _agent.Velocity * deltaTime;

        _fps = 1.0 / gameTime.DeltaTotalSeconds;
        _updateMs = gameTime.Delta.TotalMilliseconds;
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        Vector2f target = new(host.Input.MousePosition.X, host.Input.MousePosition.Y);

        if (_useArrive)
        {
            _debug.DrawArrive(window, target, SlowingRadius);
        }
        else
        {
            _debug.DrawSeek(window, target);
        }

        _debug.DrawVelocityAndHeading(window);
        _agent.Draw(window);

        // Draw cursor crosshair
        using CircleShape cursor = new(6f) { Origin = new(6f, 6f), Position = target, FillColor = Color.Yellow };
        window.Draw(cursor);

        string mode = _useArrive ? "Arrive @ Mouse" : "Seek Mouse";
        using Text hint = new(_font, $"Mode: {mode}", 20)
        {
            Position = new Vector2f(20f, 20f),
            FillColor = new Color(220, 220, 220)
        };
        window.Draw(hint);
        SteeringDebugDrawer.DrawStats(window, _font, _fps, _updateMs);
    }
}
