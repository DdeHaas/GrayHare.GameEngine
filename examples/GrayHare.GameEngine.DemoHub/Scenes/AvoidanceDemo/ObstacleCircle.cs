using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.AvoidanceDemo;

/// <summary>A circular obstacle that satisfies <see cref="IGameObject"/>.</summary>
internal sealed class ObstacleCircle : IGameObject
{
    private readonly float _radius;
    private readonly CircleShape _shape;

    public ObstacleCircle(Vector2f center, float radius)
    {
        _radius = radius;
        _shape = new CircleShape(radius)
        {
            Origin = new Vector2f(radius, radius),
            Position = center,
            FillColor = new Color(100, 60, 40),
            OutlineColor = new Color(180, 130, 80),
            OutlineThickness = 2f
        };
    }

    public Vector2f Position => _shape.Position;
    float IGameObject.Rotation => 0f;
    Vector2f IGameObject.Origin => new(_radius, _radius);
    Vector2f IGameObject.Scale => Constants.Vectors.One;
    int IGameObject.ZOrder => 0;

    public FloatRect GlobalBounds =>
        new(Position - new Vector2f(_radius, _radius), new Vector2f(_radius * 2f, _radius * 2f));

    /// <summary>Draws the obstacle circle to the window.</summary>
    public void Draw(RenderWindow window)
    {
        window.Draw(_shape);
    }

    /// <inheritdoc/>
    public void Update(float dt) { }

    /// <inheritdoc/>
    public void Dispose()
    {
        _shape.Dispose();
    }
}
