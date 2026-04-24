using GrayHare.GameEngine.Assets;
using GrayHare.GameEngine.Audio;
using GrayHare.GameEngine.Diagnostics;

namespace GrayHare.GameEngine.Tests.Audio;

public sealed class AudioPlayerTests
{
    private static AudioPlayer CreatePlayer()
    {
        using var store = new AssetStore(Path.GetTempPath());

        return new AudioPlayer(store);
    }

    [Fact]
    public void MasterVolume_DefaultIs100()
    {
        using var player = CreatePlayer();

        Assert.Equal(100f, player.MasterVolume);
    }

    [Fact]
    public void SfxVolume_DefaultIs100()
    {
        using var player = CreatePlayer();

        Assert.Equal(100f, player.SfxVolume);
    }

    [Fact]
    public void MusicVolume_DefaultIs100()
    {
        using var player = CreatePlayer();

        Assert.Equal(100f, player.MusicVolume);
    }

    [Fact]
    public void IsMuted_DefaultIsFalse()
    {
        using var player = CreatePlayer();

        Assert.False(player.IsMuted);
    }

    [Fact]
    public void SetMasterVolume_ClampsTo0_100()
    {
        using var player = CreatePlayer();

        player.SetMasterVolume(-50f);
        Assert.Equal(0f, player.MasterVolume);

        player.SetMasterVolume(200f);
        Assert.Equal(100f, player.MasterVolume);
    }

    [Fact]
    public void Mute_SetsIsMutedTrue()
    {
        using var player = CreatePlayer();

        player.Mute();

        Assert.True(player.IsMuted);
    }

    [Fact]
    public void Unmute_SetsIsMutedFalse()
    {
        using var player = CreatePlayer();
        player.Mute();

        player.Unmute();

        Assert.False(player.IsMuted);
    }

    [Fact]
    public void MaxActiveSounds_DefaultIs32()
    {
        using var player = CreatePlayer();

        Assert.Equal(32, player.MaxActiveSounds);
    }

    [Fact]
    public void ActiveSoundCount_IsZero_Initially()
    {
        using var player = CreatePlayer();

        Assert.Equal(0, player.ActiveSoundCount);
    }

    // ── PlaySound limit ───────────────────────────────────────────────────────

    [Fact]
    public void PlaySound_ReturnsNull_AndLogsWarning_WhenMaxActiveSoundsReached()
    {
        string? loggedMessage = null;
        EngineLogger.SetHandler(msg => loggedMessage = msg);

        try
        {
            using var store = new AssetStore(Path.GetTempPath());
            using var player = new AudioPlayer(store);
            player.MaxActiveSounds = 0;

            var result = player.PlaySound("nonexistent.wav");

            Assert.Null(result);
            Assert.NotNull(loggedMessage);
            Assert.Contains("MaxActiveSounds", loggedMessage);
        }
        finally
        {
            EngineLogger.SetHandler(null);
        }
    }
}
