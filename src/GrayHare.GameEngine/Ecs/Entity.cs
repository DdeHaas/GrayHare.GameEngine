namespace GrayHare.GameEngine.Ecs;

/// <summary>Lightweight handle that identifies a single entity in a <see cref="World"/>.</summary>
/// <param name="Id">The numeric identifier of this entity.</param>
/// <param name="Generation">
/// Generation counter used to detect stale handles after an entity ID has been recycled.
/// </param>
/// <example>
/// <code>
/// Entity entity = world.CreateEntity();
/// Console.WriteLine(entity); // Entity(1:0)
/// </code>
/// </example>
public readonly record struct Entity(int Id, int Generation = 0)
{
    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Entity({Id}:{Generation})";
    }
}
