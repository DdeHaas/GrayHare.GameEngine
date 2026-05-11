using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.Extensions;

/// <summary>Extension methods for <see cref="SFML.Graphics.RenderWindow"/> values used in engine math.</summary>
public static class WindowExtensions
{
    /// <summary>
    /// Draws the specified text centered horizontally at the given vertical position within the render window.
    /// The origin of the text is set to its center, so the text is drawn centered around the specified position.
    /// </summary>
    /// <param name="window">The render window on which to draw the text.</param>
    /// <param name="font">The font to use when rendering the text.</param>
    /// <param name="fontSize">The size, in pixels, of the font to use for the text.</param>
    /// <param name="fontColor">The color to use when rendering the text.</param>
    /// <param name="text">
    /// The text string to draw. If <see langword="null"/> or whitespace, the method returns without drawing.
    /// </param>
    /// <param name="y">
    /// The vertical position, in pixels, at which to draw the text, measured from the top of the window.
    /// </param>
    public static void DrawCenteredText(
        this RenderWindow window, Font font, uint fontSize, Color fontColor, string text, float y)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        using Text fontText = new(font, text, fontSize);
        fontText.FillColor = fontColor;

        FloatRect box = fontText.GetLocalBounds();
        float halfWidth = box.Size.X / 2f;
        float halfHeight = box.Size.Y / 2f;

        fontText.Origin = new Vector2f(box.Position.X + halfWidth, box.Position.Y + halfHeight);
        fontText.Position = new Vector2f(window.Size.X / 2f, y);

        window.Draw(fontText);
    }

    /// <summary>
    /// Returns the coordinates of the center point of the render window as a <see cref="Vector2f"/>.
    /// </summary>
    /// <param name="window">The render window for which to calculate the center point.</param>
    /// <returns>A <see cref="Vector2f"/> representing the center point of the render window.</returns>
    public static Vector2f GetCenter(this RenderWindow window)
    {
        return new Vector2f(window.Size.X / 2f, window.Size.Y / 2f);
    }
}
