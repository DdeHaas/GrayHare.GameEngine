using SFML.Window;

namespace GrayHare.GameEngine.Input;

/// <summary>
/// Maps named game actions to physical input bindings, allowing games to reference
/// actions by name instead of hardcoding specific keys or buttons.
/// </summary>
/// <example>
/// <code>
/// var map = new InputActionMap();
/// map.MapKey("Jump", Keyboard.Key.Space);
/// map.MapButton("Jump", joystickId: 0, button: 0);
/// map.MapMouseButton("Fire", Mouse.Button.Left);
/// map.MapAxis("MoveX", joystickId: 0, Joystick.Axis.X);
///
/// if (map.WasActionPressed("Jump", host.Input)) { /* jump */ }
/// float moveX = map.GetAxisValue("MoveX", host.Input);
/// </code>
/// </example>
/// <remarks>This type is not thread-safe. Access all members from the main thread only.</remarks>
public sealed class InputActionMap
{
    private readonly record struct JoystickButtonBinding(uint JoystickId, uint Button);
    private readonly record struct AxisBinding(uint JoystickId, Joystick.Axis Axis, float DeadZone);

    private readonly Dictionary<string, List<Keyboard.Key>> _keyBindings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<JoystickButtonBinding>> _buttonBindings =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<AxisBinding>> _axisBindings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<Mouse.Button>> _mouseButtonBindings =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Maps a keyboard key to an action. Multiple keys can map to the same action.
    /// </summary>
    /// <param name="action">The action name (case-insensitive).</param>
    /// <param name="key">The keyboard key to bind.</param>
    public void MapKey(string action, Keyboard.Key key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        if (!_keyBindings.TryGetValue(action, out List<Keyboard.Key>? keys))
        {
            keys = [];
            _keyBindings[action] = keys;
        }

        if (!keys.Contains(key))
        {
            keys.Add(key);
        }
    }

    /// <summary>
    /// Maps a joystick button to an action. Multiple buttons can map to the same action.
    /// </summary>
    /// <param name="action">The action name (case-insensitive).</param>
    /// <param name="joystickId">The joystick identifier.</param>
    /// <param name="button">The button index on the joystick.</param>
    public void MapButton(string action, uint joystickId, uint button)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        if (!_buttonBindings.TryGetValue(action, out List<JoystickButtonBinding>? bindings))
        {
            bindings = [];
            _buttonBindings[action] = bindings;
        }

        JoystickButtonBinding binding = new(joystickId, button);
        if (!bindings.Contains(binding))
        {
            bindings.Add(binding);
        }
    }

    /// <summary>
    /// Maps a mouse button to an action. Multiple mouse buttons can map to the same action.
    /// </summary>
    /// <param name="action">The action name (case-insensitive).</param>
    /// <param name="button">The mouse button to bind.</param>
    public void MapMouseButton(string action, Mouse.Button button)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        if (!_mouseButtonBindings.TryGetValue(action, out List<Mouse.Button>? buttons))
        {
            buttons = [];
            _mouseButtonBindings[action] = buttons;
        }

        if (!buttons.Contains(button))
        {
            buttons.Add(button);
        }
    }

    /// <summary>
    /// Maps a joystick axis to an action with an optional dead zone.
    /// </summary>
    /// <param name="action">The action name (case-insensitive).</param>
    /// <param name="joystickId">The joystick identifier.</param>
    /// <param name="axis">The joystick axis to bind.</param>
    /// <param name="deadZone">
    /// Axis values with an absolute value below this threshold are treated as zero.
    /// Defaults to <c>10</c>.
    /// </param>
    public void MapAxis(string action, uint joystickId, Joystick.Axis axis, float deadZone = 10f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        if (!_axisBindings.TryGetValue(action, out List<AxisBinding>? bindings))
        {
            bindings = [];
            _axisBindings[action] = bindings;
        }

        AxisBinding binding = new(joystickId, axis, deadZone);
        if (!bindings.Contains(binding))
        {
            bindings.Add(binding);
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> while any binding for <paramref name="action"/> is active (held down).
    /// Checks keyboard keys, mouse buttons, and joystick buttons.
    /// </summary>
    /// <param name="action">The action name (case-insensitive).</param>
    /// <param name="input">The current frame's input snapshot.</param>
    public bool IsActionDown(string action, InputSnapshot input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(input);

        if (_keyBindings.TryGetValue(action, out List<Keyboard.Key>? keys))
        {
            foreach (Keyboard.Key key in keys)
            {
                if (input.IsKeyDown(key))
                {
                    return true;
                }
            }
        }

        if (_mouseButtonBindings.TryGetValue(action, out List<Mouse.Button>? mouseButtons))
        {
            foreach (Mouse.Button button in mouseButtons)
            {
                if (input.IsMouseButtonDown(button))
                {
                    return true;
                }
            }
        }

        if (_buttonBindings.TryGetValue(action, out List<JoystickButtonBinding>? buttons))
        {
            foreach (JoystickButtonBinding binding in buttons)
            {
                if (input.IsJoystickButtonDown(binding.JoystickId, binding.Button))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> on the first frame any binding for <paramref name="action"/> was pressed.
    /// Checks keyboard keys, mouse buttons, and joystick buttons.
    /// </summary>
    /// <param name="action">The action name (case-insensitive).</param>
    /// <param name="input">The current frame's input snapshot.</param>
    public bool WasActionPressed(string action, InputSnapshot input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(input);

        if (_keyBindings.TryGetValue(action, out List<Keyboard.Key>? keys))
        {
            foreach (Keyboard.Key key in keys)
            {
                if (input.WasKeyPressed(key))
                {
                    return true;
                }
            }
        }

        if (_mouseButtonBindings.TryGetValue(action, out List<Mouse.Button>? mouseButtons))
        {
            foreach (Mouse.Button button in mouseButtons)
            {
                if (input.WasMouseButtonPressed(button))
                {
                    return true;
                }
            }
        }

        if (_buttonBindings.TryGetValue(action, out List<JoystickButtonBinding>? buttons))
        {
            foreach (JoystickButtonBinding binding in buttons)
            {
                if (input.WasJoystickButtonPressed(binding.JoystickId, binding.Button))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> on the first frame any binding for <paramref name="action"/> was released.
    /// Checks keyboard keys, mouse buttons, and joystick buttons.
    /// </summary>
    /// <param name="action">The action name (case-insensitive).</param>
    /// <param name="input">The current frame's input snapshot.</param>
    public bool WasActionReleased(string action, InputSnapshot input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(input);

        if (_keyBindings.TryGetValue(action, out List<Keyboard.Key>? keys))
        {
            foreach (Keyboard.Key key in keys)
            {
                if (input.WasKeyReleased(key))
                {
                    return true;
                }
            }
        }

        if (_mouseButtonBindings.TryGetValue(action, out List<Mouse.Button>? mouseButtons))
        {
            foreach (Mouse.Button button in mouseButtons)
            {
                if (input.WasMouseButtonReleased(button))
                {
                    return true;
                }
            }
        }

        if (_buttonBindings.TryGetValue(action, out List<JoystickButtonBinding>? buttons))
        {
            foreach (JoystickButtonBinding binding in buttons)
            {
                if (input.WasJoystickButtonReleased(binding.JoystickId, binding.Button))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the axis value for <paramref name="action"/>. If multiple axes are bound,
    /// the first value outside the dead zone is returned. Returns <c>0</c> if no axis is
    /// bound or all values fall within the dead zone.
    /// </summary>
    /// <param name="action">The action name (case-insensitive).</param>
    /// <param name="input">The current frame's input snapshot.</param>
    public float GetAxisValue(string action, InputSnapshot input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(input);

        if (!_axisBindings.TryGetValue(action, out List<AxisBinding>? bindings))
        {
            return 0f;
        }

        foreach (AxisBinding binding in bindings)
        {
            float value = input.GetJoystickAxis(binding.JoystickId, binding.Axis);
            if (MathF.Abs(value) > binding.DeadZone)
            {
                return value;
            }
        }

        return 0f;
    }

    /// <summary>
    /// Removes all bindings for <paramref name="action"/>.
    /// </summary>
    /// <param name="action">The action name (case-insensitive).</param>
    public void ClearAction(string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        _keyBindings.Remove(action);
        _buttonBindings.Remove(action);
        _axisBindings.Remove(action);
        _mouseButtonBindings.Remove(action);
    }

    /// <summary>
    /// Removes all bindings from the action map.
    /// </summary>
    public void ClearAll()
    {
        _keyBindings.Clear();
        _buttonBindings.Clear();
        _axisBindings.Clear();
        _mouseButtonBindings.Clear();
    }
}
