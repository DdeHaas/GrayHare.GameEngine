using GrayHare.GameEngine.Application;
using SFML.Graphics;

namespace GrayHare.GameEngine.Scenes;

/// <summary>
/// Base class for all engine scenes.  Override the virtual methods to add scene-specific logic.
/// Implements <see cref="IDisposable"/> to ensure SFML resources are released when a scene
/// is removed from the stack.
/// </summary>
/// <remarks>This type is not thread-safe. Access all members from the main thread only.</remarks>
public abstract class GameSceneBase : IRenderLayer, IDisposable
{
    // Sorted ascending by RenderOrder; equal values preserve insertion order (stable sort).
    private readonly List<ISceneLayer> _layers = [];
    private bool _disposed;

    /// <summary>Display name for this scene (defaults to the concrete type name).</summary>
    public virtual string Name => GetType().Name;

    /// <summary>
    /// Registers <paramref name="layer"/> with this scene.
    /// </summary>
    /// <remarks>
    /// The layer is inserted at the correct position according to its
    /// <see cref="ISceneLayer.RenderOrder"/> (ascending, stable).  The layer's signal
    /// events are wired to the scene's virtual hooks.  Call this method from the
    /// constructor or from <see cref="Load"/>.
    /// </remarks>
    /// <param name="layer">The layer to add.</param>
    protected void AddLayer(ISceneLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        // Binary-search the insertion point to keep the list sorted.
        int index = _layers.BinarySearch(layer, LayerRenderOrderComparer.Instance);
        if (index < 0)
        {
            index = ~index;
        }

        // Advance past layers with equal RenderOrder to preserve insertion order.
        while (index < _layers.Count && _layers[index].RenderOrder == layer.RenderOrder)
        {
            index++;
        }

        _layers.Insert(index, layer);
    }

    /// <summary>
    /// Removes a previously registered <paramref name="layer"/> from this scene.
    /// </summary>
    /// <param name="layer">The layer to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the layer was found and removed;
    /// <see langword="false"/> if it was not registered.
    /// </returns>
    protected bool RemoveLayer(ISceneLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        return _layers.Remove(layer);
    }

    /// <summary>Called once when the scene becomes active.</summary>
    public virtual void Load(GameHost host)
    {
        foreach (ISceneLayer layer in _layers)
        {
            layer.Load(host);
        }
    }

    /// <summary>Called once when the scene is replaced by another scene.</summary>
    public virtual void Unload(GameHost host)
    {
        for (int i = _layers.Count - 1; i >= 0; i--)
        {
            _layers[i].Unload(host);
        }
    }

    /// <summary>
    /// Called when this scene becomes the top of the stack — either after
    /// <see cref="Load"/> during initial activation, or when a scene above
    /// it has been popped.
    /// </summary>
    /// <param name="host">The game host providing access to engine subsystems.</param>
    public virtual void OnActivated(GameHost host)
    {
    }

    /// <summary>
    /// Called when another scene is pushed on top of this scene, moving it
    /// out of the active (top) position.
    /// </summary>
    /// <param name="host">The game host providing access to engine subsystems.</param>
    public virtual void OnDeactivated(GameHost host)
    {
    }

    /// <summary>
    /// Invokes <see cref="OnActivated"/> and then forwards the event to every registered layer.
    /// Called by <see cref="GrayHare.GameEngine.Application.SceneManager"/> — not intended for direct call.
    /// </summary>
    internal void ActivateInternal(GameHost host)
    {
        OnActivated(host);
        foreach (ISceneLayer layer in _layers)
        {
            layer.OnActivated(host);
        }
    }

    /// <summary>
    /// Invokes <see cref="OnDeactivated"/> and then forwards the event to every registered layer.
    /// Called by <see cref="GrayHare.GameEngine.Application.SceneManager"/> — not intended for direct call.
    /// </summary>
    internal void DeactivateInternal(GameHost host)
    {
        OnDeactivated(host);
        foreach (ISceneLayer layer in _layers)
        {
            layer.OnDeactivated(host);
        }
    }

    /// <summary>Called once per frame for game-logic updates.</summary>
    public virtual void Update(GameHost host, in GameTime gameTime)
    {
        foreach (ISceneLayer layer in _layers)
        {
            layer.Update(host, in gameTime);
        }
    }

    /// <summary>
    /// Renders all layers, calling <see cref="RenderLayer"/> between layers with negative
    /// and non-negative <see cref="ISceneLayer.RenderOrder"/>.
    /// </summary>
    public void Render(GameHost host, RenderWindow window)
    {
        // _layers is sorted ascending by RenderOrder; walk it with a single index pass
        // to avoid LINQ allocations on every frame.
        int i = 0;

        // Draw layers with RenderOrder < 0 first.
        while (i < _layers.Count && _layers[i].RenderOrder < 0)
        {
            _layers[i].RenderLayer(host, window);
            i++;
        }

        // Draw the scene's own content between the two layer groups.
        RenderLayer(host, window);

        // Draw remaining layers (RenderOrder >= 0).
        while (i < _layers.Count)
        {
            _layers[i].RenderLayer(host, window);
            i++;
        }
    }

    /// <summary>
    /// Renders the scene's own content (called between layers with negative and non-negative RenderOrder).
    /// </summary>
    public abstract void RenderLayer(GameHost host, RenderWindow window);

    /// <summary>Releases resources held by this scene.</summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Override to release scene-specific resources.  The base implementation is a no-op.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> when called from <see cref="Dispose()"/>;
    /// <see langword="false"/> when called from a finalizer.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    private sealed class LayerRenderOrderComparer : IComparer<ISceneLayer>
    {
        public static readonly LayerRenderOrderComparer Instance = new();

        public int Compare(ISceneLayer? x, ISceneLayer? y) => (x?.RenderOrder ?? 0).CompareTo(y?.RenderOrder ?? 0);
    }
}
