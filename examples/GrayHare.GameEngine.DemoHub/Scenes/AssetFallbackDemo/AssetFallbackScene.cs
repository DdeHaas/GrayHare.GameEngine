using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.AssetFallbackDemo;

/// <summary>
/// Demonstrates asset fallback behavior by loading a valid texture alongside
/// an intentionally missing one, showing the magenta checkerboard fallback.
/// </summary>
internal sealed class AssetFallbackScene : DemoSceneBase
{
    private Font? _font;
    private Texture? _validTexture;
    private Texture? _missingTexture;

    public AssetFallbackScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();

        // Load a valid texture from the generated assets.
        _validTexture = host.Assets.LoadTexture(Catalog.Assets.CheckerTexturePath);

        // Load an intentionally missing texture — triggers the magenta checkerboard fallback.
        _missingTexture = host.Assets.LoadTexture("nonexistent/missing.png");
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        float cx = host.Window.Size.X / 2f;
        float cy = host.Window.Size.Y / 2f;
        const float spacing = 180f;
        const float spriteScale = 2f;

        // Draw valid texture.
        if (_validTexture is not null)
        {
            using Sprite validSprite = new(_validTexture)
            {
                Position = new Vector2f(cx - spacing - 48f, cy - 60f),
                Scale = new Vector2f(spriteScale, spriteScale)
            };
            window.Draw(validSprite);
        }

        // Draw missing texture (fallback).
        if (_missingTexture is not null)
        {
            using Sprite missingSprite = new(_missingTexture)
            {
                Position = new Vector2f(cx + spacing - 48f, cy - 60f),
                Scale = new Vector2f(spriteScale * 3f, spriteScale * 3f)
            };
            window.Draw(missingSprite);
        }

        if (_font is null)
        {
            return;
        }

        // Labels.
        using Text validLabel = new(_font, "Valid Asset", 22)
        {
            FillColor = new Color(100, 255, 100)
        };
        validLabel.Position = new Vector2f(
            cx - spacing - validLabel.GetLocalBounds().Width / 2f, cy + 90f);
        window.Draw(validLabel);

        using Text missingLabel = new(_font, "Missing Asset (Fallback)", 22)
        {
            FillColor = new Color(255, 100, 255)
        };
        missingLabel.Position = new Vector2f(
            cx + spacing - missingLabel.GetLocalBounds().Width / 2f, cy + 90f);
        window.Draw(missingLabel);
    }
}
