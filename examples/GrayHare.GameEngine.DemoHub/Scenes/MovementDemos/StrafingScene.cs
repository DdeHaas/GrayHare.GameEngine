using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Extensions;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.MovementDemos;

internal sealed class StrafingScene : MovementSceneBase
{
    /// <summary>Degrees of rotation applied per pixel of horizontal mouse movement.</summary>
    private const float MouseSensitivity = 0.25f;
    private int _previousMouseX;

    public StrafingScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex)
    {
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

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
        _ship.Strafing.IsMovingForwards = host.Input.IsKeyDown(Keyboard.Key.W);
        _ship.Strafing.IsMovingBackwards = host.Input.IsKeyDown(Keyboard.Key.S);

        // A/D: strafe perpendicular to heading.
        _ship.Strafing.IsStrafingLeft = host.Input.IsKeyDown(Keyboard.Key.A);
        _ship.Strafing.IsStrafingRight = host.Input.IsKeyDown(Keyboard.Key.D);

        _velocity = _ship.Strafing.UpdateMovement(deltaTime, _velocity);
        _ship.Velocity = _velocity;
        _ship.Position = (_ship.Position + _velocity * deltaTime).WrapPosition(host.Window.Size);
    }
}
