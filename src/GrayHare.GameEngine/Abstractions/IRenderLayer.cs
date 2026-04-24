using GrayHare.GameEngine.Application;
using SFML.Graphics;

namespace GrayHare.GameEngine.Abstractions;

/// <summary>
/// Defines a contract for a render layer that can draw content onto a render window within a game host environment.
/// </summary>
/// <remarks>Implementations of this interface are responsible for rendering a specific layer or aspect of the
/// scene. Render layers are typically composed to build up the final visual output. The order in which layers are
/// rendered may affect the final appearance.</remarks>
public interface IRenderLayer
{
    /// <summary>
    /// Renders the current visual layer to the specified render window using the provided game host context.
    /// </summary>
    /// <param name="host">
    /// The game host that provides context and services required for rendering operations. Cannot be null.
    /// </param>
    /// <param name="window">The render window where the visual layer will be drawn. Cannot be null.</param>
    void RenderLayer(GameHost host, RenderWindow window);
}
