using GrayHare.GameEngine.Pathfinding;

namespace GrayHare.GameEngine.Tests.Pathfinding;

public sealed class PathfindingGridTests
{
    // ── Construction ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsRowsAndColumns()
    {
        var grid = new PathfindingGrid(10, 20);

        Assert.Equal(10, grid.Rows);
        Assert.Equal(20, grid.Columns);
    }

    [Fact]
    public void Constructor_ThrowsArgumentOutOfRangeException_WhenRowsIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PathfindingGrid(0, 5));
    }

    [Fact]
    public void Constructor_ThrowsArgumentOutOfRangeException_WhenColumnsIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PathfindingGrid(5, 0));
    }

    [Fact]
    public void Constructor_ThrowsArgumentOutOfRangeException_WhenRowsIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PathfindingGrid(-1, 5));
    }

    [Fact]
    public void Constructor_ThrowsArgumentOutOfRangeException_WhenColumnsIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PathfindingGrid(5, -3));
    }

    // ── IsInBounds ───────────────────────────────────────────────────────────

    [Fact]
    public void IsInBounds_ReturnsTrue_ForValidCell()
    {
        var grid = new PathfindingGrid(5, 5);

        Assert.True(grid.IsInBounds(new GridCell(0, 0)));
        Assert.True(grid.IsInBounds(new GridCell(4, 4)));
        Assert.True(grid.IsInBounds(new GridCell(2, 3)));
    }

    [Fact]
    public void IsInBounds_ReturnsFalse_ForNegativeIndices()
    {
        var grid = new PathfindingGrid(5, 5);

        Assert.False(grid.IsInBounds(new GridCell(-1, 0)));
        Assert.False(grid.IsInBounds(new GridCell(0, -1)));
    }

    [Fact]
    public void IsInBounds_ReturnsFalse_ForExceedingIndices()
    {
        var grid = new PathfindingGrid(5, 5);

        Assert.False(grid.IsInBounds(new GridCell(5, 0)));
        Assert.False(grid.IsInBounds(new GridCell(0, 5)));
    }

    // ── IsWalkable / IsBlocked / SetBlocked ──────────────────────────────────

    [Fact]
    public void NewGrid_AllCellsAreWalkable()
    {
        var grid = new PathfindingGrid(3, 3);

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                Assert.True(grid.IsWalkable(new GridCell(r, c)));
                Assert.False(grid.IsBlocked(new GridCell(r, c)));
            }
        }
    }

    [Fact]
    public void SetBlocked_MakesCellBlocked()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell cell = new(2, 3);

        grid.SetBlocked(cell, true);

        Assert.True(grid.IsBlocked(cell));
        Assert.False(grid.IsWalkable(cell));
    }

    [Fact]
    public void SetBlocked_False_MakesCellWalkableAgain()
    {
        var grid = new PathfindingGrid(5, 5);
        GridCell cell = new(2, 3);

        grid.SetBlocked(cell, true);
        grid.SetBlocked(cell, false);

        Assert.False(grid.IsBlocked(cell));
        Assert.True(grid.IsWalkable(cell));
    }

    [Fact]
    public void SetBlocked_ThrowsArgumentOutOfRangeException_WhenOutOfBounds()
    {
        var grid = new PathfindingGrid(5, 5);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => grid.SetBlocked(new GridCell(5, 0), true));
    }

    [Fact]
    public void IsWalkable_ReturnsFalse_ForOutOfBoundsCell()
    {
        var grid = new PathfindingGrid(5, 5);

        Assert.False(grid.IsWalkable(new GridCell(-1, 0)));
        Assert.False(grid.IsWalkable(new GridCell(0, 5)));
    }

    [Fact]
    public void IsBlocked_ReturnsFalse_ForOutOfBoundsCell()
    {
        var grid = new PathfindingGrid(5, 5);

        Assert.False(grid.IsBlocked(new GridCell(-1, 0)));
        Assert.False(grid.IsBlocked(new GridCell(0, 5)));
    }

    // ── Clear ────────────────────────────────────────────────────────────────

    [Fact]
    public void Clear_ResetsAllCellsToWalkable()
    {
        var grid = new PathfindingGrid(3, 3);
        grid.SetBlocked(new GridCell(0, 0), true);
        grid.SetBlocked(new GridCell(1, 1), true);
        grid.SetBlocked(new GridCell(2, 2), true);

        grid.Clear();

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                Assert.True(grid.IsWalkable(new GridCell(r, c)));
            }
        }
    }

    // ── GetWalkableNeighbors ─────────────────────────────────────────────────

    [Fact]
    public void GetWalkableNeighbors_ReturnsFourNeighbors_ForCenterCell()
    {
        var grid = new PathfindingGrid(5, 5);
        var results = new List<GridCell>();

        grid.GetWalkableNeighbors(new GridCell(2, 2), results);

        Assert.Equal(4, results.Count);
        Assert.Contains(new GridCell(2, 3), results);
        Assert.Contains(new GridCell(3, 2), results);
        Assert.Contains(new GridCell(2, 1), results);
        Assert.Contains(new GridCell(1, 2), results);
    }

    [Fact]
    public void GetWalkableNeighbors_ExcludesBlockedNeighbors()
    {
        var grid = new PathfindingGrid(5, 5);
        grid.SetBlocked(new GridCell(2, 3), true);
        grid.SetBlocked(new GridCell(1, 2), true);
        var results = new List<GridCell>();

        grid.GetWalkableNeighbors(new GridCell(2, 2), results);

        Assert.Equal(2, results.Count);
        Assert.Contains(new GridCell(3, 2), results);
        Assert.Contains(new GridCell(2, 1), results);
    }

    [Fact]
    public void GetWalkableNeighbors_ExcludesOutOfBounds_ForCornerCell()
    {
        var grid = new PathfindingGrid(5, 5);
        var results = new List<GridCell>();

        grid.GetWalkableNeighbors(new GridCell(0, 0), results);

        Assert.Equal(2, results.Count);
        Assert.Contains(new GridCell(0, 1), results);
        Assert.Contains(new GridCell(1, 0), results);
    }

    [Fact]
    public void GetWalkableNeighbors_ClearsResultsBeforeFilling()
    {
        var grid = new PathfindingGrid(5, 5);
        var results = new List<GridCell> { new(9, 9), new(8, 8) };

        grid.GetWalkableNeighbors(new GridCell(0, 0), results);

        Assert.Equal(2, results.Count);
        Assert.DoesNotContain(new GridCell(9, 9), results);
    }

    [Fact]
    public void GetWalkableNeighbors_ThrowsArgumentNullException_WhenResultsIsNull()
    {
        var grid = new PathfindingGrid(5, 5);

        Assert.Throws<ArgumentNullException>(
            () => grid.GetWalkableNeighbors(new GridCell(0, 0), null!));
    }
}
