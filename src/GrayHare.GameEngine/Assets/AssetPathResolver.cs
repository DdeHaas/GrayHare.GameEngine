namespace GrayHare.GameEngine.Assets;

/// <summary>
/// Resolves asset paths relative to a content root directory.
/// </summary>
public static class AssetPathResolver
{
    /// <summary>
    /// Normalizes <paramref name="contentRoot"/> to an absolute path.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="contentRoot"/> is null or whitespace.
    /// </exception>
    public static string NormalizeContentRoot(string contentRoot)
    {
        if (string.IsNullOrWhiteSpace(contentRoot))
        {
            throw new ArgumentException("Content root must not be empty.", nameof(contentRoot));
        }

        return Path.GetFullPath(contentRoot);
    }

    /// <summary>
    /// Resolves <paramref name="assetPath"/> against <paramref name="contentRoot"/>.
    /// Does not verify that the file exists.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="assetPath"/> is null or whitespace.</exception>
    public static string ResolvePath(string contentRoot, string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            throw new ArgumentException("Asset path must not be empty.", nameof(assetPath));
        }

        string fullPath = Path.IsPathRooted(assetPath)
            ? Path.GetFullPath(assetPath)
            : Path.GetFullPath(Path.Combine(NormalizeContentRoot(contentRoot), assetPath));

        return fullPath;
    }
}
