namespace GrayHare.GameEngine.Ecs;

/// <summary>
/// Stores all entities and their associated components for a single scene or context.
/// </summary>
/// <remarks>This type is not thread-safe. Access all members from the main thread only.</remarks>
public sealed class World
{
    private readonly Dictionary<Type, IComponentStore> _stores = [];
    private readonly Dictionary<int, int> _entities = [];
    private readonly Queue<int> _freeIds = new();
    private int _nextEntityId = 1;
    private int _liveEntityCount;

    /// <summary>The number of live entities in this world.</summary>
    /// <example>
    /// <code>
    /// var world = new World();
    /// world.CreateEntity();
    /// world.CreateEntity();
    /// Console.WriteLine(world.EntityCount); // 2
    /// </code>
    /// </example>
    public int EntityCount => _liveEntityCount;

    /// <summary>The number of distinct component types that have been registered.</summary>
    /// <example>
    /// <code>
    /// var world = new World();
    /// var e = world.CreateEntity();
    /// world.AddComponent(e, new Position(0f, 0f));
    /// Console.WriteLine(world.ComponentTypeCount); // 1
    /// </code>
    /// </example>
    public int ComponentTypeCount => _stores.Count;

    /// <summary>Creates a new entity and returns its handle.</summary>
    /// <example>
    /// <code>
    /// var world = new World();
    /// Entity entity = world.CreateEntity();
    /// </code>
    /// </example>
    public Entity CreateEntity()
    {
        int id;
        int generation;

        if (_freeIds.Count > 0)
        {
            id = _freeIds.Dequeue();
            // The generation was already incremented during DestroyEntity, so reuse it directly.
            generation = _entities[id];
        }
        else
        {
            id = _nextEntityId++;
            generation = 0;
        }

        _entities[id] = generation;
        _liveEntityCount++;

        return new Entity(id, generation);
    }

    /// <summary>Returns <see langword="true"/> if <paramref name="entity"/> exists in this world.</summary>
    public bool Exists(Entity entity)
    {
        return _entities.TryGetValue(entity.Id, out int gen) && gen == entity.Generation;
    }

    /// <summary>
    /// Destroys <paramref name="entity"/> and removes all its components.
    /// Does nothing if the entity does not exist or the generation does not match.
    /// </summary>
    public void DestroyEntity(Entity entity)
    {
        if (!_entities.TryGetValue(entity.Id, out int gen) || gen != entity.Generation)
        {
            return;
        }

        // Bump generation so stale handles are invalidated, but keep the entry
        // so CreateEntity can reuse the ID with the new generation.
        _entities[entity.Id] = gen + 1;
        _liveEntityCount--;

        foreach (IComponentStore store in _stores.Values)
        {
            store.Remove(entity.Id);
        }

        _freeIds.Enqueue(entity.Id);
    }

    /// <summary>Removes all entities and components and resets the entity-ID counter.</summary>
    public void Clear()
    {
        _entities.Clear();
        _freeIds.Clear();
        _liveEntityCount = 0;
        foreach (IComponentStore store in _stores.Values)
        {
            store.Clear();
        }

        _nextEntityId = 1;
    }

    /// <summary>
    /// Attaches or replaces the <typeparamref name="TComponent"/> on <paramref name="entity"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="entity"/> does not exist in this world.
    /// </exception>
    public void AddComponent<TComponent>(Entity entity, TComponent component)
        where TComponent : notnull
    {
        EnsureEntityExists(entity);
        GetStore<TComponent>().Set(entity.Id, component);
    }

    /// <summary>
    /// Removes the <typeparamref name="TComponent"/> from <paramref name="entity"/>.
    /// Returns <see langword="true"/> if a component was removed.
    /// </summary>
    public bool RemoveComponent<TComponent>(Entity entity)
        where TComponent : notnull
    {
        return _stores.TryGetValue(typeof(TComponent), out IComponentStore? store) && store.Remove(entity.Id);
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="entity"/> has a
    /// <typeparamref name="TComponent"/> attached.
    /// </summary>
    /// <example>
    /// <code>
    /// if (world.HasComponent&lt;Velocity&gt;(entity))
    /// {
    ///     // entity can move
    /// }
    /// </code>
    /// </example>
    public bool HasComponent<TComponent>(Entity entity)
        where TComponent : notnull
    {
        return _stores.TryGetValue(typeof(TComponent), out IComponentStore? store) && store.Contains(entity.Id);
    }

    /// <summary>
    /// Returns the number of entities that currently have a <typeparamref name="TComponent"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// int posCount = world.ComponentCount&lt;Position&gt;();
    /// </code>
    /// </example>
    public int ComponentCount<TComponent>()
        where TComponent : notnull
    {
        return _stores.TryGetValue(typeof(TComponent), out IComponentStore? store)
            ? store.EntityIds.Count
            : 0;
    }

    /// <summary>
    /// Tries to retrieve the <typeparamref name="TComponent"/> on <paramref name="entity"/>.
    /// </summary>
    public bool TryGetComponent<TComponent>(Entity entity, out TComponent component)
        where TComponent : notnull
    {
        if (_stores.TryGetValue(typeof(TComponent), out IComponentStore? store) &&
            store is ComponentStore<TComponent> typedStore &&
            typedStore.TryGet(entity.Id, out component))
        {
            return true;
        }

        component = default!;
        return false;
    }

    /// <summary>
    /// Returns the <typeparamref name="TComponent"/> on <paramref name="entity"/>.
    /// Throws <see cref="KeyNotFoundException"/> when the component is absent.
    /// </summary>
    public TComponent GetComponent<TComponent>(Entity entity)
        where TComponent : notnull
    {
        return TryGetComponent(entity, out TComponent component)
            ? component
            : throw new KeyNotFoundException(
                $"Entity {entity.Id} does not contain component {typeof(TComponent).Name}.");
    }

    /// <summary>
    /// Enumerates all entities that have a <typeparamref name="TComponent"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// foreach (Entity e in world.Query&lt;Position&gt;())
    /// {
    ///     var pos = world.GetComponent&lt;Position&gt;(e);
    ///     // ...
    /// }
    /// </code>
    /// </example>
    public IEnumerable<Entity> Query<TComponent>()
        where TComponent : notnull
    {
        if (!_stores.TryGetValue(typeof(TComponent), out IComponentStore? store))
        {
            yield break;
        }

        foreach (int entityId in store.EntityIds)
        {
            if (_entities.TryGetValue(entityId, out int gen))
            {
                yield return new Entity(entityId, gen);
            }
        }
    }

    /// <summary>
    /// Enumerates all entities that have both
    /// <typeparamref name="TComponentA"/> and <typeparamref name="TComponentB"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// foreach (Entity e in world.Query&lt;Position, Velocity&gt;())
    /// {
    ///     var pos = world.GetComponent&lt;Position&gt;(e);
    ///     var vel = world.GetComponent&lt;Velocity&gt;(e);
    ///     // ...
    /// }
    /// </code>
    /// </example>
    public IEnumerable<Entity> Query<TComponentA, TComponentB>()
        where TComponentA : notnull
        where TComponentB : notnull
    {
        if (!_stores.TryGetValue(typeof(TComponentA), out IComponentStore? firstStore) ||
            !_stores.TryGetValue(typeof(TComponentB), out IComponentStore? secondStore))
        {
            yield break;
        }

        IComponentStore primary = firstStore.EntityIds.Count <= secondStore.EntityIds.Count
            ? firstStore
            : secondStore;
        IComponentStore secondary = ReferenceEquals(primary, firstStore) ? secondStore : firstStore;

        foreach (int entityId in primary.EntityIds)
        {
            if (_entities.TryGetValue(entityId, out int gen) && secondary.Contains(entityId))
            {
                yield return new Entity(entityId, gen);
            }
        }
    }

    /// <summary>
    /// Enumerates all entities that have
    /// <typeparamref name="TComponentA"/>, <typeparamref name="TComponentB"/>,
    /// and <typeparamref name="TComponentC"/>.
    /// Iterates the smallest component store first for efficiency.
    /// </summary>
    /// <example>
    /// <code>
    /// foreach (Entity e in world.Query&lt;Position, Velocity, Health&gt;())
    /// {
    ///     var pos    = world.GetComponent&lt;Position&gt;(e);
    ///     var vel    = world.GetComponent&lt;Velocity&gt;(e);
    ///     var health = world.GetComponent&lt;Health&gt;(e);
    ///     // ...
    /// }
    /// </code>
    /// </example>
    public IEnumerable<Entity> Query<TComponentA, TComponentB, TComponentC>()
        where TComponentA : notnull
        where TComponentB : notnull
        where TComponentC : notnull
    {
        if (!_stores.TryGetValue(typeof(TComponentA), out IComponentStore? storeA) ||
            !_stores.TryGetValue(typeof(TComponentB), out IComponentStore? storeB) ||
            !_stores.TryGetValue(typeof(TComponentC), out IComponentStore? storeC))
        {
            yield break;
        }

        // Use the smallest store as primary to minimise iterations.
        IComponentStore primary = storeA;
        if (storeB.EntityIds.Count < primary.EntityIds.Count)
        {
            primary = storeB;
        }

        if (storeC.EntityIds.Count < primary.EntityIds.Count)
        {
            primary = storeC;
        }

        IComponentStore other1 = ReferenceEquals(primary, storeA) ? storeB : storeA;
        IComponentStore other2 = ReferenceEquals(primary, storeC) ? storeB : storeC;

        foreach (int entityId in primary.EntityIds)
        {
            if (_entities.TryGetValue(entityId, out int gen) && other1.Contains(entityId) && other2.Contains(entityId))
            {
                yield return new Entity(entityId, gen);
            }
        }
    }

    /// <summary>
    /// Invokes <paramref name="action"/> for every entity that has a <typeparamref name="TComponent"/>.
    /// Entities are collected into a snapshot before iteration so the action may safely modify components.
    /// </summary>
    /// <example>
    /// <code>
    /// world.ForEach&lt;Position&gt;((entity, pos) =>
    /// {
    ///     Console.WriteLine($"{entity} at ({pos.X}, {pos.Y})");
    /// });
    /// </code>
    /// </example>
    public void ForEach<TComponent>(Action<Entity, TComponent> action)
        where TComponent : notnull
    {
        if (!_stores.TryGetValue(typeof(TComponent), out IComponentStore? rawStore))
        {
            return;
        }

        ComponentStore<TComponent> store = (ComponentStore<TComponent>)rawStore;

        // Snapshot entity IDs so the action can safely mutate components.
        List<int> snapshot = [.. store.EntityIds];

        foreach (int entityId in snapshot)
        {
            if (_entities.TryGetValue(entityId, out int gen) &&
                store.TryGet(entityId, out TComponent comp))
            {
                action(new Entity(entityId, gen), comp);
            }
        }
    }

    /// <summary>
    /// Invokes <paramref name="action"/> for every entity that has both
    /// <typeparamref name="TComponentA"/> and <typeparamref name="TComponentB"/>.
    /// Iterates the smallest store first for efficiency.
    /// Entities are collected into a snapshot before iteration so the action may safely modify components.
    /// </summary>
    /// <example>
    /// <code>
    /// world.ForEach&lt;Position, Velocity&gt;((entity, pos, vel) =>
    /// {
    ///     world.AddComponent(entity, new Position(pos.X + vel.X, pos.Y + vel.Y));
    /// });
    /// </code>
    /// </example>
    public void ForEach<TComponentA, TComponentB>(Action<Entity, TComponentA, TComponentB> action)
        where TComponentA : notnull
        where TComponentB : notnull
    {
        if (!_stores.TryGetValue(typeof(TComponentA), out IComponentStore? rawStoreA) ||
            !_stores.TryGetValue(typeof(TComponentB), out IComponentStore? rawStoreB))
        {
            return;
        }

        ComponentStore<TComponentA> storeA = (ComponentStore<TComponentA>)rawStoreA;
        ComponentStore<TComponentB> storeB = (ComponentStore<TComponentB>)rawStoreB;

        // Pick the smallest store to iterate.
        IComponentStore primary = storeA.EntityIds.Count <= storeB.EntityIds.Count
            ? (IComponentStore)storeA
            : storeB;

        List<int> snapshot = [.. primary.EntityIds];

        foreach (int entityId in snapshot)
        {
            if (_entities.TryGetValue(entityId, out int gen) &&
                storeA.TryGet(entityId, out TComponentA compA) &&
                storeB.TryGet(entityId, out TComponentB compB))
            {
                action(new Entity(entityId, gen), compA, compB);
            }
        }
    }

    /// <summary>
    /// Invokes <paramref name="action"/> for every entity that has
    /// <typeparamref name="TComponentA"/>, <typeparamref name="TComponentB"/>,
    /// and <typeparamref name="TComponentC"/>.
    /// Iterates the smallest store first for efficiency.
    /// Entities are collected into a snapshot before iteration so the action may safely modify components.
    /// </summary>
    /// <example>
    /// <code>
    /// world.ForEach&lt;Position, Velocity, Health&gt;((entity, pos, vel, hp) =>
    /// {
    ///     // process entity with all three components
    /// });
    /// </code>
    /// </example>
    public void ForEach<TComponentA, TComponentB, TComponentC>(
        Action<Entity, TComponentA, TComponentB, TComponentC> action)
        where TComponentA : notnull
        where TComponentB : notnull
        where TComponentC : notnull
    {
        if (!_stores.TryGetValue(typeof(TComponentA), out IComponentStore? rawStoreA) ||
            !_stores.TryGetValue(typeof(TComponentB), out IComponentStore? rawStoreB) ||
            !_stores.TryGetValue(typeof(TComponentC), out IComponentStore? rawStoreC))
        {
            return;
        }

        ComponentStore<TComponentA> storeA = (ComponentStore<TComponentA>)rawStoreA;
        ComponentStore<TComponentB> storeB = (ComponentStore<TComponentB>)rawStoreB;
        ComponentStore<TComponentC> storeC = (ComponentStore<TComponentC>)rawStoreC;

        // Pick the smallest store to iterate.
        IComponentStore primary = rawStoreA;
        if (rawStoreB.EntityIds.Count < primary.EntityIds.Count)
        {
            primary = rawStoreB;
        }

        if (rawStoreC.EntityIds.Count < primary.EntityIds.Count)
        {
            primary = rawStoreC;
        }

        List<int> snapshot = [.. primary.EntityIds];

        foreach (int entityId in snapshot)
        {
            if (_entities.TryGetValue(entityId, out int gen) &&
                storeA.TryGet(entityId, out TComponentA compA) &&
                storeB.TryGet(entityId, out TComponentB compB) &&
                storeC.TryGet(entityId, out TComponentC compC))
            {
                action(new Entity(entityId, gen), compA, compB, compC);
            }
        }
    }

    private ComponentStore<TComponent> GetStore<TComponent>()
        where TComponent : notnull
    {
        if (_stores.TryGetValue(typeof(TComponent), out IComponentStore? existingStore))
        {
            return (ComponentStore<TComponent>)existingStore;
        }

        ComponentStore<TComponent> newStore = new();
        _stores[typeof(TComponent)] = newStore;
        return newStore;
    }

    private void EnsureEntityExists(Entity entity)
    {
        if (!Exists(entity))
        {
            throw new InvalidOperationException(
                $"Entity {entity.Id} does not exist in this world.");
        }
    }

    private interface IComponentStore
    {
        IReadOnlyCollection<int> EntityIds { get; }
        bool Contains(int entityId);
        bool Remove(int entityId);
        void Clear();
    }

    private sealed class ComponentStore<TComponent> : IComponentStore
        where TComponent : notnull
    {
        private readonly Dictionary<int, TComponent> _components = [];

        public IReadOnlyCollection<int> EntityIds => _components.Keys;

        public bool Contains(int entityId)
        {
            return _components.ContainsKey(entityId);
        }

        public void Set(int entityId, TComponent component)
        {
            _components[entityId] = component;
        }

        public bool TryGet(int entityId, out TComponent component)
        {
            if (_components.TryGetValue(entityId, out TComponent? value))
            {
                component = value;
                return true;
            }

            component = default!;
            return false;
        }

        public bool Remove(int entityId)
        {
            return _components.Remove(entityId);
        }

        public void Clear()
        {
            _components.Clear();
        }
    }
}
