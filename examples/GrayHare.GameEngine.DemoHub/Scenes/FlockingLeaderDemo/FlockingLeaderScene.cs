using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.FlockingLeaderDemo;

/// <summary>
/// Demonstrates a flock following a leader.
/// The leader wanders until the user left-clicks; it then travels to that position
/// using <see cref="SteeringBehavior.Arrive"/> and resumes wandering on arrival.
/// Each subsequent click sets a new destination.
/// Boids flock among themselves (Separation + Alignment + Cohesion) and are drawn
/// toward the leader with a low-priority <see cref="SteeringBehavior.Seek"/> force.
/// </summary>
internal sealed class FlockingLeaderScene : DemoSceneBase
{
    private const int BoidCount = 600;
    private const float NeighborhoodRadius = 110f;
    private const float SeparationRadius = 150f;
    private const float ArrivalRadius = 55f;
    private const float WanderRadius = 50f;

    // Leader state machine.
    private enum LeaderMode { Wander, SeekDestination }
    private LeaderMode _leaderMode = LeaderMode.Wander;
    private Vector2f _destination;
    private float _leaderWander;

    private readonly AutonomousAgent _leader = new FlockLeader();
    private SteeringBehavior _leaderSteering = null!;
    private SteeringDebugDrawer _leaderDebug = null!;

    private sealed class Boid
    {
        public Boid(AutonomousAgent agent)
        {
            Agent = agent;
            Steering = new SteeringBehavior(agent);
            Debug = new SteeringDebugDrawer(agent);
        }

        public AutonomousAgent Agent { get; }
        public SteeringBehavior Steering { get; }
        public SteeringDebugDrawer Debug { get; }
    }

    private readonly List<Boid> _boids = [];

    // Neighbor lists built once per Update and reused in Render.
    private List<IMovableGameObject>[] _neighborCache = [];

    private Font _font = null!;
    private double _fps;
    private double _updateMs;
    private bool _flee;

    public FlockingLeaderScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);

        float w = host.Window.Size.X;
        float h = host.Window.Size.Y;

        _leader.Position = new Vector2f(w / 2f, h / 2f);
        _leader.HeadingRef = new Vector2f(1f, 0f);
        _leader.Velocity = _leader.HeadingRef * 80f;
        _leaderSteering = new SteeringBehavior(_leader);
        _leaderMode = LeaderMode.Wander;
        _leaderDebug = new SteeringDebugDrawer(_leader);

        _boids.Clear();
        Random random = Random.Shared;

        for (int i = 0; i < BoidCount; i++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2.0);
            float speed = 200f + ((float)random.NextDouble() * 160f); // 200–360 px/s per boid
            LeaderFlockBoid agent = new(speed)
            {
                // Spawn boids in a cluster around the leader.
                Position = new Vector2f(
                    Math.Clamp((w / 2f) + ((float)(random.NextDouble() - 0.5) * 400f), 50f, w - 50f),
                    Math.Clamp((h / 2f) + ((float)(random.NextDouble() - 0.5) * 400f), 50f, h - 50f)),
                HeadingRef = new Vector2f(MathF.Cos(angle), MathF.Sin(angle)),
            };
            agent.Velocity = agent.HeadingRef * (speed * 0.5f);
            _boids.Add(new Boid(agent));
        }

        _neighborCache = new List<IMovableGameObject>[BoidCount];
        for (int i = 0; i < BoidCount; i++)
        {
            _neighborCache[i] = [];
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
        if (host.Input.WasKeyPressed(Keyboard.Key.Space))
        {
            _flee = !_flee;
        }

        // Each click gives the leader a new destination.
        if (host.Input.WasMouseButtonPressed(Mouse.Button.Left))
        {
            _destination = new Vector2f(host.Input.MousePosition.X, host.Input.MousePosition.Y);
            _leaderMode = LeaderMode.SeekDestination;
        }

        float deltaTime = gameTime.DeltaTotalSeconds;
        Vector2f windowSize = new(host.Window.Size.X, host.Window.Size.Y);
        FloatRect bounds = new(Constants.Vectors.Zero, windowSize);

        // ── Leader ──────────────────────────────────────────────────────────
        Vector2f leaderSteer = _leaderMode switch
        {
            LeaderMode.SeekDestination => _leaderSteering.Arrive(_destination, ArrivalRadius),
            _ => _leaderSteering.Wander(ref _leaderWander, WanderRadius, _leader.MaxSpeed * 0.8f),
        };

        // Check arrival: switch back to wander once close enough.
        if (_leaderMode == LeaderMode.SeekDestination &&
            (_leader.Position - _destination).Length < ArrivalRadius * 0.4f)
        {
            _leaderMode = LeaderMode.Wander;
        }

        // Bounds is weighted higher so it always overrides seeking/wandering near edges.
        Vector2f leaderBounds = _leaderSteering.StayWithinBounds(bounds, 80f);
        Vector2f leaderForce = SteeringForces.WeightedSum(
            _leader.MaxSpeed,
            (leaderSteer, 1f),   // seek destination or wander
            (leaderBounds, 2f));  // higher weight: keep leader on screen

        _leader.Velocity = (_leader.Velocity + (leaderForce * deltaTime)).Truncate(_leader.MaxSpeed);
        _leader.HeadingRef = _leaderSteering.UpdateHeadingWhileMoving(
            deltaTime, ref _leader.RotationDegrees);
        _leader.Position += _leader.Velocity * deltaTime;

        // ── Boids ────────────────────────────────────────────────────────────

        // Build neighbor lists from start-of-frame positions so update order
        // does not affect what each boid perceives.
        for (int i = 0; i < _boids.Count; i++)
        {
            _neighborCache[i].Clear();
            Vector2f pos = _boids[i].Agent.Position;

            foreach (Boid other in _boids)
            {
                if (other == _boids[i])
                {
                    continue;
                }

                if ((other.Agent.Position - pos).Length < NeighborhoodRadius)
                {
                    _neighborCache[i].Add(other.Agent);
                }
            }
        }

        for (int i = 0; i < _boids.Count; i++)
        {
            Boid boid = _boids[i];
            IReadOnlyList<IMovableGameObject> neighbors = _neighborCache[i];

            // Low-priority pull toward the leader; flocking forces keep them cohesive.
            Vector2f leaderSeek = Constants.Vectors.Zero;
            if (_flee)
            {
                leaderSeek = boid.Steering.Flee(_leader.Position);
            }
            else
            {
                leaderSeek = boid.Steering.Seek(_leader.Position);
            }

            Vector2f cohesionForce = boid.Steering.Cohesion(neighbors);
            Vector2f alignForce = boid.Steering.Alignment(neighbors);
            Vector2f separateForce = boid.Steering.Separation(neighbors, SeparationRadius);

            // Weighted blend: separation prevents overlap; social forces keep the flock cohesive.
            Vector2f force = SteeringForces.WeightedSum(
                boid.Agent.MaxSpeed,
                (separateForce, 4f),  // highest weight: personal space is non-negotiable
                (alignForce, 3f),
                (cohesionForce, 2f),
                (leaderSeek, 1f));    // lowest weight: gentle pull toward the leader

            boid.Agent.Velocity = (boid.Agent.Velocity + (force * deltaTime)).Truncate(boid.Agent.MaxSpeed);
            boid.Agent.HeadingRef = boid.Steering.UpdateHeadingWhileMoving(
                deltaTime, ref boid.Agent.RotationDegrees);
            boid.Agent.Position = (boid.Agent.Position + (boid.Agent.Velocity * deltaTime)).WrapPosition(windowSize);
        }

        _fps = 1.0 / gameTime.DeltaTotalSeconds;
        _updateMs = gameTime.Delta.TotalMilliseconds;
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // Destination marker when the leader is en route.
        if (_leaderMode == LeaderMode.SeekDestination)
        {
            _leaderDebug.DrawArrive(window, _destination, ArrivalRadius);

            using CircleShape ring = new(ArrivalRadius)
            {
                Origin = new Vector2f(ArrivalRadius, ArrivalRadius),
                Position = _destination,
                FillColor = Color.Transparent,
                OutlineColor = new Color(255, 220, 50),
                OutlineThickness = 2f
            };
            window.Draw(ring);

            using CircleShape dot = new(5f)
            {
                Origin = new Vector2f(5f, 5f),
                Position = _destination,
                FillColor = new Color(255, 220, 50)
            };
            window.Draw(dot);
        }
        else
        {
            _leaderDebug.DrawWander(window, _leaderWander, WanderRadius, _leader.MaxSpeed * 0.8f);
        }

        for (int i = 0; i < _boids.Count; i++)
        {
            Boid boid = _boids[i];
            IReadOnlyList<IMovableGameObject> neighbors = _neighborCache[i];

            boid.Debug.DrawVelocityAndHeading(window);
            boid.Agent.Draw(window);
        }

        _leader.Draw(window);

        SteeringDebugDrawer.DrawStats(window, _font, _fps, _updateMs);
    }
}
