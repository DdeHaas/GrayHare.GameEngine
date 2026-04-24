using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.Input;

/// <summary>
/// Accumulates raw window events and produces an <see cref="InputSnapshot"/> each frame.
/// </summary>
/// <remarks>This type is not thread-safe. Access all members from the main thread only.</remarks>
public sealed class InputTracker
{
    private readonly HashSet<Keyboard.Key> _currentKeys = [];
    private readonly HashSet<Keyboard.Key> _previousKeys = [];
    private readonly HashSet<Mouse.Button> _currentButtons = [];
    private readonly HashSet<Mouse.Button> _previousButtons = [];
    private Vector2i _mousePosition;
    private float _mouseWheelDelta;

    private readonly Dictionary<uint, HashSet<uint>> _currentJoystickButtons = new();
    private readonly Dictionary<uint, HashSet<uint>> _previousJoystickButtons = new();
    private readonly Dictionary<uint, Dictionary<Joystick.Axis, float>> _joystickAxes = new();
    private readonly HashSet<uint> _connectedJoysticks = new();

    // Single live snapshot reused every frame — avoids per-frame allocations.
    private readonly InputSnapshot _current = new();

    /// <summary>The current frame's input snapshot.</summary>
    public InputSnapshot Current => _current;

    /// <summary>
    /// Must be called at the start of every frame before dispatching window events.
    /// Copies current state to previous and clears per-frame accumulators.
    /// </summary>
    public void BeginFrame()
    {
        _previousKeys.Clear();
        foreach (Keyboard.Key key in _currentKeys)
        {
            _previousKeys.Add(key);
        }

        _previousButtons.Clear();
        foreach (Mouse.Button button in _currentButtons)
        {
            _previousButtons.Add(button);
        }

        // Copy current joystick buttons to previous.
        foreach (uint id in _previousJoystickButtons.Keys.ToArray())
        {
            if (!_currentJoystickButtons.ContainsKey(id))
            {
                _previousJoystickButtons.Remove(id);
            }
        }

        foreach ((uint id, HashSet<uint> buttons) in _currentJoystickButtons)
        {
            if (!_previousJoystickButtons.TryGetValue(id, out HashSet<uint>? prev))
            {
                prev = new HashSet<uint>();
                _previousJoystickButtons[id] = prev;
            }

            prev.Clear();
            prev.UnionWith(buttons);
        }

        _mouseWheelDelta = 0f;
        RefreshSnapshot();
    }

    /// <summary>Synchronizes the stored mouse position with the window.</summary>
    public void SyncMousePosition(WindowBase window)
    {
        _mousePosition = Mouse.GetPosition(window);
        RefreshSnapshot();
    }

    /// <summary>Called when a key is pressed.</summary>
    public void OnKeyPressed(Keyboard.Key key)
    {
        _currentKeys.Add(key);
        RefreshSnapshot();
    }

    /// <summary>Called when a key is released.</summary>
    public void OnKeyReleased(Keyboard.Key key)
    {
        _currentKeys.Remove(key);
        RefreshSnapshot();
    }

    /// <summary>Called when a mouse button is pressed.</summary>
    public void OnMouseButtonPressed(Mouse.Button button, Vector2i position)
    {
        _currentButtons.Add(button);
        _mousePosition = position;
        RefreshSnapshot();
    }

    /// <summary>Called when a mouse button is released.</summary>
    public void OnMouseButtonReleased(Mouse.Button button, Vector2i position)
    {
        _currentButtons.Remove(button);
        _mousePosition = position;
        RefreshSnapshot();
    }

    /// <summary>Called when the mouse is moved.</summary>
    public void OnMouseMoved(Vector2i position)
    {
        _mousePosition = position;
        RefreshSnapshot();
    }

    /// <summary>Called when the mouse wheel is scrolled.</summary>
    public void OnMouseWheelScrolled(float delta, Vector2i position)
    {
        _mouseWheelDelta += delta;
        _mousePosition = position;
        RefreshSnapshot();
    }

    /// <summary>Called when a joystick button is pressed.</summary>
    public void OnJoystickButtonPressed(uint joystickId, uint button)
    {
        if (!_currentJoystickButtons.TryGetValue(joystickId, out HashSet<uint>? buttons))
        {
            buttons = new HashSet<uint>();
            _currentJoystickButtons[joystickId] = buttons;
        }

        buttons.Add(button);
        RefreshSnapshot();
    }

    /// <summary>Called when a joystick button is released.</summary>
    public void OnJoystickButtonReleased(uint joystickId, uint button)
    {
        if (_currentJoystickButtons.TryGetValue(joystickId, out HashSet<uint>? buttons))
        {
            buttons.Remove(button);
        }

        RefreshSnapshot();
    }

    /// <summary>Called when a joystick axis is moved.</summary>
    public void OnJoystickMoved(uint joystickId, Joystick.Axis axis, float position)
    {
        if (!_joystickAxes.TryGetValue(joystickId, out Dictionary<Joystick.Axis, float>? axes))
        {
            axes = new Dictionary<Joystick.Axis, float>();
            _joystickAxes[joystickId] = axes;
        }

        axes[axis] = position;
        RefreshSnapshot();
    }

    /// <summary>Called when a joystick is connected.</summary>
    public void OnJoystickConnected(uint joystickId)
    {
        _connectedJoysticks.Add(joystickId);
        RefreshSnapshot();
    }

    /// <summary>
    /// Polls the hardware directly and marks any already-connected joysticks as connected.
    /// Call once after the window is created, before the main loop, to detect joysticks
    /// that were connected at launch (SFML does not guarantee a <c>JoystickConnected</c>
    /// event for those). Button and axis state remain neutral until SFML emits the
    /// corresponding events on the first frame.
    /// </summary>
    internal void InitializeJoysticks()
    {
        Joystick.Update();

        for (uint i = 0; i < Joystick.Count; i++)
        {
            if (Joystick.IsConnected(i))
            {
                _connectedJoysticks.Add(i);
            }
        }

        RefreshSnapshot();
    }

    /// <summary>Called when a joystick is disconnected.</summary>
    public void OnJoystickDisconnected(uint joystickId)
    {
        _connectedJoysticks.Remove(joystickId);
        _currentJoystickButtons.Remove(joystickId);
        _previousJoystickButtons.Remove(joystickId);
        _joystickAxes.Remove(joystickId);
        RefreshSnapshot();
    }

    private void RefreshSnapshot()
    {
        _current.Refresh(
            _currentKeys,
            _previousKeys,
            _currentButtons,
            _previousButtons,
            _mousePosition,
            _mouseWheelDelta,
            _currentJoystickButtons,
            _previousJoystickButtons,
            _joystickAxes,
            _connectedJoysticks);
    }
}
