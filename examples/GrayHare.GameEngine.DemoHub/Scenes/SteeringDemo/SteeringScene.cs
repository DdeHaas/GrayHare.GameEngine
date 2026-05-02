using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.SteeringDemo;

/// <summary>
/// Steering demo: an autonomous agent wanders and stays within window bounds.
/// Press ` (Grave) to toggle the <see cref="SteeringDebugDrawer"/> overlay.
/// </summary>
internal sealed class SteeringScene : DemoSceneBase
{
    private readonly SteeringAgent _agent = new();
    private readonly SteeringBehavior _steering;
    private readonly SteeringDebugDrawer _debug;
    private Font _font = null!;
    private float _wanderAngle;
    private double _fps;
    private double _updateMs;

    public SteeringScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex)
    {
        _steering = new SteeringBehavior(_agent);
        _debug = new SteeringDebugDrawer(_agent);
    }

    public override void Load(GameHost host)
    {
        base.Load(host);

        _agent.Position = new Vector2f(640f, 360f);
        _agent.HeadingRef = new Vector2f(1f, 0f);
        _agent.Velocity = _agent.HeadingRef * 50f;
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

        float deltaTime = gameTime.DeltaTotalSeconds;

        Vector2f wanderForce = _steering.Wander(ref _wanderAngle, 60f, 120f);
        FloatRect bounds = new(new Vector2f(0f, 0f), new Vector2f(host.Window.Size.X, host.Window.Size.Y));
        Vector2f boundsForce = _steering.StayWithinBounds(bounds, 80f);

        // Bounds enforcement is weighted higher so it dominates when the agent nears an edge,
        // even if the wander force happens to point outward.
        Vector2f steeringForce = SteeringForces.WeightedSum(
            _agent.MaxSpeed,
            (wanderForce, 1f),   // background tendency to keep moving
            (boundsForce, 2f));  // higher weight: bounds always win near edges

        _agent.Velocity += steeringForce * deltaTime;
        _agent.Velocity = _agent.Velocity.Truncate(_agent.MaxSpeed);
        _agent.HeadingRef = _steering.UpdateHeadingWhileMoving(deltaTime, ref _agent.RotationDegrees);

        _agent.Position += _agent.Velocity * deltaTime;

        _fps = 1.0 / gameTime.DeltaTotalSeconds;
        _updateMs = gameTime.Delta.TotalMilliseconds;
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        _agent.Draw(window);
        _debug.DrawWander(window, _wanderAngle, 60f, 120f);
        _debug.DrawVelocityAndHeading(window);

        SteeringDebugDrawer.DrawStats(window, _font, _fps, _updateMs);
    }
}
