using SFML.System;

namespace GrayHare.GameEngine;

/// <summary>Engine-wide constants.</summary>
public static class Constants
{
    /// <summary>Commonly used <see cref="Vector2f"/> constants.</summary>
    public static class Vectors
    {
        /// <summary>Zero vector (0, 0).</summary>
        public static readonly Vector2f Zero = new(0f, 0f);

        /// <summary>One vector (1, 1).</summary>
        public static readonly Vector2f One = new(1f, 1f);
    }
}
