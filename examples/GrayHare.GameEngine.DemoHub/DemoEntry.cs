using GrayHare.GameEngine.Scenes;

namespace GrayHare.GameEngine.DemoHub;

/// <summary>Describes a single demo entry in the hub catalog.</summary>
internal sealed record DemoEntry
{
    public DemoEntry(
        string title,
        string groupName,
        string description,
        Func<DemoCatalog, int, GameSceneBase> factory)
    {
        Title = title;
        GroupName = groupName;
        Description = description;
        Factory = factory;
    }

    public string Title { get; init; }
    public string GroupName { get; init; }
    public string Description { get; init; }
    public Func<DemoCatalog, int, GameSceneBase> Factory { get; init; }
}
