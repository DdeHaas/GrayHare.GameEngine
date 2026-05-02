using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Rendering;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.CameraExtrasDemo;

/// <summary>
/// Demonstrates <see cref="Camera2D.ScreenToWorld"/>, <see cref="Camera2D.WorldToScreen"/>,
/// and <see cref="Camera2D.Rotation"/>.
/// A grid of world landmarks is rendered through the camera. Left-click converts a
/// screen position to world space; a pinned object tracks its own screen position.
/// </summary>
internal sealed class CameraExtrasScene : DemoSceneBase
{
    private readonly Vector2f _pinnedWorldPos = new(400f, 300f);

    private Font _font = null!;
    private Vector2f _lastWorldClick;
    private bool _hasClick;

    public CameraExtrasScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();
    }

    public override void Unload(GameHost host)
    {
        host.Camera.Reset();
        base.Unload(host);
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        float deltaTime = gameTime.DeltaTotalSeconds;

        // Q: rotate camera counter-clockwise.
        if (host.Input.IsKeyDown(Keyboard.Key.Q))
        {
            host.Camera.Rotation -= 45f * deltaTime;
        }

        // E: rotate camera clockwise.
        if (host.Input.IsKeyDown(Keyboard.Key.E))
        {
            host.Camera.Rotation += 45f * deltaTime;
        }

        // R: reset camera rotation.
        if (host.Input.WasKeyPressed(Keyboard.Key.R))
        {
            host.Camera.Rotation = 0f;
        }

        // LMB: convert screen position to world position.
        if (host.Input.WasMouseButtonPressed(Mouse.Button.Left))
        {
            _lastWorldClick = host.Camera.ScreenToWorld(host.Input.MousePosition);
            _hasClick = true;
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // Apply camera view for world rendering.
        window.SetView(host.Camera.GetView());

        // Draw 5×5 grid of world-space landmark dots.
        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                float wx = 120f + col * 160f;
                float wy = 80f + row * 140f;

                using CircleShape dot = new(5f)
                {
                    Origin = new Vector2f(5f, 5f),
                    Position = new Vector2f(wx, wy),
                    FillColor = new Color(60, 90, 130)
                };
                window.Draw(dot);
            }
        }

        // Draw the pinned world object.
        using CircleShape pinned = new(10f)
        {
            Origin = new Vector2f(10f, 10f),
            Position = _pinnedWorldPos,
            FillColor = new Color(255, 100, 100),
            OutlineColor = Color.White,
            OutlineThickness = 2f
        };
        window.Draw(pinned);

        // Reset to default view for HUD.
        window.SetView(window.DefaultView);

        // WorldToScreen: compute screen position of the pinned world object.
        Vector2i screenPos = host.Camera.WorldToScreen(_pinnedWorldPos);

        DrawCrossHair(window, screenPos);

        string clickInfo = _hasClick
            ? $"Last click → world ({_lastWorldClick.X:F0}, {_lastWorldClick.Y:F0})  [ScreenToWorld]"
            : "LMB to convert screen position → world position  [ScreenToWorld]";

        using Text hud = new(_font,
            $"Camera rotation: {host.Camera.Rotation:F1}°   Q/E rotate  ·  R reset rotation\n" +
            $"Red circle screen pos: ({screenPos.X}, {screenPos.Y})  [WorldToScreen]\n" +
            clickInfo, 18)
        {
            Position = new Vector2f(20f, 20f),
            FillColor = new Color(220, 230, 255)
        };
        window.Draw(hud);
    }

    private static void DrawCrossHair(RenderWindow window, Vector2i screenPos)
    {
        float cx = screenPos.X;
        float cy = screenPos.Y;

        using RectangleShape hBar = new(new Vector2f(20f, 2f))
        {
            Origin = new Vector2f(10f, 1f),
            Position = new Vector2f(cx, cy),
            FillColor = new Color(255, 200, 100)
        };
        window.Draw(hBar);

        using RectangleShape vBar = new(new Vector2f(2f, 20f))
        {
            Origin = new Vector2f(1f, 10f),
            Position = new Vector2f(cx, cy),
            FillColor = new Color(255, 200, 100)
        };
        window.Draw(vBar);
    }
}
