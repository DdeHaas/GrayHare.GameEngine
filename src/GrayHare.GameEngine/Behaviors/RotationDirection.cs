namespace GrayHare.GameEngine.Behaviors;

/// <summary>Specifies an explicit rotational direction.</summary>
public enum RotationDirection
{
    /// <summary>Take the shortest angular path to the target.</summary>
    Default = 0,

    /// <summary>Rotate clockwise.</summary>
    Clockwise = 1,

    /// <summary>Rotate counterclockwise.</summary>
    Counterclockwise = -1
}
