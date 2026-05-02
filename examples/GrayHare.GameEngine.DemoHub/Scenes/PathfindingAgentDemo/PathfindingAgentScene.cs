using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Pathfinding;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.PathfindingAgentDemo;

/// <summary>
/// Demonstrates pathfinding in action: a randomly generated obstacle course of
/// wall segments and circular blockers is produced, then an agent navigates from
/// a random start to a random destination using the selected algorithm.
/// On arrival the agent pauses for two seconds before a new maze is generated.
/// Press <c>A</c> to cycle between BFS, DFS, Dijkstra, A*, and Flow Field.
/// In Flow Field mode the agent steers dynamically via the per-cell direction map.
/// Press <c>Space</c> to skip to a new maze immediately.
/// </summary>
internal sealed class PathfindingAgentScene : DemoSceneBase
{
    private const int GridRows = 14;
    private const int GridColumns = 32;
    private const float CellSize = 36f;
    private static readonly Vector2f GridOrigin = new(16f, 52f);

    private const float AgentSpeed = 160f;
    private const float WaypointThreshold = CellSize * 0.45f;
    private const float AgentRadius = 10f;
    private const float WallThickness = 3f;
    private const float ArrivalDelay = 2f;
    private const float GoalMarkerHalf = 8f;

    private readonly PathfindingGrid _grid = new(GridRows, GridColumns);
    private readonly List<Wall> _walls = [];
    private readonly List<(Vector2f Center, float Radius)> _circles = [];
    private readonly Random _random = Random.Shared;

    private PathfindingResult? _result;
    private FlowFieldResult? _flowField;
    private PathfindingAlgorithm _algorithm = PathfindingAlgorithm.BFS;
    private GridCell _startCell;
    private GridCell _goalCell;
    private Vector2f _agentPos;
    private int _pathIndex;
    private bool _arrived;
    private float _arrivalTimer;
    private Font _font = null!;

    public PathfindingAgentScene(DemoCatalog catalog, int sceneIndex)
        : base(catalog, sceneIndex)
    {
    }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();
        GenerateMaze();
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        if (host.Input.WasKeyPressed(Keyboard.Key.Grave))
        {
            PathfindingDebugDrawer.Enabled = !PathfindingDebugDrawer.Enabled;
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.A))
        {
            _algorithm = _algorithm switch
            {
                PathfindingAlgorithm.BFS => PathfindingAlgorithm.DFS,
                PathfindingAlgorithm.DFS => PathfindingAlgorithm.Dijkstra,
                PathfindingAlgorithm.Dijkstra => PathfindingAlgorithm.AStar,
                PathfindingAlgorithm.AStar => PathfindingAlgorithm.FlowField,
                _ => PathfindingAlgorithm.BFS
            };
            RerunPath();

            return;
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Space))
        {
            GenerateMaze();

            return;
        }

        if (_arrived)
        {
            _arrivalTimer += gameTime.DeltaTotalSeconds;
            if (_arrivalTimer >= ArrivalDelay)
            {
                GenerateMaze();
            }

            return;
        }

        MoveAgent(gameTime.DeltaTotalSeconds);
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        PathfindingDebugDrawer.DrawGrid(window, _grid, CellSize, GridOrigin);

        DrawCircleOutlines(window);
        DrawWallLines(window);

        if (_flowField is not null)
        {
            PathfindingDebugDrawer.DrawFlowField(window, _flowField, CellSize, GridOrigin);
        }

        if (_result is not null)
        {
            PathfindingDebugDrawer.DrawResult(
                window, _result, CellSize, GridOrigin, showVisited: false);
        }

        DrawGoal(window);
        DrawAgent(window);
        DrawHud(window);
    }

    // ── Agent movement ───────────────────────────────────────────────────────

    private void MoveAgent(float deltaTime)
    {
        if (_algorithm == PathfindingAlgorithm.FlowField)
        {
            MoveAgentViaFlowField(deltaTime);

            return;
        }

        if (_result is null || !_result.Found || _pathIndex >= _result.Path.Count)
        {
            _arrived = true;
            _arrivalTimer = 0f;

            return;
        }

        Vector2f target = CellCenter(_result.Path[_pathIndex]);
        Vector2f diff = target - _agentPos;
        float dist = MathF.Sqrt((diff.X * diff.X) + (diff.Y * diff.Y));

        if (dist <= WaypointThreshold)
        {
            _pathIndex++;

            if (_pathIndex >= _result.Path.Count)
            {
                _arrived = true;
                _arrivalTimer = 0f;
            }

            return;
        }

        Vector2f dir = diff / dist;
        _agentPos += dir * AgentSpeed * deltaTime;
    }

    private void MoveAgentViaFlowField(float deltaTime)
    {
        if (_flowField is null)
        {
            _arrived = true;
            _arrivalTimer = 0f;

            return;
        }

        GridCell? currentCell = WorldToCell(_agentPos);

        if (currentCell is null || currentCell.Value == _goalCell)
        {
            _arrived = true;
            _arrivalTimer = 0f;

            return;
        }

        GridCell? nextCell = _flowField.GetNextCell(currentCell.Value);

        if (nextCell is null)
        {
            _arrived = true;
            _arrivalTimer = 0f;

            return;
        }

        Vector2f target = CellCenter(nextCell.Value);
        Vector2f diff = target - _agentPos;
        float dist = MathF.Sqrt((diff.X * diff.X) + (diff.Y * diff.Y));

        if (dist <= WaypointThreshold)
        {
            return;
        }

        Vector2f dir = diff / dist;
        _agentPos += dir * AgentSpeed * deltaTime;
    }

    // ── Maze generation ──────────────────────────────────────────────────────

    private void GenerateMaze()
    {
        _arrived = false;
        _arrivalTimer = 0f;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            _grid.Clear();
            _walls.Clear();
            _circles.Clear();

            BuildRandomWalls();
            BuildRandomCircles();

            _grid.ApplyWalls(_walls, CellSize, GridOrigin);
            StampCircles();

            GridCell? start = PickRandomWalkableCell();
            GridCell? end = PickRandomWalkableCell();

            if (start is null || end is null || start == end)
            {
                continue;
            }

            // Require a minimum Manhattan distance for an interesting path.
            int manDist = Math.Abs(start.Value.Row - end.Value.Row)
                        + Math.Abs(start.Value.Column - end.Value.Column);

            if (manDist < GridRows / 2)
            {
                continue;
            }

            PathfindingResult result = PathFinder.FindPath(_grid, start.Value, end.Value, _algorithm);

            if (!result.Found)
            {
                continue;
            }

            _result = result;
            _startCell = start.Value;
            _goalCell = end.Value;
            _flowField = _algorithm == PathfindingAlgorithm.FlowField
                ? PathFinder.BuildFlowField(_grid, _goalCell)
                : null;
            _agentPos = CellCenter(start.Value);
            _pathIndex = 0;

            return;
        }

        // Fallback: open grid with well-separated endpoints.
        _grid.Clear();
        _walls.Clear();
        _circles.Clear();

        _startCell = new GridCell(GridRows / 2, 1);
        _goalCell = new GridCell(GridRows / 2, GridColumns - 2);
        _result = PathFinder.FindPath(_grid, _startCell, _goalCell, _algorithm);
        _flowField = _algorithm == PathfindingAlgorithm.FlowField
            ? PathFinder.BuildFlowField(_grid, _goalCell)
            : null;
        _agentPos = CellCenter(_startCell);
        _pathIndex = 0;
    }

    private void BuildRandomWalls()
    {
        int count = _random.Next(9, 16);

        for (int i = 0; i < count; i++)
        {
            bool horizontal = _random.Next(2) == 0;
            int length = _random.Next(3, 10);

            if (horizontal)
            {
                int row = _random.Next(2, GridRows - 2);
                int col = _random.Next(1, GridColumns - length - 1);

                Vector2f from = GridOrigin + new Vector2f(col * CellSize, row * CellSize);
                Vector2f to = GridOrigin + new Vector2f((col + length) * CellSize, row * CellSize);
                _walls.Add(new Wall(from, to));
            }
            else
            {
                int col = _random.Next(2, GridColumns - 2);
                int row = _random.Next(1, GridRows - length - 1);

                Vector2f from = GridOrigin + new Vector2f(col * CellSize, row * CellSize);
                Vector2f to = GridOrigin + new Vector2f(col * CellSize, (row + length) * CellSize);
                _walls.Add(new Wall(from, to));
            }
        }
    }

    private void BuildRandomCircles()
    {
        int count = _random.Next(4, 8);

        for (int i = 0; i < count; i++)
        {
            float radius = CellSize * (0.7f + ((float)_random.NextDouble() * 1.0f));
            float x = GridOrigin.X + (2f * CellSize)
                    + (float)_random.NextDouble() * ((GridColumns - 4) * CellSize);
            float y = GridOrigin.Y + (2f * CellSize)
                    + (float)_random.NextDouble() * ((GridRows - 4) * CellSize);

            _circles.Add((new Vector2f(x, y), radius));
        }
    }

    private void StampCircles()
    {
        foreach ((Vector2f center, float radius) in _circles)
        {
            int colMin = Math.Max(0, (int)((center.X - GridOrigin.X - radius) / CellSize));
            int colMax = Math.Min(GridColumns - 1, (int)((center.X - GridOrigin.X + radius) / CellSize));
            int rowMin = Math.Max(0, (int)((center.Y - GridOrigin.Y - radius) / CellSize));
            int rowMax = Math.Min(GridRows - 1, (int)((center.Y - GridOrigin.Y + radius) / CellSize));

            for (int r = rowMin; r <= rowMax; r++)
            {
                for (int c = colMin; c <= colMax; c++)
                {
                    Vector2f cc = new(
                        GridOrigin.X + ((c + 0.5f) * CellSize),
                        GridOrigin.Y + ((r + 0.5f) * CellSize));

                    float dx = cc.X - center.X;
                    float dy = cc.Y - center.Y;

                    if ((dx * dx) + (dy * dy) <= radius * radius)
                    {
                        _grid.SetBlocked(new GridCell(r, c), true);
                    }
                }
            }
        }
    }

    private GridCell? PickRandomWalkableCell()
    {
        for (int attempt = 0; attempt < 300; attempt++)
        {
            GridCell cell = new(_random.Next(GridRows), _random.Next(GridColumns));

            if (_grid.IsWalkable(cell))
            {
                return cell;
            }
        }

        return null;
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    private void DrawCircleOutlines(RenderWindow window)
    {
        Color outlineColor = new(200, 175, 110, 200);

        foreach ((Vector2f center, float radius) in _circles)
        {
            using CircleShape shape = new(radius)
            {
                Position = center - new Vector2f(radius, radius),
                FillColor = Color.Transparent,
                OutlineColor = outlineColor,
                OutlineThickness = 2f
            };
            window.Draw(shape);
        }
    }

    private void DrawWallLines(RenderWindow window)
    {
        Color wallColor = new(210, 200, 165);

        foreach (Wall wall in _walls)
        {
            DrawThickLine(window, wall.Start, wall.End, wallColor, WallThickness);
        }
    }

    private void DrawAgent(RenderWindow window)
    {
        using CircleShape body = new(AgentRadius)
        {
            Position = _agentPos - new Vector2f(AgentRadius, AgentRadius),
            FillColor = new Color(100, 200, 255),
            OutlineColor = Color.White,
            OutlineThickness = 1.5f
        };
        window.Draw(body);

        // Direction indicator pointing toward the next waypoint.
        if (_result is not null && _pathIndex < _result.Path.Count)
        {
            Vector2f target = CellCenter(_result.Path[_pathIndex]);
            Vector2f diff = target - _agentPos;
            float dist = MathF.Sqrt((diff.X * diff.X) + (diff.Y * diff.Y));

            if (dist > 1f)
            {
                Vector2f dir = diff / dist;
                Vector2f tip = _agentPos + (dir * (AgentRadius + 7f));

                using VertexArray line = new(PrimitiveType.Lines);
                line.Append(new Vertex(_agentPos, Color.White));
                line.Append(new Vertex(tip, Color.White));
                window.Draw(line);
            }
        }
    }

    private void DrawHud(RenderWindow window)
    {
        int pathLen = _result?.Path.Count ?? 0;
        int remaining = _result is not null
            ? Math.Max(0, pathLen - _pathIndex)
            : 0;

        string algoName = _algorithm switch
        {
            PathfindingAlgorithm.DFS => "DFS",
            PathfindingAlgorithm.Dijkstra => "Dijkstra",
            PathfindingAlgorithm.AStar => "A*",
            PathfindingAlgorithm.FlowField => "Flow Field",
            _ => "BFS"
        };

        string status = _arrived
            ? $"Arrived – new maze in {Math.Max(0f, ArrivalDelay - _arrivalTimer):F1}s"
            : $"Path: {pathLen} cells  Remaining: {remaining}";

        string text = $"[A] {algoName}  {status}  [Space] new maze";

        using Text hud = new(_font, text, 16)
        {
            Position = new Vector2f(16f, 10f),
            FillColor = new Color(220, 220, 240)
        };
        window.Draw(hud);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Re-plans a path on the current maze using the active algorithm, then
    /// resets the agent to the start of that path.
    /// </summary>
    private void RerunPath()
    {
        PathfindingResult result = PathFinder.FindPath(_grid, _startCell, _goalCell, _algorithm);
        _result = result;
        _flowField = _algorithm == PathfindingAlgorithm.FlowField
            ? PathFinder.BuildFlowField(_grid, _goalCell)
            : null;
        _agentPos = CellCenter(_startCell);
        _pathIndex = 0;
        _arrived = false;
        _arrivalTimer = 0f;
    }

    /// <summary>Draws a small red X at the goal cell center.</summary>
    private void DrawGoal(RenderWindow window)
    {
        Vector2f center = CellCenter(_goalCell);
        Color xColor = new(240, 80, 80);

        using VertexArray lines = new(PrimitiveType.Lines);
        lines.Append(new Vertex(center + new Vector2f(-GoalMarkerHalf, -GoalMarkerHalf), xColor));
        lines.Append(new Vertex(center + new Vector2f(GoalMarkerHalf, GoalMarkerHalf), xColor));
        lines.Append(new Vertex(center + new Vector2f(GoalMarkerHalf, -GoalMarkerHalf), xColor));
        lines.Append(new Vertex(center + new Vector2f(-GoalMarkerHalf, GoalMarkerHalf), xColor));
        window.Draw(lines);
    }

    private static Vector2f CellCenter(GridCell cell)
    {
        return GridOrigin + new Vector2f(
            (cell.Column + 0.5f) * CellSize,
            (cell.Row + 0.5f) * CellSize);
    }

    private static GridCell? WorldToCell(Vector2f worldPos)
    {
        float x = worldPos.X - GridOrigin.X;
        float y = worldPos.Y - GridOrigin.Y;

        if (x < 0f || y < 0f)
        {
            return null;
        }

        int col = (int)(x / CellSize);
        int row = (int)(y / CellSize);

        return row >= 0 && row < GridRows && col >= 0 && col < GridColumns
            ? new GridCell(row, col)
            : null;
    }

    private static void DrawThickLine(
        RenderWindow window,
        Vector2f from,
        Vector2f to,
        Color color,
        float thickness)
    {
        Vector2f diff = to - from;
        float length = MathF.Sqrt((diff.X * diff.X) + (diff.Y * diff.Y));

        if (length < float.Epsilon)
        {
            return;
        }

        float angle = MathF.Atan2(diff.Y, diff.X) * (180f / MathF.PI);

        using RectangleShape rect = new(new Vector2f(length, thickness))
        {
            Position = from,
            Origin = new Vector2f(0f, thickness / 2f),
            Rotation = angle,
            FillColor = color
        };
        window.Draw(rect);
    }
}
