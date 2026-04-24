namespace GrayHare.GameEngine.Pathfinding;

/// <summary>
/// Contains the result of a pathfinding search, including the solved path
/// and exploration metadata for debug visualization.
/// </summary>
public sealed class PathfindingResult
{
    /// <summary>Initializes a new <see cref="PathfindingResult"/>.</summary>
    /// <param name="start">The start cell of the search.</param>
    /// <param name="end">The target cell of the search.</param>
    /// <param name="path">
    /// The ordered path from <paramref name="start"/> to <paramref name="end"/>,
    /// or an empty list if no path was found.
    /// </param>
    /// <param name="visited">All cells that were explored during the search.</param>
    public PathfindingResult(
        GridCell start,
        GridCell end,
        IReadOnlyList<GridCell> path,
        IReadOnlySet<GridCell> visited)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(visited);

        Start = start;
        End = end;
        Path = path;
        Visited = visited;
    }

    /// <summary>The start cell of the search.</summary>
    public GridCell Start { get; }

    /// <summary>The target cell of the search.</summary>
    public GridCell End { get; }

    /// <summary>
    /// The solved path from <see cref="Start"/> to <see cref="End"/> (inclusive),
    /// or an empty list if no path was found.
    /// </summary>
    public IReadOnlyList<GridCell> Path { get; }

    /// <summary>All cells that were explored during the search.</summary>
    public IReadOnlySet<GridCell> Visited { get; }

    /// <summary>Whether a path from <see cref="Start"/> to <see cref="End"/> was found.</summary>
    public bool Found => Path.Count > 0;
}
