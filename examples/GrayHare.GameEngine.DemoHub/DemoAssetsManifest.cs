namespace GrayHare.GameEngine.DemoHub;

/// <summary>Holds paths to all generated demo textures, sounds, and shader sources.</summary>
internal sealed record DemoAssetsManifest
{
    public DemoAssetsManifest(
        string checkerTexturePath,
        string spriteSheetPath,
        string beepSoundPath,
        string grayscaleFragPath,
        string waveVertPath,
        string waveFragPath,
        string theHighlanderFragPath,
        string pixelateFragPath,
        string blurFragPath,
        string blinkFragPath,
        string stormVertPath)
    {
        CheckerTexturePath = checkerTexturePath;
        SpriteSheetPath = spriteSheetPath;
        BeepSoundPath = beepSoundPath;
        GrayscaleFragPath = grayscaleFragPath;
        WaveVertPath = waveVertPath;
        WaveFragPath = waveFragPath;
        TheHighlanderFragPath = theHighlanderFragPath;
        PixelateFragPath = pixelateFragPath;
        BlurFragPath = blurFragPath;
        BlinkFragPath = blinkFragPath;
        StormVertPath = stormVertPath;
    }

    public string CheckerTexturePath { get; init; }
    public string SpriteSheetPath { get; init; }
    public string BeepSoundPath { get; init; }
    public string GrayscaleFragPath { get; init; }
    public string WaveVertPath { get; init; }
    public string WaveFragPath { get; init; }
    public string TheHighlanderFragPath { get; init; }
    public string PixelateFragPath { get; init; }
    public string BlurFragPath { get; init; }
    public string BlinkFragPath { get; init; }
    public string StormVertPath { get; init; }
}
