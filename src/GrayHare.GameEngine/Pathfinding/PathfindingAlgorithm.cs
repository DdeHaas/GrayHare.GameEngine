namespace GrayHare.GameEngine.Pathfinding;

/// <summary>Specifies which pathfinding algorithm to use.</summary>
public enum PathfindingAlgorithm
{
    /// <summary>Breadth-first search. Guarantees shortest path on unweighted grids.</summary>
    BFS,

    /// <summary>Depth-first search. Does not guarantee shortest path.</summary>
    DFS,

    /// <summary>Dijkstra's algorithm. Guarantees shortest path.</summary>
    Dijkstra,

    /// <summary>
    /// A* search with Manhattan distance heuristic. Guarantees shortest path and
    /// typically visits far fewer cells than BFS or Dijkstra.
    /// </summary>
    AStar,

    /// <summary>
    /// Flow field search. Performs a single BFS from the goal outward to produce
    /// per-cell direction vectors for the entire grid. Efficient when many agents
    /// share the same destination.
    /// </summary>
    FlowField
}
