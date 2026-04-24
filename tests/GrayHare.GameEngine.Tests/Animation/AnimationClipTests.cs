using GrayHare.GameEngine.Animation;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.Tests.Animation;

public sealed class AnimationClipTests : IClassFixture<SfmlContextFixture>
{
    private static readonly TimeSpan _frameDuration = TimeSpan.FromMilliseconds(100);

    /// <summary>Creates a blank SFML <see cref="Image"/> of the given dimensions.</summary>
    private static Image CreateImage(uint width = 64, uint height = 32) =>
        new Image(new Vector2u(width, height));

    /// <summary>
    /// Creates <paramref name="count"/> <see cref="Texture"/> objects from a blank image.
    /// Callers are responsible for disposing the returned textures.
    /// </summary>
    private static List<Texture> CreateTextures(int count, uint width = 32, uint height = 32)
    {
        using Image image = new(new Vector2u(width, height));
        List<Texture> textures = new(count);

        for (int i = 0; i < count; i++)
        {
            textures.Add(new Texture(image));
        }

        return textures;
    }

    // ── CreateFromImage – validation ──────────────────────────────────────────

    [Fact]
    public void CreateFromImage_WithNullImage_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            AnimationClip.CreateFromImage("clip", null!, 32, 32, 1, _frameDuration));
    }

    [Fact]
    public void CreateFromImage_WithZeroFrameWidth_ThrowsArgumentOutOfRangeException()
    {
        using Image image = CreateImage();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AnimationClip.CreateFromImage("clip", image, 0, 32, 1, _frameDuration));
    }

    [Fact]
    public void CreateFromImage_WithZeroFrameHeight_ThrowsArgumentOutOfRangeException()
    {
        using Image image = CreateImage();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AnimationClip.CreateFromImage("clip", image, 32, 0, 1, _frameDuration));
    }

    [Fact]
    public void CreateFromImage_WithZeroFrameCount_ThrowsArgumentOutOfRangeException()
    {
        using Image image = CreateImage();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AnimationClip.CreateFromImage("clip", image, 32, 32, 0, _frameDuration));
    }

    [Fact]
    public void CreateFromImage_WithZeroFrameDuration_ThrowsArgumentOutOfRangeException()
    {
        using Image image = CreateImage();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AnimationClip.CreateFromImage("clip", image, 32, 32, 1, TimeSpan.Zero));
    }

    [Fact]
    public void CreateFromImage_WithNegativeFrameDuration_ThrowsArgumentOutOfRangeException()
    {
        using Image image = CreateImage();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AnimationClip.CreateFromImage("clip", image, 32, 32, 1, TimeSpan.FromMilliseconds(-1)));
    }

    // ── CreateFromImage – behavior ────────────────────────────────────────────

    [Fact]
    public void CreateFromImage_WithValidArguments_SetsNameCorrectly()
    {
        using Image image = CreateImage(64, 32);
        using AnimationClip clip = AnimationClip.CreateFromImage("walk", image, 32, 32, 2, _frameDuration);

        Assert.Equal("walk", clip.Name);
    }

    [Fact]
    public void CreateFromImage_WithFrameCount_CreatesCorrectNumberOfFrames()
    {
        using Image image = CreateImage(96, 32);
        using AnimationClip clip = AnimationClip.CreateFromImage("clip", image, 32, 32, 3, _frameDuration);

        Assert.Equal(3, clip.Frames.Count);
    }

    [Fact]
    public void CreateFromImage_AllFrames_HaveCorrectDuration()
    {
        using Image image = CreateImage(64, 32);
        using AnimationClip clip = AnimationClip.CreateFromImage("clip", image, 32, 32, 2, _frameDuration);

        Assert.All(clip.Frames, frame => Assert.Equal(_frameDuration, frame.Duration));
    }

    [Fact]
    public void CreateFromImage_EachFrameTexture_HasCorrectDimensions()
    {
        const uint frameWidth = 32U;
        const uint frameHeight = 32U;
        const uint frameCount = 3U;

        using Image image = CreateImage(frameWidth * frameCount, frameHeight);
        using AnimationClip clip = AnimationClip.CreateFromImage("clip", image, frameWidth, frameHeight, frameCount, _frameDuration);

        Assert.All(clip.Frames, frame =>
            Assert.Equal(new Vector2u(frameWidth, frameHeight), frame.Texture.Size));
    }

    // ── CreateFromTextures – validation ───────────────────────────────────────

    [Fact]
    public void CreateFromTextures_WithNullTextures_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            AnimationClip.CreateFromTextures("clip", null!, _frameDuration));
    }

    [Fact]
    public void CreateFromTextures_WithEmptyTextures_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            AnimationClip.CreateFromTextures("clip", [], _frameDuration));
    }

    [Fact]
    public void CreateFromTextures_WithZeroFrameDuration_ThrowsArgumentOutOfRangeException()
    {
        // duration is checked before the empty-list guard, so [] is safe here
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AnimationClip.CreateFromTextures("clip", [], TimeSpan.Zero));
    }

    [Fact]
    public void CreateFromTextures_WithNegativeFrameDuration_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AnimationClip.CreateFromTextures("clip", [], TimeSpan.FromMilliseconds(-1)));
    }

    // ── CreateFromTextures – behavior ─────────────────────────────────────────

    [Fact]
    public void CreateFromTextures_WithValidArguments_SetsNameCorrectly()
    {
        List<Texture> textures = CreateTextures(2);
        using AnimationClip clip = AnimationClip.CreateFromTextures("run", textures, _frameDuration);
        textures.ForEach(t => t.Dispose());

        Assert.Equal("run", clip.Name);
    }

    [Fact]
    public void CreateFromTextures_WithMultipleTextures_CreatesMatchingFrameCount()
    {
        List<Texture> textures = CreateTextures(3);
        using AnimationClip clip = AnimationClip.CreateFromTextures("clip", textures, _frameDuration);
        textures.ForEach(t => t.Dispose());

        Assert.Equal(3, clip.Frames.Count);
    }

    [Fact]
    public void CreateFromTextures_AllFrames_HaveCorrectDuration()
    {
        List<Texture> textures = CreateTextures(2);
        using AnimationClip clip = AnimationClip.CreateFromTextures("clip", textures, _frameDuration);
        textures.ForEach(t => t.Dispose());

        Assert.All(clip.Frames, frame => Assert.Equal(_frameDuration, frame.Duration));
    }

    [Fact]
    public void CreateFromTextures_CreatesOwnCopies_FrameTexturesAreDifferentInstances()
    {
        List<Texture> textures = CreateTextures(2);
        using AnimationClip clip = AnimationClip.CreateFromTextures("clip", textures, _frameDuration);

        for (int i = 0; i < textures.Count; i++)
        {
            Assert.NotSame(textures[i], clip.Frames[i].Texture);
        }

        textures.ForEach(t => t.Dispose());
    }

    // ── Dispose ───────────────────────────────────────────────────────────────

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        using Image image = CreateImage();
        AnimationClip clip = AnimationClip.CreateFromImage("clip", image, 32, 32, 1, _frameDuration);

        clip.Dispose();

        Assert.Null(Record.Exception(() => clip.Dispose()));
    }

    // ── CreateFromImage – startX / startY overload ────────────────────────────

    [Fact]
    public void CreateFromImage_WithStartOffset_ProducesCorrectFrameCount()
    {
        using Image image = CreateImage(width: 96, height: 64);

        using AnimationClip clip = AnimationClip.CreateFromImage(
            "row1", image, frameWidth: 32, frameHeight: 32, frameCount: 3,
            frameDuration: _frameDuration, startX: 0, startY: 32);

        Assert.Equal(3, clip.Frames.Count);
    }

    [Fact]
    public void CreateFromImage_StartXYZero_MatchesBaseOverload()
    {
        using Image image = CreateImage(width: 96, height: 32);

        using AnimationClip baseClip = AnimationClip.CreateFromImage(
            "base", image, 32, 32, 3, _frameDuration);

        using AnimationClip offsetClip = AnimationClip.CreateFromImage(
            "offset", image, 32, 32, 3, _frameDuration, startX: 0, startY: 0);

        Assert.Equal(baseClip.Frames.Count, offsetClip.Frames.Count);
        Assert.Equal("base", baseClip.Name);
        Assert.Equal("offset", offsetClip.Name);
    }
}
