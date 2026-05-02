using SFML.System;

namespace GrayHare.GameEngine;

/// <summary>
/// Extends <see cref="IGameObject"/> with the physics properties required by the behavior
/// and steering systems.
/// </summary>
public interface IMovableGameObject : IGameObject
{
    /// <summary>Object mass; heavier objects turn and accelerate more slowly.</summary>
    float Mass { get; }

    /// <summary>Unit direction vector derived from <see cref="IGameObject.Rotation"/>.</summary>
    /// <remarks>Typically implemented as <c>Rotation.ToVector2f().Normalized()</c>.</remarks>
    Vector2f Heading { get; }

    /// <summary>Right-perpendicular of <see cref="Heading"/>.</summary>
    /// <remarks>Typically implemented as <c>Heading.Perpendicular()</c>.</remarks>
    Vector2f Side { get; }

    /// <summary>Current velocity vector.</summary>
    Vector2f Velocity { get; }

    /// <summary>Scalar speed; equivalent to <c>Velocity.Length</c>.</summary>
    float Speed { get; }

    /// <summary>Acceleration force applied per second when moving forward.</summary>
    float Acceleration { get; }

    /// <summary>Passive deceleration applied per second when no input is active.</summary>
    float Deceleration { get; }

    /// <summary>Stronger deceleration applied per second when braking.</summary>
    float BrakingDeceleration { get; }

    /// <summary>Maximum speed the object may reach.</summary>
    float MaxSpeed { get; }

    /// <summary>Turn rate in degrees per second (before mass scaling).</summary>
    float TurnRate { get; }

    /// <summary>Hard cap on the turn rate after mass scaling.</summary>
    float MaxTurnRate { get; }
}
