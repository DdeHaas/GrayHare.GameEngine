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
    // Pool of Sound instances keyed by their OpenAL source. Stopped entries are reused in-place to
    // avoid the per-call allGenSources allocation that causes hitches on the main thread.
    private readonly List<(Sound Sound, float OriginalVolume)> _soundPool = [];
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
    public int ActiveSoundCount
    {
        get
        {
            int count = 0;
            foreach ((Sound sound, _) in _soundPool)
            {
                if (sound.Status != SoundStatus.Stopped)
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>Returns <see langword="true"/> when a music track is currently playing.</summary>
    public bool IsMusicPlaying => _currentMusic?.Status is SoundStatus.Playing;

    // ── Effective volume helpers (master × category, or 0 when muted) ─

    private float EffectiveSfxVolume => IsMuted ? 0f : MasterVolume * SfxVolume / 100f;

    private float EffectiveMusicVolume => IsMuted ? 0f : MasterVolume * MusicVolume / 100f;

    // ── Sound effects ────────────────────────────────────────────────

    /// <summary>
    /// Loads the sound buffer for <paramref name="assetPath"/> and pre-allocates a pooled OpenAL
    /// source by playing it silently then stopping it immediately.
    /// </summary>
    /// <remarks>
    /// Call this from <c>Load</c> for every sound the scene will play during gameplay. Without it,
    /// the first <see cref="PlaySound"/> call for a given path stalls on <c>alGenSources</c>
    /// allocation, causing a brief hitch in the update loop.
    /// </remarks>
    /// <param name="assetPath">Asset-relative or absolute path to the audio file.</param>
    public void PrewarmSound(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        if (_soundPool.Count >= MaxActiveSounds)
        {
            return;
        }

        SoundBuffer buffer = _assets.LoadSoundBuffer(assetPath);

        // Allocate the OpenAL source now, at load time, rather than on the first gameplay play call.
        Sound sound = new(buffer) { Volume = 0f };
        sound.Play();
        sound.Stop();

        // Restore to the current effective volume so ApplyVolumeToActiveSounds stays consistent.
        sound.Volume = EffectiveSfxVolume;

        _soundPool.Add((sound, 100f));
    }

    /// <summary>
    /// Plays a sound effect from <paramref name="assetPath"/>.
    /// Returns <see langword="null"/> when <see cref="MaxActiveSounds"/> has been reached.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="Sound"/> is a pooled instance owned by this player.
    /// Do not retain the reference; it may be reassigned to a different sound on the next play call.
    /// </remarks>
    /// <param name="assetPath">Asset-relative or absolute path to the audio file.</param>
    /// <param name="volume">Per-sound volume in the range [0, 100], scaled by the effective SFX volume.</param>
    /// <param name="loop">Whether the sound loops.</param>
    /// <returns>The started <see cref="Sound"/> instance, or <see langword="null"/> if the limit was reached.</returns>
    public Sound? PlaySound(string assetPath, float volume = 100f, bool loop = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        // Count active (playing/paused) sounds and locate the first stopped entry for reuse.
        int activeCount = 0;
        int reusableIndex = -1;
        for (int i = 0; i < _soundPool.Count; i++)
        {
            if (_soundPool[i].Sound.Status == SoundStatus.Stopped)
            {
                if (reusableIndex == -1)
                {
                    reusableIndex = i;
                }
            }
            else
            {
                activeCount++;
            }
        }

        if (activeCount >= MaxActiveSounds)
        {
            EngineLogger.Log($"AudioPlayer: MaxActiveSounds ({MaxActiveSounds}) reached; skipping '{assetPath}'.");

            return null;
        }

        SoundBuffer buffer = _assets.LoadSoundBuffer(assetPath);
        float effectiveVolume = volume * EffectiveSfxVolume / 100f;

        Sound sound;
        if (reusableIndex >= 0)
        {
            // Reuse the stopped Sound source to avoid a new alGenSources allocation.
            sound = _soundPool[reusableIndex].Sound;
            sound.SoundBuffer = buffer;
            sound.Pitch = 1.0f;
            sound.Pan = 0.0f;
            sound.Volume = effectiveVolume;
            sound.IsLooping = loop;
            _soundPool[reusableIndex] = (sound, volume);
        }
        else
        {
            sound = new Sound(buffer)
            {
                Volume = effectiveVolume,
                IsLooping = loop
            };
            _soundPool.Add((sound, volume));
        }

        sound.Play();

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
    /// Reserved for engine frame-update calls.
    /// The sound pool is self-managing: stopped entries are reused in-place by <see cref="PlaySound"/>
    /// and do not need per-frame disposal.
    /// </summary>
    public void Update()
    {
        // Pool entries are reused by PlaySound; no per-frame removal or disposal is needed.
    }

    // ── Cleanup ──────────────────────────────────────────────────────

    /// <summary>Immediately stops and disposes all active sounds and the current music track.</summary>
    public void StopAll()
    {
        foreach ((Sound sound, _) in _soundPool)
        {
            sound.Stop();
        }

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

        foreach ((Sound sound, _) in _soundPool)
        {
            sound.Stop();
            sound.Dispose();
        }

        _soundPool.Clear();

        StopMusic();
    }

    // ── Private helpers ──────────────────────────────────────────────

    /// <summary>
    /// Reapplies the effective SFX volume to every active sound, preserving each sound's original per-sound volume.
    /// </summary>
    private void ApplyVolumeToActiveSounds()
    {
        float effective = EffectiveSfxVolume;
        foreach ((Sound sound, float originalVolume) in _soundPool)
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
