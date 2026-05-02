using GrayHare.GameEngine.Extensions;
using SFML.System;

namespace GrayHare.GameEngine.Behaviors;

/// <summary>
/// Combines rotation and velocity into a single update where velocity always
/// aligns with the heading (no drift).
/// Inputs: <see cref="IsMovingForwards"/>, <see cref="IsBraking"/>,
/// <see cref="IsTurningLeft"/>, <see cref="IsTurningRight"/>.
/// </summary>
/// <example>
/// <code>
/// var movement = new MovementWithRotationBehavior(gameObject);
/// movement.IsMovingForwards = input.IsKeyDown(Key.W);
/// movement.IsBraking        = input.IsKeyDown(Key.S);
/// movement.IsTurningLeft    = input.IsKeyDown(Key.A);
/// movement.IsTurningRight   = input.IsKeyDown(Key.D);
/// Vector2f newVelocity      = movement.Update(dt, ref rotation, ref heading);
/// </code>
/// </example>
public sealed class MovementWithRotationBehavior
{
    private readonly IMovableGameObject _gameObject;
    private float _currentSpeed;

    /// <summary>Initializes the behavior for <paramref name="gameObject"/>.</summary>
    public MovementWithRotationBehavior(IMovableGameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        _gameObject = gameObject;
        _currentSpeed = 0f;
    }

    /// <summary>Accelerate in the heading direction.</summary>
    public bool IsMovingForwards { get; set; }

    /// <summary>Apply braking force (cannot reverse).</summary>
    public bool IsBraking { get; set; }

    /// <summary>Rotate counterclockwise.</summary>
    public bool IsTurningLeft { get; set; }

    /// <summary>Rotate clockwise.</summary>
    public bool IsTurningRight { get; set; }

    /// <summary>
    /// Updates <paramref name="rotation"/> and <paramref name="heading"/> in place
    /// and returns the new velocity vector.
    /// </summary>
    /// <param name="deltaTime">Elapsed seconds since the last frame.</param>
    /// <param name="rotation">Current rotation in degrees; updated in place.</param>
    /// <param name="heading">Current heading vector; updated in place.</param>
    public Vector2f Update(float deltaTime, ref float rotation, ref Vector2f heading)
    {
        float mass = MathF.Max(_gameObject.Mass, float.Epsilon);

        // --- Rotation ---
        float turnAmount = MathF.Min(_gameObject.TurnRate / mass, _gameObject.MaxTurnRate) * deltaTime;
        if (IsTurningLeft)
        {
            rotation -= turnAmount;
        }
        else if (IsTurningRight)
        {
            rotation += turnAmount;
        }

        if (IsTurningLeft || IsTurningRight)
        {
            heading = rotation.ToVector2f().Normalized();
        }

        // --- Speed ---
        if (IsMovingForwards)
        {
            float accelerationAmount = _gameObject.Acceleration / mass * deltaTime;
            _currentSpeed = MathF.Min(_currentSpeed + accelerationAmount, _gameObject.MaxSpeed);
        }
        else if (IsBraking)
        {
            float brakingAmount = _gameObject.BrakingDeceleration / mass * deltaTime;
            _currentSpeed = MathF.Max(0f, _currentSpeed - brakingAmount);
        }
        else if (_currentSpeed > 0f)
        {
            float decelerationAmount = _gameObject.Deceleration / mass * deltaTime;
            _currentSpeed = MathF.Max(0f, _currentSpeed - decelerationAmount);
        }

        // Velocity always follows heading — no drift.
        return heading * _currentSpeed;
    }
}
