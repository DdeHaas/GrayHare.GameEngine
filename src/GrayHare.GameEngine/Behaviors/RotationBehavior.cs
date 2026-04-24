using GrayHare.GameEngine.Abstractions;
using GrayHare.GameEngine.Extensions;
using SFML.System;

namespace GrayHare.GameEngine.Behaviors;

/// <summary>
/// Applies a mass-scaled rotation to an <see cref="IMovableGameObject"/> and updates
/// its heading vector accordingly.
/// </summary>
/// <example>
/// <code>
/// var rotation = new RotationBehavior(gameObject);
/// rotation.IsTurningLeft = input.IsKeyDown(Key.A);
/// rotation.IsTurningRight = input.IsKeyDown(Key.D);
/// float newRotation = rotation.UpdateRotation(dt, ref heading);
/// </code>
/// </example>
public sealed class RotationBehavior
{
    private readonly IMovableGameObject _gameObject;

    /// <summary>Initializes the behavior for <paramref name="gameObject"/>.</summary>
    public RotationBehavior(IMovableGameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);
        _gameObject = gameObject;
    }

    /// <summary>Whether the object should rotate left (counterclockwise) this frame.</summary>
    public bool IsTurningLeft { get; set; }

    /// <summary>Whether the object should rotate right (clockwise) this frame.</summary>
    public bool IsTurningRight { get; set; }

    /// <summary>
    /// Computes and returns the new rotation angle (degrees) and updates
    /// <paramref name="heading"/> when a turn is active.
    /// </summary>
    /// <param name="deltaTime">Elapsed seconds since the last frame.</param>
    /// <param name="heading">Current heading vector; updated in place when turning.</param>
    public float UpdateRotation(float deltaTime, ref Vector2f heading)
    {
        float mass = MathF.Max(_gameObject.Mass, float.Epsilon);
        float turnAmount = MathF.Min(_gameObject.TurnRate / mass, _gameObject.MaxTurnRate) * deltaTime;
        float newRotation = _gameObject.Rotation;

        if (IsTurningLeft)
        {
            newRotation -= turnAmount;
        }
        else if (IsTurningRight)
        {
            newRotation += turnAmount;
        }

        if (IsTurningLeft || IsTurningRight)
        {
            heading = newRotation.ToVector2f().Normalized();
        }

        return newRotation;
    }
}
