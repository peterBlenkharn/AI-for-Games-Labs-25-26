using UnityEngine;

/// <summary>
/// A tiny moving dot with a radius. Also supports a short red "hit flash"
/// when a collision is detected by the demo controller.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class Body : MonoBehaviour
{
    [Tooltip("Collision / interaction radius in world units.")]
    public float radius = 0.15f;

    [Tooltip("Movement speed in world units per second.")]
    public float speed = 1.5f;

    [Tooltip("Movement direction on the XY plane (normalised internally).")]
    public Vector2 direction = new Vector2(0.8f, 0.6f);

    /// <summary>Set true by the demo when this body is colliding this frame.</summary>
    [HideInInspector] public bool isColliding;

    /// <summary>Convenience accessor used by the quadtree.</summary>
    public Vector2 Position => new Vector2(transform.position.x, transform.position.y);

    // Visual flash state
    private Renderer cachedRenderer;
    private Color baseColour = Color.white;
    private float flashTimer;               // seconds remaining for red flash
    private static readonly Color FlashColour = new Color(1f, 0.2f, 0.2f, 1f);

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        // Cache the starting colour; works for both MeshRenderer and SpriteRenderer materials.
        if (cachedRenderer != null && cachedRenderer.material != null)
            baseColour = cachedRenderer.material.color;
    }

    private void Reset()
    {
        direction = Random.insideUnitCircle.normalized;
    }

    private void Update()
    {
        // Move on XY plane, frame-rate independent.
        Vector3 current = transform.position;
        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        current.x += dir.x * speed * Time.deltaTime;
        current.y += dir.y * speed * Time.deltaTime;
        transform.position = current;

        // Update flash timer and material colour.
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            SetColour(FlashColour);
        }
        else
        {
            SetColour(baseColour);
        }

        // Reset per-frame collision flag (the demo sets it true after broad-phase each frame).
        isColliding = false;
    }

    private void SetColour(Color c)
    {
        if (cachedRenderer != null && cachedRenderer.material != null)
            cachedRenderer.material.color = c; // Note: .material instantiates a copy (fine for the demo but not scalable!)
    }

    /// <summary>Trigger a short red flash on this body's material.</summary>
    public void FlashHit(float seconds)
    {
        flashTimer = Mathf.Max(flashTimer, seconds);
    }

    private void OnDrawGizmos()
    {
        // Draw the body's radius in Scene view; red if colliding this frame.
        Gizmos.color = isColliding ? new Color(1f, 0.2f, 0.2f, 0.95f) : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);

        Gizmos.color = isColliding ? new Color(1f, 0.2f, 0.2f, 0.95f) : new Color(1f, 1f, 0.5f, 1f);
        Gizmos.DrawSphere(transform.position, 0.04f);
    }
}
