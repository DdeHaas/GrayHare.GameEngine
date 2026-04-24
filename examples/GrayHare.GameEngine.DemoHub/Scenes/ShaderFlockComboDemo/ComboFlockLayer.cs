using GrayHare.GameEngine.Abstractions;
using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.ShaderFlockComboDemo;

/// <summary>
/// Scene layer that runs a full flocking simulation with 80 autonomous boids using
/// separation, alignment, cohesion, and wander steering behaviors.
/// <para>
/// Press <c>`</c> (Grave) to toggle <see cref="SteeringDebugDrawer"/> visualization
/// that renders neighborhood circles, velocity vectors, and per-boid steering forces.
/// </para>
/// </summary>
internal sealed class ComboFlockLayer : ISceneLayer
{
    private const int BoidCount = 80;
    private const float NeighborhoodRadius = 110f;
    private const float SeparationRadius = 75f;
    private const float WanderRadius = 28f;
    private const float WanderDistance = 65f;

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
    private List<IMovableGameObject>[] _neighborCache = [];

    /// <summary>Gets the number of active boids in the simulation.</summary>
    public int ActiveBoidCount => _boids.Count;

    /// <summary>Gets the most recently measured frames per second.</summary>
    public double Fps { get; private set; }

    /// <summary>Gets the most recently measured update duration in milliseconds.</summary>
    public double UpdateMs { get; private set; }

    public int RenderOrder => 0;

    public void Load(GameHost host)
    {
        _boids.Clear();

        Random random = Random.Shared;
        float w = host.Window.Size.X;
        float h = host.Window.Size.Y;

        for (int i = 0; i < BoidCount; i++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2.0);
            AutonomousAgent agent = new(new Color(80, 220, 255), maxSpeed: 220f, turnRate: 280f)
            {
                Position = new Vector2f(
                    (float)(random.NextDouble() * w),
                    (float)(random.NextDouble() * h)),
                HeadingRef = new Vector2f(MathF.Cos(angle), MathF.Sin(angle))
            };
            agent.Velocity = agent.HeadingRef * 140f;

            _boids.Add(new Boid(agent) { WanderAngle = angle });
        }

        _neighborCache = new List<IMovableGameObject>[BoidCount];
        for (int i = 0; i < BoidCount; i++)
        {
            _neighborCache[i] = [];
        }
    }

    public void Unload(GameHost host)
    {
        foreach (Boid b in _boids)
        {
            b.Debug.Dispose();
            b.Agent.Dispose();
        }

        _boids.Clear();
    }

    public void Update(GameHost host, in GameTime gameTime)
    {
        if (host.Input.WasKeyPressed(Keyboard.Key.Grave))
        {
            SteeringDebugDrawer.Enabled = !SteeringDebugDrawer.Enabled;
        }

        float dt = (float)gameTime.Delta.TotalSeconds;
        Vector2f windowSize = new(host.Window.Size.X, host.Window.Size.Y);

        // Snapshot neighbor lists from current positions so all boids
        // perceive the same world state regardless of update order.
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
            Vector2f separateForce = b.Steering.Separation(neighbors, SeparationRadius);
            Vector2f alignForce = b.Steering.Alignment(neighbors);
            Vector2f cohesionForce = b.Steering.Cohesion(neighbors);

            // Weighted blend: separation has highest priority to prevent clumping.
            Vector2f force = SteeringForces.WeightedSum(
                b.Agent.MaxSpeed,
                (separateForce, 4f),
                (alignForce, 3f),
                (cohesionForce, 2f),
                (wanderForce, 1f));

            b.Agent.Velocity = (b.Agent.Velocity + (force * dt)).Truncate(b.Agent.MaxSpeed);
            b.Agent.HeadingRef = b.Steering.UpdateHeadingWhileMoving(dt, ref b.Agent.RotationDegrees);
            b.Agent.Position = (b.Agent.Position + (b.Agent.Velocity * dt)).WrapPosition(windowSize);
        }

        Fps = 1.0 / gameTime.Delta.TotalSeconds;
        UpdateMs = gameTime.Delta.TotalMilliseconds;
    }

    public void RenderLayer(GameHost host, RenderWindow window)
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
    }
}
