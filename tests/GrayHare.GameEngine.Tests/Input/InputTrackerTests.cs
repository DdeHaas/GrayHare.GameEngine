using GrayHare.GameEngine.Input;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.Tests.Input;

public sealed class InputTrackerTests
{
    [Fact]
    public void Current_IsNotNull_AfterConstruction()
    {
        var tracker = new InputTracker();

        Assert.NotNull(tracker.Current);
    }

    [Fact]
    public void OnKeyPressed_MakesKeyDown()
    {
        var tracker = new InputTracker();

        tracker.OnKeyPressed(Keyboard.Key.Space);

        Assert.True(tracker.Current.IsKeyDown(Keyboard.Key.Space));
    }

    [Fact]
    public void OnKeyReleased_RemovesKeyFromDown()
    {
        var tracker = new InputTracker();
        tracker.OnKeyPressed(Keyboard.Key.Space);

        tracker.OnKeyReleased(Keyboard.Key.Space);

        Assert.False(tracker.Current.IsKeyDown(Keyboard.Key.Space));
    }

    [Fact]
    public void BeginFrame_KeyHeldPreviousFrame_IsNotWasKeyPressed()
    {
        var tracker = new InputTracker();
        tracker.OnKeyPressed(Keyboard.Key.A);

        // After BeginFrame, A is in both current and previous, so it is NOT just pressed.
        tracker.BeginFrame();

        Assert.False(tracker.Current.WasKeyPressed(Keyboard.Key.A));
    }

    [Fact]
    public void BeginFrame_KeyReleasedAfterBeginFrame_IsWasKeyReleased()
    {
        var tracker = new InputTracker();
        tracker.OnKeyPressed(Keyboard.Key.B);
        tracker.BeginFrame();

        tracker.OnKeyReleased(Keyboard.Key.B);

        Assert.True(tracker.Current.WasKeyReleased(Keyboard.Key.B));
    }

    [Fact]
    public void OnMouseButtonPressed_MakesButtonDown()
    {
        var tracker = new InputTracker();

        tracker.OnMouseButtonPressed(Mouse.Button.Left, new Vector2i(10, 20));

        Assert.True(tracker.Current.IsMouseButtonDown(Mouse.Button.Left));
    }

    [Fact]
    public void OnMouseButtonReleased_RemovesButtonFromDown()
    {
        var tracker = new InputTracker();
        tracker.OnMouseButtonPressed(Mouse.Button.Left, new Vector2i(0, 0));

        tracker.OnMouseButtonReleased(Mouse.Button.Left, new Vector2i(0, 0));

        Assert.False(tracker.Current.IsMouseButtonDown(Mouse.Button.Left));
    }

    [Fact]
    public void OnMouseMoved_UpdatesMousePosition()
    {
        var tracker = new InputTracker();

        tracker.OnMouseMoved(new Vector2i(50, 75));

        Assert.Equal(new Vector2i(50, 75), tracker.Current.MousePosition);
    }

    [Fact]
    public void OnMouseWheelScrolled_AccumulatesDelta()
    {
        var tracker = new InputTracker();

        tracker.OnMouseWheelScrolled(1.5f, new Vector2i(0, 0));
        tracker.OnMouseWheelScrolled(0.5f, new Vector2i(0, 0));

        Assert.Equal(2f, tracker.Current.MouseWheelDelta);
    }

    [Fact]
    public void BeginFrame_ResetMouseWheelDelta()
    {
        var tracker = new InputTracker();
        tracker.OnMouseWheelScrolled(3f, new Vector2i(0, 0));

        tracker.BeginFrame();

        Assert.Equal(0f, tracker.Current.MouseWheelDelta);
    }
}
