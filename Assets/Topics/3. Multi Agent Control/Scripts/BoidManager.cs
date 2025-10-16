using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns and manages a flock of <see cref="Boid"/>s, collects references
/// to <see cref="Obstacle"/>s, computes toroidal wrapping from an
/// orthographic camera, and owns a per-frame quadtree for fast neighbour queries.
/// 
/// For teaching simplicity, the quadtree is rebuilt each frame (robust and clear).
/// </summary>
public class BoidManager : MonoBehaviour
{
    // ---------- Spawning ----------

    [Header("Spawn")]
    [Tooltip("Boid prefab that has a Sprite + Boid component.")]
    public Boid boidPrefab;

    [Tooltip("Number of boids to spawn.")]
    public int boidCount = 200;

    [Tooltip("Random seed (0 = different every play).")]
    public int randomSeed = 0;

    // ---------- Obstacles ----------

    [Header("Obstacles")]
    [Tooltip("Optional: parent transform containing obstacle instances (with Obstacle component).")]
    public Transform obstaclesParent;

    // ---------- Camera & Bounds ----------

    [Header("Camera & Bounds")]
    [Tooltip("Orthographic camera used to define the toroidal bounds.")]
    public Camera targetCamera;

    [Tooltip("Padding outside the visible area to avoid immediate wrap flicker.")]
    public float boundsPadding = 0.2f;

    // ---------- Quadtree Settings ----------

    [Header("Quadtree")]
    [Tooltip("Max number of boids in a leaf before it subdivides.")]
    public int quadNodeCapacity = 8;

    [Tooltip("Maximum subdivision depth of the quadtree.")]
    public int quadMaxDepth = 8;

    [Tooltip("Draw the quadtree cells in the Scene view (when this manager is selected).")]
    public bool drawQuadtreeGizmos = false;

    // ---------- Public Collections ----------

    [HideInInspector] public readonly List<Boid> boids = new List<Boid>();
    [HideInInspector] public readonly List<Obstacle> obstacles = new List<Obstacle>();

    // ---------- Internals ----------

    private float _minX, _maxX, _minY, _maxY;

    /// <summary>Size of the toroidal world (computed from the camera).</summary>
    public Vector2 WorldSize => new Vector2(_maxX - _minX, _maxY - _minY);

    // Per-frame quadtree and a reusable query buffer (to avoid allocations).
    private BoidQuadtree _quadtree;
    private readonly List<Boid> _queryBuffer = new List<Boid>(128);

    // ---------- Unity Lifecycle ----------

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
    }

    private void Start()
    {
        if (randomSeed != 0) Random.InitState(randomSeed);

        ComputeWorldBounds();

        // Gather obstacles (either under a parent or across the whole scene).
        obstacles.Clear();
        if (obstaclesParent != null)
        {
            obstacles.AddRange(obstaclesParent.GetComponentsInChildren<Obstacle>());
        }
        else
        {
            obstacles.AddRange(FindObjectsOfType<Obstacle>());
        }

        // Spawn boids.
        boids.Clear();
        for (int i = 0; i < boidCount; i++)
        {
            Vector2 pos = new Vector2(
                Random.Range(_minX, _maxX),
                Random.Range(_minY, _maxY)
            );

            Boid b = Instantiate(boidPrefab, pos, Quaternion.identity, transform);
            b.manager = this;
            b.velocity = Random.insideUnitCircle.normalized * (b.maxSpeed * 0.5f);

            boids.Add(b);
        }

        // Build initial quadtree.
        RebuildQuadtree();
    }

    private void Update()
    {
        // Update bounds (handles window/aspect changes).
        ComputeWorldBounds();

        // Rebuild quadtree from current boid positions.
        RebuildQuadtree();
    }

    // ---------- Quadtree API for Boids ----------

    /// <summary>
    /// Fill 'results' with boids whose positions fall within an AABB centred at 'centre'
    /// with half-size (radius, radius). This is a broad-phase; the boid filters by exact
    /// radii for each behaviour afterwards.
    /// </summary>
    public void QueryNeighbours(Vector2 centre, float radius, List<Boid> results)
    {
        results.Clear();

        // Build a query AABB around the boid.
        AABB area = new AABB(centre, new Vector2(radius, radius));
        _quadtree.Query(area, results);
    }

    // ---------- Bounds & Wrap ----------

    private void ComputeWorldBounds()
    {
        if (targetCamera == null) return;

        // OrthographicSize is half the vertical size in world units.
        float vertExtent = targetCamera.orthographicSize;
        float horzExtent = vertExtent * targetCamera.aspect;

        _minX = targetCamera.transform.position.x - horzExtent - boundsPadding;
        _maxX = targetCamera.transform.position.x + horzExtent + boundsPadding;
        _minY = targetCamera.transform.position.y - vertExtent - boundsPadding;
        _maxY = targetCamera.transform.position.y + vertExtent + boundsPadding;
    }

    /// <summary>Wraps a Transform toroidally inside the world rectangle.</summary>
    public void WrapTransform(Transform tf)
    {
        Vector3 p = tf.position;

        if (p.x > _maxX) p.x = _minX;
        else if (p.x < _minX) p.x = _maxX;

        if (p.y > _maxY) p.y = _minY;
        else if (p.y < _minY) p.y = _maxY;

        tf.position = p;
    }

    // ---------- Quadtree Build & Gizmos ----------

    private void RebuildQuadtree()
    {
        // Root AABB covers the world rect.
        Vector2 centre = new Vector2((_minX + _maxX) * 0.5f, (_minY + _maxY) * 0.5f);
        Vector2 half = new Vector2((_maxX - _minX) * 0.5f, (_maxY - _minY) * 0.5f);

        if (_quadtree == null)
            _quadtree = new BoidQuadtree(new AABB(centre, half), quadNodeCapacity, quadMaxDepth);
        else
            _quadtree.Clear(new AABB(centre, half));

        // Insert all boids.
        for (int i = 0; i < boids.Count; i++)
            _quadtree.Insert(boids[i]);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw world bounds
        if (targetCamera == null) targetCamera = Camera.main;
        ComputeWorldBounds();

        Gizmos.color = Color.cyan;
        Vector3 centre = new Vector3((_minX + _maxX) * 0.5f, (_minY + _maxY) * 0.5f, 0f);
        Vector3 size = new Vector3(_maxX - _minX, _maxY - _minY, 0f);
        Gizmos.DrawWireCube(centre, size);

        // Optionally draw the quadtree (Editor Scene view aid).
        if (!Application.isPlaying || !drawQuadtreeGizmos) return;

        // The quadtree knows how to draw itself.
        _quadtree.DrawGizmos();
    }
}
