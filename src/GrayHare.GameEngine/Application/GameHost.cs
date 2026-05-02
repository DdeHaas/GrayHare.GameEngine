using GrayHare.GameEngine.Assets;
using GrayHare.GameEngine.Audio;
using GrayHare.GameEngine.Ecs;
using GrayHare.GameEngine.Input;
using GrayHare.GameEngine.Rendering;
using GrayHare.GameEngine.Scenes;
using SFML.Graphics;

namespace GrayHare.GameEngine.Application;

/// <summary>
/// Provides access to all engine subsystems for the currently running scene.
/// Instances are created and owned by <see cref="GameApplication"/>.
/// </summary>
/// <remarks>This type is not thread-safe. Access all members from the main thread only.</remarks>
public sealed class GameHost
{
    private readonly InputTracker _input;
    private readonly SceneManager _sceneManager;

    internal GameHost(
        RenderWindow window,
        InputTracker input,
        AssetStore assets,
        AudioPlayer audio,
        World world,
        SceneManager sceneManager,
        GameApplicationOptions options)
    {
        Window = window;
        _input = input;
        Assets = assets;
        Audio = audio;
        World = world;
        _sceneManager = sceneManager;
        Options = options;
        Camera = new Camera2D(window.Size);
    }

    /// <summary>
    /// The 2D camera used to control the view each frame.
    /// Initialized from the window size at startup.
    /// </summary>
    public Camera2D Camera { get; }

    /// <summary>The SFML render window.</summary>
    public RenderWindow Window { get; }

    /// <summary>Input state snapshot for the current frame.</summary>
    public InputSnapshot Input => _input.Current;

    /// <summary>Optional named input action map for abstracting physical input bindings.</summary>
    public InputActionMap? InputActions { get; set; }

    /// <summary>Asset cache for textures, fonts, and sounds.</summary>
    public AssetStore Assets { get; }

    /// <summary>Audio playback manager.</summary>
    public AudioPlayer Audio { get; }

    /// <summary>ECS world for the current scene.</summary>
    public World World { get; }

    /// <summary>Options that were used to create the application.</summary>
    public GameApplicationOptions Options { get; }

    /// <summary>
    /// <see langword="true"/> after <see cref="Exit"/> has been called.
    /// The main loop will close the window on the next iteration.
    /// </summary>
    public bool ExitRequested { get; private set; }

    /// <summary>
    /// The current time-scale multiplier. A value of <c>0</c> pauses the game;
    /// <c>1</c> is normal speed; values between <c>0</c> and <c>1</c> produce slow-motion.
    /// Defaults to <c>1</c>.
    /// </summary>
    public float TimeScale { get; private set; } = 1f;

    /// <summary>Returns <see langword="true"/> when <see cref="TimeScale"/> is <c>0</c>.</summary>
    public bool IsPaused => TimeScale == 0f;

    /// <summary>
    /// Sets <see cref="TimeScale"/> to <c>0</c>, freezing <c>GameTime.Delta</c>
    /// and <c>GameTime.Total</c> for all subsequent frames.
    /// </summary>
    public void Pause() => TimeScale = 0f;

    /// <summary>
    /// Restores <see cref="TimeScale"/> to <c>1</c> after a <see cref="Pause"/> call.
    /// </summary>
    public void Resume() => TimeScale = 1f;

    /// <summary>
    /// Sets <see cref="TimeScale"/> to <paramref name="timeScale"/> (clamped to ≥ 0).
    /// Use values less than <c>1</c> for slow-motion and greater than <c>1</c> for fast-forward.
    /// </summary>
    public void SetTimeScale(float timeScale) => TimeScale = MathF.Max(0f, timeScale);

    /// <summary>The number of scenes currently on the stack.</summary>
    public int SceneStackDepth => _sceneManager.SceneStackDepth;

    /// <summary>
    /// Queues <paramref name="scene"/> to replace the entire scene stack at the end of this frame.
    /// The ECS world is cleared as part of the transition.
    /// </summary>
    public void ChangeScene(GameSceneBase scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        _sceneManager.Queue(scene);
    }

    /// <summary>
    /// Queues <paramref name="overlay"/> to be pushed on top of the current scene at the
    /// end of this frame. The current top scene receives
    /// <see cref="GameSceneBase.OnDeactivated"/> and the overlay receives
    /// <see cref="GameSceneBase.OnActivated"/> after loading.
    /// </summary>
    /// <param name="overlay">The overlay scene to push (e.g. a pause menu).</param>
    public void PushScene(GameSceneBase overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);

        _sceneManager.QueuePush(overlay);
    }

    /// <summary>
    /// Queues a pop operation to remove the top scene at the end of this frame.
    /// The popped scene is unloaded and disposed, and the scene beneath it receives
    /// <see cref="GameSceneBase.OnActivated"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown (during <c>ApplyPending</c>) when the stack has fewer than two scenes.
    /// </exception>
    public void PopScene()
    {
        _sceneManager.QueuePop();
    }

    /// <summary>Signals the main loop to close the window and stop.</summary>
    public void Exit()
    {
        ExitRequested = true;
    }

}
