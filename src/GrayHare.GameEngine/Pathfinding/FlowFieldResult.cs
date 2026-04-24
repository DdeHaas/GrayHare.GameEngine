namespace GrayHare.GameEngine.Pathfinding;

/// <summary>
/// Contains the result of a flow-field computation: a per-cell direction map that
/// points every walkable cell toward a shared goal.
/// </summary>
/// <remarks>
/// <para>
/// Built by a single BFS that expands outward from the goal. Once built, any
/// number of agents can look up <see cref="GetNextCell"/> in O(1) to find their
/// next step without running a separate search.
/// </para>
/// <para>
/// The goal cell itself is not contained in <see cref="ReachableCells"/> because
/// it has no outgoing direction; use <see cref="IsReachable"/> to test any cell,
/// including the goal.
/// </para>
/// </remarks>
public sealed class FlowFieldResult
{
    private readonly IReadOnlyDictionary<GridCell, GridCell> _flowMap;

    /// <summary>Initializes a new <see cref="FlowFieldResult"/>.</summary>
    /// <param name="goal">The target cell the field points toward.</param>
    /// <param name="flowMap">
    /// A mapping from each reachable cell (excluding the goal) to its next cell
    /// on the shortest path toward <paramref name="goal"/>.
    /// </param>
    public FlowFieldResult(GridCell goal, IReadOnlyDictionary<GridCell, GridCell> flowMap)
    {
        ArgumentNullException.ThrowIfNull(flowMap);

        Goal = goal;
        _flowMap = flowMap;
    }

    /// <summary>The destination cell all flow vectors point toward.</summary>
    public GridCell Goal { get; }

    /// <summary>
    /// All cells that can reach <see cref="Goal"/> (excludes the goal itself).
    /// Iterate this to draw the full arrow overlay.
    /// </summary>
    public IEnumerable<GridCell> ReachableCells => _flowMap.Keys;

    /// <summary>
    /// Returns whether <paramref name="cell"/> can reach <see cref="Goal"/>.
    /// Always <see langword="true"/> for the goal itself.
    /// </summary>
    public bool IsReachable(GridCell cell) => cell == Goal || _flowMap.ContainsKey(cell);

    /// <summary>
    /// Returns the next cell on the shortest path from <paramref name="cell"/>
    /// toward <see cref="Goal"/>, or <see langword="null"/> if the cell is
    /// unreachable or is the goal itself.
    /// </summary>
    public GridCell? GetNextCell(GridCell cell)
    {
        return _flowMap.TryGetValue(cell, out GridCell next) ? next : null;
    }
}
