using System.Collections.ObjectModel;

namespace GrayHare.GameEngine.DemoHub;

/// <summary>Holds paths to all generated demo textures, sounds, and shader sources.</summary>
internal sealed record DemoAssetsManifest
{
    public DemoAssetsManifest(
        string backgroundImagePath,
        string checkerTexturePath,
        string spriteSheetPath,
        string beepSoundPath,
        string musicTrackPath,
        string grayscaleFragPath,
        string waveVertPath,
        string waveFragPath,
        string theHighlanderFragPath,
        string pixelateFragPath,
        string blurFragPath,
        string blinkFragPath,
        string stormVertPath,
        ReadOnlyCollection<string> explosionTexturePaths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backgroundImagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(checkerTexturePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(spriteSheetPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(beepSoundPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(musicTrackPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(grayscaleFragPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(waveVertPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(waveFragPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(theHighlanderFragPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(pixelateFragPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(blurFragPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(blinkFragPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(stormVertPath);
        if (explosionTexturePaths.Count == 0)
        {
            throw new ArgumentException("At least one explosion texture path must be provided.", nameof(explosionTexturePaths));
        }

        BackgroundImagePath = backgroundImagePath;
        CheckerTexturePath = checkerTexturePath;
        SpriteSheetPath = spriteSheetPath;
        BeepSoundPath = beepSoundPath;
        MusicTrackPath = musicTrackPath;
        GrayscaleFragPath = grayscaleFragPath;
        WaveVertPath = waveVertPath;
        WaveFragPath = waveFragPath;
        TheHighlanderFragPath = theHighlanderFragPath;
        PixelateFragPath = pixelateFragPath;
        BlurFragPath = blurFragPath;
        BlinkFragPath = blinkFragPath;
        StormVertPath = stormVertPath;
        ExplosionTexturePaths = explosionTexturePaths;
    }

    public string BackgroundImagePath { get; init; }
    public string CheckerTexturePath { get; init; }
    public string SpriteSheetPath { get; init; }
    public string BeepSoundPath { get; init; }
    public string MusicTrackPath { get; init; }
    public string GrayscaleFragPath { get; init; }
    public string WaveVertPath { get; init; }
    public string WaveFragPath { get; init; }
    public string TheHighlanderFragPath { get; init; }
    public string PixelateFragPath { get; init; }
    public string BlurFragPath { get; init; }
    public string BlinkFragPath { get; init; }
    public string StormVertPath { get; init; }
    public ReadOnlyCollection<string> ExplosionTexturePaths { get; init; }
}
