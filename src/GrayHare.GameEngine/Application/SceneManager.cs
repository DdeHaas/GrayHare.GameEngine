using GrayHare.GameEngine.Ecs;
using GrayHare.GameEngine.Scenes;
using SFML.Graphics;

namespace GrayHare.GameEngine.Application;

/// <summary>
/// Controls the active scene stack and handles transitions between scenes.
/// Supports a full stack for overlay scenes (e.g. pause menus, dialog boxes)
/// with push/pop semantics, as well as flat scene-change transitions that
/// clear the entire stack.
/// </summary>
/// <remarks>This type is not thread-safe. Access all members from the main thread only.</remarks>
public sealed class SceneManager
{
    private readonly World _world;
    private readonly List<GameSceneBase> _stack = [];
    private readonly Queue<PendingOperation> _pendingOps = new();

    /// <summary>Initializes the manager with the world it will clear on scene transitions.</summary>
    public SceneManager(World world)
    {
        _world = world;
    }

    /// <summary>The scene at the top of the stack (the one currently receiving updates).</summary>
    public GameSceneBase? CurrentScene => _stack.Count > 0 ? _stack[^1] : null;

    /// <summary>The number of scenes currently on the stack.</summary>
    public int SceneStackDepth => _stack.Count;

    /// <summary>Loads and activates <paramref name="initialScene"/>.</summary>
    public void Initialize(GameHost host, GameSceneBase initialScene)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(initialScene);

        _stack.Add(initialScene);
        initialScene.Load(host);
        initialScene.ActivateInternal(host);
    }

    /// <summary>
    /// Queues <paramref name="scene"/> to replace the entire stack on the next
    /// <see cref="ApplyPending"/> call. The ECS world is cleared during the transition.
    /// </summary>
    public void Queue(GameSceneBase scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        _pendingOps.Enqueue(new PendingOperation(OperationType.Change, scene));
    }

    /// <summary>
    /// Queues <paramref name="overlay"/> to be pushed on top of the current scene
    /// on the next <see cref="ApplyPending"/> call. The current top scene receives
    /// <see cref="GameSceneBase.OnDeactivated"/> and the overlay receives
    /// <see cref="GameSceneBase.OnActivated"/> after loading.
    /// </summary>
    public void QueuePush(GameSceneBase overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        _pendingOps.Enqueue(new PendingOperation(OperationType.Push, overlay));
    }

    /// <summary>
    /// Queues a pop operation to remove the top scene on the next
    /// <see cref="ApplyPending"/> call. The popped scene is unloaded and disposed,
    /// and the scene below it receives <see cref="GameSceneBase.OnActivated"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the stack has fewer than two scenes (you cannot pop the root scene).
    /// </exception>
    public void QueuePop()
    {
        _pendingOps.Enqueue(new PendingOperation(OperationType.Pop, null));
    }

    /// <summary>Only the top scene receives game-logic updates.</summary>
    public void Update(GameHost host, in GameTime gameTime)
    {
        if (_stack.Count > 0)
        {
            _stack[^1].Update(host, in gameTime);
        }
    }

    /// <summary>
    /// Renders the full scene stack bottom-to-top so that overlay scenes draw
    /// on top of scenes beneath them.
    /// </summary>
    public void Render(GameHost host, RenderWindow window)
    {
        foreach (GameSceneBase scene in _stack)
        {
            scene.Render(host, window);
        }
    }

    /// <summary>
    /// Processes all queued push, pop, and change operations in order.
    /// Called once per frame after <see cref="Update"/>.
    /// </summary>
    public void ApplyPending(GameHost host)
    {
        while (_pendingOps.Count > 0)
        {
            PendingOperation op = _pendingOps.Dequeue();

            switch (op.Type)
            {
                case OperationType.Change:
                    ApplyChange(host, op.Scene!);
                    break;
                case OperationType.Push:
                    ApplyPush(host, op.Scene!);
                    break;
                case OperationType.Pop:
                    ApplyPop(host);
                    break;
            }
        }
    }

    /// <summary>Clears the entire stack, resets the ECS world, and loads the new scene.</summary>
    private void ApplyChange(GameHost host, GameSceneBase newScene)
    {
        // Unload and dispose all scenes top-to-bottom.
        for (int i = _stack.Count - 1; i >= 0; i--)
        {
            _stack[i].Unload(host);
            _stack[i].Dispose();
        }

        _stack.Clear();
        _world.Clear();

        _stack.Add(newScene);
        newScene.Load(host);
        newScene.ActivateInternal(host);
    }

    /// <summary>Pushes an overlay scene on top of the current scene.</summary>
    private void ApplyPush(GameHost host, GameSceneBase overlay)
    {
        if (_stack.Count > 0)
        {
            _stack[^1].DeactivateInternal(host);
        }

        _stack.Add(overlay);
        overlay.Load(host);
        overlay.ActivateInternal(host);
    }

    /// <summary>Pops the top scene off the stack and reactivates the scene beneath it.</summary>
    private void ApplyPop(GameHost host)
    {
        if (_stack.Count < 2)
        {
            throw new InvalidOperationException(
                "Cannot pop the root scene. The stack must contain at least two scenes to pop.");
        }

        GameSceneBase top = _stack[^1];
        _stack.RemoveAt(_stack.Count - 1);

        top.Unload(host);
        top.Dispose();

        if (_stack.Count > 0)
        {
            _stack[^1].ActivateInternal(host);
        }
    }

    private enum OperationType
    {
        Change,
        Push,
        Pop
    }

    private readonly record struct PendingOperation(OperationType Type, GameSceneBase? Scene);
}
