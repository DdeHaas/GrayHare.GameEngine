using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.TextDemo;

/// <summary>
/// Demonstrates SFML text rendering at various sizes using the built-in system font.
/// </summary>
internal sealed class TextScene : DemoSceneBase
{
    private Font? _font;

    public TextScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        if (_font is null)
        {
            using RectangleShape fallback = new(new SFML.System.Vector2f(700f, 160f))
            {
                Position = new(180f, 220f),
                FillColor = new Color(60, 80, 110)
            };

            window.Draw(fallback);
            return;
        }

        float x = 80f;
        float y = 60f;

        DrawSample(window, "Text Rendering", 48, new Color(235, 242, 255), ref x, ref y, gap: 10f);
        DrawSample(window, "The quick brown fox jumps over the lazy dog.", 28, new Color(200, 210, 235), ref x, ref y, gap: 8f);
        DrawSample(window, "ABCDEFGHIJKLMNOPQRSTUVWXYZ  0123456789", 22, new Color(170, 185, 215), ref x, ref y, gap: 6f);
        DrawSample(window, "abcdefghijklmnopqrstuvwxyz  !@#$%^&*()", 22, new Color(170, 185, 215), ref x, ref y, gap: 6f);
        DrawSample(window, "Size 18 — smaller body text", 18, new Color(145, 160, 195), ref x, ref y, gap: 6f);
        DrawSample(window, "Size 14 — caption / hint text", 14, new Color(120, 135, 170), ref x, ref y, gap: 6f);
        DrawSample(window, "Size 11 — fine print", 11, new Color(100, 115, 150), ref x, ref y, gap: 0f);
    }

    private void DrawSample(
        RenderWindow window,
        string text,
        uint size,
        Color color,
        ref float x,
        ref float y,
        float gap)
    {
        using Text label = new(_font!, text, size)
        {
            Position = new Vector2f(x, y),
            FillColor = color
        };

        window.Draw(label);
        y += label.GetLocalBounds().Height + gap + size * 0.3f;
    }
}
