using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Ecs;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.EcsRecyclingDemo;

/// <summary>
/// Demonstrates ECS entity recycling by continuously spawning and destroying entities.
/// Off-screen entities are destroyed, and the scene tracks when recycled IDs appear.
/// </summary>
internal sealed class EcsRecyclingScene : DemoSceneBase
{
    private Font? _font;
    private float _spawnTimer;
    private int _maxEntityIdSeen;
    private bool _recyclingDetected;
    private int _totalSpawned;
    private int _totalDestroyed;

    public EcsRecyclingScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        float dt = gameTime.DeltaTotalSeconds;
        float w = host.Window.Size.X;
        float h = host.Window.Size.Y;

        // Spawn entities periodically.
        _spawnTimer += dt;
        if (_spawnTimer >= 0.5f)
        {
            _spawnTimer -= 0.5f;
            for (int i = 0; i < 5; i++)
            {
                SpawnEntity(host, w, h);
            }
        }

        // Move entities and destroy off-screen ones.
        List<Entity> toDestroy = [];

        host.World.ForEach<Position, Velocity>((entity, pos, vel) =>
        {
            Position newPos = pos with
            {
                X = pos.X + vel.Vx * dt,
                Y = pos.Y + vel.Vy * dt
            };
            host.World.AddComponent(entity, newPos);

            const float margin = 40f;
            if (newPos.X < -margin || newPos.X > w + margin ||
                newPos.Y < -margin || newPos.Y > h + margin)
            {
                toDestroy.Add(entity);
            }
        });

        foreach (Entity entity in toDestroy)
        {
            host.World.DestroyEntity(entity);
            _totalDestroyed++;
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // Draw entities as colored circles.
        foreach (Entity entity in host.World.Query<Position, Tint>())
        {
            Position pos = host.World.GetComponent<Position>(entity);
            Tint tint = host.World.GetComponent<Tint>(entity);

            using CircleShape shape = new(8f)
            {
                Origin = new Vector2f(8f, 8f),
                Position = new Vector2f(pos.X, pos.Y),
                FillColor = new Color(tint.R, tint.G, tint.B)
            };
            window.Draw(shape);
        }

        if (_font is null)
        {
            return;
        }

        string recycleLabel = _recyclingDetected
            ? "Entity ID recycling: DETECTED"
            : "Entity ID recycling: not yet";
        Color recycleColor = _recyclingDetected
            ? new Color(100, 255, 100)
            : new Color(180, 180, 180);

        using Text stats = new(_font,
            $"Entities: {host.World.EntityCount}  Components: {host.World.ComponentTypeCount}\n" +
            $"Spawned: {_totalSpawned}  Destroyed: {_totalDestroyed}\n" +
            recycleLabel, 20)
        {
            Position = new Vector2f(20f, 20f),
            FillColor = new Color(220, 230, 255)
        };
        window.Draw(stats);

        using Text hint = new(_font, recycleLabel, 18)
        {
            Position = new Vector2f(20f, 100f),
            FillColor = recycleColor
        };
        window.Draw(hint);
    }

    private void SpawnEntity(GameHost host, float maxX, float maxY)
    {
        Entity entity = host.World.CreateEntity();

        // Detect recycling: if a new entity has an ID lower than the previously seen max.
        if (entity.Id <= _maxEntityIdSeen && _totalSpawned > 0)
        {
            _recyclingDetected = true;
        }

        if (entity.Id > _maxEntityIdSeen)
        {
            _maxEntityIdSeen = entity.Id;
        }

        Random random = Random.Shared;

        float x = random.NextSingle() * maxX;
        float y = random.NextSingle() * maxY;
        float vx = (random.NextSingle() - 0.5f) * 200f;
        float vy = (random.NextSingle() - 0.5f) * 200f;
        byte r = (byte)(60 + random.Next(196));
        byte g = (byte)(60 + random.Next(196));
        byte b = (byte)(60 + random.Next(196));
        host.World.AddComponent(entity, new Position(x, y));
        host.World.AddComponent(entity, new Velocity(vx, vy));
        host.World.AddComponent(entity, new Tint(r, g, b));

        _totalSpawned++;
    }

    private record struct Position(float X, float Y);
    private record struct Velocity(float Vx, float Vy);
    private record struct Tint(byte R, byte G, byte B);
}
