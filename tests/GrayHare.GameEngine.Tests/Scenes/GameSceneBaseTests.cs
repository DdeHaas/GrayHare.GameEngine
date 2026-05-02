using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Scenes;
using SFML.Graphics;
using System.Runtime.CompilerServices;

namespace GrayHare.GameEngine.Tests.Scenes;

internal sealed class FakeLayer : ISceneLayer
{
    public int RenderOrder { get; }
    public bool LoadCalled { get; private set; }
    public bool UnloadCalled { get; private set; }
    public int UnloadCallOrder { get; private set; }
    public int RenderCallOrder { get; private set; }
    public int ActivatedCount { get; private set; }
    public int DeactivatedCount { get; private set; }

    private static int _globalRenderCounter;
    private static int _globalUnloadCounter;

    public FakeLayer(int renderOrder)
    {
        RenderOrder = renderOrder;
    }

    public static void ResetCounters()
    {
        _globalRenderCounter = 0;
        _globalUnloadCounter = 0;
    }

    public void Load(GameHost host) => LoadCalled = true;

    public void Unload(GameHost host)
    {
        UnloadCalled = true;
        UnloadCallOrder = ++_globalUnloadCounter;
    }

    public void Update(GameHost host, in GameTime gameTime) { }

    public void RenderLayer(GameHost host, RenderWindow window) =>
        RenderCallOrder = ++_globalRenderCounter;

    public void OnActivated(GameHost host) => ActivatedCount++;

    public void OnDeactivated(GameHost host) => DeactivatedCount++;
}

internal sealed class TestableScene : GameSceneBase
{
    public new void AddLayer(ISceneLayer layer) => base.AddLayer(layer);

    public new bool RemoveLayer(ISceneLayer layer) => base.RemoveLayer(layer);

    public override void RenderLayer(GameHost host, RenderWindow window) { }
}

internal sealed class RenderOrderTrackingLayer : ISceneLayer
{
    public int RenderOrder { get; }
    public int CallOrder { get; private set; }

    public RenderOrderTrackingLayer(int renderOrder)
    {
        RenderOrder = renderOrder;
    }

    public void Load(GameHost host) { }
    public void Unload(GameHost host) { }
    public void Update(GameHost host, in GameTime gameTime) { }

    public void RenderLayer(GameHost host, RenderWindow window)
    {
        CallOrder = ++RenderOrderTrackingScene.GlobalCounter;
    }
}

internal sealed class RenderOrderTrackingScene : GameSceneBase
{
    public static int GlobalCounter;
    public int SceneCallOrder { get; private set; }

    public static void ResetGlobalCounter() => GlobalCounter = 0;

    public void AddLayerPublic(ISceneLayer layer) => AddLayer(layer);

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        SceneCallOrder = ++GlobalCounter;
    }
}

public sealed class GameSceneBaseTests
{
    private static GameHost CreateFakeHost()
        => (GameHost)RuntimeHelpers.GetUninitializedObject(typeof(GameHost));

    [Fact]
    public void AddLayer_SortsLayersByRenderOrder()
    {
        var scene = new TestableScene();
        var high = new FakeLayer(10);
        var low = new FakeLayer(-5);
        var mid = new FakeLayer(0);

        scene.AddLayer(high);
        scene.AddLayer(low);
        scene.AddLayer(mid);

        FakeLayer.ResetCounters();
        scene.Render(CreateFakeHost(), null!);

        Assert.True(low.RenderCallOrder < mid.RenderCallOrder);
        Assert.True(mid.RenderCallOrder < high.RenderCallOrder);
    }

    [Fact]
    public void AddLayer_PreservesInsertionOrder_ForEqualRenderOrder()
    {
        var scene = new TestableScene();
        var first = new FakeLayer(0);
        var second = new FakeLayer(0);

        scene.AddLayer(first);
        scene.AddLayer(second);

        FakeLayer.ResetCounters();
        scene.Render(CreateFakeHost(), null!);

        Assert.True(first.RenderCallOrder < second.RenderCallOrder);
    }

    [Fact]
    public void RemoveLayer_ReturnsTrue_WhenLayerExists()
    {
        var scene = new TestableScene();
        var layer = new FakeLayer(0);
        scene.AddLayer(layer);

        Assert.True(scene.RemoveLayer(layer));
    }

    [Fact]
    public void RemoveLayer_ReturnsFalse_WhenLayerNotFound()
    {
        var scene = new TestableScene();
        var layer = new FakeLayer(0);

        Assert.False(scene.RemoveLayer(layer));
    }

    [Fact]
    public void Load_PropagatesTo_AllLayers()
    {
        var scene = new TestableScene();
        var a = new FakeLayer(-1);
        var b = new FakeLayer(1);
        scene.AddLayer(a);
        scene.AddLayer(b);

        scene.Load(CreateFakeHost());

        Assert.True(a.LoadCalled);
        Assert.True(b.LoadCalled);
    }

    [Fact]
    public void Unload_PropagatesTo_AllLayers_InReverseOrder()
    {
        var scene = new TestableScene();
        var first = new FakeLayer(-1);
        var second = new FakeLayer(1);
        scene.AddLayer(first);
        scene.AddLayer(second);
        FakeLayer.ResetCounters();

        scene.Unload(CreateFakeHost());

        Assert.True(first.UnloadCalled);
        Assert.True(second.UnloadCalled);
        Assert.True(second.UnloadCallOrder < first.UnloadCallOrder,
            "Layers should be unloaded in reverse order.");
    }

    [Fact]
    public void Render_CallsNegativeLayers_ThenScene_ThenPositiveLayers()
    {
        var scene = new RenderOrderTrackingScene();
        var background = new RenderOrderTrackingLayer(-1);
        var overlay = new RenderOrderTrackingLayer(1);
        scene.AddLayerPublic(background);
        scene.AddLayerPublic(overlay);

        RenderOrderTrackingScene.ResetGlobalCounter();
        scene.Render(CreateFakeHost(), null!);

        Assert.True(background.CallOrder < scene.SceneCallOrder,
            "Negative layer should render before the scene.");
        Assert.True(scene.SceneCallOrder < overlay.CallOrder,
            "Scene should render before positive layer.");
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var scene = new TestableScene();

        scene.Dispose();
        var ex = Record.Exception(() => scene.Dispose());

        Assert.Null(ex);
    }

    // ── Layer lifecycle forwarding ────────────────────────────────────────────

    [Fact]
    public void ActivateInternal_ForwardsToLayers()
    {
        var scene = new TestableScene();
        var layer = new FakeLayer(0);
        scene.AddLayer(layer);

        scene.ActivateInternal(CreateFakeHost());

        Assert.Equal(1, layer.ActivatedCount);
    }

    [Fact]
    public void DeactivateInternal_ForwardsToLayers()
    {
        var scene = new TestableScene();
        var layer = new FakeLayer(0);
        scene.AddLayer(layer);

        scene.DeactivateInternal(CreateFakeHost());

        Assert.Equal(1, layer.DeactivatedCount);
    }

    [Fact]
    public void ActivateInternal_ForwardsToAllLayers()
    {
        var scene = new TestableScene();
        var a = new FakeLayer(-1);
        var b = new FakeLayer(1);
        scene.AddLayer(a);
        scene.AddLayer(b);

        scene.ActivateInternal(CreateFakeHost());

        Assert.Equal(1, a.ActivatedCount);
        Assert.Equal(1, b.ActivatedCount);
    }

    [Fact]
    public void DeactivateInternal_ForwardsToAllLayers()
    {
        var scene = new TestableScene();
        var a = new FakeLayer(-1);
        var b = new FakeLayer(1);
        scene.AddLayer(a);
        scene.AddLayer(b);

        scene.DeactivateInternal(CreateFakeHost());

        Assert.Equal(1, a.DeactivatedCount);
        Assert.Equal(1, b.DeactivatedCount);
    }
}
