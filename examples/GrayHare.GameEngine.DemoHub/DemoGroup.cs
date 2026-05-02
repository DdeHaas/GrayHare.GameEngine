namespace GrayHare.GameEngine.DemoHub;

/// <summary>A contiguous range of demo scenes that share a logical category.</summary>
internal sealed record DemoGroup
{
    public DemoGroup(string name, int startIndex, int endIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        StartIndex = startIndex;
        EndIndex = endIndex;
    }

    public string Name { get; init; }
    public int StartIndex { get; init; }
    public int EndIndex { get; init; }
}
