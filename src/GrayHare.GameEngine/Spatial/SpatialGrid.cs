using SFML.System;

namespace GrayHare.GameEngine.Spatial;

/// <summary>
/// A grid-based spatial hash that partitions 2D space into fixed-size cells
/// for fast radius-based neighbor queries.
/// </summary>
/// <remarks>
/// <para>
/// Designed for a per-frame rebuild workflow: call <see cref="Clear"/>, insert
/// every item with <see cref="Add"/>, then issue <see cref="FindNeighbors"/>
/// queries. Cell lists are pooled internally to avoid per-frame allocations.
/// </para>
/// <para>
/// Choose a <see cref="CellSize"/> close to the largest query radius used in
/// practice. Smaller cells reduce the number of items tested per query but
/// increase the number of cells to visit; larger cells do the opposite.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of item stored in the grid.</typeparam>
/// <example>
/// <code>
/// var grid = new SpatialGrid&lt;IMovableGameObject&gt;(cellSize: 120f);
/// var neighbors = new List&lt;IMovableGameObject&gt;();
///
/// // Each frame:
/// grid.Clear();
/// foreach (var agent in allAgents)
///     grid.Add(agent, agent.Position);
///
/// foreach (var agent in allAgents)
/// {
///     grid.FindNeighbors(agent.Position, neighborhoodRadius, neighbors, exclude: agent);
///     Vector2f sep = steering.Separation(neighbors, separationRadius);
/// }
/// </code>
/// </example>
/// <remarks>This type is not thread-safe. Access all members from the main thread only.</remarks>
public sealed class SpatialGrid<T> where T : class
{
    private readonly record struct Entry(T Item, Vector2f Position);

    private readonly float _cellSize;
    private readonly float _inverseCellSize;
    private readonly Dictionary<long, List<Entry>> _cells = [];
    private readonly Stack<List<Entry>> _listPool = new();
    private int _count;

    /// <summary>
    /// Initializes a new <see cref="SpatialGrid{T}"/> with the specified cell size.
    /// </summary>
    /// <param name="cellSize">
    /// The width and height of each grid cell. Must be greater than zero.
    /// A good default is the largest neighborhood radius used for queries.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="cellSize"/> is less than or equal to zero.
    /// </exception>
    public SpatialGrid(float cellSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(cellSize, 0f);

        _cellSize = cellSize;
        _inverseCellSize = 1f / cellSize;
    }

    /// <summary>The width and height of each grid cell.</summary>
    public float CellSize => _cellSize;

    /// <summary>The number of items currently stored in the grid.</summary>
    public int Count => _count;

    /// <summary>
    /// Removes all items from the grid. Cell lists are returned to an internal
    /// pool so that subsequent <see cref="Add"/> calls can reuse them without
    /// allocating new lists.
    /// </summary>
    public void Clear()
    {
        foreach (List<Entry> cell in _cells.Values)
        {
            cell.Clear();
            _listPool.Push(cell);
        }

        _cells.Clear();
        _count = 0;
    }

    /// <summary>
    /// Inserts <paramref name="item"/> into the grid at the given
    /// <paramref name="position"/>.
    /// </summary>
    /// <param name="item">The item to store.</param>
    /// <param name="position">World-space position of the item.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="item"/> is <see langword="null"/>.
    /// </exception>
    public void Add(T item, Vector2f position)
    {
        ArgumentNullException.ThrowIfNull(item);

        long key = CellKey(ToCellCoord(position.X), ToCellCoord(position.Y));

        if (!_cells.TryGetValue(key, out List<Entry>? list))
        {
            list = _listPool.Count > 0 ? _listPool.Pop() : [];
            _cells[key] = list;
        }

        list.Add(new Entry(item, position));
        _count++;
    }

    /// <summary>
    /// Finds all items within <paramref name="radius"/> of
    /// <paramref name="position"/> and appends them to <paramref name="results"/>.
    /// </summary>
    /// <param name="position">The center of the search circle.</param>
    /// <param name="radius">
    /// The search radius. Must be greater than or equal to zero.
    /// </param>
    /// <param name="results">
    /// A caller-owned list that is cleared and filled with the matching items.
    /// Reuse this list across frames to avoid allocations.
    /// </param>
    /// <param name="exclude">
    /// An optional item to skip (typically the querying agent itself).
    /// Compared by reference equality.
    /// </param>
    /// <returns>The number of neighbors found.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="results"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="radius"/> is negative.
    /// </exception>
    public int FindNeighbors(Vector2f position, float radius, List<T> results, T? exclude = null)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentOutOfRangeException.ThrowIfNegative(radius);

        results.Clear();

        float radiusSquared = radius * radius;

        int minCellX = ToCellCoord(position.X - radius);
        int maxCellX = ToCellCoord(position.X + radius);
        int minCellY = ToCellCoord(position.Y - radius);
        int maxCellY = ToCellCoord(position.Y + radius);

        for (int cellX = minCellX; cellX <= maxCellX; cellX++)
        {
            for (int cellY = minCellY; cellY <= maxCellY; cellY++)
            {
                long key = CellKey(cellX, cellY);

                if (!_cells.TryGetValue(key, out List<Entry>? cell))
                {
                    continue;
                }

                for (int i = 0; i < cell.Count; i++)
                {
                    Entry entry = cell[i];

                    if (ReferenceEquals(entry.Item, exclude))
                    {
                        continue;
                    }

                    float dx = entry.Position.X - position.X;
                    float dy = entry.Position.Y - position.Y;

                    if (dx * dx + dy * dy <= radiusSquared)
                    {
                        results.Add(entry.Item);
                    }
                }
            }
        }

        return results.Count;
    }

    /// <summary>
    /// Enumerates every occupied cell, returning its world-space origin and
    /// the number of items it contains. Useful for debug visualization.
    /// </summary>
    /// <returns>
    /// A sequence of tuples where <c>CellOrigin</c> is the top-left corner
    /// of the cell in world space and <c>ItemCount</c> is the number of items
    /// stored in that cell.
    /// </returns>
    public IEnumerable<(Vector2f CellOrigin, int ItemCount)> EnumerateCells()
    {
        foreach (KeyValuePair<long, List<Entry>> kvp in _cells)
        {
            if (kvp.Value.Count == 0)
            {
                continue;
            }

            int cellX = (int)(kvp.Key >> 32);
            int cellY = (int)(kvp.Key & 0xFFFFFFFF);

            Vector2f origin = new(cellX * _cellSize, cellY * _cellSize);

            yield return (origin, kvp.Value.Count);
        }
    }

    /// <summary>
    /// Converts a world-space coordinate to a cell index along one axis.
    /// </summary>
    private int ToCellCoord(float value)
    {
        return (int)MathF.Floor(value * _inverseCellSize);
    }

    /// <summary>
    /// Packs two 32-bit cell coordinates into a single 64-bit dictionary key.
    /// </summary>
    private static long CellKey(int cellX, int cellY)
    {
        return ((long)cellX << 32) | (uint)cellY;
    }
}
