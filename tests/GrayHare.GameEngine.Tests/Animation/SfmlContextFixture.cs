using GrayHare.GameEngine.Animation;
using SFML.Graphics;

namespace GrayHare.GameEngine.Tests.Animation;

/// <summary>
/// Class-level fixture that establishes an off-screen OpenGL context required
/// by SFML texture creation, without opening a visible window.
/// </summary>
public sealed class SfmlContextFixture : IDisposable
{
    // RenderTexture creates an off-screen OpenGL context without requiring a visible window.
    private readonly RenderTexture _context = new RenderTexture(new SFML.System.Vector2u(1, 1));

    /// <summary>
    /// Creates a minimal <see cref="AnimationClip"/> backed by a solid-colour 16×16 image strip.
    /// </summary>
    /// <param name="frameCount">Number of frames in the clip.</param>
    /// <param name="frameDuration">Duration per frame; defaults to 100 ms.</param>
    public AnimationClip CreateClip(int frameCount = 3, TimeSpan? frameDuration = null)
    {
        TimeSpan duration = frameDuration ?? TimeSpan.FromMilliseconds(100);
        using Image image = new Image(new SFML.System.Vector2u((uint)(16 * frameCount), 16), Color.Red);

        return AnimationClip.CreateFromImage("test-clip", image, 16, 16, (uint)frameCount, duration);
    }

    public void Dispose() => _context.Dispose();
}
