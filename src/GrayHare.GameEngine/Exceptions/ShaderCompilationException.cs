namespace GrayHare.GameEngine.Exceptions;

/// <summary>
/// Thrown when an SFML shader fails to compile or link.
/// </summary>
public sealed class ShaderCompilationException : InvalidOperationException
{
    /// <summary>Initializes with the shader key and compilation error.</summary>
    public ShaderCompilationException(string shaderKey, string error)
        : base($"Shader '{shaderKey}' failed to compile: {error}")
    {
        ShaderKey = shaderKey;
    }

    /// <summary>The cache key identifying the shader that failed.</summary>
    public string ShaderKey { get; }
}
