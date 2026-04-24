using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.Animation;

/// <summary>
/// A named sequence of <see cref="AnimationFrame"/> records that defines a single animation.
/// Clips are created via the static factory methods
/// <see cref="CreateFromTextures"/> or one of the <c>CreateFromImage</c> overloads.
/// </summary>
public sealed class AnimationClip : IDisposable
{
    private bool _disposed;

    /// <summary>Creates a new <see cref="AnimationClip"/>.</summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is null or whitespace, or when
    /// <paramref name="frames"/> is empty.
    /// </exception>
    private AnimationClip(string name, IReadOnlyList<AnimationFrame> frames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (frames.Count == 0)
        {
            throw new ArgumentException("Animation clips must include at least one frame.", nameof(frames));
        }

        Name = name;
        Frames = frames;
    }

    /// <summary>Unique name identifying this clip.</summary>
    public string Name { get; }

    /// <summary>Ordered list of frames in this clip.</summary>
    public IReadOnlyList<AnimationFrame> Frames { get; }

    /// <summary>
    /// Builds a clip from a single horizontal <see cref="Image"/> strip.
    /// Frames are evenly spaced starting at column 0 on row 0.
    /// </summary>
    /// <param name="name">Clip name.</param>
    /// <param name="image">Image strip.</param>
    /// <param name="frameWidth">Width of each frame in pixels.</param>
    /// <param name="frameHeight">Height of each frame in pixels.</param>
    /// <param name="frameCount">Number of frames.</param>
    /// <param name="frameDuration">Display duration per frame.</param>
    public static AnimationClip CreateFromImage(
        string name,
        Image image,
        uint frameWidth,
        uint frameHeight,
        uint frameCount,
        TimeSpan frameDuration)
    {
        return CreateFromImage(name, image, frameWidth, frameHeight, frameCount, frameDuration, startX: 0, startY: 0);
    }

    /// <summary>
    /// Builds a clip from a region of an <see cref="Image"/>, starting at
    /// (<paramref name="startX"/>, <paramref name="startY"/>) and reading
    /// <paramref name="frameCount"/> frames horizontally.
    /// Use this overload to read a single row from a multi-row sprite sheet
    /// (e.g. <c>startY = rowIndex * frameHeight</c>).
    /// </summary>
    /// <param name="name">Clip name.</param>
    /// <param name="image">Source image.</param>
    /// <param name="frameWidth">Width of each frame in pixels.</param>
    /// <param name="frameHeight">Height of each frame in pixels.</param>
    /// <param name="frameCount">Number of frames to read.</param>
    /// <param name="frameDuration">Display duration per frame.</param>
    /// <param name="startX">Horizontal pixel offset of the first frame.</param>
    /// <param name="startY">Vertical pixel offset of the first frame.</param>
    public static AnimationClip CreateFromImage(
        string name,
        Image image,
        uint frameWidth,
        uint frameHeight,
        uint frameCount,
        TimeSpan frameDuration,
        uint startX,
        uint startY)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(image);
        ArgumentOutOfRangeException.ThrowIfLessThan(frameWidth, 1U);
        ArgumentOutOfRangeException.ThrowIfLessThan(frameHeight, 1U);
        ArgumentOutOfRangeException.ThrowIfLessThan(frameCount, 1U);

        if (frameDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(frameDuration), "Frame duration must be positive.");
        }

        List<AnimationFrame> frames = new((int)frameCount);
        for (int index = 0; index < frameCount; index++)
        {
            IntRect rect = new(
                new Vector2i((int)(startX + index * frameWidth), (int)startY),
                new Vector2i((int)frameWidth, (int)frameHeight));
            Texture frameTexture = new(image, rect);

            frames.Add(new AnimationFrame(frameDuration, frameTexture));
        }

        return new AnimationClip(name, frames);
    }

    /// <summary>
    /// Builds a clip from a list of <see cref="Texture"/>.
    /// </summary>
    /// <param name="name">Clip name.</param>
    /// <param name="textures">
    /// Ordered list of textures, one per frame.
    /// The clip creates its own copy of each texture.
    /// </param>
    /// <param name="frameDuration">Display duration applied to every frame.</param>
    public static AnimationClip CreateFromTextures(
        string name,
        IReadOnlyList<Texture> textures,
        TimeSpan frameDuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(textures);

        if (frameDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(frameDuration), "Frame duration must be positive.");
        }

        if (textures.Count == 0)
        {
            throw new ArgumentException("At least one texture is required.", nameof(textures));
        }

        List<AnimationFrame> frames = new(textures.Count);
        foreach (Texture texture in textures)
        {
            frames.Add(new AnimationFrame(frameDuration, new Texture(texture)));
        }

        return new AnimationClip(name, frames);
    }

    /// <summary>Releases all frame textures owned by this clip.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (AnimationFrame frame in Frames)
        {
            frame.Texture.Dispose();
        }
    }
}
