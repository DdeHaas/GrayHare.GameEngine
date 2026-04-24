using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Ecs;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.Ecs3Demo;

/// <summary>
/// ECS 3-component query demo.
/// Entities each carry a <c>Position</c>, <c>Velocity</c>, and <c>Health</c> component.
/// Update iterates <c>Query&lt;Position, Velocity, Health&gt;()</c> to move and damage balls.
/// Render uses <c>Query&lt;Position, Health&gt;()</c> to colour each ball by its health.
/// </summary>
internal sealed class Ecs3Scene : DemoSceneBase
{
    private Font? _font;

    public Ecs3Scene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();

        Random random = Random.Shared;
        float w = host.Window.Size.X;
        float h = host.Window.Size.Y;

        for (int i = 0; i < 20; i++)
        {
            Entity e = host.World.CreateEntity();
            host.World.AddComponent(e, new Position(new Vector2f(random.NextSingle() * w, random.NextSingle() * h)));
            host.World.AddComponent(e, new Velocity(new Vector2f((random.NextSingle() - 0.5f) * 260f, (random.NextSingle() - 0.5f) * 260f)));
            host.World.AddComponent(e, new Health(80 + random.Next(21)));  // 80-100
        }
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        float dt = (float)gameTime.Delta.TotalSeconds;
        float w = host.Window.Size.X;
        float h = host.Window.Size.Y;

        // 3-component query: move each ball and slowly drain its health.
        foreach (Entity e in host.World.Query<Position, Velocity, Health>())
        {
            Position pos = host.World.GetComponent<Position>(e);
            Velocity vel = host.World.GetComponent<Velocity>(e);
            Health hp = host.World.GetComponent<Health>(e);

            Vector2f newPos = pos.Value + vel.Value * dt;

            // Bounce off window edges.
            Vector2f newVel = vel.Value;
            if (newPos.X < Radius || newPos.X > w - Radius)
            {
                newVel.X = -newVel.X;
                newPos.X = Math.Clamp(newPos.X, Radius, w - Radius);
            }

            if (newPos.Y < Radius || newPos.Y > h - Radius)
            {
                newVel.Y = -newVel.Y;
                newPos.Y = Math.Clamp(newPos.Y, Radius, h - Radius);
            }

            // Health drains at 5 HP/s — the 3-component query is the only place this happens.
            float newHp = Math.Max(0f, hp.Value - 5f * dt);

            host.World.AddComponent(e, new Position(newPos));
            host.World.AddComponent(e, new Velocity(newVel));
            host.World.AddComponent(e, new Health(newHp));
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        foreach (Entity e in host.World.Query<Position, Health>())
        {
            Position pos = host.World.GetComponent<Position>(e);
            Health hp = host.World.GetComponent<Health>(e);

            // Colour shifts from green (full health) to red (zero health).
            byte red = (byte)(255 * (1f - hp.Value / 100f));
            byte green = (byte)(255 * (hp.Value / 100f));

            using CircleShape shape = new(Radius)
            {
                Origin = new Vector2f(Radius, Radius),
                Position = pos.Value,
                FillColor = new Color(red, green, 60),
                OutlineColor = new Color(220, 220, 220),
                OutlineThickness = 1f
            };
            window.Draw(shape);
        }

        if (_font is not null)
        {
            using Text hint = new(_font,
                "ECS 3-component query  –  balls drain from green→red over time", 18)
            {
                Position = new Vector2f(20f, 20f),
                FillColor = new Color(200, 200, 200)
            };
            window.Draw(hint);
        }
    }

    private const float Radius = 16f;

    private readonly record struct Position(Vector2f Value);
    private readonly record struct Velocity(Vector2f Value);
    private readonly record struct Health(float Value);
}
