using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.SpriteDemo;

internal sealed class SpriteScene : DemoSceneBase
{
    private Sprite? _sprite;
    private Vector2f _position = new(640f, 360f);

    public SpriteScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);

        Texture texture = host.Assets.LoadTexture(Catalog.Assets.CheckerTexturePath, smooth: true);
        _sprite = new Sprite(texture)
        {
            Origin = new(48f, 48f),
            Position = _position,
            Scale = new(2f, 2f)
        };
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        if (_sprite is null)
        {
            return;
        }

        Vector2f moveDirection = new(0f, 0f);
        if (host.Input.IsKeyDown(Keyboard.Key.A) || host.Input.IsKeyDown(Keyboard.Key.Left))
        {
            moveDirection.X -= 1f;
        }

        if (host.Input.IsKeyDown(Keyboard.Key.D) || host.Input.IsKeyDown(Keyboard.Key.Right))
        {
            moveDirection.X += 1f;
        }

        if (host.Input.IsKeyDown(Keyboard.Key.W) || host.Input.IsKeyDown(Keyboard.Key.Up))
        {
            moveDirection.Y -= 1f;
        }

        if (host.Input.IsKeyDown(Keyboard.Key.S) || host.Input.IsKeyDown(Keyboard.Key.Down))
        {
            moveDirection.Y += 1f;
        }

        float speed = 280f * (float)gameTime.Delta.TotalSeconds;
        _position = new Vector2f(
            _position.X + moveDirection.X * speed,
            _position.Y + moveDirection.Y * speed);
        _sprite.Position = _position;
        _sprite.Rotation = (float)(gameTime.Total.TotalSeconds * 30.0);
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        if (_sprite is not null)
        {
            window.Draw(_sprite);
        }
    }
}
