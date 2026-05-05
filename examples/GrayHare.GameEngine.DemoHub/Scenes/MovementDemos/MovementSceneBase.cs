using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.MovementDemos;

internal abstract class MovementSceneBase : DemoSceneBase
{
    private readonly SteeringDebugDrawer _debug;
    private readonly StarfieldLayer _starfield = new(500);
    private readonly NebulaLayer _nebula = new(5);

    protected readonly ShipAgent _ship = new();
    protected Vector2f _velocity;

    protected MovementSceneBase(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex)
    {
        _debug = new SteeringDebugDrawer(_ship);
        AddLayer(_starfield);
        AddLayer(_nebula);
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
        base.Update(host, gameTime);

        if (host.Input.WasKeyPressed(Keyboard.Key.Grave))
        {
            SteeringDebugDrawer.Enabled = !SteeringDebugDrawer.Enabled;
        }

        if (_ship.Speed > 0)
        {
            float deltaTime = gameTime.DeltaTotalSeconds;

            float frameScrollSpeed = _velocity.Length * deltaTime;
            Vector2f scrollDirection = _velocity.Normalized() * -1f;

            var delta = scrollDirection * frameScrollSpeed;
            _starfield.Move(delta);
            _nebula.Move(delta);
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        _ship.Draw(window);
        _debug.DrawVelocityAndHeading(window);
    }
}
