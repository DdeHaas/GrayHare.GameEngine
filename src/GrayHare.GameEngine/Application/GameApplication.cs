using GrayHare.GameEngine.Assets;
using GrayHare.GameEngine.Audio;
using GrayHare.GameEngine.Diagnostics;
using GrayHare.GameEngine.Ecs;
using GrayHare.GameEngine.Input;
using GrayHare.GameEngine.Scenes;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.Application;

/// <summary>
/// Entry point that creates the window, wires subsystems, and runs the main loop.
/// </summary>
/// <remarks>This type is not thread-safe. Call <see cref="Run"/> from the main thread only.</remarks>
public sealed class GameApplication
{
    private readonly GameApplicationOptions _options;

    /// <summary>Creates a new application with the supplied options, or defaults if omitted.</summary>
    public GameApplication(GameApplicationOptions? options = null)
    {
        _options = options ?? new GameApplicationOptions();
    }

    /// <summary>
    /// Opens the window, loads <paramref name="initialScene"/>, and runs the main loop
    /// until the window is closed or <see cref="GameHost.Exit"/> is called.
    /// </summary>
    public void Run(GameSceneBase initialScene)
    {
        ArgumentNullException.ThrowIfNull(initialScene);

        if (_options.LogHandler is not null)
        {
            EngineLogger.SetHandler(_options.LogHandler);
        }

        using RenderWindow window = new(
            new VideoMode(_options.WindowSize),
            _options.Title,
            Styles.Default,
            State.Windowed);

        window.SetVerticalSyncEnabled(_options.VerticalSyncEnabled);
        if (_options.FrameRateLimit > 0)
        {
            window.SetFramerateLimit(_options.FrameRateLimit);
        }

        using AssetStore assets = new(_options.ContentRootPath);
        using AudioPlayer audio = new(assets);

        World world = new();
        InputTracker input = new();
        SceneManager sceneManager = new(world);
        GameHost host = new(window, input, assets, audio, world, sceneManager, _options);
        Clock clock = new();
        GameTime gameTime = GameTime.Start;

        // Store delegates so they can be unsubscribed before the window is disposed.
        EventHandler onClosed = (_, _) => host.Exit();
        EventHandler<KeyEventArgs> onKeyPressed = (_, args) => input.OnKeyPressed(args.Code);
        EventHandler<KeyEventArgs> onKeyReleased = (_, args) => input.OnKeyReleased(args.Code);
        EventHandler<MouseButtonEventArgs> onMousePressed =
            (_, args) => input.OnMouseButtonPressed(args.Button, args.Position);
        EventHandler<MouseButtonEventArgs> onMouseReleased =
            (_, args) => input.OnMouseButtonReleased(args.Button, args.Position);
        EventHandler<MouseMoveEventArgs> onMouseMoved = (_, args) => input.OnMouseMoved(args.Position);
        EventHandler<MouseWheelScrollEventArgs> onMouseScrolled =
            (_, args) => input.OnMouseWheelScrolled(args.Delta, args.Position);
        EventHandler<JoystickButtonEventArgs> onJoystickButtonPressed =
            (_, args) => input.OnJoystickButtonPressed(args.JoystickId, args.Button);
        EventHandler<JoystickButtonEventArgs> onJoystickButtonReleased =
            (_, args) => input.OnJoystickButtonReleased(args.JoystickId, args.Button);
        EventHandler<JoystickMoveEventArgs> onJoystickMoved =
            (_, args) => input.OnJoystickMoved(args.JoystickId, args.Axis, args.Position);
        EventHandler<JoystickConnectEventArgs> onJoystickConnected =
            (_, args) => input.OnJoystickConnected(args.JoystickId);
        EventHandler<JoystickConnectEventArgs> onJoystickDisconnected =
            (_, args) => input.OnJoystickDisconnected(args.JoystickId);

        window.Closed += onClosed;
        window.KeyPressed += onKeyPressed;
        window.KeyReleased += onKeyReleased;
        window.MouseButtonPressed += onMousePressed;
        window.MouseButtonReleased += onMouseReleased;
        window.MouseMoved += onMouseMoved;
        window.MouseWheelScrolled += onMouseScrolled;
        window.JoystickButtonPressed += onJoystickButtonPressed;
        window.JoystickButtonReleased += onJoystickButtonReleased;
        window.JoystickMoved += onJoystickMoved;
        window.JoystickConnected += onJoystickConnected;
        window.JoystickDisconnected += onJoystickDisconnected;

        // Seed connection state for joysticks already connected at launch.
        // SFML does not guarantee JoystickConnected events for pre-existing devices.
        input.InitializeJoysticks();

        sceneManager.Initialize(host, initialScene);

        while (window.IsOpen && !host.ExitRequested)
        {
            input.BeginFrame();
            input.SyncMousePosition(window);
            window.DispatchEvents();

            if (host.ExitRequested)
            {
                window.Close();
                break;
            }

            audio.Update();

            TimeSpan delta = clock.Restart().ToTimeSpan();
            gameTime = gameTime.Advance(delta, host.TimeScale);

            sceneManager.Update(host, gameTime);
            sceneManager.ApplyPending(host);

            window.Clear(_options.ClearColor);
            host.Camera.UpdateShake(gameTime.RawDeltaTotalSeconds);
            window.SetView(host.Camera.GetView());
            sceneManager.Render(host, window);
            window.Display();
        }

        // Unsubscribe before the window is disposed to release delegate references.
        window.Closed -= onClosed;
        window.KeyPressed -= onKeyPressed;
        window.KeyReleased -= onKeyReleased;
        window.MouseButtonPressed -= onMousePressed;
        window.MouseButtonReleased -= onMouseReleased;
        window.MouseMoved -= onMouseMoved;
        window.MouseWheelScrolled -= onMouseScrolled;
        window.JoystickButtonPressed -= onJoystickButtonPressed;
        window.JoystickButtonReleased -= onJoystickButtonReleased;
        window.JoystickMoved -= onJoystickMoved;
        window.JoystickConnected -= onJoystickConnected;
        window.JoystickDisconnected -= onJoystickDisconnected;
    }
}
