using GrayHare.GameEngine.Abstractions;
using SFML.System;

namespace GrayHare.GameEngine.Behaviors;

/// <summary>
/// Composes <see cref="RotationBehavior"/> and <see cref="MovementBehavior"/> so that
/// turning does not instantly redirect momentum (the object drifts).
/// Inputs: <see cref="IsMovingForwards"/>, <see cref="IsBraking"/>,
/// <see cref="IsTurningLeft"/>, <see cref="IsTurningRight"/>.
/// </summary>
/// <example>
/// <code>
/// var drifting = new MovementWithDriftingBehavior(gameObject);
/// drifting.IsMovingForwards = input.IsKeyDown(Key.W);
/// drifting.IsBraking        = input.IsKeyDown(Key.S);
/// drifting.IsTurningLeft    = input.IsKeyDown(Key.A);
/// drifting.IsTurningRight   = input.IsKeyDown(Key.D);
/// Vector2f newVelocity = drifting.Update(dt, ref rotation, ref heading);
/// </code>
/// </example>
public sealed class MovementWithDriftingBehavior
{
    private readonly IMovableGameObject _gameObject;
    private readonly RotationBehavior _rotationBehavior;
    private readonly MovementBehavior _movementBehavior;

    /// <summary>Initializes the behavior for <paramref name="gameObject"/>.</summary>
    public MovementWithDriftingBehavior(IMovableGameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);
        _gameObject = gameObject;
        _rotationBehavior = new RotationBehavior(gameObject);
        _movementBehavior = new MovementBehavior(gameObject);
    }

    /// <summary>Accelerate in the heading direction.</summary>
    public bool IsMovingForwards
    {
        get => _movementBehavior.IsMovingForwards;
        set => _movementBehavior.IsMovingForwards = value;
    }

    /// <summary>
    /// Bleed speed along the current velocity direction.
    /// The object will never reverse.
    /// Ignored when <see cref="IsMovingForwards"/> is also set.
    /// </summary>
    public bool IsBraking
    {
        get => _movementBehavior.IsBraking;
        set => _movementBehavior.IsBraking = value;
    }

    /// <summary>Rotate counterclockwise.</summary>
    public bool IsTurningLeft
    {
        get => _rotationBehavior.IsTurningLeft;
        set => _rotationBehavior.IsTurningLeft = value;
    }

    /// <summary>Rotate clockwise.</summary>
    public bool IsTurningRight
    {
        get => _rotationBehavior.IsTurningRight;
        set => _rotationBehavior.IsTurningRight = value;
    }

    /// <summary>
    /// Updates <paramref name="rotation"/> and <paramref name="heading"/> via the rotation
    /// sub-behavior, then applies the movement sub-behavior and returns the new velocity.
    /// </summary>
    /// <param name="deltaTime">Elapsed seconds since the last frame.</param>
    /// <param name="rotation">Current rotation in degrees; updated in place.</param>
    /// <param name="heading">Current heading vector; updated in place.</param>
    public Vector2f Update(float deltaTime, ref float rotation, ref Vector2f heading)
    {
        rotation = _rotationBehavior.UpdateRotation(deltaTime, ref heading);
        return _movementBehavior.UpdateMovement(deltaTime, _gameObject.Velocity, _gameObject.Position);
    }
}
