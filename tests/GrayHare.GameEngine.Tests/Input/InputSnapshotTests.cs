using GrayHare.GameEngine.Input;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.Tests.Input;

public sealed class InputSnapshotTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static InputSnapshot Make(
        HashSet<Keyboard.Key>? current = null,
        HashSet<Keyboard.Key>? previous = null,
        HashSet<Mouse.Button>? currentButtons = null,
        HashSet<Mouse.Button>? previousButtons = null,
        Vector2i? mousePosition = null,
        float mouseWheelDelta = 0f)
    {
        return new InputSnapshot(
            current ?? [],
            previous ?? [],
            currentButtons ?? [],
            previousButtons ?? [],
            mousePosition ?? new Vector2i(0, 0),
            mouseWheelDelta);
    }

    // ── IsKeyDown ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsKeyDown_ReturnsFalse_WhenKeyNotInCurrentSet()
    {
        InputSnapshot snap = Make(current: []);

        Assert.False(snap.IsKeyDown(Keyboard.Key.Space));
    }

    [Fact]
    public void IsKeyDown_ReturnsTrue_WhenKeyInCurrentSet()
    {
        InputSnapshot snap = Make(current: [Keyboard.Key.Space]);

        Assert.True(snap.IsKeyDown(Keyboard.Key.Space));
    }

    // ── WasKeyPressed ─────────────────────────────────────────────────────────

    [Fact]
    public void WasKeyPressed_ReturnsFalse_WhenKeyNotInCurrentOrPrevious()
    {
        InputSnapshot snap = Make(current: [], previous: []);

        Assert.False(snap.WasKeyPressed(Keyboard.Key.A));
    }

    [Fact]
    public void WasKeyPressed_ReturnsTrue_WhenKeyInCurrentButNotPrevious()
    {
        InputSnapshot snap = Make(current: [Keyboard.Key.A], previous: []);

        Assert.True(snap.WasKeyPressed(Keyboard.Key.A));
    }

    [Fact]
    public void WasKeyPressed_ReturnsFalse_WhenKeyInBothCurrentAndPrevious()
    {
        InputSnapshot snap = Make(current: [Keyboard.Key.A], previous: [Keyboard.Key.A]);

        Assert.False(snap.WasKeyPressed(Keyboard.Key.A));
    }

    [Fact]
    public void WasKeyPressed_ReturnsFalse_WhenKeyOnlyInPrevious()
    {
        InputSnapshot snap = Make(current: [], previous: [Keyboard.Key.A]);

        Assert.False(snap.WasKeyPressed(Keyboard.Key.A));
    }

    // ── WasAnyKeyPressed ──────────────────────────────────────────────────────

    [Fact]
    public void WasAnyKeyPressed_ReturnsFalse_WhenNothingPressed()
    {
        InputSnapshot snap = Make(current: [], previous: []);

        Assert.False(snap.WasAnyKeyPressed());
    }

    [Fact]
    public void WasAnyKeyPressed_ReturnsTrue_WhenNewKeyAppearsThisFrame()
    {
        InputSnapshot snap = Make(current: [Keyboard.Key.B], previous: []);

        Assert.True(snap.WasAnyKeyPressed());
    }

    [Fact]
    public void WasAnyKeyPressed_ReturnsFalse_WhenKeyHeldFromPreviousFrame()
    {
        // Key was already held last frame — not a new press.
        InputSnapshot snap = Make(current: [Keyboard.Key.B], previous: [Keyboard.Key.B]);

        Assert.False(snap.WasAnyKeyPressed());
    }

    [Fact]
    public void WasAnyKeyPressed_ReturnsTrue_WhenOneNewKeyAmongHeldKeys()
    {
        // B was held, C is newly pressed.
        InputSnapshot snap = Make(
            current: [Keyboard.Key.B, Keyboard.Key.C],
            previous: [Keyboard.Key.B]);

        Assert.True(snap.WasAnyKeyPressed());
    }

    // ── WasKeyReleased ────────────────────────────────────────────────────────

    [Fact]
    public void WasKeyReleased_ReturnsFalse_WhenKeyNeverHeld()
    {
        InputSnapshot snap = Make(current: [], previous: []);

        Assert.False(snap.WasKeyReleased(Keyboard.Key.D));
    }

    [Fact]
    public void WasKeyReleased_ReturnsTrue_WhenKeyInPreviousButNotCurrent()
    {
        InputSnapshot snap = Make(current: [], previous: [Keyboard.Key.D]);

        Assert.True(snap.WasKeyReleased(Keyboard.Key.D));
    }

    [Fact]
    public void WasKeyReleased_ReturnsFalse_WhenKeyStillHeld()
    {
        InputSnapshot snap = Make(current: [Keyboard.Key.D], previous: [Keyboard.Key.D]);

        Assert.False(snap.WasKeyReleased(Keyboard.Key.D));
    }

    // ── Mouse buttons ─────────────────────────────────────────────────────────

    [Fact]
    public void IsMouseButtonDown_ReturnsFalse_WhenButtonNotPressed()
    {
        InputSnapshot snap = Make(currentButtons: []);

        Assert.False(snap.IsMouseButtonDown(Mouse.Button.Left));
    }

    [Fact]
    public void IsMouseButtonDown_ReturnsTrue_WhenButtonHeld()
    {
        InputSnapshot snap = Make(currentButtons: [Mouse.Button.Left]);

        Assert.True(snap.IsMouseButtonDown(Mouse.Button.Left));
    }

    [Fact]
    public void WasMouseButtonPressed_ReturnsTrue_WhenButtonNewThisFrame()
    {
        InputSnapshot snap = Make(currentButtons: [Mouse.Button.Left], previousButtons: []);

        Assert.True(snap.WasMouseButtonPressed(Mouse.Button.Left));
    }

    [Fact]
    public void WasMouseButtonPressed_ReturnsFalse_WhenButtonHeldFromLastFrame()
    {
        InputSnapshot snap = Make(
            currentButtons: [Mouse.Button.Left],
            previousButtons: [Mouse.Button.Left]);

        Assert.False(snap.WasMouseButtonPressed(Mouse.Button.Left));
    }

    [Fact]
    public void WasMouseButtonReleased_ReturnsTrue_WhenButtonDroppedThisFrame()
    {
        InputSnapshot snap = Make(currentButtons: [], previousButtons: [Mouse.Button.Right]);

        Assert.True(snap.WasMouseButtonReleased(Mouse.Button.Right));
    }

    [Fact]
    public void WasMouseButtonReleased_ReturnsFalse_WhenButtonStillHeld()
    {
        InputSnapshot snap = Make(
            currentButtons: [Mouse.Button.Right],
            previousButtons: [Mouse.Button.Right]);

        Assert.False(snap.WasMouseButtonReleased(Mouse.Button.Right));
    }

    // ── Mouse position / wheel ────────────────────────────────────────────────

    [Fact]
    public void MousePosition_ReflectsSuppliedValue()
    {
        InputSnapshot snap = Make(mousePosition: new Vector2i(320, 240));

        Assert.Equal(new Vector2i(320, 240), snap.MousePosition);
    }

    [Fact]
    public void MouseWheelDelta_ReflectsSuppliedValue()
    {
        InputSnapshot snap = Make(mouseWheelDelta: 1.5f);

        Assert.Equal(1.5f, snap.MouseWheelDelta);
    }

    // ── Empty singleton ───────────────────────────────────────────────────────

    [Fact]
    public void Empty_HasNoKeysOrButtons()
    {
        InputSnapshot snap = InputSnapshot.Empty;

        Assert.Empty(snap.CurrentKeys);
        Assert.Empty(snap.PreviousKeys);
        Assert.Empty(snap.CurrentButtons);
        Assert.Empty(snap.PreviousButtons);
    }

    // ── Joystick buttons ──────────────────────────────────────────────────────

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

    [Fact]
    public void IsJoystickButtonDown_ReturnsTrue_WhenButtonHeld()
    {
        InputSnapshot snap = MakeJoystick(
            currentButtons: new Dictionary<uint, HashSet<uint>> { [0] = [1] });

        Assert.True(snap.IsJoystickButtonDown(0, 1));
    }

    [Fact]
    public void WasJoystickButtonPressed_ReturnsTrue_OnFirstFrame()
    {
        InputSnapshot snap = MakeJoystick(
            currentButtons: new Dictionary<uint, HashSet<uint>> { [0] = [2] },
            previousButtons: new Dictionary<uint, HashSet<uint>> { [0] = [] });

        Assert.True(snap.WasJoystickButtonPressed(0, 2));
    }

    [Fact]
    public void GetJoystickAxis_ReturnsValue_WhenAxisMoved()
    {
        InputSnapshot snap = MakeJoystick(
            axes: new Dictionary<uint, Dictionary<Joystick.Axis, float>>
            {
                [0] = new() { [Joystick.Axis.X] = 75f }
            });

        Assert.Equal(75f, snap.GetJoystickAxis(0, Joystick.Axis.X));
    }

    [Fact]
    public void GetJoystickAxis_ReturnsZero_WhenNoData()
    {
        InputSnapshot snap = MakeJoystick();

        Assert.Equal(0f, snap.GetJoystickAxis(0, Joystick.Axis.X));
    }

    [Fact]
    public void IsJoystickConnected_ReturnsTrue_WhenConnected()
    {
        InputSnapshot snap = MakeJoystick(connected: [0, 1]);

        Assert.True(snap.IsJoystickConnected(0));
        Assert.True(snap.IsJoystickConnected(1));
        Assert.False(snap.IsJoystickConnected(2));
    }
}
