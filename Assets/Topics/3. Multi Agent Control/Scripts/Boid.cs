using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A simple 2D Boid that uses classic flocking steering behaviours
/// (Separation, Alignment, Cohesion), plus Obstacle Avoidance and
/// a gentle Wander perturbation. Designed for clarity and teaching.
/// 
/// This version queries nearby boids through the manager’s quadtree
/// (one query per boid per frame using the largest sensing radius),
/// then filters that neighbour set for each behaviour. This replaces
/// the O(N²) “scan every other boid” approach with a fast broad-phase.
/// 
/// Attach this to a 2D Sprite (e.g., a small triangle or arrow).
/// It expects a <see cref="BoidManager"/> in the scene.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Boid : MonoBehaviour
{
    // ---------- Links ----------

    /// <summary>
    /// Reference to the scene’s <see cref="BoidManager"/>.
    /// Set automatically when spawned by the manager.
    /// </summary>
    [Header("Links")]
    [Tooltip("Reference to the BoidManager in the scene.")]
    public BoidManager manager;

    // ---------- Motion ----------

    /// <summary>Maximum movement speed in world units per second.</summary>
    [Header("Motion")]
    [Tooltip("Maximum movement speed (world units per second).")]
    public float maxSpeed = 5f;

    /// <summary>
    /// Maximum steering force (acceleration) applied per frame.
    /// Limits how sharply the boid can change its velocity.
    /// </summary>
    [Tooltip("Maximum steering force (acceleration) applied per step.")]
    public float maxForce = 8f;

    /// <summary>The boid’s current velocity.</summary>
    [Tooltip("Current velocity (read-only in Inspector).")]
    public Vector2 velocity;

    // ---------- Neighbourhood Radii ----------

    /// <summary>Separation (personal space) radius.</summary>
    [Header("Neighbourhood Radii")]
    [Tooltip("Radius for Separation (smaller, personal space).")]
    public float separationRadius = 0.6f;

    /// <summary>Alignment (match heading) radius.</summary>
    [Tooltip("Radius for Alignment (medium).")]
    public float alignmentRadius = 1.2f;

    /// <summary>Cohesion (group) radius.</summary>
    [Tooltip("Radius for Cohesion (larger).")]
    public float cohesionRadius = 1.6f;

    // ---------- Weights ----------

    [Header("Weights")]
    [Tooltip("Weight for Separation force.")]
    public float separationWeight = 1.2f;

    [Tooltip("Weight for Alignment force.")]
    public float alignmentWeight = 1.0f;

    [Tooltip("Weight for Cohesion force.")]
    public float cohesionWeight = 0.9f;

    // ---------- Obstacle Avoidance ----------

    [Header("Obstacle Avoidance")]
    [Tooltip("Base radius on the boid used for avoidance trigger building.")]
    public float obstacleAvoidRadius = 1.2f;

    [Tooltip("How far from an obstacle edge the boid senses and starts to avoid. Defaults to obstacleAvoidRadius if 0.")]
    public float obstacleSenseRadius = 0f; // 0 => use obstacleAvoidRadius

    [Tooltip("Weight for obstacle avoidance force.")]
    public float obstacleAvoidWeight = 1.4f;

    private enum FalloffType { Linear, Quadratic, Smoothstep }

    [SerializeField, Tooltip("How avoidance strength decays with distance from the obstacle edge.")]
    private FalloffType obstacleFalloff = FalloffType.Quadratic;

    // ---------- Wander ----------

    [Header("Wander (gentle randomising force)")]
    [Tooltip("Weight for wander steering (keep small).")]
    public float wanderWeight = 0.4f;

    [Tooltip("How quickly wander noise changes over time.")]
    public float wanderNoiseSpeed = 0.8f;

    [Tooltip("Max angle offset (degrees) applied to the current heading.")]
    public float wanderAngleRangeDeg = 30f;

    // ---------- Teaching Gizmos ----------

    [Header("Gizmos")]
    [Tooltip("Show sensing radii when this boid is selected.")]
    public bool showSensingGizmos = true;

    [Tooltip("Also draw force arrows when selected and in Play mode.")]
    public bool showForceArrows = true;

    [Tooltip("Scales arrow length so they’re visible.")]
    public float gizmoForceScale = 0.5f;

    [Tooltip("Size of the arrow heads.")]
    public float gizmoArrowHeadSize = 0.15f;

    // ---------- Internals ----------

    private Transform _tf;
    private float _wanderSeed;

    // Per-frame forces (for gizmos)
    private Vector2 _fSep, _fAli, _fCoh, _fAvoid, _fWan;
    private Vector2 _fSteerRaw, _fSteerClamped;

    // A tiny reusable neighbour buffer to avoid per-frame allocations.
    // Unity will show this in the Inspector; that’s fine for teaching.
    [System.NonSerialized] private readonly List<Boid> _neighbourBuffer = new List<Boid>(64);

    /// <summary>Convenience: current world position as Vector2 (for quadtree and maths).</summary>
    public Vector2 Position => (Vector2)transform.position;

    // ---------- Unity Lifecycle ----------

    private void Awake()
    {
        _tf = transform;                 // Cache the Transform (common Unity optimisation).
        _wanderSeed = Random.value * 1000f;
    }

    private void Start()
    {
        // Small random initial velocity so they don’t all start static.
        if (velocity.sqrMagnitude < 0.01f)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            velocity = randomDir * (maxSpeed * 0.5f);
        }
    }

    private void Update()
    {
        if (manager == null) return;

        float dt = Time.deltaTime;

        // --- 1) Query neighbours once via quadtree (use the largest sensing radius) ---
        float maxSense = Mathf.Max(separationRadius, Mathf.Max(alignmentRadius, cohesionRadius));
        _neighbourBuffer.Clear();
        manager.QueryNeighbours(Position, maxSense, _neighbourBuffer);

        // --- 2) Compute steering contributions using the same neighbour set (filter per behaviour) ---
        _fSep = ComputeSeparation(_neighbourBuffer);
        _fAli = ComputeAlignment(_neighbourBuffer);
        _fCoh = ComputeCohesion(_neighbourBuffer);
        _fAvoid = ComputeObstacleAvoidance(manager.obstacles);
        _fWan = ComputeWander();

        // --- 3) Sum with weights (blended steering) and clamp acceleration ---
        _fSteerRaw = _fSep * separationWeight;
        _fSteerRaw += _fAli * alignmentWeight;
        _fSteerRaw += _fCoh * cohesionWeight;
        _fSteerRaw += _fAvoid * obstacleAvoidWeight;
        _fSteerRaw += _fWan * wanderWeight;

        _fSteerClamped = Limit(_fSteerRaw, maxForce);

        // --- 4) Integrate and cap speed ---
        velocity += _fSteerClamped * dt;
        velocity = Limit(velocity, maxSpeed);
        _tf.position += (Vector3)(velocity * dt);

        // --- 5) Face direction of travel (optional but helpful) ---
        if (velocity.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            _tf.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        // --- 6) Wrap across screen edges (toroidal) ---
        manager.WrapTransform(_tf);
    }

    // ---------- Steering Behaviours (now take a neighbour list) ----------

    /// <summary>Separation: steer away from boids closer than <see cref="separationRadius"/>.</summary>
    private Vector2 ComputeSeparation(List<Boid> neighbours)
    {
        Vector2 force = Vector2.zero;
        int count = 0;

        for (int i = 0; i < neighbours.Count; i++)
        {
            Boid other = neighbours[i];
            if (other == this) continue;

            float d = ToroidalDistance(_tf.position, other.transform.position);
            if (d > 0f && d < separationRadius)
            {
                Vector2 away = (Vector2)(ToroidalVector(_tf.position, other.transform.position) * -1f);
                if (away.sqrMagnitude > 0.0001f)
                {
                    away = away.normalized / Mathf.Max(d, 0.0001f); // stronger when closer
                    force += away;
                    count++;
                }
            }
        }

        if (count > 0) force /= count;

        if (force.sqrMagnitude > 0.0001f)
        {
            force = force.normalized * maxSpeed - velocity;
        }

        return force;
    }

    /// <summary>Alignment: match the average heading of boids within <see cref="alignmentRadius"/>.</summary>
    private Vector2 ComputeAlignment(List<Boid> neighbours)
    {
        Vector2 avgVel = Vector2.zero;
        int count = 0;

        for (int i = 0; i < neighbours.Count; i++)
        {
            Boid other = neighbours[i];
            if (other == this) continue;

            float d = ToroidalDistance(_tf.position, other.transform.position);
            if (d < alignmentRadius)
            {
                avgVel += other.velocity;
                count++;
            }
        }

        if (count > 0)
        {
            avgVel /= count;
            if (avgVel.sqrMagnitude > 0.0001f)
            {
                Vector2 desired = avgVel.normalized * maxSpeed;
                return desired - velocity;
            }
        }

        return Vector2.zero;
    }

    /// <summary>Cohesion: move towards the average position of boids within <see cref="cohesionRadius"/>.</summary>
    private Vector2 ComputeCohesion(List<Boid> neighbours)
    {
        Vector2 centre = Vector2.zero;
        int count = 0;

        for (int i = 0; i < neighbours.Count; i++)
        {
            Boid other = neighbours[i];
            if (other == this) continue;

            float d = ToroidalDistance(_tf.position, other.transform.position);
            if (d < cohesionRadius)
            {
                // Use toroidal-aware position so wrapping edges do not skew the average.
                Vector2 curPos = (Vector2)_tf.position;
                Vector2 toOther = ToroidalVector(curPos, other.transform.position);
                Vector2 wrappedNeighbourPos = curPos + toOther;

                centre += wrappedNeighbourPos;
                count++;
            }
        }

        if (count > 0)
        {
            centre /= count;
            Vector2 desired = (centre - (Vector2)_tf.position);
            if (desired.sqrMagnitude > 0.0001f)
            {
                desired = desired.normalized * maxSpeed;
                return desired - velocity;
            }
        }

        return Vector2.zero;
    }

    /// <summary>
    /// Obstacle avoidance (edge-aware) as before. Obstacles are few, so we keep a simple scan here.
    /// </summary>
    private Vector2 ComputeObstacleAvoidance(List<Obstacle> obstacles)
    {
        if (obstacles == null || obstacles.Count == 0) return Vector2.zero;

        float sense = obstacleSenseRadius > 0f ? obstacleSenseRadius : obstacleAvoidRadius;

        Vector2 accum = Vector2.zero;
        int count = 0;

        for (int i = 0; i < obstacles.Count; i++)
        {
            Obstacle ob = obstacles[i];

            float obstacleRadius = ob.obstacleRadius;
            CircleCollider2D cc = ob.GetComponent<CircleCollider2D>();
            if (cc != null)
            {
                obstacleRadius = cc.radius * Mathf.Abs(ob.transform.lossyScale.x);
            }

            Vector2 toObstacle = ToroidalVector(_tf.position, ob.transform.position);
            float centreDist = toObstacle.magnitude;
            if (centreDist <= 0.0001f) continue;

            float edgeDist = centreDist - obstacleRadius; // negative => inside
            float senseBand = Mathf.Max(0f, sense + ob.influenceRadius);

            if (edgeDist >= 0f && edgeDist < senseBand)
            {
                float t = Mathf.Clamp01(edgeDist / senseBand);               // 0 at edge, 1 at outer band
                float strength = EvaluateFalloff(1f - t, obstacleFalloff);   // 1 near edge, 0 far

                Vector2 away = (-toObstacle).normalized;
                accum += away * strength;
                count++;
            }
            else if (edgeDist < 0f)
            {
                Vector2 away = (-toObstacle).normalized;
                accum += away * 2.0f; // firm shove out of the obstacle
                count++;
            }
        }

        if (count == 0) return Vector2.zero;

        Vector2 avgDir = accum / count;
        if (avgDir.sqrMagnitude < 0.0001f) return Vector2.zero;

        Vector2 desired = avgDir.normalized * maxSpeed;
        return desired - velocity;
    }

    /// <summary>Wander: Perlin-noise-driven heading perturbation.</summary>
    private Vector2 ComputeWander()
    {
        Vector2 baseDir = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : Vector2.right;

        float t = Time.time * wanderNoiseSpeed + _wanderSeed;
        float n = Mathf.PerlinNoise(t, 0f); // Smooth value 0..1
        float angleOffsetDeg = Mathf.Lerp(-wanderAngleRangeDeg, wanderAngleRangeDeg, n);

        Vector2 rotated = Rotate(baseDir, angleOffsetDeg * Mathf.Deg2Rad);
        Vector2 desired = rotated * maxSpeed;
        return desired - velocity;
    }

    // ---------- Utility ----------

    private static Vector2 Limit(Vector2 v, float max)
    {
        float mag = v.magnitude;
        if (mag > max && mag > 0f) return v * (max / mag);
        return v;
    }

    private static Vector2 Rotate(Vector2 v, float radians)
    {
        float s = Mathf.Sin(radians);
        float c = Mathf.Cos(radians);
        return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
    }

    private Vector2 ToroidalVector(Vector2 fromPos, Vector2 toPos)
    {
        Vector2 size = manager.WorldSize;
        Vector2 delta = toPos - fromPos;

        if (delta.x > size.x * 0.5f) delta.x -= size.x;
        if (delta.x < -size.x * 0.5f) delta.x += size.x;

        if (delta.y > size.y * 0.5f) delta.y -= size.y;
        if (delta.y < -size.y * 0.5f) delta.y += size.y;

        return delta;
    }

    private float ToroidalDistance(Vector2 a, Vector2 b)
    {
        return ToroidalVector(a, b).magnitude;
    }

    private static float EvaluateFalloff(float x, FalloffType type)
    {
        switch (type)
        {
            case FalloffType.Linear: return x;
            case FalloffType.Quadratic: return x * x;
            case FalloffType.Smoothstep: return x * x * (3f - 2f * x);
            default: return x;
        }
    }

    // ---------- Teaching Gizmos ----------

    private void OnDrawGizmosSelected()
    {
        if (!showSensingGizmos && !showForceArrows) return;

        // Sensing radii (Edit + Play)
        if (showSensingGizmos)
        {
            Vector3 p = transform.position;

            Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.9f);
            Gizmos.DrawWireSphere(p, separationRadius);

            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.9f);
            Gizmos.DrawWireSphere(p, alignmentRadius);

            Gizmos.color = new Color(0.35f, 0.6f, 1f, 0.9f);
            Gizmos.DrawWireSphere(p, cohesionRadius);

            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
            float sense = obstacleSenseRadius > 0f ? obstacleSenseRadius : obstacleAvoidRadius;
            Gizmos.DrawWireSphere(p, sense);
        }

        // Force arrows (Play only)
        if (showForceArrows && Application.isPlaying)
        {
            Vector3 origin = transform.position;

            DrawArrow(origin, _fSep * gizmoForceScale, new Color(1f, 0.2f, 0.2f, 0.95f));  // Separation
            DrawArrow(origin, _fAli * gizmoForceScale, new Color(0.2f, 1f, 0.4f, 0.95f));  // Alignment
            DrawArrow(origin, _fCoh * gizmoForceScale, new Color(0.3f, 0.55f, 1f, 0.95f)); // Cohesion
            DrawArrow(origin, _fAvoid * gizmoForceScale, new Color(1f, 0.7f, 0.25f, 0.95f)); // Obstacle
            DrawArrow(origin, _fWan * gizmoForceScale, new Color(0.8f, 0.6f, 1f, 0.95f));  // Wander

            // Overall steering (clamped) — bright yellow
            DrawArrow(origin, _fSteerClamped * gizmoForceScale, new Color(1f, 1f, 0.2f, 1f));
        }
    }

    /// <summary>Draws a simple 2D arrow using Gizmos.</summary>
    private void DrawArrow(Vector3 from, Vector2 vec, Color colour)
    {
        if (vec.sqrMagnitude < 0.000001f) return;

        Gizmos.color = colour;

        Vector3 to = from + new Vector3(vec.x, vec.y, 0f);
        Gizmos.DrawLine(from, to);

        // Arrow head
        Vector2 dir = vec.normalized;
        float headLen = gizmoArrowHeadSize;
        float headAngle = 25f * Mathf.Deg2Rad;

        Vector2 left = Rotate(-dir, headAngle) * headLen;
        Vector2 right = Rotate(-dir, -headAngle) * headLen;

        Gizmos.DrawLine(to, to + new Vector3(left.x, left.y, 0f));
        Gizmos.DrawLine(to, to + new Vector3(right.x, right.y, 0f));
    }
}
