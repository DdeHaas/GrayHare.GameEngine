using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.MovementDemo;

/// <summary>Demonstrates <see cref="MovementWithRotationBehavior"/> (no drift).</summary>
internal sealed class MovementScene : DemoSceneBase
{
    private readonly ShipAgent _ship = new();
    private readonly SteeringDebugDrawer _debug;
    private Vector2f _velocity;

    public MovementScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex)
    {
        _debug = new SteeringDebugDrawer(_ship);
    }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _ship.Position = new Vector2f(640f, 360f);
        _ship.RotationDegrees = 0f;
        _ship.HeadingRef = _ship.RotationDegrees.ToVector2f().Normalized();
        _velocity = Constants.Vectors.Zero;
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        if (host.Input.WasKeyPressed(Keyboard.Key.Grave))
        {
            SteeringDebugDrawer.Enabled = !SteeringDebugDrawer.Enabled;
        }

        float dt = (float)gameTime.Delta.TotalSeconds;
        _ship.Movement.IsMovingForwards = host.Input.IsKeyDown(Keyboard.Key.W);
        _ship.Movement.IsBraking = host.Input.IsKeyDown(Keyboard.Key.S);
        _ship.Movement.IsTurningLeft = host.Input.IsKeyDown(Keyboard.Key.A);
        _ship.Movement.IsTurningRight = host.Input.IsKeyDown(Keyboard.Key.D);

        _velocity = _ship.Movement.Update(dt, ref _ship.RotationDegrees, ref _ship.HeadingRef);
        _ship.Velocity = _velocity;
        _ship.Position = (_ship.Position + (_velocity * dt)).WrapPosition(host.Window.Size);
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        _ship.Draw(window);
        _debug.DrawVelocityAndHeading(window);
    }
}
