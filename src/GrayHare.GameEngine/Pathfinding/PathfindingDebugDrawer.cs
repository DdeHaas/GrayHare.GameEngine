using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.Pathfinding;

/// <summary>
/// Draws debug visuals for pathfinding grids and search results.
/// </summary>
/// <remarks>
/// Toggle all rendering with the <see langword="static"/>
/// <see cref="Enabled"/> flag (default <see langword="true"/>).
/// All methods are static and do not require per-instance state.
/// </remarks>
public static class PathfindingDebugDrawer
{
    /// <summary>Gets or sets whether debug drawing is active.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>
    /// Draws the grid lines and blocked (wall) cells.
    /// </summary>
    /// <param name="window">The render window.</param>
    /// <param name="grid">The pathfinding grid.</param>
    /// <param name="cellSize">The width and height of each cell in pixels.</param>
    /// <param name="origin">The top-left corner of the grid in screen space.</param>
    public static void DrawGrid(
        RenderWindow window,
        PathfindingGrid grid,
        float cellSize,
        Vector2f origin)
    {
        if (!Enabled)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(grid);

        Color wallColor = new(60, 65, 80);
        Vector2f size = new(cellSize, cellSize);

        using RectangleShape rect = new(size);

        for (int row = 0; row < grid.Rows; row++)
        {
            for (int col = 0; col < grid.Columns; col++)
            {
                GridCell cell = new(row, col);

                if (grid.IsBlocked(cell))
                {
                    rect.Position = CellToWorld(cell, cellSize, origin);
                    rect.FillColor = wallColor;
                    rect.OutlineThickness = 0f;
                    window.Draw(rect);
                }
            }
        }

        DrawGridLines(window, grid, cellSize, origin);
    }

    /// <summary>
    /// Draws the search result overlay: visited cells, solved path, and
    /// start/end markers.
    /// </summary>
    /// <param name="window">The render window.</param>
    /// <param name="result">The pathfinding result to visualize.</param>
    /// <param name="cellSize">The width and height of each cell in pixels.</param>
    /// <param name="origin">The top-left corner of the grid in screen space.</param>
    /// <param name="showVisited">
    /// When <see langword="true"/>, explored cells are drawn with a translucent fill.
    /// </param>
    public static void DrawResult(
        RenderWindow window,
        PathfindingResult result,
        float cellSize,
        Vector2f origin,
        bool showVisited)
    {
        if (!Enabled)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(result);

        Vector2f size = new(cellSize, cellSize);

        using RectangleShape rect = new(size);

        if (showVisited)
        {
            Color visitedColor = new(40, 80, 140, 80);

            foreach (GridCell cell in result.Visited)
            {
                rect.Position = CellToWorld(cell, cellSize, origin);
                rect.FillColor = visitedColor;
                rect.OutlineThickness = 0f;
                window.Draw(rect);
            }
        }

        if (result.Found)
        {
            Color pathColor = new(80, 220, 120, 180);

            foreach (GridCell cell in result.Path)
            {
                rect.Position = CellToWorld(cell, cellSize, origin);
                rect.FillColor = pathColor;
                rect.OutlineThickness = 0f;
                window.Draw(rect);
            }
        }

        Color startColor = new(50, 200, 50);
        Color endColor = new(220, 50, 50);

        rect.Position = CellToWorld(result.Start, cellSize, origin);
        rect.FillColor = startColor;
        rect.OutlineThickness = 0f;
        window.Draw(rect);

        rect.Position = CellToWorld(result.End, cellSize, origin);
        rect.FillColor = endColor;
        rect.OutlineThickness = 0f;
        window.Draw(rect);
    }

    /// <summary>
    /// Draws an arrow in every reachable cell of the flow field pointing toward
    /// the next step on the path to the goal.
    /// </summary>
    /// <param name="window">The render window.</param>
    /// <param name="field">The flow field to visualize.</param>
    /// <param name="cellSize">The width and height of each cell in pixels.</param>
    /// <param name="origin">The top-left corner of the grid in screen space.</param>
    public static void DrawFlowField(
        RenderWindow window,
        FlowFieldResult field,
        float cellSize,
        Vector2f origin)
    {
        if (!Enabled)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(field);

        float arrowLen = cellSize * 0.32f;
        float headLen = arrowLen * 0.38f;
        float headWidth = arrowLen * 0.28f;
        Color arrowColor = new(120, 190, 255, 170);

        using VertexArray lines = new(PrimitiveType.Lines);

        foreach (GridCell cell in field.ReachableCells)
        {
            GridCell? next = field.GetNextCell(cell);

            if (next is null)
            {
                continue;
            }

            Vector2f center = CellToWorldCenter(cell, cellSize, origin);
            Vector2f dir = new(next.Value.Column - cell.Column, next.Value.Row - cell.Row);
            Vector2f perp = new(-dir.Y, dir.X);

            Vector2f tail = center - (dir * (arrowLen * 0.5f));
            Vector2f tip = center + (dir * (arrowLen * 0.5f));

            lines.Append(new Vertex(tail, arrowColor));
            lines.Append(new Vertex(tip, arrowColor));

            lines.Append(new Vertex(tip, arrowColor));
            lines.Append(new Vertex(tip - (dir * headLen) + (perp * headWidth), arrowColor));

            lines.Append(new Vertex(tip, arrowColor));
            lines.Append(new Vertex(tip - (dir * headLen) - (perp * headWidth), arrowColor));
        }

        window.Draw(lines);
    }

    private static void DrawGridLines(
        RenderWindow window,
        PathfindingGrid grid,
        float cellSize,
        Vector2f origin)
    {
        Color lineColor = new(50, 55, 70);
        float totalWidth = grid.Columns * cellSize;
        float totalHeight = grid.Rows * cellSize;

        using VertexArray lines = new(PrimitiveType.Lines);

        for (int row = 0; row <= grid.Rows; row++)
        {
            float y = origin.Y + (row * cellSize);
            lines.Append(new Vertex(new Vector2f(origin.X, y), lineColor));
            lines.Append(new Vertex(new Vector2f(origin.X + totalWidth, y), lineColor));
        }

        for (int col = 0; col <= grid.Columns; col++)
        {
            float x = origin.X + (col * cellSize);
            lines.Append(new Vertex(new Vector2f(x, origin.Y), lineColor));
            lines.Append(new Vertex(new Vector2f(x, origin.Y + totalHeight), lineColor));
        }

        window.Draw(lines);
    }

    private static Vector2f CellToWorld(GridCell cell, float cellSize, Vector2f origin)
    {
        return new Vector2f(
            origin.X + (cell.Column * cellSize),
            origin.Y + (cell.Row * cellSize));
    }

    private static Vector2f CellToWorldCenter(GridCell cell, float cellSize, Vector2f origin)
    {
        return new Vector2f(
            origin.X + ((cell.Column + 0.5f) * cellSize),
            origin.Y + ((cell.Row + 0.5f) * cellSize));
    }
}
