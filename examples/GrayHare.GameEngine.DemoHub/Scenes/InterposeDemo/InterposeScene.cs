using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.InterposeDemo;

/// <summary>
/// Demonstrates <see cref="SteeringBehavior.Interpose"/>.
/// A cyan interposer agent always moves to sit between the two wandering target agents.
/// </summary>
internal sealed class InterposeScene : DemoSceneBase
{
    private readonly AutonomousAgent _target1 = new TargetShip(new Color(255, 200, 60));
    private readonly AutonomousAgent _target2 = new TargetShip(new Color(60, 200, 255));
    private readonly AutonomousAgent _interposer = new InterposeShip();
    private SteeringBehavior _interposerSteering = null!;
    private SteeringBehavior _target1Steering = null!;
    private SteeringBehavior _target2Steering = null!;
    private SteeringDebugDrawer _interposerDebug = null!;
    private float _t1Wander;
    private float _t2Wander;
    private Font _font = null!;
    private double _fps;
    private double _updateMs;

    public InterposeScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);

        _target1.Position = new Vector2f(300f, 250f);
        _target1.HeadingRef = new Vector2f(1f, 0.3f).Normalized();
        _target1.Velocity = _target1.HeadingRef * 60f;

        _target2.Position = new Vector2f(980f, 470f);
        _target2.HeadingRef = new Vector2f(-1f, -0.2f).Normalized();
        _target2.Velocity = _target2.HeadingRef * 60f;

        _interposer.Position = new Vector2f(640f, 360f);
        _interposer.HeadingRef = new Vector2f(0f, -1f);
        _interposer.Velocity = Constants.Vectors.Zero;

        _target1Steering = new SteeringBehavior(_target1);
        _target2Steering = new SteeringBehavior(_target2);
        _interposerSteering = new SteeringBehavior(_interposer);
        _interposerDebug = new SteeringDebugDrawer(_interposer);

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
        Vector2f size = new(host.Window.Size.X, host.Window.Size.Y);

        // Targets wander
        _target1.Velocity = (_target1.Velocity + (_target1Steering.Wander(ref _t1Wander, 40f, 80f) * deltaTime)).Truncate(_target1.MaxSpeed);
        _target1.HeadingRef = _target1Steering.UpdateHeadingWhileMoving(deltaTime, ref _target1.RotationDegrees);
        _target1.Position = (_target1.Position + (_target1.Velocity * deltaTime)).WrapPosition(size);

        _target2.Velocity = (_target2.Velocity + (_target2Steering.Wander(ref _t2Wander, 40f, 80f) * deltaTime)).Truncate(_target2.MaxSpeed);
        _target2.HeadingRef = _target2Steering.UpdateHeadingWhileMoving(deltaTime, ref _target2.RotationDegrees);
        _target2.Position = (_target2.Position + (_target2.Velocity * deltaTime)).WrapPosition(size);

        // Interposer
        Vector2f force = _interposerSteering.Interpose(_target1, _target2);
        _interposer.Velocity = (_interposer.Velocity + (force * deltaTime)).Truncate(_interposer.MaxSpeed);
        _interposer.HeadingRef = _interposerSteering.UpdateHeadingWhileMoving(deltaTime, ref _interposer.RotationDegrees);
        _interposer.Position = (_interposer.Position + (_interposer.Velocity * deltaTime)).WrapPosition(size);

        _fps = 1.0 / gameTime.DeltaTotalSeconds;
        _updateMs = gameTime.Delta.TotalMilliseconds;
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        _interposerDebug.DrawInterpose(window, _target1, _target2);
        _interposerDebug.DrawVelocityAndHeading(window);

        _target1.Draw(window);
        _target2.Draw(window);
        _interposer.Draw(window);

        // Connecting line between targets
        using VertexArray line = new(PrimitiveType.Lines, 2);
        line[0] = new Vertex(_target1.Position, new Color(180, 180, 60, 120));
        line[1] = new Vertex(_target2.Position, new Color(60, 180, 220, 120));
        window.Draw(line);

        SteeringDebugDrawer.DrawStats(window, _font, _fps, _updateMs);
    }
}
