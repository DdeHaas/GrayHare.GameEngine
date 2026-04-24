using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.FollowPathDemo;

/// <summary>
/// Demonstrates <see cref="SteeringBehavior.FollowPath"/>.
/// A green agent loops around one of five waypoint presets indefinitely.
/// Press <c>1</c> through <c>5</c> to switch the active path, <c>Tab</c> to toggle the
/// follow mode, and <c>`</c> to toggle debug.
/// </summary>
internal sealed class FollowPathScene : DemoSceneBase
{
    private readonly record struct PathOption(string Name, Vector2f[] Waypoints);
    private enum PathFollowMode
    {
        Smooth,
        OnDot
    }

    private static readonly Vector2f PathCenter = new(640f, 280f);
    private static readonly PathOption[] PathOptions =
    [
        new("Pentagon", CreateRegularPolygonPath(PathCenter, 220f, 5)),
        new("Triangle", CreateRegularPolygonPath(PathCenter, 225f, 3)),
        new("Square", CreateRegularPolygonPath(PathCenter, 210f, 4)),
        new("Hexagon", CreateRegularPolygonPath(PathCenter, 210f, 6)),
        new("Star", CreateStarPath(PathCenter, 220f, 95f, 5))
    ];

    private readonly AutonomousAgent _agent = new PathFollower();
    private SteeringBehavior _steering = null!;
    private SteeringDebugDrawer _debug = null!;
    private IReadOnlyList<Vector2f> _path = Array.Empty<Vector2f>();
    private int _pathIndex;
    private int _selectedPathIndex;
    private PathFollowMode _followMode;
    private Font? _font;
    private double _fps;
    private double _updateMs;

    private const float InitialSpeed = 80f;
    private const float SlowingRadius = 60f;
    private const float ExactWaypointSnapDistance = 10f;

    public FollowPathScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);

        _steering = new SteeringBehavior(_agent);
        _debug = new SteeringDebugDrawer(_agent);

        _font = host.Assets.LoadFont();
        SelectPath(0);
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        if (host.Input.WasKeyPressed(Keyboard.Key.Grave))
        {
            SteeringDebugDrawer.Enabled = !SteeringDebugDrawer.Enabled;
        }

        HandleControls(host);

        float dt = (float)gameTime.Delta.TotalSeconds;

        // Wrap path index so the agent loops forever
        if (_pathIndex >= _path.Count)
        {
            _pathIndex = 0;
        }

        if (_followMode == PathFollowMode.OnDot)
        {
            FollowPathOnDot(dt);
        }
        else
        {
            Vector2f force = _steering.FollowPath(ref _pathIndex, _path, SlowingRadius);
            _agent.Velocity = (_agent.Velocity + (force * dt)).Truncate(_agent.MaxSpeed);
            _agent.HeadingRef = _steering.UpdateHeadingWhileMoving(dt, ref _agent.RotationDegrees);
            _agent.Position += _agent.Velocity * dt;
        }

        _fps = 1.0 / gameTime.Delta.TotalSeconds;
        _updateMs = gameTime.Delta.TotalMilliseconds;
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        DrawPath(window);
        DrawSelectedPath(window);
        _debug.DrawFollowPath(window, _path);
        _debug.DrawVelocityAndHeading(window);
        _agent.Draw(window);

        if (_font is not null)
        {
            SteeringDebugDrawer.DrawStats(window, _font, _fps, _updateMs);
        }
    }

    private void DrawPath(RenderWindow window)
    {
        if (_path.Count < 2)
        {
            return;
        }

        using VertexArray lines = new(PrimitiveType.Lines);
        Color pathColor = new(80, 180, 80, 100);

        for (int i = 0; i < _path.Count; i++)
        {
            Vector2f from = _path[i];
            Vector2f to = _path[(i + 1) % _path.Count];
            lines.Append(new Vertex(from, pathColor));
            lines.Append(new Vertex(to, pathColor));
        }

        window.Draw(lines);
    }

    private void DrawSelectedPath(RenderWindow window)
    {
        if (_font is null)
        {
            return;
        }

        string mode = _followMode == PathFollowMode.Smooth ? "Smooth" : "On-Dot";
        string text = $"Path {_selectedPathIndex + 1}/{PathOptions.Length}: {PathOptions[_selectedPathIndex].Name}   Mode: {mode}";

        using Text label = new(_font, text, 20)
        {
            Position = new Vector2f(24f, 20f),
            FillColor = new Color(230, 238, 255)
        };

        window.Draw(label);
    }

    private void HandleControls(GameHost host)
    {
        if (host.Input.WasKeyPressed(Keyboard.Key.Tab))
        {
            _followMode = _followMode == PathFollowMode.Smooth
                ? PathFollowMode.OnDot
                : PathFollowMode.Smooth;
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Num1))
        {
            SelectPath(0);
        }
        else if (host.Input.WasKeyPressed(Keyboard.Key.Num2))
        {
            SelectPath(1);
        }
        else if (host.Input.WasKeyPressed(Keyboard.Key.Num3))
        {
            SelectPath(2);
        }
        else if (host.Input.WasKeyPressed(Keyboard.Key.Num4))
        {
            SelectPath(3);
        }
        else if (host.Input.WasKeyPressed(Keyboard.Key.Num5))
        {
            SelectPath(4);
        }
    }

    private void FollowPathOnDot(float deltaTime)
    {
        if (_path.Count < 2)
        {
            _agent.Velocity = Constants.Vectors.Zero;
            return;
        }

        float speed = MathF.Max(_agent.Velocity.Length, InitialSpeed);
        float remainingDistance = speed * deltaTime;

        while (remainingDistance > 0f)
        {
            Vector2f waypoint = _path[_pathIndex];
            Vector2f toWaypoint = waypoint - _agent.Position;
            float distance = toWaypoint.Length;

            if (distance <= ExactWaypointSnapDistance)
            {
                _agent.Position = waypoint;
                _pathIndex = (_pathIndex + 1) % _path.Count;

                Vector2f nextSegment = _path[_pathIndex] - _agent.Position;
                if (nextSegment.Length > float.Epsilon)
                {
                    _agent.HeadingRef = nextSegment.Normalized();
                }

                continue;
            }

            Vector2f direction = toWaypoint / distance;

            if (remainingDistance < distance)
            {
                _agent.Position += direction * remainingDistance;
                _agent.HeadingRef = direction;
                remainingDistance = 0f;
                break;
            }

            _agent.Position = waypoint;
            _agent.HeadingRef = direction;
            remainingDistance -= distance;
            _pathIndex = (_pathIndex + 1) % _path.Count;

            Vector2f nextDirection = _path[_pathIndex] - _agent.Position;
            if (nextDirection.Length > float.Epsilon)
            {
                _agent.HeadingRef = nextDirection.Normalized();
            }
        }

        _agent.Velocity = _agent.HeadingRef * speed;
        _agent.RotationDegrees = _agent.HeadingRef.Angle().WrapUnsigned().Degrees;
    }

    private void SelectPath(int pathOptionIndex)
    {
        PathOption pathOption = PathOptions[pathOptionIndex];
        _selectedPathIndex = pathOptionIndex;
        _path = pathOption.Waypoints;
        _pathIndex = 0;
        _agent.Position = _path[0];
        _agent.HeadingRef = (_path[1] - _path[0]).Normalized();
        _agent.Velocity = _agent.HeadingRef * InitialSpeed;
        _agent.RotationDegrees = _agent.HeadingRef.Angle().WrapUnsigned().Degrees;
    }

    private static Vector2f[] CreateRegularPolygonPath(Vector2f center, float radius, int sides)
    {
        Vector2f[] waypoints = new Vector2f[sides];

        for (int i = 0; i < sides; i++)
        {
            float angle = ((MathF.Tau * i) / sides) - (MathF.PI / 2f);
            waypoints[i] = new Vector2f(
                center.X + (MathF.Cos(angle) * radius),
                center.Y + (MathF.Sin(angle) * radius));
        }

        return waypoints;
    }

    private static Vector2f[] CreateStarPath(
        Vector2f center,
        float outerRadius,
        float innerRadius,
        int points)
    {
        Vector2f[] waypoints = new Vector2f[points * 2];

        for (int i = 0; i < waypoints.Length; i++)
        {
            float angle = ((MathF.PI * i) / points) - (MathF.PI / 2f);
            float radius = i % 2 == 0 ? outerRadius : innerRadius;
            waypoints[i] = new Vector2f(
                center.X + (MathF.Cos(angle) * radius),
                center.Y + (MathF.Sin(angle) * radius));
        }

        return waypoints;
    }
}
