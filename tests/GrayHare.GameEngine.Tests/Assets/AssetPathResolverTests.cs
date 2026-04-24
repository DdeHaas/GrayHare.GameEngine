using GrayHare.GameEngine.Assets;

namespace GrayHare.GameEngine.Tests.Assets;

public sealed class AssetPathResolverTests
{
    [Fact]
    public void ResolvePath_CombinesContentRootAndRelativePath()
    {
        string contentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(contentRoot);
        string assetPath = Path.Combine(contentRoot, "example.txt");
        File.WriteAllText(assetPath, "demo");

        string resolved = AssetPathResolver.ResolvePath(contentRoot, "example.txt");

        Assert.Equal(assetPath, resolved);
    }

    [Fact]
    public void NormalizeContentRoot_ThrowsForEmptyString()
    {
        Assert.Throws<ArgumentException>(() => AssetPathResolver.NormalizeContentRoot(""));
    }

    [Fact]
    public void ResolvePath_ThrowsForEmptyAssetPath()
    {
        string contentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(contentRoot);

        Assert.Throws<ArgumentException>(
            () => AssetPathResolver.ResolvePath(contentRoot, ""));
    }

    [Fact]
    public void NormalizeContentRoot_ThrowsForWhitespaceOnly()
    {
        Assert.Throws<ArgumentException>(() => AssetPathResolver.NormalizeContentRoot("   "));
    }

    [Fact]
    public void ResolvePath_WithAbsolutePath_ReturnsNormalizedAbsolutePath()
    {
        string absolutePath = Path.GetTempPath();

        string resolved = AssetPathResolver.ResolvePath("irrelevant-root", absolutePath);

        Assert.Equal(Path.GetFullPath(absolutePath), resolved);
    }

    [Fact]
    public void ResolvePath_ThrowsForWhitespaceAssetPath()
    {
        string contentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(contentRoot);

        Assert.Throws<ArgumentException>(
            () => AssetPathResolver.ResolvePath(contentRoot, "   "));
    }
}
