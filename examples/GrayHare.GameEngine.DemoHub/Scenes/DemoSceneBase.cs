using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Scenes;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes;

/// <summary>
/// Base class for all demo scenes.
/// Handles global navigation: Esc to exit, + / - to cycle scenes,
/// PageDown / PageUp to jump between groups.
/// Registers the shared <see cref="HubLayer"/> overlay on every scene.
/// </summary>
internal abstract class DemoSceneBase : GameSceneBase
{
    protected DemoSceneBase(DemoCatalog catalog, int sceneIndex)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        Catalog = catalog;
        SceneIndex = sceneIndex;
        AddLayer(new HubLayer(catalog, sceneIndex));
    }

    protected DemoCatalog Catalog { get; }

    protected int SceneIndex { get; }

    public override void Load(GameHost host)
    {
        base.Load(host);

        host.Window.SetTitle(
            $"GrayHare DemoHub  [{Catalog.Entries[SceneIndex].Title}]");
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        if (host.Input.WasKeyPressed(Keyboard.Key.Escape))
        {
            host.Exit();
            return;
        }

        // + / Numpad+ → next scene
        if (host.Input.WasKeyPressed(Keyboard.Key.Add) ||
            host.Input.WasKeyPressed(Keyboard.Key.Equal))
        {
            host.ChangeScene(Catalog.Create(SceneIndex + 1));
            return;
        }

        // - / Numpad- → previous scene
        if (host.Input.WasKeyPressed(Keyboard.Key.Subtract) ||
            host.Input.WasKeyPressed(Keyboard.Key.Hyphen))
        {
            host.ChangeScene(Catalog.Create(SceneIndex - 1));
            return;
        }

        base.Update(host, in gameTime);
    }
}
