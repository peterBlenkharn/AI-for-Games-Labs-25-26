using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One node in the quadtree: holds bounds, depth, bodies if leaf, or 4 children if subdivided.
/// </summary>
public class QuadNode
{
    // ADVANCED KEYWORD NOTE:
    // 'readonly' means assignable only here or in the constructor
    // We use it to communicate these values never change after creation
    public readonly AABB bounds;
    public readonly int depth;

    private readonly int nodeCapacity;
    private readonly int maxDepth;

    private List<Body> bodies;   // used when this node is a leaf (has no further nodes inside it, just bodies!)
    private QuadNode[] children; // 4 children when subdivided; null if leaf

    public bool IsLeaf => children == null; // shorthand to set this property to true if children==null

    public QuadNode(AABB bounds, int depth, int nodeCapacity, int maxDepth)
    {
        this.bounds = bounds;
        this.depth = depth;
        this.nodeCapacity = Mathf.Max(1, nodeCapacity);
        this.maxDepth = Mathf.Max(0, maxDepth);

        bodies = new List<Body>(this.nodeCapacity);
        children = null;
    }

    public bool Insert(Body body)
    {
        if (!bounds.ContainsPoint(body.Position)) return false;

        // if this node is a leaf (doesn't just contain other nodes), and there's space in this node, add this body
        if (IsLeaf && bodies.Count < nodeCapacity)
        {
            bodies.Add(body);
            return true;
        }

        // but if this is a leaf and you've reached the max number of bodies in a node - subdivide the node and keep going
        if (IsLeaf && depth < maxDepth)
        {
            Subdivide();
            for (int i = bodies.Count - 1; i >= 0; i--)
            {
                Body existing = bodies[i];
                if (InsertIntoChildren(existing))
                    bodies.RemoveAt(i);
            }
        }

        // if this node isn't a leaf, then keep going deeper into its child nodes to find a leaf and insert into that
        if (!IsLeaf && InsertIntoChildren(body)) return true;

        // this bit is where you'd handle any weird edge cases or what to do on maximum depth by deafult etc.
        bodies.Add(body); 
        return true;
    }

    private bool InsertIntoChildren(Body body)
    {
        if (children == null) return false;
        // check which child of the node you're inserting a body into contains the body position, and insert it into that one
        // bear in mind for larger objects which one you put it in is subjective and is something that depends on design a lot...
        for (int i = 0; i < 4; i++)
        {
            if (children[i].bounds.ContainsPoint(body.Position))
                return children[i].Insert(body);
        }
        return false;
    }

    private void Subdivide()
    {
        Vector2 centre = bounds.centre;
        Vector2 childHalf = bounds.halfSize * 0.5f;

        children = new QuadNode[4];
        // 0 TL, 1 TR, 2 BL, 3 BR
        children[0] = new QuadNode(new AABB(new Vector2(centre.x - childHalf.x, centre.y + childHalf.y), childHalf), depth + 1, nodeCapacity, maxDepth);
        children[1] = new QuadNode(new AABB(new Vector2(centre.x + childHalf.x, centre.y + childHalf.y), childHalf), depth + 1, nodeCapacity, maxDepth);
        children[2] = new QuadNode(new AABB(new Vector2(centre.x - childHalf.x, centre.y - childHalf.y), childHalf), depth + 1, nodeCapacity, maxDepth);
        children[3] = new QuadNode(new AABB(new Vector2(centre.x + childHalf.x, centre.y - childHalf.y), childHalf), depth + 1, nodeCapacity, maxDepth);
    }

    /// <summary>
    /// Adds all bodies whose centres lie in (or on) 'area' to 'results'.
    /// We pass a buffer list to avoid per-frame allocations.
    /// </summary>
    public void Query(AABB area, List<Body> results)
    {
        if (!bounds.Intersects(area)) return;

        if (IsLeaf)
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                Body b = bodies[i];
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

    public int CountNodes()
    {
        if (IsLeaf) return 1;
        int total = 1;
        for (int i = 0; i < 4; i++) total += children[i].CountNodes();
        return total;
    }

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
