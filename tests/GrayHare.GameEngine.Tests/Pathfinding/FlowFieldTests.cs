using GrayHare.GameEngine.Pathfinding;

namespace GrayHare.GameEngine.Tests.Pathfinding;

public sealed class FlowFieldTests
{
    // ── Null validation ──────────────────────────────────────────────────────

    [Fact]
    public void BuildFlowField_ThrowsArgumentNullException_WhenGridIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => PathFinder.BuildFlowField(null!, new(0, 0)));
    }

    // ── Blocked goal ─────────────────────────────────────────────────────────

    [Fact]
    public void BuildFlowField_ReturnsEmptyField_WhenGoalIsBlocked()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell goal = new(2, 2);
        grid.SetBlocked(goal, true);

        var field = PathFinder.BuildFlowField(grid, goal);

        Assert.Empty(field.ReachableCells);
    }

    // ── Reachability ─────────────────────────────────────────────────────────

    [Fact]
    public void BuildFlowField_GoalIsAlwaysReachable()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell goal = new(2, 2);

        var field = PathFinder.BuildFlowField(grid, goal);

        Assert.True(field.IsReachable(goal));
    }

    [Fact]
    public void BuildFlowField_AllWalkableCellsAreReachable_OnOpenGrid()
    {
        var grid = new PathfindingGrid(4, 4);
        GridCell goal = new(0, 0);

        var field = PathFinder.BuildFlowField(grid, goal);

        for (int r = 0; r < grid.Rows; r++)
        {
            for (int c = 0; c < grid.Columns; c++)
            {
                Assert.True(field.IsReachable(new GridCell(r, c)));
            }
        }
    }

    [Fact]
    public void BuildFlowField_UnreachableCells_AreNotReachable()
    {
        var grid = new PathfindingGrid(5, 5);

        // Wall off column 2 completely.
        for (int r = 0; r < 5; r++)
        {
            grid.SetBlocked(new GridCell(r, 2), true);
        }

        GridCell goal = new(2, 4);
        var field = PathFinder.BuildFlowField(grid, goal);

        Assert.False(field.IsReachable(new GridCell(2, 0)));
        Assert.False(field.IsReachable(new GridCell(2, 1)));
    }

    // ── Direction / next cell ────────────────────────────────────────────────

    [Fact]
    public void GetNextCell_ReturnsNull_ForGoalCell()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell goal = new(2, 2);

        var field = PathFinder.BuildFlowField(grid, goal);

        Assert.Null(field.GetNextCell(goal));
    }

    [Fact]
    public void GetNextCell_ReturnsNull_ForUnreachableCell()
    {
        var grid = new PathfindingGrid(5, 5);
        for (int r = 0; r < 5; r++)
        {
            grid.SetBlocked(new GridCell(r, 2), true);
        }

        GridCell goal = new(2, 4);
        var field = PathFinder.BuildFlowField(grid, goal);

        Assert.Null(field.GetNextCell(new GridCell(2, 0)));
    }

    [Fact]
    public void GetNextCell_DirectAdjacent_PointsTowardGoal()
    {
        var grid = new PathfindingGrid(1, 5);
        GridCell goal = new(0, 4);

        var field = PathFinder.BuildFlowField(grid, goal);

        // Each cell in the single row should step one column to the right.
        for (int c = 0; c < 4; c++)
        {
            GridCell? next = field.GetNextCell(new GridCell(0, c));
            Assert.NotNull(next);
            Assert.Equal(new GridCell(0, c + 1), next!.Value);
        }
    }

    [Fact]
    public void GetNextCell_PathFollowing_ReachesGoal()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell start = new(0, 0);
        GridCell goal = new(4, 4);

        var field = PathFinder.BuildFlowField(grid, goal);

        GridCell current = start;
        HashSet<GridCell> visited = [current];
        int steps = 0;

        while (current != goal && steps < grid.Rows * grid.Columns)
        {
            GridCell? next = field.GetNextCell(current);
            Assert.NotNull(next);
            Assert.True(visited.Add(next!.Value), "Cycle detected in flow field path");
            current = next.Value;
            steps++;
        }

        Assert.Equal(goal, current);
    }

    [Fact]
    public void GetNextCell_NextCellIsAlwaysAdjacent()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell goal = new(2, 2);

        var field = PathFinder.BuildFlowField(grid, goal);

        foreach (GridCell cell in field.ReachableCells)
        {
            GridCell? next = field.GetNextCell(cell);

            if (next is null)
            {
                continue;
            }

            int dist = Math.Abs(next.Value.Row - cell.Row)
                     + Math.Abs(next.Value.Column - cell.Column);
            Assert.Equal(1, dist);
        }
    }

    // ── Integration with PathfindingResult ───────────────────────────────────

    [Fact]
    public void FlowField_PathLength_MatchesBFS_OnOpenGrid()
    {
        var grid = new PathfindingGrid(6, 6);
        GridCell start = new(0, 0);
        GridCell end = new(5, 5);

        var bfs = PathFinder.BreadthFirstSearch(grid, start, end);
        var ff = PathFinder.FindPath(grid, start, end, PathfindingAlgorithm.FlowField);

        Assert.Equal(bfs.Path.Count, ff.Path.Count);
    }
}
