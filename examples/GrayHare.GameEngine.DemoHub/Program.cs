using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub;

internal static class Program
{
    private static void Main()
    {
        string contentRoot = Path.Combine(AppContext.BaseDirectory, "Assets");
        DemoAssetsManifest assets = DemoAssets.EnsureCreated(contentRoot);
        DemoCatalog catalog = new(assets);

        GameApplicationOptions options = new()
        {
            Title = "GrayHare DemoHub",
            WindowSize = new Vector2u(1280, 720),
            ContentRootPath = contentRoot,
            ClearColor = new Color(14, 18, 26),
            FrameRateLimit = 60,
            VerticalSyncEnabled = true
        };

        GameApplication application = new(options);
        application.Run(catalog.Create(0));
    }
}
