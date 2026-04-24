using GrayHare.GameEngine.Animation;
using GrayHare.GameEngine.Application;
using SFML.Graphics;

namespace GrayHare.GameEngine.DemoHub.Scenes.AnimationDemo;

internal sealed class AnimationScene : DemoSceneBase
{
    private AnimationClip? _clip;
    private AnimationPlayer? _player;

    public AnimationScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);

        Image image = host.Assets.LoadImage(Catalog.Assets.SpriteSheetPath);
        _clip = AnimationClip.CreateFromImage("pulse", image, 32, 32, 4, TimeSpan.FromMilliseconds(120));

        _player = new AnimationPlayer(_clip, true, true)
        {
            Position = new(640f, 360f),
            Scale = new(6f, 6f)
        };
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        _player?.Update(gameTime.Delta);
        _player?.Rotation = (float)(gameTime.Total.TotalSeconds * 18.0);
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        _player?.Render(window);
    }

    public override void Unload(GameHost host)
    {
        base.Unload(host);

        _player?.Dispose();
        _clip?.Dispose();
    }
}
