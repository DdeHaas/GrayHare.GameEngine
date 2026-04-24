using GrayHare.GameEngine.Abstractions;
using SFML.System;

namespace GrayHare.GameEngine.Behaviors;

/// <summary>
/// Applies force-based acceleration, braking, and passive deceleration to an
/// <see cref="IMovableGameObject"/>.  Rotation and velocity are independent —
/// use this behavior for free-drifting movement.
/// </summary>
/// <example>
/// <code>
/// var movement = new MovementBehavior(gameObject);
/// movement.IsMovingForwards = input.IsKeyDown(Key.W);
/// movement.IsMovingBackwards = input.IsKeyDown(Key.S);
/// Vector2f newVelocity = movement.UpdateMovement(dt, velocity, position);
/// </code>
/// </example>
public sealed class MovementBehavior
{
    private readonly IMovableGameObject _gameObject;

    /// <summary>Initializes the behavior for <paramref name="gameObject"/>.</summary>
    public MovementBehavior(IMovableGameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);
        _gameObject = gameObject;
    }

    /// <summary>Accelerate along the current heading.</summary>
    public bool IsMovingForwards { get; set; }

    /// <summary>Decelerate or reverse along the current heading.</summary>
    public bool IsMovingBackwards { get; set; }

    /// <summary>
    /// Bleed speed along the current velocity direction using <see cref="IMovableGameObject.BrakingDeceleration"/>.
    /// Speed is clamped to zero — the object will never reverse.
    /// Ignored when <see cref="IsMovingForwards"/> is also set.
    /// </summary>
    public bool IsBraking { get; set; }

    /// <summary>Strafe perpendicular-left relative to heading.</summary>
    public bool IsStrafingLeft { get; set; }

    /// <summary>Strafe perpendicular-right relative to heading.</summary>
    public bool IsStrafingRight { get; set; }

    /// <summary>
    /// Computes and returns the new velocity vector after applying all forces.
    /// </summary>
    /// <param name="deltaTime">Elapsed seconds since the last frame.</param>
    /// <param name="currentVelocity">Velocity from the previous frame.</param>
    /// <param name="currentPosition">
    /// Position from the previous frame (unused internally, passed for extensibility).
    /// </param>
    public Vector2f UpdateMovement(float deltaTime, Vector2f currentVelocity, Vector2f currentPosition)
    {
        float mass = MathF.Max(_gameObject.Mass, float.Epsilon);

        Vector2f accelerationForce = new(0f, 0f);

        if (IsMovingForwards)
        {
            accelerationForce += _gameObject.Heading * _gameObject.Acceleration;
        }
        else if (IsMovingBackwards)
        {
            accelerationForce -= _gameObject.Heading * _gameObject.BrakingDeceleration;
        }

        if (IsStrafingLeft)
        {
            accelerationForce -= _gameObject.Side * _gameObject.Acceleration;
        }
        else if (IsStrafingRight)
        {
            accelerationForce += _gameObject.Side * _gameObject.Acceleration;
        }

        Vector2f newVelocity = currentVelocity + (accelerationForce / mass * deltaTime);

        if (IsBraking && !IsMovingForwards)
        {
            // Decelerate along the current velocity direction; never reverse.
            float currentSpeed = newVelocity.Length;
            if (currentSpeed > 0f)
            {
                float decelerationAmount = _gameObject.BrakingDeceleration / mass * deltaTime;
                float newSpeed = MathF.Max(0f, currentSpeed - decelerationAmount);
                newVelocity = newVelocity.Normalized() * newSpeed;
            }
        }
        else if (!IsMovingForwards && !IsMovingBackwards && !IsStrafingLeft && !IsStrafingRight)
        {
            // Passive deceleration when no directional input is active.
            float currentSpeed = newVelocity.Length;
            if (currentSpeed > 0f)
            {
                float decelerationAmount = _gameObject.Deceleration / mass * deltaTime;
                float newSpeed = MathF.Max(0f, currentSpeed - decelerationAmount);
                newVelocity = newVelocity.Normalized() * newSpeed;
            }
        }

        // Clamp to maximum speed.
        float speed = newVelocity.Length;
        if (speed > _gameObject.MaxSpeed)
        {
            newVelocity = newVelocity.Normalized() * _gameObject.MaxSpeed;
        }

        return newVelocity;
    }
}
