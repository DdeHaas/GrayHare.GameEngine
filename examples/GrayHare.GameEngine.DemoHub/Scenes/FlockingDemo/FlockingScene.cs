using GrayHare.GameEngine.Abstractions;
using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.FlockingDemo;

/// <summary>
/// Demonstrates group flocking behavior using three steering forces operating
/// over a shared neighborhood:
/// <list type="bullet">
///   <item><see cref="SteeringBehavior.Separation"/> – avoid crowding flockmates</item>
///   <item><see cref="SteeringBehavior.Alignment"/>  – match the average heading</item>
///   <item><see cref="SteeringBehavior.Cohesion"/>   – steer toward the center of mass</item>
/// </list>
/// A mild wander force keeps the flock moving when isolated.
/// </summary>
internal sealed class FlockingScene : DemoSceneBase
{
    private const int BoidCount = 115;
    private const float NeighborhoodRadius = 110f;
    private const float SeparationRadius = 100f;
    private const float WanderRadius = 30f;
    private const float WanderDistance = 70f;

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

    private readonly List<Boid> _boids = [];

    // Neighbor lists populated each Update and reused in Render for debug drawing.
    private List<IMovableGameObject>[] _neighborCache = [];

    private Font? _font;
    private double _fps;
    private double _updateMs;

    public FlockingScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);

        _boids.Clear();
        Random random = Random.Shared;
        float w = host.Window.Size.X;
        float h = host.Window.Size.Y;

        for (int i = 0; i < BoidCount; i++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2.0);
            FlockBoid agent = new()
            {
                Position = new Vector2f(
                    (float)(random.NextDouble() * w),
                    (float)(random.NextDouble() * h)),
                HeadingRef = new Vector2f(MathF.Cos(angle), MathF.Sin(angle)),
            };
            agent.Velocity = agent.HeadingRef * 160f;

            _boids.Add(new Boid(agent) { WanderAngle = angle });
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

        float dt = (float)gameTime.Delta.TotalSeconds;
        Vector2f windowSize = new(host.Window.Size.X, host.Window.Size.Y);

        // Build neighbor lists from positions at the START of this frame so all
        // boids perceive the same world state regardless of update order.
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
            Boid b = _boids[i];
            IReadOnlyList<IMovableGameObject> neighbors = _neighborCache[i];

            Vector2f wanderForce = b.Steering.Wander(ref b.WanderAngle, WanderRadius, WanderDistance);
            Vector2f cohesionForce = b.Steering.Cohesion(neighbors);
            Vector2f alignForce = b.Steering.Alignment(neighbors);
            Vector2f separateForce = b.Steering.Separation(neighbors, SeparationRadius);

            // Weighted blend: separation prevents overlap, alignment and cohesion maintain flock structure.
            Vector2f force = SteeringForces.WeightedSum(
                b.Agent.MaxSpeed,
                (separateForce, 4f),   // highest weight: personal space is non-negotiable
                (alignForce, 3f),
                (cohesionForce, 2f),
                (wanderForce, 1f));     // lowest weight: gentle tendency to keep moving

            b.Agent.Velocity = (b.Agent.Velocity + (force * dt)).Truncate(b.Agent.MaxSpeed);
            b.Agent.HeadingRef = b.Steering.UpdateHeadingWhileMoving(dt, ref b.Agent.RotationDegrees);
            b.Agent.Position = (b.Agent.Position + (b.Agent.Velocity * dt)).WrapPosition(windowSize);
        }

        _fps = 1.0 / gameTime.Delta.TotalSeconds;
        _updateMs = gameTime.Delta.TotalMilliseconds;
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        for (int i = 0; i < _boids.Count; i++)
        {
            Boid b = _boids[i];
            IReadOnlyList<IMovableGameObject> neighbors = _neighborCache[i];

            b.Debug.DrawNeighborhood(window, neighbors, NeighborhoodRadius);
            b.Debug.DrawSeparation(window, neighbors, SeparationRadius);
            b.Debug.DrawAlignment(window, neighbors);
            b.Debug.DrawCohesion(window, neighbors);
            b.Debug.DrawVelocityAndHeading(window);
            b.Agent.Draw(window);
        }

        if (_font is not null)
        {
            SteeringDebugDrawer.DrawStats(window, _font, _fps, _updateMs);
        }
    }
}
