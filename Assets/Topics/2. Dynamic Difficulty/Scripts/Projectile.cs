using UnityEngine;

// We wrap everything inside a namespace to keep everything clean, and avoid any issues with clashing names across modules.
namespace DDA.Core
{
    // The [RequireComponent(typeof(<TYPE>),...)] tag enforces that the GameObject this component is attached to MUST ALSO have components of the specified type(s)

    /// <summary>
    /// Simple forward-moving projectile with trigger hit detection.
    /// Requires a kinematic Rigidbody + trigger collider for reliable OnTriggerEnter.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    [DisallowMultipleComponent]
    [AddComponentMenu("DDA Demo/Gameplay/Projectile")]
    public class Projectile : MonoBehaviour
    {
        [Tooltip("Seconds before auto-destroy if nothing is hit.")]
        public float lifetime = 3f;

        private PerformanceTracker _performance;
        private float _speed;
        private Vector3 _direction;
        private float _timer;
        private Rigidbody _rb;

        /// <summary>Called immediately after instantiate by PlayerController.</summary>
        public void Init(PerformanceTracker perf, float speed, Vector3 direction)
        {
            _performance = perf;
            _speed = speed;
            _direction = direction.normalized;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true; // move manually; triggers still fire.
            var col = GetComponent<Collider>();
            col.isTrigger = true;   // ensure OnTriggerEnter is used.
        }

        private void Update()
        {
            // Move straight forward in world space.
            transform.position += _direction * (_speed * Time.deltaTime);

            // Lifetime expiry (counts as a miss; the fired shot was already registered).
            _timer += Time.deltaTime;
            if (_timer >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Minimal tag check.
            if (other.CompareTag("Enemy"))
            {
                _performance?.RegisterShotHit();

                var enemy = other.GetComponent<Enemy>();
                if (enemy) enemy.TakeHit();

                Destroy(gameObject);
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("Environment"))
            {
                // Hit a wall; destroy (miss).
                Destroy(gameObject);
            }
        }

        // EXTENSION IDEAS
        // • Add ricochet count, penetration, or damage falloff with distance.
        // • Use Raycast per-frame to avoid tunneling at very high speeds.
    }
}
