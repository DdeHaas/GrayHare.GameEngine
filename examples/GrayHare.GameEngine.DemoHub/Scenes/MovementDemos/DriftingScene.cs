using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Extensions;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.MovementDemos;

/// <summary>Demonstrates <see cref="MovementWithDriftingBehavior"/> (momentum/drift).</summary>
internal sealed class DriftingScene : MovementSceneBase
{
    public DriftingScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex)
    {
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        float deltaTime = gameTime.DeltaTotalSeconds;
        _ship.Drifting.IsMovingForwards = host.Input.IsKeyDown(Keyboard.Key.W);
        _ship.Drifting.IsBraking = host.Input.IsKeyDown(Keyboard.Key.S);
        _ship.Drifting.IsTurningLeft = host.Input.IsKeyDown(Keyboard.Key.A);
        _ship.Drifting.IsTurningRight = host.Input.IsKeyDown(Keyboard.Key.D);

        _velocity = _ship.Drifting.Update(deltaTime, ref _ship.RotationDegrees, ref _ship.HeadingRef);
        _ship.Velocity = _velocity;
        _ship.Position = (_ship.Position + (_velocity * deltaTime)).WrapPosition(host.Window.Size);
    }
}
