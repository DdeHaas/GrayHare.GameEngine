using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.AvoidanceDemo;

/// <summary>
/// Demonstrates <see cref="SteeringBehavior.WallAvoidance"/> and
/// <see cref="SteeringBehavior.ObstacleAvoidance"/> together.
/// An agent wanders freely inside a walled arena while dodging six circular rocks.
/// </summary>
internal sealed class AvoidanceScene : DemoSceneBase
{
    private readonly AutonomousAgent _agent = new AvoidanceWanderer();
    private SteeringBehavior _steering = null!;
    private SteeringDebugDrawer _debug = null!;
    private List<Wall> _walls = [];
    private readonly List<ObstacleCircle> _obstacles = [];
    private float _wanderAngle;
    private Font? _font;
    private double _fps;
    private double _updateMs;

    private const float FeelerLength = 80f;
    private const float FeelerAngle = 45f;
    private const float Margin = 60f;
    private const float AgentRadius = 14f;
    private const float DetectionLength = 120f;
    private const float WanderRadius = 50f;
    private const float WanderDistance = 100f;

    public AvoidanceScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);

        _agent.Position = new Vector2f(640f, 360f);
        _agent.HeadingRef = new Vector2f(1f, 0f);
        _agent.Velocity = _agent.HeadingRef * 80f;

        _steering = new SteeringBehavior(_agent);
        _debug = new SteeringDebugDrawer(_agent);

        float w = host.Window.Size.X;
        float h = host.Window.Size.Y;

        // Four border walls directed inward + two diagonal interior walls (each listed
        // twice in opposite directions so both faces correctly repel the agent).
        _walls =
        [
            new Wall(new Vector2f(Margin, Margin), new Vector2f(w - Margin, Margin)),           // top
            new Wall(new Vector2f(w - Margin, Margin), new Vector2f(w - Margin, h - Margin)),   // right
            new Wall(new Vector2f(w - Margin, h - Margin), new Vector2f(Margin, h - Margin)),   // bottom
            new Wall(new Vector2f(Margin, h - Margin), new Vector2f(Margin, Margin)),           // left
            new Wall(new Vector2f(250f, 180f), new Vector2f(450f, 380f)),                        // diagonal ↘ front
            new Wall(new Vector2f(450f, 380f), new Vector2f(250f, 180f)),                        // diagonal ↖ back
            new Wall(new Vector2f(900f, 200f), new Vector2f(700f, 480f)),                        // diagonal ↙ front
            new Wall(new Vector2f(700f, 480f), new Vector2f(900f, 200f)),                        // diagonal ↗ back
        ];

        _obstacles.Clear();
        _obstacles.AddRange(
        [
            new ObstacleCircle(new Vector2f(350f, 280f), 32f),
            new ObstacleCircle(new Vector2f(850f, 240f), 40f),
            new ObstacleCircle(new Vector2f(520f, 500f), 28f),
            new ObstacleCircle(new Vector2f(780f, 510f), 36f),
            new ObstacleCircle(new Vector2f(200f, 460f), 30f),
            new ObstacleCircle(new Vector2f(1000f, 390f), 34f),
        ]);

        _font = host.Assets.LoadFont();
    }

    public override void Unload(GameHost host)
    {
        base.Unload(host);
        // Dispose the CircleShape inside each ObstacleCircle to release unmanaged resources.
        foreach (ObstacleCircle obstacle in _obstacles)
        {
            obstacle.Dispose();
        }

        _obstacles.Clear();
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        if (host.Input.WasKeyPressed(Keyboard.Key.Grave))
        {
            SteeringDebugDrawer.Enabled = !SteeringDebugDrawer.Enabled;
        }

        float dt = (float)gameTime.Delta.TotalSeconds;

        Vector2f wanderForce = _steering.Wander(ref _wanderAngle, WanderRadius, WanderDistance);
        Vector2f obstacleForce = _steering.ObstacleAvoidance(_obstacles, DetectionLength, AgentRadius);
        // StayWithinBounds + WallAvoidance together keep the agent inside the arena.
        FloatRect safeArea = new(
            new Vector2f(Margin, Margin),
            new Vector2f(host.Window.Size.X - (Margin * 2f), host.Window.Size.Y - (Margin * 2f)));
        Vector2f boundsForce = _steering.StayWithinBounds(safeArea, Margin * 0.5f);
        Vector2f wallForce = _steering.WallAvoidance(_walls, FeelerLength, FeelerAngle);

        // Weighted blend: avoidance forces are given progressively higher weights so they
        // dominate even when wander happens to point toward a hazard.
        Vector2f force = SteeringForces.WeightedSum(
            _agent.MaxSpeed,
            (wanderForce, 1f),   // lowest weight: background exploration
            (obstacleForce, 2f),
            (boundsForce, 3f),
            (wallForce, 4f));  // highest weight: wall avoidance always dominates

        _agent.Velocity = (_agent.Velocity + (force * dt)).Truncate(_agent.MaxSpeed);
        _agent.HeadingRef = _steering.UpdateHeadingWhileMoving(dt, ref _agent.RotationDegrees);
        _agent.Position += _agent.Velocity * dt;

        _fps = 1.0 / gameTime.Delta.TotalSeconds;
        _updateMs = gameTime.Delta.TotalMilliseconds;
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        DrawWalls(window);

        foreach (ObstacleCircle obstacle in _obstacles)
        {
            obstacle.Draw(window);
        }

        _debug.DrawWander(window, _wanderAngle, WanderRadius, WanderDistance);
        _debug.DrawWallAvoidance(window, _walls, FeelerLength, FeelerAngle);
        _debug.DrawObstacleAvoidance(window, _obstacles, DetectionLength, AgentRadius);
        _debug.DrawVelocityAndHeading(window);
        _agent.Draw(window);

        if (_font is not null)
        {
            SteeringDebugDrawer.DrawStats(window, _font, _fps, _updateMs);
        }
    }

    private void DrawWalls(RenderWindow window)
    {
        using VertexArray lines = new(PrimitiveType.Lines);
        Color wallColor = new(180, 140, 80);
        foreach (Wall wall in _walls)
        {
            lines.Append(new Vertex(wall.Start, wallColor));
            lines.Append(new Vertex(wall.End, wallColor));
        }

        window.Draw(lines);
    }
}
