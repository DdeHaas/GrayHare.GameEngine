using GrayHare.GameEngine.Animation;

namespace GrayHare.GameEngine.Tests.Animation;

public sealed class AnimationPlayerTests : IClassFixture<SfmlContextFixture>
{
    private readonly SfmlContextFixture _sfml;

    private static readonly TimeSpan _frameDuration = TimeSpan.FromMilliseconds(100);

    public AnimationPlayerTests(SfmlContextFixture sfml)
    {
        _sfml = sfml;
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithNullClip_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AnimationPlayer(null!, false));
    }

    [Fact]
    public void Constructor_DefaultFrameIndex_IsZero()
    {
        using AnimationClip clip = _sfml.CreateClip();
        using AnimationPlayer player = new(clip, isLooping: false);

        Assert.Equal(0, player.FrameIndex);
    }

    [Fact]
    public void Constructor_WithAutoPlayTrue_IsNotFinished()
    {
        using AnimationClip clip = _sfml.CreateClip();
        using AnimationPlayer player = new(clip, isLooping: false, autoPlay: true);

        Assert.False(player.IsFinished);
    }

    [Fact]
    public void Constructor_WithAutoPlayFalse_IsNotFinished()
    {
        using AnimationClip clip = _sfml.CreateClip();
        using AnimationPlayer player = new(clip, isLooping: false, autoPlay: false);

        Assert.False(player.IsFinished);
    }

    // ── Play ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Play_AfterAnimationFinishes_SetsIsFinishedFalse()
    {
        using AnimationClip clip = _sfml.CreateClip(frameCount: 1, frameDuration: _frameDuration);
        using AnimationPlayer player = new(clip, isLooping: false);

        player.Update(_frameDuration);
        player.Play();

        Assert.False(player.IsFinished);
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_AfterFrameAdvanced_ResetsFrameIndexToZero()
    {
        using AnimationClip clip = _sfml.CreateClip(frameCount: 3, frameDuration: _frameDuration);
        using AnimationPlayer player = new(clip, isLooping: false);

        player.Update(_frameDuration);
        player.Reset();

        Assert.Equal(0, player.FrameIndex);
    }

    [Fact]
    public void Reset_AfterFinishing_SetsIsFinishedFalse()
    {
        using AnimationClip clip = _sfml.CreateClip(frameCount: 1, frameDuration: _frameDuration);
        using AnimationPlayer player = new(clip, isLooping: false);

        player.Update(_frameDuration);
        player.Reset();

        Assert.False(player.IsFinished);
    }

    // ── Update – non-looping ──────────────────────────────────────────────────

    [Fact]
    public void Update_DeltaShorterThanFrameDuration_DoesNotAdvanceFrame()
    {
        using AnimationClip clip = _sfml.CreateClip(frameCount: 3, frameDuration: _frameDuration);
        using AnimationPlayer player = new(clip, isLooping: false);

        player.Update(_frameDuration - TimeSpan.FromMilliseconds(1));

        Assert.Equal(0, player.FrameIndex);
    }

    [Fact]
    public void Update_DeltaEqualToFrameDuration_AdvancesToNextFrame()
    {
        using AnimationClip clip = _sfml.CreateClip(frameCount: 3, frameDuration: _frameDuration);
        using AnimationPlayer player = new(clip, isLooping: false);

        player.Update(_frameDuration);

        Assert.Equal(1, player.FrameIndex);
    }

    [Fact]
    public void Update_DeltaSpansMultipleFrames_AdvancesCorrectly()
    {
        using AnimationClip clip = _sfml.CreateClip(frameCount: 5, frameDuration: _frameDuration);
        using AnimationPlayer player = new(clip, isLooping: false);

        player.Update(_frameDuration * 2);

        Assert.Equal(2, player.FrameIndex);
    }

    [Fact]
    public void Update_AccumulatedDeltasReachFrameDuration_AdvancesFrame()
    {
        using AnimationClip clip = _sfml.CreateClip(frameCount: 3, frameDuration: _frameDuration);
        using AnimationPlayer player = new(clip, isLooping: false);

        player.Update(TimeSpan.FromMilliseconds(60));
        player.Update(TimeSpan.FromMilliseconds(40));

        Assert.Equal(1, player.FrameIndex);
    }

    [Fact]
    public void Update_WhenSingleFrameDurationElapsed_SetsIsFinished()
    {
        using AnimationClip clip = _sfml.CreateClip(frameCount: 1, frameDuration: _frameDuration);
        using AnimationPlayer player = new(clip, isLooping: false);

        player.Update(_frameDuration);

        Assert.True(player.IsFinished);
    }

    [Fact]
    public void Update_WhenAllFramesElapse_SetsIsFinished()
    {
        const int frameCount = 3;
        using AnimationClip clip = _sfml.CreateClip(frameCount: frameCount, frameDuration: _frameDuration);
        using AnimationPlayer player = new(clip, isLooping: false);

        player.Update(_frameDuration * frameCount);

        Assert.True(player.IsFinished);
    }

    [Fact]
    public void Update_WhenFinished_KeepsFrameIndexOnLastFrame()
    {
        const int frameCount = 2;
        using AnimationClip clip = _sfml.CreateClip(frameCount: frameCount, frameDuration: _frameDuration);
        using AnimationPlayer player = new(clip, isLooping: false);

        player.Update(_frameDuration * frameCount);
        int lastFrameIndex = player.FrameIndex;

        player.Update(_frameDuration * 10);

        Assert.Equal(lastFrameIndex, player.FrameIndex);
    }

    // ── Update – looping ──────────────────────────────────────────────────────

    [Fact]
    public void Update_Looping_WhenLastFrameDurationElapsed_WrapsToFirstFrame()
    {
        const int frameCount = 3;
        using AnimationClip clip = _sfml.CreateClip(frameCount: frameCount, frameDuration: _frameDuration);
        using AnimationPlayer player = new(clip, isLooping: true);

        player.Update(_frameDuration * frameCount);

        Assert.Equal(0, player.FrameIndex);
    }

    [Fact]
    public void Update_Looping_AfterMultipleLoops_NeverSetsIsFinished()
    {
        const int frameCount = 3;
        using AnimationClip clip = _sfml.CreateClip(frameCount: frameCount, frameDuration: _frameDuration);
        using AnimationPlayer player = new(clip, isLooping: true);

        player.Update(_frameDuration * frameCount * 5);

        Assert.False(player.IsFinished);
    }

    // ── FrameIndex property ───────────────────────────────────────────────────

    [Fact]
    public void FrameIndex_SetNegativeValue_ClampsToZero()
    {
        using AnimationClip clip = _sfml.CreateClip(frameCount: 3);
        using AnimationPlayer player = new(clip, isLooping: false);

        player.FrameIndex = -5;

        Assert.Equal(0, player.FrameIndex);
    }

    [Theory]
    [InlineData(3, 0)]  // 3 % 3 = 0
    [InlineData(4, 1)]  // 4 % 3 = 1
    [InlineData(5, 2)]  // 5 % 3 = 2
    public void FrameIndex_SetValueAtOrBeyondFrameCount_WrapsAround(int setValue, int expected)
    {
        using AnimationClip clip = _sfml.CreateClip(frameCount: 3);
        using AnimationPlayer player = new(clip, isLooping: false);

        player.FrameIndex = setValue;

        Assert.Equal(expected, player.FrameIndex);
    }

    // ── Dispose ───────────────────────────────────────────────────────────────

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        using AnimationClip clip = _sfml.CreateClip();
        AnimationPlayer player = new(clip, isLooping: false);

        player.Dispose();

        Assert.Null(Record.Exception(() => player.Dispose()));
    }

    // ── Transform properties ──────────────────────────────────────────────────

    [Fact]
    public void Position_GetSet_WorksCorrectly()
    {
        using AnimationClip clip = _sfml.CreateClip();
        using AnimationPlayer player = new(clip, isLooping: false);

        player.Position = new SFML.System.Vector2f(100f, 200f);

        Assert.Equal(new SFML.System.Vector2f(100f, 200f), player.Position);
    }

    [Fact]
    public void Scale_GetSet_WorksCorrectly()
    {
        using AnimationClip clip = _sfml.CreateClip();
        using AnimationPlayer player = new(clip, isLooping: false);

        player.Scale = new SFML.System.Vector2f(2f, 3f);

        Assert.Equal(new SFML.System.Vector2f(2f, 3f), player.Scale);
    }

    [Fact]
    public void Rotation_GetSet_WorksCorrectly()
    {
        using AnimationClip clip = _sfml.CreateClip();
        using AnimationPlayer player = new(clip, isLooping: false);

        player.Rotation = 45f;

        Assert.Equal(45f, player.Rotation);
    }

    // ── Pause / Resume ────────────────────────────────────────────────────────

    [Fact]
    public void IsPaused_DefaultIsFalse()
    {
        using AnimationClip clip = _sfml.CreateClip();
        using AnimationPlayer player = new(clip, isLooping: false);

        Assert.False(player.IsPaused);
    }

    [Fact]
    public void Pause_SetsIsPausedTrue()
    {
        using AnimationClip clip = _sfml.CreateClip();
        using AnimationPlayer player = new(clip, isLooping: false);

        player.Pause();

        Assert.True(player.IsPaused);
    }

    [Fact]
    public void Resume_ClearsIsPaused()
    {
        using AnimationClip clip = _sfml.CreateClip();
        using AnimationPlayer player = new(clip, isLooping: false);
        player.Pause();

        player.Resume();

        Assert.False(player.IsPaused);
    }

    [Fact]
    public void Pause_PreventsFrameAdvancement()
    {
        using AnimationClip clip = _sfml.CreateClip(frameCount: 3, frameDuration: _frameDuration);
        using AnimationPlayer player = new(clip, isLooping: false);
        player.Pause();

        player.Update(_frameDuration * 2);

        Assert.Equal(0, player.FrameIndex);
    }

    [Fact]
    public void Resume_AllowsFrameAdvancement()
    {
        using AnimationClip clip = _sfml.CreateClip(frameCount: 3, frameDuration: _frameDuration);
        using AnimationPlayer player = new(clip, isLooping: false);
        player.Pause();
        player.Resume();

        player.Update(_frameDuration * 2);

        Assert.True(player.FrameIndex > 0, "Frames should advance after Resume.");
    }

    [Fact]
    public void Play_ClearsIsPaused()
    {
        using AnimationClip clip = _sfml.CreateClip();
        using AnimationPlayer player = new(clip, isLooping: false);
        player.Pause();

        player.Play();

        Assert.False(player.IsPaused);
    }
}
