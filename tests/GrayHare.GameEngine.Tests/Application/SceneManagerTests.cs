using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Ecs;
using GrayHare.GameEngine.Scenes;
using SFML.Graphics;
using System.Runtime.CompilerServices;

namespace GrayHare.GameEngine.Tests.Application;

/// <summary>
/// Minimal scene stub that ignores <see cref="GameHost"/> to allow unit testing
/// of <see cref="SceneManager"/> without a real SFML window.
/// </summary>
internal sealed class FakeScene : GameSceneBase
{
    public bool LoadCalled { get; private set; }
    public bool UnloadCalled { get; private set; }
    public int ActivatedCallCount { get; private set; }
    public bool ActivatedCalled => ActivatedCallCount > 0;
    public bool DeactivatedCalled { get; private set; }
    public bool DisposeCalled { get; private set; }

    public override void Load(GameHost host)
    {
        LoadCalled = true;
        // Do NOT call base.Load(host) to avoid iterating layers with a fake host.
    }

    public override void Unload(GameHost host)
    {
        UnloadCalled = true;
        // Do NOT call base.Unload(host) to avoid iterating layers with a fake host.
    }

    public override void OnActivated(GameHost host) => ActivatedCallCount++;

    public override void OnDeactivated(GameHost host) => DeactivatedCalled = true;

    protected override void Dispose(bool disposing)
    {
        DisposeCalled = true;
        base.Dispose(disposing);
    }

    public override void RenderLayer(GameHost host, RenderWindow window) { }
}

public sealed class SceneManagerTests
{
    /// <summary>
    /// Creates an uninitialized <see cref="GameHost"/> without calling its constructor,
    /// bypassing the SFML window dependency while still satisfying ArgumentNullException checks.
    /// </summary>
    private static GameHost CreateFakeHost()
        => (GameHost)RuntimeHelpers.GetUninitializedObject(typeof(GameHost));

    [Fact]
    public void CurrentScene_IsNull_BeforeInitialize()
    {
        var manager = new SceneManager(new World());

        Assert.Null(manager.CurrentScene);
    }

    [Fact]
    public void Initialize_SetsCurrentScene()
    {
        var manager = new SceneManager(new World());
        var scene = new FakeScene();
        GameHost host = CreateFakeHost();

        manager.Initialize(host, scene);

        Assert.Same(scene, manager.CurrentScene);
    }

    [Fact]
    public void Initialize_CallsSceneLoad()
    {
        var manager = new SceneManager(new World());
        var scene = new FakeScene();
        GameHost host = CreateFakeHost();

        manager.Initialize(host, scene);

        Assert.True(scene.LoadCalled);
    }

    [Fact]
    public void Queue_DoesNotChangeCurrentSceneImmediately()
    {
        var manager = new SceneManager(new World());
        var pending = new FakeScene();

        manager.Queue(pending);

        Assert.Null(manager.CurrentScene);
    }

    [Fact]
    public void ApplyPending_WithNoPendingScene_IsNoOp()
    {
        var world = new World();
        var manager = new SceneManager(world);

        // No pending scene; host is unused because method returns early.
        manager.ApplyPending(null!);

        Assert.Null(manager.CurrentScene);
    }

    [Fact]
    public void Queue_AndApplyPending_TransitionsToNewScene()
    {
        var manager = new SceneManager(new World());
        var next = new FakeScene();
        GameHost host = CreateFakeHost();

        manager.Queue(next);
        manager.ApplyPending(host);

        Assert.Same(next, manager.CurrentScene);
    }

    [Fact]
    public void ApplyPending_UnloadsCurrentScene_BeforeLoadingNext()
    {
        var manager = new SceneManager(new World());
        var first = new FakeScene();
        var second = new FakeScene();
        GameHost host = CreateFakeHost();

        manager.Initialize(host, first);
        manager.Queue(second);
        manager.ApplyPending(host);

        Assert.True(first.UnloadCalled);
        Assert.True(second.LoadCalled);
    }

    // ── Scene stack: Initialize lifecycle ─────────────────────────────────────

    [Fact]
    public void Initialize_CallsOnActivated()
    {
        var manager = new SceneManager(new World());
        var scene = new FakeScene();
        GameHost host = CreateFakeHost();

        manager.Initialize(host, scene);

        Assert.True(scene.ActivatedCalled);
    }

    // ── Scene stack: depth tracking ───────────────────────────────────────────

    [Fact]
    public void SceneStackDepth_IsZero_BeforeInitialize()
    {
        var manager = new SceneManager(new World());

        Assert.Equal(0, manager.SceneStackDepth);
    }

    [Fact]
    public void SceneStackDepth_IsOne_AfterInitialize()
    {
        var manager = new SceneManager(new World());
        GameHost host = CreateFakeHost();

        manager.Initialize(host, new FakeScene());

        Assert.Equal(1, manager.SceneStackDepth);
    }

    // ── Scene stack: push ─────────────────────────────────────────────────────

    [Fact]
    public void QueuePush_AndApplyPending_IncreasesStackDepth()
    {
        var manager = new SceneManager(new World());
        GameHost host = CreateFakeHost();
        manager.Initialize(host, new FakeScene());

        manager.QueuePush(new FakeScene());
        manager.ApplyPending(host);

        Assert.Equal(2, manager.SceneStackDepth);
    }

    [Fact]
    public void QueuePush_CallsOnDeactivated_OnPreviousScene()
    {
        var manager = new SceneManager(new World());
        var first = new FakeScene();
        GameHost host = CreateFakeHost();
        manager.Initialize(host, first);

        manager.QueuePush(new FakeScene());
        manager.ApplyPending(host);

        Assert.True(first.DeactivatedCalled);
    }

    [Fact]
    public void QueuePush_CallsLoad_AndOnActivated_OnOverlay()
    {
        var manager = new SceneManager(new World());
        GameHost host = CreateFakeHost();
        manager.Initialize(host, new FakeScene());
        var overlay = new FakeScene();

        manager.QueuePush(overlay);
        manager.ApplyPending(host);

        Assert.True(overlay.LoadCalled);
        Assert.True(overlay.ActivatedCalled);
    }

    // ── Scene stack: pop ──────────────────────────────────────────────────────

    [Fact]
    public void QueuePop_AndApplyPending_DecreasesStackDepth()
    {
        var manager = new SceneManager(new World());
        GameHost host = CreateFakeHost();
        manager.Initialize(host, new FakeScene());
        manager.QueuePush(new FakeScene());
        manager.ApplyPending(host);

        manager.QueuePop();
        manager.ApplyPending(host);

        Assert.Equal(1, manager.SceneStackDepth);
    }

    [Fact]
    public void QueuePop_CallsUnload_AndDispose_OnPoppedScene()
    {
        var manager = new SceneManager(new World());
        GameHost host = CreateFakeHost();
        manager.Initialize(host, new FakeScene());
        var overlay = new FakeScene();
        manager.QueuePush(overlay);
        manager.ApplyPending(host);

        manager.QueuePop();
        manager.ApplyPending(host);

        Assert.True(overlay.UnloadCalled);
        Assert.True(overlay.DisposeCalled);
    }

    [Fact]
    public void QueuePop_CallsOnActivated_OnSceneBeneath()
    {
        var manager = new SceneManager(new World());
        var root = new FakeScene();
        GameHost host = CreateFakeHost();
        manager.Initialize(host, root);
        manager.QueuePush(new FakeScene());
        manager.ApplyPending(host);

        int countBeforePop = root.ActivatedCallCount;

        manager.QueuePop();
        manager.ApplyPending(host);

        Assert.Equal(countBeforePop + 1, root.ActivatedCallCount);
    }

    [Fact]
    public void QueuePop_WithSingleScene_ThrowsInvalidOperationException()
    {
        var manager = new SceneManager(new World());
        GameHost host = CreateFakeHost();
        manager.Initialize(host, new FakeScene());

        manager.QueuePop();

        Assert.Throws<InvalidOperationException>(() => manager.ApplyPending(host));
    }

    // ── Scene stack: change ───────────────────────────────────────────────────

    [Fact]
    public void Queue_Change_ClearsEntireStack()
    {
        var manager = new SceneManager(new World());
        GameHost host = CreateFakeHost();
        var root = new FakeScene();
        var overlay = new FakeScene();
        manager.Initialize(host, root);
        manager.QueuePush(overlay);
        manager.ApplyPending(host);

        manager.Queue(new FakeScene());
        manager.ApplyPending(host);

        Assert.Equal(1, manager.SceneStackDepth);
        Assert.True(root.DisposeCalled);
        Assert.True(overlay.DisposeCalled);
    }

    [Fact]
    public void Queue_Change_ClearsWorld()
    {
        var world = new World();
        var manager = new SceneManager(world);
        GameHost host = CreateFakeHost();
        manager.Initialize(host, new FakeScene());

        var entity = world.CreateEntity();
        world.AddComponent(entity, 42);

        manager.Queue(new FakeScene());
        manager.ApplyPending(host);

        Assert.Equal(0, world.EntityCount);
    }
}
