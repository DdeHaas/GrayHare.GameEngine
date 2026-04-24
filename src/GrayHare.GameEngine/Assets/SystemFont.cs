using System.Runtime.InteropServices;

namespace GrayHare.GameEngine.Assets;

/// <summary>
/// Provides methods for locating a system font file path appropriate for the current operating system.
/// </summary>
/// <remarks>This class is intended to assist in cross-platform scenarios where a standard system font is
/// required. The selection of the font file is based on common font availability for Windows, macOS, and Linux
/// platforms. The returned path can be used to load the font in applications that require explicit font file
/// references.</remarks>
public static class SystemFont
{
    /// <summary>
    /// Resolves a system font path for Windows, Linux, or macOS.
    /// </summary>
    /// <returns>The absolute path to a system font file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when none of the expected system fonts were found.</exception>
    public static string FindSystemFont()
    {
        IEnumerable<string> paths;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string winFonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

            paths =
                [
                    Path.Combine(winFonts, "arial.ttf"),
                    Path.Combine(winFonts, "cour.ttf"),
                ];
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            paths =
                [
                    "/System/Library/Fonts/Supplemental/Arial.ttf",
                    "/System/Library/Fonts/Supplemental/Courier New.ttf",
                ];
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            paths =
                [
                    "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
                    "/usr/share/fonts/truetype/liberation/LiberationMono-Regular.ttf",
                    "/usr/share/fonts/liberation/LiberationSans-Regular.ttf",
                    "/usr/share/fonts/liberation/LiberationMono-Regular.ttf",
                ];
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported operating system.");
        }

        foreach (string path in paths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException($"The system font files were not found: {string.Join(',', paths)}");
    }
}
