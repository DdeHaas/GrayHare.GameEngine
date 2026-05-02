using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.StrafingDemo;

/// <summary>
/// Demonstrates <see cref="MovementBehavior.IsStrafingLeft"/>,
/// <see cref="MovementBehavior.IsStrafingRight"/>, and standalone
/// <see cref="RotationBehavior.UpdateRotation"/> (decoupled from movement).
/// Mouse horizontal movement also turns the ship.
/// </summary>
internal sealed class StrafingScene : DemoSceneBase
{
    /// <summary>Degrees of rotation applied per pixel of horizontal mouse movement.</summary>
    private const float MouseSensitivity = 0.25f;

    private readonly ShipAgent _ship = new();
    private readonly MovementBehavior _movement;
    private readonly SteeringDebugDrawer _debug;
    private Vector2f _velocity;
    private Font _font = null!;
    private int _previousMouseX;

    public StrafingScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex)
    {
        _movement = new MovementBehavior(_ship);
        _debug = new SteeringDebugDrawer(_ship);
    }

    public override void Load(GameHost host)
    {
        base.Load(host);

        _font = host.Assets.LoadFont();
        _ship.Position = new Vector2f(640f, 360f);
        _ship.RotationDegrees = 0f;
        _ship.HeadingRef = _ship.RotationDegrees.ToVector2f().Normalized();
        _velocity = Constants.Vectors.Zero;
        _previousMouseX = host.Input.MousePosition.X;
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        if (host.Input.WasKeyPressed(Keyboard.Key.Grave))
        {
            SteeringDebugDrawer.Enabled = !SteeringDebugDrawer.Enabled;
        }

        float deltaTime = gameTime.DeltaTotalSeconds;

        // Left/Right arrows or mouse horizontal delta: rotate heading via standalone RotationBehavior.
        int currentMouseX = host.Input.MousePosition.X;
        int mouseDeltaX = currentMouseX - _previousMouseX;
        _previousMouseX = currentMouseX;

        _ship.Rotation.IsTurningLeft = host.Input.IsKeyDown(Keyboard.Key.Left);
        _ship.Rotation.IsTurningRight = host.Input.IsKeyDown(Keyboard.Key.Right);
        float newRotation = _ship.Rotation.UpdateRotation(deltaTime, ref _ship.HeadingRef);

        // Apply mouse delta on top of the keyboard-driven rotation.
        if (mouseDeltaX != 0)
        {
            newRotation += mouseDeltaX * MouseSensitivity;
            _ship.HeadingRef = newRotation.ToVector2f().Normalized();
        }

        _ship.RotationDegrees = newRotation;

        // W/S: move forwards/backwards along heading, independent of rotation.
        _movement.IsMovingForwards = host.Input.IsKeyDown(Keyboard.Key.W);
        _movement.IsMovingBackwards = host.Input.IsKeyDown(Keyboard.Key.S);

        // A/D: strafe perpendicular to heading.
        _movement.IsStrafingLeft = host.Input.IsKeyDown(Keyboard.Key.A);
        _movement.IsStrafingRight = host.Input.IsKeyDown(Keyboard.Key.D);

        _velocity = _movement.UpdateMovement(deltaTime, _velocity);
        _ship.Velocity = _velocity;
        _ship.Position = (_ship.Position + _velocity * deltaTime).WrapPosition(host.Window.Size);
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        _ship.Draw(window);
        _debug.DrawVelocityAndHeading(window);

        using Text hint = new(_font,
            "W/S forward/backward  ·  A/D  strafe left/right\n" +
            "←/→ or mouse rotate heading  ·  `  toggle debug overlay", 18)
        {
            Position = new Vector2f(20f, 20f),
            FillColor = new Color(220, 230, 255)
        };
        window.Draw(hint);
    }
}
