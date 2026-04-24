using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.OverviewScene;

/// <summary>
/// Scene 1 — displays all demo titles organised by group.
/// This acts as a browsable index for the entire DemoHub.
/// </summary>
internal sealed class DemoOverviewScene : DemoSceneBase
{
    private Font? _font;

    public DemoOverviewScene(DemoCatalog catalog, int sceneIndex)
        : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        if (_font is null)
        {
            return;
        }

        DrawHeading(window);
        DrawGroupColumns(window);
    }

    private void DrawHeading(RenderWindow window)
    {
        using Text heading = new(_font!, "GrayHare.GameEngine  –  Demo Index", 26)
        {
            Position = new Vector2f(40f, 18f),
            FillColor = new Color(235, 242, 255)
        };

        window.Draw(heading);
    }

    private void DrawGroupColumns(RenderWindow window)
    {
        // Split groups into two columns for a compact layout.
        IReadOnlyList<DemoGroup> groups = Catalog.Groups;
        int half = (groups.Count + 1) / 2;

        DrawColumn(window, groups, 0, half, startX: 40f);
        DrawColumn(window, groups, half, groups.Count, startX: 660f);
    }

    private void DrawColumn(
        RenderWindow window,
        IReadOnlyList<DemoGroup> groups,
        int groupFrom,
        int groupTo,
        float startX)
    {
        const float topY = 60f;
        const float groupHeaderHeight = 22f;
        const float entryHeight = 17f;
        const float groupGap = 8f;

        float y = topY;

        for (int g = groupFrom; g < groupTo; g++)
        {
            DemoGroup group = groups[g];

            // Group header
            using Text groupHeader = new(_font!, group.Name, 14)
            {
                Position = new Vector2f(startX, y),
                FillColor = new Color(100, 145, 210)
            };

            window.Draw(groupHeader);
            y += groupHeaderHeight;

            // Scene entries inside this group
            for (int idx = group.StartIndex; idx < group.EndIndex; idx++)
            {
                DemoEntry entry = Catalog.Entries[idx];
                string line = $"  {idx + 1,2}.  {entry.Title}";

                using Text entryText = new(_font!, line, 13)
                {
                    Position = new Vector2f(startX, y),
                    FillColor = new Color(200, 208, 225)
                };

                window.Draw(entryText);
                y += entryHeight;
            }

            y += groupGap;
        }
    }
}
