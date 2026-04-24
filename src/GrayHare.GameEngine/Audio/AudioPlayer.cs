using GrayHare.GameEngine.Assets;
using GrayHare.GameEngine.Diagnostics;
using SFML.Audio;

namespace GrayHare.GameEngine.Audio;

/// <summary>
/// Plays and manages a pool of active <see cref="Sound"/> instances and a single
/// streamed <see cref="Music"/> track, with master/category volume control and
/// an active sound count limit.
/// </summary>
/// <remarks>This type is not thread-safe. Access all members from the main thread only.</remarks>
public sealed class AudioPlayer : IDisposable
{
    private readonly AssetStore _assets;
    private readonly List<(Sound Sound, float OriginalVolume)> _activeSounds = [];
    private Music? _currentMusic;
    private float _currentMusicVolume = 100f;
    private bool _disposed;

    /// <summary>Initializes the player with the asset store used for buffer loading.</summary>
    public AudioPlayer(AssetStore assets)
    {
        _assets = assets;
    }

    // ── Volume properties ────────────────────────────────────────────

    /// <summary>Master volume applied on top of all categories (0–100).</summary>
    public float MasterVolume { get; private set; } = 100f;

    /// <summary>Volume multiplier for sound effects (0–100).</summary>
    public float SfxVolume { get; private set; } = 100f;

    /// <summary>Volume multiplier for music (0–100).</summary>
    public float MusicVolume { get; private set; } = 100f;

    /// <summary>When <see langword="true"/>, all output is silenced without losing the stored volume levels.</summary>
    public bool IsMuted { get; private set; }

    /// <summary>
    /// Maximum number of concurrent <see cref="Sound"/> instances.
    /// <see cref="PlaySound"/> returns <see langword="null"/> when the limit is reached.
    /// </summary>
    public int MaxActiveSounds { get; set; } = 32;

    /// <summary>Number of <see cref="Sound"/> instances currently playing or paused.</summary>
    public int ActiveSoundCount => _activeSounds.Count;  // tuple list, Count is still correct

    /// <summary>Returns <see langword="true"/> when a music track is currently playing.</summary>
    public bool IsMusicPlaying => _currentMusic?.Status is SoundStatus.Playing;

    // ── Effective volume helpers (master × category, or 0 when muted) ─

    private float EffectiveSfxVolume => IsMuted ? 0f : MasterVolume * SfxVolume / 100f;

    private float EffectiveMusicVolume => IsMuted ? 0f : MasterVolume * MusicVolume / 100f;

    // ── Sound effects ────────────────────────────────────────────────

    /// <summary>
    /// Plays a sound effect from <paramref name="assetPath"/>.
    /// Returns <see langword="null"/> when <see cref="MaxActiveSounds"/> has been reached.
    /// </summary>
    /// <param name="assetPath">Asset-relative or absolute path to the audio file.</param>
    /// <param name="volume">Per-sound volume in the range [0, 100], scaled by the effective SFX volume.</param>
    /// <param name="loop">Whether the sound loops.</param>
    /// <returns>The started <see cref="Sound"/> instance, or <see langword="null"/> if the limit was reached.</returns>
    public Sound? PlaySound(string assetPath, float volume = 100f, bool loop = false)
    {
        if (_activeSounds.Count >= MaxActiveSounds)
        {
            EngineLogger.Log($"AudioPlayer: MaxActiveSounds ({MaxActiveSounds}) reached; skipping '{assetPath}'.");
            return null;
        }

        SoundBuffer buffer = _assets.LoadSoundBuffer(assetPath);
        Sound sound = new(buffer)
        {
            Volume = volume * EffectiveSfxVolume / 100f,
            IsLooping = loop
        };

        sound.Play();
        _activeSounds.Add((sound, volume));

        return sound;
    }

    // ── Music streaming ──────────────────────────────────────────────

    /// <summary>
    /// Streams a music track from <paramref name="assetPath"/>.
    /// Any previously playing music is stopped and disposed first.
    /// Uses SFML's <see cref="Music"/> class which streams from disk rather than buffering the entire file.
    /// </summary>
    /// <param name="assetPath">Asset-relative or absolute path to the music file.</param>
    /// <param name="volume">Per-track volume in the range [0, 100], scaled by the effective music volume.</param>
    /// <param name="loop">Whether the track loops (default <see langword="true"/>).</param>
    /// <returns>The started <see cref="Music"/> instance.</returns>
    public Music? PlayMusic(string assetPath, float volume = 100f, bool loop = true)
    {
        StopMusic();

        string resolvedPath = _assets.ResolvePath(assetPath);
        Music music = new(resolvedPath)
        {
            IsLooping = loop
        };

        _currentMusicVolume = Math.Clamp(volume, 0f, 100f);
        _currentMusic = music;
        _currentMusic.Volume = _currentMusicVolume * EffectiveMusicVolume / 100f;

        music.Play();

        return _currentMusic;
    }

    /// <summary>Stops and disposes the current music track, if any.</summary>
    public void StopMusic()
    {
        if (_currentMusic is null)
        {
            return;
        }

        _currentMusic.Stop();
        _currentMusic.Dispose();
        _currentMusic = null;
    }

    /// <summary>Pauses the current music track if it is playing.</summary>
    public void PauseMusic()
    {
        if (_currentMusic?.Status is SoundStatus.Playing)
        {
            _currentMusic.Pause();
        }
    }

    /// <summary>Resumes the current music track if it is paused.</summary>
    public void ResumeMusic()
    {
        if (_currentMusic?.Status is SoundStatus.Paused)
        {
            _currentMusic.Play();
        }
    }

    // ── Volume control ───────────────────────────────────────────────

    /// <summary>Sets the master volume (clamped to 0–100) and updates all active audio.</summary>
    /// <param name="volume">Desired master volume.</param>
    public void SetMasterVolume(float volume)
    {
        MasterVolume = Math.Clamp(volume, 0f, 100f);
        ApplyVolumeToActiveSounds();
        ApplyVolumeToMusic();
    }

    /// <summary>Sets the sound-effect volume (clamped to 0–100) and updates active sounds.</summary>
    /// <param name="volume">Desired SFX volume.</param>
    public void SetSfxVolume(float volume)
    {
        SfxVolume = Math.Clamp(volume, 0f, 100f);
        ApplyVolumeToActiveSounds();
    }

    /// <summary>Sets the music volume (clamped to 0–100) and updates the current track.</summary>
    /// <param name="volume">Desired music volume.</param>
    public void SetMusicVolume(float volume)
    {
        MusicVolume = Math.Clamp(volume, 0f, 100f);
        ApplyVolumeToMusic();
    }

    /// <summary>Mutes all audio output without changing stored volume levels.</summary>
    public void Mute()
    {
        IsMuted = true;
        ApplyVolumeToActiveSounds();
        ApplyVolumeToMusic();
    }

    /// <summary>Restores audio output to the stored volume levels.</summary>
    public void Unmute()
    {
        IsMuted = false;
        ApplyVolumeToActiveSounds();
        ApplyVolumeToMusic();
    }

    // ── Frame update ─────────────────────────────────────────────────

    /// <summary>
    /// Removes and disposes any sounds that have finished playing.
    /// Called automatically by the engine once per frame; scenes do not need to call this.
    /// </summary>
    public void Update()
    {
        for (int index = _activeSounds.Count - 1; index >= 0; index--)
        {
            if (_activeSounds[index].Sound.Status != SoundStatus.Stopped)
            {
                continue;
            }

            _activeSounds[index].Sound.Dispose();
            _activeSounds.RemoveAt(index);
        }
    }

    // ── Cleanup ──────────────────────────────────────────────────────

    /// <summary>Immediately stops and disposes all active sounds and the current music track.</summary>
    public void StopAll()
    {
        foreach ((Sound sound, _) in _activeSounds)
        {
            sound.Stop();
            sound.Dispose();
        }

        _activeSounds.Clear();

        StopMusic();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopAll();
    }

    // ── Private helpers ──────────────────────────────────────────────

    /// <summary>
    /// Reapplies the effective SFX volume to every active sound, preserving each sound's original per-sound volume.
    /// </summary>
    private void ApplyVolumeToActiveSounds()
    {
        float effective = EffectiveSfxVolume;
        foreach ((Sound sound, float originalVolume) in _activeSounds)
        {
            sound.Volume = originalVolume * effective / 100f;
        }
    }

    /// <summary>
    /// Reapplies the effective music volume to the current track, preserving its original per-track volume.
    /// </summary>
    private void ApplyVolumeToMusic()
    {
        if (_currentMusic is not null)
        {
            _currentMusic.Volume = _currentMusicVolume * EffectiveMusicVolume / 100f;
        }
    }
}
