using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Pathfinding;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.PathfindingDemo;

/// <summary>
/// Interactive pathfinding demo. Displays a grid where the user can paint/erase
/// walls, place start and end points, and cycle between DFS, BFS, Dijkstra, A*,
/// and Flow Field to compare exploration patterns and solved paths.
/// </summary>
internal sealed class PathfindingScene : DemoSceneBase
{
    private const int GridRows = 19;
    private const int GridColumns = 42;
    private const float CellSize = 28f;
    private static readonly Vector2f GridOrigin = new(20f, 60f);

    private readonly PathfindingGrid _grid = new(GridRows, GridColumns);
    private GridCell _start = new(GridRows / 2, 3);
    private GridCell _end = new(GridRows / 2, GridColumns - 4);
    private PathfindingAlgorithm _algorithm = PathfindingAlgorithm.BFS;
    private PathfindingResult? _result;
    private FlowFieldResult? _flowField;
    private bool _showVisited = true;
    private bool _dirty = true;
    private Font _font = null!;

    public PathfindingScene(DemoCatalog catalog, int sceneIndex)
        : base(catalog, sceneIndex)
    {
    }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        if (host.Input.WasKeyPressed(Keyboard.Key.Grave))
        {
            _showVisited = !_showVisited;
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Tab))
        {
            _algorithm = _algorithm switch
            {
                PathfindingAlgorithm.BFS => PathfindingAlgorithm.DFS,
                PathfindingAlgorithm.DFS => PathfindingAlgorithm.Dijkstra,
                PathfindingAlgorithm.Dijkstra => PathfindingAlgorithm.AStar,
                PathfindingAlgorithm.AStar => PathfindingAlgorithm.FlowField,
                _ => PathfindingAlgorithm.BFS
            };
            _dirty = true;
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Space))
        {
            _grid.Clear();
            _dirty = true;
        }

        GridCell? cell = MouseToCell(
            host.Input.MousePosition.X,
            host.Input.MousePosition.Y);

        if (host.Input.WasKeyPressed(Keyboard.Key.S) && cell is not null)
        {
            _start = cell.Value;
            _grid.SetBlocked(_start, false);
            _dirty = true;
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.E) && cell is not null)
        {
            _end = cell.Value;
            _grid.SetBlocked(_end, false);
            _dirty = true;
        }

        if (cell is not null && cell.Value != _start && cell.Value != _end)
        {
            if (Mouse.IsButtonPressed(Mouse.Button.Left))
            {
                if (!_grid.IsBlocked(cell.Value))
                {
                    _grid.SetBlocked(cell.Value, true);
                    _dirty = true;
                }
            }
            else if (Mouse.IsButtonPressed(Mouse.Button.Right))
            {
                if (_grid.IsBlocked(cell.Value))
                {
                    _grid.SetBlocked(cell.Value, false);
                    _dirty = true;
                }
            }
        }

        if (_dirty)
        {
            _result = PathFinder.FindPath(_grid, _start, _end, _algorithm);
            _flowField = _algorithm == PathfindingAlgorithm.FlowField
                ? PathFinder.BuildFlowField(_grid, _end)
                : null;
            _dirty = false;
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        PathfindingDebugDrawer.DrawGrid(window, _grid, CellSize, GridOrigin);

        if (_flowField is not null)
        {
            PathfindingDebugDrawer.DrawFlowField(window, _flowField, CellSize, GridOrigin);
        }

        if (_result is not null)
        {
            PathfindingDebugDrawer.DrawResult(
                window, _result, CellSize, GridOrigin, _showVisited);
        }

        string pathInfo = _result is not null
            ? _result.Found
                ? $"Path: {_result.Path.Count} cells"
                : "No path"
            : "";

        string visitedInfo = _result is not null
            ? $"Visited: {_result.Visited.Count}"
            : "";

        string status = $"{_algorithm}  {pathInfo}  {visitedInfo}";

        using Text hud = new(_font, status, 18)
        {
            Position = new Vector2f(20f, 8f),
            FillColor = new Color(220, 220, 240)
        };
        window.Draw(hud);
    }

    private static GridCell? MouseToCell(float mouseX, float mouseY)
    {
        float x = mouseX - GridOrigin.X;
        float y = mouseY - GridOrigin.Y;

        if (x < 0f || y < 0f)
        {
            return null;
        }

        int col = (int)(x / CellSize);
        int row = (int)(y / CellSize);
        GridCell cell = new(row, col);

        return row >= 0 && row < GridRows && col >= 0 && col < GridColumns
            ? cell
            : null;
    }
}
