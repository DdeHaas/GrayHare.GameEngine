using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Ecs;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GrayHare.GameEngine.Tests.Application;

public sealed class GameHostTests
{
    /// <summary>
    /// Creates an uninitialized <see cref="GameHost"/> without calling its constructor,
    /// bypassing the SFML window dependency while still allowing pure-C# member tests.
    /// </summary>
    private static GameHost CreateFakeHost() =>
        (GameHost)RuntimeHelpers.GetUninitializedObject(typeof(GameHost));

    /// <summary>
    /// Creates an uninitialized <see cref="GameHost"/> and injects a real
    /// <see cref="SceneManager"/> so that <see cref="GameHost.SceneStackDepth"/> works.
    /// </summary>
    private static GameHost CreateFakeHostWithSceneManager(out SceneManager sceneManager)
    {
        var host = CreateFakeHost();
        sceneManager = new SceneManager(new World());
        typeof(GameHost)
            .GetField("_sceneManager", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(host, sceneManager);

        return host;
    }

    // ── Pause / Resume ────────────────────────────────────────────────────────

    [Fact]
    public void Resume_SetsTimeScaleToOne()
    {
        var host = CreateFakeHost();

        host.Resume();

        Assert.Equal(1f, host.TimeScale);
    }

    [Fact]
    public void Resume_SetsIsPausedFalse()
    {
        var host = CreateFakeHost();

        host.Resume();

        Assert.False(host.IsPaused);
    }

    [Fact]
    public void Pause_SetsTimeScaleToZero()
    {
        var host = CreateFakeHost();
        host.Resume();

        host.Pause();

        Assert.Equal(0f, host.TimeScale);
    }

    [Fact]
    public void Pause_SetsIsPausedTrue()
    {
        var host = CreateFakeHost();
        host.Resume();

        host.Pause();

        Assert.True(host.IsPaused);
    }

    [Fact]
    public void Pause_ThenResume_RestoresTimeScale()
    {
        var host = CreateFakeHost();
        host.Resume();
        host.Pause();

        host.Resume();

        Assert.Equal(1f, host.TimeScale);
        Assert.False(host.IsPaused);
    }

    // ── SetTimeScale ──────────────────────────────────────────────────────────

    [Fact]
    public void SetTimeScale_StoresSuppliedValue()
    {
        var host = CreateFakeHost();

        host.SetTimeScale(2f);

        Assert.Equal(2f, host.TimeScale);
    }

    [Fact]
    public void SetTimeScale_ClampsNegativeValueToZero()
    {
        var host = CreateFakeHost();

        host.SetTimeScale(-1f);

        Assert.Equal(0f, host.TimeScale);
    }

    [Theory]
    [InlineData(0f, true)]
    [InlineData(0.5f, false)]
    [InlineData(1f, false)]
    [InlineData(2f, false)]
    public void IsPaused_ReflectsTimeScale(float timeScale, bool expectedPaused)
    {
        var host = CreateFakeHost();

        host.SetTimeScale(timeScale);

        Assert.Equal(expectedPaused, host.IsPaused);
    }

    // ── Exit / ExitRequested ──────────────────────────────────────────────────

    [Fact]
    public void ExitRequested_IsFalse_Initially()
    {
        var host = CreateFakeHost();

        Assert.False(host.ExitRequested);
    }

    [Fact]
    public void Exit_SetsExitRequestedTrue()
    {
        var host = CreateFakeHost();

        host.Exit();

        Assert.True(host.ExitRequested);
    }

    // ── SceneStackDepth ───────────────────────────────────────────────────────

    [Fact]
    public void SceneStackDepth_IsZero_BeforeAnyScene()
    {
        var host = CreateFakeHostWithSceneManager(out _);

        Assert.Equal(0, host.SceneStackDepth);
    }
}
