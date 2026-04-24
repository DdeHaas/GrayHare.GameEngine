using GrayHare.GameEngine.Input;
using SFML.Window;

namespace GrayHare.GameEngine.Tests.Input;

public sealed class InputActionMapTests
{
    private static InputSnapshot MakeKeyboard(
        HashSet<Keyboard.Key>? current = null,
        HashSet<Keyboard.Key>? previous = null)
    {
        return new InputSnapshot(
            current ?? [],
            previous ?? [],
            [],
            [],
            default,
            0f);
    }

    private static InputSnapshot MakeMouse(
        HashSet<Mouse.Button>? current = null,
        HashSet<Mouse.Button>? previous = null)
    {
        return new InputSnapshot(
            [],
            [],
            current ?? [],
            previous ?? [],
            default,
            0f);
    }

    private static InputSnapshot MakeJoystick(
        Dictionary<uint, HashSet<uint>>? currentButtons = null,
        Dictionary<uint, HashSet<uint>>? previousButtons = null,
        Dictionary<uint, Dictionary<Joystick.Axis, float>>? axes = null,
        HashSet<uint>? connected = null)
    {
        return new InputSnapshot(
            [],
            [],
            [],
            [],
            default,
            0f,
            currentButtons ?? [],
            previousButtons ?? [],
            axes ?? [],
            connected ?? []);
    }

    // ── Key bindings ──────────────────────────────────────────────────────────

    [Fact]
    public void MapKey_AndIsActionDown_ReturnsTrue_WhenKeyHeld()
    {
        var map = new InputActionMap();
        map.MapKey("Jump", Keyboard.Key.Space);
        InputSnapshot input = MakeKeyboard(current: [Keyboard.Key.Space]);

        Assert.True(map.IsActionDown("Jump", input));
    }

    [Fact]
    public void WasActionPressed_ReturnsTrue_OnFirstFrame()
    {
        var map = new InputActionMap();
        map.MapKey("Jump", Keyboard.Key.Space);
        InputSnapshot input = MakeKeyboard(
            current: [Keyboard.Key.Space],
            previous: []);

        Assert.True(map.WasActionPressed("Jump", input));
    }

    // ── Button bindings ───────────────────────────────────────────────────────

    [Fact]
    public void MapButton_AndIsActionDown_ReturnsTrue()
    {
        var map = new InputActionMap();
        map.MapButton("Fire", joystickId: 0, button: 1);
        InputSnapshot input = MakeJoystick(
            currentButtons: new Dictionary<uint, HashSet<uint>> { [0] = [1] },
            connected: [0]);

        Assert.True(map.IsActionDown("Fire", input));
    }

    // ── Axis bindings ─────────────────────────────────────────────────────────

    [Fact]
    public void GetAxisValue_ReturnsZero_WhenWithinDeadZone()
    {
        var map = new InputActionMap();
        map.MapAxis("MoveX", joystickId: 0, Joystick.Axis.X, deadZone: 10f);
        InputSnapshot input = MakeJoystick(
            axes: new Dictionary<uint, Dictionary<Joystick.Axis, float>>
            {
                [0] = new() { [Joystick.Axis.X] = 5f }
            });

        Assert.Equal(0f, map.GetAxisValue("MoveX", input));
    }

    [Fact]
    public void GetAxisValue_ReturnsValue_WhenOutsideDeadZone()
    {
        var map = new InputActionMap();
        map.MapAxis("MoveX", joystickId: 0, Joystick.Axis.X, deadZone: 10f);
        InputSnapshot input = MakeJoystick(
            axes: new Dictionary<uint, Dictionary<Joystick.Axis, float>>
            {
                [0] = new() { [Joystick.Axis.X] = 50f }
            });

        Assert.Equal(50f, map.GetAxisValue("MoveX", input));
    }

    // ── Clear operations ──────────────────────────────────────────────────────

    [Fact]
    public void ClearAction_RemovesAllBindings()
    {
        var map = new InputActionMap();
        map.MapKey("Jump", Keyboard.Key.Space);
        map.MapButton("Jump", joystickId: 0, button: 0);
        map.MapAxis("Jump", joystickId: 0, Joystick.Axis.Y);

        map.ClearAction("Jump");

        InputSnapshot input = MakeKeyboard(current: [Keyboard.Key.Space]);
        Assert.False(map.IsActionDown("Jump", input));
        Assert.Equal(0f, map.GetAxisValue("Jump", input));
    }

    [Fact]
    public void ClearAll_RemovesEverything()
    {
        var map = new InputActionMap();
        map.MapKey("Jump", Keyboard.Key.Space);
        map.MapKey("Fire", Keyboard.Key.F);

        map.ClearAll();

        InputSnapshot input = MakeKeyboard(current: [Keyboard.Key.Space, Keyboard.Key.F]);
        Assert.False(map.IsActionDown("Jump", input));
        Assert.False(map.IsActionDown("Fire", input));
    }

    [Fact]
    public void ActionNames_AreCaseInsensitive()
    {
        var map = new InputActionMap();
        map.MapKey("Jump", Keyboard.Key.Space);
        InputSnapshot input = MakeKeyboard(current: [Keyboard.Key.Space]);

        Assert.True(map.IsActionDown("JUMP", input));
        Assert.True(map.IsActionDown("jump", input));
    }

    // ── WasActionReleased — keyboard ──────────────────────────────────────────

    [Fact]
    public void WasActionReleased_ReturnsTrue_WhenKeyWasHeldAndIsNowUp()
    {
        var map = new InputActionMap();
        map.MapKey("Jump", Keyboard.Key.Space);
        InputSnapshot input = MakeKeyboard(
            current: [],
            previous: [Keyboard.Key.Space]);

        Assert.True(map.WasActionReleased("Jump", input));
    }

    [Fact]
    public void WasActionReleased_ReturnsFalse_WhenKeyStillHeld()
    {
        var map = new InputActionMap();
        map.MapKey("Jump", Keyboard.Key.Space);
        InputSnapshot input = MakeKeyboard(
            current: [Keyboard.Key.Space],
            previous: [Keyboard.Key.Space]);

        Assert.False(map.WasActionReleased("Jump", input));
    }

    [Fact]
    public void WasActionReleased_ReturnsFalse_WhenKeyWasNeverDown()
    {
        var map = new InputActionMap();
        map.MapKey("Jump", Keyboard.Key.Space);
        InputSnapshot input = MakeKeyboard(current: [], previous: []);

        Assert.False(map.WasActionReleased("Jump", input));
    }

    // ── Mouse button bindings ─────────────────────────────────────────────────

    [Fact]
    public void MapMouseButton_IsActionDown_ReturnsTrue_WhenButtonHeld()
    {
        var map = new InputActionMap();
        map.MapMouseButton("Shoot", Mouse.Button.Left);
        InputSnapshot input = MakeMouse(current: [Mouse.Button.Left]);

        Assert.True(map.IsActionDown("Shoot", input));
    }

    [Fact]
    public void MapMouseButton_IsActionDown_ReturnsFalse_WhenButtonUp()
    {
        var map = new InputActionMap();
        map.MapMouseButton("Shoot", Mouse.Button.Left);
        InputSnapshot input = MakeMouse(current: []);

        Assert.False(map.IsActionDown("Shoot", input));
    }

    [Fact]
    public void MapMouseButton_WasActionPressed_ReturnsTrue_OnFirstFrame()
    {
        var map = new InputActionMap();
        map.MapMouseButton("Shoot", Mouse.Button.Left);
        InputSnapshot input = MakeMouse(
            current: [Mouse.Button.Left],
            previous: []);

        Assert.True(map.WasActionPressed("Shoot", input));
    }

    [Fact]
    public void MapMouseButton_WasActionReleased_ReturnsTrue_WhenButtonJustReleased()
    {
        var map = new InputActionMap();
        map.MapMouseButton("Shoot", Mouse.Button.Left);
        InputSnapshot input = MakeMouse(
            current: [],
            previous: [Mouse.Button.Left]);

        Assert.True(map.WasActionReleased("Shoot", input));
    }

    [Fact]
    public void ClearAction_AlsoRemovesMouseButtonBindings()
    {
        var map = new InputActionMap();
        map.MapMouseButton("Shoot", Mouse.Button.Left);
        map.ClearAction("Shoot");
        InputSnapshot input = MakeMouse(current: [Mouse.Button.Left]);

        Assert.False(map.IsActionDown("Shoot", input));
    }
}
