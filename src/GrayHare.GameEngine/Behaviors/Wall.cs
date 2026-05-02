using SFML.System;

namespace GrayHare.GameEngine.Behaviors;

/// <summary>
/// A directed line segment used for wall-avoidance steering.
/// The wall normal is the left perpendicular of the Start→End direction;
/// swap Start and End to control which side the wall faces.
/// </summary>
/// <remarks>
/// Each <see cref="Wall"/> is one-sided: <see cref="SteeringBehavior.WallAvoidance"/>
/// only applies repulsion when the agent is on the normal (front) face.To create a solid interior
/// wall that repels agents from both sides, register the segment twice — once in each direction.
/// </remarks>
public readonly record struct Wall
{
    /// <summary>Start point of the wall segment.</summary>
    public Vector2f Start { get; init; }

    /// <summary>End point of the wall segment.</summary>
    public Vector2f End { get; init; }

    /// <summary>Unit normal pointing in the direction the wall faces.</summary>
    public Vector2f Normal { get; init; }

    /// <summary>
    /// Initializes a new <see cref="Wall"/> from <paramref name="start"/> to <paramref name="end"/>
    /// and computes the outward-facing unit normal.
    /// </summary>
    /// <param name="start">Start point.</param>
    /// <param name="end">
    /// End point.  The normal is the left perpendicular of Start→End.
    /// </param>
    public Wall(Vector2f start, Vector2f end)
    {
        Start = start;
        End = end;
        Vector2f dir = end - start;
        Normal = dir.Length > float.Epsilon ? dir.Normalized().Perpendicular() : new Vector2f(0f, -1f);
    }

    /// <summary>
    /// Tests whether the segment from <paramref name="from"/> to <paramref name="to"/>
    /// intersects this wall segment.
    /// </summary>
    /// <param name="from">Ray origin.</param>
    /// <param name="to">Ray end point.</param>
    /// <param name="test">
    /// Parametric position along the feeler (0 = <paramref name="from"/>, 1 = <paramref name="to"/>)
    /// at the intersection.  Only meaningful when the method returns <see langword="true"/>.
    /// </param>
    /// <returns><see langword="true"/> if the segments intersect.</returns>
    public bool TryGetIntersection(Vector2f from, Vector2f to, out float test)
    {
        Vector2f feelerDir = to - from;
        Vector2f wallDir = End - Start;

        float denom = (feelerDir.X * wallDir.Y) - (feelerDir.Y * wallDir.X);

        if (MathF.Abs(denom) < float.Epsilon)
        {
            test = float.MaxValue;

            return false;
        }

        Vector2f diff = Start - from;
        test = ((diff.X * wallDir.Y) - (diff.Y * wallDir.X)) / denom;
        float u = ((diff.X * feelerDir.Y) - (diff.Y * feelerDir.X)) / denom;

        if (test is >= 0f and <= 1f && u is >= 0f and <= 1f)
        {
            return true;
        }

        test = float.MaxValue;

        return false;
    }
}
