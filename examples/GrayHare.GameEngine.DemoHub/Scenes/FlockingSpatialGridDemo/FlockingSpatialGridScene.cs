using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Extensions;
using GrayHare.GameEngine.Spatial;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.FlockingSpatialGridDemo;

/// <summary>
/// Demonstrates <see cref="SpatialGrid{T}"/> for efficient neighbor lookups combined
/// with <see cref="SteeringDebugDrawer"/> visualization. A swarm of agents flock using
/// separation, alignment, and cohesion — but neighbor queries go through the spatial
/// grid instead of a brute-force O(n²) scan.
/// <para>
/// Press <c>`</c> (Grave) to toggle the debug overlay which renders the spatial grid
/// cells, neighbor connections, and steering forces.
/// </para>
/// </summary>
internal sealed class FlockingSpatialGridScene : DemoSceneBase
{
    private const int AgentCount = 150;
    private const float CellSize = 120f;
    private const float NeighborhoodRadius = 110f;
    private const float SeparationRadius = 75f;
    private const float WanderRadius = 25f;
    private const float WanderDistance = 60f;

    /// <summary>Bundles an agent with its steering behavior, debug drawer, and per-frame state.</summary>
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
        public float WanderAngle;
    }

    private readonly SpatialGrid<AutonomousAgent> _grid = new(CellSize);
    private readonly List<Boid> _boids = [];

    // Reusable buffers to avoid per-frame allocations.
    private readonly List<AutonomousAgent> _neighborBuffer = [];
    private List<IMovableGameObject>[] _neighborCache = [];

    private Font _font = null!;
    private double _fps;
    private double _updateMs;

    public FlockingSpatialGridScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);

        _boids.Clear();
        Random random = Random.Shared;
        float w = host.Window.Size.X;
        float h = host.Window.Size.Y;

        for (int i = 0; i < AgentCount; i++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2.0);
            GridAgent agent = new()
            {
                Position = new Vector2f(
                    (float)(random.NextDouble() * w),
                    (float)(random.NextDouble() * h)),
                HeadingRef = new Vector2f(MathF.Cos(angle), MathF.Sin(angle)),
            };
            agent.Velocity = agent.HeadingRef * 140f;

            _boids.Add(new Boid(agent) { WanderAngle = angle });
        }

        _neighborCache = new List<IMovableGameObject>[AgentCount];
        for (int i = 0; i < AgentCount; i++)
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

        float deltaTime = gameTime.DeltaTotalSeconds;
        Vector2f windowSize = new(host.Window.Size.X, host.Window.Size.Y);
        FloatRect bounds = new(new Vector2f(0f, 0f), windowSize);

        // Rebuild the spatial grid from scratch each frame so cell assignments
        // match current positions rather than stale data from the previous frame.
        _grid.Clear();
        for (int i = 0; i < _boids.Count; i++)
        {
            _grid.Add(_boids[i].Agent, _boids[i].Agent.Position);
        }

        // Query neighbors via the spatial grid and cache for Render.
        for (int i = 0; i < _boids.Count; i++)
        {
            Boid boid = _boids[i];

            _grid.FindNeighbors(
                boid.Agent.Position,
                NeighborhoodRadius,
                _neighborBuffer,
                exclude: boid.Agent);

            _neighborCache[i].Clear();
            for (int n = 0; n < _neighborBuffer.Count; n++)
            {
                _neighborCache[i].Add(_neighborBuffer[n]);
            }
        }

        // Apply steering forces.
        for (int i = 0; i < _boids.Count; i++)
        {
            Boid boid = _boids[i];
            IReadOnlyList<IMovableGameObject> neighbors = _neighborCache[i];

            Vector2f wanderForce = boid.Steering.Wander(ref boid.WanderAngle, WanderRadius, WanderDistance);
            Vector2f separateForce = boid.Steering.Separation(neighbors, SeparationRadius);
            Vector2f alignForce = boid.Steering.Alignment(neighbors);
            Vector2f cohesionForce = boid.Steering.Cohesion(neighbors);

            Vector2f force = SteeringForces.WeightedSum(
                boid.Agent.MaxSpeed,
                (separateForce, 4f),
                (alignForce, 3f),
                (cohesionForce, 2f),
                (wanderForce, 1f));

            boid.Agent.Velocity = (boid.Agent.Velocity + (force * deltaTime)).Truncate(boid.Agent.MaxSpeed);
            boid.Agent.HeadingRef = boid.Steering.UpdateHeadingWhileMoving(deltaTime, ref boid.Agent.RotationDegrees);
            boid.Agent.Position = (boid.Agent.Position + (boid.Agent.Velocity * deltaTime)).WrapPosition(windowSize);
        }

        _fps = 1.0 / gameTime.DeltaTotalSeconds;
        _updateMs = gameTime.Delta.TotalMilliseconds;
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // Draw the spatial grid cells when debug is enabled.
        if (SteeringDebugDrawer.Enabled)
        {
            SteeringDebugDrawer.DrawSpatialGrid(window, _grid, _font);
        }

        for (int i = 0; i < _boids.Count; i++)
        {
            Boid boid = _boids[i];
            boid.Agent.Draw(window);
        }

        SteeringDebugDrawer.DrawStats(window, _font, _fps, _updateMs);
    }

    public override void Unload(GameHost host)
    {
        base.Unload(host);

        foreach (Boid boid in _boids)
        {
            boid.Debug.Dispose();
            boid.Agent.Dispose();
        }
    }
}
