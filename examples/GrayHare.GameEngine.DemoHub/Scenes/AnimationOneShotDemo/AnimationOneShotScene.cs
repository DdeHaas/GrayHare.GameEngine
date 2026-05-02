using GrayHare.GameEngine.Animation;
using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.AnimationOneShotDemo;

/// <summary>
/// Demonstrates <see cref="AnimationPlayer.Reset"/>, <see cref="AnimationPlayer.IsFinished"/>,
/// and pause/resume on a non-looping animation clip.
/// </summary>
internal sealed class AnimationOneShotScene : DemoSceneBase
{
    private Font _font = null!;
    private AnimationClip? _clip;
    private AnimationPlayer? _player;

    public AnimationOneShotScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();

        Texture[] textures = Catalog.Assets.ExplosionTexturePaths
            .Select(path => host.Assets.LoadTexture(path, smooth: true))
            .ToArray();

        _clip = AnimationClip.CreateFromTextures(
            "explosion",
            textures,
            TimeSpan.FromMilliseconds(80));

        _player = new AnimationPlayer(_clip, isLooping: false, autoPlay: true)
        {
            Position = new Vector2f(640f, 360f),
            Scale = new Vector2f(0.9f, 0.9f),
        };
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        _player?.Update(gameTime.Delta);

        // Space: reset to frame 0, clear the finished flag, and resume if paused.
        if (host.Input.WasKeyPressed(Keyboard.Key.Space) && _player is not null)
        {
            _player.Reset();

            if (_player.IsPaused)
            {
                _player.Resume();
            }
        }

        // P: toggle pause / resume.
        if (host.Input.WasKeyPressed(Keyboard.Key.P) && _player is not null)
        {
            if (_player.IsPaused)
            {
                _player.Resume();
            }
            else
            {
                _player.Pause();
            }
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        _player?.Render(window);

        bool finished = _player?.IsFinished ?? false;
        bool paused = _player?.IsPaused ?? false;

        string status = (finished, paused) switch
        {
            (true, _) => "FINISHED",
            (_, true) => "PAUSED",
            _ => "PLAYING"
        };

        Color statusColor = finished
            ? new Color(255, 80, 80)
            : paused
                ? new Color(255, 200, 60)
                : new Color(100, 255, 100);

        using Text statusText = new(_font, status, 36)
        {
            FillColor = statusColor
        };

        FloatRect statusBounds = statusText.GetLocalBounds();
        statusText.Origin = new Vector2f(statusBounds.Width / 2f, 0f);
        statusText.Position = new Vector2f(640f, 200f);
        window.Draw(statusText);

        using Text hint = new(_font,
            "Non-looping animation  ·  Space  reset + replay  ·  P  pause / resume", 18)
        {
            Position = new Vector2f(20f, 20f),
            FillColor = new Color(220, 230, 255)
        };
        window.Draw(hint);
    }

    public override void Unload(GameHost host)
    {
        base.Unload(host);

        _player?.Dispose();
        _clip?.Dispose();
    }
}
