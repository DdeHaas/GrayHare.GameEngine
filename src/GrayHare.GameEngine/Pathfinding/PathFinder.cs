namespace GrayHare.GameEngine.Pathfinding;

/// <summary>
/// Provides static methods for finding paths on a <see cref="PathfindingGrid"/>
/// using BFS, DFS, Dijkstra, A*, or a flow field.
/// </summary>
/// <remarks>
/// Each <c>FindPath</c> overload returns a <see cref="PathfindingResult"/> that
/// contains both the solved path and exploration metadata suitable for debug
/// visualization. Use <see cref="BuildFlowField"/> when you need the full
/// per-cell direction map (e.g., for multi-agent navigation or overlay drawing).
/// </remarks>
public static class PathFinder
{
    /// <summary>
    /// Finds a path from <paramref name="start"/> to <paramref name="end"/> using
    /// the specified <paramref name="algorithm"/>.
    /// </summary>
    /// <param name="grid">The grid to search.</param>
    /// <param name="start">The starting cell.</param>
    /// <param name="end">The target cell.</param>
    /// <param name="algorithm">The search algorithm to use.</param>
    /// <returns>A <see cref="PathfindingResult"/> with the path and exploration data.</returns>
    public static PathfindingResult FindPath(
        PathfindingGrid grid,
        GridCell start,
        GridCell end,
        PathfindingAlgorithm algorithm)
    {
        return algorithm switch
        {
            PathfindingAlgorithm.BFS => BreadthFirstSearch(grid, start, end),
            PathfindingAlgorithm.DFS => DepthFirstSearch(grid, start, end),
            PathfindingAlgorithm.Dijkstra => Dijkstra(grid, start, end),
            PathfindingAlgorithm.AStar => AStar(grid, start, end),
            PathfindingAlgorithm.FlowField => FindPathViaFlowField(grid, start, end),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };
    }

    /// <summary>
    /// Finds a path using breadth-first search.
    /// Guarantees shortest path on an unweighted grid.
    /// </summary>
    public static PathfindingResult BreadthFirstSearch(
        PathfindingGrid grid,
        GridCell start,
        GridCell end)
    {
        ArgumentNullException.ThrowIfNull(grid);

        if (!grid.IsWalkable(start))
        {
            return new PathfindingResult(start, end, [], new HashSet<GridCell>());
        }

        if (start == end)
        {
            return new PathfindingResult(start, end, [start], new HashSet<GridCell> { start });
        }

        Dictionary<GridCell, GridCell?> cameFrom = new() { [start] = null };
        Queue<GridCell> frontier = new();
        frontier.Enqueue(start);
        List<GridCell> neighbors = [];

        while (frontier.Count > 0)
        {
            GridCell current = frontier.Dequeue();

            if (current == end)
            {
                break;
            }

            grid.GetWalkableNeighbors(current, neighbors);

            foreach (GridCell neighbor in neighbors)
            {
                if (!cameFrom.ContainsKey(neighbor))
                {
                    cameFrom[neighbor] = current;
                    frontier.Enqueue(neighbor);
                }
            }
        }

        HashSet<GridCell> visited = new(cameFrom.Keys);
        List<GridCell> path = ReconstructPath(cameFrom, start, end);

        return new PathfindingResult(start, end, path, visited);
    }

    /// <summary>
    /// Finds a path using depth-first search.
    /// Does <b>not</b> guarantee shortest path.
    /// </summary>
    public static PathfindingResult DepthFirstSearch(
        PathfindingGrid grid,
        GridCell start,
        GridCell end)
    {
        ArgumentNullException.ThrowIfNull(grid);

        if (!grid.IsWalkable(start))
        {
            return new PathfindingResult(start, end, [], new HashSet<GridCell>());
        }

        if (start == end)
        {
            return new PathfindingResult(start, end, [start], new HashSet<GridCell> { start });
        }

        Dictionary<GridCell, GridCell?> cameFrom = new() { [start] = null };
        Stack<GridCell> frontier = new();
        frontier.Push(start);
        List<GridCell> neighbors = [];

        while (frontier.Count > 0)
        {
            GridCell current = frontier.Pop();

            if (current == end)
            {
                break;
            }

            grid.GetWalkableNeighbors(current, neighbors);

            foreach (GridCell neighbor in neighbors)
            {
                if (!cameFrom.ContainsKey(neighbor))
                {
                    cameFrom[neighbor] = current;
                    frontier.Push(neighbor);
                }
            }
        }

        HashSet<GridCell> visited = new(cameFrom.Keys);
        List<GridCell> path = ReconstructPath(cameFrom, start, end);

        return new PathfindingResult(start, end, path, visited);
    }

    /// <summary>
    /// Finds a path using Dijkstra's algorithm with uniform edge cost.
    /// Guarantees shortest path.
    /// </summary>
    public static PathfindingResult Dijkstra(
        PathfindingGrid grid,
        GridCell start,
        GridCell end)
    {
        ArgumentNullException.ThrowIfNull(grid);

        if (!grid.IsWalkable(start))
        {
            return new PathfindingResult(start, end, [], new HashSet<GridCell>());
        }

        if (start == end)
        {
            return new PathfindingResult(start, end, [start], new HashSet<GridCell> { start });
        }

        Dictionary<GridCell, GridCell?> cameFrom = new() { [start] = null };
        Dictionary<GridCell, int> costSoFar = new() { [start] = 0 };
        PriorityQueue<GridCell, int> frontier = new();
        frontier.Enqueue(start, 0);
        HashSet<GridCell> processed = [];
        List<GridCell> neighbors = [];

        while (frontier.Count > 0)
        {
            GridCell current = frontier.Dequeue();

            if (current == end)
            {
                break;
            }

            if (!processed.Add(current))
            {
                continue;
            }

            grid.GetWalkableNeighbors(current, neighbors);
            int newCost = costSoFar[current] + 1;

            foreach (GridCell neighbor in neighbors)
            {
                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = newCost;
                    cameFrom[neighbor] = current;
                    frontier.Enqueue(neighbor, newCost);
                }
            }
        }

        HashSet<GridCell> visited = new(cameFrom.Keys);
        List<GridCell> path = ReconstructPath(cameFrom, start, end);

        return new PathfindingResult(start, end, path, visited);
    }

    /// <summary>
    /// Finds a path using A* with a Manhattan distance heuristic.
    /// Guarantees shortest path and typically explores far fewer cells than
    /// BFS or Dijkstra on open grids.
    /// </summary>
    public static PathfindingResult AStar(
        PathfindingGrid grid,
        GridCell start,
        GridCell end)
    {
        ArgumentNullException.ThrowIfNull(grid);

        if (!grid.IsWalkable(start))
        {
            return new PathfindingResult(start, end, [], new HashSet<GridCell>());
        }

        if (start == end)
        {
            return new PathfindingResult(start, end, [start], new HashSet<GridCell> { start });
        }

        Dictionary<GridCell, GridCell?> cameFrom = new() { [start] = null };
        Dictionary<GridCell, int> costSoFar = new() { [start] = 0 };
        PriorityQueue<GridCell, int> frontier = new();
        frontier.Enqueue(start, Heuristic(start, end));
        HashSet<GridCell> processed = [];
        List<GridCell> neighbors = [];

        while (frontier.Count > 0)
        {
            GridCell current = frontier.Dequeue();

            if (current == end)
            {
                break;
            }

            if (!processed.Add(current))
            {
                continue;
            }

            grid.GetWalkableNeighbors(current, neighbors);
            int newCost = costSoFar[current] + 1;

            foreach (GridCell neighbor in neighbors)
            {
                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = newCost;
                    cameFrom[neighbor] = current;
                    frontier.Enqueue(neighbor, newCost + Heuristic(neighbor, end));
                }
            }
        }

        HashSet<GridCell> visited = new(cameFrom.Keys);
        List<GridCell> path = ReconstructPath(cameFrom, start, end);

        return new PathfindingResult(start, end, path, visited);
    }

    /// <summary>
    /// Builds a flow field from <paramref name="goal"/> outward across the entire
    /// reachable grid. Every walkable cell in the result points toward its next
    /// step on the shortest path to <paramref name="goal"/>.
    /// </summary>
    /// <remarks>
    /// Use this when many agents share the same goal. The upfront BFS cost is
    /// paid once; each agent then navigates in O(1) per step via
    /// <see cref="FlowFieldResult.GetNextCell"/>.
    /// </remarks>
    /// <param name="grid">The grid to build the flow field on.</param>
    /// <param name="goal">The destination all flow vectors converge toward.</param>
    /// <returns>
    /// A <see cref="FlowFieldResult"/> containing per-cell next-step data.
    /// </returns>
    public static FlowFieldResult BuildFlowField(PathfindingGrid grid, GridCell goal)
    {
        ArgumentNullException.ThrowIfNull(grid);

        Dictionary<GridCell, GridCell> flowMap = [];

        if (!grid.IsWalkable(goal))
        {
            return new FlowFieldResult(goal, flowMap);
        }

        Queue<GridCell> frontier = new();
        frontier.Enqueue(goal);
        HashSet<GridCell> visited = [goal];
        List<GridCell> neighbors = [];

        while (frontier.Count > 0)
        {
            GridCell current = frontier.Dequeue();
            grid.GetWalkableNeighbors(current, neighbors);

            foreach (GridCell neighbor in neighbors)
            {
                if (visited.Add(neighbor))
                {
                    // neighbor's next step toward goal is current
                    flowMap[neighbor] = current;
                    frontier.Enqueue(neighbor);
                }
            }
        }

        return new FlowFieldResult(goal, flowMap);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static PathfindingResult FindPathViaFlowField(
        PathfindingGrid grid,
        GridCell start,
        GridCell end)
    {
        ArgumentNullException.ThrowIfNull(grid);

        if (!grid.IsWalkable(start))
        {
            return new PathfindingResult(start, end, [], new HashSet<GridCell>());
        }

        if (start == end)
        {
            return new PathfindingResult(start, end, [start], new HashSet<GridCell> { start });
        }

        FlowFieldResult field = BuildFlowField(grid, end);
        List<GridCell> path = ExtractFlowPath(field, start);

        // Visited = all cells reachable from goal (the full field) + the goal itself.
        HashSet<GridCell> visited = new(field.ReachableCells) { end };

        return new PathfindingResult(start, end, path, visited);
    }

    private static List<GridCell> ExtractFlowPath(FlowFieldResult field, GridCell start)
    {
        if (!field.IsReachable(start))
        {
            return [];
        }

        List<GridCell> path = [start];
        GridCell current = start;
        HashSet<GridCell> seen = [start];

        while (current != field.Goal)
        {
            GridCell? next = field.GetNextCell(current);

            if (next is null || !seen.Add(next.Value))
            {
                break;
            }

            path.Add(next.Value);
            current = next.Value;
        }

        // Return the path only when it actually reaches the goal.
        return path[^1] == field.Goal ? path : [];
    }

    private static int Heuristic(GridCell a, GridCell b)
    {
        return Math.Abs(a.Row - b.Row) + Math.Abs(a.Column - b.Column);
    }

    private static List<GridCell> ReconstructPath(
        Dictionary<GridCell, GridCell?> cameFrom,
        GridCell start,
        GridCell end)
    {
        if (!cameFrom.ContainsKey(end))
        {
            return [];
        }

        List<GridCell> path = [];
        GridCell? current = end;

        while (current is not null)
        {
            path.Add(current.Value);
            current = cameFrom[current.Value];
        }

        path.Reverse();

        return path;
    }
}
