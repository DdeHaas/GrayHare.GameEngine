using GrayHare.GameEngine.Application;

namespace GrayHare.GameEngine;

/// <summary>
/// Defines the contract for a scene layer that participates in rendering and update cycles.
/// </summary>
/// <remarks>
/// The render order is determined by <see cref="RenderOrder"/>; lower values render first.
/// Layers should release their resources in <see cref="Unload"/> to avoid resource leaks.
/// <para>
/// <see cref="OnActivated"/> and <see cref="OnDeactivated"/> have default empty implementations
/// and are forwarded automatically by <see cref="GrayHare.GameEngine.Scenes.GameSceneBase"/>.
/// Override them when a layer needs to react to the owning scene being pushed or popped.
/// </para>
/// </remarks>
public interface ISceneLayer : IRenderLayer
{
    /// <summary>Gets the relative rendering order within the parent scene.</summary>
    int RenderOrder { get; }

    /// <summary>Initializes and loads resources for this layer.</summary>
    /// <param name="host">The game host providing access to engine subsystems.</param>
    void Load(GameHost host);

    /// <summary>Releases resources held by this layer.</summary>
    /// <param name="host">The game host providing access to engine subsystems.</param>
    void Unload(GameHost host);

    /// <summary>Advances game logic for this layer by one frame.</summary>
    /// <param name="host">The game host providing access to engine subsystems.</param>
    /// <param name="gameTime">Timing information for the current frame.</param>
    void Update(GameHost host, in GameTime gameTime);

    /// <summary>
    /// Called when the owning scene becomes the active top of the scene stack.
    /// Override to react to push or initial-activation events.
    /// The default implementation is a no-op.
    /// </summary>
    /// <param name="host">The game host providing access to engine subsystems.</param>
    void OnActivated(GameHost host) { }

    /// <summary>
    /// Called when the owning scene is pushed down by another scene being placed on top.
    /// Override to react to deactivation events (e.g. pause overlays).
    /// The default implementation is a no-op.
    /// </summary>
    /// <param name="host">The game host providing access to engine subsystems.</param>
    void OnDeactivated(GameHost host) { }
}
