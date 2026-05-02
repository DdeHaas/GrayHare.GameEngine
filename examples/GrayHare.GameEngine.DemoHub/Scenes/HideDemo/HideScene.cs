using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.DemoHub.Scenes.AvoidanceDemo;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.HideDemo;

/// <summary>
/// Demonstrates <see cref="SteeringBehavior.Hide"/>.
/// Several purple hiders hide behind rock obstacles to escape an orange threat.
/// Falls back to <see cref="SteeringBehavior.Evade"/> when no cover is available.
/// </summary>
internal sealed class HideScene : DemoSceneBase
{
    private readonly AutonomousAgent _threat = new ThreatShip();
    private SteeringBehavior _threatSteering = null!;
    private SteeringDebugDrawer _threatDebug = null!;
    private readonly List<ObstacleCircle> _rocks = [];
    private float _threatWander;
    private Font _font = null!;
    private double _fps;
    private double _updateMs;

    // Each hider bundles its agent, steering, and debug drawer together.
    private sealed class Hider
    {
        public Hider(AutonomousAgent agent)
        {
            ArgumentNullException.ThrowIfNull(agent);

            Agent = agent;
            Steering = new SteeringBehavior(agent);
            Debug = new SteeringDebugDrawer(agent);
        }

        public AutonomousAgent Agent { get; }
        public SteeringBehavior Steering { get; }
        public SteeringDebugDrawer Debug { get; }
    }

    private readonly List<Hider> _hiders = [];

    private const float HideDistance = 30f;

    // Covers the full 1280×720 window diagonal (~1464 px) so the hider always feels
    // threatened and continuously seeks cover regardless of the threat's position.
    private const float ThreatRadius = 1500f;
    private const float ThreatWanderRadius = 40f;
    private const float ThreatWanderDistance = 80f;

    private const float DetectionLength = 100f;
    private const float AgentRadius = 14f;

    public HideScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);

        _hiders.Clear();
        _hiders.AddRange(
        [
            new Hider(new HiderShip(new Color(180,  80, 255)) { Position = new Vector2f(640f, 360f), HeadingRef = new Vector2f( 0f, -1f) }),
            new Hider(new HiderShip(new Color(100, 160, 255)) { Position = new Vector2f(400f, 500f), HeadingRef = new Vector2f( 1f,  0f) }),
            new Hider(new HiderShip(new Color(80,  220, 180)) { Position = new Vector2f(800f, 420f), HeadingRef = new Vector2f(-1f,  0f) }),
            new Hider(new HiderShip(new Color(240, 120, 180)) { Position = new Vector2f(550f, 200f), HeadingRef = new Vector2f( 0f,  1f) }),
        ]);

        _threat.Position = new Vector2f(200f, 180f);
        _threat.HeadingRef = new Vector2f(1f, 0f);
        _threat.Velocity = _threat.HeadingRef * 40f;

        _threatSteering = new SteeringBehavior(_threat);
        _threatDebug = new SteeringDebugDrawer(_threat);

        _rocks.Clear();
        _rocks.AddRange(
        [
            new ObstacleCircle(new Vector2f(350f, 250f), 48f),
            new ObstacleCircle(new Vector2f(900f, 200f), 42f),
            new ObstacleCircle(new Vector2f(600f, 520f), 50f),
            new ObstacleCircle(new Vector2f(1050f, 500f), 36f),
            new ObstacleCircle(new Vector2f(250f, 560f), 40f),
        ]);

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
        Vector2f windowSize = new(host.Window.Size.X, host.Window.Size.Y);

        // Threat wanders and avoids obstacles; obstacle avoidance has highest priority.
        Vector2f wanderForce = _threatSteering.Wander(ref _threatWander, ThreatWanderRadius, ThreatWanderDistance);
        Vector2f threatObstacleForce = _threatSteering.ObstacleAvoidance(_rocks, DetectionLength, AgentRadius);
        // Obstacle avoidance is weighted higher so it can overcome wander even when
        // wander happens to point toward a rock.
        Vector2f threatForce = SteeringForces.WeightedSum(
            _threat.MaxSpeed,
            (wanderForce, 1f),   // background roaming
            (threatObstacleForce, 2f)); // higher weight: obstacle avoidance dominates
        _threat.Velocity = (_threat.Velocity + (threatForce * deltaTime)).Truncate(_threat.MaxSpeed);
        _threat.HeadingRef = _threatSteering.UpdateHeadingWhileMoving(deltaTime, ref _threat.RotationDegrees);
        _threat.Position = (_threat.Position + (_threat.Velocity * deltaTime)).WrapPosition(windowSize);

        // Each hider independently hides and avoids obstacles
        foreach (Hider hider in _hiders)
        {
            Vector2f hideForce = hider.Steering.Hide(_threat, _rocks, HideDistance, ThreatRadius);
            Vector2f obstacleForce = hider.Steering.ObstacleAvoidance(_rocks, DetectionLength, AgentRadius);
            // Obstacle avoidance is weighted higher so it dominates over the hiding goal
            // when both forces are active simultaneously.
            Vector2f hiderForce = SteeringForces.WeightedSum(
                hider.Agent.MaxSpeed,
                (hideForce, 1f),   // seek cover
                (obstacleForce, 2f));  // higher weight: don't clip through rocks
            hider.Agent.Velocity = (hider.Agent.Velocity + (hiderForce * deltaTime)).Truncate(hider.Agent.MaxSpeed);
            hider.Agent.HeadingRef = hider.Steering.UpdateHeadingWhileMoving(deltaTime, ref hider.Agent.RotationDegrees);
            hider.Agent.Position = (hider.Agent.Position + (hider.Agent.Velocity * deltaTime)).WrapPosition(windowSize);
        }

        _fps = 1.0 / gameTime.DeltaTotalSeconds;
        _updateMs = gameTime.Delta.TotalMilliseconds;
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        foreach (ObstacleCircle rock in _rocks)
        {
            rock.Draw(window);
        }

        foreach (Hider hider in _hiders)
        {
            hider.Debug.DrawHide(window, _threat, _rocks, HideDistance, ThreatRadius);
            hider.Debug.DrawObstacleAvoidance(window, _rocks, DetectionLength, AgentRadius);
            hider.Debug.DrawVelocityAndHeading(window);
            hider.Agent.Draw(window);
        }

        _threatDebug.DrawObstacleAvoidance(window, _rocks, DetectionLength, AgentRadius);
        _threatDebug.DrawWander(window, _threatWander, ThreatWanderRadius, ThreatWanderDistance);
        _threat.Draw(window);

        SteeringDebugDrawer.DrawStats(window, _font, _fps, _updateMs);
    }
}
