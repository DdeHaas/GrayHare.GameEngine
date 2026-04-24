using SFML.Graphics;

namespace GrayHare.GameEngine.Animation;

/// <summary>A single frame in an <see cref="AnimationClip"/>.</summary>
/// <param name="Duration">How long this frame is shown.</param>
/// <param name="Texture">Frame texture to display.</param>
public readonly record struct AnimationFrame(TimeSpan Duration, Texture Texture);
