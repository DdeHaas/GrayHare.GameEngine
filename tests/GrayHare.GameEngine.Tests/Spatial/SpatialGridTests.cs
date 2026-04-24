using GrayHare.GameEngine.Spatial;
using SFML.System;

namespace GrayHare.GameEngine.Tests.Spatial;

public sealed class SpatialGridTests
{
    // ── Construction ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ThrowsArgumentOutOfRangeException_WhenCellSizeIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpatialGrid<object>(0f));
    }

    [Fact]
    public void Constructor_ThrowsArgumentOutOfRangeException_WhenCellSizeIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpatialGrid<object>(-10f));
    }

    [Fact]
    public void Constructor_SetsCellSize()
    {
        var grid = new SpatialGrid<object>(64f);

        Assert.Equal(64f, grid.CellSize);
    }

    // ── Add ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Add_ThrowsArgumentNullException_WhenItemIsNull()
    {
        var grid = new SpatialGrid<object>(100f);

        Assert.Throws<ArgumentNullException>(() => grid.Add(null!, new Vector2f(0f, 0f)));
    }

    [Fact]
    public void Add_IncrementsCount()
    {
        var grid = new SpatialGrid<object>(100f);

        grid.Add(new object(), new Vector2f(10f, 20f));
        grid.Add(new object(), new Vector2f(30f, 40f));

        Assert.Equal(2, grid.Count);
    }

    // ── Clear ────────────────────────────────────────────────────────────────

    [Fact]
    public void Clear_ResetsCountToZero()
    {
        var grid = new SpatialGrid<object>(100f);
        grid.Add(new object(), new Vector2f(0f, 0f));
        grid.Add(new object(), new Vector2f(50f, 50f));

        grid.Clear();

        Assert.Equal(0, grid.Count);
    }

    [Fact]
    public void Clear_RemovesAllItemsFromQueries()
    {
        var grid = new SpatialGrid<object>(100f);
        grid.Add(new object(), new Vector2f(0f, 0f));

        grid.Clear();

        var results = new List<object>();
        int count = grid.FindNeighbors(new Vector2f(0f, 0f), 1000f, results);

        Assert.Equal(0, count);
        Assert.Empty(results);
    }

    // ── FindNeighbors — argument validation ──────────────────────────────────

    [Fact]
    public void FindNeighbors_ThrowsArgumentNullException_WhenResultsIsNull()
    {
        var grid = new SpatialGrid<object>(100f);

        Assert.Throws<ArgumentNullException>(
            () => grid.FindNeighbors(new Vector2f(0f, 0f), 50f, null!));
    }

    [Fact]
    public void FindNeighbors_ThrowsArgumentOutOfRangeException_WhenRadiusIsNegative()
    {
        var grid = new SpatialGrid<object>(100f);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => grid.FindNeighbors(new Vector2f(0f, 0f), -1f, []));
    }

    // ── FindNeighbors — basic queries ────────────────────────────────────────

    [Fact]
    public void FindNeighbors_ReturnsEmpty_WhenGridIsEmpty()
    {
        var grid = new SpatialGrid<object>(100f);
        var results = new List<object>();

        int count = grid.FindNeighbors(new Vector2f(0f, 0f), 50f, results);

        Assert.Equal(0, count);
        Assert.Empty(results);
    }

    [Fact]
    public void FindNeighbors_ReturnsItemWithinRadius()
    {
        var grid = new SpatialGrid<object>(100f);
        var item = new object();
        grid.Add(item, new Vector2f(10f, 0f));

        var results = new List<object>();
        int count = grid.FindNeighbors(new Vector2f(0f, 0f), 50f, results);

        Assert.Equal(1, count);
        Assert.Contains(item, results);
    }

    [Fact]
    public void FindNeighbors_ExcludesItemOutsideRadius()
    {
        var grid = new SpatialGrid<object>(100f);
        grid.Add(new object(), new Vector2f(200f, 0f));

        var results = new List<object>();
        int count = grid.FindNeighbors(new Vector2f(0f, 0f), 50f, results);

        Assert.Equal(0, count);
        Assert.Empty(results);
    }

    [Fact]
    public void FindNeighbors_ExcludesSpecifiedItem()
    {
        var grid = new SpatialGrid<object>(100f);
        var self = new object();
        var neighbor = new object();
        grid.Add(self, new Vector2f(0f, 0f));
        grid.Add(neighbor, new Vector2f(10f, 0f));

        var results = new List<object>();
        int count = grid.FindNeighbors(new Vector2f(0f, 0f), 50f, results, exclude: self);

        Assert.Equal(1, count);
        Assert.DoesNotContain(self, results);
        Assert.Contains(neighbor, results);
    }

    [Fact]
    public void FindNeighbors_ClearsResultsBeforeFilling()
    {
        var grid = new SpatialGrid<object>(100f);
        grid.Add(new object(), new Vector2f(10f, 0f));

        var results = new List<object> { new(), new(), new() };
        grid.FindNeighbors(new Vector2f(0f, 0f), 50f, results);

        Assert.Single(results);
    }

    // ── FindNeighbors — cross-cell queries ───────────────────────────────────

    [Fact]
    public void FindNeighbors_FindsItemsAcrossCellBoundaries()
    {
        var grid = new SpatialGrid<object>(50f);
        var item1 = new object();
        var item2 = new object();

        // Items in different cells but within radius of a query point
        grid.Add(item1, new Vector2f(45f, 0f));
        grid.Add(item2, new Vector2f(55f, 0f));

        var results = new List<object>();
        int count = grid.FindNeighbors(new Vector2f(50f, 0f), 20f, results);

        Assert.Equal(2, count);
        Assert.Contains(item1, results);
        Assert.Contains(item2, results);
    }

    [Fact]
    public void FindNeighbors_FindsItemsInDiagonalCells()
    {
        var grid = new SpatialGrid<object>(50f);
        var item = new object();

        grid.Add(item, new Vector2f(60f, 60f));

        var results = new List<object>();
        int count = grid.FindNeighbors(new Vector2f(50f, 50f), 20f, results);

        Assert.Equal(1, count);
        Assert.Contains(item, results);
    }

    // ── FindNeighbors — negative coordinates ─────────────────────────────────

    [Fact]
    public void FindNeighbors_WorksWithNegativeCoordinates()
    {
        var grid = new SpatialGrid<object>(100f);
        var item = new object();
        grid.Add(item, new Vector2f(-50f, -30f));

        var results = new List<object>();
        int count = grid.FindNeighbors(new Vector2f(-40f, -20f), 25f, results);

        Assert.Equal(1, count);
        Assert.Contains(item, results);
    }

    [Fact]
    public void FindNeighbors_WorksAcrossOrigin()
    {
        var grid = new SpatialGrid<object>(100f);
        var neg = new object();
        var pos = new object();
        grid.Add(neg, new Vector2f(-5f, 0f));
        grid.Add(pos, new Vector2f(5f, 0f));

        var results = new List<object>();
        int count = grid.FindNeighbors(new Vector2f(0f, 0f), 10f, results);

        Assert.Equal(2, count);
    }

    // ── FindNeighbors — boundary precision ───────────────────────────────────

    [Fact]
    public void FindNeighbors_IncludesItemExactlyOnRadiusBoundary()
    {
        var grid = new SpatialGrid<object>(100f);
        var item = new object();
        grid.Add(item, new Vector2f(50f, 0f));

        var results = new List<object>();
        int count = grid.FindNeighbors(new Vector2f(0f, 0f), 50f, results);

        Assert.Equal(1, count);
    }

    [Fact]
    public void FindNeighbors_ExcludesItemJustBeyondRadius()
    {
        var grid = new SpatialGrid<object>(100f);
        grid.Add(new object(), new Vector2f(50.01f, 0f));

        var results = new List<object>();
        int count = grid.FindNeighbors(new Vector2f(0f, 0f), 50f, results);

        Assert.Equal(0, count);
    }

    [Fact]
    public void FindNeighbors_WithZeroRadius_OnlyFindsCoincidentItems()
    {
        var grid = new SpatialGrid<object>(100f);
        var atOrigin = new object();
        var nearby = new object();
        grid.Add(atOrigin, new Vector2f(0f, 0f));
        grid.Add(nearby, new Vector2f(1f, 0f));

        var results = new List<object>();
        int count = grid.FindNeighbors(new Vector2f(0f, 0f), 0f, results);

        Assert.Equal(1, count);
        Assert.Contains(atOrigin, results);
    }

    // ── FindNeighbors — multiple items ───────────────────────────────────────

    [Fact]
    public void FindNeighbors_FindsCorrectSubset_WhenItemsAreSpread()
    {
        var grid = new SpatialGrid<object>(50f);
        var close1 = new object();
        var close2 = new object();
        var far = new object();
        grid.Add(close1, new Vector2f(20f, 20f));
        grid.Add(close2, new Vector2f(-15f, 10f));
        grid.Add(far, new Vector2f(300f, 300f));

        var results = new List<object>();
        int count = grid.FindNeighbors(new Vector2f(0f, 0f), 50f, results);

        Assert.Equal(2, count);
        Assert.Contains(close1, results);
        Assert.Contains(close2, results);
        Assert.DoesNotContain(far, results);
    }

    // ── Clear + re-add (rebuild pattern) ─────────────────────────────────────

    [Fact]
    public void ClearAndRebuild_ReflectsNewPositions()
    {
        var grid = new SpatialGrid<object>(100f);
        var item = new object();
        grid.Add(item, new Vector2f(0f, 0f));

        grid.Clear();
        grid.Add(item, new Vector2f(500f, 500f));

        var nearOrigin = new List<object>();
        grid.FindNeighbors(new Vector2f(0f, 0f), 50f, nearOrigin);

        var nearNew = new List<object>();
        grid.FindNeighbors(new Vector2f(500f, 500f), 50f, nearNew);

        Assert.Empty(nearOrigin);
        Assert.Single(nearNew);
        Assert.Contains(item, nearNew);
    }

    // ── List reuse across frames (pool behavior) ─────────────────────────────

    [Fact]
    public void ClearAndRebuild_DoesNotThrow_AfterMultipleRounds()
    {
        var grid = new SpatialGrid<object>(50f);
        var results = new List<object>();

        for (int round = 0; round < 10; round++)
        {
            grid.Clear();
            for (int i = 0; i < 100; i++)
            {
                grid.Add(new object(), new Vector2f(i * 5f, i * 3f));
            }

            grid.FindNeighbors(new Vector2f(50f, 30f), 30f, results);
        }

        Assert.True(grid.Count == 100);
        Assert.True(results.Count >= 0);
    }

    // ── EnumerateCells ───────────────────────────────────────────────────────

    [Fact]
    public void EnumerateCells_ReturnsEmpty_WhenGridIsEmpty()
    {
        var grid = new SpatialGrid<object>(100f);

        Assert.Empty(grid.EnumerateCells());
    }

    [Fact]
    public void EnumerateCells_ReturnsOneCell_WhenSingleItemAdded()
    {
        var grid = new SpatialGrid<object>(100f);
        grid.Add(new object(), new Vector2f(10f, 20f));

        var cells = grid.EnumerateCells().ToArray();

        Assert.Single(cells);
        Assert.Equal(1, cells[0].ItemCount);
    }

    [Fact]
    public void EnumerateCells_ItemCount_ReflectsItemsInSameCell()
    {
        var grid = new SpatialGrid<object>(100f);

        // Both items fall in the same cell (cell size 100, both within [0,100)x[0,100)).
        grid.Add(new object(), new Vector2f(10f, 10f));
        grid.Add(new object(), new Vector2f(20f, 30f));

        var cells = grid.EnumerateCells().ToArray();

        Assert.Single(cells);
        Assert.Equal(2, cells[0].ItemCount);
    }
}
