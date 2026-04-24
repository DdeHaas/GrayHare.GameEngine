using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.AudioDemo;

internal sealed class AudioScene : DemoSceneBase
{
    // Null means no sound has been triggered yet this session.
    private TimeSpan? _soundStartTime;

    // Cached each frame so Render can read it without a parameter change.
    private TimeSpan _currentTime;

    public AudioScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        _currentTime = gameTime.Total;

        if (host.Input.WasKeyPressed(Keyboard.Key.Space))
        {
            host.Audio.PlaySound(Catalog.Assets.BeepSoundPath, volume: 40f);

            // Record start time to drive the vibration; AudioPlayer owns the Sound lifetime.
            _soundStartTime = gameTime.Total;
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // Elapsed seconds since the last beep; 0 when no sound has been played yet.
        float elapsed = _soundStartTime is { } start
            ? (float)(_currentTime - start).TotalSeconds
            : 0f;

        // Amplitude decays exponentially — naturally fades to zero well before 1 s.
        float decay = MathF.Exp(-elapsed * 4f);

        for (int ring = 0; ring < 5; ring++)
        {
            float baseRadius = 50f + (ring * 40f);

            // Each ring is phase-shifted so they ripple outward rather than pulsing in lockstep.
            float phaseOffset = ring * MathF.PI / 5f;
            float ringVibration = MathF.Sin((elapsed * 60f) + phaseOffset) * 10f * decay;
            float radius = baseRadius + ringVibration;

            using CircleShape shape = new(radius)
            {
                Origin = new(radius, radius),
                Position = new(640f, 360f),
                FillColor = Color.Transparent,
                OutlineColor = new Color((byte)(80 + (ring * 25)), (byte)(120 + (ring * 15)), 255),
                OutlineThickness = 4f
            };

            window.Draw(shape);
        }


    }
}
