using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One node in the quadtree: holds bounds, depth, boids if leaf, or 4 children if subdivided.
/// Specialised for <see cref="Boid"/> to keep the code straightforward for teaching.
/// </summary>
public class BoidQuadNode
{
    // These are set at construction and never change (hence 'readonly').
    public readonly AABB bounds;
    public readonly int depth;

    private readonly int nodeCapacity;
    private readonly int maxDepth;

    private List<Boid> boids;     // Used when this node is a leaf (no children).
    private BoidQuadNode[] children; // 4 children when subdivided; null if leaf.

    /// <summary>True when this node is a leaf (has no children).</summary>
    public bool IsLeaf => children == null;

    public BoidQuadNode(AABB bounds, int depth, int nodeCapacity, int maxDepth)
    {
        this.bounds = bounds;
        this.depth = depth;
        this.nodeCapacity = Mathf.Max(1, nodeCapacity);
        this.maxDepth = Mathf.Max(0, maxDepth);

        boids = new List<Boid>(this.nodeCapacity);
        children = null;
    }

    /// <summary>
    /// Inserts a boid into this node or its children. Subdivides if needed and depth allows.
    /// Returns true if inserted.
    /// </summary>
    public bool Insert(Boid boid)
    {
        if (!bounds.ContainsPoint(boid.Position)) return false;

        // If we are a leaf and have space, store it here.
        if (IsLeaf && boids.Count < nodeCapacity)
        {
            boids.Add(boid);
            return true;
        }

        // If a leaf but full, and we can still subdivide, split and push contents down.
        if (IsLeaf && depth < maxDepth)
        {
            Subdivide();

            for (int i = boids.Count - 1; i >= 0; i--)
            {
                Boid existing = boids[i];
                if (InsertIntoChildren(existing))
                    boids.RemoveAt(i);
            }
        }

        // If we have children, try to insert into one of them.
        if (!IsLeaf && InsertIntoChildren(boid)) return true;

        // Fallback: store here (handles degenerates at max depth).
        boids.Add(boid);
        return true;
    }

    private bool InsertIntoChildren(Boid boid)
    {
        if (children == null) return false;

        for (int i = 0; i < 4; i++)
        {
            if (children[i].bounds.ContainsPoint(boid.Position))
                return children[i].Insert(boid);
        }
        return false;
    }

    private void Subdivide()
    {
        Vector2 centre = bounds.centre;
        Vector2 childHalf = bounds.halfSize * 0.5f;

        children = new BoidQuadNode[4];
        // 0 TL, 1 TR, 2 BL, 3 BR
        children[0] = new BoidQuadNode(new AABB(new Vector2(centre.x - childHalf.x, centre.y + childHalf.y), childHalf), depth + 1, nodeCapacity, maxDepth);
        children[1] = new BoidQuadNode(new AABB(new Vector2(centre.x + childHalf.x, centre.y + childHalf.y), childHalf), depth + 1, nodeCapacity, maxDepth);
        children[2] = new BoidQuadNode(new AABB(new Vector2(centre.x - childHalf.x, centre.y - childHalf.y), childHalf), depth + 1, nodeCapacity, maxDepth);
        children[3] = new BoidQuadNode(new AABB(new Vector2(centre.x + childHalf.x, centre.y - childHalf.y), childHalf), depth + 1, nodeCapacity, maxDepth);
    }

    /// <summary>
    /// Adds all boids whose **centres** lie in (or on) 'area' to 'results'.
    /// Caller supplies 'results' to avoid per-frame allocations.
    /// </summary>
    public void Query(AABB area, List<Boid> results)
    {
        if (!bounds.Intersects(area)) return;

        if (IsLeaf)
        {
            for (int i = 0; i < boids.Count; i++)
            {
                Boid b = boids[i];
                if (area.ContainsPoint(b.Position))
                    results.Add(b);
            }
        }
        else
        {
            for (int i = 0; i < 4; i++)
                children[i].Query(area, results);
        }
    }

    /// <summary>Counts this node and all descendants.</summary>
    public int CountNodes()
    {
        if (IsLeaf) return 1;
        int total = 1;
        for (int i = 0; i < 4; i++) total += children[i].CountNodes();
        return total;
    }

    /// <summary>Draws the node bounds as green wire rectangles (for demos).</summary>
    public void DrawGizmos(int maxDepth)
    {
        float alpha = Mathf.Lerp(0.35f, 0.85f, depth / (float)(maxDepth + 1));
        Gizmos.color = new Color(0f, 1f, 0.25f, alpha);

        var corners = bounds.Corners();
        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);

        if (!IsLeaf)
            for (int i = 0; i < 4; i++)
                children[i].DrawGizmos(maxDepth);
    }
}
