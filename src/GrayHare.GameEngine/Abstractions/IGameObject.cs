using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.Abstractions;

/// <summary>
/// The minimal contract for any object that can be placed and rendered in the world.
/// </summary>
public interface IGameObject : IDisposable
{
    /// <summary>Rotation in degrees.</summary>
    float Rotation { get; }

    /// <summary>Local origin (pivot point).</summary>
    Vector2f Origin { get; }

    /// <summary>World-space position.</summary>
    Vector2f Position { get; }

    /// <summary>Scale applied to the object.</summary>
    Vector2f Scale { get; }

    /// <summary>Draw-ordering hint; lower values are drawn first.</summary>
    int ZOrder { get; }

    /// <summary>Axis-aligned bounding box in world space.</summary>
    FloatRect GlobalBounds { get; }

    /// <summary>Draws the object to <paramref name="window"/>.</summary>
    void Draw(RenderWindow window);

    /// <summary>Advances the object's state by <paramref name="deltaTime"/> seconds.</summary>
    void Update(float deltaTime);
}
