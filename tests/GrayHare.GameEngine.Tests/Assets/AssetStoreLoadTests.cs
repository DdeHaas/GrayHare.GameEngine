using GrayHare.GameEngine.Assets;
using GrayHare.GameEngine.Exceptions;
using SFML.Graphics;

namespace GrayHare.GameEngine.Tests.Assets;

public sealed class AssetStoreLoadTests : IDisposable
{
    private readonly string _contentRoot;

    public AssetStoreLoadTests()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_contentRoot);
    }

    public void Dispose()
    {
        Directory.Delete(_contentRoot, recursive: true);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string WritePpm(string name)
    {
        string path = Path.Combine(_contentRoot, name);
        File.WriteAllText(path, "P3\n1 1\n255\n255 0 0\n");
        return name;
    }

    // ── LoadImage ─────────────────────────────────────────────────────────────

    [Fact]
    public void LoadImage_ThrowsAssetNotFoundException_WhenFileNotFound()
    {
        using var store = new AssetStore(_contentRoot);
        const string missing = "missing.png";

        var ex = Assert.Throws<AssetNotFoundException>(() => store.LoadImage(missing));

        Assert.Equal(missing, ex.AssetPath);
    }

    [Fact]
    public void LoadImage_ReturnsSameInstance_OnSecondCall()
    {
        using var store = new AssetStore(_contentRoot);
        string rel = WritePpm("img.ppm");

        Image first = store.LoadImage(rel);
        Image second = store.LoadImage(rel);

        Assert.Same(first, second);
    }

    // ── LoadTexture ───────────────────────────────────────────────────────────

    [Fact]
    public void LoadTexture_ReturnsFallbackTexture_WhenFileNotFound()
    {
        using var store = new AssetStore(_contentRoot);

        Texture result = store.LoadTexture("missing.png");

        Assert.NotNull(result);
    }

    [Fact]
    public void LoadTexture_ReturnsSameInstance_OnSecondCall()
    {
        using var store = new AssetStore(_contentRoot);
        string rel = WritePpm("tex.ppm");

        Texture first = store.LoadTexture(rel);
        Texture second = store.LoadTexture(rel);

        Assert.Same(first, second);
    }

    [Fact]
    public void LoadTexture_MissingFile_ReturnsSameFallback_OnEveryCall()
    {
        using var store = new AssetStore(_contentRoot);

        Texture first = store.LoadTexture("a.png");
        Texture second = store.LoadTexture("b.png");

        // Both missing paths should return the same shared fallback instance.
        Assert.Same(first, second);
    }

    [Fact]
    public void LoadTexture_DifferentSmoothValues_ReturnDifferentInstances()
    {
        using var store = new AssetStore(_contentRoot);
        string rel = WritePpm("smooth_test.ppm");

        Texture notSmooth = store.LoadTexture(rel, smooth: false);
        Texture smooth = store.LoadTexture(rel, smooth: true);

        Assert.NotSame(notSmooth, smooth);
    }

    [Fact]
    public void LoadTexture_SameSmoothValue_ReturnsSameInstance()
    {
        using var store = new AssetStore(_contentRoot);
        string rel = WritePpm("cache_test.ppm");

        Texture first = store.LoadTexture(rel, smooth: true);
        Texture second = store.LoadTexture(rel, smooth: true);

        Assert.Same(first, second);
    }

    // ── LoadSoundBuffer ───────────────────────────────────────────────────────

    [Fact]
    public void LoadSoundBuffer_ThrowsAssetNotFoundException_WhenFileNotFound()
    {
        using var store = new AssetStore(_contentRoot);
        const string missing = "sfx/missing.wav";

        var ex = Assert.Throws<AssetNotFoundException>(() => store.LoadSoundBuffer(missing));

        Assert.Equal(missing, ex.AssetPath);
    }

    // ── LoadShader (fragment-only) ────────────────────────────────────────────

    [Fact]
    public void LoadShader_ThrowsAssetNotFoundException_WhenFragFileNotFound()
    {
        using var store = new AssetStore(_contentRoot);
        const string missing = "shaders/missing.frag";

        var ex = Assert.Throws<AssetNotFoundException>(() => store.LoadShader(missing));

        Assert.Equal(missing, ex.AssetPath);
    }

    // ── LoadShader (vertex + fragment) ────────────────────────────────────────

    [Fact]
    public void LoadShader_ThrowsAssetNotFoundException_WhenVertFileNotFound()
    {
        using var store = new AssetStore(_contentRoot);
        const string missingVert = "shaders/missing.vert";
        const string missingFrag = "shaders/missing.frag";

        var ex = Assert.Throws<AssetNotFoundException>(() => store.LoadShader(missingVert, missingFrag));

        Assert.Equal(missingVert, ex.AssetPath);
    }

    [Fact]
    public void LoadShader_ThrowsAssetNotFoundException_WhenFragFileNotFound_WithVertPresent()
    {
        using var store = new AssetStore(_contentRoot);
        string vert = Path.GetFileName(Path.GetTempFileName());
        string vertPath = Path.Combine(_contentRoot, vert);
        File.WriteAllText(vertPath, "void main() {}");
        const string missingFrag = "shaders/missing.frag";

        var ex = Assert.Throws<AssetNotFoundException>(() => store.LoadShader(vert, missingFrag));

        Assert.Equal(missingFrag, ex.AssetPath);
    }

    // ── TryLoadShader ─────────────────────────────────────────────────────────

    [Fact]
    public void TryLoadShader_ThrowsAssetNotFoundException_WhenFragFileNotFound()
    {
        using var store = new AssetStore(_contentRoot);
        const string missing = "shaders/missing.frag";

        var ex = Assert.Throws<AssetNotFoundException>(() => store.TryLoadShader(missing, out _));

        Assert.Equal(missing, ex.AssetPath);
    }

    [Fact]
    public void TryLoadShader_VertFrag_ThrowsAssetNotFoundException_WhenVertFileNotFound()
    {
        using var store = new AssetStore(_contentRoot);
        const string missing = "shaders/missing.vert";

        var ex = Assert.Throws<AssetNotFoundException>(() => store.TryLoadShader(missing, "shaders/any.frag", out _));

        Assert.Equal(missing, ex.AssetPath);
    }
}
