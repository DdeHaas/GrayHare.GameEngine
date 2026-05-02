using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Ecs;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.EcsComponentOpsDemo;

/// <summary>
/// Demonstrates <see cref="World.HasComponent{T}"/>, <see cref="World.TryGetComponent{T}"/>,
/// <see cref="World.RemoveComponent{T}"/>, and <see cref="World.Clear"/>.
/// Thirty bouncing circles are managed entirely through the ECS world.
/// </summary>
internal sealed class EcsComponentOpsScene : DemoSceneBase
{
    private const int EntityCount = 30;
    private const int InitialHighlighted = 15;
    private const float DotRadius = 6f;
    private const float HighlightedRadius = 10f;

    private Font _font = null!;
    private readonly List<Entity> _entityHandles = [];
    private string _lastTryGetResult = string.Empty;

    public EcsComponentOpsScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();

        SpawnAll(host);
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        float deltaTime = gameTime.DeltaTotalSeconds;
        float w = host.Window.Size.X;
        float h = host.Window.Size.Y;

        BounceAll(host, deltaTime, w, h);

        // H: toggle Highlighted on the entity closest to center using HasComponent.
        if (host.Input.WasKeyPressed(Keyboard.Key.H))
        {
            Vector2f center = new(w / 2f, h / 2f);
            Entity? closest = FindClosestToCenter(host, center);

            if (closest.HasValue)
            {
                Entity e = closest.Value;

                if (host.World.HasComponent<Highlighted>(e))
                {
                    host.World.RemoveComponent<Highlighted>(e);
                }
                else
                {
                    host.World.AddComponent(e, new Highlighted());
                }
            }
        }

        // T: TryGetComponent on the first spawned entity.
        if (host.Input.WasKeyPressed(Keyboard.Key.T) && _entityHandles.Count > 0)
        {
            Entity first = _entityHandles[0];
            bool found = host.World.TryGetComponent<Highlighted>(first, out _);

            _lastTryGetResult = found
                ? "TryGet<Highlighted>(entities[0]) → true (highlighted)"
                : "TryGet<Highlighted>(entities[0]) → false (not highlighted)";
        }

        // R: RemoveComponent<Highlighted> from every entity (snapshot to avoid mutation during iteration).
        if (host.Input.WasKeyPressed(Keyboard.Key.R))
        {
            foreach (Entity e in _entityHandles.ToArray())
            {
                host.World.RemoveComponent<Highlighted>(e);
            }

            _lastTryGetResult = "RemoveComponent<Highlighted> called on all entities.";
        }

        // C: World.Clear() then re-spawn.
        if (host.Input.WasKeyPressed(Keyboard.Key.C))
        {
            host.World.Clear();
            _entityHandles.Clear();
            _lastTryGetResult = "World.Clear() called — world re-populated.";

            SpawnAll(host);
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        foreach (Entity entity in _entityHandles)
        {
            BotPosition pos = host.World.GetComponent<BotPosition>(entity);
            bool highlighted = host.World.HasComponent<Highlighted>(entity);

            float r = highlighted ? HighlightedRadius : DotRadius;

            using CircleShape dot = new(r)
            {
                Origin = new Vector2f(r, r),
                Position = new Vector2f(pos.X, pos.Y),
                FillColor = highlighted ? new Color(255, 220, 60) : new Color(80, 140, 200),
                OutlineColor = highlighted ? new Color(255, 160, 0) : Color.Transparent,
                OutlineThickness = highlighted ? 2f : 0f
            };
            window.Draw(dot);
        }

        int highlightedCount = host.World.ComponentCount<Highlighted>();

        using Text stats = new(_font,
            $"Entities: {host.World.EntityCount}  Highlighted: {highlightedCount}\n" +
            "H  toggle highlight on nearest entity  ·  T  TryGetComponent on entities[0]\n" +
            "R  RemoveComponent all highlights  ·  C  World.Clear() + re-spawn\n" +
            _lastTryGetResult, 18)
        {
            Position = new Vector2f(20f, 20f),
            FillColor = new Color(220, 230, 255)
        };

        window.Draw(stats);
    }

    private void SpawnAll(GameHost host)
    {
        float w = host.Window.Size.X;
        float h = host.Window.Size.Y;
        Random rng = Random.Shared;

        for (int i = 0; i < EntityCount; i++)
        {
            Entity entity = host.World.CreateEntity();

            float x = rng.NextSingle() * w;
            float y = rng.NextSingle() * h;
            float vx = (rng.NextSingle() - 0.5f) * 200f;
            float vy = (rng.NextSingle() - 0.5f) * 200f;

            host.World.AddComponent(entity, new BotPosition(x, y));
            host.World.AddComponent(entity, new BotVelocity(vx, vy));

            if (i < InitialHighlighted)
            {
                host.World.AddComponent(entity, new Highlighted());
            }

            _entityHandles.Add(entity);
        }
    }

    private void BounceAll(GameHost host, float dt, float w, float h)
    {
        foreach (Entity entity in _entityHandles)
        {
            BotPosition pos = host.World.GetComponent<BotPosition>(entity);
            BotVelocity vel = host.World.GetComponent<BotVelocity>(entity);

            float nx = pos.X + vel.Vx * dt;
            float ny = pos.Y + vel.Vy * dt;
            float nvx = vel.Vx;
            float nvy = vel.Vy;

            if (nx < 0f || nx > w)
            {
                nvx = -nvx;
                nx = Math.Clamp(nx, 0f, w);
            }

            if (ny < 0f || ny > h)
            {
                nvy = -nvy;
                ny = Math.Clamp(ny, 0f, h);
            }

            host.World.AddComponent(entity, new BotPosition(nx, ny));
            host.World.AddComponent(entity, new BotVelocity(nvx, nvy));
        }
    }

    private Entity? FindClosestToCenter(GameHost host, Vector2f center)
    {
        Entity? closest = null;
        float minDistSq = float.MaxValue;

        foreach (Entity entity in _entityHandles)
        {
            BotPosition pos = host.World.GetComponent<BotPosition>(entity);
            float dx = pos.X - center.X;
            float dy = pos.Y - center.Y;
            float distSq = dx * dx + dy * dy;

            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                closest = entity;
            }
        }

        return closest;
    }

    private record struct BotPosition(float X, float Y);
    private record struct BotVelocity(float Vx, float Vy);
    private record struct Highlighted();
}
