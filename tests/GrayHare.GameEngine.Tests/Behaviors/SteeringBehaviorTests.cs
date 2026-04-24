using GrayHare.GameEngine.Behaviors;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.Tests.Behaviors;

public sealed class SteeringBehaviorTests
{
    [Fact]
    public void Seek_ReturnsForceTowardTarget()
    {
        var obj = new FakeMovable
        {
            Position = new Vector2f(0f, 0f),
            Velocity = Constants.Vectors.Zero,
            Heading = new Vector2f(1f, 0f),
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(obj);

        Vector2f force = steering.Seek(new Vector2f(200f, 0f));

        Assert.True(force.X > 0f);
        Assert.True(MathF.Abs(force.Y) < float.Epsilon);
    }

    [Fact]
    public void Flee_ReturnsForceAwayFromTarget()
    {
        var obj = new FakeMovable
        {
            Position = new Vector2f(0f, 0f),
            Velocity = Constants.Vectors.Zero,
            Heading = new Vector2f(1f, 0f),
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(obj);

        Vector2f force = steering.Flee(new Vector2f(200f, 0f));

        Assert.True(force.X < 0f);
    }

    [Fact]
    public void Arrive_ReturnsZero_WhenAlreadyAtTarget()
    {
        var obj = new FakeMovable
        {
            Position = new Vector2f(100f, 100f),
            Velocity = Constants.Vectors.Zero,
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(obj);

        Vector2f force = steering.Arrive(new Vector2f(100f, 100f), 50f);

        Assert.Equal(Constants.Vectors.Zero, force);
    }

    [Fact]
    public void StayWithinBounds_ReturnsPositiveXForce_WhenLeftOfBoundary()
    {
        var obj = new FakeMovable
        {
            Position = new Vector2f(5f, 100f),
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(obj);

        Vector2f force = steering.StayWithinBounds(
            new FloatRect(new Vector2f(0f, 0f), new Vector2f(1280f, 720f)),
            50f);

        Assert.True(force.X > 0f);
    }


    [Fact]
    public void FollowPath_ReturnsZero_WhenPathIsEmpty()
    {
        var obj = new FakeMovable { Position = new Vector2f(0f, 0f) };
        var steering = new SteeringBehavior(obj);
        int index = 0;

        Vector2f force = steering.FollowPath(ref index, [], 20f);

        Assert.Equal(Constants.Vectors.Zero, force);
    }

    // ── Pursue ───────────────────────────────────────────────────────────────

    [Fact]
    public void Pursue_ReturnsForceTowardTarget_WhenTargetIsAheadAndMovingAway()
    {
        var agent = new FakeMovable
        {
            Position = new Vector2f(0f, 0f),
            Velocity = Constants.Vectors.Zero,
            Heading = new Vector2f(1f, 0f),
            MaxSpeed = 100f
        };
        var target = new FakeMovable
        {
            Position = new Vector2f(200f, 0f),
            Velocity = new Vector2f(50f, 0f),
            Heading = new Vector2f(1f, 0f),
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.Pursue(target);

        Assert.True(force.X > 0f);
    }

    [Fact]
    public void Pursue_SeeksDirectly_WhenTargetIsFacingAgent()
    {
        // relativeHeading < -0.95 and target is ahead → direct seek
        var agent = new FakeMovable
        {
            Position = new Vector2f(0f, 0f),
            Velocity = Constants.Vectors.Zero,
            Heading = new Vector2f(1f, 0f),
            MaxSpeed = 100f
        };
        var target = new FakeMovable
        {
            Position = new Vector2f(200f, 0f),
            Velocity = new Vector2f(-50f, 0f),
            Heading = new Vector2f(-1f, 0f),  // facing directly at agent
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.Pursue(target);

        Assert.True(force.X > 0f);
    }

    // ── Evade ────────────────────────────────────────────────────────────────

    [Fact]
    public void Evade_ReturnsForceAway_FromApproachingTarget()
    {
        var agent = new FakeMovable
        {
            Position = new Vector2f(0f, 0f),
            Velocity = Constants.Vectors.Zero,
            MaxSpeed = 100f
        };
        var pursuer = new FakeMovable
        {
            Position = new Vector2f(200f, 0f),
            Velocity = new Vector2f(-100f, 0f),  // moving toward agent
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.Evade(pursuer);

        Assert.True(force.X < 0f);
    }

    // ── Wander ───────────────────────────────────────────────────────────────

    [Fact]
    public void Wander_ReturnsNonZeroForce()
    {
        var agent = new FakeMovable
        {
            Heading = new Vector2f(1f, 0f),
            Velocity = Constants.Vectors.Zero,
            MaxSpeed = 200f
        };
        var steering = new SteeringBehavior(agent);
        float wanderAngle = 0f;

        Vector2f force = steering.Wander(ref wanderAngle, 50f, 100f);

        Assert.True(force.Length > float.Epsilon);
    }

    [Fact]
    public void Wander_ModifiesWanderAngle_WithinExpectedBounds()
    {
        var agent = new FakeMovable { Heading = new Vector2f(1f, 0f), MaxSpeed = 100f };
        var steering = new SteeringBehavior(agent);
        float wanderAngle = 0f;

        steering.Wander(ref wanderAngle, 50f, 100f);

        // Each call perturbs the angle by at most ±0.25 radians
        Assert.InRange(wanderAngle, -0.25f, 0.25f);
    }

    // ── ObstacleAvoidance ────────────────────────────────────────────────────

    [Fact]
    public void ObstacleAvoidance_ReturnsZero_WhenNoObstacles()
    {
        var agent = new FakeMovable
        {
            Position = new Vector2f(0f, 0f),
            Heading = new Vector2f(1f, 0f),
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.ObstacleAvoidance([], 100f, 10f);

        Assert.Equal(Constants.Vectors.Zero, force);
    }

    [Fact]
    public void ObstacleAvoidance_ReturnsNonZeroForce_WhenObstacleIsAhead()
    {
        var agent = new FakeMovable
        {
            Position = new Vector2f(0f, 0f),
            Heading = new Vector2f(1f, 0f),
            MaxSpeed = 100f
        };
        // Obstacle directly on the heading axis, within detection range
        var obstacle = new FakeGameObject
        {
            Position = new Vector2f(50f, 0f),
            GlobalBounds = new FloatRect(new Vector2f(40f, -10f), new Vector2f(20f, 20f))
        };
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.ObstacleAvoidance([obstacle], 100f, 15f);

        Assert.True(force.Length > 0f);
    }

    [Fact]
    public void ObstacleAvoidance_ReturnsZero_WhenObstacleIsBehind()
    {
        var agent = new FakeMovable
        {
            Position = new Vector2f(0f, 0f),
            Heading = new Vector2f(1f, 0f),
            MaxSpeed = 100f
        };
        var obstacle = new FakeGameObject
        {
            Position = new Vector2f(-50f, 0f),  // behind agent
            GlobalBounds = new FloatRect(new Vector2f(-60f, -10f), new Vector2f(20f, 20f))
        };
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.ObstacleAvoidance([obstacle], 100f, 15f);

        Assert.Equal(Constants.Vectors.Zero, force);
    }

    // ── WallAvoidance ────────────────────────────────────────────────────────

    [Fact]
    public void WallAvoidance_ReturnsZero_WhenNoWalls()
    {
        var agent = new FakeMovable
        {
            Position = new Vector2f(0f, 0f),
            Heading = new Vector2f(1f, 0f),
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.WallAvoidance([], 100f, 45f);

        Assert.Equal(Constants.Vectors.Zero, force);
    }

    [Fact]
    public void WallAvoidance_ReturnsNonZeroForce_WhenWallIsAhead()
    {
        var agent = new FakeMovable
        {
            Position = new Vector2f(0f, 0f),
            Heading = new Vector2f(1f, 0f),
            MaxSpeed = 100f
        };
        // Vertical wall at x=50, spanning y=-50 to y=50 — directly in the center feeler path
        var wall = new Wall(new Vector2f(50f, -50f), new Vector2f(50f, 50f));
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.WallAvoidance([wall], 80f, 45f);

        Assert.True(force.Length > 0f);
    }

    // ── OffsetPursuit ────────────────────────────────────────────────────────

    [Fact]
    public void OffsetPursuit_ReturnsForceTowardOffsetSlot_WhenAgentIsBehindLeader()
    {
        var leader = new FakeMovable
        {
            Position = new Vector2f(200f, 0f),
            Velocity = new Vector2f(50f, 0f),
            Heading = new Vector2f(1f, 0f),
            MaxSpeed = 100f
        };
        var agent = new FakeMovable
        {
            Position = new Vector2f(0f, 0f),
            Velocity = Constants.Vectors.Zero,
            Heading = new Vector2f(1f, 0f),
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(agent);

        // Zero offset → slot is right at leader position; agent needs to move right
        Vector2f force = steering.OffsetPursuit(leader, new Vector2f(0f, 0f), 20f);

        Assert.True(force.X > 0f);
    }

    // ── Interpose ────────────────────────────────────────────────────────────

    [Fact]
    public void Interpose_ReturnsForceTowardMidpoint_WhenAgentIsAbove()
    {
        var obj1 = new FakeMovable
        {
            Position = new Vector2f(0f, 0f),
            Velocity = Constants.Vectors.Zero,
            MaxSpeed = 50f
        };
        var obj2 = new FakeMovable
        {
            Position = new Vector2f(200f, 0f),
            Velocity = Constants.Vectors.Zero,
            MaxSpeed = 50f
        };
        // Agent above midpoint (100, 0) — should receive downward force (Y > 0 in SFML)
        var agent = new FakeMovable
        {
            Position = new Vector2f(100f, -100f),
            Velocity = Constants.Vectors.Zero,
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.Interpose(obj1, obj2);

        Assert.True(force.Y > 0f);
    }

    // ── Hide ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Hide_ReturnsZero_WhenAgentIsBeyondThreatDistance()
    {
        var threat = new FakeMovable
        {
            Position = new Vector2f(0f, 0f),
            Velocity = Constants.Vectors.Zero,
            MaxSpeed = 100f
        };
        var agent = new FakeMovable
        {
            Position = new Vector2f(500f, 0f),
            Velocity = Constants.Vectors.Zero,
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.Hide(threat, [], 20f, 200f);

        Assert.Equal(Constants.Vectors.Zero, force);
    }

    [Fact]
    public void Hide_ReturnsEvadeForce_WhenWithinThreatDistanceAndNoObstacles()
    {
        var threat = new FakeMovable
        {
            Position = new Vector2f(0f, 0f),
            Velocity = new Vector2f(0f, 100f),  // moving downward
            MaxSpeed = 100f
        };
        // Agent directly above threat — evade should push further upward (Y < 0 in SFML)
        var agent = new FakeMovable
        {
            Position = new Vector2f(0f, -100f),
            Velocity = Constants.Vectors.Zero,
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.Hide(threat, [], 20f, 300f);

        Assert.True(force.Y < 0f);
    }

    [Fact]
    public void Hide_ReturnsForceTowardHidingSpot_WhenObstacleIsAvailable()
    {
        var threat = new FakeMovable
        {
            Position = new Vector2f(0f, 0f),
            Velocity = Constants.Vectors.Zero,
            MaxSpeed = 100f
        };
        // Obstacle to the right; hiding spot will be further right of it
        var obstacle = new FakeGameObject
        {
            Position = new Vector2f(200f, 0f),
            GlobalBounds = new FloatRect(new Vector2f(180f, -20f), new Vector2f(40f, 40f))
        };
        // Agent between threat and obstacle
        var agent = new FakeMovable
        {
            Position = new Vector2f(100f, 0f),
            Velocity = Constants.Vectors.Zero,
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(agent);

        // threatDistance large enough to include agent; hiding spot is right of obstacle
        Vector2f force = steering.Hide(threat, [obstacle], 20f, 300f);

        Assert.True(force.X > 0f);
    }

    // ── Separation ───────────────────────────────────────────────────────────

    [Fact]
    public void Separation_ReturnsZero_WhenNoNeighbors()
    {
        var agent = new FakeMovable { Position = new Vector2f(0f, 0f), MaxSpeed = 100f };
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.Separation([], 50f);

        Assert.Equal(Constants.Vectors.Zero, force);
    }

    [Fact]
    public void Separation_ReturnsForceAway_WhenNeighborIsWithinSeparationRadius()
    {
        var agent = new FakeMovable { Position = new Vector2f(0f, 0f), MaxSpeed = 100f };
        var neighbor = new FakeMovable { Position = new Vector2f(10f, 0f), MaxSpeed = 100f };
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.Separation([neighbor], 50f);

        // Neighbor is to the right, so force pushes agent left
        Assert.True(force.X < 0f);
    }

    [Fact]
    public void Separation_ReturnsZero_WhenNeighborIsOutsideSeparationRadius()
    {
        var agent = new FakeMovable { Position = new Vector2f(0f, 0f), MaxSpeed = 100f };
        var neighbor = new FakeMovable { Position = new Vector2f(100f, 0f), MaxSpeed = 100f };
        var steering = new SteeringBehavior(agent);

        // separationRadius = 20, neighbor at distance 100 → outside radius
        Vector2f force = steering.Separation([neighbor], 20f);

        Assert.Equal(Constants.Vectors.Zero, force);
    }

    // ── Alignment ────────────────────────────────────────────────────────────

    [Fact]
    public void Alignment_ReturnsZero_WhenNoNeighbors()
    {
        var agent = new FakeMovable { MaxSpeed = 100f };
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.Alignment([]);

        Assert.Equal(Constants.Vectors.Zero, force);
    }

    [Fact]
    public void Alignment_ReturnsForceTowardAverageNeighborHeading()
    {
        // Agent heading right, neighbors heading downward (+Y in SFML)
        var agent = new FakeMovable
        {
            Velocity = Constants.Vectors.Zero,
            Heading = new Vector2f(1f, 0f),
            MaxSpeed = 100f
        };
        var neighbor = new FakeMovable { Heading = new Vector2f(0f, 1f), MaxSpeed = 100f };
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.Alignment([neighbor]);

        // Desired heading is (0,1), force steers downward
        Assert.True(force.Y > 0f);
    }

    // ── Cohesion ─────────────────────────────────────────────────────────────

    [Fact]
    public void Cohesion_ReturnsZero_WhenNoNeighbors()
    {
        var agent = new FakeMovable { Position = new Vector2f(0f, 0f), MaxSpeed = 100f };
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.Cohesion([]);

        Assert.Equal(Constants.Vectors.Zero, force);
    }

    [Fact]
    public void Cohesion_ReturnsForceTowardCenterOfMass()
    {
        var agent = new FakeMovable
        {
            Position = new Vector2f(0f, 0f),
            Velocity = Constants.Vectors.Zero,
            MaxSpeed = 100f
        };
        var neighbor = new FakeMovable { Position = new Vector2f(200f, 0f), MaxSpeed = 100f };
        var steering = new SteeringBehavior(agent);

        Vector2f force = steering.Cohesion([neighbor]);

        // Center of mass is to the right
        Assert.True(force.X > 0f);
    }

    // ── UpdateHeadingWhileMoving ──────────────────────────────────────────────

    [Fact]
    public void UpdateHeadingWhileMoving_RotatesHeadingTowardVelocityDirection()
    {
        // Heading right, velocity pointing downward → heading should rotate toward downward
        var agent = new FakeMovable
        {
            Heading = new Vector2f(1f, 0f),
            Velocity = new Vector2f(0f, 100f),
            TurnRate = 360f,
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(agent);
        float rotation = 0f;

        Vector2f newHeading = steering.UpdateHeadingWhileMoving(1f, ref rotation);

        Assert.True(newHeading.Y > 0f);
    }

    [Fact]
    public void UpdateHeadingWhileMoving_ReturnsNormalizedVector()
    {
        var agent = new FakeMovable
        {
            Heading = new Vector2f(1f, 0f),
            Velocity = new Vector2f(50f, 50f),
            TurnRate = 180f,
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(agent);
        float rotation = 0f;

        Vector2f newHeading = steering.UpdateHeadingWhileMoving(0.1f, ref rotation);

        Assert.True(MathF.Abs(newHeading.Length - 1f) < 0.001f);
    }

    [Fact]
    public void UpdateHeadingWhileMoving_UpdatesRotationField()
    {
        var agent = new FakeMovable
        {
            Heading = new Vector2f(1f, 0f),
            Velocity = new Vector2f(0f, 100f),  // pointing downward
            TurnRate = 360f,
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(agent);
        float rotation = 0f;

        steering.UpdateHeadingWhileMoving(1f, ref rotation);

        Assert.True(rotation > 0f);
    }

    // ── StayWithinBounds ─────────────────────────────────────────────────────

    [Fact]
    public void StayWithinBounds_ReturnsZero_WhenInsideBoundary()
    {
        var obj = new FakeMovable
        {
            Position = new Vector2f(640f, 360f),  // center of bounds
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(obj);

        Vector2f force = steering.StayWithinBounds(
            new FloatRect(new Vector2f(0f, 0f), new Vector2f(1280f, 720f)),
            50f);

        Assert.Equal(Constants.Vectors.Zero, force);
    }

    [Fact]
    public void StayWithinBounds_ReturnsNegativeXForce_WhenRightOfBoundary()
    {
        var obj = new FakeMovable
        {
            Position = new Vector2f(1275f, 360f),  // within right margin (50px)
            MaxSpeed = 100f
        };
        var steering = new SteeringBehavior(obj);

        Vector2f force = steering.StayWithinBounds(
            new FloatRect(new Vector2f(0f, 0f), new Vector2f(1280f, 720f)),
            50f);

        Assert.True(force.X < 0f);
    }

    // ── FollowPath ────────────────────────────────────────────────────────────

    [Fact]
    public void FollowPath_ReturnsZero_WhenIndexExceedsPath()
    {
        var obj = new FakeMovable { Position = new Vector2f(0f, 0f), MaxSpeed = 100f };
        var steering = new SteeringBehavior(obj);
        int index = 5;

        Vector2f force = steering.FollowPath(
            ref index,
            [new Vector2f(100f, 0f)],
            20f);

        Assert.Equal(Constants.Vectors.Zero, force);
    }

    [Fact]
    public void FollowPath_AdvancesIndex_WhenWithinSlowingRadiusOfWaypoint()
    {
        // Place agent just inside the slowing radius of the current waypoint.
        var obj = new FakeMovable
        {
            Position = new Vector2f(95f, 0f),  // 5 units from waypoint at (100,0)
            MaxSpeed = 100f,
            GlobalBounds = new FloatRect(new Vector2f(-10f, -10f), new Vector2f(20f, 20f))
        };
        var steering = new SteeringBehavior(obj);
        int index = 0;
        Vector2f waypoint = new(100f, 0f);

        // slowingRadius = 20, distance = 5 → within radius → index advances
        steering.FollowPath(ref index, [waypoint, new Vector2f(200f, 0f)], 20f);

        Assert.Equal(1, index);
    }
}
