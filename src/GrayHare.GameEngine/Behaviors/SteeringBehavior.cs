using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.Behaviors;

/// <summary>
/// Provides steering-force calculations for autonomous agent navigation.
/// All methods return a <see cref="Vector2f"/> force that can be combined and applied
/// as: <c>acceleration = force / mass → velocity += acceleration * dt → position += velocity * dt</c>.
/// </summary>
public sealed class SteeringBehavior
{
    private readonly IMovableGameObject _gameObject;
    private readonly Random _random = Random.Shared;

    /// <summary>Initializes the behavior for <paramref name="gameObject"/>.</summary>
    public SteeringBehavior(IMovableGameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        _gameObject = gameObject;
    }

    // ── Basic forces ────────────────────────────────────────────────────────

    /// <summary>Steers directly toward <paramref name="targetPosition"/>.</summary>
    public Vector2f Seek(Vector2f targetPosition)
    {
        Vector2f desiredVelocity =
            (targetPosition - _gameObject.Position).Normalized() * _gameObject.MaxSpeed;

        return desiredVelocity - _gameObject.Velocity;
    }

    /// <summary>Steers directly away from <paramref name="targetPosition"/>.</summary>
    public Vector2f Flee(Vector2f targetPosition)
    {
        Vector2f desiredVelocity =
            (_gameObject.Position - targetPosition).Normalized() * _gameObject.MaxSpeed;

        return desiredVelocity - _gameObject.Velocity;
    }

    /// <summary>
    /// Seeks <paramref name="targetPosition"/> but tapers speed as the agent enters
    /// <paramref name="slowingRadius"/>.
    /// </summary>
    public Vector2f Arrive(Vector2f targetPosition, float slowingRadius)
    {
        Vector2f toTarget = targetPosition - _gameObject.Position;
        float distance = toTarget.Length;

        if (distance < float.Epsilon)
        {
            return Constants.Vectors.Zero;
        }

        slowingRadius = MathF.Max(slowingRadius, float.Epsilon);

        float desiredSpeed = distance < slowingRadius
            ? _gameObject.MaxSpeed * (distance / slowingRadius)
            : _gameObject.MaxSpeed;

        Vector2f desiredVelocity = toTarget.Normalized() * desiredSpeed;

        return desiredVelocity - _gameObject.Velocity;
    }

    // ── Prediction forces ────────────────────────────────────────────────────

    /// <summary>Seeks the predicted future position of <paramref name="target"/>.</summary>
    public Vector2f Pursue(IMovableGameObject target)
    {
        Vector2f toTarget = target.Position - _gameObject.Position;
        float relativeHeading = _gameObject.Heading.Dot(target.Heading);

        if (toTarget.Dot(_gameObject.Heading) > 0f && relativeHeading < -0.95f)
        {
            return Seek(target.Position);
        }

        float lookAheadTime = toTarget.Length / (_gameObject.MaxSpeed + target.Speed);

        return Seek(target.Position + (target.Velocity * lookAheadTime));
    }

    /// <summary>Flees the predicted future position of <paramref name="target"/>.</summary>
    public Vector2f Evade(IMovableGameObject target)
    {
        Vector2f toTarget = target.Position - _gameObject.Position;
        float lookAheadTime = toTarget.Length / (_gameObject.MaxSpeed + target.Speed);

        return Flee(target.Position + (target.Velocity * lookAheadTime));
    }

    // ── Pattern forces ───────────────────────────────────────────────────────

    /// <summary>
    /// Generates a smooth random wandering force by displacing a point on a circle
    /// projected ahead of the agent.
    /// </summary>
    /// <param name="wanderAngle">Accumulated wander angle; updated in place each call.</param>
    /// <param name="wanderRadius">Radius of the displacement circle.</param>
    /// <param name="wanderDistance">Distance the circle is projected ahead of the agent.</param>
    public Vector2f Wander(ref float wanderAngle, float wanderRadius, float wanderDistance)
    {
        wanderAngle += ((float)_random.NextDouble() - 0.5f) * 0.5f;
        Vector2f circleCenter = _gameObject.Heading.Normalized() * wanderDistance;
        Vector2f displacement =
            new Vector2f(MathF.Cos(wanderAngle), MathF.Sin(wanderAngle)) * wanderRadius;

        return (circleCenter + displacement) - _gameObject.Velocity;
    }

    // ── Obstacle / wall avoidance ────────────────────────────────────────────

    /// <summary>
    /// Returns a lateral + braking force to dodge the closest obstacle that lies inside
    /// a detection box of length <paramref name="detectionLength"/> ahead of the agent.
    /// </summary>
    /// <param name="obstacles">The list of obstacles to check. Must not be <see langword="null"/>.</param>
    /// <param name="detectionLength">Length of the forward detection box in world units.</param>
    /// <param name="agentRadius">Approximate radius of the agent used for overlap calculations.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="obstacles"/> is <see langword="null"/>.
    /// </exception>
    public Vector2f ObstacleAvoidance(
        IReadOnlyList<IGameObject> obstacles,
        float detectionLength,
        float agentRadius)
    {
        Vector2f heading = _gameObject.Heading;
        Vector2f side = _gameObject.Side;
        Vector2f pos = _gameObject.Position;

        IGameObject? closestObstacle = null;
        float closestLocalX = float.MaxValue;
        float closestLocalY = 0f;
        float closestObstacleRadius = 0f;

        foreach (IGameObject obstacle in obstacles)
        {
            Vector2f toObstacle = obstacle.Position - pos;
            float localX = toObstacle.Dot(heading);

            if (localX < 0f || localX > detectionLength)
            {
                continue;
            }

            float localY = toObstacle.Dot(side);
            FloatRect bounds = obstacle.GlobalBounds;
            float obstacleRadius = MathF.Max(bounds.Width, bounds.Height) / 2f;

            if (MathF.Abs(localY) < agentRadius + obstacleRadius && localX < closestLocalX)
            {
                closestLocalX = localX;
                closestLocalY = localY;
                closestObstacle = obstacle;
                closestObstacleRadius = obstacleRadius;
            }
        }

        if (closestObstacle is null)
        {
            return Constants.Vectors.Zero;
        }

        float urgency = 1f - (closestLocalX / detectionLength);
        float combinedRadius = agentRadius + closestObstacleRadius;
        float overlap = combinedRadius - MathF.Abs(closestLocalY);

        float lateralSign = closestLocalY > 0f ? -1f : 1f;
        Vector2f lateralSteering =
            side * lateralSign * (overlap / combinedRadius) * _gameObject.MaxSpeed;
        Vector2f brakingSteering = -heading * urgency * _gameObject.MaxSpeed * 0.5f;

        return lateralSteering + brakingSteering;
    }

    /// <summary>
    /// Projects three feelers ahead and returns a force pushing away from the closest
    /// wall intersection.  The center feeler has length <paramref name="feelerLength"/>;
    /// the angled feelers are 75 % of that length.
    /// </summary>
    /// <remarks>
    /// Only walls whose normal faces the agent (dot product of agent-to-wall and normal ≥ 0)
    /// are considered.  This prevents back-face intersections from pushing the agent into a wall.
    /// For interior walls that must be solid on both sides, add a second <see cref="Wall"/>
    /// with Start and End swapped so the back face has its own outward-pointing normal.
    /// </remarks>
    /// <param name="walls">The list of walls to check. Must not be <see langword="null"/>.</param>
    /// <param name="feelerLength">Length of the center detection feeler.</param>
    /// <param name="feelerAngle">Angle in degrees of the two angled feelers relative to the heading.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="walls"/> is <see langword="null"/>.
    /// </exception>
    public Vector2f WallAvoidance(
        IReadOnlyList<Wall> walls,
        float feelerLength,
        float feelerAngle)
    {
        if (walls.Count == 0)
        {
            return Constants.Vectors.Zero;
        }

        Vector2f pos = _gameObject.Position;
        Vector2f heading = _gameObject.Heading;
        Angle angle = Angle.FromDegrees(feelerAngle);

        Vector2f[] feelerTips =
        [
            pos + (heading * feelerLength),
            pos + (heading.RotatedBy(angle) * (feelerLength * 0.75f)),
            pos + (heading.RotatedBy(-angle) * (feelerLength * 0.75f)),
        ];

        float closestT = float.MaxValue;
        Vector2f closestNormal = Constants.Vectors.Zero;
        float closestFeelerLength = feelerLength;

        foreach (Vector2f feelerTip in feelerTips)
        {
            float thisFeelerLength = (feelerTip - pos).Length;

            foreach (Wall wall in walls)
            {
                // Only consider walls where the agent is on the normal (front) side.
                // Without this check, back-face intersections return a normal that
                // points INTO the wall and would push the agent through it.
                Vector2f toAgent = pos - wall.Start;
                float agentSide = toAgent.Dot(wall.Normal);

                if (agentSide >= 0f &&
                    wall.TryGetIntersection(pos, feelerTip, out float t) &&
                    t < closestT)
                {
                    closestT = t;
                    closestNormal = wall.Normal;
                    closestFeelerLength = thisFeelerLength;
                }
            }
        }

        if (closestT == float.MaxValue)
        {
            return Constants.Vectors.Zero;
        }

        float overshoot = closestFeelerLength * (1f - closestT);

        return closestNormal * (overshoot / closestFeelerLength) * _gameObject.MaxSpeed;
    }

    /// <summary>Returns a restoring force toward the inside of <paramref name="boundary"/>.</summary>
    public Vector2f StayWithinBounds(FloatRect boundary, float margin)
    {
        Vector2f steeringForce = Constants.Vectors.Zero;
        Vector2f position = _gameObject.Position;

        if (position.X < boundary.Left + margin)
        {
            float distance = position.X - boundary.Left;
            float strength = (margin - distance) / margin;
            steeringForce += new Vector2f(1f, 0f) * _gameObject.MaxSpeed * strength;
        }
        else if (position.X > boundary.Left + boundary.Width - margin)
        {
            float distance = boundary.Left + boundary.Width - position.X;
            float strength = (margin - distance) / margin;
            steeringForce += new Vector2f(-1f, 0f) * _gameObject.MaxSpeed * strength;
        }

        if (position.Y < boundary.Top + margin)
        {
            float distance = position.Y - boundary.Top;
            float strength = (margin - distance) / margin;
            steeringForce += new Vector2f(0f, 1f) * _gameObject.MaxSpeed * strength;
        }
        else if (position.Y > boundary.Top + boundary.Height - margin)
        {
            float distance = boundary.Top + boundary.Height - position.Y;
            float strength = (margin - distance) / margin;
            steeringForce += new Vector2f(0f, -1f) * _gameObject.MaxSpeed * strength;
        }

        return steeringForce;
    }

    // ── Multi-agent forces ───────────────────────────────────────────────────

    /// <summary>
    /// Arrives at an offset position that is fixed relative to a leader's local coordinate frame.
    /// The <paramref name="offset"/> is expressed in the leader's local space:
    /// <c>X</c> is along the leader's heading (positive = forward) and
    /// <c>Y</c> is along the leader's side vector (positive = right).
    /// </summary>
    /// <param name="leader">The leader to follow.</param>
    /// <param name="offset">Desired offset in the leader's local frame.</param>
    /// <param name="slowingRadius">Arrival slowing radius for smooth formation-slot entry.</param>
    /// <example>
    /// <code>
    /// // Station the agent 80 units behind and 40 units to the left of the leader.
    /// var force = steering.OffsetPursuit(leader, new Vector2f(-80f, -40f), 20f);
    /// </code>
    /// </example>
    public Vector2f OffsetPursuit(IMovableGameObject leader, Vector2f offset, float slowingRadius)
    {
        // Transform the desired slot from leader-local space into world space.
        Vector2f offsetWorldPos =
            leader.Position + (leader.Heading * offset.X) + (leader.Side * offset.Y);

        // Predict where the slot will be when the agent can reach it.
        Vector2f toOffset = offsetWorldPos - _gameObject.Position;
        float lookAheadTime = toOffset.Length /
            MathF.Max(_gameObject.MaxSpeed + leader.Speed, float.Epsilon);
        Vector2f predictedTarget = offsetWorldPos + (leader.Velocity * lookAheadTime);

        return Arrive(predictedTarget, slowingRadius);
    }

    /// <summary>
    /// Arrives at the predicted midpoint between two moving objects.
    /// </summary>
    public Vector2f Interpose(IMovableGameObject object1, IMovableGameObject object2)
    {
        Vector2f midPoint = (object1.Position + object2.Position) / 2f;
        float timeToReachMidPoint =
            _gameObject.Position.DistanceTo(midPoint) /
            MathF.Max(_gameObject.MaxSpeed, float.Epsilon);

        Vector2f predicted1 = object1.Position + (object1.Velocity * timeToReachMidPoint);
        Vector2f predicted2 = object2.Position + (object2.Velocity * timeToReachMidPoint);
        Vector2f interposeTarget = (predicted1 + predicted2) / 2f;
        float slowingRadius = (predicted1 - predicted2).Length / 2f;

        return Arrive(interposeTarget, slowingRadius);
    }

    /// <summary>
    /// Moves to a hiding spot behind the nearest obstacle relative to
    /// <paramref name="target"/> when within <paramref name="threatDistance"/>.
    /// Falls back to <see cref="Evade"/> when no obstacle is available.
    /// </summary>
    public Vector2f Hide(
        IMovableGameObject target,
        IReadOnlyList<IGameObject> obstacles,
        float distanceFromBoundary,
        float threatDistance)
    {
        if (_gameObject.Position.DistanceTo(target.Position) > threatDistance)
        {
            return Constants.Vectors.Zero;
        }

        Vector2f bestHidingSpot = Constants.Vectors.Zero;
        float distanceToClosest = float.MaxValue;

        foreach (IGameObject obstacle in obstacles)
        {
            FloatRect bounds = obstacle.GlobalBounds;
            float radius = MathF.Max(bounds.Width, bounds.Height) + distanceFromBoundary;
            Vector2f toObstacle = (obstacle.Position - target.Position).Normalized();
            Vector2f hidingSpot = obstacle.Position + (toObstacle * radius);
            float distance = hidingSpot.DistanceTo(_gameObject.Position);

            if (distance < distanceToClosest)
            {
                distanceToClosest = distance;
                bestHidingSpot = hidingSpot;
            }
        }

        return distanceToClosest >= float.MaxValue
            ? Evade(target)
            : Arrive(bestHidingSpot, 5f);
    }

    /// <summary>
    /// Follows a list of waypoints using <see cref="Seek"/> toward each point and
    /// <see cref="Arrive"/> when entering <paramref name="slowingRadius"/> of the current waypoint.
    /// </summary>
    /// <param name="pathToFollowIndex">Current waypoint index; incremented in place when reached.</param>
    /// <param name="pathToFollow">Ordered list of waypoints. Must not be <see langword="null"/>.</param>
    /// <param name="slowingRadius">Arrival slowing radius per waypoint.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pathToFollow"/> is <see langword="null"/>.
    /// </exception>
    public Vector2f FollowPath(
        ref int pathToFollowIndex,
        IReadOnlyList<Vector2f> pathToFollow,
        float slowingRadius)
    {
        if (pathToFollow.Count == 0 || pathToFollowIndex >= pathToFollow.Count)
        {
            return Constants.Vectors.Zero;
        }

        Vector2f waypoint = pathToFollow[pathToFollowIndex];
        float distance = _gameObject.Position.DistanceTo(waypoint);

        if (distance < slowingRadius)
        {
            FloatRect size = _gameObject.GlobalBounds;
            float agentRadius = MathF.Max(size.Width, size.Height) / 2f;
            pathToFollowIndex++;

            return Arrive(waypoint, agentRadius);
        }

        return Seek(waypoint);
    }

    // ── Group behaviors (Flocking) ────────────────────────────────────────────

    /// <summary>
    /// Returns a force that steers away from nearby flockmates within
    /// <paramref name="separationRadius"/>. Closer neighbors produce a stronger push.
    /// </summary>
    /// <param name="neighbors">
    /// Flockmates within the agent's neighborhood, excluding itself.
    /// Pre-filter by distance before calling to control the neighborhood size.
    /// Must not be <see langword="null"/>.
    /// </param>
    /// <param name="separationRadius">
    /// Personal-space radius; neighbors inside this distance are repelled.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="neighbors"/> is <see langword="null"/>.
    /// </exception>
    public Vector2f Separation(IReadOnlyList<IMovableGameObject> neighbors, float separationRadius)
    {
        Vector2f steeringForce = Constants.Vectors.Zero;

        foreach (IMovableGameObject neighbor in neighbors)
        {
            Vector2f toAgent = _gameObject.Position - neighbor.Position;
            float distance = toAgent.Length;

            if (distance > float.Epsilon && distance < separationRadius)
            {
                // Inverse-distance weighting: closer neighbors push harder.
                steeringForce += toAgent.Normalized() * (separationRadius / distance);
            }
        }

        return steeringForce.Truncate(_gameObject.MaxSpeed);
    }

    /// <summary>
    /// Returns a force that steers the agent to match the average heading of
    /// <paramref name="neighbors"/>.
    /// </summary>
    /// <param name="neighbors">
    /// Flockmates within the agent's neighborhood, excluding itself.
    /// </param>
    public Vector2f Alignment(IReadOnlyList<IMovableGameObject> neighbors)
    {
        if (neighbors.Count == 0)
        {
            return Constants.Vectors.Zero;
        }

        Vector2f avgHeading = Constants.Vectors.Zero;

        foreach (IMovableGameObject neighbor in neighbors)
        {
            avgHeading += neighbor.Heading;
        }

        Vector2f desired = (avgHeading / (float)neighbors.Count).Normalized() * _gameObject.MaxSpeed;

        return desired - _gameObject.Velocity;
    }

    /// <summary>
    /// Returns a force that steers toward the center of mass of <paramref name="neighbors"/>.
    /// </summary>
    /// <param name="neighbors">
    /// Flockmates within the agent's neighborhood, excluding itself.
    /// </param>
    public Vector2f Cohesion(IReadOnlyList<IMovableGameObject> neighbors)
    {
        if (neighbors.Count == 0)
        {
            return Constants.Vectors.Zero;
        }

        Vector2f centerOfMass = Constants.Vectors.Zero;

        foreach (IMovableGameObject neighbor in neighbors)
        {
            centerOfMass += neighbor.Position;
        }

        return Seek(centerOfMass / (float)neighbors.Count);
    }

    // ── Heading helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Rotates the agent's heading toward the direction of its current velocity,
    /// clamped to <see cref="IMovableGameObject.TurnRate"/> so heading can never
    /// flip in a single frame.
    /// </summary>
    /// <param name="deltaTime">Elapsed seconds since the last frame.</param>
    /// <param name="rotation">Current rotation in degrees; updated in place.</param>
    /// <returns>The new normalized heading vector.</returns>
    public Vector2f UpdateHeadingWhileMoving(float deltaTime, ref float rotation)
    {
        Vector2f desiredHeading = _gameObject.Velocity.Normalized();

        float cross = (_gameObject.Heading.X * desiredHeading.Y) -
                      (_gameObject.Heading.Y * desiredHeading.X);
        float dot = _gameObject.Heading.Dot(desiredHeading);
        float angleDeg = MathF.Atan2(cross, dot) * (180f / MathF.PI);

        float turnDeg = Math.Clamp(
            angleDeg,
            -_gameObject.TurnRate * deltaTime,
            _gameObject.TurnRate * deltaTime);

        float rad = turnDeg * (MathF.PI / 180f);
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);

        Vector2f heading = new Vector2f(
            (_gameObject.Heading.X * cos) - (_gameObject.Heading.Y * sin),
            (_gameObject.Heading.X * sin) + (_gameObject.Heading.Y * cos)).Normalized();

        rotation = heading.Angle().WrapUnsigned().Degrees;

        return heading;
    }

}
