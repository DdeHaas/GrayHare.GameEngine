using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.MusicDemo;

/// <summary>
/// Demonstrates music streaming and volume control.
/// Starts playing the bundled MP3 track on load and exposes the full
/// master/music/SFX volume pipeline via keyboard.
/// </summary>
internal sealed class MusicScene : DemoSceneBase
{
    private const string MusicTrackPath = "audio/magpiemusic-action-trailer-promo-rock-513687.mp3";

    private Font? _font;

    public MusicScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();

        host.Audio.SetMasterVolume(25f);
        host.Audio.SetMusicVolume(40f);
        host.Audio.PlayMusic(MusicTrackPath, volume: 100f, loop: true);
    }

    public override void Unload(GameHost host)
    {
        host.Audio.StopMusic();
        base.Unload(host);
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        // Volume adjustment with Up/Down.
        if (host.Input.WasKeyPressed(Keyboard.Key.Up))
        {
            host.Audio.SetMasterVolume(host.Audio.MasterVolume + 5f);
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Down))
        {
            host.Audio.SetMasterVolume(host.Audio.MasterVolume - 5f);
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Right))
        {
            host.Audio.SetMusicVolume(host.Audio.MusicVolume + 5f);
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Left))
        {
            host.Audio.SetMusicVolume(host.Audio.MusicVolume - 5f);
        }

        // Mute toggle.
        if (host.Input.WasKeyPressed(Keyboard.Key.M))
        {
            if (host.Audio.IsMuted)
            {
                host.Audio.Unmute();
            }
            else
            {
                host.Audio.Mute();
            }
        }

        // Music pause / resume.
        if (host.Input.WasKeyPressed(Keyboard.Key.P))
        {
            if (host.Audio.IsMusicPlaying)
            {
                host.Audio.PauseMusic();
            }
            else
            {
                host.Audio.ResumeMusic();
            }
        }

        // Play beep with Space.
        if (host.Input.WasKeyPressed(Keyboard.Key.Space))
        {
            host.Audio.PlaySound(Catalog.Assets.BeepSoundPath, volume: 60f);
        }

        // SFX volume presets with number keys.
        if (host.Input.WasKeyPressed(Keyboard.Key.Num1))
        {
            host.Audio.SetSfxVolume(33f);
            return;
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Num2))
        {
            host.Audio.SetSfxVolume(66f);
            return;
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Num3))
        {
            host.Audio.SetSfxVolume(100f);
            return;
        }

        base.Update(host, in gameTime);
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        if (_font is null)
        {
            return;
        }

        float y = 40f;
        const float lineHeight = 36f;

        string musicStatus = host.Audio.IsMusicPlaying ? "Playing" : "Paused";
        Color musicStatusColor = host.Audio.IsMusicPlaying ? new Color(100, 255, 100) : new Color(255, 220, 60);
        DrawLine(window, $"Music: {musicStatus}", 24, new Vector2f(40f, y), musicStatusColor);
        y += lineHeight;

        DrawLine(window, $"Master Volume: {host.Audio.MasterVolume:F0}%", 24, new Vector2f(40f, y), new Color(220, 230, 255));
        y += lineHeight;

        DrawLine(window, $"SFX Volume:    {host.Audio.SfxVolume:F0}%", 24, new Vector2f(40f, y), new Color(200, 220, 240));
        y += lineHeight;

        DrawLine(window, $"Music Volume:  {host.Audio.MusicVolume:F0}%", 24, new Vector2f(40f, y), new Color(200, 220, 240));
        y += lineHeight;

        string mutedState = host.Audio.IsMuted ? "YES" : "NO";
        Color mutedColor = host.Audio.IsMuted ? new Color(255, 100, 100) : new Color(100, 255, 100);
        DrawLine(window, $"Muted: {mutedState}", 24, new Vector2f(40f, y), mutedColor);
        y += lineHeight;

        DrawLine(window, $"Active Sounds: {host.Audio.ActiveSoundCount}", 20, new Vector2f(40f, y), new Color(180, 180, 180));
        y += lineHeight * 2f;

        // Volume bar visualization.
        DrawVolumeBar(window, "Master", host.Audio.MasterVolume, new Vector2f(40f, y), new Color(80, 200, 255));
        y += 50f;
        DrawVolumeBar(window, "SFX", host.Audio.SfxVolume, new Vector2f(40f, y), new Color(80, 255, 120));
        y += 50f;
        DrawVolumeBar(window, "Music", host.Audio.MusicVolume, new Vector2f(40f, y), new Color(255, 200, 80));
    }

    private void DrawLine(RenderWindow window, string text, uint size, Vector2f position, Color color)
    {
        using Text line = new(_font!, text, size)
        {
            Position = position,
            FillColor = color
        };
        window.Draw(line);
    }

    private void DrawVolumeBar(RenderWindow window, string label, float volume, Vector2f position, Color color)
    {
        const float barWidth = 300f;
        const float barHeight = 24f;

        // Background.
        using RectangleShape bg = new(new Vector2f(barWidth, barHeight))
        {
            Position = new Vector2f(position.X + 80f, position.Y),
            FillColor = new Color(40, 40, 50),
            OutlineColor = new Color(80, 80, 100),
            OutlineThickness = 1f
        };
        window.Draw(bg);

        // Fill.
        float fillWidth = barWidth * Math.Clamp(volume, 0f, 100f) / 100f;
        using RectangleShape fill = new(new Vector2f(fillWidth, barHeight))
        {
            Position = new Vector2f(position.X + 80f, position.Y),
            FillColor = color
        };
        window.Draw(fill);

        // Label.
        if (_font is not null)
        {
            using Text lbl = new(_font, label, 16)
            {
                Position = new Vector2f(position.X, position.Y + 2f),
                FillColor = new Color(200, 200, 200)
            };
            window.Draw(lbl);
        }
    }
}
