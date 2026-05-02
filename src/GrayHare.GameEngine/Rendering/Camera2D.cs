using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.Rendering;

/// <summary>
/// A 2D camera that wraps an SFML <see cref="View"/> and provides smooth following,
/// zoom, rotation, and screen-shake effects.
/// </summary>
/// <example>
/// <code>
/// var camera = new Camera2D(window.Size);
/// camera.Follow(playerPosition, lerpSpeed: 5f, deltaTime: dt);
/// camera.Zoom = 1.5f;
/// window.SetView(camera.GetView());
/// </code>
/// </example>
/// <remarks>This type is not thread-safe. Access all members from the main thread only.</remarks>
public sealed class Camera2D
{
    private float _shakeIntensity;
    private float _shakeDuration;
    private float _shakeTimeRemaining;
    private Vector2f _shakeOffset;
    private readonly Random _random = Random.Shared;

    /// <summary>
    /// Initializes a new <see cref="Camera2D"/> whose viewport matches
    /// the supplied window dimensions.
    /// </summary>
    /// <param name="viewportSize">
    /// The size of the render target, typically obtained from
    /// <c>RenderWindow.Size</c>.
    /// </param>
    public Camera2D(Vector2u viewportSize)
    {
        ViewportSize = new Vector2f(viewportSize.X, viewportSize.Y);
        Position = ViewportSize / 2f;
    }

    /// <summary>The world-space center position of the camera.</summary>
    public Vector2f Position { get; set; }

    /// <summary>
    /// Zoom level where <c>1</c> is the default size, values greater than <c>1</c>
    /// zoom in, and values less than <c>1</c> zoom out.
    /// Clamped to a minimum of <c>0.01</c> to avoid division by zero.
    /// </summary>
    public float Zoom { get; set; } = 1f;

    /// <summary>Camera rotation in degrees (clockwise).</summary>
    public float Rotation { get; set; }

    /// <summary>
    /// The viewport dimensions used to compute the visible area.
    /// Set once at construction from the window size.
    /// </summary>
    public Vector2f ViewportSize { get; }

    /// <summary>
    /// Smoothly moves the camera toward <paramref name="target"/> using
    /// linear interpolation scaled by <paramref name="deltaTime"/>.
    /// </summary>
    /// <param name="target">The world-space position to follow.</param>
    /// <param name="lerpSpeed">
    /// Interpolation speed factor. Higher values track faster.
    /// Internally clamped so the effective lerp factor never exceeds <c>1</c>,
    /// preventing overshoot.
    /// </param>
    /// <param name="deltaTime">Frame delta time in seconds.</param>
    /// <example>
    /// <code>
    /// camera.Follow(player.Position, lerpSpeed: 4f, deltaTime: gameTime.DeltaTotalSeconds);
    /// </code>
    /// </example>
    public void Follow(Vector2f target, float lerpSpeed, float deltaTime)
    {
        // Clamp the effective factor to [0, 1] to prevent overshoot.
        float factor = Math.Clamp(lerpSpeed * deltaTime, 0f, 1f);
        Position += (target - Position) * factor;
    }

    /// <summary>
    /// Initiates a screen-shake effect that decays linearly over
    /// <paramref name="duration"/> seconds.
    /// </summary>
    /// <param name="intensity">Maximum pixel offset of the shake.</param>
    /// <param name="duration">How long the shake lasts, in seconds.</param>
    /// <example>
    /// <code>
    /// camera.Shake(intensity: 6f, duration: 0.3f);
    /// </code>
    /// </example>
    public void Shake(float intensity, float duration)
    {
        _shakeIntensity = MathF.Abs(intensity);
        _shakeDuration = MathF.Abs(duration);
        _shakeTimeRemaining = _shakeDuration;
    }

    /// <summary>
    /// Advances the screen-shake timer and recalculates the shake offset.
    /// Call once per frame before <see cref="GetView"/>.
    /// </summary>
    /// <param name="deltaTime">Frame delta time in seconds (use raw/unscaled time).</param>
    public void UpdateShake(float deltaTime)
    {
        if (_shakeTimeRemaining <= 0f)
        {
            _shakeOffset = new Vector2f(0f, 0f);

            return;
        }

        _shakeTimeRemaining -= deltaTime;

        float progress = Math.Clamp(_shakeTimeRemaining / _shakeDuration, 0f, 1f);
        float currentIntensity = _shakeIntensity * progress;

        float offsetX = (_random.NextSingle() * 2f - 1f) * currentIntensity;
        float offsetY = (_random.NextSingle() * 2f - 1f) * currentIntensity;
        _shakeOffset = new Vector2f(offsetX, offsetY);
    }

    /// <summary>
    /// Produces an SFML <see cref="View"/> reflecting the current camera state
    /// (position, zoom, rotation, and any active shake offset).
    /// </summary>
    /// <returns>A new <see cref="View"/> ready to be applied to a render target.</returns>
    /// <example>
    /// <code>
    /// window.SetView(camera.GetView());
    /// </code>
    /// </example>
    public View GetView()
    {
        float effectiveZoom = MathF.Max(Zoom, 0.01f);
        Vector2f center = Position + _shakeOffset;
        Vector2f size = ViewportSize / effectiveZoom;

        return new View(center, size)
        {
            Rotation = Angle.FromDegrees(Rotation)
        };
    }

    /// <summary>
    /// Converts a screen-space position to a world-space position, accounting for
    /// camera position, zoom, and rotation. Includes any active shake offset so the
    /// result matches what is currently visible on screen.
    /// </summary>
    /// <param name="screenPos">The screen position in pixels (e.g. mouse cursor position).</param>
    /// <returns>The corresponding world-space position.</returns>
    public Vector2f ScreenToWorld(Vector2i screenPos)
    {
        float effectiveZoom = MathF.Max(Zoom, 0.01f);
        Vector2f center = Position + _shakeOffset;
        float dx = (screenPos.X - ViewportSize.X / 2f) / effectiveZoom;
        float dy = (screenPos.Y - ViewportSize.Y / 2f) / effectiveZoom;

        float rad = Rotation * MathF.PI / 180f;
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);

        return new Vector2f(
            center.X + dx * cos - dy * sin,
            center.Y + dx * sin + dy * cos);
    }

    /// <summary>
    /// Converts a world-space position to a screen-space pixel position, accounting for
    /// camera position, zoom, and rotation. Includes any active shake offset so the
    /// result matches what is currently visible on screen.
    /// </summary>
    /// <param name="worldPos">The world-space position to project onto the screen.</param>
    /// <returns>The corresponding screen position in pixels.</returns>
    public Vector2i WorldToScreen(Vector2f worldPos)
    {
        float effectiveZoom = MathF.Max(Zoom, 0.01f);
        Vector2f center = Position + _shakeOffset;
        float dx = worldPos.X - center.X;
        float dy = worldPos.Y - center.Y;

        float rad = -Rotation * MathF.PI / 180f;
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);

        return new Vector2i(
            (int)(ViewportSize.X / 2f + (dx * cos - dy * sin) * effectiveZoom),
            (int)(ViewportSize.Y / 2f + (dx * sin + dy * cos) * effectiveZoom));
    }

    /// <summary>
    /// Resets the camera to its default state: centered on the viewport,
    /// no zoom, no rotation, and no active shake.
    /// </summary>
    public void Reset()
    {
        Position = ViewportSize / 2f;
        Zoom = 1f;
        Rotation = 0f;
        _shakeIntensity = 0f;
        _shakeDuration = 0f;
        _shakeTimeRemaining = 0f;
        _shakeOffset = new Vector2f(0f, 0f);
    }
}
