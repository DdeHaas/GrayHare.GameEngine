namespace GrayHare.GameEngine.Exceptions;

/// <summary>
/// Thrown when a requested asset file cannot be found at the resolved path.
/// </summary>
public sealed class AssetNotFoundException : FileNotFoundException
{
    /// <summary>Initializes with the asset path that was not found.</summary>
    public AssetNotFoundException(string assetPath)
        : base($"The asset '{assetPath}' could not be found.", assetPath)
    {
        AssetPath = assetPath;
    }

    /// <summary>Initializes with the asset path and an inner exception.</summary>
    public AssetNotFoundException(string assetPath, Exception innerException)
        : base($"The asset '{assetPath}' could not be found.", assetPath, innerException)
    {
        AssetPath = assetPath;
    }

    /// <summary>The asset-relative path that was requested.</summary>
    public string AssetPath { get; }
}
