using GrayHare.GameEngine.Animation;
using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.ExplosionAnimationDemo;

internal sealed class ExplosionAnimationScene : DemoSceneBase
{
    private AnimationClip? _clip;

    private readonly List<AnimationPlayer> _explosionPlayers = [];
    private readonly Random _random = Random.Shared;
    private bool _isAnimating = true;

    public ExplosionAnimationScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex)
    {
    }

    public override void Load(GameHost host)
    {
        base.Load(host);

        Texture[] textures = Catalog.Assets.ExplosionTexturePaths
            .Select(path => host.Assets.LoadTexture(path, smooth: true))
            .ToArray();

        _clip = AnimationClip.CreateFromTextures(
            "explosion",
            textures,
            TimeSpan.FromMilliseconds(80));

        _explosionPlayers.AddRange([
            new AnimationPlayer(_clip!, isLooping: true) { Position = new Vector2f(640f, 360f), Scale = new Vector2f(1.5f, 1.5f), FrameIndex = 0 },
            new AnimationPlayer(_clip!, isLooping: true) { Position = new Vector2f(210f, 270f), Scale = new Vector2f(0.8f, 0.8f), FrameIndex = 3 },
            new AnimationPlayer(_clip!, isLooping: true) { Position = new Vector2f(1060, 270f), Scale = new Vector2f(1f, 1f), FrameIndex = 8 },
            new AnimationPlayer(_clip!, isLooping: true) { Position = new Vector2f(320, 545f), Scale = new Vector2f(0.65f, 0.65f), FrameIndex = 7 },
            new AnimationPlayer(_clip!, isLooping: true) { Position = new Vector2f(920, 510f), Scale = new Vector2f(0.90f, 0.90f), FrameIndex = 4 },
            new AnimationPlayer(_clip!, isLooping: true) { Position = new Vector2f(640, 130f), Scale = new Vector2f(0.70f, 0.70f), FrameIndex = 6 },
        ]);
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        if (_explosionPlayers.Count > 0 && host.Input.WasKeyPressed(Keyboard.Key.P))
        {
            _isAnimating = !_isAnimating;

            foreach (AnimationPlayer player in _explosionPlayers)
            {
                if (_isAnimating)
                {
                    player.Resume();
                }
                else
                {
                    player.Pause();
                }
            }
        }

        if (!_isAnimating)
        {
            return;
        }

        foreach (AnimationPlayer player in _explosionPlayers)
        {
            player.Update(gameTime.Delta);
        }

        if (host.Input.WasMouseButtonPressed(Mouse.Button.Left))
        {
            Vector2f pos = new(host.Input.MousePosition.X, host.Input.MousePosition.Y);
            float scale = 0.5f + (_random.NextSingle() * 1.2f);
            int skipFrames = _random.Next(_clip!.Frames.Count);

            AnimationPlayer player = new(_clip!, isLooping: true, autoPlay: false)
            {
                Position = pos,
                Scale = new Vector2f(scale, scale),
                FrameIndex = skipFrames
            };

            _explosionPlayers.Add(player);
        }

        if (host.Input.WasMouseButtonPressed(Mouse.Button.Right))
        {
            AnimationPlayer? last = _explosionPlayers.LastOrDefault();
            if (last is not null)
            {
                _explosionPlayers.Remove(last);
                last.Dispose();
            }
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        foreach (AnimationPlayer player in _explosionPlayers)
        {
            player.Render(window);
        }

        using Text label = new(
            host.Assets.LoadFont(),
            $"Explosions: {_explosionPlayers.Count}\n" +
            "- left click to add\n" +
            "- right click to remove",
            22)
        {
            Position = new Vector2f(20f, 20f),
            FillColor = new Color(220, 230, 255)
        };

        window.Draw(label);
    }

    public override void Unload(GameHost host)
    {
        foreach (AnimationPlayer player in _explosionPlayers)
        {
            player.Dispose();
        }

        _explosionPlayers.Clear();
        _clip?.Dispose();
    }
}
