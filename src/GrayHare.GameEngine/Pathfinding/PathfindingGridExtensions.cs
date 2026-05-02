using GrayHare.GameEngine.Behaviors;
using SFML.System;

namespace GrayHare.GameEngine.Pathfinding;

/// <summary>
/// Extension methods on <see cref="PathfindingGrid"/> for integration with
/// engine geometry types.
/// </summary>
public static class PathfindingGridExtensions
{
    /// <summary>
    /// Marks every grid cell that is intersected by one or more <see cref="Wall"/>
    /// segments as blocked.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For each wall, only the cells whose bounding box overlaps the wall's own
    /// axis-aligned bounding box are tested, so the method scales with wall length
    /// rather than total grid size.
    /// </para>
    /// <para>
    /// A cell is marked blocked when any of its four edges intersects the wall
    /// segment, or when either endpoint of the wall falls inside the cell. This
    /// handles walls that cross cell corners as well as very short walls that lie
    /// entirely within a single cell.
    /// </para>
    /// <para>
    /// This method does <b>not</b> clear the grid before applying walls. Call
    /// <see cref="PathfindingGrid.Clear"/> first if you want a clean slate.
    /// </para>
    /// </remarks>
    /// <param name="grid">The grid to modify.</param>
    /// <param name="walls">The walls to stamp onto the grid.</param>
    /// <param name="cellSize">Width and height of each cell in world-space pixels.</param>
    /// <param name="origin">World-space position of the grid's top-left corner.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="grid"/> or <paramref name="walls"/> is
    /// <see langword="null"/>.
    /// </exception>
    public static void ApplyWalls(
        this PathfindingGrid grid,
        IEnumerable<Wall> walls,
        float cellSize,
        Vector2f origin)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(walls);

        foreach (Wall wall in walls)
        {
            // Narrow candidate cells to the wall's AABB to avoid testing every cell.
            float minX = MathF.Min(wall.Start.X, wall.End.X);
            float maxX = MathF.Max(wall.Start.X, wall.End.X);
            float minY = MathF.Min(wall.Start.Y, wall.End.Y);
            float maxY = MathF.Max(wall.Start.Y, wall.End.Y);

            int colMin = Math.Max(0, (int)((minX - origin.X) / cellSize));
            int colMax = Math.Min(grid.Columns - 1, (int)((maxX - origin.X) / cellSize));
            int rowMin = Math.Max(0, (int)((minY - origin.Y) / cellSize));
            int rowMax = Math.Min(grid.Rows - 1, (int)((maxY - origin.Y) / cellSize));

            for (int row = rowMin; row <= rowMax; row++)
            {
                for (int col = colMin; col <= colMax; col++)
                {
                    if (CellIntersectsWall(wall, col, row, cellSize, origin))
                    {
                        grid.SetBlocked(new GridCell(row, col), true);
                    }
                }
            }
        }
    }

    private static bool CellIntersectsWall(
        Wall wall,
        int col,
        int row,
        float cellSize,
        Vector2f origin)
    {
        Vector2f topLeft = origin + new Vector2f(col * cellSize, row * cellSize);
        Vector2f topRight = topLeft + new Vector2f(cellSize, 0f);
        Vector2f bottomRight = topLeft + new Vector2f(cellSize, cellSize);
        Vector2f bottomLeft = topLeft + new Vector2f(0f, cellSize);

        // Test each of the four cell edges against the wall segment.
        if (wall.TryGetIntersection(topLeft, topRight, out _) ||
            wall.TryGetIntersection(topRight, bottomRight, out _) ||
            wall.TryGetIntersection(bottomRight, bottomLeft, out _) ||
            wall.TryGetIntersection(bottomLeft, topLeft, out _))
        {
            return true;
        }

        // A very short wall may lie entirely inside a cell without touching any edge.
        return PointInsideCell(topLeft, cellSize, wall.Start)
            || PointInsideCell(topLeft, cellSize, wall.End);
    }

    private static bool PointInsideCell(Vector2f topLeft, float cellSize, Vector2f point)
    {
        return point.X >= topLeft.X && point.X < topLeft.X + cellSize
            && point.Y >= topLeft.Y && point.Y < topLeft.Y + cellSize;
    }
}
