using GrayHare.GameEngine.Ecs;

namespace GrayHare.GameEngine.Tests.Ecs;

public sealed class WorldTests
{
    [Fact]
    public void Query_ReturnsEntitiesContainingBothRequestedComponents()
    {
        var world = new World();
        var movingEntity = world.CreateEntity();
        var staticEntity = world.CreateEntity();

        world.AddComponent(movingEntity, new Position(5f, 10f));
        world.AddComponent(movingEntity, new Velocity(1f, 2f));
        world.AddComponent(staticEntity, new Position(20f, 30f));

        Entity[] results = world.Query<Position, Velocity>().ToArray();

        Assert.Single(results);
        Assert.Equal(movingEntity, results[0]);
    }

    [Fact]
    public void DestroyEntity_RemovesItsComponents()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new Position(1f, 2f));

        world.DestroyEntity(entity);

        Assert.False(world.Exists(entity));
        Assert.False(world.TryGetComponent(entity, out Position _));
    }

    [Fact]
    public void Clear_RemovesAllEntitiesAndResetsIds()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new Position(3f, 4f));

        world.Clear();

        Assert.False(world.Exists(entity));
        Assert.Empty(world.Query<Position>());
    }

    [Fact]
    public void GetComponent_ThrowsWhenComponentAbsent()
    {
        var world = new World();
        var entity = world.CreateEntity();

        Assert.Throws<KeyNotFoundException>(() => world.GetComponent<Position>(entity));
    }

    [Fact]
    public void AddComponent_OverwritesExistingComponent()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new Position(1f, 2f));
        world.AddComponent(entity, new Position(9f, 8f));

        var pos = world.GetComponent<Position>(entity);

        Assert.Equal(9f, pos.X);
    }

    // ── Query<TA,TB,TC> ───────────────────────────────────────────────────────

    [Fact]
    public void Query3_ReturnsOnlyEntitiesWithAllThreeComponents()
    {
        var world = new World();
        var full = world.CreateEntity();
        var missing = world.CreateEntity();

        world.AddComponent(full, new Position(1f, 1f));
        world.AddComponent(full, new Velocity(1f, 0f));
        world.AddComponent(full, new Tag(99));

        world.AddComponent(missing, new Position(2f, 2f));
        world.AddComponent(missing, new Velocity(0f, 1f));
        // missing does NOT have Tag

        Entity[] results = world.Query<Position, Velocity, Tag>().ToArray();

        Assert.Single(results);
        Assert.Equal(full, results[0]);
    }

    [Fact]
    public void Query3_ReturnsEmpty_WhenNoEntitiesHaveAllThreeComponents()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.AddComponent(e, new Position(1f, 1f));
        world.AddComponent(e, new Velocity(1f, 0f));
        // No Tag component

        Assert.Empty(world.Query<Position, Velocity, Tag>());
    }

    [Fact]
    public void Query3_ReturnsEmpty_WhenComponentStoreDoesNotExist()
    {
        var world = new World();

        Assert.Empty(world.Query<Position, Velocity, Tag>());
    }

    [Fact]
    public void Query3_ReturnsMultipleMatchingEntities()
    {
        var world = new World();

        for (int i = 0; i < 5; i++)
        {
            var e = world.CreateEntity();
            world.AddComponent(e, new Position(i, i));
            world.AddComponent(e, new Velocity(i, 0f));
            world.AddComponent(e, new Tag(i));
        }

        Assert.Equal(5, world.Query<Position, Velocity, Tag>().Count());
    }

    [Fact]
    public void Query3_ExcludesDestroyedEntities()
    {
        var world = new World();
        var alive = world.CreateEntity();
        var dead = world.CreateEntity();

        foreach (Entity e in new[] { alive, dead })
        {
            world.AddComponent(e, new Position(0f, 0f));
            world.AddComponent(e, new Velocity(0f, 0f));
            world.AddComponent(e, new Tag(0));
        }

        world.DestroyEntity(dead);

        Entity[] results = world.Query<Position, Velocity, Tag>().ToArray();

        Assert.Single(results);
        Assert.Equal(alive, results[0]);
    }

    private readonly record struct Position(float X, float Y);
    private readonly record struct Velocity(float X, float Y);
    private readonly record struct Tag(int Value);

    // ── CreateEntity ──────────────────────────────────────────────────────────

    [Fact]
    public void CreateEntity_AssignsUniqueIds()
    {
        var world = new World();

        Entity a = world.CreateEntity();
        Entity b = world.CreateEntity();

        Assert.NotEqual(a, b);
    }

    // ── DestroyEntity ─────────────────────────────────────────────────────────

    [Fact]
    public void DestroyEntity_WithNonExistentEntity_DoesNotThrow()
    {
        var world = new World();
        var ghost = new Entity(999);

        Assert.Null(Record.Exception(() => world.DestroyEntity(ghost)));
    }

    // ── RemoveComponent ───────────────────────────────────────────────────────

    [Fact]
    public void RemoveComponent_ReturnsTrue_WhenComponentPresent()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new Position(1f, 2f));

        bool removed = world.RemoveComponent<Position>(entity);

        Assert.True(removed);
    }

    [Fact]
    public void RemoveComponent_ReturnsFalse_WhenComponentAbsent()
    {
        var world = new World();
        var entity = world.CreateEntity();

        bool removed = world.RemoveComponent<Position>(entity);

        Assert.False(removed);
    }

    // ── TryGetComponent ───────────────────────────────────────────────────────

    [Fact]
    public void TryGetComponent_ReturnsFalse_WhenNoStoreExists()
    {
        var world = new World();
        var entity = world.CreateEntity();

        bool found = world.TryGetComponent(entity, out Position _);

        Assert.False(found);
    }

    // ── AddComponent ──────────────────────────────────────────────────────────

    [Fact]
    public void AddComponent_ThrowsInvalidOperationException_WhenEntityDoesNotExist()
    {
        var world = new World();
        var ghost = new Entity(999);

        Assert.Throws<InvalidOperationException>(
            () => world.AddComponent(ghost, new Position(0f, 0f)));
    }

    // ── Query<TComponent> ─────────────────────────────────────────────────────

    [Fact]
    public void Query_ReturnsEmpty_WhenNoStoreExists()
    {
        var world = new World();

        Assert.Empty(world.Query<Position>());
    }

    // ── HasComponent ──────────────────────────────────────────────────────────

    [Fact]
    public void HasComponent_ReturnsTrue_WhenComponentExists()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new Position(1f, 2f));

        Assert.True(world.HasComponent<Position>(entity));
    }

    [Fact]
    public void HasComponent_ReturnsFalse_WhenComponentMissing()
    {
        var world = new World();
        var entity = world.CreateEntity();

        Assert.False(world.HasComponent<Position>(entity));
    }

    // ── EntityCount / ComponentTypeCount / ComponentCount ──────────────────────

    [Fact]
    public void EntityCount_ReflectsLiveEntities()
    {
        var world = new World();
        var a = world.CreateEntity();
        world.CreateEntity();
        world.CreateEntity();

        Assert.Equal(3, world.EntityCount);

        world.DestroyEntity(a);

        Assert.Equal(2, world.EntityCount);
    }

    [Fact]
    public void ComponentTypeCount_ReflectsRegisteredTypes()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new Position(0f, 0f));
        world.AddComponent(entity, new Velocity(0f, 0f));

        Assert.Equal(2, world.ComponentTypeCount);
    }

    [Fact]
    public void ComponentCount_ReturnsCorrectCount()
    {
        var world = new World();
        var a = world.CreateEntity();
        var b = world.CreateEntity();
        world.CreateEntity(); // no Position
        world.AddComponent(a, new Position(0f, 0f));
        world.AddComponent(b, new Position(1f, 1f));

        Assert.Equal(2, world.ComponentCount<Position>());
    }

    // ── ForEach ───────────────────────────────────────────────────────────────

    [Fact]
    public void ForEach_SingleComponent_IteratesAllMatching()
    {
        var world = new World();
        var a = world.CreateEntity();
        var b = world.CreateEntity();
        world.AddComponent(a, new Position(1f, 0f));
        world.AddComponent(b, new Position(2f, 0f));

        List<float> xValues = [];
        world.ForEach<Position>((_, pos) => xValues.Add(pos.X));

        Assert.Equal(2, xValues.Count);
        Assert.Contains(1f, xValues);
        Assert.Contains(2f, xValues);
    }

    [Fact]
    public void ForEach_TwoComponents_IteratesIntersection()
    {
        var world = new World();
        var both = world.CreateEntity();
        var posOnly = world.CreateEntity();
        world.AddComponent(both, new Position(1f, 0f));
        world.AddComponent(both, new Velocity(2f, 0f));
        world.AddComponent(posOnly, new Position(3f, 0f));

        List<Entity> visited = [];
        world.ForEach<Position, Velocity>((e, _, _) => visited.Add(e));

        Assert.Single(visited);
        Assert.Equal(both, visited[0]);
    }

    [Fact]
    public void ForEach_ThreeComponents_IteratesIntersection()
    {
        var world = new World();
        var full = world.CreateEntity();
        var partial = world.CreateEntity();
        world.AddComponent(full, new Position(0f, 0f));
        world.AddComponent(full, new Velocity(0f, 0f));
        world.AddComponent(full, new Tag(1));
        world.AddComponent(partial, new Position(0f, 0f));
        world.AddComponent(partial, new Velocity(0f, 0f));

        List<Entity> visited = [];
        world.ForEach<Position, Velocity, Tag>((e, _, _, _) => visited.Add(e));

        Assert.Single(visited);
        Assert.Equal(full, visited[0]);
    }

    // ── Entity ID reuse and generation ────────────────────────────────────────

    [Fact]
    public void CreateEntity_ReusesDestroyedId()
    {
        var world = new World();
        Entity first = world.CreateEntity();
        int originalId = first.Id;
        int originalGen = first.Generation;

        world.DestroyEntity(first);
        Entity reused = world.CreateEntity();

        Assert.Equal(originalId, reused.Id);
        Assert.True(reused.Generation > originalGen,
            "Reused entity should have a higher generation.");
    }

    [Fact]
    public void Exists_ReturnsFalse_ForStaleEntity()
    {
        var world = new World();
        Entity original = world.CreateEntity();
        world.DestroyEntity(original);

        // original still has the old generation — it's now stale.
        Assert.False(world.Exists(original));
    }

    [Fact]
    public void AddComponent_ThrowsForStaleEntity()
    {
        var world = new World();
        Entity original = world.CreateEntity();
        world.DestroyEntity(original);

        Assert.Throws<InvalidOperationException>(
            () => world.AddComponent(original, new Position(0f, 0f)));
    }

    [Fact]
    public void DestroyEntity_IgnoresStaleEntity()
    {
        var world = new World();
        Entity first = world.CreateEntity();
        world.DestroyEntity(first);
        Entity reused = world.CreateEntity();

        // Destroying with the old/stale handle should be a no-op.
        world.DestroyEntity(first);

        Assert.True(world.Exists(reused));
        Assert.Equal(1, world.EntityCount);
    }
}
