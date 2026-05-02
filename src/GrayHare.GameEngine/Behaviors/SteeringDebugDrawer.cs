using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.Behaviors;

/// <summary>
/// Draws debug visuals for steering behaviors.  Mirrors the API of
/// <see cref="SteeringBehavior"/> but renders SFML primitives instead of
/// computing forces.
/// </summary>
/// <remarks>
/// Toggle all instances simultaneously with the <see langword="static"/>
/// <see cref="Enabled"/> flag (default <see langword="false"/>).
/// When enabled, <see cref="DrawStats"/> displays framerate and update time
/// in the bottom-left corner of the window.
/// </remarks>
public sealed class SteeringDebugDrawer : IDisposable
{
    private const float DotRadius = 4f;
    private const float HeadingLength = 40f;

    private readonly IMovableGameObject _gameObject;
    private readonly CircleShape _circleShape = new();
    private readonly VertexArray _lines = new(PrimitiveType.Lines);

    /// <summary>Gets or sets whether debug drawing is active for all instances.</summary>
    public static bool Enabled { get; set; }

    /// <summary>Initializes the drawer for <paramref name="gameObject"/>.</summary>
    public SteeringDebugDrawer(IMovableGameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        _gameObject = gameObject;
    }

    // ── Per-behavior draw methods ───────────────────────────────────────────

    /// <summary>Draws the wander ahead-circle, displacement point, and direction line.</summary>
    public void DrawWander(
        RenderWindow window,
        float wanderAngle,
        float wanderRadius,
        float wanderDistance)
    {
        if (!Enabled)
        {
            return;
        }

        Vector2f circleCenter =
            _gameObject.Position + (_gameObject.Heading.Normalized() * wanderDistance);
        Vector2f wanderTarget =
            circleCenter +
            (new Vector2f(MathF.Cos(wanderAngle), MathF.Sin(wanderAngle)) * wanderRadius);

        DrawCircle(window, circleCenter, wanderRadius, Color.Cyan);
        DrawLine(window, _gameObject.Position, circleCenter, Color.Cyan);
        DrawLine(window, circleCenter, wanderTarget, Color.Yellow);
        DrawDot(window, wanderTarget, Color.Yellow);
    }

    /// <summary>Draws the slowing-radius circle and a line toward the arrive target.</summary>
    public void DrawArrive(RenderWindow window, Vector2f targetPosition, float slowingRadius)
    {
        if (!Enabled)
        {
            return;
        }

        DrawCircle(window, targetPosition, slowingRadius, Color.Green);
        DrawLine(window, _gameObject.Position, targetPosition, Color.Green);
        DrawDot(window, targetPosition, Color.Green);
    }

    /// <summary>Draws a line toward the seek target and a dot at the target.</summary>
    public void DrawSeek(RenderWindow window, Vector2f targetPosition)
    {
        if (!Enabled)
        {
            return;
        }

        DrawLine(window, _gameObject.Position, targetPosition, Color.Green);
        DrawDot(window, targetPosition, Color.Green);
    }

    /// <summary>Draws a line away from the flee origin and a dot at the origin.</summary>
    public void DrawFlee(RenderWindow window, Vector2f targetPosition)
    {
        if (!Enabled)
        {
            return;
        }

        Color orange = new(255, 140, 0);
        DrawLine(window, _gameObject.Position, targetPosition, orange);
        DrawDot(window, targetPosition, orange);
    }

    /// <summary>Draws the predicted future position used by the pursue behavior.</summary>
    public void DrawPursue(RenderWindow window, IMovableGameObject target)
    {
        if (!Enabled)
        {
            return;
        }

        Vector2f predicted = PredictedPosition(target);
        DrawDot(window, predicted, Color.Magenta);
        DrawLine(window, _gameObject.Position, predicted, Color.Magenta);
        DrawLine(window, target.Position, predicted, new Color(180, 0, 180));
    }

    /// <summary>Draws the predicted future position used by the evade behavior (red).</summary>
    public void DrawEvade(RenderWindow window, IMovableGameObject target)
    {
        if (!Enabled)
        {
            return;
        }

        Vector2f predicted = PredictedPosition(target);
        Color red = new(255, 80, 80);
        DrawDot(window, predicted, red);
        DrawLine(window, _gameObject.Position, predicted, red);
        DrawLine(window, target.Position, predicted, new Color(200, 50, 50));
    }

    /// <summary>Draws the velocity vector (white) and heading vector (green).</summary>
    public void DrawVelocityAndHeading(RenderWindow window)
    {
        if (!Enabled)
        {
            return;
        }

        Vector2f pos = _gameObject.Position;
        DrawLine(window, pos, pos + _gameObject.Velocity, Color.White);
        DrawLine(
            window,
            pos,
            pos + (_gameObject.Heading.Normalized() * HeadingLength),
            Color.Green);
    }

    /// <summary>
    /// Draws the auto-pilot target, its arrival radius, and a required-heading arrow
    /// from the agent.
    /// </summary>
    public void DrawAutoPilot(
        RenderWindow window,
        Vector2f targetPosition,
        float arrivalRadius)
    {
        if (!Enabled)
        {
            return;
        }

        Color color = new(100, 255, 100);
        DrawLine(window, _gameObject.Position, targetPosition, color);
        DrawCircle(window, targetPosition, arrivalRadius, color);
        DrawDot(window, targetPosition, color);

        Vector2f toTarget = targetPosition - _gameObject.Position;
        if (toTarget.Length > float.Epsilon)
        {
            Vector2f requiredHeading = toTarget / toTarget.Length;
            DrawLine(
                window,
                _gameObject.Position,
                _gameObject.Position + (requiredHeading * HeadingLength * 1.5f),
                Color.Yellow);
        }
    }

    /// <summary>Draws the outer boundary rect and the inner margin rect.</summary>
    public void DrawBoundary(RenderWindow window, FloatRect boundary, float margin)
    {
        if (!Enabled)
        {
            return;
        }

        DrawRect(window, boundary, new Color(80, 80, 220));
        DrawRect(
            window,
            new FloatRect(
                new Vector2f(boundary.Left + margin, boundary.Top + margin),
                new Vector2f(boundary.Width - (margin * 2f), boundary.Height - (margin * 2f))),
            new Color(80, 80, 220, 80));
    }

    /// <summary>
    /// Draws the detection box (OBB) and highlights obstacles within range.
    /// </summary>
    public void DrawObstacleAvoidance(
        RenderWindow window,
        IReadOnlyList<IGameObject> obstacles,
        float detectionLength,
        float agentRadius)
    {
        if (!Enabled)
        {
            return;
        }

        Vector2f pos = _gameObject.Position;
        Vector2f heading = _gameObject.Heading;
        Vector2f side = _gameObject.Side;
        Color boxColor = new(255, 165, 0);

        DrawLine(window, pos - (side * agentRadius),
            pos + (heading * detectionLength) - (side * agentRadius), boxColor);
        DrawLine(window, pos + (heading * detectionLength) - (side * agentRadius),
            pos + (heading * detectionLength) + (side * agentRadius), boxColor);
        DrawLine(window, pos + (heading * detectionLength) + (side * agentRadius),
            pos + (side * agentRadius), boxColor);

        foreach (IGameObject obstacle in obstacles)
        {
            Vector2f toObstacle = obstacle.Position - pos;
            float localX = toObstacle.Dot(heading);

            if (localX < 0f || localX > detectionLength)
            {
                continue;
            }

            float localY = toObstacle.Dot(side);
            FloatRect bounds = obstacle.GlobalBounds;
            float obstacleRadius = MathF.Max(bounds.Width, bounds.Height) / 2f;

            if (MathF.Abs(localY) < agentRadius + obstacleRadius)
            {
                DrawDot(window, obstacle.Position, new Color(255, 100, 0));
            }
        }
    }

    /// <summary>Draws the three feelers and marks any wall intersection points.</summary>
    public void DrawWallAvoidance(
        RenderWindow window,
        IReadOnlyList<Wall> walls,
        float feelerLength,
        float feelerAngle)
    {
        if (!Enabled)
        {
            return;
        }

        Vector2f pos = _gameObject.Position;
        Vector2f heading = _gameObject.Heading;
        Color feelerColor = new(255, 200, 0);
        Angle angle = Angle.FromDegrees(feelerAngle);

        Vector2f[] feelerTips =
        [
            pos + (heading * feelerLength),
            pos + (heading.RotatedBy(angle) * (feelerLength * 0.75f)),
            pos + (heading.RotatedBy(-angle) * (feelerLength * 0.75f)),
        ];

        foreach (Vector2f feelerTip in feelerTips)
        {
            DrawLine(window, pos, feelerTip, feelerColor);

            foreach (Wall wall in walls)
            {
                if (wall.TryGetIntersection(pos, feelerTip, out float t))
                {
                    DrawDot(window, pos + ((feelerTip - pos) * t), Color.Red);
                }
            }
        }
    }

    /// <summary>
    /// Draws the current formation slot (cyan dot), the predicted arrival target (magenta dot),
    /// and steering lines for the <see cref="SteeringBehavior.OffsetPursuit"/> behavior.
    /// </summary>
    public void DrawOffsetPursuit(
        RenderWindow window,
        IMovableGameObject leader,
        Vector2f offset,
        float slowingRadius)
    {
        if (!Enabled)
        {
            return;
        }

        Vector2f offsetWorldPos =
            leader.Position + (leader.Heading * offset.X) + (leader.Side * offset.Y);

        Vector2f toOffset = offsetWorldPos - _gameObject.Position;
        float lookAheadTime = toOffset.Length /
            MathF.Max(_gameObject.MaxSpeed + leader.Speed, float.Epsilon);
        Vector2f predictedTarget = offsetWorldPos + (leader.Velocity * lookAheadTime);

        Color slotColor = new(100, 200, 255);
        Color predictColor = new(200, 100, 255);

        // Current slot position and connection to the leader
        DrawDot(window, offsetWorldPos, slotColor);
        DrawLine(window, leader.Position, offsetWorldPos, slotColor);

        // Predicted target and agent's intended path
        DrawDot(window, predictedTarget, predictColor);
        DrawLine(window, _gameObject.Position, predictedTarget, predictColor);
        DrawCircle(window, predictedTarget, slowingRadius,
            new Color(predictColor.R, predictColor.G, predictColor.B, 80));
    }

    /// <summary>
    /// Draws the predicted positions of both objects, connecting lines,
    /// and the midpoint target.
    /// </summary>
    public void DrawInterpose(
        RenderWindow window,
        IMovableGameObject? object1,
        IMovableGameObject? object2)
    {
        if (!Enabled || object1 is null || object2 is null)
        {
            return;
        }

        Vector2f midPoint = (object1.Position + object2.Position) / 2f;
        float timeToReach =
            _gameObject.Position.DistanceTo(midPoint) /
            MathF.Max(_gameObject.MaxSpeed, float.Epsilon);

        Vector2f predicted1 = object1.Position + (object1.Velocity * timeToReach);
        Vector2f predicted2 = object2.Position + (object2.Velocity * timeToReach);
        Vector2f interposeTarget = (predicted1 + predicted2) / 2f;

        Color predColor = new(255, 165, 100);
        Color targetColor = new(100, 220, 100);

        DrawDot(window, predicted1, predColor);
        DrawLine(window, object1.Position, predicted1, predColor);
        DrawDot(window, predicted2, predColor);
        DrawLine(window, object2.Position, predicted2, predColor);
        DrawLine(window, predicted1, predicted2, predColor);
        DrawDot(window, interposeTarget, targetColor);
        DrawLine(window, _gameObject.Position, interposeTarget, targetColor);
    }

    /// <summary>Draws hiding spots behind each obstacle relative to the threat.</summary>
    public void DrawHide(
        RenderWindow window,
        IMovableGameObject? target,
        IReadOnlyList<IGameObject> obstacles,
        float distanceFromBoundary,
        float threatDistance)
    {
        if (!Enabled || target is null)
        {
            return;
        }

        if (_gameObject.Position.DistanceTo(target.Position) > threatDistance)
        {
            return;
        }

        Color hidingColor = new(255, 165, 100);
        Color targetColor = new(100, 220, 100);

        foreach (IGameObject obstacle in obstacles)
        {
            FloatRect bounds = obstacle.GlobalBounds;
            float radius = MathF.Max(bounds.Width, bounds.Height) + distanceFromBoundary;
            Vector2f toObstacle = (obstacle.Position - target.Position).Normalized();
            Vector2f hidingSpot = obstacle.Position + (toObstacle * radius);

            DrawLine(window, target.Position, hidingSpot, targetColor);
            DrawDot(window, hidingSpot, hidingColor);
        }
    }

    /// <summary>Draws waypoint dots and connecting path lines.</summary>
    public void DrawFollowPath(RenderWindow window, IReadOnlyList<Vector2f> pathToFollow)
    {
        if (!Enabled || pathToFollow is null || pathToFollow.Count == 0)
        {
            return;
        }

        Color lineColor = new(100, 100, 100);
        Color pointColor = Color.Yellow;

        for (int i = 0; i < pathToFollow.Count - 1; i++)
        {
            DrawLine(window, pathToFollow[i], pathToFollow[i + 1], lineColor);
        }

        foreach (Vector2f point in pathToFollow)
        {
            DrawDot(window, point, pointColor, 3f);
        }
    }

    /// <summary>
    /// Draws the neighborhood radius circle and faint lines to each neighbor.
    /// </summary>
    public void DrawNeighborhood(
        RenderWindow window,
        IReadOnlyList<IMovableGameObject> neighbors,
        float neighborhoodRadius)
    {
        if (!Enabled)
        {
            return;
        }

        Color dimColor = new(70, 70, 100);
        DrawCircle(window, _gameObject.Position, neighborhoodRadius, dimColor);

        foreach (IMovableGameObject neighbor in neighbors)
        {
            DrawLine(window, _gameObject.Position, neighbor.Position, new Color(50, 50, 80));
        }
    }

    /// <summary>
    /// Draws the separation radius circle and repulsion arrows toward close neighbors.
    /// </summary>
    public void DrawSeparation(
        RenderWindow window,
        IReadOnlyList<IMovableGameObject> neighbors,
        float separationRadius)
    {
        if (!Enabled)
        {
            return;
        }

        DrawCircle(window, _gameObject.Position, separationRadius, new Color(180, 60, 60));

        foreach (IMovableGameObject neighbor in neighbors)
        {
            Vector2f toAgent = _gameObject.Position - neighbor.Position;
            float distance = toAgent.Length;

            if (distance > float.Epsilon && distance < separationRadius)
            {
                DrawLine(
                    window,
                    _gameObject.Position,
                    _gameObject.Position + (toAgent.Normalized() * 30f),
                    new Color(255, 100, 100));
            }
        }
    }

    /// <summary>
    /// Draws an arrow in the direction of the average heading of <paramref name="neighbors"/>.
    /// </summary>
    public void DrawAlignment(RenderWindow window, IReadOnlyList<IMovableGameObject> neighbors)
    {
        if (!Enabled || neighbors.Count == 0)
        {
            return;
        }

        Vector2f avgHeading = new(0f, 0f);

        foreach (IMovableGameObject neighbor in neighbors)
        {
            avgHeading += neighbor.Heading;
        }

        Vector2f dir = (avgHeading / (float)neighbors.Count).Normalized();
        Color cyan = new(100, 220, 255);
        Vector2f tip = _gameObject.Position + (dir * 40f);
        DrawLine(window, _gameObject.Position, tip, cyan);
        DrawDot(window, tip, cyan, 3f);
    }

    /// <summary>
    /// Draws the center of mass of <paramref name="neighbors"/> and a line from the agent toward it.
    /// </summary>
    public void DrawCohesion(RenderWindow window, IReadOnlyList<IMovableGameObject> neighbors)
    {
        if (!Enabled || neighbors.Count == 0)
        {
            return;
        }

        Vector2f centerOfMass = new(0f, 0f);

        foreach (IMovableGameObject neighbor in neighbors)
        {
            centerOfMass += neighbor.Position;
        }

        centerOfMass /= (float)neighbors.Count;
        Color green = new(100, 255, 140);
        DrawLine(window, _gameObject.Position, centerOfMass, new Color(60, 160, 90));
        DrawDot(window, centerOfMass, green, 5f);
    }

    /// <summary>
    /// Draws framerate and last-frame duration in the bottom-left corner.
    /// Only draws when <see cref="Enabled"/> is <see langword="true"/>.
    /// </summary>
    /// <param name="window">The render window.</param>
    /// <param name="font">Font to use for the text.</param>
    /// <param name="fps">Current frames per second.</param>
    /// <param name="updateMs">Last update duration in milliseconds.</param>
    public static void DrawStats(
        RenderWindow window,
        Font font,
        double fps,
        double updateMs)
    {
        if (!Enabled)
        {
            return;
        }

        using Text text = new(
            font,
            $"FPS: {fps:F0}  Update: {updateMs:F2} ms",
            14)
        {
            FillColor = new Color(200, 200, 200, 180),
            Position = new Vector2f(8f, window.Size.Y - 28f)
        };

        window.Draw(text);
    }

    /// <summary>
    /// Draws the occupied cells of a <see cref="Spatial.SpatialGrid{T}"/> as
    /// translucent rectangles with an item-count label in each cell.
    /// Cells with more items are drawn with a brighter fill.
    /// Only draws when <see cref="Enabled"/> is <see langword="true"/>.
    /// </summary>
    /// <typeparam name="T">The item type stored in the grid.</typeparam>
    /// <param name="window">The render window.</param>
    /// <param name="grid">The spatial grid to visualize.</param>
    /// <param name="font">Font used for the per-cell item count.</param>
    public static void DrawSpatialGrid<T>(
        RenderWindow window,
        Spatial.SpatialGrid<T> grid,
        Font font) where T : class
    {
        if (!Enabled)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(font);

        float cellSize = grid.CellSize;
        Color outlineColor = new(80, 120, 200, 100);

        using RectangleShape rect = new(new Vector2f(cellSize, cellSize))
        {
            OutlineThickness = 1f
        };

        foreach ((Vector2f cellOrigin, int itemCount) in grid.EnumerateCells())
        {
            // Brighter fill for denser cells (clamped at 10 items).
            byte alpha = (byte)Math.Min(20 + itemCount * 20, 200);
            rect.FillColor = new Color(60, 100, 180, alpha);
            rect.OutlineColor = outlineColor;
            rect.Position = cellOrigin;
            window.Draw(rect);

            using Text label = new(font, itemCount.ToString(), 11)
            {
                FillColor = new Color(220, 220, 255, 200),
                Position = new Vector2f(cellOrigin.X + 3f, cellOrigin.Y + 1f)
            };

            window.Draw(label);
        }
    }

    // ── Primitives ──────────────────────────────────────────────────────────

    private void DrawCircle(RenderWindow window, Vector2f center, float radius, Color color)
    {
        _circleShape.Radius = radius;
        _circleShape.Origin = new Vector2f(radius, radius);
        _circleShape.Position = center;
        _circleShape.FillColor = Color.Transparent;
        _circleShape.OutlineColor = color;
        _circleShape.OutlineThickness = 1f;
        _circleShape.SetPointCount(32);
        window.Draw(_circleShape);
    }

    private void DrawDot(
        RenderWindow window,
        Vector2f center,
        Color color,
        float radius = DotRadius)
    {
        _circleShape.Radius = radius;
        _circleShape.Origin = new Vector2f(radius, radius);
        _circleShape.Position = center;
        _circleShape.FillColor = color;
        _circleShape.OutlineThickness = 0f;
        _circleShape.SetPointCount(8);
        window.Draw(_circleShape);
    }

    private void DrawLine(RenderWindow window, Vector2f from, Vector2f to, Color color)
    {
        _lines.Clear();
        _lines.Append(new Vertex(from, color));
        _lines.Append(new Vertex(to, color));
        window.Draw(_lines);
    }

    private void DrawRect(RenderWindow window, FloatRect rect, Color color)
    {
        Vector2f topLeft = new(rect.Left, rect.Top);
        Vector2f topRight = new(rect.Left + rect.Width, rect.Top);
        Vector2f bottomRight = new(rect.Left + rect.Width, rect.Top + rect.Height);
        Vector2f bottomLeft = new(rect.Left, rect.Top + rect.Height);

        _lines.Clear();
        _lines.Append(new Vertex(topLeft, color));
        _lines.Append(new Vertex(topRight, color));
        _lines.Append(new Vertex(topRight, color));
        _lines.Append(new Vertex(bottomRight, color));
        _lines.Append(new Vertex(bottomRight, color));
        _lines.Append(new Vertex(bottomLeft, color));
        _lines.Append(new Vertex(bottomLeft, color));
        _lines.Append(new Vertex(topLeft, color));
        window.Draw(_lines);
    }

    private Vector2f PredictedPosition(IMovableGameObject target)
    {
        Vector2f toTarget = target.Position - _gameObject.Position;
        float lookAheadTime = toTarget.Length / (_gameObject.MaxSpeed + target.Speed);

        return target.Position + (target.Velocity * lookAheadTime);
    }

    private bool _disposed;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _circleShape.Dispose();
            _lines.Dispose();
            _disposed = true;
        }
    }
}
