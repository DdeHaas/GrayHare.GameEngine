using GrayHare.GameEngine.Diagnostics;
using GrayHare.GameEngine.Exceptions;
using GrayHare.GameEngine.Shaders;
using SFML.Audio;
using SFML.Graphics;
using SFML.System;
using System.Text;

namespace GrayHare.GameEngine.Assets;

/// <summary>
/// Caches and provides access to textures, fonts, and sound buffers loaded from disk.
/// Supports all common image formats as well as PPM P3 (ASCII) and PPM P6 (binary) files.
/// </summary>
/// <remarks>This type is not thread-safe. Access all members from the main thread only.</remarks>
public sealed class AssetStore : IDisposable
{
    private readonly Dictionary<string, Image> _images = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Texture> _textures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Font> _fonts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SoundBuffer> _soundBuffers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Shader> _shaders = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _shaderFailures = new(StringComparer.OrdinalIgnoreCase);
    private Texture? _fallbackTexture;
    private bool _disposed;

    /// <summary>Initializes the store with the given content root directory.</summary>
    public AssetStore(string contentRootPath)
    {
        ContentRootPath = AssetPathResolver.NormalizeContentRoot(contentRootPath);
    }

    /// <summary>Absolute path to the root directory that asset paths are resolved against.</summary>
    public string ContentRootPath { get; }

    /// <summary>Resolves <paramref name="assetPath"/> against <see cref="ContentRootPath"/>.</summary>
    public string ResolvePath(string assetPath)
    {
        return AssetPathResolver.ResolvePath(ContentRootPath, assetPath);
    }

    /// <summary>
    /// Removes a previously loaded asset from the cache and disposes it.
    /// The resolved <paramref name="assetPath"/> is looked up across all asset caches
    /// (images, textures, fonts, sound buffers, and shaders).
    /// Does nothing if the asset is not currently cached.
    /// </summary>
    /// <remarks>
    /// After unloading, any object that still holds a reference to the asset (e.g. a
    /// <see cref="SFML.Graphics.Sprite"/> pointing at an unloaded <see cref="SFML.Graphics.Texture"/>)
    /// will be invalid.  Ensure all consumers have released the asset before calling this method.
    /// </remarks>
    public void Unload(string assetPath)
    {
        string resolvedPath = AssetPathResolver.ResolvePath(ContentRootPath, assetPath);

        if (_images.Remove(resolvedPath, out Image? image))
        {
            image.Dispose();

            return;
        }

        // Textures are keyed as "{resolvedPath}|{smooth}". Remove all variants.
        bool textureRemoved = false;
        string[] textureKeysToRemove = _textures.Keys
            .Where(k => k.StartsWith(resolvedPath + "|", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (string key in textureKeysToRemove)
        {
            if (_textures.Remove(key, out Texture? texture))
            {
                texture.Dispose();
                textureRemoved = true;
            }
        }

        if (textureRemoved)
        {
            return;
        }

        if (_fonts.Remove(resolvedPath, out Font? font))
        {
            font.Dispose();

            return;
        }

        if (_soundBuffers.Remove(resolvedPath, out SoundBuffer? buffer))
        {
            buffer.Dispose();

            return;
        }

        // Shader cache uses either a single resolved path or a "vert|frag" composite key.
        // Check the single-path form first, then scan composite keys that contain it.
        if (_shaders.Remove(resolvedPath, out Shader? shader))
        {
            shader.Dispose();
            _shaderFailures.Remove(resolvedPath);

            return;
        }

        string[] compositeKeys = [.. _shaders.Keys.Where(k => k.Contains(resolvedPath))];
        foreach (string key in compositeKeys)
        {
            if (_shaders.Remove(key, out Shader? compositeShader))
            {
                compositeShader.Dispose();
            }

            _shaderFailures.Remove(key);
        }
    }

    /// <summary>
    /// Removes all cached assets and disposes them.
    /// Equivalent to calling <see cref="Dispose"/> without permanently closing the store.
    /// </summary>
    public void UnloadAll()
    {
        foreach (Image img in _images.Values)
        {
            img.Dispose();
        }

        foreach (Texture tex in _textures.Values)
        {
            tex.Dispose();
        }

        foreach (Font fnt in _fonts.Values)
        {
            fnt.Dispose();
        }

        foreach (SoundBuffer soundBuffer in _soundBuffers.Values)
        {
            soundBuffer.Dispose();
        }

        foreach (Shader shader in _shaders.Values)
        {
            shader.Dispose();
        }

        _images.Clear();
        _textures.Clear();
        _fonts.Clear();
        _soundBuffers.Clear();
        _shaders.Clear();
        _shaderFailures.Clear();
    }

    /// <summary>
    /// Loads an image from the specified asset path, returning a cached instance if available.
    /// PPM P3 and P6 files are decoded internally; all other formats are delegated to SFML.
    /// </summary>
    public Image LoadImage(string assetPath)
    {
        string resolvedPath = ResolvePath(assetPath);
        if (_images.TryGetValue(resolvedPath, out Image? existingImage))
        {
            return existingImage;
        }

        if (!File.Exists(resolvedPath))
        {
            throw new AssetNotFoundException(assetPath);
        }

        Image? img = TryLoadPpm(resolvedPath);
        Image image = img ?? new Image(resolvedPath);

        _images[resolvedPath] = image;

        return image;
    }

    /// <summary>
    /// Loads a texture from <paramref name="assetPath"/>, caching by resolved path and smooth setting.
    /// PPM P3 and P6 files are decoded internally; all other formats are delegated to SFML.
    /// </summary>
    public Texture LoadTexture(string assetPath, bool smooth = false)
    {
        string resolvedPath = ResolvePath(assetPath);
        string cacheKey = $"{resolvedPath}|{smooth}";
        if (_textures.TryGetValue(cacheKey, out Texture? existingTexture))
        {
            return existingTexture;
        }

        if (!File.Exists(resolvedPath))
        {
            EngineLogger.Log($"Asset not found: {resolvedPath}, using fallback texture.");

            return GetFallbackTexture();
        }

        Image? image = TryLoadPpm(resolvedPath);
        Texture texture = image is not null
            ? new Texture(image, smooth)
            : new Texture(resolvedPath);
        image?.Dispose();

        texture.Smooth = smooth;
        _textures[cacheKey] = texture;

        return texture;
    }

    /// <summary>
    /// Loads a font from <paramref name="assetPath"/>,
    /// with fallback to system font and caching by resolved path.
    /// </summary>
    public Font LoadFont(string? assetPath = null)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            assetPath = "system";
        }

        string resolvedPath = ResolvePath(assetPath);
        string key = resolvedPath;

        if (_fonts.TryGetValue(key, out Font? existingFont))
        {
            return existingFont;
        }

        if (!File.Exists(resolvedPath))
        {
            resolvedPath = ResolvePath(SystemFont.FindSystemFont());
        }

        Font font = new(resolvedPath);
        _fonts[key] = font;

        return font;
    }

    /// <summary>Loads a sound buffer from <paramref name="assetPath"/>, caching by resolved path.</summary>
    public SoundBuffer LoadSoundBuffer(string assetPath)
    {
        string resolvedPath = ResolvePath(assetPath);
        if (_soundBuffers.TryGetValue(resolvedPath, out SoundBuffer? existingBuffer))
        {
            return existingBuffer;
        }

        if (!File.Exists(resolvedPath))
        {
            throw new AssetNotFoundException(assetPath);
        }

        SoundBuffer buffer = new(resolvedPath);
        _soundBuffers[resolvedPath] = buffer;

        return buffer;
    }

    /// <summary>
    /// Loads a fragment-only GLSL shader from <paramref name="fragAssetPath"/>, caching by resolved path.
    /// </summary>
    /// <example>
    /// <code>
    /// Shader shader = host.Assets.LoadShader("shaders/grayscale.frag");
    /// shader.SetUniform("u_texture", Shader.CurrentTexture);
    /// window.Draw(sprite, new RenderStates(shader));
    /// </code>
    /// </example>
    public Shader LoadShader(string fragAssetPath)
    {
        string resolvedFrag = ResolvePath(fragAssetPath);
        if (_shaders.TryGetValue(resolvedFrag, out Shader? existing))
        {
            return existing;
        }

        if (!File.Exists(resolvedFrag))
        {
            throw new AssetNotFoundException(fragAssetPath);
        }

        using Stream fragStream = File.OpenRead(resolvedFrag);

        // null for the vertex and geometry stages tells SFML to use its built-in pass-through.
        Shader shader = new(null!, null!, fragStream);
        _shaders[resolvedFrag] = shader;

        return shader;
    }

    /// <summary>
    /// Loads a vertex + fragment GLSL shader pair, caching by the combined resolved paths.
    /// </summary>
    /// <example>
    /// <code>
    /// Shader shader = host.Assets.LoadShader("shaders/wave.vert", "shaders/wave.frag");
    /// shader.SetUniform("u_time", (float)gameTime.Total.TotalSeconds);
    /// window.Draw(sprite, new RenderStates(shader));
    /// </code>
    /// </example>
    public Shader LoadShader(string vertAssetPath, string fragAssetPath)
    {
        string resolvedVert = ResolvePath(vertAssetPath);
        string resolvedFrag = ResolvePath(fragAssetPath);
        string key = $"{resolvedVert}|{resolvedFrag}";

        if (_shaders.TryGetValue(key, out Shader? existing))
        {
            return existing;
        }

        if (!File.Exists(resolvedVert))
        {
            throw new AssetNotFoundException(vertAssetPath);
        }

        if (!File.Exists(resolvedFrag))
        {
            throw new AssetNotFoundException(fragAssetPath);
        }

        using Stream vertStream = File.OpenRead(resolvedVert);
        using Stream fragStream = File.OpenRead(resolvedFrag);

        // null for the geometry stage tells SFML to omit that pipeline stage.
        Shader shader = new(vertStream, null!, fragStream);
        _shaders[key] = shader;

        return shader;
    }

    /// <summary>
    /// Attempts to load a fragment-only GLSL shader from <paramref name="fragAssetPath"/>.
    /// Returns <see langword="null"/> and sets <paramref name="failureReason"/> when the
    /// shader cannot be compiled, such as when the source requires an unsupported GLSL version.
    /// Successful loads and failures are both cached so reloading is never re-attempted.
    /// </summary>
    public Shader? TryLoadShader(string fragAssetPath, out string? failureReason)
    {
        string resolvedFrag = ResolvePath(fragAssetPath);

        if (!File.Exists(resolvedFrag))
        {
            throw new AssetNotFoundException(fragAssetPath);
        }

        return TryLoadShaderCore(
            key: resolvedFrag,
            loadShader: () =>
            {
                using Stream fragStream = File.OpenRead(resolvedFrag);

                return new Shader(null!, null!, fragStream);
            },
            buildFailureReason: ex =>
            {
                int? required = GlslVersionParser.Parse(File.ReadAllText(resolvedFrag));

                return required is not null
                    ? $"Shader requires GLSL {required}, which is not supported on this GPU."
                    : ex.Message;
            },
            out failureReason);
    }

    /// <summary>
    /// Attempts to load a vertex + fragment GLSL shader pair.
    /// Returns <see langword="null"/> and sets <paramref name="failureReason"/> when either
    /// shader stage cannot be compiled.
    /// Successful loads and failures are both cached so reloading is never re-attempted.
    /// </summary>
    public Shader? TryLoadShader(string vertAssetPath, string fragAssetPath, out string? failureReason)
    {
        string resolvedVert = ResolvePath(vertAssetPath);
        string resolvedFrag = ResolvePath(fragAssetPath);
        string key = $"{resolvedVert}|{resolvedFrag}";

        if (!File.Exists(resolvedVert))
        {
            throw new AssetNotFoundException(vertAssetPath);
        }

        if (!File.Exists(resolvedFrag))
        {
            throw new AssetNotFoundException(fragAssetPath);
        }

        return TryLoadShaderCore(
            key,
            loadShader: () =>
            {
                using Stream vertStream = File.OpenRead(resolvedVert);
                using Stream fragStream = File.OpenRead(resolvedFrag);

                return new Shader(vertStream, null!, fragStream);
            },
            buildFailureReason: ex =>
            {
                // Check both stages; report whichever has the higher (incompatible) version.
                int? vertVersion = GlslVersionParser.Parse(File.ReadAllText(resolvedVert));
                int? fragVersion = GlslVersionParser.Parse(File.ReadAllText(resolvedFrag));
                int? required = vertVersion > fragVersion ? vertVersion : fragVersion;

                return required is not null
                    ? $"Shader requires GLSL {required}, which is not supported on this GPU."
                    : ex.Message;
            },
            out failureReason);
    }

    /// <summary>
    /// Shared implementation for both <c>TryLoadShader</c> overloads.
    /// Checks the success and failure caches before attempting to compile.
    /// </summary>
    private Shader? TryLoadShaderCore(
        string key,
        Func<Shader> loadShader,
        Func<Exception, string> buildFailureReason,
        out string? failureReason)
    {
        if (_shaders.TryGetValue(key, out Shader? existing))
        {
            failureReason = null;

            return existing;
        }

        if (_shaderFailures.TryGetValue(key, out string? cached))
        {
            failureReason = cached;

            return null;
        }

        try
        {
            Shader shader = loadShader();
            _shaders[key] = shader;
            failureReason = null;

            return shader;
        }
        catch (Exception ex)
        {
            failureReason = buildFailureReason(ex);
            _shaderFailures[key] = failureReason;

            return null;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _fallbackTexture?.Dispose();

        foreach (Image image in _images.Values)
        {
            image.Dispose();
        }

        foreach (Texture texture in _textures.Values)
        {
            texture.Dispose();
        }

        foreach (Font font in _fonts.Values)
        {
            font.Dispose();
        }

        foreach (SoundBuffer buffer in _soundBuffers.Values)
        {
            buffer.Dispose();
        }

        foreach (Shader shader in _shaders.Values)
        {
            shader.Dispose();
        }

        _images.Clear();
        _textures.Clear();
        _fonts.Clear();
        _soundBuffers.Clear();
        _shaders.Clear();
    }

    /// <summary>Returns a small 16×16 magenta-and-black checkerboard texture for missing assets.</summary>
    private Texture GetFallbackTexture()
    {
        if (_fallbackTexture is not null)
        {
            return _fallbackTexture;
        }

        // 16×16 checkerboard: 8×8 blocks of magenta (#FF00FF) and black.
        const uint Size = 16;
        const uint Half = Size / 2;
        using Image image = new(new Vector2u(Size, Size), Color.Black);

        for (uint y = 0; y < Size; y++)
        {
            for (uint x = 0; x < Size; x++)
            {
                bool isMagenta = (x < Half) ^ (y < Half);
                if (isMagenta)
                {
                    image.SetPixel(new Vector2u(x, y), new Color(255, 0, 255));
                }
            }
        }

        _fallbackTexture = new Texture(image);

        return _fallbackTexture;
    }

    // ── PPM loader ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a decoded <see cref="Image"/> for PPM P3 or P6 files, or
    /// <see langword="null"/> for any other format so the caller can fall back to SFML.
    /// </summary>
    private static Image? TryLoadPpm(string path)
    {
        try
        {
            using FileStream fs = File.OpenRead(path);
            using BinaryReader reader = new(fs);

            // Read the magic number (up to 2 chars + newline/space)
            string magic = ReadPpmToken(fs);

            return magic switch
            {
                "P3" => LoadP3(fs),
                "P6" => LoadP6(reader, fs),
                _ => null
            };
        }
        catch
        {
            // Any parse failure falls back to the SFML loader.
            return null;
        }
    }

    /// <summary>Reads the next whitespace-delimited token from <paramref name="stream"/>.</summary>
    private static string ReadPpmToken(Stream stream)
    {
        SkipWhitespaceAndComments(stream);
        StringBuilder sb = new();
        int b;
        while ((b = stream.ReadByte()) != -1 && !char.IsWhiteSpace((char)b))
        {
            sb.Append((char)b);
        }

        return sb.ToString();
    }

    private static void SkipWhitespaceAndComments(Stream stream)
    {
        int b;
        while ((b = stream.ReadByte()) != -1)
        {
            char c = (char)b;
            if (c == '#')
            {
                // Skip the rest of the comment line.
                while ((b = stream.ReadByte()) != -1 && (char)b != '\n')
                { }
            }
            else if (!char.IsWhiteSpace(c))
            {
                stream.Position--;

                return;
            }
        }
    }

    private static int ReadPpmInt(Stream stream)
    {
        string token = ReadPpmToken(stream);

        return int.Parse(token);
    }

    /// <summary>Decodes a PPM P3 (ASCII) image.</summary>
    private static Image LoadP3(Stream stream)
    {
        int width = ReadPpmInt(stream);
        int height = ReadPpmInt(stream);
        int maxColor = ReadPpmInt(stream);

        byte[] rgba = new byte[width * height * 4];

        for (int i = 0; i < width * height; i++)
        {
            byte r = NormalizeChannel(ReadPpmInt(stream), maxColor);
            byte g = NormalizeChannel(ReadPpmInt(stream), maxColor);
            byte b = NormalizeChannel(ReadPpmInt(stream), maxColor);

            int offset = i * 4;
            rgba[offset] = r;
            rgba[offset + 1] = g;
            rgba[offset + 2] = b;
            rgba[offset + 3] = 255;
        }

        return new Image(new Vector2u((uint)width, (uint)height), rgba);
    }

    /// <summary>Decodes a PPM P6 (binary) image.</summary>
    private static Image LoadP6(BinaryReader binaryReader, Stream stream)
    {
        int width = ReadPpmInt(stream);
        int height = ReadPpmInt(stream);
        int maxColor = ReadPpmInt(stream);

        // After the maxColor value there is exactly one whitespace character before binary data.
        stream.ReadByte();

        byte[] rgba = new byte[width * height * 4];
        bool highDepth = maxColor > 255;

        for (int i = 0; i < width * height; i++)
        {
            byte r, g, b;
            if (highDepth)
            {
                // 16-bit channels stored big-endian.
                r = NormalizeChannel((binaryReader.ReadByte() << 8) | binaryReader.ReadByte(), maxColor);
                g = NormalizeChannel((binaryReader.ReadByte() << 8) | binaryReader.ReadByte(), maxColor);
                b = NormalizeChannel((binaryReader.ReadByte() << 8) | binaryReader.ReadByte(), maxColor);
            }
            else
            {
                r = NormalizeChannel(binaryReader.ReadByte(), maxColor);
                g = NormalizeChannel(binaryReader.ReadByte(), maxColor);
                b = NormalizeChannel(binaryReader.ReadByte(), maxColor);
            }

            int offset = i * 4;
            rgba[offset] = r;
            rgba[offset + 1] = g;
            rgba[offset + 2] = b;
            rgba[offset + 3] = 255;
        }

        return new Image(new Vector2u((uint)width, (uint)height), rgba);
    }

    private static byte NormalizeChannel(int value, int maxColor)
    {
        return maxColor == 255
            ? (byte)value
            : (byte)((double)value / maxColor * 255);
    }

}
