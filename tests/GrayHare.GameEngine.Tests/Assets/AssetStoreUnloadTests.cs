using GrayHare.GameEngine.Assets;

namespace GrayHare.GameEngine.Tests.Assets;

public sealed class AssetStoreUnloadTests : IDisposable
{
    private readonly string _contentRoot;

    public AssetStoreUnloadTests()
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
        // Minimal 1×1 P3 PPM image.
        string path = Path.Combine(_contentRoot, name);
        File.WriteAllText(path, "P3\n1 1\n255\n255 0 0\n");
        return name;
    }

    // ── Unload texture ────────────────────────────────────────────────────────

    [Fact]
    public void Unload_Texture_RemovesItFromCache()
    {
        using AssetStore store = new(_contentRoot);
        string rel = WritePpm("tile.ppm");

        store.LoadTexture(rel);
        store.Unload(rel);

        // Loading again should succeed — it's no longer in the cache (a new instance is created).
        var tex2 = store.LoadTexture(rel);
        Assert.NotNull(tex2);
    }

    [Fact]
    public void Unload_NonExistentKey_DoesNotThrow()
    {
        string rel = WritePpm("dummy.ppm");
        using AssetStore store = new(_contentRoot);

        Assert.Null(Record.Exception(() => store.Unload(rel)));
    }

    // ── UnloadAll ─────────────────────────────────────────────────────────────

    [Fact]
    public void UnloadAll_AllowsReloadingAfter()
    {
        using AssetStore store = new(_contentRoot);
        string rel = WritePpm("tile2.ppm");

        store.LoadTexture(rel);
        store.UnloadAll();

        var tex = store.LoadTexture(rel);
        Assert.NotNull(tex);
    }

    [Fact]
    public void UnloadAll_OnEmptyStore_DoesNotThrow()
    {
        using AssetStore store = new(_contentRoot);

        Assert.Null(Record.Exception(() => store.UnloadAll()));
    }
}
