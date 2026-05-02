using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.Animation;

/// <summary>
/// Drives playback of an <see cref="AnimationClip"/>, advancing frames by elapsed time
/// and rendering the current frame to a <see cref="RenderWindow"/>.
/// </summary>
public sealed class AnimationPlayer : IDisposable
{
    private readonly Sprite _sprite;
    private readonly AnimationClip _clip;
    private bool _disposed;

    private TimeSpan _frameElapsed;
    private int _frameIndex;

    /// <summary><see langword="true"/> when a non-looping clip has displayed its last frame.</summary>
    public bool IsFinished { get; private set; }

    /// <summary><see langword="true"/> when the player loops the clip after the last frame.</summary>
    public bool IsLooping { get; }

    /// <summary><see langword="true"/> when the player is paused mid-sequence.</summary>
    public bool IsPaused { get; private set; }

    /// <summary>World-space position where the animation is rendered.</summary>
    public Vector2f Position { get => _sprite.Position; set => _sprite.Position = value; }

    /// <summary>Scale factor applied to each frame.</summary>
    public Vector2f Scale { get => _sprite.Scale; set => _sprite.Scale = value; }

    /// <summary>Rotation in degrees applied to each frame.</summary>
    public float Rotation { get => _sprite.Rotation; set => _sprite.Rotation = value; }

    /// <summary>
    /// Zero-based index of the currently displayed frame.
    /// Negative values are clamped to zero; values beyond the last frame wrap around.
    /// </summary>
    public int FrameIndex
    {
        get => _frameIndex;

        set
        {
            if (value < 0)
            {
                value = 0;
            }

            _frameIndex = value % _clip.Frames.Count;
        }
    }

    /// <summary>
    /// Initializes a new instance of the AnimationPlayer class with the specified animation clip and playback options.
    /// </summary>
    /// <param name="clip">The animation clip to be played. Cannot be null.</param>
    /// <param name="isLooping">
    /// <see langword="true"/> to enable looping playback of the animation; otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="autoPlay">
    /// <see langword="true"/> to automatically start playing the animation upon initialization;
    /// otherwise, <see langword="false"/>. The default is <see langword="true"/>.
    /// </param>
    public AnimationPlayer(AnimationClip clip, bool isLooping, bool autoPlay = true)
    {
        ArgumentNullException.ThrowIfNull(clip);

        _clip = clip;
        IsLooping = isLooping;

        _sprite = new Sprite(clip.Frames[0].Texture);

        IntRect textureRect = _sprite.TextureRect;
        _sprite.Origin = new Vector2f(textureRect.Width / 2f, textureRect.Height / 2f);

        _frameIndex = 0;
        IsFinished = false;

        if (autoPlay)
        {
            Play();
        }
    }

    /// <summary>
    /// Starts or resumes playback starting from the frame index.
    /// Does not reset <see cref="FrameIndex"/>; call <see cref="Reset"/> first to restart from frame 0.
    /// </summary>
    /// <example>
    /// <code>
    /// // Resume a finished animation from its current frame index.
    /// if (player.IsFinished)
    ///     player.Play();
    /// </code>
    /// </example>
    public void Play()
    {
        _frameElapsed = TimeSpan.Zero;
        IsFinished = false;
        IsPaused = false;
    }

    /// <summary>
    /// Freezes playback at the current frame. Call <see cref="Resume"/> to continue.
    /// </summary>
    public void Pause()
    {
        IsPaused = true;
    }

    /// <summary>
    /// Resumes playback after a <see cref="Pause"/> call without resetting the frame.
    /// </summary>
    public void Resume()
    {
        IsPaused = false;
    }

    /// <summary>
    /// Resets the animation to frame 0 and clears the finished flag.
    /// </summary>
    /// <example>
    /// <code>
    /// // Restart the clip from the beginning.
    /// player.Reset();
    /// </code>
    /// </example>
    public void Reset()
    {
        _frameElapsed = TimeSpan.Zero;
        _frameIndex = 0;
        IsFinished = false;
    }

    /// <summary>Advances playback by <paramref name="delta"/>, switching frames as needed.</summary>
    /// <param name="delta">Elapsed time since the last update.</param>
    /// <example>
    /// <code>
    /// // Call once per game loop tick.
    /// player.Update(gameTime.Delta);
    /// </code>
    /// </example>
    public void Update(TimeSpan delta)
    {
        if (IsPaused || (IsFinished && !IsLooping))
        {
            return;
        }

        _frameElapsed += delta;

        while (_frameElapsed >= _clip.Frames[_frameIndex].Duration)
        {
            _frameElapsed -= _clip.Frames[_frameIndex].Duration;

            if (_frameIndex == _clip.Frames.Count - 1)
            {
                if (IsLooping)
                {
                    _frameIndex = 0;
                    continue;
                }

                IsFinished = true;
                _frameElapsed = TimeSpan.Zero;

                return;
            }

            _frameIndex++;
        }
    }

    /// <summary>Draws the current frame to <paramref name="window"/>.</summary>
    /// <param name="window">The render window to draw on.</param>
    /// <example>
    /// <code>
    /// // Call once per game loop tick after Update.
    /// player.Render(window);
    /// </code>
    /// </example>
    public void Render(RenderWindow window)
    {
        _sprite.Texture = _clip.Frames[_frameIndex].Texture;
        window.Draw(_sprite);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _sprite.Texture = null;
        _sprite.Dispose();
    }
}
