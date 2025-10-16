using UnityEngine;

/// <summary>
/// A simple 2D Axis-Aligned Bounding Box (AABB) for the XY plane.
/// </summary>
[System.Serializable]
public struct AABB
{
    /// <summary>Centre of the box in world units.</summary>
    public Vector2 centre;

    /// <summary>Half-size extents in each axis (i.e., from centre to edge).</summary>
    public Vector2 halfSize;

    public AABB(Vector2 centre, Vector2 halfSize)
    {
        this.centre = centre;
        this.halfSize = halfSize;
    }

    /// <summary>True if the point lies inside this box (edges inclusive).</summary>
    public bool ContainsPoint(Vector2 point)
    {
        bool insideX = Mathf.Abs(point.x - centre.x) <= halfSize.x;
        bool insideY = Mathf.Abs(point.y - centre.y) <= halfSize.y;
        return insideX && insideY;
    }

    /// <summary>True if this box intersects another box at all.</summary>
    public bool Intersects(AABB other)
    {
        bool overlapX = Mathf.Abs(centre.x - other.centre.x) <= (halfSize.x + other.halfSize.x);
        bool overlapY = Mathf.Abs(centre.y - other.centre.y) <= (halfSize.y + other.halfSize.y);
        return overlapX && overlapY;
    }

    /// <summary>Clockwise world-space corners (BL, TL, TR, BR) for drawing.</summary>
    public Vector3[] Corners()
    {
        Vector3 bottomLeft = new Vector3(centre.x - halfSize.x, centre.y - halfSize.y, 0f);
        Vector3 topLeft = new Vector3(centre.x - halfSize.x, centre.y + halfSize.y, 0f);
        Vector3 topRight = new Vector3(centre.x + halfSize.x, centre.y + halfSize.y, 0f);
        Vector3 bottomRight = new Vector3(centre.x + halfSize.x, centre.y - halfSize.y, 0f);
        return new[] { bottomLeft, topLeft, topRight, bottomRight };
    }
}
