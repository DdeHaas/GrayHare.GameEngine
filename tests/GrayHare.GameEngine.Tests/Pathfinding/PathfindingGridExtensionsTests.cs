using GrayHare.GameEngine.Behaviors;
using GrayHare.GameEngine.Pathfinding;
using SFML.System;

namespace GrayHare.GameEngine.Tests.Pathfinding;

public sealed class PathfindingGridExtensionsTests
{
    private static readonly Vector2f Origin = new(0f, 0f);
    private const float CellSize = 10f;

    // ── Null validation ──────────────────────────────────────────────────────

    [Fact]
    public void ApplyWalls_ThrowsArgumentNullException_WhenGridIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => ((PathfindingGrid)null!).ApplyWalls([], CellSize, Origin));
    }

    [Fact]
    public void ApplyWalls_ThrowsArgumentNullException_WhenWallsIsNull()
    {
        var grid = new PathfindingGrid(5, 5);

        Assert.Throws<ArgumentNullException>(
            () => grid.ApplyWalls(null!, CellSize, Origin));
    }

    // ── No walls ─────────────────────────────────────────────────────────────

    [Fact]
    public void ApplyWalls_DoesNotBlockAnyCells_WhenWallListIsEmpty()
    {
        var grid = new PathfindingGrid(5, 5);

        grid.ApplyWalls([], CellSize, Origin);

        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 5; c++)
            {
                Assert.True(grid.IsWalkable(new GridCell(r, c)));
            }
        }
    }

    // ── Horizontal wall crossing cell edges ──────────────────────────────────

    [Fact]
    public void ApplyWalls_BlocksCell_WhenWallCrossesTopEdge()
    {
        var grid = new PathfindingGrid(5, 5);

        // Wall along y = 10 (top edge of row 1), spanning column 0–1
        Wall wall = new(new Vector2f(0f, 10f), new Vector2f(20f, 10f));

        grid.ApplyWalls([wall], CellSize, Origin);

        // The wall sits on the boundary between row 0 (bottom edge) and row 1 (top edge).
        // Cells in both rows that overlap the AABB should be blocked.
        Assert.True(grid.IsBlocked(new GridCell(1, 0)));
        Assert.True(grid.IsBlocked(new GridCell(1, 1)));
    }

    [Fact]
    public void ApplyWalls_BlocksCell_WhenWallCrossesLeftEdge()
    {
        var grid = new PathfindingGrid(5, 5);

        // Vertical wall along x = 10 (left edge of column 1), spanning row 0–1
        Wall wall = new(new Vector2f(10f, 0f), new Vector2f(10f, 20f));

        grid.ApplyWalls([wall], CellSize, Origin);

        Assert.True(grid.IsBlocked(new GridCell(0, 1)));
        Assert.True(grid.IsBlocked(new GridCell(1, 1)));
    }

    // ── Diagonal wall cutting through cells ──────────────────────────────────

    [Fact]
    public void ApplyWalls_BlocksCells_WhenDiagonalWallCrossesMultipleCells()
    {
        var grid = new PathfindingGrid(5, 5);

        // Diagonal from (0,0) to (30,30) — should cross cells (0,0), (1,1), (2,2)
        Wall wall = new(new Vector2f(0f, 0f), new Vector2f(30f, 30f));

        grid.ApplyWalls([wall], CellSize, Origin);

        Assert.True(grid.IsBlocked(new GridCell(0, 0)));
        Assert.True(grid.IsBlocked(new GridCell(1, 1)));
        Assert.True(grid.IsBlocked(new GridCell(2, 2)));
    }

    // ── Wall endpoint inside cell ─────────────────────────────────────────────

    [Fact]
    public void ApplyWalls_BlocksCell_WhenWallEndpointIsInsideCell()
    {
        var grid = new PathfindingGrid(5, 5);

        // Very short wall entirely inside cell (2,3): cell spans x=[30,40], y=[20,30]
        Wall wall = new(new Vector2f(32f, 22f), new Vector2f(38f, 28f));

        grid.ApplyWalls([wall], CellSize, Origin);

        Assert.True(grid.IsBlocked(new GridCell(2, 3)));
    }

    // ── Wall outside grid bounds ──────────────────────────────────────────────

    [Fact]
    public void ApplyWalls_DoesNotThrow_WhenWallIsCompletelyOutsideGrid()
    {
        var grid = new PathfindingGrid(5, 5);

        // Wall far outside the 5×5 grid (grid spans x=[0,50], y=[0,50])
        Wall wall = new(new Vector2f(200f, 200f), new Vector2f(300f, 300f));

        var exception = Record.Exception(() => grid.ApplyWalls([wall], CellSize, Origin));

        Assert.Null(exception);
    }

    [Fact]
    public void ApplyWalls_DoesNotBlockAnyCells_WhenWallIsCompletelyOutsideGrid()
    {
        var grid = new PathfindingGrid(5, 5);
        Wall wall = new(new Vector2f(200f, 200f), new Vector2f(300f, 300f));

        grid.ApplyWalls([wall], CellSize, Origin);

        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 5; c++)
            {
                Assert.True(grid.IsWalkable(new GridCell(r, c)));
            }
        }
    }

    // ── Does not clear pre-existing blocked cells ─────────────────────────────

    [Fact]
    public void ApplyWalls_PreservesExistingBlockedCells()
    {
        var grid = new PathfindingGrid(5, 5);
        grid.SetBlocked(new GridCell(4, 4), true);

        // Wall nowhere near (4,4)
        Wall wall = new(new Vector2f(0f, 0f), new Vector2f(10f, 0f));

        grid.ApplyWalls([wall], CellSize, Origin);

        Assert.True(grid.IsBlocked(new GridCell(4, 4)));
    }

    // ── Grid + pathfinding integration ───────────────────────────────────────

    [Fact]
    public void FindPath_ReturnsNoPath_WhenWallBlocksAllRoutes()
    {
        // 5×5 grid, wall seals column 2 completely (x = 20..30 at x=20)
        var grid = new PathfindingGrid(5, 5);

        // Vertical wall at x=20, from y=0 to y=50 — covers the full left edge of column 2
        Wall wall = new(new Vector2f(20f, 0f), new Vector2f(20f, 50f));
        grid.ApplyWalls([wall], CellSize, Origin);

        var result = PathFinder.FindPath(grid, new GridCell(2, 0), new GridCell(2, 4),
            PathfindingAlgorithm.BFS);

        Assert.False(result.Found);
    }

    [Fact]
    public void FindPath_FindsPath_WhenWallHasGap()
    {
        // 5×5 grid, partial wall along x=20, row 1-4 only — gap at row 0
        var grid = new PathfindingGrid(5, 5);

        // Wall from y=10 to y=50 (rows 1–4 only, row 0 is clear)
        Wall wall = new(new Vector2f(20f, 10f), new Vector2f(20f, 50f));
        grid.ApplyWalls([wall], CellSize, Origin);

        var result = PathFinder.FindPath(grid, new GridCell(2, 0), new GridCell(2, 4),
            PathfindingAlgorithm.BFS);

        Assert.True(result.Found);
        Assert.Equal(new GridCell(2, 0), result.Path[0]);
        Assert.Equal(new GridCell(2, 4), result.Path[^1]);
    }
}
