using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.OffsetPursuitDemo;

/// <summary>
/// Demonstrates <see cref="SteeringBehavior.OffsetPursuit"/>.
/// A white leader wanders the screen while five coloured followers each maintain
/// a fixed slot in a V-formation relative to the leader's heading.
/// </summary>
internal sealed class OffsetPursuitScene : DemoSceneBase
{
    // Slowing radius for smooth formation-slot arrival.
    private const float SlowingRadius = 24f;
    private const float WanderRadius = 50f;
    private const float WanderDistance = 100f;

    // Formation offsets in leader-local space (X = forward, Y = right).
    // Negative X places followers behind the leader; Y spreads them laterally.
    private static readonly Vector2f[] _formationOffsets =
    [
        new(-70f, -50f),   // left inner wing
        new(-70f,  50f),   // right inner wing
        new(-140f, -100f), // left outer wing
        new(-140f,  100f), // right outer wing
        new(-200f,   0f),  // rear center
    ];

    private static readonly Color[] _followerColors =
    [
        new Color(100, 180, 255), // blue
        new Color(100, 255, 160), // green
        new Color(255, 200,  60), // yellow
        new Color(255, 100, 160), // pink
        new Color(180, 120, 255), // purple
    ];

    private readonly AutonomousAgent _leader = new LeaderShip();
    private SteeringDebugDrawer _leaderDebug = null!;

    private SteeringBehavior _leaderSteering = null!;
    private float _leaderWander;

    private sealed class Follower
    {
        public Follower(AutonomousAgent agent, Vector2f offset)
        {
            Agent = agent;
            Offset = offset;
            Steering = new SteeringBehavior(agent);
            Debug = new SteeringDebugDrawer(agent);
        }

        public AutonomousAgent Agent { get; }
        public Vector2f Offset { get; }
        public SteeringBehavior Steering { get; }
        public SteeringDebugDrawer Debug { get; }
    }

    private readonly List<Follower> _followers = [];
    private Font? _font;
    private double _fps;
    private double _updateMs;

    public OffsetPursuitScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);

        _leader.Position = new Vector2f(640f, 360f);
        _leader.HeadingRef = new Vector2f(1f, 0f);
        _leader.Velocity = _leader.HeadingRef * 80f;
        _leaderSteering = new SteeringBehavior(_leader);
        _leaderDebug = new SteeringDebugDrawer(_leader);

        _followers.Clear();
        for (int i = 0; i < _formationOffsets.Length; i++)
        {
            FollowerShip agent = new(_followerColors[i]);

            // Stagger initial positions so they don't all pile up at the origin.
            agent.Position = _leader.Position - (_leader.HeadingRef * (80f + (i * 30f)));
            agent.HeadingRef = _leader.HeadingRef;
            agent.Velocity = _leader.HeadingRef * _leader.MaxSpeed;

            _followers.Add(new Follower(agent, _formationOffsets[i]));
        }

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

        // Leader wanders and wraps around the window edges.
        Vector2f leaderForce = _leaderSteering.Wander(ref _leaderWander, WanderRadius, WanderDistance);
        _leader.Velocity = (_leader.Velocity + (leaderForce * dt)).Truncate(_leader.MaxSpeed);
        _leader.HeadingRef = _leaderSteering.UpdateHeadingWhileMoving(dt, ref _leader.RotationDegrees);
        _leader.Position = (_leader.Position + (_leader.Velocity * dt)).WrapPosition(windowSize);

        // Each follower pursues its assigned formation slot.
        foreach (Follower follower in _followers)
        {
            Vector2f force = follower.Steering.OffsetPursuit(_leader, follower.Offset, SlowingRadius);
            follower.Agent.Velocity = (follower.Agent.Velocity + (force * dt)).Truncate(follower.Agent.MaxSpeed);
            follower.Agent.HeadingRef = follower.Steering.UpdateHeadingWhileMoving(dt, ref follower.Agent.RotationDegrees);
            follower.Agent.Position = (follower.Agent.Position + (follower.Agent.Velocity * dt)).WrapPosition(windowSize);
        }

        _fps = 1.0 / gameTime.Delta.TotalSeconds;
        _updateMs = gameTime.Delta.TotalMilliseconds;
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // Draw followers behind the leader.
        foreach (Follower follower in _followers)
        {
            follower.Debug.DrawOffsetPursuit(window, _leader, follower.Offset, SlowingRadius);
            follower.Debug.DrawVelocityAndHeading(window);
            follower.Agent.Draw(window);
        }

        _leaderDebug.DrawWander(window, _leaderWander, WanderRadius, WanderDistance);
        _leader.Draw(window);

        if (_font is not null)
        {
            SteeringDebugDrawer.DrawStats(window, _font, _fps, _updateMs);
        }
    }
}
