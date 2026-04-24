using GrayHare.GameEngine.Pathfinding;

namespace GrayHare.GameEngine.Tests.Pathfinding;

public sealed class PathFinderTests
{
    // ── Null validation ──────────────────────────────────────────────────────

    [Fact]
    public void BreadthFirstSearch_ThrowsArgumentNullException_WhenGridIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => PathFinder.BreadthFirstSearch(null!, new(0, 0), new(1, 1)));
    }

    [Fact]
    public void DepthFirstSearch_ThrowsArgumentNullException_WhenGridIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => PathFinder.DepthFirstSearch(null!, new(0, 0), new(1, 1)));
    }

    [Fact]
    public void Dijkstra_ThrowsArgumentNullException_WhenGridIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => PathFinder.Dijkstra(null!, new(0, 0), new(1, 1)));
    }

    // ── BFS ──────────────────────────────────────────────────────────────────

    [Fact]
    public void BFS_FindsPath_InOpenGrid()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell start = new(0, 0);
        GridCell end = new(4, 4);

        var result = PathFinder.BreadthFirstSearch(grid, start, end);

        Assert.True(result.Found);
        Assert.Equal(start, result.Path[0]);
        Assert.Equal(end, result.Path[^1]);
    }

    [Fact]
    public void BFS_FindsShortestPath()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell start = new(0, 0);
        GridCell end = new(4, 4);

        var result = PathFinder.BreadthFirstSearch(grid, start, end);

        // Manhattan distance = 4 + 4 = 8 steps, so path length = 9 cells
        Assert.Equal(9, result.Path.Count);
    }

    [Fact]
    public void BFS_ReturnsEmptyPath_WhenStartIsBlocked()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell start = new(0, 0);
        grid.SetBlocked(start, true);

        var result = PathFinder.BreadthFirstSearch(grid, start, new(4, 4));

        Assert.False(result.Found);
        Assert.Empty(result.Path);
        Assert.Empty(result.Visited);
    }

    [Fact]
    public void BFS_ReturnsEmptyPath_WhenNoPathExists()
    {
        var grid = new PathfindingGrid(5, 5);

        // Wall off column 2
        for (int r = 0; r < 5; r++)
        {
            grid.SetBlocked(new GridCell(r, 2), true);
        }

        var result = PathFinder.BreadthFirstSearch(grid, new(2, 0), new(2, 4));

        Assert.False(result.Found);
        Assert.Empty(result.Path);
    }

    [Fact]
    public void BFS_VisitedContainsExploredCells_WhenNoPathExists()
    {
        var grid = new PathfindingGrid(5, 5);

        for (int r = 0; r < 5; r++)
        {
            grid.SetBlocked(new GridCell(r, 2), true);
        }

        var result = PathFinder.BreadthFirstSearch(grid, new(2, 0), new(2, 4));

        Assert.NotEmpty(result.Visited);
        Assert.Contains(new GridCell(2, 0), result.Visited);
        Assert.Contains(new GridCell(2, 1), result.Visited);
    }

    [Fact]
    public void BFS_ReturnsSingleCellPath_WhenStartEqualsEnd()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell cell = new(2, 2);

        var result = PathFinder.BreadthFirstSearch(grid, cell, cell);

        Assert.True(result.Found);
        Assert.Single(result.Path);
        Assert.Equal(cell, result.Path[0]);
    }

    [Fact]
    public void BFS_PathIsConnected()
    {
        var grid = new PathfindingGrid(5, 5);
        var result = PathFinder.BreadthFirstSearch(grid, new(0, 0), new(4, 4));

        for (int i = 1; i < result.Path.Count; i++)
        {
            GridCell prev = result.Path[i - 1];
            GridCell curr = result.Path[i];
            int dist = Math.Abs(prev.Row - curr.Row) + Math.Abs(prev.Column - curr.Column);
            Assert.Equal(1, dist);
        }
    }

    // ── DFS ──────────────────────────────────────────────────────────────────

    [Fact]
    public void DFS_FindsPath_InOpenGrid()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell start = new(0, 0);
        GridCell end = new(4, 4);

        var result = PathFinder.DepthFirstSearch(grid, start, end);

        Assert.True(result.Found);
        Assert.Equal(start, result.Path[0]);
        Assert.Equal(end, result.Path[^1]);
    }

    [Fact]
    public void DFS_ReturnsEmptyPath_WhenStartIsBlocked()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell start = new(0, 0);
        grid.SetBlocked(start, true);

        var result = PathFinder.DepthFirstSearch(grid, start, new(4, 4));

        Assert.False(result.Found);
        Assert.Empty(result.Path);
    }

    [Fact]
    public void DFS_ReturnsEmptyPath_WhenNoPathExists()
    {
        var grid = new PathfindingGrid(5, 5);

        for (int r = 0; r < 5; r++)
        {
            grid.SetBlocked(new GridCell(r, 2), true);
        }

        var result = PathFinder.DepthFirstSearch(grid, new(2, 0), new(2, 4));

        Assert.False(result.Found);
        Assert.Empty(result.Path);
    }

    [Fact]
    public void DFS_ReturnsSingleCellPath_WhenStartEqualsEnd()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell cell = new(2, 2);

        var result = PathFinder.DepthFirstSearch(grid, cell, cell);

        Assert.True(result.Found);
        Assert.Single(result.Path);
        Assert.Equal(cell, result.Path[0]);
    }

    [Fact]
    public void DFS_PathIsConnected()
    {
        var grid = new PathfindingGrid(5, 5);
        var result = PathFinder.DepthFirstSearch(grid, new(0, 0), new(4, 4));

        for (int i = 1; i < result.Path.Count; i++)
        {
            GridCell prev = result.Path[i - 1];
            GridCell curr = result.Path[i];
            int dist = Math.Abs(prev.Row - curr.Row) + Math.Abs(prev.Column - curr.Column);
            Assert.Equal(1, dist);
        }
    }

    // ── Dijkstra ─────────────────────────────────────────────────────────────

    [Fact]
    public void Dijkstra_FindsPath_InOpenGrid()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell start = new(0, 0);
        GridCell end = new(4, 4);

        var result = PathFinder.Dijkstra(grid, start, end);

        Assert.True(result.Found);
        Assert.Equal(start, result.Path[0]);
        Assert.Equal(end, result.Path[^1]);
    }

    [Fact]
    public void Dijkstra_FindsShortestPath()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell start = new(0, 0);
        GridCell end = new(4, 4);

        var result = PathFinder.Dijkstra(grid, start, end);

        Assert.Equal(9, result.Path.Count);
    }

    [Fact]
    public void Dijkstra_ReturnsEmptyPath_WhenStartIsBlocked()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell start = new(0, 0);
        grid.SetBlocked(start, true);

        var result = PathFinder.Dijkstra(grid, start, new(4, 4));

        Assert.False(result.Found);
        Assert.Empty(result.Path);
    }

    [Fact]
    public void Dijkstra_ReturnsEmptyPath_WhenNoPathExists()
    {
        var grid = new PathfindingGrid(5, 5);

        for (int r = 0; r < 5; r++)
        {
            grid.SetBlocked(new GridCell(r, 2), true);
        }

        var result = PathFinder.Dijkstra(grid, new(2, 0), new(2, 4));

        Assert.False(result.Found);
        Assert.Empty(result.Path);
    }

    [Fact]
    public void Dijkstra_ReturnsSingleCellPath_WhenStartEqualsEnd()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell cell = new(2, 2);

        var result = PathFinder.Dijkstra(grid, cell, cell);

        Assert.True(result.Found);
        Assert.Single(result.Path);
        Assert.Equal(cell, result.Path[0]);
    }

    // ── FindPath dispatch ────────────────────────────────────────────────────

    [Theory]
    [InlineData(PathfindingAlgorithm.BFS)]
    [InlineData(PathfindingAlgorithm.DFS)]
    [InlineData(PathfindingAlgorithm.Dijkstra)]
    public void FindPath_ReturnsResult_ForEachAlgorithm(PathfindingAlgorithm algorithm)
    {
        var grid = new PathfindingGrid(5, 5);

        var result = PathFinder.FindPath(grid, new(0, 0), new(4, 4), algorithm);

        Assert.True(result.Found);
        Assert.NotEmpty(result.Path);
    }

    [Fact]
    public void FindPath_ThrowsArgumentNullException_WhenGridIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => PathFinder.FindPath(null!, new(0, 0), new(1, 1), PathfindingAlgorithm.BFS));
    }

    // ── Path around obstacles ────────────────────────────────────────────────

    [Theory]
    [InlineData(PathfindingAlgorithm.BFS)]
    [InlineData(PathfindingAlgorithm.DFS)]
    [InlineData(PathfindingAlgorithm.Dijkstra)]
    public void FindPath_NavigatesAroundObstacle(PathfindingAlgorithm algorithm)
    {
        // 5×5 grid with a wall in column 2, but a gap at row 0
        var grid = new PathfindingGrid(5, 5);
        for (int r = 1; r < 5; r++)
        {
            grid.SetBlocked(new GridCell(r, 2), true);
        }

        var result = PathFinder.FindPath(grid, new(2, 0), new(2, 4), algorithm);

        Assert.True(result.Found);
        Assert.Equal(new GridCell(2, 0), result.Path[0]);
        Assert.Equal(new GridCell(2, 4), result.Path[^1]);
    }

    // ── A* ───────────────────────────────────────────────────────────────────

    [Fact]
    public void AStar_ThrowsArgumentNullException_WhenGridIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => PathFinder.AStar(null!, new(0, 0), new(1, 1)));
    }

    [Fact]
    public void AStar_FindsPath_InOpenGrid()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell start = new(0, 0);
        GridCell end = new(4, 4);

        var result = PathFinder.AStar(grid, start, end);

        Assert.True(result.Found);
        Assert.Equal(start, result.Path[0]);
        Assert.Equal(end, result.Path[^1]);
    }

    [Fact]
    public void AStar_FindsShortestPath()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell start = new(0, 0);
        GridCell end = new(4, 4);

        var result = PathFinder.AStar(grid, start, end);

        Assert.Equal(9, result.Path.Count);
    }

    [Fact]
    public void AStar_ReturnsEmptyPath_WhenStartIsBlocked()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell start = new(0, 0);
        grid.SetBlocked(start, true);

        var result = PathFinder.AStar(grid, start, new(4, 4));

        Assert.False(result.Found);
        Assert.Empty(result.Path);
        Assert.Empty(result.Visited);
    }

    [Fact]
    public void AStar_ReturnsEmptyPath_WhenNoPathExists()
    {
        var grid = new PathfindingGrid(5, 5);
        for (int r = 0; r < 5; r++)
        {
            grid.SetBlocked(new GridCell(r, 2), true);
        }

        var result = PathFinder.AStar(grid, new(2, 0), new(2, 4));

        Assert.False(result.Found);
        Assert.Empty(result.Path);
    }

    [Fact]
    public void AStar_ReturnsSingleCellPath_WhenStartEqualsEnd()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell cell = new(2, 2);

        var result = PathFinder.AStar(grid, cell, cell);

        Assert.True(result.Found);
        Assert.Single(result.Path);
        Assert.Equal(cell, result.Path[0]);
    }

    [Fact]
    public void AStar_PathIsConnected()
    {
        var grid = new PathfindingGrid(5, 5);
        var result = PathFinder.AStar(grid, new(0, 0), new(4, 4));

        for (int i = 1; i < result.Path.Count; i++)
        {
            GridCell prev = result.Path[i - 1];
            GridCell curr = result.Path[i];
            int dist = Math.Abs(prev.Row - curr.Row) + Math.Abs(prev.Column - curr.Column);
            Assert.Equal(1, dist);
        }
    }

    [Fact]
    public void AStar_VisitsFewerCellsThanBFS_OnOpenGrid()
    {
        // A* focuses search toward the goal; on a 20×20 open grid it should
        // explore fewer cells than BFS (which fans out uniformly).
        var grid = new PathfindingGrid(20, 20);
        GridCell start = new(0, 0);
        GridCell end = new(19, 19);

        var bfsResult = PathFinder.BreadthFirstSearch(grid, start, end);
        var astarResult = PathFinder.AStar(grid, start, end);

        Assert.True(astarResult.Visited.Count < bfsResult.Visited.Count);
    }

    // ── FlowField via FindPath ────────────────────────────────────────────────

    [Fact]
    public void FindPath_FlowField_FindsPath_InOpenGrid()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell start = new(0, 0);
        GridCell end = new(4, 4);

        var result = PathFinder.FindPath(grid, start, end, PathfindingAlgorithm.FlowField);

        Assert.True(result.Found);
        Assert.Equal(start, result.Path[0]);
        Assert.Equal(end, result.Path[^1]);
    }

    [Fact]
    public void FindPath_FlowField_FindsShortestPath()
    {
        var grid = new PathfindingGrid(5, 5);

        var result = PathFinder.FindPath(grid, new(0, 0), new(4, 4), PathfindingAlgorithm.FlowField);

        Assert.Equal(9, result.Path.Count);
    }

    [Fact]
    public void FindPath_FlowField_ReturnsEmptyPath_WhenStartIsBlocked()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell start = new(0, 0);
        grid.SetBlocked(start, true);

        var result = PathFinder.FindPath(grid, start, new(4, 4), PathfindingAlgorithm.FlowField);

        Assert.False(result.Found);
        Assert.Empty(result.Path);
    }

    [Fact]
    public void FindPath_FlowField_ReturnsEmptyPath_WhenNoPathExists()
    {
        var grid = new PathfindingGrid(5, 5);
        for (int r = 0; r < 5; r++)
        {
            grid.SetBlocked(new GridCell(r, 2), true);
        }

        var result = PathFinder.FindPath(grid, new(2, 0), new(2, 4), PathfindingAlgorithm.FlowField);

        Assert.False(result.Found);
        Assert.Empty(result.Path);
    }

    // ── FindPath dispatch (extended) ─────────────────────────────────────────

    [Theory]
    [InlineData(PathfindingAlgorithm.BFS)]
    [InlineData(PathfindingAlgorithm.DFS)]
    [InlineData(PathfindingAlgorithm.Dijkstra)]
    [InlineData(PathfindingAlgorithm.AStar)]
    [InlineData(PathfindingAlgorithm.FlowField)]
    public void FindPath_AllAlgorithms_FindPath_InOpenGrid(PathfindingAlgorithm algorithm)
    {
        var grid = new PathfindingGrid(5, 5);

        var result = PathFinder.FindPath(grid, new(0, 0), new(4, 4), algorithm);

        Assert.True(result.Found);
        Assert.NotEmpty(result.Path);
    }
}
