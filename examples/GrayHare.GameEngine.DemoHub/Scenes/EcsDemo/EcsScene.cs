using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Ecs;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.EcsDemo;

/// <summary>
/// Demonstrates the <see cref="GrayHare.GameEngine.Ecs.World"/> ECS by bouncing
/// eight coloured circles around the window. Each circle is an <see cref="GrayHare.GameEngine.Ecs.Entity"/>
/// with <c>Position</c>, <c>Velocity</c>, and <c>Tint</c> components updated
/// every frame via <see cref="GrayHare.GameEngine.Ecs.World.Query{TA,TB}"/>.
/// </summary>
internal sealed class EcsScene : DemoSceneBase
{
    public EcsScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);

        for (int index = 0; index < 8; index++)
        {
            Entity entity = host.World.CreateEntity();
            host.World.AddComponent(entity, new Position(new Vector2f(120f + index * 120f, 160f + index * 40f)));
            host.World.AddComponent(entity, new Velocity(new Vector2f(100f + index * 12f, 70f + index * 10f)));
            host.World.AddComponent(entity, new Tint(
                new Color(
                    (byte)(60 + index * 20),
                    (byte)(180 - index * 10),
                    (byte)(120 + index * 10))));
        }
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        Vector2f bounds = new(host.Window.Size.X - 30f, host.Window.Size.Y - 30f);
        float deltaTime = gameTime.DeltaTotalSeconds;

        foreach (Entity entity in host.World.Query<Position, Velocity>())
        {
            Position position = host.World.GetComponent<Position>(entity);
            Velocity velocity = host.World.GetComponent<Velocity>(entity);

            position = position with
            {
                Value = new Vector2f(
                    position.Value.X + velocity.Value.X * deltaTime,
                    position.Value.Y + velocity.Value.Y * deltaTime)
            };

            if (position.Value.X < 30f || position.Value.X > bounds.X)
            {
                velocity = velocity with { Value = new Vector2f(-velocity.Value.X, velocity.Value.Y) };
                position = position with
                {
                    Value = new Vector2f(Math.Clamp(position.Value.X, 30f, bounds.X), position.Value.Y)
                };
            }

            if (position.Value.Y < 30f || position.Value.Y > bounds.Y)
            {
                velocity = velocity with { Value = new Vector2f(velocity.Value.X, -velocity.Value.Y) };
                position = position with
                {
                    Value = new Vector2f(position.Value.X, Math.Clamp(position.Value.Y, 30f, bounds.Y))
                };
            }

            host.World.AddComponent(entity, position);
            host.World.AddComponent(entity, velocity);
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        foreach (Entity entity in host.World.Query<Position, Tint>())
        {
            Position position = host.World.GetComponent<Position>(entity);
            Tint tint = host.World.GetComponent<Tint>(entity);

            using CircleShape shape = new(18f)
            {
                Origin = new(18f, 18f),
                Position = position.Value,
                FillColor = tint.Value
            };

            window.Draw(shape);
        }
    }

    private readonly record struct Position(Vector2f Value);
    private readonly record struct Velocity(Vector2f Value);
    private readonly record struct Tint(Color Value);
}
