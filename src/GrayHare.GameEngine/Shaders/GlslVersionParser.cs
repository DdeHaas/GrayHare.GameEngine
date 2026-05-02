using System.Text.RegularExpressions;

namespace GrayHare.GameEngine.Shaders;

/// <summary>
/// Extracts the required GLSL version number from a shader source string
/// by reading its <c>#version</c> preprocessor directive.
/// </summary>
public static class GlslVersionParser
{
    // Matches:  #version 460
    //           #version 460 core
    // Leading whitespace is tolerated; the optional profile token is ignored.
    private static readonly Regex _versionDirective =
        new(@"^\s*#version\s+(\d+)", RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Returns the integer version number from a shader's <c>#version</c> directive,
    /// or <see langword="null"/> if no directive is present.
    /// </summary>
    /// <example>
    /// <c>#version 460 core</c> → <c>460</c>
    /// </example>
    public static int? Parse(string shaderSource)
    {
        Match match = _versionDirective.Match(shaderSource);

        return match.Success && int.TryParse(match.Groups[1].Value, out int version)
            ? version
            : null;
    }
}
