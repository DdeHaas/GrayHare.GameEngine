using SFML.System;

namespace GrayHare.GameEngine.Extensions;

/// <summary>Extension methods for <see cref="float"/> values used in engine math.</summary>
public static class FloatExtensions
{
    /// <summary>
    /// Converts an angle in degrees to a unit direction vector.
    /// 0° points right (+X); angles increase clockwise in SFML screen-space.
    /// </summary>
    public static Vector2f ToVector2f(this float degrees)
    {
        float radians = Angle.FromDegrees(degrees).Radians;
        return new Vector2f(MathF.Cos(radians), MathF.Sin(radians));
    }
}
