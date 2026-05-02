using SFML.System;

namespace GrayHare.GameEngine.Extensions;

/// <summary>Extension methods for <see cref="Vector2f"/>.</summary>
public static class VectorExtensions
{
    /// <summary>
    /// Returns a vector with its length limited to <paramref name="maximum"/>, preserving direction.
    /// If the vector's length is already within the limit, the original vector is returned.
    /// </summary>
    public static Vector2f Truncate(this Vector2f value, float maximum)
    {
        if (value.Length > maximum)
        {
            return value.Normalized() * maximum;
        }

        return value;
    }

    /// <summary>Returns the Euclidean distance between <paramref name="from"/> and <paramref name="to"/>.</summary>
    public static float DistanceTo(this Vector2f from, Vector2f to)
    {
        return (to - from).Length;
    }

    /// <summary>
    /// Returns a new vector representing the wrapped position,
    /// where each component is within the range [0, size component).
    /// </summary>
    public static Vector2f WrapPosition(this Vector2f position, Vector2f size)
    {
        return new(((position.X % size.X) + size.X) % size.X, ((position.Y % size.Y) + size.Y) % size.Y);
    }

    /// <summary>
    /// Returns a new vector representing the wrapped position,
    /// where each component is within the range [0, size component).
    /// </summary>
    public static Vector2f WrapPosition(this Vector2f position, Vector2u size)
    {
        Vector2f vectorSize = new(size.X, size.Y);

        return WrapPosition(position, vectorSize);
    }
}
