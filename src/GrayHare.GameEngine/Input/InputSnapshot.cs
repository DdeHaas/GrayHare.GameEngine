using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.Input;

/// <summary>
/// Snapshot of all input state for a single frame.
/// The engine maintains one live instance per <see cref="InputTracker"/> that is
/// updated in-place each frame to avoid per-frame allocations.
/// </summary>
/// <remarks>This type is not thread-safe. Access all members from the main thread only.</remarks>
public sealed class InputSnapshot
{
    private readonly HashSet<Keyboard.Key> _currentKeys;
    private readonly HashSet<Keyboard.Key> _previousKeys;
    private readonly HashSet<Mouse.Button> _currentButtons;
    private readonly HashSet<Mouse.Button> _previousButtons;
    private readonly Dictionary<uint, HashSet<uint>> _currentJoystickButtons;
    private readonly Dictionary<uint, HashSet<uint>> _previousJoystickButtons;
    private readonly Dictionary<uint, Dictionary<Joystick.Axis, float>> _joystickAxes;
    private readonly HashSet<uint> _connectedJoysticks;

    /// <summary>An empty snapshot with no input active.</summary>
    public static InputSnapshot Empty { get; } = new();

    /// <summary>
    /// Creates the live snapshot instance used by <see cref="InputTracker"/>.
    /// Pre-allocates the backing collections so they can be reused every frame.
    /// </summary>
    internal InputSnapshot()
    {
        _currentKeys = new HashSet<Keyboard.Key>();
        _previousKeys = new HashSet<Keyboard.Key>();
        _currentButtons = new HashSet<Mouse.Button>();
        _previousButtons = new HashSet<Mouse.Button>();
        _currentJoystickButtons = new Dictionary<uint, HashSet<uint>>();
        _previousJoystickButtons = new Dictionary<uint, HashSet<uint>>();
        _joystickAxes = new Dictionary<uint, Dictionary<Joystick.Axis, float>>();
        _connectedJoysticks = new HashSet<uint>();
    }

    /// <summary>
    /// Initializes a snapshot from caller-supplied sets.
    /// Intended for tests or one-off construction outside the engine loop.
    /// The sets are referenced directly — the caller must not mutate them after this call.
    /// </summary>
    public InputSnapshot(
        HashSet<Keyboard.Key> currentKeys,
        HashSet<Keyboard.Key> previousKeys,
        HashSet<Mouse.Button> currentButtons,
        HashSet<Mouse.Button> previousButtons,
        Vector2i mousePosition,
        float mouseWheelDelta)
    {
        _currentKeys = currentKeys;
        _previousKeys = previousKeys;
        _currentButtons = currentButtons;
        _previousButtons = previousButtons;
        MousePosition = mousePosition;
        MouseWheelDelta = mouseWheelDelta;
        _currentJoystickButtons = new Dictionary<uint, HashSet<uint>>();
        _previousJoystickButtons = new Dictionary<uint, HashSet<uint>>();
        _joystickAxes = new Dictionary<uint, Dictionary<Joystick.Axis, float>>();
        _connectedJoysticks = new HashSet<uint>();
    }

    /// <summary>
    /// Initializes a snapshot from caller-supplied sets including joystick state.
    /// Intended for tests or one-off construction outside the engine loop.
    /// The collections are referenced directly — the caller must not mutate them after this call.
    /// </summary>
    public InputSnapshot(
        HashSet<Keyboard.Key> currentKeys,
        HashSet<Keyboard.Key> previousKeys,
        HashSet<Mouse.Button> currentButtons,
        HashSet<Mouse.Button> previousButtons,
        Vector2i mousePosition,
        float mouseWheelDelta,
        Dictionary<uint, HashSet<uint>> currentJoystickButtons,
        Dictionary<uint, HashSet<uint>> previousJoystickButtons,
        Dictionary<uint, Dictionary<Joystick.Axis, float>> joystickAxes,
        HashSet<uint> connectedJoysticks)
    {
        _currentKeys = currentKeys;
        _previousKeys = previousKeys;
        _currentButtons = currentButtons;
        _previousButtons = previousButtons;
        MousePosition = mousePosition;
        MouseWheelDelta = mouseWheelDelta;
        _currentJoystickButtons = currentJoystickButtons;
        _previousJoystickButtons = previousJoystickButtons;
        _joystickAxes = joystickAxes;
        _connectedJoysticks = connectedJoysticks;
    }

    /// <summary>
    /// Updates the snapshot in-place from the tracker's raw state.
    /// Called by <see cref="InputTracker"/> — never allocates new collections.
    /// </summary>
    internal void Refresh(
        HashSet<Keyboard.Key> currentKeys,
        HashSet<Keyboard.Key> previousKeys,
        HashSet<Mouse.Button> currentButtons,
        HashSet<Mouse.Button> previousButtons,
        Vector2i mousePosition,
        float mouseWheelDelta,
        Dictionary<uint, HashSet<uint>> currentJoystickButtons,
        Dictionary<uint, HashSet<uint>> previousJoystickButtons,
        Dictionary<uint, Dictionary<Joystick.Axis, float>> joystickAxes,
        HashSet<uint> connectedJoysticks)
    {
        _currentKeys.Clear();
        _currentKeys.UnionWith(currentKeys);
        _previousKeys.Clear();
        _previousKeys.UnionWith(previousKeys);
        _currentButtons.Clear();
        _currentButtons.UnionWith(currentButtons);
        _previousButtons.Clear();
        _previousButtons.UnionWith(previousButtons);
        MousePosition = mousePosition;
        MouseWheelDelta = mouseWheelDelta;

        CopyJoystickButtons(currentJoystickButtons, _currentJoystickButtons);
        CopyJoystickButtons(previousJoystickButtons, _previousJoystickButtons);
        CopyJoystickAxes(joystickAxes, _joystickAxes);

        _connectedJoysticks.Clear();
        _connectedJoysticks.UnionWith(connectedJoysticks);
    }

    /// <summary>All keys currently held down.</summary>
    public IReadOnlySet<Keyboard.Key> CurrentKeys => _currentKeys;

    /// <summary>Keys that were held down last frame.</summary>
    public IReadOnlySet<Keyboard.Key> PreviousKeys => _previousKeys;

    /// <summary>Mouse buttons currently held down.</summary>
    public IReadOnlySet<Mouse.Button> CurrentButtons => _currentButtons;

    /// <summary>Mouse buttons that were held down last frame.</summary>
    public IReadOnlySet<Mouse.Button> PreviousButtons => _previousButtons;

    /// <summary>Current mouse position in window coordinates.</summary>
    public Vector2i MousePosition { get; private set; }

    /// <summary>Accumulated mouse-wheel scroll delta this frame.</summary>
    public float MouseWheelDelta { get; private set; }

    /// <summary>Set of joystick IDs that are currently connected.</summary>
    public IReadOnlySet<uint> ConnectedJoysticks => _connectedJoysticks;

    /// <summary>Returns <see langword="true"/> while <paramref name="key"/> is held down.</summary>
    public bool IsKeyDown(Keyboard.Key key)
    {
        return CurrentKeys.Contains(key);
    }

    /// <summary>Returns <see langword="true"/> on the first frame <paramref name="key"/> was pressed.</summary>
    public bool WasKeyPressed(Keyboard.Key key)
    {
        return CurrentKeys.Contains(key) && !PreviousKeys.Contains(key);
    }

    /// <summary>Returns <see langword="true"/> on the first frame any key was pressed.</summary>
    public bool WasAnyKeyPressed()
    {
        return CurrentKeys.Any(k => !PreviousKeys.Contains(k));
    }

    /// <summary>Returns <see langword="true"/> on the first frame <paramref name="key"/> was released.</summary>
    public bool WasKeyReleased(Keyboard.Key key)
    {
        return !CurrentKeys.Contains(key) && PreviousKeys.Contains(key);
    }

    /// <summary>Returns <see langword="true"/> while <paramref name="button"/> is held down.</summary>
    public bool IsMouseButtonDown(Mouse.Button button)
    {
        return CurrentButtons.Contains(button);
    }

    /// <summary>Returns <see langword="true"/> on the first frame <paramref name="button"/> was pressed.</summary>
    public bool WasMouseButtonPressed(Mouse.Button button)
    {
        return CurrentButtons.Contains(button) && !PreviousButtons.Contains(button);
    }

    /// <summary>Returns <see langword="true"/> on the first frame <paramref name="button"/> was released.</summary>
    public bool WasMouseButtonReleased(Mouse.Button button)
    {
        return !CurrentButtons.Contains(button) && PreviousButtons.Contains(button);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the joystick with <paramref name="joystickId"/> is connected.
    /// </summary>
    public bool IsJoystickConnected(uint joystickId)
    {
        return _connectedJoysticks.Contains(joystickId);
    }

    /// <summary>
    /// Returns <see langword="true"/> while <paramref name="button"/> on joystick
    /// <paramref name="joystickId"/> is held down.
    /// </summary>
    public bool IsJoystickButtonDown(uint joystickId, uint button)
    {
        return _currentJoystickButtons.TryGetValue(joystickId, out HashSet<uint>? buttons) && buttons.Contains(button);
    }

    /// <summary>
    /// Returns <see langword="true"/> on the first frame <paramref name="button"/> on joystick
    /// <paramref name="joystickId"/> was pressed.
    /// </summary>
    public bool WasJoystickButtonPressed(uint joystickId, uint button)
    {
        bool current = _currentJoystickButtons.TryGetValue(joystickId, out HashSet<uint>? currentButtonSet)
            && currentButtonSet.Contains(button);
        bool previous = _previousJoystickButtons.TryGetValue(joystickId, out HashSet<uint>? previousButtonSet)
            && previousButtonSet.Contains(button);

        return current && !previous;
    }

    /// <summary>
    /// Returns <see langword="true"/> on the first frame <paramref name="button"/> on joystick
    /// <paramref name="joystickId"/> was released.
    /// </summary>
    public bool WasJoystickButtonReleased(uint joystickId, uint button)
    {
        bool current = _currentJoystickButtons.TryGetValue(joystickId, out HashSet<uint>? currentButtonSet)
            && currentButtonSet.Contains(button);
        bool previous = _previousJoystickButtons.TryGetValue(joystickId, out HashSet<uint>? previousButtonSet)
            && previousButtonSet.Contains(button);

        return !current && previous;
    }

    /// <summary>
    /// Returns the current position of <paramref name="axis"/> on joystick <paramref name="joystickId"/>.
    /// Returns <c>0</c> if the joystick or axis is not found.
    /// </summary>
    public float GetJoystickAxis(uint joystickId, Joystick.Axis axis)
    {
        if (_joystickAxes.TryGetValue(joystickId, out Dictionary<Joystick.Axis, float>? axes) &&
            axes.TryGetValue(axis, out float position))
        {
            return position;
        }

        return 0f;
    }

    /// <summary>
    /// Copies joystick button state from source to destination without allocating new outer dictionaries.
    /// </summary>
    private static void CopyJoystickButtons(
        Dictionary<uint, HashSet<uint>> source,
        Dictionary<uint, HashSet<uint>> destination)
    {
        // Remove joystick entries that no longer exist in source.
        foreach (uint id in destination.Keys.ToArray())
        {
            if (!source.ContainsKey(id))
            {
                destination.Remove(id);
            }
        }

        foreach ((uint id, HashSet<uint> buttons) in source)
        {
            if (!destination.TryGetValue(id, out HashSet<uint>? dest))
            {
                dest = new HashSet<uint>();
                destination[id] = dest;
            }

            dest.Clear();
            dest.UnionWith(buttons);
        }
    }

    /// <summary>
    /// Copies joystick axis state from source to destination without allocating new outer dictionaries.
    /// </summary>
    private static void CopyJoystickAxes(
        Dictionary<uint, Dictionary<Joystick.Axis, float>> source,
        Dictionary<uint, Dictionary<Joystick.Axis, float>> destination)
    {
        foreach (uint id in destination.Keys.ToArray())
        {
            if (!source.ContainsKey(id))
            {
                destination.Remove(id);
            }
        }

        foreach ((uint id, Dictionary<Joystick.Axis, float> axes) in source)
        {
            if (!destination.TryGetValue(id, out Dictionary<Joystick.Axis, float>? dest))
            {
                dest = new Dictionary<Joystick.Axis, float>();
                destination[id] = dest;
            }

            dest.Clear();
            foreach ((Joystick.Axis axis, float value) in axes)
            {
                dest[axis] = value;
            }
        }
    }
}
