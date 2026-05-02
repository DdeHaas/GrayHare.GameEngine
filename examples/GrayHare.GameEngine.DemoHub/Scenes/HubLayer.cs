using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes;

/// <summary>
/// Renders a semi-transparent hub panel at the bottom of the window that every demo scene shares.
/// Displays the current group tag, scene title, description (including scene-specific controls),
/// and a fixed global navigation hint block.
/// </summary>
internal sealed class HubLayer : ISceneLayer
{
    private const float WindowWidth = 1280f;
    private const float WindowHeight = 720f;
    private const float PanelHeight = 110f;
    private const float PanelY = WindowHeight - PanelHeight;
    private const float PaddingX = 16f;
    private const float PaddingY = 8f;
    private const float NavColumnWidth = 210f;

    private static readonly string _navHints =
        "+  next      -  prev\n" +
        "Esc   exit";

    private readonly DemoCatalog _catalog;
    private readonly int _sceneIndex;

    private Font _font = null!;

    public HubLayer(DemoCatalog catalog, int sceneIndex)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        _catalog = catalog;
        _sceneIndex = sceneIndex;
    }

    /// <inheritdoc/>
    /// <remarks>Renders above all demo content.</remarks>
    public int RenderOrder => 100;

    public void Load(GameHost host) => _font = host.Assets.LoadFont();

    public void Unload(GameHost host) { }

    public void Update(GameHost host, in GameTime gameTime) { }

    public void RenderLayer(GameHost host, RenderWindow window)
    {
        DrawPanel(window);

        DemoEntry entry = _catalog.Entries[_sceneIndex];
        DemoGroup group = _catalog.GroupOf(_sceneIndex);

        float contentY = PanelY + PaddingY;
        float rightX = WindowWidth - NavColumnWidth;

        DrawSeparator(window, rightX - 8f);
        DrawNavHints(window, rightX, contentY);
        DrawGroupTag(window, group, contentY);
        DrawTitle(window, entry, contentY + 18f);
        DrawDescription(window, entry, contentY + 44f);
    }

    private static void DrawPanel(RenderWindow window)
    {
        using RectangleShape panel = new(new Vector2f(WindowWidth, PanelHeight))
        {
            Position = new Vector2f(0f, PanelY),
            FillColor = new Color(8, 10, 18, 215),
            OutlineColor = new Color(45, 55, 85, 200),
            OutlineThickness = 1f
        };

        window.Draw(panel);
    }

    private static void DrawSeparator(RenderWindow window, float x)
    {
        using RectangleShape line = new(new Vector2f(1f, PanelHeight - PaddingY * 2f))
        {
            Position = new Vector2f(x, PanelY + PaddingY),
            FillColor = new Color(45, 55, 85, 180)
        };

        window.Draw(line);
    }

    private void DrawNavHints(RenderWindow window, float x, float y)
    {
        using Text nav = new(_font, _navHints, 13)
        {
            Position = new Vector2f(x, y),
            FillColor = new Color(110, 125, 155)
        };

        window.Draw(nav);
    }

    private void DrawGroupTag(RenderWindow window, DemoGroup group, float y)
    {
        string tag = $"[ {group.Name} ]   {_sceneIndex + 1} / {_catalog.Entries.Count}";

        using Text text = new(_font, tag, 13)
        {
            Position = new Vector2f(PaddingX, y),
            FillColor = new Color(100, 145, 210)
        };

        window.Draw(text);
    }

    private void DrawTitle(RenderWindow window, DemoEntry entry, float y)
    {
        using Text text = new(_font, entry.Title, 20)
        {
            Position = new Vector2f(PaddingX, y),
            FillColor = new Color(235, 242, 255)
        };

        window.Draw(text);
    }

    private void DrawDescription(RenderWindow window, DemoEntry entry, float y)
    {
        using Text text = new(_font, entry.Description, 14)
        {
            Position = new Vector2f(PaddingX, y),
            FillColor = new Color(170, 182, 205)
        };

        window.Draw(text);
    }
}
