using GrayHare.GameEngine.Rendering;
using SFML.System;

namespace GrayHare.GameEngine.Tests.Rendering;

public sealed class Camera2DTests
{
    private static Camera2D CreateDefault() => new(new Vector2u(800, 600));

    [Fact]
    public void Constructor_CentersPositionOnViewport()
    {
        var camera = CreateDefault();

        Assert.Equal(new Vector2f(400f, 300f), camera.Position);
    }

    [Fact]
    public void Follow_MovesTowardTarget()
    {
        var camera = CreateDefault();
        var target = new Vector2f(600f, 500f);

        camera.Follow(target, lerpSpeed: 5f, deltaTime: 0.1f);

        Assert.True(camera.Position.X > 400f, "Camera should move toward target X.");
        Assert.True(camera.Position.Y > 300f, "Camera should move toward target Y.");
        Assert.True(camera.Position.X < 600f, "Camera should not reach target X in one step.");
        Assert.True(camera.Position.Y < 500f, "Camera should not reach target Y in one step.");
    }

    [Fact]
    public void Follow_DoesNotOvershoot()
    {
        var camera = CreateDefault();
        var target = new Vector2f(600f, 500f);

        // Very high lerp speed × deltaTime should clamp factor to 1.
        camera.Follow(target, lerpSpeed: 1000f, deltaTime: 10f);

        Assert.Equal(target.X, camera.Position.X, 0.001f);
        Assert.Equal(target.Y, camera.Position.Y, 0.001f);
    }

    [Fact]
    public void Shake_ProducesNonZeroOffset_DuringDuration()
    {
        var camera = CreateDefault();
        camera.Shake(intensity: 10f, duration: 1f);

        // Run multiple updates to get at least one non-zero offset (random-based).
        bool anyNonZero = false;
        for (int i = 0; i < 50; i++)
        {
            camera.UpdateShake(0.01f);
            var view = camera.GetView();
            if (view.Center != camera.Position)
            {
                anyNonZero = true;
                break;
            }
        }

        Assert.True(anyNonZero, "Shake should produce a non-zero offset during its duration.");
    }

    [Fact]
    public void Shake_DecaysToZero_AfterDuration()
    {
        var camera = CreateDefault();
        camera.Shake(intensity: 10f, duration: 0.1f);

        // Advance well past the shake duration.
        camera.UpdateShake(1f);

        var view = camera.GetView();
        Assert.Equal(camera.Position.X, view.Center.X, 0.001f);
        Assert.Equal(camera.Position.Y, view.Center.Y, 0.001f);
    }

    [Fact]
    public void Zoom_DefaultIsOne()
    {
        var camera = CreateDefault();

        Assert.Equal(1f, camera.Zoom);
    }

    [Fact]
    public void GetView_ReflectsPosition()
    {
        var camera = CreateDefault();
        camera.Position = new Vector2f(100f, 200f);

        var view = camera.GetView();

        Assert.Equal(100f, view.Center.X, 0.001f);
        Assert.Equal(200f, view.Center.Y, 0.001f);
    }

    [Fact]
    public void GetView_ReflectsZoom()
    {
        var camera = CreateDefault();

        camera.Zoom = 2f;
        var zoomedIn = camera.GetView();

        camera.Zoom = 0.5f;
        var zoomedOut = camera.GetView();

        // Higher zoom = smaller view size (zoomed in).
        Assert.True(zoomedIn.Size.X < zoomedOut.Size.X,
            "Higher zoom should produce a smaller view.");
    }

    [Fact]
    public void Reset_RestoresDefaults()
    {
        var camera = CreateDefault();
        camera.Position = new Vector2f(999f, 999f);
        camera.Zoom = 3f;
        camera.Rotation = 45f;
        camera.Shake(10f, 1f);

        camera.Reset();

        Assert.Equal(new Vector2f(400f, 300f), camera.Position);
        Assert.Equal(1f, camera.Zoom);
        Assert.Equal(0f, camera.Rotation);

        // Shake offset should be zero after reset.
        var view = camera.GetView();
        Assert.Equal(camera.Position.X, view.Center.X, 0.001f);
        Assert.Equal(camera.Position.Y, view.Center.Y, 0.001f);
    }

    // ── ScreenToWorld / WorldToScreen ─────────────────────────────────────────

    [Fact]
    public void ScreenToWorld_CenterPixel_ReturnsPositionNoRotation()
    {
        var camera = CreateDefault();
        // Viewport center (400, 300) should map exactly to the camera position.
        Vector2f world = camera.ScreenToWorld(new Vector2i(400, 300));

        Assert.Equal(camera.Position.X, world.X, 0.01f);
        Assert.Equal(camera.Position.Y, world.Y, 0.01f);
    }

    [Fact]
    public void ScreenToWorld_NoRotation_OffsetMatchesPixelDelta()
    {
        var camera = CreateDefault();
        // Pixel (500, 350) is (+100, +50) from the 800×600 viewport center.
        Vector2f world = camera.ScreenToWorld(new Vector2i(500, 350));

        Assert.Equal(camera.Position.X + 100f, world.X, 0.01f);
        Assert.Equal(camera.Position.Y + 50f, world.Y, 0.01f);
    }

    [Fact]
    public void WorldToScreen_CameraPosition_ReturnsViewportCenter()
    {
        var camera = CreateDefault();
        Vector2i screen = camera.WorldToScreen(camera.Position);

        Assert.Equal(400, screen.X);
        Assert.Equal(300, screen.Y);
    }

    [Fact]
    public void WorldToScreen_NoRotation_OffsetMatchesWorldDelta()
    {
        var camera = CreateDefault();
        Vector2i screen = camera.WorldToScreen(new Vector2f(camera.Position.X + 100f, camera.Position.Y + 50f));

        Assert.Equal(500, screen.X);
        Assert.Equal(350, screen.Y);
    }

    [Fact]
    public void ScreenToWorld_AndWorldToScreen_RoundTrip()
    {
        var camera = CreateDefault();
        camera.Position = new Vector2f(200f, 150f);
        camera.Zoom = 2f;
        camera.Rotation = 30f;

        var original = new Vector2i(350, 275);
        Vector2f world = camera.ScreenToWorld(original);
        Vector2i roundTrip = camera.WorldToScreen(world);

        Assert.Equal(original.X, roundTrip.X);
        Assert.Equal(original.Y, roundTrip.Y);
    }
}
