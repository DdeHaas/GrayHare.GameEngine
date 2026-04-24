namespace GrayHare.GameEngine.Pathfinding;

/// <summary>Represents a position in a <see cref="PathfindingGrid"/>.</summary>
/// <param name="Row">Zero-based row index.</param>
/// <param name="Column">Zero-based column index.</param>
public readonly record struct GridCell(int Row, int Column);
