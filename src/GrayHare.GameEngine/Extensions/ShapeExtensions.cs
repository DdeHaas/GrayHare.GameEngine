using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.Extensions;

/// <summary>Extension methods that convert SFML shapes to textures.</summary>
public static class ShapeExtensions
{
    /// <summary>
    /// Renders this <paramref name="shape"/> into a new <see cref="Texture"/>.
    /// An optional <paramref name="padding"/> (pixels) is added around all sides.
    /// The shape's <c>Position</c> is temporarily modified and then restored.
    /// </summary>
    public static Texture ToTexture(this Shape shape, uint padding = 0)
    {
        FloatRect bounds = shape.GetGlobalBounds();

        uint width = Math.Max(1u, (uint)(bounds.Width + (padding * 2)));
        uint height = Math.Max(1u, (uint)(bounds.Height + (padding * 2)));

        using RenderTexture renderTexture = new(new Vector2u(width, height));
        renderTexture.Clear(Color.Transparent);

        Vector2f originalPos = shape.Position;
        shape.Position = new Vector2f(padding - bounds.Left, padding - bounds.Top);
        renderTexture.Draw(shape);
        renderTexture.Display();
        shape.Position = originalPos;

        return new Texture(renderTexture.Texture);
    }
}
