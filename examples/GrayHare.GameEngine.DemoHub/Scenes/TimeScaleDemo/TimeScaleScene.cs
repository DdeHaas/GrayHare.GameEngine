using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.TimeScaleDemo;

/// <summary>
/// Demonstrates <see cref="GameHost.Pause"/>, <see cref="GameHost.Resume"/>,
/// and <see cref="GameHost.SetTimeScale"/>.
/// <list type="bullet">
///   <item>Space — toggle pause (<c>TimeScale = 0</c>)</item>
///   <item>Tab — toggle slow-motion (<c>TimeScale = 0.25</c>)</item>
///   <item>Delta and Total only advance while unpaused.</item>
///   <item>RawTotal always advances regardless of time scale.</item>
/// </list>
/// </summary>
internal sealed class TimeScaleScene : DemoSceneBase
{
    private Font? _font;

    // Spinning square angle — driven by scaled Delta so it stops when paused.
    private float _angle;

    // Raw-time driven angle — driven by RawDelta so it never stops.
    private float _rawAngle;

    public TimeScaleScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        if (host.Input.WasKeyPressed(Keyboard.Key.Space))
        {
            if (host.IsPaused)
            {
                host.Resume();
            }
            else
            {
                host.Pause();
            }
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Tab))
        {
            host.SetTimeScale(host.TimeScale is 1f ? 0.25f : 1f);
        }

        // Scaled angle stops when paused; slows in slow-motion.
        _angle += gameTime.DeltaTotalSeconds * 90f;

        // Raw angle keeps spinning no matter what.
        _rawAngle += gameTime.RawDeltaTotalSeconds * 45f;
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        float cx = host.Window.Size.X / 2f;
        float cy = host.Window.Size.Y / 2f;

        // Scaled square (stops when paused).
        using RectangleShape scaled = new(new Vector2f(100f, 100f))
        {
            Origin = new Vector2f(50f, 50f),
            Position = new Vector2f(cx - 120f, cy),
            Rotation = _angle,
            FillColor = new Color(80, 180, 255)
        };
        window.Draw(scaled);

        // Raw square (always spinning).
        using RectangleShape raw = new(new Vector2f(100f, 100f))
        {
            Origin = new Vector2f(50f, 50f),
            Position = new Vector2f(cx + 120f, cy),
            Rotation = _rawAngle,
            FillColor = new Color(255, 160, 60)
        };
        window.Draw(raw);

        if (_font is null)
        {
            return;
        }

        string status = host.TimeScale switch
        {
            0f => "PAUSED",
            1f => "Normal",
            _ => $"Slow-motion  ×{host.TimeScale:F2}"
        };

        using Text statusText = new(_font, status, 22)
        {
            Position = new Vector2f(20f, 20f),
            FillColor = host.IsPaused ? new Color(255, 100, 100) : new Color(100, 255, 100)
        };
        window.Draw(statusText);

        DrawLabel(window, "Scaled\n(pauses)", new Vector2f(cx - 120f, cy + 80f));
        DrawLabel(window, "Raw\n(always spins)", new Vector2f(cx + 120f, cy + 80f));

        using Text stats = new(_font, $"TimeScale={host.TimeScale:F2}", 15)
        {
            Position = new Vector2f(20f, 50f),
            FillColor = new Color(160, 160, 160)
        };
        window.Draw(stats);
    }

    private void DrawLabel(RenderWindow window, string text, Vector2f position)
    {
        if (_font is null)
        {
            return;
        }

        using Text label = new(_font, text, 15)
        {
            Position = position,
            FillColor = new Color(200, 200, 200)
        };
        label.Origin = new Vector2f(label.GetLocalBounds().Width / 2f, 0f);
        window.Draw(label);
    }
}
