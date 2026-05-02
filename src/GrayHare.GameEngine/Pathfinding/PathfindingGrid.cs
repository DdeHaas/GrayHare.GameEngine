namespace GrayHare.GameEngine.Pathfinding;

/// <summary>
/// A rectangular grid of walkable and blocked cells used for pathfinding queries.
/// </summary>
/// <remarks>
/// <para>
/// Each cell is walkable by default. Use <see cref="SetBlocked"/> to mark cells
/// as walls and <see cref="Clear"/> to reset every cell to walkable.
/// </para>
/// <para>
/// Neighbor queries return only orthogonal (4-direction) neighbors that are
/// both in-bounds and walkable.
/// </para>
/// <para>This type is not thread-safe. Access all members from the main thread only.</para>
/// </remarks>
public sealed class PathfindingGrid
{
    private static readonly (int DRow, int DCol)[] _offsets =
        [(0, 1), (1, 0), (0, -1), (-1, 0)];

    private readonly bool[,] _blocked;

    /// <summary>
    /// Initializes a new <see cref="PathfindingGrid"/> where every cell is walkable.
    /// </summary>
    /// <param name="rows">Number of rows. Must be greater than zero.</param>
    /// <param name="columns">Number of columns. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="rows"/> or <paramref name="columns"/> is
    /// less than or equal to zero.
    /// </exception>
    public PathfindingGrid(int rows, int columns)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(rows, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(columns, 0);

        Rows = rows;
        Columns = columns;
        _blocked = new bool[rows, columns];
    }

    /// <summary>The number of rows in the grid.</summary>
    public int Rows { get; }

    /// <summary>The number of columns in the grid.</summary>
    public int Columns { get; }

    /// <summary>Returns whether <paramref name="cell"/> is within the grid bounds.</summary>
    public bool IsInBounds(GridCell cell)
    {
        return cell.Row >= 0 && cell.Row < Rows
            && cell.Column >= 0 && cell.Column < Columns;
    }

    /// <summary>Returns whether <paramref name="cell"/> is within bounds and not blocked.</summary>
    public bool IsWalkable(GridCell cell)
    {
        return IsInBounds(cell) && !_blocked[cell.Row, cell.Column];
    }

    /// <summary>Returns whether <paramref name="cell"/> is within bounds and blocked.</summary>
    public bool IsBlocked(GridCell cell)
    {
        return IsInBounds(cell) && _blocked[cell.Row, cell.Column];
    }

    /// <summary>Marks <paramref name="cell"/> as blocked or walkable.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="cell"/> is out of bounds.
    /// </exception>
    public void SetBlocked(GridCell cell, bool blocked)
    {
        if (!IsInBounds(cell))
        {
            throw new ArgumentOutOfRangeException(nameof(cell));
        }

        _blocked[cell.Row, cell.Column] = blocked;
    }

    /// <summary>Resets all cells to walkable.</summary>
    public void Clear()
    {
        Array.Clear(_blocked);
    }

    /// <summary>
    /// Fills <paramref name="results"/> with the walkable orthogonal neighbors
    /// of <paramref name="cell"/>.
    /// </summary>
    /// <param name="cell">The cell to query neighbors for.</param>
    /// <param name="results">
    /// A caller-owned list that is cleared and filled with walkable neighbors.
    /// Reuse this list across calls to avoid allocations.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="results"/> is <see langword="null"/>.
    /// </exception>
    public void GetWalkableNeighbors(GridCell cell, List<GridCell> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        results.Clear();

        foreach ((int dRow, int dCol) in _offsets)
        {
            GridCell neighbor = new(cell.Row + dRow, cell.Column + dCol);

            if (IsWalkable(neighbor))
            {
                results.Add(neighbor);
            }
        }
    }
}
