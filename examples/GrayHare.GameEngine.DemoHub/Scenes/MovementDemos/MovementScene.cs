using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Extensions;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.MovementDemos;

internal sealed class MovementScene : MovementSceneBase
{
    public MovementScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex)
    {
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        float deltaTime = gameTime.DeltaTotalSeconds;
        _ship.Movement.IsMovingForwards = host.Input.IsKeyDown(Keyboard.Key.W);
        _ship.Movement.IsBraking = host.Input.IsKeyDown(Keyboard.Key.S);
        _ship.Movement.IsTurningLeft = host.Input.IsKeyDown(Keyboard.Key.A);
        _ship.Movement.IsTurningRight = host.Input.IsKeyDown(Keyboard.Key.D);

        _velocity = _ship.Movement.Update(deltaTime, ref _ship.RotationDegrees, ref _ship.HeadingRef);
        _ship.Velocity = _velocity;
        _ship.Position = (_ship.Position + (_velocity * deltaTime)).WrapPosition(host.Window.Size);
    }
}
