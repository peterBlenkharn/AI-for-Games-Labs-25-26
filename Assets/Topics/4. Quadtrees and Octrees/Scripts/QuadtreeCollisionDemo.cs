using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Demonstrates broad-phase pruning using a Quadtree:
/// - Rebuilds the quadtree each frame
/// - Gathers collision candidates per body via a small AABB query (broad-phase)
/// - Runs exact distance checks only on those candidates (narrow-phase)
/// - Deduplicates pairs so A,B is checked once only
///
/// New in this version:
/// - Colliding bodies' gizmos draw in red for that frame
/// - Bodies also flash their material red briefly on collision
/// - Inspector exposes timing and FPS for naïve vs quadtree paths, plus node count
/// </summary>
public class QuadtreeCollisionDemo : MonoBehaviour
{
    [Header("World Bounds (XY)")]
    public Vector2 worldCentre = Vector2.zero;
    public Vector2 worldHalfSize = new Vector2(6f, 3.5f);

    [Header("Bodies")]
    public int bodyCount = 120;
    public Body bodyPrefab;

    [Header("Quadtree Settings")]
    public int nodeCapacity = 4;
    public int maxDepth = 7;

    [Header("Collision Settings")]
    [Tooltip("If true, use Quadtree broad-phase; if false, use naïve all-pairs.")]
    public bool useQuadtree = true;

    [Tooltip("Multiplier to inflate the query box (safety margin).")]
    [Range(1f, 3f)]
    public float queryInflation = 1.25f;

    [Tooltip("How long the material should flash red on collision (seconds).")]
    [Range(0.05f, 0.6f)]
    public float hitFlashSeconds = 0.15f;

    [Header("Visuals")]
    public bool drawQuadtree = true;
    public bool drawCandidateLinks = true;
    public bool drawCollisions = true;

    [Header("Metrics (read-only)")]
    [Tooltip("Number of candidate pairs considered by the current method this frame.")]
    public int lastCandidatePairs;

    [Tooltip("Number of pairs that actually overlapped this frame.")]
    public int lastCollisionPairs;

    [Tooltip("Total nodes (quads) in the current quadtree.")]
    public int lastNodeCount;

    [Tooltip("Broad-phase time (ms) for the naïve method (0 when using quadtree).")]
    public float lastNaiveMs;

    [Tooltip("Broad-phase time (ms) for the quadtree method (0 when using naïve).")]
    public float lastQuadtreeMs;

    [Tooltip("Approx broad-phase FPS for the naïve method (1000/ms).")]
    public float lastNaiveFps;

    [Tooltip("Approx broad-phase FPS for the quadtree method (1000/ms).")]
    public float lastQuadtreeFps;

    private readonly List<Body> bodies = new List<Body>();
    private Quadtree quadtree;
    private readonly List<Body> queryBuffer = new List<Body>(128);

    // To avoid duplicate checks, store unordered pair keys (minId,maxId)
    private readonly HashSet<ulong> pairSet = new HashSet<ulong>();
    private readonly List<(Body a, Body b)> collisionPairs = new List<(Body, Body)>();
    private readonly List<(Body a, Body b)> candidatePairs = new List<(Body, Body)>();

    private float maxBodyRadius;

    private void Start()
    {
        if (bodyPrefab == null)
        {
            Debug.LogError("Please assign a Body prefab.");
            enabled = false; return;
        }

        // Spawn
        for (int i = 0; i < bodyCount; i++)
        {
            Vector2 r = new Vector2(
                Random.Range(-worldHalfSize.x, worldHalfSize.x),
                Random.Range(-worldHalfSize.y, worldHalfSize.y)
            );
            Body b = Instantiate(bodyPrefab, new Vector3(worldCentre.x + r.x, worldCentre.y + r.y, 0f), Quaternion.identity, transform);
            b.direction = Random.insideUnitCircle.normalized;
            bodies.Add(b);
        }

        // Create initial tree
        quadtree = new Quadtree(new AABB(worldCentre, worldHalfSize), nodeCapacity, maxDepth);
        RecomputeMaxRadius();
    }

    private void RecomputeMaxRadius()
    {
        maxBodyRadius = 0f;
        for (int i = 0; i < bodies.Count; i++)
            if (bodies[i].radius > maxBodyRadius) maxBodyRadius = bodies[i].radius;
    }

    private void Update()
    {
        // Keep inside bounds (reflect at edges)
        Vector2 min = worldCentre - worldHalfSize;
        Vector2 max = worldCentre + worldHalfSize;
        foreach (var b in bodies)
        {
            Vector2 p = b.Position;
            if (p.x - b.radius < min.x || p.x + b.radius > max.x) b.direction.x *= -1f;
            if (p.y - b.radius < min.y || p.y + b.radius > max.y) b.direction.y *= -1f;

            // Clear per-frame collision flag here; Body.Update also resets as a backup.
            b.isColliding = false;
        }

        // Rebuild quadtree
        quadtree.Clear(new AABB(worldCentre, worldHalfSize));
        for (int i = 0; i < bodies.Count; i++)
            quadtree.Insert(bodies[i]);
        lastNodeCount = quadtree.CountNodes();

        // Run collision systems
        if (useQuadtree) RunQuadtreeBroadphase();
        else RunNaiveAllPairs();

        // Apply collision visuals: set per-body isColliding and trigger hit flash.
        for (int i = 0; i < collisionPairs.Count; i++)
        {
            var (a, b) = collisionPairs[i];
            a.isColliding = true;
            b.isColliding = true;
            a.FlashHit(hitFlashSeconds);
            b.FlashHit(hitFlashSeconds);
        }
    }

    private void RunNaiveAllPairs()
    {
        var start = Time.realtimeSinceStartup;
        candidatePairs.Clear();
        collisionPairs.Clear();

        for (int i = 0; i < bodies.Count; i++)
        {
            Body a = bodies[i];
            for (int j = i + 1; j < bodies.Count; j++)
            {
                Body b = bodies[j];
                candidatePairs.Add((a, b));
                float r = a.radius + b.radius;
                if ((a.Position - b.Position).sqrMagnitude <= r * r)
                    collisionPairs.Add((a, b));
            }
        }

        lastCandidatePairs = candidatePairs.Count;
        lastCollisionPairs = collisionPairs.Count;
        lastNaiveMs = (Time.realtimeSinceStartup - start) * 1000f;
        lastQuadtreeMs = 0f;

        // Simple instantaneous FPS estimate for the broad-phase section.
        lastNaiveFps = lastNaiveMs > 0f ? 1000f / lastNaiveMs : 0f;
        lastQuadtreeFps = 0f;
    }

    private void RunQuadtreeBroadphase()
    {
        var start = Time.realtimeSinceStartup;
        candidatePairs.Clear();
        collisionPairs.Clear();
        pairSet.Clear();

        for (int i = 0; i < bodies.Count; i++)
        {
            Body a = bodies[i];

            // Broad-phase query half-size: radius of 'a' + worst-case other radius, with a safety inflation.
            float half = (a.radius + maxBodyRadius) * queryInflation;
            AABB area = new AABB(a.Position, new Vector2(half, half));

            queryBuffer.Clear();
            quadtree.Query(area, queryBuffer);

            // Narrow-phase only with candidates; dedupe via unordered pair key.
            int idA = a.GetInstanceID();
            for (int j = 0; j < queryBuffer.Count; j++)
            {
                Body b = queryBuffer[j];
                if (ReferenceEquals(a, b)) continue;

                int idB = b.GetInstanceID();
                int minId = idA < idB ? idA : idB;
                int maxId = idA < idB ? idB : idA;
                ulong key = ((ulong)(uint)minId << 32) | (uint)maxId;

                if (!pairSet.Add(key)) continue;

                candidatePairs.Add((a, b));
                float r = a.radius + b.radius;
                if ((a.Position - b.Position).sqrMagnitude <= r * r)
                    collisionPairs.Add((a, b));
            }
        }

        lastCandidatePairs = candidatePairs.Count;
        lastCollisionPairs = collisionPairs.Count;
        lastQuadtreeMs = (Time.realtimeSinceStartup - start) * 1000f;
        lastNaiveMs = 0f;

        // Simple instantaneous FPS estimate for the broad-phase section.
        lastQuadtreeFps = lastQuadtreeMs > 0f ? 1000f / lastQuadtreeMs : 0f;
        lastNaiveFps = 0f;
    }

    private void OnDrawGizmos()
    {
        // World bounds
        var corners = new AABB(worldCentre, worldHalfSize).Corners();
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.85f);
        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);

        // Tree
        if (drawQuadtree && quadtree != null)
            quadtree.DrawGizmos();

        // Broad-phase candidate links
        if (drawCandidateLinks && candidatePairs != null)
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
            for (int i = 0; i < candidatePairs.Count; i++)
            {
                var (a, b) = candidatePairs[i];
                Gizmos.DrawLine(
                    new Vector3(a.Position.x, a.Position.y, 0f),
                    new Vector3(b.Position.x, b.Position.y, 0f)
                );
            }
        }

        // Collision links bold/red
        if (drawCollisions && collisionPairs != null)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            for (int i = 0; i < collisionPairs.Count; i++)
            {
                var (a, b) = collisionPairs[i];
                Gizmos.DrawLine(
                    new Vector3(a.Position.x, a.Position.y, 0f),
                    new Vector3(b.Position.x, b.Position.y, 0f)
                );
            }
        }
    }
}
