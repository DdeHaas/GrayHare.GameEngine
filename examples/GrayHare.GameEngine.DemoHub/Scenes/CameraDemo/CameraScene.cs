using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.CameraDemo;

/// <summary>
/// Demonstrates <see cref="Rendering.Camera2D"/> features: smooth follow, zoom, and screen shake.
/// WASD moves a player square through a 2000×2000 world with a grid of landmarks.
/// </summary>
internal sealed class CameraScene : DemoSceneBase
{
    private const float WorldSize = 2000f;
    private const float PlayerSize = 24f;
    private const float MoveSpeed = 200f;

    private Font _font = null!;
    private Vector2f _playerPos = new(WorldSize / 2f, WorldSize / 2f);

    public CameraScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);

        _font = host.Assets.LoadFont();
        host.Camera.Position = _playerPos;
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

        // Player movement with WASD.
        Vector2f move = new(0f, 0f);
        if (host.Input.IsKeyDown(Keyboard.Key.W))
        {
            move += new Vector2f(0f, -1f);
        }

        if (host.Input.IsKeyDown(Keyboard.Key.S))
        {
            move += new Vector2f(0f, 1f);
        }

        if (host.Input.IsKeyDown(Keyboard.Key.A))
        {
            move += new Vector2f(-1f, 0f);
        }

        if (host.Input.IsKeyDown(Keyboard.Key.D))
        {
            move += new Vector2f(1f, 0f);
        }

        // Normalize diagonal movement.
        float len = MathF.Sqrt(move.X * move.X + move.Y * move.Y);
        if (len > 0f)
        {
            _playerPos += move / len * MoveSpeed * deltaTime;
        }

        // Clamp to world bounds.
        _playerPos = new Vector2f(
            Math.Clamp(_playerPos.X, 0f, WorldSize),
            Math.Clamp(_playerPos.Y, 0f, WorldSize));

        // Zoom control.
        if (host.Input.IsKeyDown(Keyboard.Key.Z))
        {
            host.Camera.Zoom += 0.5f * deltaTime;
        }

        if (host.Input.IsKeyDown(Keyboard.Key.X))
        {
            host.Camera.Zoom = MathF.Max(0.1f, host.Camera.Zoom - 0.5f * deltaTime);
        }

        // Screen shake.
        if (host.Input.WasKeyPressed(Keyboard.Key.Space))
        {
            host.Camera.Shake(8f, 0.4f);
        }

        host.Camera.Follow(_playerPos, 4f, deltaTime);
        host.Camera.UpdateShake(gameTime.RawDeltaTotalSeconds);
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // Apply camera view for world rendering.
        window.SetView(host.Camera.GetView());

        // Draw world boundary.
        using RectangleShape border = new(new Vector2f(WorldSize, WorldSize))
        {
            Position = new Vector2f(0f, 0f),
            FillColor = Color.Transparent,
            OutlineColor = new Color(60, 80, 100),
            OutlineThickness = 4f
        };
        window.Draw(border);

        // Draw grid of landmark crosses every 200 pixels.
        for (float x = 0f; x <= WorldSize; x += 200f)
        {
            for (float y = 0f; y <= WorldSize; y += 200f)
            {
                using CircleShape dot = new(3f)
                {
                    Origin = new Vector2f(3f, 3f),
                    Position = new Vector2f(x, y),
                    FillColor = new Color(50, 60, 80)
                };
                window.Draw(dot);
            }
        }

        // Draw player.
        float half = PlayerSize / 2f;
        using RectangleShape player = new(new Vector2f(PlayerSize, PlayerSize))
        {
            Origin = new Vector2f(half, half),
            Position = _playerPos,
            FillColor = new Color(255, 160, 60)
        };
        window.Draw(player);

        // Reset view for HUD.
        window.SetView(window.DefaultView);

        using Text hud = new(_font,
            $"Pos: ({_playerPos.X:F0}, {_playerPos.Y:F0})  Zoom: {host.Camera.Zoom:F2}", 18)
        {
            Position = new Vector2f(20f, 20f),
            FillColor = new Color(200, 220, 240)
        };
        window.Draw(hud);
    }
}
