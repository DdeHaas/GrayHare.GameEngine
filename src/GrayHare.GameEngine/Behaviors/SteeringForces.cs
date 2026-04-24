using GrayHare.GameEngine.Extensions;
using SFML.System;

namespace GrayHare.GameEngine.Behaviors;

/// <summary>
/// Static utility class providing two strategies for combining multiple steering forces
/// into a single resultant force.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="WeightedSum"/> accumulates all forces simultaneously, scaled by individual weights.
/// Giving safety behaviors a higher weight ensures they dominate even when a goal or wander force
/// happens to point toward a hazard. Use this for avoidance-dominated scenarios where forces
/// can oppose each other.
/// </para>
/// <para>
/// <see cref="PriorityTruncated"/> fills a force budget in strict priority order so that a
/// higher-priority behavior fully consumes the budget before any lower-priority behavior can act.
/// This guarantees <b>zero behavioral overlap</b>: the same force capacity can never be claimed
/// by more than one behavior. It works best when high-priority forces are either fully active
/// (magnitude near the budget) or completely inactive (magnitude zero), such as flocking forces
/// that naturally point in independent directions. Avoid it when a low-priority force (e.g.,
/// wander) can directly oppose a high-priority avoidance force — in that case the remaining
/// budget given to wander would counteract the avoidance, causing the agent to drift into hazards.
/// </para>
/// </remarks>
public static class SteeringForces
{
    /// <summary>
    /// Combines forces by computing a weighted sum and truncating the result to
    /// <paramref name="maxForce"/>. All forces contribute simultaneously; they may
    /// reinforce or oppose each other.
    /// </summary>
    /// <param name="maxForce">The maximum length of the returned force vector.</param>
    /// <param name="entries">Force-weight pairs. Each force is scaled by its weight before summation.</param>
    /// <returns>The combined force, capped at <paramref name="maxForce"/>.</returns>
    /// <example>
    /// <code>
    /// // Flocking: all three social forces blend together, separation weighted highest.
    /// Vector2f force = SteeringForces.WeightedSum(
    ///     agent.MaxSpeed,
    ///     (steering.Separation(neighbors, 50f), 4f),
    ///     (steering.Alignment(neighbors), 3f),
    ///     (steering.Cohesion(neighbors), 2f),
    ///     (steering.Wander(ref wanderAngle, 50f, 100f), 1f));
    /// </code>
    /// </example>
    public static Vector2f WeightedSum(
        float maxForce,
        params ReadOnlySpan<(Vector2f Force, float Weight)> entries)
    {
        Vector2f total = Constants.Vectors.Zero;

        foreach ((Vector2f force, float weight) in entries)
        {
            total += force * weight;
        }

        return total.Truncate(maxForce);
    }

    /// <summary>
    /// Combines forces by filling a force <paramref name="budget"/> in strict priority order.
    /// The first force has the highest priority and is satisfied first; once the budget is
    /// exhausted all subsequent forces contribute nothing.
    /// </summary>
    /// <param name="budget">Total force capacity available (typically <c>agent.MaxSpeed</c>).</param>
    /// <param name="forces">
    /// Forces in descending priority order. Pass the most critical behavior first
    /// (e.g., collision avoidance), and the least critical last (e.g., wandering).
    /// </param>
    /// <returns>
    /// The combined force whose magnitude is at most <paramref name="budget"/>.
    /// </returns>
    /// <remarks>
    /// Because each unit of force capacity can only be allocated once, no two behaviors
    /// can act on the same portion of the output — guaranteeing zero behavioral overlap.
    /// A zero-length input force is skipped without consuming any budget.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Hard safety constraints run first; wandering only acts when budget remains.
    /// Vector2f force = SteeringForces.PriorityTruncated(
    ///     agent.MaxSpeed,
    ///     steering.WallAvoidance(walls, 80f, 45f),          // highest priority
    ///     steering.ObstacleAvoidance(obstacles, 120f, 14f),
    ///     steering.StayWithinBounds(bounds, 30f),
    ///     steering.Wander(ref wanderAngle, 50f, 100f));      // lowest priority
    /// </code>
    /// </example>
    public static Vector2f PriorityTruncated(float budget, params ReadOnlySpan<Vector2f> forces)
    {
        Vector2f result = Constants.Vectors.Zero;
        float remaining = budget;

        foreach (Vector2f force in forces)
        {
            // No budget left — all remaining forces contribute nothing.
            if (remaining <= float.Epsilon)
            {
                break;
            }

            float magnitude = force.Length;

            // Skip zero-length forces; they carry no information and cost no budget.
            if (magnitude <= float.Epsilon)
            {
                continue;
            }

            if (magnitude <= remaining)
            {
                // Force fits entirely within the remaining budget.
                result += force;
                remaining -= magnitude;
            }
            else
            {
                // Partially fill the remaining budget in the direction of this force.
                result += force.Normalized() * remaining;
                remaining = 0f;
            }
        }

        return result;
    }
}
