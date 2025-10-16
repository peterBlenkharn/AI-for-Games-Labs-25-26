using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quadtree manager that owns the root node and provides a small API.
/// </summary>
public class Quadtree
{
    public readonly int nodeCapacity; // ADVANCED NOTE: 'readonly' signals no changes after construction
    public readonly int maxDepth;

    private QuadNode root; //there's a starting 'node' that represents the entire 'world'

    // the AABB (axis-aligned bounding box) defines the 'world' in which you're dividing things up
    public Quadtree(AABB rootBounds, int nodeCapacity, int maxDepth)
    {
        this.nodeCapacity = Mathf.Max(1, nodeCapacity);
        this.maxDepth = Mathf.Max(0, maxDepth);
        root = new QuadNode(rootBounds, 0, this.nodeCapacity, this.maxDepth);
    }

    public void Clear(AABB newRootBounds)
    {
        root = new QuadNode(newRootBounds, 0, nodeCapacity, maxDepth);
    }

    // this tries to insert a body into the root node, which, if full, will subdivide and create new nodes to add things to!
    public void Insert(Body body) => root.Insert(body);

    /// <summary>Query all bodies whose centres lie in (or on) the given area.</summary>
    public void Query(AABB area, List<Body> results) => root.Query(area, results);

    public int CountNodes() => root.CountNodes();

    public void DrawGizmos() => root.DrawGizmos(maxDepth);
}
