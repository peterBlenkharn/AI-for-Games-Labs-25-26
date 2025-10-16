using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quadtree manager that owns the root node and provides a small API.
/// Specialised for <see cref="Boid"/> to keep things simple for students.
/// </summary>
public class BoidQuadtree
{
    public readonly int nodeCapacity; // Note: 'readonly' means no changes after construction.
    public readonly int maxDepth;

    private BoidQuadNode root;

    /// <summary>
    /// Create a quadtree covering 'rootBounds' with the given capacity and depth.
    /// </summary>
    public BoidQuadtree(AABB rootBounds, int nodeCapacity, int maxDepth)
    {
        this.nodeCapacity = Mathf.Max(1, nodeCapacity);
        this.maxDepth = Mathf.Max(0, maxDepth);
        root = new BoidQuadNode(rootBounds, 0, this.nodeCapacity, this.maxDepth);
    }

    /// <summary>
    /// Clear and rebuild with a new root bounds (used each frame in this demo for clarity).
    /// </summary>
    public void Clear(AABB newRootBounds)
    {
        root = new BoidQuadNode(newRootBounds, 0, nodeCapacity, maxDepth);
    }

    /// <summary>Insert a boid (the node chooses whether to subdivide).</summary>
    public void Insert(Boid boid) => root.Insert(boid);

    /// <summary>Query all boids whose centres lie in (or on) the given AABB.</summary>
    public void Query(AABB area, List<Boid> results) => root.Query(area, results);

    /// <summary>Count all nodes (useful for debugging / teaching).</summary>
    public int CountNodes() => root.CountNodes();

    /// <summary>Draw the whole tree as Scene view gizmos.</summary>
    public void DrawGizmos() => root.DrawGizmos(maxDepth);
}
