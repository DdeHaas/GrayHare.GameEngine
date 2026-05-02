using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Diagnostics;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.ShapeTextureDemo;

/// <summary>
/// Demonstrates <see cref="ShapeExtensions.ToTexture(Shape, uint)"/>,
/// <see cref="Assets.AssetStore.Unload"/>, and <see cref="EngineLogger"/>.
/// Shows the difference between AssetStore-managed textures and caller-owned textures
/// produced by <c>ToTexture()</c>.
/// </summary>
internal sealed class ShapeTextureScene : DemoSceneBase
{
    private Font _font = null!;
    private Texture? _checkerTexture;
    private Texture? _circleTexture;
    private Texture? _polygonTexture;
    private string _lastLogMessage = string.Empty;

    public ShapeTextureScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();

        // Intercept EngineLogger to capture messages for on-screen display.
        EngineLogger.SetHandler(msg => _lastLogMessage = msg);

        // Load checker texture via AssetStore (cache-managed, shared lifetime).
        _checkerTexture = host.Assets.LoadTexture(Catalog.Assets.CheckerTexturePath);
        EngineLogger.Log($"Loaded via AssetStore: {Catalog.Assets.CheckerTexturePath}");

        // Create caller-owned textures directly from SFML shapes (not cached by AssetStore).
        using CircleShape circle = new(40f) { FillColor = new Color(100, 180, 255) };
        _circleTexture = circle.ToTexture(4);
        EngineLogger.Log("Created circle texture via ShapeExtensions.ToTexture()");

        _polygonTexture = CreateStarTexture();
        EngineLogger.Log("Created star texture via ShapeExtensions.ToTexture()");
    }

    public override void Unload(GameHost host)
    {
        // Restore the default Debug.WriteLine handler so other demos are not affected.
        EngineLogger.SetHandler(msg => System.Diagnostics.Debug.WriteLine(msg));

        // Caller-owned textures from ToTexture() must be manually disposed.
        _circleTexture?.Dispose();
        _circleTexture = null;
        _polygonTexture?.Dispose();
        _polygonTexture = null;

        base.Unload(host);
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        // U: Unload the AssetStore-managed checker texture.
        if (host.Input.WasKeyPressed(Keyboard.Key.U) && _checkerTexture is not null)
        {
            host.Assets.Unload(Catalog.Assets.CheckerTexturePath);
            _checkerTexture = null;
            EngineLogger.Log($"AssetStore.Unload called for checker texture.");
        }

        // R: Reload the checker texture from disk.
        if (host.Input.WasKeyPressed(Keyboard.Key.R) && _checkerTexture is null)
        {
            _checkerTexture = host.Assets.LoadTexture(Catalog.Assets.CheckerTexturePath);
            EngineLogger.Log("AssetStore.LoadTexture — checker texture reloaded.");
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        float cy = host.Window.Size.Y / 2f;

        DrawTextureCard(window, _checkerTexture, new Vector2f(220f, cy), 3f, "AssetStore\n(managed)");
        DrawTextureCard(window, _circleTexture, new Vector2f(520f, cy), 2f, "ToTexture()\ncircle");
        DrawTextureCard(window, _polygonTexture, new Vector2f(820f, cy), 2f, "ToTexture()\nstar");

        string checkerState = _checkerTexture is not null ? "loaded" : "UNLOADED";

        using Text hud = new(_font,
            $"U  unload checker via AssetStore  ·  R  reload\n" +
            $"Checker: {checkerState}  ·  Shape textures: caller-owned (not in AssetStore)\n" +
            $"Logger: {_lastLogMessage}", 18)
        {
            Position = new Vector2f(20f, 20f),
            FillColor = new Color(220, 230, 255)
        };
        window.Draw(hud);
    }

    private void DrawTextureCard(RenderWindow window, Texture? texture, Vector2f center, float scale, string label)
    {
        if (texture is not null)
        {
            using Sprite sprite = new(texture)
            {
                Scale = new Vector2f(scale, scale)
            };

            FloatRect lb = sprite.GetLocalBounds();
            sprite.Origin = new Vector2f(lb.Width / 2f, lb.Height / 2f);
            sprite.Position = center;
            window.Draw(sprite);
        }
        else
        {
            using RectangleShape placeholder = new(new Vector2f(96f, 96f))
            {
                Origin = new Vector2f(48f, 48f),
                Position = center,
                FillColor = Color.Transparent,
                OutlineColor = new Color(100, 100, 100),
                OutlineThickness = 2f
            };
            window.Draw(placeholder);
        }

        using Text lbl = new(_font, label, 16)
        {
            FillColor = texture is not null ? new Color(200, 220, 255) : new Color(100, 100, 100)
        };

        FloatRect lblBounds = lbl.GetLocalBounds();
        lbl.Origin = new Vector2f(lblBounds.Width / 2f, 0f);
        lbl.Position = new Vector2f(center.X, center.Y + 90f);
        window.Draw(lbl);
    }

    private static Texture CreateStarTexture()
    {
        using ConvexShape star = new(10);

        star.FillColor = new Color(255, 200, 50);

        const float outerR = 38f;
        const float innerR = 16f;
        const float cx = 48f;
        const float cy = 48f;

        for (int i = 0; i < 10; i++)
        {
            float angle = i * MathF.PI / 5f - MathF.PI / 2f;
            float r = i % 2 == 0 ? outerR : innerR;

            star.SetPoint((uint)i, new Vector2f(cx + r * MathF.Cos(angle), cy + r * MathF.Sin(angle)));
        }

        return star.ToTexture(4);
    }
}
