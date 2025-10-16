using UnityEngine;

/// <summary>
/// A simple, circular obstacle for teaching purposes.
/// Boids will steer away from the obstacle’s edge using an extendable falloff model.
/// 
/// Optionally add a <see cref="CircleCollider2D"/> to define the physical radius.
/// If none is present, the fallback <see cref="obstacleRadius"/> is used.
/// The <see cref="influenceRadius"/> extends the avoidance sensing band outward.
/// </summary>
public class Obstacle : MonoBehaviour
{
    [Tooltip("How far from this obstacle boids start to steer away (adds to the boid's sense radius).")]
    public float influenceRadius = 0.8f;

    [Header("Shape (optional)")]
    [Tooltip("If no CircleCollider2D present, use this as the obstacle's physical radius.")]
    public float obstacleRadius = 0.5f;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.35f);
        Gizmos.DrawSphere(transform.position, 0.08f);

        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, influenceRadius);

        CircleCollider2D cc = GetComponent<CircleCollider2D>();
        if (cc != null)
        {
            float worldRadius = cc.radius * Mathf.Abs(transform.lossyScale.x);
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, worldRadius);
        }
        else
        {
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, obstacleRadius);
        }
    }
}
