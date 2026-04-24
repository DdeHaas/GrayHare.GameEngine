using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.PursueEvadeDemo;

/// <summary>
/// Demonstrates <see cref="SteeringBehavior.Pursue"/> and <see cref="SteeringBehavior.Evade"/>.
/// A yellow pursuer chases a red evader; the evader predicts the pursuer and flees.
/// Both agents wander when the other is far away.
/// </summary>
internal sealed class PursueEvadeScene : DemoSceneBase
{
    // Pursuer — yellow ship
    private readonly AutonomousAgent _pursuer = new PursuerShip();
    // Evader — red ship
    private readonly AutonomousAgent _evader = new EvaderShip();

    private SteeringBehavior _pursuerSteering = null!;
    private SteeringBehavior _evaderSteering = null!;
    private SteeringDebugDrawer _pursuerDebug = null!;
    private SteeringDebugDrawer _evaderDebug = null!;

    private float _evaderWander;
    private Font? _font;
    private double _fps;
    private double _updateMs;

    public PursueEvadeScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);

        _pursuer.Position = new Vector2f(300f, 360f);
        _pursuer.HeadingRef = new Vector2f(1f, 0f);
        _pursuer.Velocity = _pursuer.HeadingRef * 60f;

        _evader.Position = new Vector2f(980f, 360f);
        _evader.HeadingRef = new Vector2f(-1f, 0f);
        _evader.Velocity = _evader.HeadingRef * 60f;

        _pursuerSteering = new SteeringBehavior(_pursuer);
        _evaderSteering = new SteeringBehavior(_evader);
        _pursuerDebug = new SteeringDebugDrawer(_pursuer);
        _evaderDebug = new SteeringDebugDrawer(_evader);

        _font = host.Assets.LoadFont();
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        if (host.Input.WasKeyPressed(Keyboard.Key.Grave))
        {
            SteeringDebugDrawer.Enabled = !SteeringDebugDrawer.Enabled;
        }

        float dt = (float)gameTime.Delta.TotalSeconds;
        Vector2f windowSize = new(host.Window.Size.X, host.Window.Size.Y);

        // Pursuer: pursue evader
        Vector2f pursuerForce = _pursuerSteering.Pursue(_evader);
        _pursuer.Velocity = (_pursuer.Velocity + (pursuerForce * dt)).Truncate(_pursuer.MaxSpeed);
        _pursuer.HeadingRef = _pursuerSteering.UpdateHeadingWhileMoving(dt, ref _pursuer.RotationDegrees);
        _pursuer.Position = (_pursuer.Position + (_pursuer.Velocity * dt)).WrapPosition(windowSize);

        // Evader: evade pursuer, wander if far
        float pursuerDistance = (_evader.Position - _pursuer.Position).Length;
        Vector2f evaderForce = pursuerDistance < 400f
            ? _evaderSteering.Evade(_pursuer)
            : _evaderSteering.Wander(ref _evaderWander, 50f, 100f);

        _evader.Velocity = (_evader.Velocity + (evaderForce * dt)).Truncate(_evader.MaxSpeed);
        _evader.HeadingRef = _evaderSteering.UpdateHeadingWhileMoving(dt, ref _evader.RotationDegrees);
        _evader.Position = (_evader.Position + (_evader.Velocity * dt)).WrapPosition(windowSize);

        _fps = 1.0 / gameTime.Delta.TotalSeconds;
        _updateMs = gameTime.Delta.TotalMilliseconds;
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        _pursuerDebug.DrawPursue(window, _evader);
        _evaderDebug.DrawEvade(window, _pursuer);
        _pursuerDebug.DrawVelocityAndHeading(window);
        _evaderDebug.DrawVelocityAndHeading(window);

        _pursuer.Draw(window);
        _evader.Draw(window);

        if (_font is not null)
        {
            SteeringDebugDrawer.DrawStats(window, _font, _fps, _updateMs);
        }
    }
}
